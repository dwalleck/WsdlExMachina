using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WsdlExMachina.Parser.Models;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace WsdlExMachina.Generator.Generators;

/// <summary>
/// Generates the options class for SOAP clients.
/// </summary>
public class ClientOptionsGenerator : ICodeGenerator
{
    /// <summary>
    /// Generates the options class for SOAP clients.
    /// </summary>
    /// <param name="wsdlDefinition">The WSDL definition.</param>
    /// <param name="outputNamespace">The namespace to use for the generated code.</param>
    /// <param name="outputDirectory">The directory where the files will be created.</param>
    public void Generate(WsdlDefinition wsdlDefinition, string outputNamespace, string outputDirectory)
    {
        // Create the SoapClientOptions class
        var optionsClassDeclaration = ClassDeclaration("SoapClientOptions")
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .AddMembers(
                PropertyDeclaration(
                    PredefinedType(Token(SyntaxKind.StringKeyword)),
                    Identifier("Endpoint")
                )
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .AddAccessorListAccessors(
                    AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                        .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                    AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                        .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                )
            );

        // Create the compilation unit
        var compilationUnit = CompilationUnit()
            .AddUsings(
                UsingDirective(ParseName("System"))
            )
            .AddMembers(
                NamespaceDeclaration(ParseName($"{outputNamespace}.Client"))
                    .AddMembers(optionsClassDeclaration)
            );

        // Format the code
        var code = compilationUnit
            .NormalizeWhitespace()
            .ToFullString();

        // Write the file
        File.WriteAllText(Path.Combine(outputDirectory, "Client", "SoapClientOptions.cs"), code);
    }
}
