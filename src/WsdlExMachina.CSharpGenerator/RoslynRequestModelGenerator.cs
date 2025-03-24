using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WsdlExMachina.Parser.Models;

namespace WsdlExMachina.CSharpGenerator;

/// <summary>
/// Generates C# request model classes for WSDL messages using the Roslyn API.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="RoslynRequestModelGenerator"/> class.
/// </remarks>
/// <param name="codeGenerator">The code generator.</param>
/// <param name="complexTypeGenerator">The complex type generator.</param>
/// <param name="enumGenerator">The enum generator.</param>
public class RoslynRequestModelGenerator(
    RoslynCodeGenerator codeGenerator,
    RoslynComplexTypeGenerator complexTypeGenerator,
    RoslynEnumGenerator enumGenerator)
{
    private readonly RoslynCodeGenerator _codeGenerator = codeGenerator ?? throw new ArgumentNullException(nameof(codeGenerator));
    private readonly RoslynComplexTypeGenerator _complexTypeGenerator = complexTypeGenerator ?? throw new ArgumentNullException(nameof(complexTypeGenerator));
    private readonly RoslynEnumGenerator _enumGenerator = enumGenerator ?? throw new ArgumentNullException(nameof(enumGenerator));
    private readonly NamingHelper _namingHelper = new NamingHelper();
    private readonly TypeMapper _typeMapper = new TypeMapper();
    private readonly Dictionary<string, CompilationUnitSyntax> _generatedTypes = new Dictionary<string, CompilationUnitSyntax>();

    /// <summary>
    /// Generates C# request model classes from a WSDL definition.
    /// </summary>
    /// <param name="wsdl">The WSDL definition.</param>
    /// <param name="namespaceName">The namespace name.</param>
    /// <returns>A dictionary of file names to generated code.</returns>
    public Dictionary<string, string> GenerateRequestModels(WsdlDefinition wsdl, string namespaceName)
    {
        ArgumentNullException.ThrowIfNull(wsdl);

        if (string.IsNullOrEmpty(namespaceName))
        {
            throw new ArgumentException("Namespace name cannot be null or empty.", nameof(namespaceName));
        }

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
                var className = _namingHelper.GetSafeClassName(operation.Name + "Request");
                var compilationUnit = GenerateRequestModel(wsdl, message, operation.Name, namespaceName);

                // Skip empty compilation units
                if (compilationUnit.Members.Count > 0)
                {
                    var code = _codeGenerator.FormatNode(compilationUnit);
                    result[className + ".cs"] = code;
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
    /// <param name="namespaceName">The namespace name.</param>
    /// <returns>A compilation unit syntax representing the class.</returns>
    public CompilationUnitSyntax GenerateRequestModel(
        WsdlDefinition wsdl,
        WsdlMessage message,
        string operationName,
        string namespaceName)
    {
        ArgumentNullException.ThrowIfNull(wsdl);

        ArgumentNullException.ThrowIfNull(message);

        if (string.IsNullOrEmpty(operationName))
        {
            throw new ArgumentException("Operation name cannot be null or empty.", nameof(operationName));
        }

        if (string.IsNullOrEmpty(namespaceName))
        {
            throw new ArgumentException("Namespace name cannot be null or empty.", nameof(namespaceName));
        }

        // Check if we've already generated this type
        var className = _namingHelper.GetSafeClassName(operationName + "Request");
        if (_generatedTypes.TryGetValue(className, out var existingType))
        {
            return existingType;
        }

        // Create file-scoped namespace
        var namespaceDeclaration = _codeGenerator.CreateNamespace(
            namespaceName,
            "System",
            "System.Collections.Generic",
            "System.Xml.Serialization");

        // Get using directives
        var usingDirectives = _codeGenerator.GetUsingDirectives(namespaceDeclaration);

        // Create XML root attribute
        var xmlRootAttribute = _codeGenerator.CreateXmlAttribute(
            "XmlRoot",
            ("ElementName", message.Name),
            ("Namespace", wsdl.TargetNamespace ?? string.Empty));

        // Create class declaration
        var classDeclaration = SyntaxFactory.ClassDeclaration(className)
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .AddAttributeLists(
                SyntaxFactory.AttributeList(
                    SyntaxFactory.SingletonSeparatedList(xmlRootAttribute)));

        // Add documentation
        var documentationComment = _codeGenerator.CreateDocumentationComment($"Represents a request for the {operationName} operation.");
        var documentation = SyntaxFactory.TriviaList(SyntaxFactory.Trivia(documentationComment));
        classDeclaration = classDeclaration.WithLeadingTrivia(documentation);

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
                        // Add all properties from the complex type directly to the request class
                        foreach (var childElement in complexType.Elements)
                        {
                            // Add property for each child element
                            var propertyName = _namingHelper.GetSafePropertyName(childElement.Name);
                            var typeName = GetTypeName(wsdl, childElement);

                            // Create property
                            var property = CreateProperty(
                                propertyName,
                                typeName,
                                childElement.Name,
                                $"Gets or sets the {propertyName}.");

                            // Add property to class
                            classDeclaration = classDeclaration.AddMembers(property);
                        }
                    }
                    else
                    {
                        // If we can't find the complex type, create a property for the element
                        var propertyName = _namingHelper.GetSafePropertyName(part.Name);
                        var elementClassName = _namingHelper.GetSafeClassName(element.Name);

                        // Create property
                        var property = CreateProperty(
                            propertyName,
                            elementClassName,
                            element.Name,
                            $"Gets or sets the {propertyName}.");

                        // Add property to class
                        classDeclaration = classDeclaration.AddMembers(property);
                    }
                }
            }
            // If the part references a type, use the type directly
            else if (!string.IsNullOrEmpty(part.Type))
            {
                var typeName = _typeMapper.MapToCSharpType(part.Type, part.TypeNamespace);
                var propertyName = _namingHelper.GetSafePropertyName(part.Name);

                // Create property
                var property = CreateProperty(
                    propertyName,
                    typeName,
                    part.Name,
                    $"Gets or sets the {propertyName}.");

                // Add property to class
                classDeclaration = classDeclaration.AddMembers(property);
            }
        }

        // Add class to namespace
        if (namespaceDeclaration is FileScopedNamespaceDeclarationSyntax fileScopedNamespace)
        {
            namespaceDeclaration = fileScopedNamespace.AddMembers(classDeclaration);
        }
        else if (namespaceDeclaration is NamespaceDeclarationSyntax regularNamespace)
        {
            namespaceDeclaration = regularNamespace.AddMembers(classDeclaration);
        }

        // Create compilation unit with using directives
        var compilationUnit = SyntaxFactory.CompilationUnit()
            .AddUsings(usingDirectives.ToArray())
            .AddMembers(namespaceDeclaration);

        // Add the generated type to the dictionary
        _generatedTypes[className] = compilationUnit;

        return compilationUnit;
    }

    /// <summary>
    /// Gets the C# type name for a WSDL element.
    /// </summary>
    /// <param name="wsdl">The WSDL definition.</param>
    /// <param name="element">The WSDL element.</param>
    /// <returns>The C# type name.</returns>
    private string GetTypeName(WsdlDefinition wsdl, WsdlElement element)
    {
        // Handle ArrayOfX types
        if (element.Type.StartsWith("ArrayOf", StringComparison.OrdinalIgnoreCase))
        {
            // Extract the element type from the ArrayOf name
            var elementTypeName = element.Type.Substring("ArrayOf".Length);

            // Check if it's a complex type or a simple type
            var isComplexType = wsdl.Types.ComplexTypes.Any(ct =>
                ct.Name == elementTypeName ||
                ct.Name == elementTypeName.Split(':').Last());

            if (isComplexType)
            {
                return $"List<{_namingHelper.GetSafeClassName(elementTypeName)}>";
            }
            else
            {
                // Special case for string to ensure lowercase
                if (elementTypeName.Equals("String", StringComparison.OrdinalIgnoreCase))
                {
                    return "List<string>";
                }
                else
                {
                    return $"List<{_typeMapper.MapToCSharpType(elementTypeName, element.TypeNamespace)}>";
                }
            }
        }
        else
        {
            var typeName = element.IsComplexType
                ? _namingHelper.GetSafeClassName(element.Type)
                : _typeMapper.MapToCSharpType(element.Type, element.TypeNamespace);

            // Handle arrays
            if (element.IsArray)
            {
                return $"List<{typeName}>";
            }

            return typeName;
        }
    }

    /// <summary>
    /// Creates a property with XML serialization attributes.
    /// </summary>
    /// <param name="propertyName">The property name.</param>
    /// <param name="typeName">The type name.</param>
    /// <param name="xmlElementName">The XML element name.</param>
    /// <param name="summary">The property summary (not used).</param>
    /// <returns>A property declaration syntax.</returns>
    private PropertyDeclarationSyntax CreateProperty(string propertyName, string typeName, string xmlElementName, string summary)
    {
        // Create XML element attribute
        var xmlElementAttribute = _codeGenerator.CreateXmlAttribute(
            "XmlElement",
            ("ElementName", xmlElementName));

        // Create initializer based on type
        ExpressionSyntax initializer = null;
        if (typeName.Equals("string", StringComparison.OrdinalIgnoreCase))
        {
            // Initialize string properties to string.Empty
            initializer = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("string"),
                SyntaxFactory.IdentifierName("Empty"));
        }
        else if (typeName.StartsWith("List<", StringComparison.OrdinalIgnoreCase))
        {
            // Initialize List properties to new List<T>()
            initializer = SyntaxFactory.ObjectCreationExpression(
                SyntaxFactory.ParseTypeName(typeName))
                .WithArgumentList(SyntaxFactory.ArgumentList());
        }

        // Create property
        var property = SyntaxFactory.PropertyDeclaration(
            SyntaxFactory.ParseTypeName(typeName),
            SyntaxFactory.Identifier(propertyName))
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .AddAccessorListAccessors(
                SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)))
            .AddAttributeLists(
                SyntaxFactory.AttributeList(
                    SyntaxFactory.SingletonSeparatedList(xmlElementAttribute)));

        // Add initializer if we have one
        if (initializer != null)
        {
            property = property.WithInitializer(
                SyntaxFactory.EqualsValueClause(initializer))
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
        }

        // Add a newline before the property for better readability
        var leadingTrivia = SyntaxFactory.TriviaList(
            SyntaxFactory.CarriageReturnLineFeed,
            SyntaxFactory.Comment(""),
            SyntaxFactory.CarriageReturnLineFeed);
        property = property.WithLeadingTrivia(leadingTrivia);

        return property;
    }

    /// <summary>
    /// Generates C# code for a WSDL request model.
    /// </summary>
    /// <param name="wsdl">The WSDL definition.</param>
    /// <param name="message">The WSDL message.</param>
    /// <param name="operationName">The name of the operation.</param>
    /// <param name="namespaceName">The namespace name.</param>
    /// <returns>The generated C# code as a string.</returns>
    public string GenerateRequestModelCode(
        WsdlDefinition wsdl,
        WsdlMessage message,
        string operationName,
        string namespaceName)
    {
        var compilationUnit = GenerateRequestModel(wsdl, message, operationName, namespaceName);
        return _codeGenerator.FormatNode(compilationUnit);
    }
}
