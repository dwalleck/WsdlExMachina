namespace WsdlExMachina.Parser.Models;

/// <summary>
/// Represents a port type defined in a WSDL document.
/// </summary>
public class WsdlPortType
{
    /// <summary>
    /// Gets or sets the name of the port type.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the collection of operations contained in the port type.
    /// </summary>
    public List<WsdlOperation> Operations { get; set; } = [];
}

/// <summary>
/// Represents an operation defined in a WSDL port type.
/// </summary>
public class WsdlOperation
{
    /// <summary>
    /// Gets or sets the name of the operation.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the input message for the operation.
    /// </summary>
    public WsdlOperationMessage? Input { get; set; }

    /// <summary>
    /// Gets or sets the output message for the operation.
    /// </summary>
    public WsdlOperationMessage? Output { get; set; }

    /// <summary>
    /// Gets or sets the collection of fault messages for the operation.
    /// </summary>
    public List<WsdlOperationMessage> Faults { get; set; } = [];

    /// <summary>
    /// Gets or sets the documentation for the operation.
    /// </summary>
    public string Documentation { get; set; } = string.Empty;
}

/// <summary>
/// Represents a message reference in a WSDL operation.
/// </summary>
public class WsdlOperationMessage
{
    /// <summary>
    /// Gets or sets the name of the message reference.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message being referenced.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the namespace of the message being referenced.
    /// </summary>
    public string MessageNamespace { get; set; } = string.Empty;
}
