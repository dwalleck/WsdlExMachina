namespace WsdlExMachina.Parser.Models;

/// <summary>
/// Represents a service defined in a WSDL document.
/// </summary>
public class WsdlService
{
    /// <summary>
    /// Gets or sets the name of the service.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the documentation for the service.
    /// </summary>
    public string Documentation { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the collection of ports contained in the service.
    /// </summary>
    public List<WsdlPort> Ports { get; set; } = [];
}

/// <summary>
/// Represents a port defined in a WSDL service.
/// </summary>
public class WsdlPort
{
    /// <summary>
    /// Gets or sets the name of the port.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the binding used by the port.
    /// </summary>
    public string Binding { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the namespace of the binding.
    /// </summary>
    public string BindingNamespace { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the address location for the port.
    /// </summary>
    public string Location { get; set; } = string.Empty;
}
