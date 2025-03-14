using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using WsdlExMachina.Parser.Models;

namespace WsdlExMachina.Parser;

/// <summary>
/// Provides functionality to parse WSDL documents.
/// </summary>
public class WsdlParser
{
    private const string WsdlNamespace = "http://schemas.xmlsoap.org/wsdl/";
    private const string XsdNamespace = "http://www.w3.org/2001/XMLSchema";
    private const string SoapNamespace = "http://schemas.xmlsoap.org/wsdl/soap/";
    private const string Soap12Namespace = "http://schemas.xmlsoap.org/wsdl/soap12/";

    /// <summary>
    /// Parses a WSDL document from the specified file path.
    /// </summary>
    /// <param name="filePath">The path to the WSDL file.</param>
    /// <returns>A <see cref="WsdlDefinition"/> object representing the parsed WSDL.</returns>
    public WsdlDefinition ParseFile(string filePath)
    {
        var document = XDocument.Load(filePath);
        return Parse(document);
    }

    /// <summary>
    /// Parses a WSDL document from the specified XML string.
    /// </summary>
    /// <param name="wsdlXml">The XML string containing the WSDL document.</param>
    /// <returns>A <see cref="WsdlDefinition"/> object representing the parsed WSDL.</returns>
    public WsdlDefinition ParseXml(string wsdlXml)
    {
        var document = XDocument.Parse(wsdlXml);
        return Parse(document);
    }

    /// <summary>
    /// Parses a WSDL document from the specified XML document.
    /// </summary>
    /// <param name="document">The XML document containing the WSDL.</param>
    /// <returns>A <see cref="WsdlDefinition"/> object representing the parsed WSDL.</returns>
    public WsdlDefinition Parse(XDocument document)
    {
        var definitions = document.Root;
        if (definitions == null || definitions.Name.LocalName != "definitions")
        {
            throw new InvalidOperationException("The XML document is not a valid WSDL document. Root element must be 'definitions'.");
        }

        var wsdl = new WsdlDefinition
        {
            TargetNamespace = definitions.Attribute("targetNamespace")?.Value ?? string.Empty,
            Namespaces = GetNamespaces(definitions)
        };

        // Parse types
        var typesElement = definitions.Elements().FirstOrDefault(e => e.Name.LocalName == "types");
        if (typesElement != null)
        {
            wsdl.Types = ParseTypes(typesElement);
        }

        // Parse messages
        foreach (var messageElement in definitions.Elements().Where(e => e.Name.LocalName == "message"))
        {
            wsdl.Messages.Add(ParseMessage(messageElement));
        }

        // Parse port types
        foreach (var portTypeElement in definitions.Elements().Where(e => e.Name.LocalName == "portType"))
        {
            wsdl.PortTypes.Add(ParsePortType(portTypeElement));
        }

        // Parse bindings
        foreach (var bindingElement in definitions.Elements().Where(e => e.Name.LocalName == "binding"))
        {
            wsdl.Bindings.Add(ParseBinding(bindingElement));
        }

        // Parse services
        foreach (var serviceElement in definitions.Elements().Where(e => e.Name.LocalName == "service"))
        {
            wsdl.Services.Add(ParseService(serviceElement));
        }

        return wsdl;
    }

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

    private WsdlTypes ParseTypes(XElement typesElement)
    {
        var types = new WsdlTypes();

        // Parse XML schemas
        foreach (var schemaElement in typesElement.Elements().Where(e => e.Name.LocalName == "schema"))
        {
            var schema = new XmlSchema();
            using (var reader = schemaElement.CreateReader())
            {
                schema = XmlSchema.Read(reader, null);
            }
            types.Schemas.Add(schema);

            // Parse complex types
            foreach (var complexTypeElement in schemaElement.Elements().Where(e => e.Name.LocalName == "complexType"))
            {
                types.ComplexTypes.Add(ParseComplexType(complexTypeElement, schemaElement.Attribute("targetNamespace")?.Value ?? string.Empty));
            }

            // Parse simple types
            foreach (var simpleTypeElement in schemaElement.Elements().Where(e => e.Name.LocalName == "simpleType"))
            {
                types.SimpleTypes.Add(ParseSimpleType(simpleTypeElement, schemaElement.Attribute("targetNamespace")?.Value ?? string.Empty));
            }

            // Parse elements
            foreach (var elementElement in schemaElement.Elements().Where(e => e.Name.LocalName == "element"))
            {
                types.Elements.Add(ParseElement(elementElement, schemaElement.Attribute("targetNamespace")?.Value ?? string.Empty));
            }
        }

        return types;
    }

