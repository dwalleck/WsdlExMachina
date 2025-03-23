using System.Xml.Linq;

namespace WsdlExMachina.Parser.Utilities;

/// <summary>
/// Provides utility methods for parsing qualified names in XML.
/// </summary>
public static class QualifiedNameParser
{
    /// <summary>
    /// Parses a qualified name into its local name and namespace parts.
    /// </summary>
    /// <param name="qualifiedName">The qualified name to parse (e.g. "xs:string")</param>
    /// <param name="contextElement">The XML element providing namespace context</param>
    /// <param name="defaultNamespace">The default namespace to use if no prefix is present</param>
    /// <returns>A tuple containing the local name and namespace URI</returns>
    public static (string localName, string namespaceUri) Parse(string qualifiedName, XElement contextElement, string defaultNamespace = "")
    {
        if (string.IsNullOrEmpty(qualifiedName))
        {
            return (string.Empty, defaultNamespace);
        }

        var parts = qualifiedName.Split(':', 2); // Split on first colon only
        if (parts.Length == 2)
        {
            var prefix = parts[0];
            var localName = parts[1];
            var namespaceUri = GetNamespaceFromPrefix(contextElement, prefix);
            return (localName, namespaceUri);
        }
        else
        {
            return (qualifiedName, defaultNamespace);
        }
    }

    /// <summary>
    /// Gets the namespace URI for a given prefix in the context of an XML element.
    /// </summary>
    /// <param name="element">The XML element providing namespace context</param>
    /// <param name="prefix">The namespace prefix</param>
    /// <returns>The namespace URI, or an empty string if the prefix is not found</returns>
    public static string GetNamespaceFromPrefix(XElement element, string prefix)
    {
        var ns = element.GetNamespaceOfPrefix(prefix);
        return ns?.NamespaceName ?? string.Empty;
    }

    /// <summary>
    /// Processes a qualified attribute value and sets the corresponding properties on the target object.
    /// </summary>
    /// <typeparam name="T">The type of object to set properties on</typeparam>
    /// <param name="attribute">The XML attribute containing the qualified name</param>
    /// <param name="contextElement">The XML element providing namespace context</param>
    /// <param name="target">The target object to set properties on</param>
    /// <param name="nameProperty">Action to set the name property</param>
    /// <param name="namespaceProperty">Action to set the namespace property</param>
    /// <param name="defaultNamespace">Optional default namespace to use if no prefix is present</param>
    public static void ProcessQualifiedAttribute<T>(
        XAttribute? attribute,
        XElement contextElement,
        T target,
        Action<T, string> nameProperty,
        Action<T, string> namespaceProperty,
        string defaultNamespace = "")
    {
        if (attribute != null)
        {
            var (localName, namespaceUri) = Parse(attribute.Value, contextElement, defaultNamespace);
            nameProperty(target, localName);
            namespaceProperty(target, namespaceUri);
        }
    }
}
