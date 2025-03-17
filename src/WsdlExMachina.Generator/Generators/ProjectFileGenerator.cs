using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WsdlExMachina.Parser.Models;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace WsdlExMachina.Generator.Generators;

/// <summary>
/// Generates the project file for the SOAP client.
/// </summary>
public class ProjectFileGenerator : ICodeGenerator
{
    /// <summary>
    /// Generates the project file for the SOAP client.
    /// </summary>
    /// <param name="wsdlDefinition">The WSDL definition.</param>
    /// <param name="outputNamespace">The namespace to use for the generated code.</param>
    /// <param name="outputDirectory">The directory where the files will be created.</param>
    public void Generate(WsdlDefinition wsdlDefinition, string outputNamespace, string outputDirectory)
    {
        // Get the project name from the output directory
        var projectName = Path.GetFileName(outputDirectory);
        if (string.IsNullOrEmpty(projectName))
        {
            projectName = outputNamespace;
        }

        // Create the project file content
        var projectFileContent = $@"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>{outputNamespace}</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Microsoft.Extensions.Http"" Version=""9.0.3"" />
    <PackageReference Include=""Microsoft.Extensions.Http.Polly"" Version=""9.0.3"" />
    <PackageReference Include=""Polly"" Version=""8.5.2"" />
    <PackageReference Include=""Polly.Extensions.Http"" Version=""3.0.0"" />
  </ItemGroup>

</Project>";

        // Write the project file
        File.WriteAllText(Path.Combine(outputDirectory, $"{projectName}.csproj"), projectFileContent);
    }
}
