using System;
using System.Collections.Generic;
using System.Linq;
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
    /// <exception cref="WsdlParserException">Thrown when the document is not a valid WSDL document.</exception>
    public WsdlDefinitionBuilder(XDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        _document = document;

        try
        {
            _definitionsElement = document.Root ?? throw new WsdlParserException("The XML document has no root element.");

            if (_definitionsElement.Name.LocalName != "definitions")
            {
                throw new WsdlParserException($"The XML document is not a valid WSDL document. Root element must be 'definitions', but found '{_definitionsElement.Name.LocalName}'.");
            }
        }
        catch (Exception ex) when (ex is not WsdlParserException && ex is not ArgumentNullException)
        {
            throw new WsdlParserException("Error initializing WSDL definition builder.", ex);
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
    /// <exception cref="WsdlParserException">Thrown when an error occurs during parsing.</exception>
    public WsdlDefinitionBuilder WithBasicProperties()
    {
        try
        {
            _definition.TargetNamespace = _definitionsElement.Attribute("targetNamespace")?.Value ?? string.Empty;
            _definition.Namespaces = GetNamespaces(_definitionsElement);
            return this;
        }
        catch (Exception ex) when (ex is not WsdlParserException)
        {
            throw new WsdlParserException("Error parsing basic WSDL properties.", ex);
        }
    }

    /// <summary>
    /// Adds types to the WsdlDefinition.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="WsdlParserException">Thrown when an error occurs during parsing.</exception>
    public WsdlDefinitionBuilder WithTypes()
    {
        try
        {
            var typesElement = _definitionsElement.Elements().FirstOrDefault(e => e.Name.LocalName == "types");
            if (typesElement != null)
            {
                try
                {
                    _definition.Types = new TypesBuilder(typesElement).Build();
                }
                catch (Exception ex)
                {
                    throw new WsdlParserException($"Error parsing types element: {ex.Message}", ex);
                }
            }
            else
            {
                _definition.Types = new WsdlTypes(); // Initialize with empty types instead of null
            }
            return this;
        }
        catch (WsdlParserException)
        {
            throw; // Re-throw WsdlParserException as is
        }
        catch (Exception ex)
        {
            throw new WsdlParserException("Error parsing WSDL types.", ex);
        }
    }

    /// <summary>
    /// Adds messages to the WsdlDefinition.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="WsdlParserException">Thrown when an error occurs during parsing.</exception>
    public WsdlDefinitionBuilder WithMessages()
    {
        try
        {
            foreach (var messageElement in _definitionsElement.Elements().Where(e => e.Name.LocalName == "message"))
            {
                try
                {
                    _definition.Messages.Add(new MessageBuilder(messageElement).Build());
                }
                catch (Exception ex)
                {
                    // Log the error but continue processing other messages
                    Console.Error.WriteLine($"Error parsing message element: {ex.Message}");
                }
            }
            return this;
        }
        catch (Exception ex) when (ex is not WsdlParserException)
        {
            throw new WsdlParserException("Error parsing WSDL messages.", ex);
        }
    }

    /// <summary>
    /// Adds port types to the WsdlDefinition.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="WsdlParserException">Thrown when an error occurs during parsing.</exception>
    public WsdlDefinitionBuilder WithPortTypes()
    {
        try
        {
            foreach (var portTypeElement in _definitionsElement.Elements().Where(e => e.Name.LocalName == "portType"))
            {
                try
                {
                    _definition.PortTypes.Add(new PortTypeBuilder(portTypeElement).Build());
                }
                catch (Exception ex)
                {
                    // Log the error but continue processing other port types
                    Console.Error.WriteLine($"Error parsing portType element: {ex.Message}");
                }
            }
            return this;
        }
        catch (Exception ex) when (ex is not WsdlParserException)
        {
            throw new WsdlParserException("Error parsing WSDL port types.", ex);
        }
    }

    /// <summary>
    /// Adds bindings to the WsdlDefinition.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="WsdlParserException">Thrown when an error occurs during parsing.</exception>
    public WsdlDefinitionBuilder WithBindings()
    {
        try
        {
            foreach (var bindingElement in _definitionsElement.Elements().Where(e => e.Name.LocalName == "binding"))
            {
                try
                {
                    _definition.Bindings.Add(new BindingBuilder(bindingElement).Build());
                }
                catch (Exception ex)
                {
                    // Log the error but continue processing other bindings
                    Console.Error.WriteLine($"Error parsing binding element: {ex.Message}");
                }
            }
            return this;
        }
        catch (Exception ex) when (ex is not WsdlParserException)
        {
            throw new WsdlParserException("Error parsing WSDL bindings.", ex);
        }
    }

    /// <summary>
    /// Adds services to the WsdlDefinition.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="WsdlParserException">Thrown when an error occurs during parsing.</exception>
    public WsdlDefinitionBuilder WithServices()
    {
        try
        {
            foreach (var serviceElement in _definitionsElement.Elements().Where(e => e.Name.LocalName == "service"))
            {
                try
                {
                    _definition.Services.Add(new ServiceBuilder(serviceElement).Build());
                }
                catch (Exception ex)
                {
                    // Log the error but continue processing other services
                    Console.Error.WriteLine($"Error parsing service element: {ex.Message}");
                }
            }
            return this;
        }
        catch (Exception ex) when (ex is not WsdlParserException)
        {
            throw new WsdlParserException("Error parsing WSDL services.", ex);
        }
    }

    /// <summary>
    /// Gets the namespaces from an XML element.
    /// </summary>
    /// <param name="element">The XML element.</param>
    /// <returns>A dictionary of namespace prefixes and URIs.</returns>
    private static Dictionary<string, string> GetNamespaces(XElement element)
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
