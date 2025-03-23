using System;
using System.IO;
using System.Xml;
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
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="filePath"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="filePath"/> is empty or whitespace.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the specified file does not exist.</exception>
    /// <exception cref="WsdlParserException">Thrown when an error occurs during parsing.</exception>
    public WsdlDefinition ParseFile(string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be empty or whitespace.", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"WSDL file not found: {filePath}", filePath);
        }

        try
        {
            var document = XDocument.Load(filePath, LoadOptions.SetLineInfo);
            return Parse(document);
        }
        catch (XmlException ex)
        {
            throw new WsdlParserException($"XML parsing error in file '{filePath}': {ex.Message}", ex);
        }
        catch (Exception ex) when (ex is not WsdlParserException)
        {
            throw new WsdlParserException($"Error parsing WSDL file '{filePath}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Parses a WSDL document from the specified XML string.
    /// </summary>
    /// <param name="wsdlXml">The XML string containing the WSDL document.</param>
    /// <returns>A <see cref="WsdlDefinition"/> object representing the parsed WSDL.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="wsdlXml"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="wsdlXml"/> is empty or whitespace.</exception>
    /// <exception cref="WsdlParserException">Thrown when an error occurs during parsing.</exception>
    public WsdlDefinition ParseXml(string wsdlXml)
    {
        ArgumentNullException.ThrowIfNull(wsdlXml);

        if (string.IsNullOrWhiteSpace(wsdlXml))
        {
            throw new ArgumentException("WSDL XML cannot be empty or whitespace.", nameof(wsdlXml));
        }

        try
        {
            var document = XDocument.Parse(wsdlXml, LoadOptions.SetLineInfo);
            return Parse(document);
        }
        catch (XmlException ex)
        {
            throw new WsdlParserException($"XML parsing error: {ex.Message}", ex);
        }
        catch (Exception ex) when (ex is not WsdlParserException)
        {
            throw new WsdlParserException($"Error parsing WSDL XML: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Parses a WSDL document from the specified XML document.
    /// </summary>
    /// <param name="document">The XML document containing the WSDL.</param>
    /// <returns>A <see cref="WsdlDefinition"/> object representing the parsed WSDL.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="document"/> is null.</exception>
    /// <exception cref="WsdlParserException">Thrown when an error occurs during parsing.</exception>
    public WsdlDefinition Parse(XDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        try
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
        catch (Exception ex) when (ex is not WsdlParserException)
        {
            throw new WsdlParserException($"Error parsing WSDL document: {ex.Message}", ex);
        }
    }
}