    private WsdlComplexType ParseComplexType(XElement complexTypeElement, string schemaNamespace)
    {
        var complexType = new WsdlComplexType
        {
            Name = complexTypeElement.Attribute("name")?.Value ?? string.Empty,
            Namespace = schemaNamespace
        };

        // Parse base type if this is an extension
        var extensionElement = complexTypeElement.Descendants().FirstOrDefault(e => e.Name.LocalName == "extension");
        if (extensionElement != null)
        {
            var baseAttribute = extensionElement.Attribute("base");
            if (baseAttribute != null)
            {
                var baseTypeParts = baseAttribute.Value.Split(':');
                if (baseTypeParts.Length == 2)
                {
                    var prefix = baseTypeParts[0];
                    complexType.BaseType = baseTypeParts[1];
                    complexType.BaseTypeNamespace = GetNamespaceFromPrefix(complexTypeElement, prefix);
                }
                else
                {
                    complexType.BaseType = baseAttribute.Value;
                    complexType.BaseTypeNamespace = schemaNamespace;
                }
            }
        }

        // Parse elements
        var sequenceElement = complexTypeElement.Descendants().FirstOrDefault(e => e.Name.LocalName == "sequence");
        if (sequenceElement != null)
        {
            foreach (var elementElement in sequenceElement.Elements().Where(e => e.Name.LocalName == "element"))
            {
                complexType.Elements.Add(ParseElement(elementElement, schemaNamespace));
            }
        }

        return complexType;
    }

    private WsdlSimpleType ParseSimpleType(XElement simpleTypeElement, string schemaNamespace)
    {
        var simpleType = new WsdlSimpleType
        {
            Name = simpleTypeElement.Attribute("name")?.Value ?? string.Empty,
            Namespace = schemaNamespace
        };

        // Parse restriction
        var restrictionElement = simpleTypeElement.Elements().FirstOrDefault(e => e.Name.LocalName == "restriction");
        if (restrictionElement != null)
        {
            var baseAttribute = restrictionElement.Attribute("base");
            if (baseAttribute != null)
            {
                var baseTypeParts = baseAttribute.Value.Split(':');
                if (baseTypeParts.Length == 2)
                {
                    var prefix = baseTypeParts[0];
                    simpleType.BaseType = baseTypeParts[1];
                    simpleType.BaseTypeNamespace = GetNamespaceFromPrefix(simpleTypeElement, prefix);
                }
                else
                {
                    simpleType.BaseType = baseAttribute.Value;
                    simpleType.BaseTypeNamespace = schemaNamespace;
                }
            }

            // Parse enumeration values
            foreach (var enumerationElement in restrictionElement.Elements().Where(e => e.Name.LocalName == "enumeration"))
            {
                var value = enumerationElement.Attribute("value")?.Value;
                if (value != null)
                {
                    simpleType.EnumerationValues.Add(value);
                }
            }
        }

        return simpleType;
    }

