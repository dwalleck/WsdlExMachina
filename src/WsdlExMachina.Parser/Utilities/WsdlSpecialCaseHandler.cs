using System.Xml.Linq;
using WsdlExMachina.Parser.Builders;
using WsdlExMachina.Parser.Models;

namespace WsdlExMachina.Parser.Utilities;

/// <summary>
/// Provides utility methods for handling special cases in WSDL parsing.
/// </summary>
public static class WsdlSpecialCaseHandler
{
    /// <summary>
    /// Handles complex type inheritance by processing extension elements.
    /// </summary>
    /// <param name="complexType">The complex type to process.</param>
    /// <param name="extensionElement">The extension element containing base type information.</param>
    /// <param name="schemaNamespace">The namespace of the schema.</param>
    public static void HandleComplexTypeInheritance(WsdlComplexType complexType, XElement? extensionElement, string schemaNamespace)
    {
        if (extensionElement == null)
        {
            return;
        }

        QualifiedNameParser.ProcessQualifiedAttribute(
            extensionElement.Attribute("base"),
            extensionElement,
            complexType,
            (ct, name) => ct.BaseType = name,
            (ct, ns) => ct.BaseTypeNamespace = ns,
            schemaNamespace);
    }

    /// <summary>
    /// Detects if a complex type represents an array based on its structure.
    /// </summary>
    /// <param name="complexType">The complex type to check.</param>
    /// <param name="complexTypeElement">The XML element representing the complex type.</param>
    /// <param name="schemaNamespace">The namespace of the schema.</param>
    /// <returns>True if the complex type represents an array, otherwise false.</returns>
    public static bool DetectArrayType(WsdlComplexType complexType, XElement complexTypeElement, string schemaNamespace)
    {
        // Check for "ArrayOfX" naming pattern
        if (complexType.Name.StartsWith("ArrayOf", StringComparison.OrdinalIgnoreCase))
        {
            var elementTypeName = complexType.Name.Substring("ArrayOf".Length);

            // Check if this is a simple array with a single element
            var sequenceElement = complexTypeElement.Descendants().FirstOrDefault(e => e.Name.LocalName == "sequence");
            if (sequenceElement != null)
            {
                var elements = sequenceElement.Elements().Where(e => e.Name.LocalName == "element").ToList();

                // Only process as an array if there's exactly one element
                if (elements.Count == 1)
                {
                    var elementElement = elements[0];

                    // Set the IsArray flag on the element
                    var (element, extractedComplexType) = new ElementBuilder(elementElement, schemaNamespace).Build();
                    element.IsArray = true;

                    // If the element doesn't have a type attribute, use the one from the array name
                    if (string.IsNullOrEmpty(element.Type))
                    {
                        element.Type = elementTypeName;
                        element.TypeNamespace = schemaNamespace;
                    }

                    // Clear existing elements and add the array element
                    complexType.Elements.Clear();
                    complexType.Elements.Add(element);

                    // Mark the complex type as an array
                    complexType.IsArray = true;
                    complexType.ArrayItemType = elementTypeName;
                    complexType.ArrayItemTypeNamespace = schemaNamespace;

                    return true;
                }
                else
                {
                    // If there are multiple elements, this is not a simple array type
                    // despite the "ArrayOfX" naming pattern
                    return false;
                }
            }
        }

        // Check for sequence with maxOccurs="unbounded"
        var sequenceWithUnboundedElement = complexTypeElement.Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "sequence" &&
                                 e.Elements().Any(el => el.Name.LocalName == "element" &&
                                                       (el.Attribute("maxOccurs")?.Value == "unbounded" ||
                                                        (int.TryParse(el.Attribute("maxOccurs")?.Value, out var maxOccurs) && maxOccurs > 1))));

