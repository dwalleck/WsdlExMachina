using System.IO;
using WsdlExMachina.Parser.Models;

namespace WsdlExMachina.Generator.Generators;

/// <summary>
/// Creates the directory structure for the generated code.
/// </summary>
public class DirectoryStructureGenerator : ICodeGenerator
{
    /// <summary>
    /// Creates the directory structure for the generated code.
    /// </summary>
    /// <param name="wsdlDefinition">The WSDL definition.</param>
    /// <param name="outputNamespace">The namespace to use for the generated code.</param>
    /// <param name="outputDirectory">The directory where the files will be created.</param>
    public void Generate(WsdlDefinition wsdlDefinition, string outputNamespace, string outputDirectory)
    {
        // Create the main directory
        Directory.CreateDirectory(outputDirectory);

        // Create subdirectories
        Directory.CreateDirectory(Path.Combine(outputDirectory, "Models"));
        Directory.CreateDirectory(Path.Combine(outputDirectory, "Models", "Common"));
        Directory.CreateDirectory(Path.Combine(outputDirectory, "Models", "Requests"));
        Directory.CreateDirectory(Path.Combine(outputDirectory, "Models", "Responses"));
        Directory.CreateDirectory(Path.Combine(outputDirectory, "Interfaces"));
        Directory.CreateDirectory(Path.Combine(outputDirectory, "Client"));
        Directory.CreateDirectory(Path.Combine(outputDirectory, "Extensions"));
    }
}
