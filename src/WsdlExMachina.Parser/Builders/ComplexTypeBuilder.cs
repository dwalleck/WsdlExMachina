using System.Xml.Linq;
using WsdlExMachina.Parser.Models;
using WsdlExMachina.Parser.Utilities;

namespace WsdlExMachina.Parser.Builders;

/// <summary>
/// Builder for creating WsdlComplexType objects from XML.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ComplexTypeBuilder"/> class.
/// </remarks>
/// <param name="complexTypeElement">The XML element containing the complex type.</param>
/// <param name="schemaNamespace">The namespace of the schema.</param>
/// <exception cref="ArgumentNullException">Thrown when complexTypeElement is null.</exception>
public class ComplexTypeBuilder(XElement complexTypeElement, string schemaNamespace)
{
    private readonly XElement _complexTypeElement = complexTypeElement ?? throw new ArgumentNullException(nameof(complexTypeElement));
    private readonly string _schemaNamespace = schemaNamespace;
    private readonly WsdlComplexType _complexType = new();

    /// <summary>
    /// Builds the WsdlComplexType object.
    /// </summary>
    /// <returns>The built WsdlComplexType.</returns>
    public WsdlComplexType Build()
    {
        _complexType.Name = _complexTypeElement.Attribute("name")?.Value ?? string.Empty;
        _complexType.Namespace = _schemaNamespace;

        // Parse base type if this is an extension
        var extensionElement = _complexTypeElement.Descendants().FirstOrDefault(e => e.Name.LocalName == "extension");
        if (extensionElement != null)
        {
            QualifiedNameParser.ProcessQualifiedAttribute(
                extensionElement.Attribute("base"),
                _complexTypeElement,
                _complexType,
                (ct, name) => ct.BaseType = name,
                (ct, ns) => ct.BaseTypeNamespace = ns,
                _schemaNamespace);
        }

        // Parse elements
        var sequenceElement = _complexTypeElement.Descendants().FirstOrDefault(e => e.Name.LocalName == "sequence");
        if (sequenceElement != null)
        {
            foreach (var elementElement in sequenceElement.Elements().Where(e => e.Name.LocalName == "element"))
            {
                _complexType.Elements.Add(new ElementBuilder(elementElement, _schemaNamespace).Build());
            }
        }

        // Check if this is an array type (e.g., ArrayOfString)
        // This must be done after parsing elements so we can modify them
        DetectArrayType();

        return _complexType;
    }

    /// <summary>
    /// Detects if this complex type represents an array type (e.g., ArrayOfString).
    /// </summary>
    private void DetectArrayType()
    {
        // Check if the name follows the pattern "ArrayOfX"
        if (_complexType.Name.StartsWith("ArrayOf", StringComparison.OrdinalIgnoreCase))
        {
            // Extract the element type from the name
            string elementTypeName = _complexType.Name.Substring("ArrayOf".Length);

            // Check if this is a simple array with a single element
            var sequenceElement = _complexTypeElement.Descendants().FirstOrDefault(e => e.Name.LocalName == "sequence");
            if (sequenceElement != null)
            {
                var elements = sequenceElement.Elements().Where(e => e.Name.LocalName == "element").ToList();
                if (elements.Count == 1)
                {
                    var elementElement = elements[0];

                    // Set the IsArray flag on the element
                    var element = new ElementBuilder(elementElement, _schemaNamespace).Build();
                    element.IsArray = true;

                    // If the element doesn't have a type attribute, use the one from the array name
                    if (string.IsNullOrEmpty(element.Type))
                    {
                        element.Type = elementTypeName;
                        element.TypeNamespace = _schemaNamespace;
                    }

                    // Clear existing elements and add the array element
                    _complexType.Elements.Clear();
                    _complexType.Elements.Add(element);

                    // Mark the complex type as an array
                    _complexType.IsArray = true;
                    _complexType.ArrayItemType = elementTypeName;
                    _complexType.ArrayItemTypeNamespace = _schemaNamespace;
                }
            }
        }
    }
}
