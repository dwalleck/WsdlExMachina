using System.Xml.Linq;
using WsdlExMachina.Parser.Models;
using WsdlExMachina.Parser.Utilities;

namespace WsdlExMachina.Parser.Builders;

/// <summary>
/// Builder for creating WsdlBinding objects from XML.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="BindingBuilder"/> class.
/// </remarks>
/// <param name="bindingElement">The XML element containing the binding.</param>
/// <exception cref="ArgumentNullException">Thrown when bindingElement is null.</exception>
public class BindingBuilder(XElement bindingElement)
{
    private readonly XElement _bindingElement = bindingElement ?? throw new ArgumentNullException(nameof(bindingElement));
    private readonly WsdlBinding _binding = new();
    private const string SoapNamespace = "http://schemas.xmlsoap.org/wsdl/soap/";
    private const string Soap12Namespace = "http://schemas.xmlsoap.org/wsdl/soap12/";

    /// <summary>
    /// Builds the WsdlBinding object.
    /// </summary>
    /// <returns>The built WsdlBinding.</returns>
    public WsdlBinding Build()
    {
        _binding.Name = _bindingElement.Attribute("name")?.Value ?? string.Empty;

        // Process type attribute
        QualifiedNameParser.ProcessQualifiedAttribute(
            _bindingElement.Attribute("type"),
            _bindingElement,
            _binding,
            (b, name) => b.Type = name,
            (b, ns) => b.TypeNamespace = ns);

        // Parse SOAP binding
        var soapBindingElement = _bindingElement.Elements()
            .FirstOrDefault(e => e.Name.LocalName == "binding" &&
                                (e.Name.NamespaceName == SoapNamespace ||
                                 e.Name.NamespaceName == Soap12Namespace));

        if (soapBindingElement != null)
        {
            _binding.SoapVersion = soapBindingElement.Name.NamespaceName == Soap12Namespace ? "1.2" : "1.1";
            _binding.Transport = soapBindingElement.Attribute("transport")?.Value ?? string.Empty;
            _binding.Style = soapBindingElement.Attribute("style")?.Value ?? "document";
        }

        // Parse operations
        foreach (var operationElement in _bindingElement.Elements().Where(e => e.Name.LocalName == "operation"))
        {
            _binding.Operations.Add(new BindingOperationBuilder(operationElement).Build());
        }

        return _binding;
    }
}

/// <summary>
/// Builder for creating WsdlBindingOperation objects from XML.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="BindingOperationBuilder"/> class.
/// </remarks>
/// <param name="operationElement">The XML element containing the operation.</param>
/// <exception cref="ArgumentNullException">Thrown when operationElement is null.</exception>
public class BindingOperationBuilder(XElement operationElement)
{
    private readonly XElement _operationElement = operationElement ?? throw new ArgumentNullException(nameof(operationElement));
    private readonly WsdlBindingOperation _operation = new();
    private const string SoapNamespace = "http://schemas.xmlsoap.org/wsdl/soap/";
    private const string Soap12Namespace = "http://schemas.xmlsoap.org/wsdl/soap12/";

    /// <summary>
    /// Builds the WsdlBindingOperation object.
    /// </summary>
    /// <returns>The built WsdlBindingOperation.</returns>
    public WsdlBindingOperation Build()
    {
        _operation.Name = _operationElement.Attribute("name")?.Value ?? string.Empty;

        // Parse SOAP operation
        var soapOperationElement = _operationElement.Elements()
            .FirstOrDefault(e => e.Name.LocalName == "operation" &&
                                (e.Name.NamespaceName == SoapNamespace ||
                                 e.Name.NamespaceName == Soap12Namespace));

        if (soapOperationElement != null)
        {
            _operation.SoapAction = soapOperationElement.Attribute("soapAction")?.Value ?? string.Empty;
            _operation.Style = soapOperationElement.Attribute("style")?.Value ?? string.Empty;
        }

        // Parse input
        var inputElement = _operationElement.Elements().FirstOrDefault(e => e.Name.LocalName == "input");
        if (inputElement != null)
        {
            _operation.Input = new BindingOperationMessageBuilder(inputElement).Build();
        }

        // Parse output
        var outputElement = _operationElement.Elements().FirstOrDefault(e => e.Name.LocalName == "output");
        if (outputElement != null)
        {
            _operation.Output = new BindingOperationMessageBuilder(outputElement).Build();
        }

        // Parse faults
        foreach (var faultElement in _operationElement.Elements().Where(e => e.Name.LocalName == "fault"))
        {
            _operation.Faults.Add(new BindingOperationMessageBuilder(faultElement).Build());
        }

        return _operation;
    }
}

/// <summary>
/// Builder for creating WsdlBindingOperationMessage objects from XML.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="BindingOperationMessageBuilder"/> class.
/// </remarks>
/// <param name="messageElement">The XML element containing the message.</param>
/// <exception cref="ArgumentNullException">Thrown when messageElement is null.</exception>
public class BindingOperationMessageBuilder(XElement messageElement)
{
    private readonly XElement _messageElement = messageElement ?? throw new ArgumentNullException(nameof(messageElement));
    private readonly WsdlBindingOperationMessage _message = new();
    private const string SoapNamespace = "http://schemas.xmlsoap.org/wsdl/soap/";
    private const string Soap12Namespace = "http://schemas.xmlsoap.org/wsdl/soap12/";

    /// <summary>
    /// Builds the WsdlBindingOperationMessage object.
    /// </summary>
    /// <returns>The built WsdlBindingOperationMessage.</returns>
    public WsdlBindingOperationMessage Build()
    {
        _message.Name = _messageElement.Attribute("name")?.Value ?? string.Empty;

        // Parse SOAP body
        var soapBodyElement = _messageElement.Elements()
            .FirstOrDefault(e => e.Name.LocalName == "body" &&
                                (e.Name.NamespaceName == SoapNamespace ||
                                 e.Name.NamespaceName == Soap12Namespace));

        if (soapBodyElement != null)
        {
            _message.Use = soapBodyElement.Attribute("use")?.Value ?? "literal";
            _message.Namespace = soapBodyElement.Attribute("namespace")?.Value ?? string.Empty;
            _message.EncodingStyle = soapBodyElement.Attribute("encodingStyle")?.Value ?? string.Empty;
        }

        // Parse SOAP headers
        var soapHeaderElements = _messageElement.Elements()
            .Where(e => e.Name.LocalName == "header" &&
                       (e.Name.NamespaceName == SoapNamespace ||
                        e.Name.NamespaceName == Soap12Namespace));

        foreach (var soapHeaderElement in soapHeaderElements)
        {
            var header = new WsdlBindingOperationMessageHeader
            {
                Use = soapHeaderElement.Attribute("use")?.Value ?? "literal",
                Part = soapHeaderElement.Attribute("part")?.Value ?? string.Empty,
                EncodingStyle = soapHeaderElement.Attribute("encodingStyle")?.Value,
                Namespace = soapHeaderElement.Attribute("namespace")?.Value
            };

            QualifiedNameParser.ProcessQualifiedAttribute(
                soapHeaderElement.Attribute("message"),
                _messageElement,
                header,
                (h, name) => h.Message = name,
                (h, ns) => h.MessageNamespace = ns);

            _message.Headers.Add(header);
        }

        return _message;
    }
}