    private WsdlElement ParseElement(XElement elementElement, string schemaNamespace)
    {
        var element = new WsdlElement
        {
            Name = elementElement.Attribute("name")?.Value ?? string.Empty,
            Namespace = schemaNamespace
        };

        // Parse type
        var typeAttribute = elementElement.Attribute("type");
        if (typeAttribute != null)
        {
            var typeParts = typeAttribute.Value.Split(':');
            if (typeParts.Length == 2)
            {
                var prefix = typeParts[0];
                element.Type = typeParts[1];
                element.TypeNamespace = GetNamespaceFromPrefix(elementElement, prefix);
                element.IsComplexType = !element.TypeNamespace.Equals(XsdNamespace);
            }
            else
            {
                element.Type = typeAttribute.Value;
                element.TypeNamespace = schemaNamespace;
                element.IsComplexType = true;
            }
        }
        else
        {
            // Check if the element has a complex type defined inline
            var complexTypeElement = elementElement.Elements().FirstOrDefault(e => e.Name.LocalName == "complexType");
            if (complexTypeElement != null)
            {
                element.IsComplexType = true;
            }
        }

        // Parse min/max occurs
        var minOccursAttribute = elementElement.Attribute("minOccurs");
        if (minOccursAttribute != null && int.TryParse(minOccursAttribute.Value, out var minOccurs))
        {
            element.MinOccurs = minOccurs;
            element.IsOptional = minOccurs == 0;
        }

        var maxOccursAttribute = elementElement.Attribute("maxOccurs");
        if (maxOccursAttribute != null)
        {
            if (maxOccursAttribute.Value == "unbounded")
            {
                element.MaxOccurs = int.MaxValue;
                element.IsArray = true;
            }
            else if (int.TryParse(maxOccursAttribute.Value, out var maxOccurs))
            {
                element.MaxOccurs = maxOccurs;
                element.IsArray = maxOccurs > 1;
            }
        }

        return element;
    }

    private WsdlMessage ParseMessage(XElement messageElement)
    {
        var message = new WsdlMessage
        {
            Name = messageElement.Attribute("name")?.Value ?? string.Empty
        };

        // Parse parts
        foreach (var partElement in messageElement.Elements().Where(e => e.Name.LocalName == "part"))
        {
            var part = new WsdlMessagePart
            {
                Name = partElement.Attribute("name")?.Value ?? string.Empty
            };

            // Parse element reference
            var elementAttribute = partElement.Attribute("element");
            if (elementAttribute != null)
            {
                var elementParts = elementAttribute.Value.Split(':');
                if (elementParts.Length == 2)
                {
                    var prefix = elementParts[0];
                    part.Element = elementParts[1];
                    part.ElementNamespace = GetNamespaceFromPrefix(messageElement, prefix);
                }
                else
                {
                    part.Element = elementAttribute.Value;
                }
            }

            // Parse type reference
            var typeAttribute = partElement.Attribute("type");
            if (typeAttribute != null)
            {
                var typeParts = typeAttribute.Value.Split(':');
                if (typeParts.Length == 2)
                {
                    var prefix = typeParts[0];
                    part.Type = typeParts[1];
                    part.TypeNamespace = GetNamespaceFromPrefix(messageElement, prefix);
                }
                else
                {
                    part.Type = typeAttribute.Value;
                }
            }

            message.Parts.Add(part);
        }

        return message;
    }

    private WsdlPortType ParsePortType(XElement portTypeElement)
    {
        var portType = new WsdlPortType
        {
            Name = portTypeElement.Attribute("name")?.Value ?? string.Empty
        };

        // Parse operations
        foreach (var operationElement in portTypeElement.Elements().Where(e => e.Name.LocalName == "operation"))
        {
            var operation = new WsdlOperation
            {
                Name = operationElement.Attribute("name")?.Value ?? string.Empty
            };

            // Parse documentation
            var documentationElement = operationElement.Elements().FirstOrDefault(e => e.Name.LocalName == "documentation");
            if (documentationElement != null)
            {
                operation.Documentation = documentationElement.Value.Trim();
            }

            // Parse input
            var inputElement = operationElement.Elements().FirstOrDefault(e => e.Name.LocalName == "input");
            if (inputElement != null)
            {
                operation.Input = ParseOperationMessage(inputElement);
            }

            // Parse output
            var outputElement = operationElement.Elements().FirstOrDefault(e => e.Name.LocalName == "output");
            if (outputElement != null)
            {
                operation.Output = ParseOperationMessage(outputElement);
            }

            // Parse faults
            foreach (var faultElement in operationElement.Elements().Where(e => e.Name.LocalName == "fault"))
            {
                operation.Faults.Add(ParseOperationMessage(faultElement));
            }

            portType.Operations.Add(operation);
        }

        return portType;
    }

