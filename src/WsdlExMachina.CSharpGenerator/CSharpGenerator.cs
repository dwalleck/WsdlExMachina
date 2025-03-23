using System.Text;
using WsdlExMachina.Parser.Models;

namespace WsdlExMachina.CSharpGenerator;

/// <summary>
/// Generates C# code from WSDL definitions.
/// </summary>
public class CSharpGenerator
{
    private readonly RoslynGeneratorFacade _generator;
    private readonly TypeMapper _typeMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="CSharpGenerator"/> class.
    /// </summary>
    public CSharpGenerator()
    {
        _generator = new RoslynGeneratorFacade();
        _typeMapper = new TypeMapper();
    }

    /// <summary>
    /// Generates C# code from a WSDL definition.
    /// </summary>
    /// <param name="wsdl">The WSDL definition.</param>
    /// <returns>A <see cref="GeneratedCodeResult"/> containing the generated code.</returns>
    public GeneratedCodeResult Generate(WsdlDefinition wsdl)
    {
        var result = new GeneratedCodeResult();

        // Extract namespace from WSDL target namespace
        string namespaceName = _typeMapper.GetCSharpNamespace(wsdl.TargetNamespace);

        // Generate all types at once
        var generatedCode = _generator.GenerateAll(wsdl, namespaceName);
        foreach (KeyValuePair<string, string> entry in generatedCode)
        {
            result.Files.Add(entry.Key, entry.Value);
        }

        return result;
    }

    /// <summary>
    /// Generates a summary of the generated code.
    /// </summary>
    /// <param name="result">The generated code result.</param>
    /// <returns>A string containing a summary of the generated code.</returns>
    public string GenerateSummary(GeneratedCodeResult result)
    {
        var sb = new StringBuilder();

        sb.AppendLine("# Generated C# Code Summary");
        sb.AppendLine();
        sb.AppendLine($"Total files generated: {result.Files.Count}");
        sb.AppendLine();
        sb.AppendLine("## Files");
        sb.AppendLine();

        foreach (var fileName in result.Files.Keys.OrderBy(f => f))
        {
            sb.AppendLine($"- {fileName}");
        }

        return sb.ToString();
    }
}

/// <summary>
/// Represents the result of generating C# code from a WSDL definition.
/// </summary>
public class GeneratedCodeResult
{
    /// <summary>
    /// Gets the dictionary of generated files.
    /// </summary>
    public Dictionary<string, string> Files { get; } = new();

    /// <summary>
    /// Gets the content of a file.
    /// </summary>
    /// <param name="fileName">The name of the file.</param>
    /// <returns>The content of the file, or null if the file does not exist.</returns>
    public string? GetFileContent(string fileName)
    {
        return Files.TryGetValue(fileName, out var content) ? content : null;
    }

    /// <summary>
    /// Saves the generated files to a directory.
    /// </summary>
    /// <param name="directory">The directory to save the files to.</param>
    public void SaveToDirectory(string directory)
    {
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        foreach (var entry in Files)
        {
            var filePath = Path.Combine(directory, entry.Key);
            File.WriteAllText(filePath, entry.Value);
        }
    }

    /// <summary>
    /// Gets a concatenated string of all generated file contents.
    /// </summary>
    /// <returns>A string containing all generated file contents.</returns>
    public string GetAllContent()
    {
        var sb = new StringBuilder();

        foreach (var fileName in Files.Keys.OrderBy(f => f))
        {
            sb.AppendLine($"// File: {fileName}");
            sb.AppendLine(Files[fileName]);
            sb.AppendLine();
            sb.AppendLine("// -----------------------------------------------");
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
