namespace WsdlExMachina.Parser.Models;

/// <summary>
/// Represents a binding defined in a WSDL document.
/// </summary>
public class WsdlBinding
{
    /// <summary>
    /// Gets or sets the name of the binding.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of port this binding is for.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the namespace of the port type.
    /// </summary>
    public string TypeNamespace { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transport protocol used by this binding.
    /// </summary>
    public string Transport { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the SOAP version used by this binding (1.1 or 1.2).
    /// </summary>
    public string SoapVersion { get; set; } = "1.1";

    /// <summary>
    /// Gets or sets the style of the binding (document or rpc).
    /// </summary>
    public string Style { get; set; } = "document";

    /// <summary>
    /// Gets or sets the collection of operations contained in the binding.
    /// </summary>
    public List<WsdlBindingOperation> Operations { get; set; } = [];
}

/// <summary>
/// Represents an operation defined in a WSDL binding.
/// </summary>
public class WsdlBindingOperation
{
    /// <summary>
    /// Gets or sets the name of the operation.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the SOAP action for the operation.
    /// </summary>
    public string SoapAction { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the style of the operation (document or rpc).
    /// </summary>
    public string Style { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the input binding for the operation.
    /// </summary>
    public WsdlBindingOperationMessage? Input { get; set; }

    /// <summary>
    /// Gets or sets the output binding for the operation.
    /// </summary>
    public WsdlBindingOperationMessage? Output { get; set; }

    /// <summary>
    /// Gets or sets the collection of fault bindings for the operation.
    /// </summary>
    public List<WsdlBindingOperationMessage> Faults { get; set; } = [];
}

/// <summary>
/// Represents a message binding in a WSDL binding operation.
/// </summary>
public class WsdlBindingOperationMessage
{
    /// <summary>
    /// Gets or sets the name of the message binding.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the use of the message binding (literal or encoded).
    /// </summary>
    public string Use { get; set; } = "literal";

    /// <summary>
    /// Gets or sets the encoding style if use is "encoded".
    /// </summary>
    public string? EncodingStyle { get; set; }

    /// <summary>
    /// Gets or sets the namespace if style is "rpc".
    /// </summary>
    public string? Namespace { get; set; }

    /// <summary>
    /// Gets or sets the collection of header bindings for the message.
    /// </summary>
    public List<WsdlBindingOperationMessageHeader> Headers { get; set; } = [];
}

/// <summary>
/// Represents a header binding in a WSDL binding operation message.
/// </summary>
public class WsdlBindingOperationMessageHeader
{
    /// <summary>
    /// Gets or sets the message referenced by the header.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the namespace of the message.
    /// </summary>
    public string MessageNamespace { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the part of the message referenced by the header.
    /// </summary>
    public string Part { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the use of the header (literal or encoded).
    /// </summary>
    public string Use { get; set; } = "literal";

    /// <summary>
    /// Gets or sets the encoding style if use is "encoded".
    /// </summary>
    public string? EncodingStyle { get; set; }

    /// <summary>
    /// Gets or sets the namespace if style is "rpc".
    /// </summary>
    public string? Namespace { get; set; }
}