        if (sequenceWithUnboundedElement != null)
        {
            var unboundedElement = sequenceWithUnboundedElement.Elements()
                .First(e => e.Name.LocalName == "element" &&
                           (e.Attribute("maxOccurs")?.Value == "unbounded" ||
                            (int.TryParse(e.Attribute("maxOccurs")?.Value, out var maxOccurs) && maxOccurs > 1)));

            var elementType = unboundedElement.Attribute("type")?.Value;
            if (!string.IsNullOrEmpty(elementType))
            {
                var (localName, namespaceUri) = QualifiedNameParser.Parse(elementType, unboundedElement, schemaNamespace);

                complexType.IsArray = true;
                complexType.ArrayItemType = localName;
                complexType.ArrayItemTypeNamespace = namespaceUri;

                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Processes imported schemas in a WSDL document.
    /// </summary>
    /// <param name="schemaElement">The schema element containing imports.</param>
    /// <param name="types">The WsdlTypes object to populate.</param>
    public static void ProcessImportedSchemas(XElement schemaElement, WsdlTypes types)
    {
        foreach (var importElement in schemaElement.Elements().Where(e => e.Name.LocalName == "import"))
        {
            var importedNamespace = importElement.Attribute("namespace")?.Value;
            if (!string.IsNullOrEmpty(importedNamespace))
            {
                // In a real implementation, we would need to resolve and load the imported schema
                // For now, we just record that there was an import
                types.ImportedNamespaces.Add(importedNamespace);
            }
        }
    }

    /// <summary>
    /// Processes SOAP headers defined in a WSDL document.
    /// </summary>
    /// <param name="bindingElement">The binding element containing header definitions.</param>
    /// <param name="binding">The WsdlBinding object to populate.</param>
    public static void ProcessSoapHeaders(XElement bindingElement, WsdlBinding binding)
    {
        // This would process SOAP headers at the binding level
        // For now, we'll just note that this is a capability we might want to add
    }

    /// <summary>
    /// Handles operation overloading in a WSDL document.
    /// </summary>
    /// <param name="portTypeElement">The portType element containing operations.</param>
    /// <param name="portType">The WsdlPortType object to populate.</param>
    public static void HandleOperationOverloading(XElement portTypeElement, WsdlPortType portType)
    {
        // Group operations by name
        var operationsByName = portTypeElement.Elements()
            .Where(e => e.Name.LocalName == "operation")
            .GroupBy(e => e.Attribute("name")?.Value ?? string.Empty)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Process each group of operations with the same name
        foreach (var group in operationsByName)
        {
            if (group.Value.Count > 1)
            {
                // For overloaded operations, we need to distinguish them somehow
                // One approach is to append a suffix based on input/output messages
                for (var i = 0; i < group.Value.Count; i++)
                {
                    var operationElement = group.Value[i];
                    var operation = new WsdlOperation
                    {
                        Name = operationElement.Attribute("name")?.Value ?? string.Empty
                    };

                    // If this is not the first operation with this name, append a suffix
                    if (i > 0)
                    {
                        operation.Name = $"{operation.Name}_{i}";
                    }

                    // Parse input
                    var inputElement = operationElement.Elements().FirstOrDefault(e => e.Name.LocalName == "input");
                    if (inputElement != null)
                    {
                        operation.Input = new WsdlOperationMessage
                        {
                            Name = inputElement.Attribute("name")?.Value ?? string.Empty
                        };
                    }

                    // Parse output
                    var outputElement = operationElement.Elements().FirstOrDefault(e => e.Name.LocalName == "output");
                    if (outputElement != null)
                    {
                        operation.Output = new WsdlOperationMessage
                        {
                            Name = outputElement.Attribute("name")?.Value ?? string.Empty
                        };
                    }

                    portType.Operations.Add(operation);
                }
            }
            else
            {
                // For non-overloaded operations, process normally
                var operationElement = group.Value[0];
                var operation = new WsdlOperation
                {
                    Name = operationElement.Attribute("name")?.Value ?? string.Empty
                };

                // Parse input
                var inputElement = operationElement.Elements().FirstOrDefault(e => e.Name.LocalName == "input");
                if (inputElement != null)
                {
                    operation.Input = new WsdlOperationMessage
                    {
                        Name = inputElement.Attribute("name")?.Value ?? string.Empty
                    };
                }

                // Parse output
                var outputElement = operationElement.Elements().FirstOrDefault(e => e.Name.LocalName == "output");
                if (outputElement != null)
                {
                    operation.Output = new WsdlOperationMessage
                    {
                        Name = outputElement.Attribute("name")?.Value ?? string.Empty
                    };
                }

                portType.Operations.Add(operation);
            }
        }
    }
}
