using System.Xml.Linq;
using WsdlExMachina.Parser.Builders;
using WsdlExMachina.Parser.Models;

namespace WsdlExMachina.Parser;

/// <summary>
/// Provides functionality to parse WSDL documents.
/// </summary>
public class WsdlParser
{
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
        return new WsdlDefinitionBuilder(document)
            .WithBasicProperties()
            .WithTypes()
            .WithMessages()
            .WithPortTypes()
            .WithBindings()
            .WithServices()
            .Build();
    }
}
