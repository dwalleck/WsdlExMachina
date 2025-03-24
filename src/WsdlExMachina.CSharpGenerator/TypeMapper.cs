using System;
using System.Collections.Generic;

namespace WsdlExMachina.CSharpGenerator;

/// <summary>
/// Provides functionality to map XML schema types to C# types.
/// </summary>
public class TypeMapper
{
    private readonly Dictionary<string, string> _xmlToCSharpTypeMap;
    private readonly Dictionary<string, string> _xmlNamespaceAliases;

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeMapper"/> class.
    /// </summary>
    public TypeMapper()
    {
        _xmlToCSharpTypeMap = new Dictionary<string, string>
        {
            // XML Schema primitive types to C# types
            { "string", "string" },
            { "boolean", "bool" },
            { "decimal", "decimal" },
            { "float", "float" },
            { "double", "double" },
            { "duration", "TimeSpan" },
            { "dateTime", "DateTime" },
            { "time", "TimeSpan" },
            { "date", "DateTime" },
            { "gYearMonth", "string" },
            { "gYear", "string" },
            { "gMonthDay", "string" },
            { "gDay", "string" },
            { "gMonth", "string" },
            { "hexBinary", "byte[]" },
            { "base64Binary", "byte[]" },
            { "anyURI", "string" },
            { "QName", "string" },
            { "NOTATION", "string" },
            { "normalizedString", "string" },
            { "token", "string" },
            { "language", "string" },
            { "NMTOKEN", "string" },
            { "NMTOKENS", "string[]" },
            { "Name", "string" },
            { "NCName", "string" },
            { "ID", "string" },
            { "IDREF", "string" },
            { "IDREFS", "string[]" },
            { "ENTITY", "string" },
            { "ENTITIES", "string[]" },
            { "integer", "long" },
            { "nonPositiveInteger", "long" },
            { "negativeInteger", "long" },
            { "long", "long" },
            { "int", "int" },
            { "short", "short" },
            { "byte", "sbyte" },
            { "nonNegativeInteger", "ulong" },
            { "unsignedLong", "ulong" },
            { "unsignedInt", "uint" },
            { "unsignedShort", "ushort" },
            { "unsignedByte", "byte" },
            { "positiveInteger", "ulong" },
            { "anyType", "object" },
            { "anySimpleType", "object" },
            { "char", "char" }
        };

        _xmlNamespaceAliases = new Dictionary<string, string>
        {
            { "http://www.w3.org/2001/XMLSchema", "xs" },
            { "http://www.w3.org/2001/XMLSchema-instance", "xsi" },
            { "http://schemas.xmlsoap.org/soap/encoding/", "soapenc" },
            { "http://schemas.xmlsoap.org/wsdl/", "wsdl" },
            { "http://schemas.xmlsoap.org/wsdl/soap/", "soap" },
            { "http://schemas.xmlsoap.org/wsdl/soap12/", "soap12" },
            { "http://schemas.xmlsoap.org/wsdl/http/", "http" },
            { "http://schemas.xmlsoap.org/wsdl/mime/", "mime" },
            { "http://microsoft.com/wsdl/mime/textMatching/", "tm" }
        };
    }

    /// <summary>
    /// Maps an XML schema type to a C# type.
    /// </summary>
    /// <param name="xmlType">The XML schema type.</param>
    /// <param name="xmlNamespace">The XML namespace of the type.</param>
    /// <returns>The corresponding C# type.</returns>
    public string MapToCSharpType(string xmlType, string? xmlNamespace = null)
    {
        if (string.IsNullOrEmpty(xmlType))
        {
            return "object";
        }

        // Handle XML Schema types
        if (xmlNamespace == "http://www.w3.org/2001/XMLSchema" || string.IsNullOrEmpty(xmlNamespace))
        {
            if (_xmlToCSharpTypeMap.TryGetValue(xmlType, out var csharpType))
            {
                return csharpType;
            }
        }

        // For custom types, use the type name directly
        // We'll handle namespaces and complex types elsewhere
        return xmlType;
    }

    /// <summary>
    /// Gets a C# namespace from an XML namespace.
    /// </summary>
    /// <param name="xmlNamespace">The XML namespace.</param>
    /// <returns>The corresponding C# namespace.</returns>
    public string GetCSharpNamespace(string xmlNamespace)
    {
        if (string.IsNullOrEmpty(xmlNamespace))
        {
            return "DefaultNamespace";
        }

        // Convert URI to a valid C# namespace
        var ns = xmlNamespace
            .Replace("http://", "")
            .Replace("https://", "")
            .Replace("urn:", "")
            .Replace(":", ".")
            .Replace("/", ".")
            .Replace("-", "_")
            .Replace(" ", "_");

        // Remove trailing dots
        ns = ns.TrimEnd('.');

        // Split by dots and capitalize each segment
        var segments = ns.Split('.');
        for (var i = 0; i < segments.Length; i++)
        {
            if (string.IsNullOrEmpty(segments[i]))
            {
                continue;
            }

            segments[i] = char.ToUpperInvariant(segments[i][0]) + segments[i][1..];
        }

        return string.Join(".", segments);
    }

    /// <summary>
    /// Gets an XML namespace alias.
    /// </summary>
    /// <param name="xmlNamespace">The XML namespace.</param>
    /// <returns>The alias for the namespace, or a generated alias if not found.</returns>
    public string GetNamespaceAlias(string xmlNamespace)
    {
        if (string.IsNullOrEmpty(xmlNamespace))
        {
            return "ns";
        }

        if (_xmlNamespaceAliases.TryGetValue(xmlNamespace, out var alias))
        {
            return alias;
        }

        // Generate a simple alias from the namespace
        var uri = new Uri(xmlNamespace, UriKind.RelativeOrAbsolute);
        var host = uri.IsAbsoluteUri ? uri.Host : xmlNamespace;

        if (string.IsNullOrEmpty(host))
        {
            host = xmlNamespace;
        }

        var parts = host.Split('.');
        if (parts.Length > 1)
        {
            return parts[0].ToLowerInvariant();
        }

        return "ns" + Math.Abs(xmlNamespace.GetHashCode() % 100);
    }
}
