using System.Xml.Linq;
using WsdlExMachina.Parser.Models;
using WsdlExMachina.Parser.Utilities;

namespace WsdlExMachina.Parser.Builders;

/// <summary>
/// Builder for creating WsdlComplexType objects from XML.
/// </summary>
public class ComplexTypeBuilder
{
    private readonly XElement _complexTypeElement;
    private readonly string _schemaNamespace;
    private readonly WsdlComplexType _complexType = new();
    private const string XsdNamespace = "http://www.w3.org/2001/XMLSchema";

    /// <summary>
    /// Initializes a new instance of the <see cref="ComplexTypeBuilder"/> class.
    /// </summary>
    /// <param name="complexTypeElement">The XML element containing the complex type.</param>
    /// <param name="schemaNamespace">The namespace of the schema.</param>
    /// <exception cref="ArgumentNullException">Thrown when complexTypeElement is null.</exception>
    public ComplexTypeBuilder(XElement complexTypeElement, string schemaNamespace)
    {
        _complexTypeElement = complexTypeElement ?? throw new ArgumentNullException(nameof(complexTypeElement));
        _schemaNamespace = schemaNamespace;
    }

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

        return _complexType;
    }
}
