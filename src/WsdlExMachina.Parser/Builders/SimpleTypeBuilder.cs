using System.Xml.Linq;
using WsdlExMachina.Parser.Models;
using WsdlExMachina.Parser.Utilities;

namespace WsdlExMachina.Parser.Builders;

/// <summary>
/// Builder for creating WsdlSimpleType objects from XML.
/// </summary>
public class SimpleTypeBuilder
{
    private readonly XElement _simpleTypeElement;
    private readonly string _schemaNamespace;
    private readonly WsdlSimpleType _simpleType = new();
    private const string XsdNamespace = "http://www.w3.org/2001/XMLSchema";

    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleTypeBuilder"/> class.
    /// </summary>
    /// <param name="simpleTypeElement">The XML element containing the simple type.</param>
    /// <param name="schemaNamespace">The namespace of the schema.</param>
    /// <exception cref="ArgumentNullException">Thrown when simpleTypeElement is null.</exception>
    public SimpleTypeBuilder(XElement simpleTypeElement, string schemaNamespace)
    {
        _simpleTypeElement = simpleTypeElement ?? throw new ArgumentNullException(nameof(simpleTypeElement));
        _schemaNamespace = schemaNamespace;
    }

    /// <summary>
    /// Builds the WsdlSimpleType object.
    /// </summary>
    /// <returns>The built WsdlSimpleType.</returns>
    public WsdlSimpleType Build()
    {
        _simpleType.Name = _simpleTypeElement.Attribute("name")?.Value ?? string.Empty;
        _simpleType.Namespace = _schemaNamespace;

        // Parse restriction element
        var restrictionElement = _simpleTypeElement.Descendants().FirstOrDefault(e => e.Name.LocalName == "restriction");
        if (restrictionElement != null)
        {
            QualifiedNameParser.ProcessQualifiedAttribute(
                restrictionElement.Attribute("base"),
                _simpleTypeElement,
                _simpleType,
                (st, name) => st.BaseType = name,
                (st, ns) => st.BaseTypeNamespace = ns,
                _schemaNamespace);

            // Parse enumeration values
            foreach (var enumerationElement in restrictionElement.Elements().Where(e => e.Name.LocalName == "enumeration"))
            {
                var value = enumerationElement.Attribute("value")?.Value;
                if (!string.IsNullOrEmpty(value))
                {
                    _simpleType.EnumerationValues.Add(value);
                }
            }
        }

        return _simpleType;
    }
}
