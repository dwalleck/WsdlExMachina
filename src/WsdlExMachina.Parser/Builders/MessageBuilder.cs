using System.Xml.Linq;
using WsdlExMachina.Parser.Models;
using WsdlExMachina.Parser.Utilities;

namespace WsdlExMachina.Parser.Builders;

/// <summary>
/// Builder for creating WsdlMessage objects from XML.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MessageBuilder"/> class.
/// </remarks>
/// <param name="messageElement">The XML element containing the message.</param>
/// <exception cref="ArgumentNullException">Thrown when messageElement is null.</exception>
public class MessageBuilder(XElement messageElement)
{
    private readonly XElement _messageElement = messageElement ?? throw new ArgumentNullException(nameof(messageElement));
    private readonly WsdlMessage _message = new();

    /// <summary>
    /// Builds the WsdlMessage object.
    /// </summary>
    /// <returns>The built WsdlMessage.</returns>
    public WsdlMessage Build()
    {
        _message.Name = _messageElement.Attribute("name")?.Value ?? string.Empty;

        // Parse parts
        foreach (var partElement in _messageElement.Elements().Where(e => e.Name.LocalName == "part"))
        {
            var part = new WsdlMessagePart
            {
                Name = partElement.Attribute("name")?.Value ?? string.Empty
            };

            // Process element attribute
            QualifiedNameParser.ProcessQualifiedAttribute(
                partElement.Attribute("element"),
                _messageElement,
                part,
                (p, name) => p.Element = name,
                (p, ns) => p.ElementNamespace = ns);

            // Process type attribute
            QualifiedNameParser.ProcessQualifiedAttribute(
                partElement.Attribute("type"),
                _messageElement,
                part,
                (p, name) => p.Type = name,
                (p, ns) => p.TypeNamespace = ns);

            _message.Parts.Add(part);
        }

        return _message;
    }
}
