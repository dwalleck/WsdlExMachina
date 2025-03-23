using System.Xml.Linq;
using WsdlExMachina.Parser.Models;
using WsdlExMachina.Parser.Utilities;

namespace WsdlExMachina.Parser.Builders;

/// <summary>
/// Builder for creating WsdlService objects from XML.
/// </summary>
public class ServiceBuilder
{
    private readonly XElement _serviceElement;
    private readonly WsdlService _service = new();
    private const string SoapNamespace = "http://schemas.xmlsoap.org/wsdl/soap/";
    private const string Soap12Namespace = "http://schemas.xmlsoap.org/wsdl/soap12/";

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceBuilder"/> class.
    /// </summary>
    /// <param name="serviceElement">The XML element containing the service.</param>
    /// <exception cref="ArgumentNullException">Thrown when serviceElement is null.</exception>
    public ServiceBuilder(XElement serviceElement)
    {
        _serviceElement = serviceElement ?? throw new ArgumentNullException(nameof(serviceElement));
    }

    /// <summary>
    /// Builds the WsdlService object.
    /// </summary>
    /// <returns>The built WsdlService.</returns>
    public WsdlService Build()
    {
        _service.Name = _serviceElement.Attribute("name")?.Value ?? string.Empty;

        // Parse documentation
        var documentationElement = _serviceElement.Elements().FirstOrDefault(e => e.Name.LocalName == "documentation");
        if (documentationElement != null)
        {
            _service.Documentation = documentationElement.Value.Trim();
        }

        // Parse ports
        foreach (var portElement in _serviceElement.Elements().Where(e => e.Name.LocalName == "port"))
        {
            _service.Ports.Add(new ServicePortBuilder(portElement).Build());
        }

        return _service;
    }
}

/// <summary>
/// Builder for creating WsdlPort objects from XML.
/// </summary>
public class ServicePortBuilder
{
    private readonly XElement _portElement;
    private readonly WsdlPort _port = new();
    private const string SoapNamespace = "http://schemas.xmlsoap.org/wsdl/soap/";
    private const string Soap12Namespace = "http://schemas.xmlsoap.org/wsdl/soap12/";

    /// <summary>
    /// Initializes a new instance of the <see cref="ServicePortBuilder"/> class.
    /// </summary>
    /// <param name="portElement">The XML element containing the port.</param>
    /// <exception cref="ArgumentNullException">Thrown when portElement is null.</exception>
    public ServicePortBuilder(XElement portElement)
    {
        _portElement = portElement ?? throw new ArgumentNullException(nameof(portElement));
    }

    /// <summary>
    /// Builds the WsdlPort object.
    /// </summary>
    /// <returns>The built WsdlPort.</returns>
    public WsdlPort Build()
    {
        _port.Name = _portElement.Attribute("name")?.Value ?? string.Empty;

        // Process binding attribute
        QualifiedNameParser.ProcessQualifiedAttribute(
            _portElement.Attribute("binding"),
            _portElement,
            _port,
            (p, name) => p.Binding = name,
            (p, ns) => p.BindingNamespace = ns);

        // Parse SOAP address
        var soapAddressElement = _portElement.Elements()
            .FirstOrDefault(e => e.Name.LocalName == "address" &&
                                (e.Name.NamespaceName == SoapNamespace ||
                                 e.Name.NamespaceName == Soap12Namespace));

        if (soapAddressElement != null)
        {
            _port.Location = soapAddressElement.Attribute("location")?.Value ?? string.Empty;
        }

        return _port;
    }
}
