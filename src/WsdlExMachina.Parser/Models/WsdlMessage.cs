namespace WsdlExMachina.Parser.Models;

/// <summary>
/// Represents a message defined in a WSDL document.
/// </summary>
public class WsdlMessage
{
    /// <summary>
    /// Gets or sets the name of the message.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the collection of parts contained in the message.
    /// </summary>
    public List<WsdlMessagePart> Parts { get; set; } = [];
}

/// <summary>
/// Represents a part of a WSDL message.
/// </summary>
public class WsdlMessagePart
{
    /// <summary>
    /// Gets or sets the name of the message part.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the element name if this part references an element.
    /// </summary>
    public string? Element { get; set; }

    /// <summary>
    /// Gets or sets the element namespace if this part references an element.
    /// </summary>
    public string? ElementNamespace { get; set; }

    /// <summary>
    /// Gets or sets the type name if this part references a type.
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the type namespace if this part references a type.
    /// </summary>
    public string? TypeNamespace { get; set; }
}
