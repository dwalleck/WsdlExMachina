using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WsdlExMachina.Parser.Models;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace WsdlExMachina.Generator.Generators;

/// <summary>
/// Generates interface definitions for the SOAP client.
/// </summary>
public class InterfacesGenerator : ICodeGenerator
{
    /// <summary>
    /// Generates interface definitions.
    /// </summary>
    /// <param name="wsdlDefinition">The WSDL definition.</param>
    /// <param name="outputNamespace">The namespace to use for the generated code.</param>
    /// <param name="outputDirectory">The directory where the files will be created.</param>
    public void Generate(WsdlDefinition wsdlDefinition, string outputNamespace, string outputDirectory)
    {
        foreach (var portType in wsdlDefinition.PortTypes)
        {
            // Create the interface
            var interfaceDeclaration = InterfaceDeclaration($"I{portType.Name}")
                .AddModifiers(Token(SyntaxKind.PublicKeyword));

            // Add methods for each operation
            // Group operations by name to avoid duplicates
            var uniqueOperations = portType.Operations
                .GroupBy(o => o.Name)
                .Select(g => g.First())
                .ToList();

            foreach (var operation in uniqueOperations)
            {
                // Find the input and output messages
                var inputMessage = operation.Input != null
                    ? wsdlDefinition.Messages.FirstOrDefault(m => m.Name == operation.Input.Message)
                    : null;

                var outputMessage = operation.Output != null
                    ? wsdlDefinition.Messages.FirstOrDefault(m => m.Name == operation.Output.Message)
                    : null;

                // Determine return type and parameters
                string returnType = "Task";
                var parameters = new List<ParameterSyntax>();

                if (inputMessage != null && inputMessage.Parts.Count > 0)
                {
                    // Use the request class
                    parameters.Add(
                        Parameter(
                            Identifier("request")
                        )
                        .WithType(
                            ParseTypeName($"{operation.Name}Request")
                        )
                    );
                }

                // Add cancellation token parameter
                parameters.Add(
                    Parameter(
                        Identifier("cancellationToken")
                    )
                    .WithType(
                        ParseTypeName("CancellationToken")
                    )
                    .WithDefault(
                        EqualsValueClause(
                            LiteralExpression(SyntaxKind.DefaultLiteralExpression)
                        )
                    )
                );

                if (outputMessage != null && outputMessage.Parts.Count > 0)
                {
                    // Use the response class
                    returnType = $"Task<{operation.Name}Response>";
                }

                // Create the method declaration
                var methodDeclaration = MethodDeclaration(
                    ParseTypeName(returnType),
                    Identifier($"{operation.Name}Async")
                )
                .AddParameterListParameters(parameters.ToArray())
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));

                interfaceDeclaration = interfaceDeclaration.AddMembers(methodDeclaration);
            }

            // Create the compilation unit
            var compilationUnit = CompilationUnit()
                .AddUsings(
                    UsingDirective(ParseName("System")),
                    UsingDirective(ParseName("System.Threading")),
                    UsingDirective(ParseName("System.Threading.Tasks")),
                    UsingDirective(ParseName($"{outputNamespace}.Models.Requests")),
                    UsingDirective(ParseName($"{outputNamespace}.Models.Responses"))
                )
                .AddMembers(
                    NamespaceDeclaration(ParseName($"{outputNamespace}.Interfaces"))
                        .AddMembers(interfaceDeclaration)
                );

            // Format the code
            var code = compilationUnit
                .NormalizeWhitespace()
                .ToFullString();

            // Write the file
            File.WriteAllText(Path.Combine(outputDirectory, "Interfaces", $"I{portType.Name}.cs"), code);
        }
    }
}
