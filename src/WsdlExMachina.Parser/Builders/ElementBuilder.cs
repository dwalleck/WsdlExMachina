using System.Xml.Linq;
using WsdlExMachina.Parser.Models;
using WsdlExMachina.Parser.Utilities;

namespace WsdlExMachina.Parser.Builders;

/// <summary>
/// Builder for creating WsdlElement objects from XML.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ElementBuilder"/> class.
/// </remarks>
/// <param name="elementElement">The XML element containing the element.</param>
/// <param name="schemaNamespace">The namespace of the schema.</param>
/// <exception cref="ArgumentNullException">Thrown when elementElement is null.</exception>
public class ElementBuilder(XElement elementElement, string schemaNamespace)
{
    private readonly XElement _elementElement = elementElement ?? throw new ArgumentNullException(nameof(elementElement));
    private readonly string _schemaNamespace = schemaNamespace;
    private readonly WsdlElement _element = new();

    /// <summary>
    /// Builds the WsdlElement object and extracts any inline complex type.
    /// </summary>
    /// <returns>A tuple containing the built WsdlElement and an optional extracted WsdlComplexType.</returns>
    public (WsdlElement Element, WsdlComplexType? ExtractedComplexType) Build()
    {
        _element.Name = _elementElement.Attribute("name")?.Value ?? string.Empty;
        _element.Namespace = _schemaNamespace;

        // Parse type attribute
        QualifiedNameParser.ProcessQualifiedAttribute(
            _elementElement.Attribute("type"),
            _elementElement,
            _element,
            (e, name) => e.Type = name,
            (e, ns) => e.TypeNamespace = ns,
            _schemaNamespace);

        // Parse min/max occurs
        var minOccursAttr = _elementElement.Attribute("minOccurs");
        if (minOccursAttr != null && int.TryParse(minOccursAttr.Value, out int minOccurs))
        {
            _element.MinOccurs = minOccurs;
            _element.IsOptional = minOccurs == 0;
        }

        var maxOccursAttr = _elementElement.Attribute("maxOccurs");
        if (maxOccursAttr != null)
        {
            if (maxOccursAttr.Value == "unbounded")
            {
                _element.MaxOccurs = int.MaxValue;
                _element.IsArray = true;
            }
            else if (int.TryParse(maxOccursAttr.Value, out int maxOccurs))
            {
                _element.MaxOccurs = maxOccurs;
                _element.IsArray = maxOccurs > 1;
            }
        }

        // Check if this element has a complex type defined inline
        var complexTypeElement = _elementElement.Elements().FirstOrDefault(e => e.Name.LocalName == "complexType");
        if (complexTypeElement != null)
        {
            _element.IsComplexType = true;

            // Generate a name for the complex type based on the element name
            string complexTypeName = _element.Name + "Type";

            // Create a new XElement for the complex type with the generated name
            var namedComplexTypeElement = new XElement(complexTypeElement);
            namedComplexTypeElement.Add(new XAttribute("name", complexTypeName));

            // Build the complex type and get any nested extracted complex types
            var (complexType, extractedNestedComplexTypes) = new ComplexTypeBuilder(namedComplexTypeElement, _schemaNamespace).Build();

            // Set the element's type to reference this complex type
            _element.Type = complexTypeName;
            _element.TypeNamespace = _schemaNamespace;

            // Return the element and the extracted complex type
            // The TypesBuilder will handle adding the nested complex types
            return (_element, complexType);
        }

        // If there's no inline complex type, just return the element with no extracted complex type
        return (_element, null);
    }
}
