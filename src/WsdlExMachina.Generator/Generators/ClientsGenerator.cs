using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WsdlExMachina.Parser.Models;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace WsdlExMachina.Generator.Generators;

/// <summary>
/// Generates client implementations for the SOAP client.
/// </summary>
public class ClientsGenerator : ICodeGenerator
{
    /// <summary>
    /// Generates client implementations.
    /// </summary>
    /// <param name="wsdlDefinition">The WSDL definition.</param>
    /// <param name="outputNamespace">The namespace to use for the generated code.</param>
    /// <param name="outputDirectory">The directory where the files will be created.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="wsdlDefinition"/>, <paramref name="outputNamespace"/>, or <paramref name="outputDirectory"/> is null.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when the specified <paramref name="outputDirectory"/> does not exist.</exception>
    /// <exception cref="IOException">Thrown when an I/O error occurs while writing the file.</exception>
    public void Generate(WsdlDefinition wsdlDefinition, string outputNamespace, string outputDirectory)
    {
        // Create a dictionary for bindings for quick lookup
        var bindingsDictionary = wsdlDefinition.Bindings.ToDictionary(b => b.Name);

        foreach (var service in wsdlDefinition.Services)
        {
        // Create dictionaries for bindings and port types for faster lookups
        var bindingsDictionary = wsdlDefinition.Bindings.ToDictionary(b => b.Name);
        var portTypesDictionary = wsdlDefinition.PortTypes.ToDictionary(pt => pt.Name);

        foreach (var service in wsdlDefinition.Services)
        {
            // Find the port type for this service
            if (!portsDictionary.TryGetValue(service.Name, out var port))
            {
                continue;
            }

            if (!bindingsDictionary.TryGetValue(port.Binding, out var binding))
            {
                continue;
            }

            if (!portTypesDictionary.TryGetValue(binding.Type, out var portType))
            {
                continue;
            }
                continue;
            }

            // Create the class declaration
            var classDeclaration = ClassDeclaration($"{service.Name}Client")
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .AddBaseListTypes(
                    SimpleBaseType(
                        ParseTypeName("SoapClientBase")
                    ),
                    SimpleBaseType(
                        ParseTypeName($"I{portType.Name}")
                    )
                );

            // Add constructors
            classDeclaration = classDeclaration.AddMembers(
                ConstructorDeclaration(Identifier($"{service.Name}Client"))
                    .AddModifiers(Token(SyntaxKind.PublicKeyword))
                    .AddParameterListParameters(
                        Parameter(Identifier("options"))
                            .WithType(ParseTypeName("SoapClientOptions")),
                        Parameter(Identifier("httpClient"))
                            .WithType(ParseTypeName("HttpClient"))
                    )
                    .WithInitializer(
                        ConstructorInitializer(
                            SyntaxKind.BaseConstructorInitializer,
                            ArgumentList(
                                SeparatedList(
                                    new[] {
                                        Argument(
                                            MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                IdentifierName("options"),
                                                IdentifierName("Endpoint")
                                            )
                                        ),
                                        Argument(IdentifierName("httpClient"))
                                    }
                                )
                            )
            // Add method implementations
            // Get unique operations to avoid duplicates
            var uniqueOperations = portType.Operations
                .GroupBy(o => o.Name)
                .Select(g => g.First())
                .ToList();

            // Create a dictionary for binding operations to find the SOAP action quickly
            var bindingOperationsDictionary = wsdlDefinition.Bindings
                .SelectMany(b => b.Operations)
                .Where(bo => !string.IsNullOrEmpty(bo.SoapAction))
                .ToDictionary(bo => bo.Name, bo => bo.SoapAction);

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

                // Find the SOAP action from the dictionary
                bindingOperationsDictionary.TryGetValue(operation.Name, out string soapAction);

                // Create the method implementation
                var methodDeclaration = MethodDeclaration(
                    ParseTypeName(returnType),
                    Identifier($"{operation.Name}Async")
                )
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .AddParameterListParameters(parameters.ToArray());

                // Create the method body
                StatementSyntax returnStatement;
                if (outputMessage != null && outputMessage.Parts.Count > 0)
                {
                    // Return the result of SendSoapRequestAsync
                    var genericName = GenericName(
                        Identifier("SendSoapRequestAsync"))
                        .WithTypeArgumentList(
                            TypeArgumentList(
                                SeparatedList<TypeSyntax>(
                                    new[] {
                                        ParseTypeName($"{operation.Name}Request"),
                                        ParseTypeName($"{operation.Name}Response")
                                    }
                                )
                            )
                        );

                    returnStatement = ReturnStatement(
                        InvocationExpression(genericName)
                        .WithArgumentList(
                            ArgumentList(
                                SeparatedList(
                                    new[] {
                                        Argument(
                                            LiteralExpression(
                                                SyntaxKind.StringLiteralExpression,
                                                Literal(soapAction)
                                            )
                                        ),
                                        Argument(IdentifierName("request")),
                                        Argument(IdentifierName("cancellationToken"))
                                    }
                                )
                            )
                        )
                    );
                }
                else
                {
                    // Return the result of SendSoapRequestAsync with a void response
                    var genericName = GenericName(
                        Identifier("SendSoapRequestAsync"))
                        .WithTypeArgumentList(
                            TypeArgumentList(
                                SeparatedList<TypeSyntax>(
                                    new[] {
                                        ParseTypeName($"{operation.Name}Request"),
                                        ParseTypeName("object")
                                    }
                                )
                            )
                        );

                    returnStatement = ReturnStatement(
                        InvocationExpression(genericName)
                        .WithArgumentList(
                            ArgumentList(
                                SeparatedList(
                                    new[] {
                                        Argument(
                                            LiteralExpression(
                                                SyntaxKind.StringLiteralExpression,
                                                Literal(soapAction)
                                            )
                                        ),
                                        Argument(IdentifierName("request")),
                                        Argument(IdentifierName("cancellationToken"))
                                    }
                                )
                            )
                        )
                    );
                }

                methodDeclaration = methodDeclaration.WithBody(
                    Block(
                        returnStatement
                    )
                );

                classDeclaration = classDeclaration.AddMembers(methodDeclaration);
            }
                    )
                );

                classDeclaration = classDeclaration.AddMembers(methodDeclaration);
            }

            // Create the compilation unit
            var compilationUnit = CompilationUnit()
                .AddUsings(
                    UsingDirective(ParseName("System")),
                    UsingDirective(ParseName("System.Net.Http")),
                    UsingDirective(ParseName("System.Threading")),
                    UsingDirective(ParseName("System.Threading.Tasks")),
                    UsingDirective(ParseName($"{outputNamespace}.Interfaces")),
                    UsingDirective(ParseName($"{outputNamespace}.Models.Requests")),
                    UsingDirective(ParseName($"{outputNamespace}.Models.Responses"))
                )
                .AddMembers(
                    NamespaceDeclaration(ParseName($"{outputNamespace}.Client"))
                        .AddMembers(classDeclaration)
                );

            // Format the code
            var code = compilationUnit
                .NormalizeWhitespace()
            File.WriteAllText(Path.Combine(outputDirectory, "Client", service.Name + "Client.cs"), code);

            // Ensure the directory exists
            var clientDirectory = Path.Combine(outputDirectory, "Client");
            if (!Directory.Exists(clientDirectory))
            {
                Directory.CreateDirectory(clientDirectory);
            }

            // Write the file
            File.WriteAllText(Path.Combine(clientDirectory, $"{service.Name}Client.cs"), code);
        }
    }
}
