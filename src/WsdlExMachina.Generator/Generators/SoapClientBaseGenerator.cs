using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WsdlExMachina.Generator.Generators.SoapClient;
using WsdlExMachina.Parser.Models;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using CommunityToolkit.Diagnostics;

namespace WsdlExMachina.Generator.Generators;

/// <summary>
/// Generates the base class for SOAP clients.
/// </summary>
public class SoapClientBaseGenerator : ICodeGenerator
{
    private readonly ISoapClientComponentGenerator[] _componentGenerators;

    /// <summary>
    /// Initializes a new instance of the <see cref="SoapClientBaseGenerator"/> class.
    /// </summary>
    public SoapClientBaseGenerator()
    {
        _componentGenerators = new ISoapClientComponentGenerator[]
        {
            new SoapClientFieldsGenerator(),
            new SoapClientRequestGenerator(),
            new SoapClientEnvelopeGenerator()
        };
    }

    /// <summary>
    /// Generates the base class for SOAP clients.
    /// </summary>
    /// <param name="wsdlDefinition">The WSDL definition.</param>
    /// <param name="outputNamespace">The namespace to use for the generated code.</param>
    /// <param name="outputDirectory">The directory where the files will be created.</param>
    public void Generate(WsdlDefinition wsdlDefinition, string outputNamespace, string outputDirectory)
    {
         // Validate parameters
        Guard.IsNotNull(wsdlDefinition, nameof(wsdlDefinition));
        Guard.IsNotNullOrWhiteSpace(outputNamespace, nameof(outputNamespace));
        Guard.IsNotNullOrWhiteSpace(outputDirectory, nameof(outputDirectory));

        // Create the SoapClientBase class
        var classDeclaration = ClassDeclaration("SoapClientBase")
            .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.AbstractKeyword));

        // Generate members from component generators
        foreach (var generator in _componentGenerators)
        {
            classDeclaration = classDeclaration.AddMembers(generator.Generate(wsdlDefinition));
        }

        // Create the compilation unit
        var compilationUnit = CompilationUnit()
            .AddUsings(
                UsingDirective(ParseName("System")),
                UsingDirective(ParseName("System.IO")),
                UsingDirective(ParseName("System.Net.Http")),
                UsingDirective(ParseName("System.Text")),
                UsingDirective(ParseName("System.Threading")),
                UsingDirective(ParseName("System.Threading.Tasks")),
                UsingDirective(ParseName("System.Xml")),
                UsingDirective(ParseName("System.Xml.Serialization"))
            )
            .AddMembers(
                NamespaceDeclaration(ParseName($"{outputNamespace}.Client"))
                    .AddMembers(classDeclaration)
            );

        // Format the code
        var code = compilationUnit
            .NormalizeWhitespace()
            .ToFullString();

        // Write the file
        File.WriteAllText(Path.Combine(outputDirectory, "Client", "SoapClientBase.cs"), code);
    }
}
