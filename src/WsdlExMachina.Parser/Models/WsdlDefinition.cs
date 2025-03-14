namespace WsdlExMachina.Parser.Models;

/// <summary>
/// Represents a WSDL definition document.
/// </summary>
public class WsdlDefinition
{
    /// <summary>
    /// Gets or sets the target namespace of the WSDL.
    /// </summary>
    public string TargetNamespace { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the types section of the WSDL.
    /// </summary>
    public WsdlTypes Types { get; set; } = new();

    /// <summary>
    /// Gets or sets the collection of messages defined in the WSDL.
    /// </summary>
    public List<WsdlMessage> Messages { get; set; } = new();

    /// <summary>
    /// Gets or sets the collection of port types defined in the WSDL.
    /// </summary>
    public List<WsdlPortType> PortTypes { get; set; } = new();

    /// <summary>
    /// Gets or sets the collection of bindings defined in the WSDL.
    /// </summary>
    public List<WsdlBinding> Bindings { get; set; } = new();

    /// <summary>
    /// Gets or sets the collection of services defined in the WSDL.
    /// </summary>
    public List<WsdlService> Services { get; set; } = new();

    /// <summary>
    /// Gets or sets the collection of XML namespace declarations.
    /// </summary>
    public Dictionary<string, string> Namespaces { get; set; } = new();
}
