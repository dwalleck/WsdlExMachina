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

                // Find the binding operation to check for headers
                var bindingOperation = wsdlDefinition.Bindings
                    .SelectMany(b => b.Operations)
                    .FirstOrDefault(bo => bo.Name == operation.Name);

                // Add header parameters if needed
                if (bindingOperation?.Input?.Headers.Count > 0)
                {
                    bool hasSWBCAuthHeader = false;

                    // Check if any of the headers is a SWBCAuthHeader
                    foreach (var header in bindingOperation.Input.Headers)
                    {
                        var headerMessage = wsdlDefinition.Messages.FirstOrDefault(m => m.Name == header.Message);
                        if (headerMessage != null && headerMessage.Parts.Count > 0)
                        {
                            // Check if this is a SWBCAuthHeader
                            foreach (var part in headerMessage.Parts)
                            {
                                if (!string.IsNullOrEmpty(part.Element))
                                {
                                    var element = wsdlDefinition.Types.Elements.FirstOrDefault(e => e.Name == part.Element);
                                    if (element != null && element.Name.Contains("SWBCAuthHeader"))
                                    {
                                        hasSWBCAuthHeader = true;
                                        break;
                                    }
                                }
                            }

                            if (hasSWBCAuthHeader)
                                break;
                        }
                    }

                    // If we have a SWBCAuthHeader, use the common SoapAuthHeader class
                    if (hasSWBCAuthHeader)
                    {
                        var headerParam = Parameter(
                            Identifier("authHeader")
                        )
                        .WithType(
                            ParseTypeName("SoapAuthHeader")
                        );

                        parameters.Add(headerParam);
                    }
                    else
                    {
                        // For other header types, use the specific header classes
                        foreach (var header in bindingOperation.Input.Headers)
                        {
                            var headerMessage = wsdlDefinition.Messages.FirstOrDefault(m => m.Name == header.Message);
                            if (headerMessage != null)
                            {
                                var headerParam = Parameter(
                                    Identifier(_toCamelCase(headerMessage.Name))
                                )
                                .WithType(
                                    ParseTypeName(headerMessage.Name)
                                );

                                parameters.Add(headerParam);
                            }
                        }
                    }
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
                    UsingDirective(ParseName($"{outputNamespace}.Models.Responses")),
                    UsingDirective(ParseName($"{outputNamespace}.Models.Headers"))
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

    // Helper method to convert a string to camelCase
    private string _toCamelCase(string input)
    {
        if (string.IsNullOrEmpty(input) || char.IsLower(input[0]))
        {
            return input;
        }

        return char.ToLowerInvariant(input[0]) + input.Substring(1);
    }
}
