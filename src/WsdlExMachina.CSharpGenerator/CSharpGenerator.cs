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
    /// Generates C# code from a WSDL definition with a specified namespace.
    /// </summary>
    /// <param name="wsdl">The WSDL definition.</param>
    /// <param name="namespaceName">The namespace to use for the generated code.</param>
    /// <returns>A <see cref="GeneratedCodeResult"/> containing the generated code.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="wsdl"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="namespaceName"/> is null or empty.</exception>
    /// <exception cref="CodeGenerationException">Thrown when an error occurs during code generation.</exception>
    public GeneratedCodeResult Generate(WsdlDefinition wsdl, string namespaceName)
    {
        ArgumentNullException.ThrowIfNull(wsdl);

        if (string.IsNullOrEmpty(namespaceName))
        {
            throw new ArgumentException("Namespace name cannot be null or empty.", nameof(namespaceName));
        }

        try
        {
            var result = new GeneratedCodeResult();

            // Generate all types at once
            var generatedCode = _generator.GenerateAll(wsdl, namespaceName);
            foreach (var entry in generatedCode)
            {
                result.Files.Add(entry.Key, entry.Value);
            }

            return result;
        }
        catch (CodeGenerationException)
        {
            // Re-throw CodeGenerationException as is
            throw;
        }
        catch (Exception ex)
        {
            throw new CodeGenerationException("Failed to generate code.", ex);
        }
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

        // Create a .csproj file in the output directory
        CreateProjectFile(directory);

        // Copy SoapClientBase.cs to the output directory
        CopySoapClientBase(directory);
    }

    /// <summary>
    /// Creates a .csproj file in the specified directory.
    /// </summary>
    /// <param name="directory">The directory to create the .csproj file in.</param>
    private void CreateProjectFile(string directory)
    {
        // Extract namespace from the first file (assuming all files use the same namespace)
        var namespaceName = "Generated";
        foreach (var file in Files.Values)
        {
            var namespaceMatch = System.Text.RegularExpressions.Regex.Match(file, @"namespace\s+([^\s{;]+)");
            if (namespaceMatch.Success)
            {
                namespaceName = namespaceMatch.Groups[1].Value;
                break;
            }
        }

        // Create the .csproj file content
        var projectContent = $@"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>{namespaceName}</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Polly"" Version=""8.5.2"" />
    <PackageReference Include=""Polly.Extensions.Http"" Version=""3.0.0"" />
  </ItemGroup>

</Project>
";

        // Write the .csproj file
        var projectFileName = $"{Path.GetFileName(directory)}.csproj";
        if (namespaceName.Contains('.'))
        {
            projectFileName = namespaceName.Replace(";", "") + ".csproj";
        }
        var projectFilePath = Path.Combine(directory, projectFileName);
        File.WriteAllText(projectFilePath, projectContent);
    }

    /// <summary>
    /// Copies the SoapClientBase.cs file to the specified directory.
    /// </summary>
    /// <param name="directory">The directory to copy SoapClientBase.cs to.</param>
    private void CopySoapClientBase(string directory)
    {
        // Get the path to the SoapClientBase.cs file
        var assemblyLocation = typeof(CSharpGenerator).Assembly.Location;
        var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
        var soapClientBasePath = Path.Combine(assemblyDirectory!, "SoapClientBase.cs");

        // If the file doesn't exist in the assembly directory, try to find it in the source code
        if (!File.Exists(soapClientBasePath))
        {
            // Try to find it in the current directory or parent directories
            var currentDir = Directory.GetCurrentDirectory();
            var srcDir = FindDirectory(currentDir, "src");
            if (srcDir != null)
            {
                var csharpGeneratorDir = Path.Combine(srcDir, "WsdlExMachina.CSharpGenerator");
                if (Directory.Exists(csharpGeneratorDir))
                {
                    soapClientBasePath = Path.Combine(csharpGeneratorDir, "SoapClientBase.cs");
                }
            }
        }

        // If we found the file, copy it to the output directory
        if (File.Exists(soapClientBasePath))
        {
            // Read the content of SoapClientBase.cs
            var soapClientBaseContent = File.ReadAllText(soapClientBasePath);

            // Extract namespace from the first file (assuming all files use the same namespace)
            var namespaceName = "Generated";
            foreach (var file in Files.Values)
            {
                var namespaceMatch = System.Text.RegularExpressions.Regex.Match(file, @"namespace\s+([^\s{;]+)");
                if (namespaceMatch.Success)
                {
                    namespaceName = namespaceMatch.Groups[1].Value;
                    break;
                }
            }

            // Replace the namespace in SoapClientBase.cs
            // Handle both file-scoped namespaces (with semicolons) and block-scoped namespaces (with braces)
            if (soapClientBaseContent.Contains("namespace"))
            {
                // First, determine if it's a file-scoped namespace (with semicolon) or block-scoped namespace (with brace)
                bool isFileScoped = System.Text.RegularExpressions.Regex.IsMatch(soapClientBaseContent, @"namespace\s+[^\s{;]+\s*;");

                if (isFileScoped)
                {
                    // Replace file-scoped namespace with block-scoped namespace
                    soapClientBaseContent = System.Text.RegularExpressions.Regex.Replace(
                        soapClientBaseContent,
                        @"namespace\s+[^\s{;]+\s*;",
                        $"namespace {namespaceName}\r\n{{");

                    // Add closing brace at the end of the file
                    soapClientBaseContent = soapClientBaseContent + "\r\n}";
                }
                else
                {
                    // Replace block-scoped namespace
                    soapClientBaseContent = System.Text.RegularExpressions.Regex.Replace(
                        soapClientBaseContent,
                        @"namespace\s+[^\s{;]+\s*{",
                        $"namespace {namespaceName} {{");
                }
            }

            // Write the modified SoapClientBase.cs to the output directory
            var outputPath = Path.Combine(directory, "SoapClientBase.cs");
            File.WriteAllText(outputPath, soapClientBaseContent);
        }
        else
        {
            Console.Error.WriteLine("Warning: Could not find SoapClientBase.cs to copy to the output directory.");
        }
    }

    /// <summary>
    /// Recursively searches for a directory with the specified name.
    /// </summary>
    /// <param name="startDir">The directory to start searching from.</param>
    /// <param name="dirName">The name of the directory to find.</param>
    /// <returns>The full path to the directory if found, otherwise null.</returns>
    private static string? FindDirectory(string startDir, string dirName)
    {
        // Check if the current directory is the one we're looking for
        if (Path.GetFileName(startDir) == dirName)
        {
            return startDir;
        }

        // Check if the directory we're looking for is a direct child of the current directory
        var dirPath = Path.Combine(startDir, dirName);
        if (Directory.Exists(dirPath))
        {
            return dirPath;
        }

        // Check parent directory (up to the root)
        var parentDir = Directory.GetParent(startDir);
        if (parentDir != null)
        {
            return FindDirectory(parentDir.FullName, dirName);
        }

        return null;
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
