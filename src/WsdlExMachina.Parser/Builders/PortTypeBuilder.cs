using System.Xml.Linq;
using WsdlExMachina.Parser.Models;
using WsdlExMachina.Parser.Utilities;

namespace WsdlExMachina.Parser.Builders;

/// <summary>
/// Builder for creating WsdlPortType objects from XML.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PortTypeBuilder"/> class.
/// </remarks>
/// <param name="portTypeElement">The XML element containing the port type.</param>
/// <exception cref="ArgumentNullException">Thrown when portTypeElement is null.</exception>
public class PortTypeBuilder(XElement portTypeElement)
{
    private readonly XElement _portTypeElement = portTypeElement ?? throw new ArgumentNullException(nameof(portTypeElement));
    private readonly WsdlPortType _portType = new();

    /// <summary>
    /// Builds the WsdlPortType object.
    /// </summary>
    /// <returns>The built WsdlPortType.</returns>
    public WsdlPortType Build()
    {
        _portType.Name = _portTypeElement.Attribute("name")?.Value ?? string.Empty;

        // Parse operations
        foreach (var operationElement in _portTypeElement.Elements().Where(e => e.Name.LocalName == "operation"))
        {
            _portType.Operations.Add(new OperationBuilder(operationElement).Build());
        }

        return _portType;
    }
}

/// <summary>
/// Builder for creating WsdlOperation objects from XML.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="OperationBuilder"/> class.
/// </remarks>
/// <param name="operationElement">The XML element containing the operation.</param>
/// <exception cref="ArgumentNullException">Thrown when operationElement is null.</exception>
public class OperationBuilder(XElement operationElement)
{
    private readonly XElement _operationElement = operationElement ?? throw new ArgumentNullException(nameof(operationElement));
    private readonly WsdlOperation _operation = new();

    /// <summary>
    /// Builds the WsdlOperation object.
    /// </summary>
    /// <returns>The built WsdlOperation.</returns>
    public WsdlOperation Build()
    {
        _operation.Name = _operationElement.Attribute("name")?.Value ?? string.Empty;

        // Parse documentation
        var documentationElement = _operationElement.Elements().FirstOrDefault(e => e.Name.LocalName == "documentation");
        if (documentationElement != null)
        {
            _operation.Documentation = documentationElement.Value.Trim();
        }

        // Parse input
        var inputElement = _operationElement.Elements().FirstOrDefault(e => e.Name.LocalName == "input");
        if (inputElement != null)
        {
            _operation.Input = new OperationMessageBuilder(inputElement).Build();
        }

        // Parse output
        var outputElement = _operationElement.Elements().FirstOrDefault(e => e.Name.LocalName == "output");
        if (outputElement != null)
        {
            _operation.Output = new OperationMessageBuilder(outputElement).Build();
        }

        // Parse faults
        foreach (var faultElement in _operationElement.Elements().Where(e => e.Name.LocalName == "fault"))
        {
            _operation.Faults.Add(new OperationMessageBuilder(faultElement).Build());
        }

        return _operation;
    }
}

/// <summary>
/// Builder for creating WsdlOperationMessage objects from XML.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="OperationMessageBuilder"/> class.
/// </remarks>
/// <param name="messageElement">The XML element containing the message.</param>
/// <exception cref="ArgumentNullException">Thrown when messageElement is null.</exception>
public class OperationMessageBuilder(XElement messageElement)
{
    private readonly XElement _messageElement = messageElement ?? throw new ArgumentNullException(nameof(messageElement));
    private readonly WsdlOperationMessage _message = new();

    /// <summary>
    /// Builds the WsdlOperationMessage object.
    /// </summary>
    /// <returns>The built WsdlOperationMessage.</returns>
    public WsdlOperationMessage Build()
    {
        _message.Name = _messageElement.Attribute("name")?.Value ?? string.Empty;

        // Process message attribute
        QualifiedNameParser.ProcessQualifiedAttribute(
            _messageElement.Attribute("message"),
            _messageElement,
            _message,
            (m, name) => m.Message = name,
            (m, ns) => m.MessageNamespace = ns);

        return _message;
    }
}
