using System.Xml.Linq;
using WsdlExMachina.Parser.Models;
using WsdlExMachina.Parser.Utilities;

namespace WsdlExMachina.Parser.Builders;

/// <summary>
/// Builder for creating WsdlSimpleType objects from XML.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SimpleTypeBuilder"/> class.
/// </remarks>
/// <param name="simpleTypeElement">The XML element containing the simple type.</param>
/// <param name="schemaNamespace">The namespace of the schema.</param>
/// <exception cref="ArgumentNullException">Thrown when simpleTypeElement is null.</exception>
public class SimpleTypeBuilder(XElement simpleTypeElement, string schemaNamespace)
{
    private readonly XElement _simpleTypeElement = simpleTypeElement ?? throw new ArgumentNullException(nameof(simpleTypeElement));
    private readonly string _schemaNamespace = schemaNamespace;
    private readonly WsdlSimpleType _simpleType = new();

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
