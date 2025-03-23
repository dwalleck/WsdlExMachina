using System.Text;
using WsdlExMachina.Parser.Models;

namespace WsdlExMachina.CSharpGenerator;

/// <summary>
/// Generates C# request model classes from WSDL definitions.
/// </summary>
public class RequestModelGenerator
{
    private readonly TypeMapper _typeMapper;
    private readonly NamingHelper _namingHelper;
    private readonly Dictionary<string, string> _generatedTypes;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestModelGenerator"/> class.
    /// </summary>
    public RequestModelGenerator()
    {
        _typeMapper = new TypeMapper();
        _namingHelper = new NamingHelper();
        _generatedTypes = new Dictionary<string, string>();
    }

    /// <summary>
    /// Generates C# request model classes from a WSDL definition.
    /// </summary>
    /// <param name="wsdl">The WSDL definition.</param>
    /// <returns>A dictionary of file names to file contents.</returns>
    public Dictionary<string, string> GenerateRequestModels(WsdlDefinition wsdl)
    {
        var result = new Dictionary<string, string>();
        _generatedTypes.Clear();

        // Generate classes for all operations
        foreach (var portType in wsdl.PortTypes)
        {
            foreach (var operation in portType.Operations)
            {
                if (operation.Input == null)
                {
                    continue;
                }

                // Find the message for this operation
                var message = wsdl.Messages.FirstOrDefault(m =>
                    m.Name == operation.Input.Message ||
                    m.Name == operation.Input.Message.Split(':').Last());

                if (message == null)
                {
                    continue;
                }

                // Generate the request model class
                var (className, classContent) = GenerateRequestModelClass(wsdl, message, operation.Name);
                if (!string.IsNullOrEmpty(classContent))
                {
                    result[className + ".cs"] = classContent;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Generates a C# request model class for a WSDL message.
    /// </summary>
    /// <param name="wsdl">The WSDL definition.</param>
    /// <param name="message">The WSDL message.</param>
    /// <param name="operationName">The name of the operation.</param>
    /// <returns>A tuple containing the class name and class content.</returns>
    private (string className, string classContent) GenerateRequestModelClass(
        WsdlDefinition wsdl,
        WsdlMessage message,
        string operationName)
    {
        var className = _namingHelper.GetSafeClassName(operationName + "Request");
        var csharpNamespace = _typeMapper.GetCSharpNamespace(wsdl.TargetNamespace);
        var sb = new StringBuilder();

        // Add using statements
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Xml.Serialization;");
        sb.AppendLine();

        // Add namespace
        sb.AppendLine($"namespace {csharpNamespace};");
        sb.AppendLine();

        // Add class documentation
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// Represents a request for the {operationName} operation.");
        sb.AppendLine("/// </summary>");

        // Add class declaration with XML serialization attributes
        sb.AppendLine("[XmlRoot(ElementName = \"" + message.Name + "\", Namespace = \"" + wsdl.TargetNamespace + "\")]");
        sb.AppendLine($"public class {className}");
        sb.AppendLine("{");

        // Process each message part
        foreach (var part in message.Parts)
        {
            // If the part references an element, find the element
            if (!string.IsNullOrEmpty(part.Element))
            {
                var element = wsdl.Types.Elements.FirstOrDefault(e =>
                    e.Name == part.Element ||
                    e.Name == part.Element.Split(':').Last());

                if (element != null)
                {
                    // Find the complex type for this element
                    // If element.Type is empty, look for a complex type with the same name as the element
                    var complexType = string.IsNullOrEmpty(element.Type)
                        ? wsdl.Types.ComplexTypes.FirstOrDefault(ct => ct.Name == element.Name)
                        : wsdl.Types.ComplexTypes.FirstOrDefault(ct =>
                            ct.Name == element.Type ||
                            ct.Name == element.Type.Split(':').Last());

                    if (complexType != null)
                    {
                        // Generate the complex type class to ensure it's available
                        GenerateComplexTypeClass(wsdl, complexType);

                        // Add all properties from the complex type directly to the request class
                        foreach (var childElement in complexType.Elements)
                        {
                            // Add property for each child element
                            var propertyName = _namingHelper.GetSafePropertyName(childElement.Name);
                            string typeName;

                            // Handle ArrayOfX types
                            if (childElement.Type.StartsWith("ArrayOf", StringComparison.OrdinalIgnoreCase))
                            {
                                // Extract the element type from the ArrayOf name
                                var elementTypeName = childElement.Type.Substring("ArrayOf".Length);

                                // Check if it's a complex type or a simple type
                                var isComplexType = wsdl.Types.ComplexTypes.Any(ct =>
                                    ct.Name == elementTypeName ||
                                    ct.Name == elementTypeName.Split(':').Last());

                                if (isComplexType)
                                {
                                    typeName = $"List<{_namingHelper.GetSafeClassName(elementTypeName)}>";
                                }
                                else
                                {
                                    // Special case for string to ensure lowercase
                                    if (elementTypeName.Equals("String", StringComparison.OrdinalIgnoreCase))
                                    {
                                        typeName = "List<string>";
                                    }
                                    else
                                    {
                                        typeName = $"List<{_typeMapper.MapToCSharpType(elementTypeName, childElement.TypeNamespace)}>";
                                    }
                                }
                            }
                            else
                            {
                                typeName = childElement.IsComplexType
                                    ? _namingHelper.GetSafeClassName(childElement.Type)
                                    : _typeMapper.MapToCSharpType(childElement.Type, childElement.TypeNamespace);

                                // Handle arrays
                                if (childElement.IsArray)
                                {
                                    typeName = $"List<{typeName}>";
                                }
                            }

                            // Add property documentation
                            sb.AppendLine("    /// <summary>");
                            sb.AppendLine($"    /// Gets or sets the {propertyName}.");
                            sb.AppendLine("    /// </summary>");

                            // Add property with XML serialization attributes
                            sb.AppendLine($"    [XmlElement(ElementName = \"{childElement.Name}\")]");
                            sb.AppendLine($"    public {typeName} {propertyName} {{ get; set; }}");
                            sb.AppendLine();
                        }
                    }
                    else
                    {
                        // If we can't find the complex type, create a property for the element
                        var propertyName = _namingHelper.GetSafePropertyName(part.Name);

                        // Add property documentation
                        sb.AppendLine("    /// <summary>");
                        sb.AppendLine($"    /// Gets or sets the {propertyName}.");
                        sb.AppendLine("    /// </summary>");

                        // Add property with XML serialization attributes
                        sb.AppendLine($"    [XmlElement(ElementName = \"{element.Name}\")]");

                        // Generate a class for the element
                        var (elementClassName, _) = GenerateElementClass(wsdl, element);
                        sb.AppendLine($"    public {elementClassName} {propertyName} {{ get; set; }}");
                        sb.AppendLine();
                    }
                }
            }
            // If the part references a type, use the type directly
            else if (!string.IsNullOrEmpty(part.Type))
            {
                var typeName = _typeMapper.MapToCSharpType(part.Type, part.TypeNamespace);
                var propertyName = _namingHelper.GetSafePropertyName(part.Name);

                // Add property documentation
                sb.AppendLine("    /// <summary>");
                sb.AppendLine($"    /// Gets or sets the {propertyName}.");
                sb.AppendLine("    /// </summary>");

                // Add property with XML serialization attributes
                sb.AppendLine($"    [XmlElement(ElementName = \"{part.Name}\")]");
                sb.AppendLine($"    public {typeName} {propertyName} {{ get; set; }}");
                sb.AppendLine();
            }
        }

        sb.AppendLine("}");

        // Add the generated type to the dictionary
        _generatedTypes[className] = sb.ToString();

        return (className, sb.ToString());
    }

    /// <summary>
    /// Adds a property for a WSDL element to the class.
    /// </summary>
    /// <param name="sb">The string builder.</param>
    /// <param name="element">The WSDL element.</param>
    /// <param name="partName">The name of the message part.</param>
    private void AddElementProperty(StringBuilder sb, WsdlElement element, string partName)
    {
        var propertyName = _namingHelper.GetSafePropertyName(partName);
        string typeName;

        // Handle ArrayOfX types
        if (element.Type.StartsWith("ArrayOf", StringComparison.OrdinalIgnoreCase))
        {
            // Extract the element type from the ArrayOf name
            var elementTypeName = element.Type.Substring("ArrayOf".Length);

            // For simplicity, we'll assume it's a complex type if it's not a built-in type
            if (_typeMapper.MapToCSharpType(elementTypeName, element.TypeNamespace) == elementTypeName)
            {
                typeName = $"List<{_namingHelper.GetSafeClassName(elementTypeName)}>";
            }
            else
            {
                typeName = $"List<{_typeMapper.MapToCSharpType(elementTypeName, element.TypeNamespace)}>";
            }
        }
        else
        {
            typeName = element.IsComplexType
                ? _namingHelper.GetSafeClassName(element.Type)
                : _typeMapper.MapToCSharpType(element.Type, element.TypeNamespace);

            // Handle arrays
            if (element.IsArray)
            {
                typeName = $"List<{typeName}>";
            }
        }

        // Add property documentation
        sb.AppendLine("    /// <summary>");
        sb.AppendLine($"    /// Gets or sets the {propertyName}.");
        sb.AppendLine("    /// </summary>");

        // Add property with XML serialization attributes
        sb.AppendLine($"    [XmlElement(ElementName = \"{element.Name}\")]");
        sb.AppendLine($"    public {typeName} {propertyName} {{ get; set; }}");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates C# classes for all complex types in a WSDL definition.
    /// </summary>
    /// <param name="wsdl">The WSDL definition.</param>
    /// <returns>A dictionary of file names to file contents.</returns>
    public Dictionary<string, string> GenerateComplexTypes(WsdlDefinition wsdl)
    {
        var result = new Dictionary<string, string>();

        foreach (var complexType in wsdl.Types.ComplexTypes)
        {
            // Skip generating classes for ArrayOfX types
            if (complexType.Name.StartsWith("ArrayOf", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var (className, classContent) = GenerateComplexTypeClass(wsdl, complexType);
            if (!string.IsNullOrEmpty(classContent))
            {
                result[className + ".cs"] = classContent;
            }
        }

        return result;
    }

    /// <summary>
    /// Generates a C# class for a WSDL complex type.
    /// </summary>
    /// <param name="wsdl">The WSDL definition.</param>
    /// <param name="complexType">The WSDL complex type.</param>
    /// <returns>A tuple containing the class name and class content.</returns>
    private (string className, string classContent) GenerateComplexTypeClass(
        WsdlDefinition wsdl,
        WsdlComplexType complexType)
    {
        var className = _namingHelper.GetSafeClassName(complexType.Name);
        var csharpNamespace = _typeMapper.GetCSharpNamespace(wsdl.TargetNamespace);
        var sb = new StringBuilder();

        // Check if we've already generated this type
        if (_generatedTypes.ContainsKey(className))
        {
            return (className, _generatedTypes[className]);
        }

        // Skip generating classes for ArrayOfX types
        if (complexType.Name.StartsWith("ArrayOf", StringComparison.OrdinalIgnoreCase))
        {
            // We'll handle these types directly as List<T> in the properties
            // Just add an empty entry to _generatedTypes to mark it as processed
            _generatedTypes[className] = string.Empty;
            return (className, string.Empty);
        }

        // Add using statements
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Xml.Serialization;");
        sb.AppendLine();

        // Add namespace
        sb.AppendLine($"namespace {csharpNamespace};");
        sb.AppendLine();

        // Add class documentation
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// Represents the {complexType.Name} complex type.");
        sb.AppendLine("/// </summary>");

        // Add class declaration with XML serialization attributes
        sb.AppendLine("[XmlType(TypeName = \"" + complexType.Name + "\", Namespace = \"" + complexType.Namespace + "\")]");

        // Handle inheritance
        if (!string.IsNullOrEmpty(complexType.BaseType))
        {
            var baseTypeName = _namingHelper.GetSafeClassName(complexType.BaseType);
            sb.AppendLine($"public class {className} : {baseTypeName}");
        }
        else
        {
            sb.AppendLine($"public class {className}");
        }

        sb.AppendLine("{");

        // Handle array types
        if (complexType.IsArray)
        {
            var itemTypeName = _typeMapper.MapToCSharpType(complexType.ArrayItemType ?? "string", complexType.ArrayItemTypeNamespace);

            // Add array item property
            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// Gets or sets the items.");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    [XmlElement(ElementName = \"Item\")]");
            sb.AppendLine($"    public List<{itemTypeName}> Items {{ get; set; }} = new List<{itemTypeName}>();");
            sb.AppendLine();
        }
        else
        {
            // Process each element in the complex type
            foreach (var element in complexType.Elements)
            {
                AddElementProperty(sb, element, element.Name);
            }
        }

        sb.AppendLine("}");

        // Add the generated type to the dictionary
        _generatedTypes[className] = sb.ToString();

        return (className, sb.ToString());
    }

    /// <summary>
    /// Generates C# classes for all simple types in a WSDL definition.
    /// </summary>
    /// <param name="wsdl">The WSDL definition.</param>
    /// <returns>A dictionary of file names to file contents.</returns>
    public Dictionary<string, string> GenerateSimpleTypes(WsdlDefinition wsdl)
    {
        var result = new Dictionary<string, string>();

        foreach (var simpleType in wsdl.Types.SimpleTypes)
        {
            // Only generate enums for simple types that are enumerations
            if (simpleType.IsEnum)
            {
                var (enumName, enumContent) = GenerateEnumType(wsdl, simpleType);
                if (!string.IsNullOrEmpty(enumContent))
                {
                    result[enumName + ".cs"] = enumContent;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Generates a C# enum for a WSDL simple type that is an enumeration.
    /// </summary>
    /// <param name="wsdl">The WSDL definition.</param>
    /// <param name="simpleType">The WSDL simple type.</param>
    /// <returns>A tuple containing the enum name and enum content.</returns>
    private (string enumName, string enumContent) GenerateEnumType(
        WsdlDefinition wsdl,
        WsdlSimpleType simpleType)
    {
        var enumName = _namingHelper.GetSafeClassName(simpleType.Name);
        var csharpNamespace = _typeMapper.GetCSharpNamespace(wsdl.TargetNamespace);
        var sb = new StringBuilder();

        // Check if we've already generated this type
        if (_generatedTypes.ContainsKey(enumName))
        {
            return (enumName, _generatedTypes[enumName]);
        }

        // Add using statements
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Xml.Serialization;");
        sb.AppendLine();

        // Add namespace
        sb.AppendLine($"namespace {csharpNamespace};");
        sb.AppendLine();

        // Add enum documentation
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// Represents the {simpleType.Name} enumeration.");
        sb.AppendLine("/// </summary>");

        // Add enum declaration with XML serialization attributes
        sb.AppendLine("[XmlType(TypeName = \"" + simpleType.Name + "\", Namespace = \"" + simpleType.Namespace + "\")]");
        sb.AppendLine($"public enum {enumName}");
        sb.AppendLine("{");

        // Add enum values
        for (int i = 0; i < simpleType.EnumerationValues.Count; i++)
        {
            var value = simpleType.EnumerationValues[i];
            var enumValueName = _namingHelper.GetSafePropertyName(value);

            // Add enum value documentation
            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// {value}");
            sb.AppendLine("    /// </summary>");

            // Add enum value with XML serialization attribute
            sb.AppendLine($"    [XmlEnum(Name = \"{value}\")]");

            // Add comma if not the last value
            if (i < simpleType.EnumerationValues.Count - 1)
            {
                sb.AppendLine($"    {enumValueName},");
            }
            else
            {
                sb.AppendLine($"    {enumValueName}");
            }
        }

        sb.AppendLine("}");

        // Add the generated type to the dictionary
        _generatedTypes[enumName] = sb.ToString();

        return (enumName, sb.ToString());
    }

    /// <summary>
    /// Generates a C# class for a WSDL element.
    /// </summary>
    /// <param name="wsdl">The WSDL definition.</param>
    /// <param name="element">The WSDL element.</param>
    /// <returns>A tuple containing the class name and class content.</returns>
    private (string className, string classContent) GenerateElementClass(
        WsdlDefinition wsdl,
        WsdlElement element)
    {
        var className = _namingHelper.GetSafeClassName(element.Name);
        var csharpNamespace = _typeMapper.GetCSharpNamespace(wsdl.TargetNamespace);
        var sb = new StringBuilder();

        // Check if we've already generated this type
        if (_generatedTypes.ContainsKey(className))
        {
            return (className, _generatedTypes[className]);
        }

        // Skip generating classes for elements with ArrayOfX types
        if (element.Type.StartsWith("ArrayOf", StringComparison.OrdinalIgnoreCase))
        {
            // We'll handle these types directly as List<T> in the properties
            // Just add an empty entry to _generatedTypes to mark it as processed
            _generatedTypes[className] = string.Empty;
            return (className, string.Empty);
        }

        // Add using statements
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Xml.Serialization;");
        sb.AppendLine();

        // Add namespace
        sb.AppendLine($"namespace {csharpNamespace};");
        sb.AppendLine();

        // Add class documentation
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// Represents the {element.Name} element.");
        sb.AppendLine("/// </summary>");

        // Add class declaration with XML serialization attributes
        sb.AppendLine("[XmlType(TypeName = \"" + element.Name + "\", Namespace = \"" + element.Namespace + "\")]");
        sb.AppendLine($"public class {className}");
        sb.AppendLine("{");

        // Add properties based on the element type
        if (element.IsComplexType)
        {
            // Find the complex type
            var complexType = wsdl.Types.ComplexTypes.FirstOrDefault(ct =>
                ct.Name == element.Type ||
                ct.Name == element.Type.Split(':').Last());

            if (complexType != null)
            {
                // Process each element in the complex type
                foreach (var childElement in complexType.Elements)
                {
                    AddElementProperty(sb, childElement, childElement.Name);
                }
            }
        }
        else
        {
            // Simple type - add a single property
            var typeName = _typeMapper.MapToCSharpType(element.Type, element.TypeNamespace);

            // Add property documentation
            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// Gets or sets the value.");
            sb.AppendLine("    /// </summary>");

            // Add property with XML serialization attributes
            sb.AppendLine("    [XmlElement(ElementName = \"Value\")]");
            sb.AppendLine($"    public {typeName} Value {{ get; set; }}");
            sb.AppendLine();
        }

        sb.AppendLine("}");

        // Add the generated type to the dictionary
        _generatedTypes[className] = sb.ToString();

        return (className, sb.ToString());
    }
}
