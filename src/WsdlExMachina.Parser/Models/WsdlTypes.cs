using System.Xml.Schema;

namespace WsdlExMachina.Parser.Models;

/// <summary>
/// Represents the types section of a WSDL document.
/// </summary>
public class WsdlTypes
{
    /// <summary>
    /// Gets or sets the collection of XML schemas defined in the types section.
    /// </summary>
    public List<XmlSchema> Schemas { get; set; } = [];

    /// <summary>
    /// Gets or sets the collection of complex types defined in the schemas.
    /// </summary>
    public List<WsdlComplexType> ComplexTypes { get; set; } = [];

    /// <summary>
    /// Gets or sets the collection of simple types defined in the schemas.
    /// </summary>
    public List<WsdlSimpleType> SimpleTypes { get; set; } = [];

    /// <summary>
    /// Gets or sets the collection of elements defined in the schemas.
    /// </summary>
    public List<WsdlElement> Elements { get; set; } = [];
}

/// <summary>
/// Represents a complex type defined in a WSDL schema.
/// </summary>
public class WsdlComplexType
{
    /// <summary>
    /// Gets or sets the name of the complex type.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the namespace of the complex type.
    /// </summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the collection of elements contained in the complex type.
    /// </summary>
    public List<WsdlElement> Elements { get; set; } = [];

    /// <summary>
    /// Gets or sets the base type if this complex type extends another type.
    /// </summary>
    public string? BaseType { get; set; }

    /// <summary>
    /// Gets or sets the namespace of the base type.
    /// </summary>
    public string? BaseTypeNamespace { get; set; }
}

/// <summary>
/// Represents a simple type defined in a WSDL schema.
/// </summary>
public class WsdlSimpleType
{
    /// <summary>
    /// Gets or sets the name of the simple type.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the namespace of the simple type.
    /// </summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the base type of the simple type.
    /// </summary>
    public string BaseType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the namespace of the base type.
    /// </summary>
    public string BaseTypeNamespace { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the collection of enumeration values if this is an enumeration type.
    /// </summary>
    public List<string> EnumerationValues { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether this simple type is an enumeration.
    /// </summary>
    public bool IsEnum => EnumerationValues.Count > 0;
}

/// <summary>
/// Represents an element defined in a WSDL schema.
/// </summary>
public class WsdlElement
{
    /// <summary>
    /// Gets or sets the name of the element.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the namespace of the element.
    /// </summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of the element.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the namespace of the type.
    /// </summary>
    public string TypeNamespace { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the element is a complex type.
    /// </summary>
    public bool IsComplexType { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the element is optional (minOccurs=0).
    /// </summary>
    public bool IsOptional { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the element can occur multiple times (maxOccurs>1).
    /// </summary>
    public bool IsArray { get; set; }

    /// <summary>
    /// Gets or sets the minimum number of occurrences.
    /// </summary>
    public int MinOccurs { get; set; } = 1;

    /// <summary>
    /// Gets or sets the maximum number of occurrences.
    /// </summary>
    public int MaxOccurs { get; set; } = 1;
}
