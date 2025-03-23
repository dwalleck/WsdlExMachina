using System.Xml.Linq;
using WsdlExMachina.Parser.Models;

namespace WsdlExMachina.Parser.Builders;

/// <summary>
/// Builder for creating WsdlDefinition objects from XML.
/// </summary>
public class WsdlDefinitionBuilder
{
    private readonly WsdlDefinition _definition = new();
    private readonly XDocument _document;
    private readonly XElement _definitionsElement;

    /// <summary>
    /// Initializes a new instance of the <see cref="WsdlDefinitionBuilder"/> class.
    /// </summary>
    /// <param name="document">The XML document containing the WSDL.</param>
    /// <exception cref="ArgumentNullException">Thrown when document is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the document is not a valid WSDL document.</exception>
    public WsdlDefinitionBuilder(XDocument document)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));
        _definitionsElement = document.Root ?? throw new InvalidOperationException("The XML document has no root element.");

        if (_definitionsElement.Name.LocalName != "definitions")
        {
            throw new InvalidOperationException("The XML document is not a valid WSDL document. Root element must be 'definitions'.");
        }
    }

    /// <summary>
    /// Builds the WsdlDefinition object.
    /// </summary>
    /// <returns>The built WsdlDefinition.</returns>
    public WsdlDefinition Build()
    {
        return _definition;
    }

    /// <summary>
    /// Sets the basic properties of the WsdlDefinition.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    public WsdlDefinitionBuilder WithBasicProperties()
    {
        _definition.TargetNamespace = _definitionsElement.Attribute("targetNamespace")?.Value ?? string.Empty;
        _definition.Namespaces = GetNamespaces(_definitionsElement);
        return this;
    }

    /// <summary>
    /// Adds types to the WsdlDefinition.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    public WsdlDefinitionBuilder WithTypes()
    {
        var typesElement = _definitionsElement.Elements().FirstOrDefault(e => e.Name.LocalName == "types");
        if (typesElement != null)
        {
            _definition.Types = new TypesBuilder(typesElement).Build();
        }
        else
        {
            _definition.Types = null;
        }
        return this;
    }

    /// <summary>
    /// Adds messages to the WsdlDefinition.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    public WsdlDefinitionBuilder WithMessages()
    {
        foreach (var messageElement in _definitionsElement.Elements().Where(e => e.Name.LocalName == "message"))
        {
            _definition.Messages.Add(new MessageBuilder(messageElement).Build());
        }
        return this;
    }

    /// <summary>
    /// Adds port types to the WsdlDefinition.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    public WsdlDefinitionBuilder WithPortTypes()
    {
        foreach (var portTypeElement in _definitionsElement.Elements().Where(e => e.Name.LocalName == "portType"))
        {
            _definition.PortTypes.Add(new PortTypeBuilder(portTypeElement).Build());
        }
        return this;
    }

    /// <summary>
    /// Adds bindings to the WsdlDefinition.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    public WsdlDefinitionBuilder WithBindings()
    {
        foreach (var bindingElement in _definitionsElement.Elements().Where(e => e.Name.LocalName == "binding"))
        {
            _definition.Bindings.Add(new BindingBuilder(bindingElement).Build());
        }
        return this;
    }

    /// <summary>
    /// Adds services to the WsdlDefinition.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    public WsdlDefinitionBuilder WithServices()
    {
        foreach (var serviceElement in _definitionsElement.Elements().Where(e => e.Name.LocalName == "service"))
        {
            _definition.Services.Add(new ServiceBuilder(serviceElement).Build());
        }
        return this;
    }

    /// <summary>
    /// Gets the namespaces from an XML element.
    /// </summary>
    /// <param name="element">The XML element.</param>
    /// <returns>A dictionary of namespace prefixes and URIs.</returns>
    private Dictionary<string, string> GetNamespaces(XElement element)
    {
        var namespaces = new Dictionary<string, string>();
        foreach (var attribute in element.Attributes().Where(a => a.IsNamespaceDeclaration))
        {
            var prefix = attribute.Name.LocalName == "xmlns" ? string.Empty : attribute.Name.LocalName;
            namespaces[prefix] = attribute.Value;
        }
        return namespaces;
    }
}
