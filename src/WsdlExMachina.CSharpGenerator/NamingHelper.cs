using System.Text;
using System.Text.RegularExpressions;

namespace WsdlExMachina.CSharpGenerator;

/// <summary>
/// Provides helper methods for generating valid C# names from XML names.
/// </summary>
public class NamingHelper
{
    private static readonly HashSet<string> _csharpKeywords = new()
    {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
        "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
        "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
        "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
        "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
        "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed",
        "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw",
        "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using",
        "virtual", "void", "volatile", "while"
    };

    /// <summary>
    /// Gets a valid C# class name from an XML name.
    /// </summary>
    /// <param name="xmlName">The XML name.</param>
    /// <returns>A valid C# class name.</returns>
    public string GetSafeClassName(string xmlName)
    {
        if (string.IsNullOrEmpty(xmlName))
        {
            return "UnnamedClass";
        }

        // Remove any invalid characters
        var name = Regex.Replace(xmlName, @"[^\p{L}\p{N}_]", "");

        // Ensure the name starts with a letter
        if (!char.IsLetter(name[0]))
        {
            name = "Class" + name;
        }

        // Convert to PascalCase
        name = ToPascalCase(name);

        // Check if the name is a C# keyword
        if (_csharpKeywords.Contains(name.ToLowerInvariant()))
        {
            name = "@" + name;
        }

        return name;
    }

    /// <summary>
    /// Gets a valid C# property name from an XML name.
    /// </summary>
    /// <param name="xmlName">The XML name.</param>
    /// <returns>A valid C# property name.</returns>
    public string GetSafePropertyName(string xmlName)
    {
        if (string.IsNullOrEmpty(xmlName))
        {
            return "UnnamedProperty";
        }

        // Remove any invalid characters
        var name = Regex.Replace(xmlName, @"[^\p{L}\p{N}_]", "");

        // Ensure the name starts with a letter
        if (!char.IsLetter(name[0]))
        {
            name = "Property" + name;
        }

        // Convert to PascalCase
        name = ToPascalCase(name);

        // Check if the name is a C# keyword
        if (_csharpKeywords.Contains(name.ToLowerInvariant()))
        {
            name = "@" + name;
        }

        return name;
    }

    /// <summary>
    /// Converts a string to PascalCase.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <returns>The PascalCase string.</returns>
    private static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        // Handle camelCase or snake_case
        var words = Regex.Split(input, @"(?<=[a-z])(?=[A-Z])|[_\-\.]");
        var result = new StringBuilder();

        foreach (var word in words)
        {
            if (string.IsNullOrEmpty(word))
            {
                continue;
            }

            result.Append(char.ToUpperInvariant(word[0]));
            if (word.Length > 1)
            {
                result.Append(word[1..]);
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// Gets a valid C# namespace from a string.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <returns>A valid C# namespace.</returns>
    public string GetSafeNamespace(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return "DefaultNamespace";
        }

        // Remove any invalid characters
        var name = Regex.Replace(input, @"[^\p{L}\p{N}_\.]", "");

        // Split by dots and process each segment
        var segments = name.Split('.');
        for (int i = 0; i < segments.Length; i++)
        {
            if (string.IsNullOrEmpty(segments[i]))
            {
                segments[i] = "Segment" + i;
                continue;
            }

            // Ensure segment starts with a letter
            if (!char.IsLetter(segments[i][0]))
            {
                segments[i] = "N" + segments[i];
            }

            // Convert to PascalCase
            segments[i] = ToPascalCase(segments[i]);

            // Check if the segment is a C# keyword
            if (_csharpKeywords.Contains(segments[i].ToLowerInvariant()))
            {
                segments[i] = "N" + segments[i];
            }
        }

        return string.Join(".", segments);
    }
}
