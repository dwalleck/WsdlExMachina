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
    private const string XsdNamespace = "http://www.w3.org/2001/XMLSchema";

    /// <summary>
    /// Builds the WsdlElement object.
    /// </summary>
    /// <returns>The built WsdlElement.</returns>
    public WsdlElement Build()
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

            // We don't actually add child elements to the WsdlElement since it doesn't have an Elements property
            // Instead, we mark it as a complex type and the consumer should look up the complex type definition
            // in the WsdlTypes.ComplexTypes collection
        }

        return _element;
    }
}
