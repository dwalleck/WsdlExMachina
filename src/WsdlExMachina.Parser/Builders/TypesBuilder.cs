using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using WsdlExMachina.Parser.Models;

namespace WsdlExMachina.Parser.Builders;

/// <summary>
/// Builder for creating WsdlTypes objects from XML.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="TypesBuilder"/> class.
/// </remarks>
/// <param name="typesElement">The XML element containing the types.</param>
/// <exception cref="ArgumentNullException">Thrown when typesElement is null.</exception>
public class TypesBuilder(XElement typesElement)
{
    private readonly XElement _typesElement = typesElement ?? throw new ArgumentNullException(nameof(typesElement));
    private readonly WsdlTypes _types = new();

    /// <summary>
    /// Builds the WsdlTypes object.
    /// </summary>
    /// <returns>The built WsdlTypes.</returns>
    public WsdlTypes Build()
    {
        foreach (var schemaElement in _typesElement.Elements().Where(e => e.Name.LocalName == "schema"))
        {
            BuildSchema(schemaElement);
        }

        return _types;
    }

    /// <summary>
    /// Builds a schema from an XML element.
    /// </summary>
    /// <param name="schemaElement">The XML element containing the schema.</param>
    private void BuildSchema(XElement schemaElement)
    {
        // Create a validation event handler that ignores validation errors
        ValidationEventHandler validationEventHandler = (sender, args) => { };

        var schema = new XmlSchema();
        try
        {
            using (var reader = schemaElement.CreateReader())
            {
                schema = XmlSchema.Read(reader, validationEventHandler);
            }
            if (schema != null)
            {
                _types.Schemas.Add(schema);
            }
        }
        catch (Exception)
        {
            // Ignore schema reading errors and continue with our custom parsing
        }

        var schemaNamespace = schemaElement.Attribute("targetNamespace")?.Value ?? string.Empty;

        // Build complex types
        foreach (var complexTypeElement in schemaElement.Elements().Where(e => e.Name.LocalName == "complexType"))
        {
            _types.ComplexTypes.Add(new ComplexTypeBuilder(complexTypeElement, schemaNamespace).Build());
        }

        // Build simple types
        foreach (var simpleTypeElement in schemaElement.Elements().Where(e => e.Name.LocalName == "simpleType"))
        {
            _types.SimpleTypes.Add(new SimpleTypeBuilder(simpleTypeElement, schemaNamespace).Build());
        }

        // Build elements
        foreach (var elementElement in schemaElement.Elements().Where(e => e.Name.LocalName == "element"))
        {
            _types.Elements.Add(new ElementBuilder(elementElement, schemaNamespace).Build());
        }
    }
}