    private WsdlOperationMessage ParseOperationMessage(XElement messageElement)
    {
        var operationMessage = new WsdlOperationMessage
        {
            Name = messageElement.Attribute("name")?.Value ?? string.Empty
        };

        // Parse message reference
        var messageAttribute = messageElement.Attribute("message");
        if (messageAttribute != null)
        {
            var messageParts = messageAttribute.Value.Split(':');
            if (messageParts.Length == 2)
            {
                var prefix = messageParts[0];
                operationMessage.Message = messageParts[1];
                operationMessage.MessageNamespace = GetNamespaceFromPrefix(messageElement, prefix);
            }
            else
            {
                operationMessage.Message = messageAttribute.Value;
            }
        }

        return operationMessage;
    }

    private WsdlBinding ParseBinding(XElement bindingElement)
    {
        var binding = new WsdlBinding
        {
            Name = bindingElement.Attribute("name")?.Value ?? string.Empty
        };

        // Parse type reference
        var typeAttribute = bindingElement.Attribute("type");
        if (typeAttribute != null)
        {
            var typeParts = typeAttribute.Value.Split(':');
            if (typeParts.Length == 2)
            {
                var prefix = typeParts[0];
                binding.Type = typeParts[1];
                binding.TypeNamespace = GetNamespaceFromPrefix(bindingElement, prefix);
            }
            else
            {
                binding.Type = typeAttribute.Value;
            }
        }

        // Parse SOAP binding
        var soapBindingElement = bindingElement.Elements()
            .FirstOrDefault(e => e.Name.LocalName == "binding" &&
                                (e.Name.NamespaceName == SoapNamespace ||
                                 e.Name.NamespaceName == Soap12Namespace));

        if (soapBindingElement != null)
        {
            binding.SoapVersion = soapBindingElement.Name.NamespaceName == Soap12Namespace ? "1.2" : "1.1";
            binding.Transport = soapBindingElement.Attribute("transport")?.Value ?? string.Empty;
            binding.Style = soapBindingElement.Attribute("style")?.Value ?? "document";
        }

        // Parse operations
        foreach (var operationElement in bindingElement.Elements().Where(e => e.Name.LocalName == "operation"))
        {
            var operation = new WsdlBindingOperation
            {
                Name = operationElement.Attribute("name")?.Value ?? string.Empty
            };

            // Parse SOAP operation
            var soapOperationElement = operationElement.Elements()
                .FirstOrDefault(e => e.Name.LocalName == "operation" &&
                                    (e.Name.NamespaceName == SoapNamespace ||
                                     e.Name.NamespaceName == Soap12Namespace));

            if (soapOperationElement != null)
            {
                operation.SoapAction = soapOperationElement.Attribute("soapAction")?.Value ?? string.Empty;
                operation.Style = soapOperationElement.Attribute("style")?.Value ?? binding.Style;
            }

            // Parse input
            var inputElement = operationElement.Elements().FirstOrDefault(e => e.Name.LocalName == "input");
            if (inputElement != null)
            {
                operation.Input = ParseBindingOperationMessage(inputElement);
            }

            // Parse output
            var outputElement = operationElement.Elements().FirstOrDefault(e => e.Name.LocalName == "output");
            if (outputElement != null)
            {
                operation.Output = ParseBindingOperationMessage(outputElement);
            }

            // Parse faults
            foreach (var faultElement in operationElement.Elements().Where(e => e.Name.LocalName == "fault"))
            {
                operation.Faults.Add(ParseBindingOperationMessage(faultElement));
            }

            binding.Operations.Add(operation);
        }

        return binding;
    }

    private WsdlBindingOperationMessage ParseBindingOperationMessage(XElement messageElement)
    {
        var bindingMessage = new WsdlBindingOperationMessage
        {
            Name = messageElement.Attribute("name")?.Value ?? string.Empty
        };

        // Parse SOAP body
        var soapBodyElement = messageElement.Elements()
            .FirstOrDefault(e => e.Name.LocalName == "body" &&
                                (e.Name.NamespaceName == SoapNamespace ||
                                 e.Name.NamespaceName == Soap12Namespace));

        if (soapBodyElement != null)
        {
            bindingMessage.Use = soapBodyElement.Attribute("use")?.Value ?? "literal";
            bindingMessage.EncodingStyle = soapBodyElement.Attribute("encodingStyle")?.Value;
            bindingMessage.Namespace = soapBodyElement.Attribute("namespace")?.Value;
        }

        // Parse SOAP headers
        foreach (var soapHeaderElement in messageElement.Elements()
            .Where(e => e.Name.LocalName == "header" &&
                        (e.Name.NamespaceName == SoapNamespace ||
                         e.Name.NamespaceName == Soap12Namespace)))
        {
            var header = new WsdlBindingOperationMessageHeader
            {
                Use = soapHeaderElement.Attribute("use")?.Value ?? "literal",
                EncodingStyle = soapHeaderElement.Attribute("encodingStyle")?.Value,
                Namespace = soapHeaderElement.Attribute("namespace")?.Value,
                Part = soapHeaderElement.Attribute("part")?.Value ?? string.Empty
            };

            // Parse message reference
            var messageAttribute = soapHeaderElement.Attribute("message");
            if (messageAttribute != null)
            {
                var messageParts = messageAttribute.Value.Split(':');
                if (messageParts.Length == 2)
                {
                    var prefix = messageParts[0];
                    header.Message = messageParts[1];
                    header.MessageNamespace = GetNamespaceFromPrefix(messageElement, prefix);
                }
                else
                {
                    header.Message = messageAttribute.Value;
                }
            }

            bindingMessage.Headers.Add(header);
        }

        return bindingMessage;
    }

    private WsdlService ParseService(XElement serviceElement)
    {
        var service = new WsdlService
        {
            Name = serviceElement.Attribute("name")?.Value ?? string.Empty
        };

        // Parse documentation
        var documentationElement = serviceElement.Elements().FirstOrDefault(e => e.Name.LocalName == "documentation");
        if (documentationElement != null)
        {
            service.Documentation = documentationElement.Value.Trim();
        }

        // Parse ports
        foreach (var portElement in serviceElement.Elements().Where(e => e.Name.LocalName == "port"))
        {
            var port = new WsdlPort
            {
                Name = portElement.Attribute("name")?.Value ?? string.Empty
            };

            // Parse binding reference
            var bindingAttribute = portElement.Attribute("binding");
            if (bindingAttribute != null)
            {
                var bindingParts = bindingAttribute.Value.Split(':');
                if (bindingParts.Length == 2)
                {
                    var prefix = bindingParts[0];
                    port.Binding = bindingParts[1];
                    port.BindingNamespace = GetNamespaceFromPrefix(serviceElement, prefix);
                }
                else
                {
                    port.Binding = bindingAttribute.Value;
                }
            }

            // Parse SOAP address
            var soapAddressElement = portElement.Elements()
                .FirstOrDefault(e => e.Name.LocalName == "address" &&
                                    (e.Name.NamespaceName == SoapNamespace ||
                                     e.Name.NamespaceName == Soap12Namespace));

            if (soapAddressElement != null)
            {
                port.Location = soapAddressElement.Attribute("location")?.Value ?? string.Empty;
            }

            service.Ports.Add(port);
        }

        return service;
    }

    private string GetNamespaceFromPrefix(XElement element, string prefix)
    {
        var ns = element.GetNamespaceOfPrefix(prefix);
        return ns?.NamespaceName ?? string.Empty;
    }
}
