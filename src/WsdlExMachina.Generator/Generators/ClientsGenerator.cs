using System.Collections.Generic;
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
        // Create dictionaries for bindings and port types for faster lookups
        var bindingsDictionary = wsdlDefinition.Bindings.ToDictionary(b => b.Name);
        var portTypesDictionary = wsdlDefinition.PortTypes.ToDictionary(pt => pt.Name);

        foreach (var service in wsdlDefinition.Services)
        {
            // Find the port for this service
            var port = service.Ports.FirstOrDefault();
            if (port == null)
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
                        )
                    )
                    .WithBody(Block())
            );

            // Add method implementations
            // We need to handle operations with the same name but different input/output messages
            // Create a dictionary to track method names to avoid duplicates
            var methodNames = new Dictionary<string, int>();

            // Create a lookup for binding operations to find the SOAP action quickly
            // Use a composite key of operation name and input name to handle overloaded operations
            var bindingOperationsLookup = wsdlDefinition.Bindings
                .SelectMany(b => b.Operations)
                .Where(bo => !string.IsNullOrEmpty(bo.SoapAction))
                .ToLookup(
                    bo => new { OperationName = bo.Name, InputName = bo.Input?.Name ?? string.Empty },
                    bo => new { SoapAction = bo.SoapAction, InputName = bo.Input?.Name ?? string.Empty }
                );

            foreach (var operation in portType.Operations)
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
                var headerParameters = new List<ParameterSyntax>();
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

                        headerParameters.Add(headerParam);
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

                                headerParameters.Add(headerParam);
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

                // Find the SOAP action from the lookup
                // Use the operation's input name if available
                var operationKey = new { OperationName = operation.Name, InputName = operation.Input?.Name ?? string.Empty };
                var operationInfos = bindingOperationsLookup[operationKey].ToList();
                var operationInfo = operationInfos.FirstOrDefault();
                string soapAction = operationInfo?.SoapAction ?? string.Empty;

                // Create a unique method name for this operation
                string methodName = $"{operation.Name}Async";

                // Check if we already have a method with this name
                if (methodNames.ContainsKey(methodName))
                {
                    // If we do, increment the count and append it to the method name
                    methodNames[methodName]++;

                    // For overloaded operations, append a suffix based on the input message name
                    // This ensures unique method names while maintaining readability
                    string suffix = string.Empty;
                    if (inputMessage != null)
                    {
                        // Check if the operation name already contains the suffix we would extract
                        // For example "PostSinglePaymentWithPaymentSourceV3_1" already contains "WithPaymentSource"
                        string messageName = inputMessage.Name;
                        if (messageName.Contains("With"))
                        {
                            string extractedSuffix = messageName.Substring(messageName.IndexOf("With"));
                            if (extractedSuffix.EndsWith("SoapIn"))
                            {
                                extractedSuffix = extractedSuffix.Substring(0, extractedSuffix.Length - 6);
                            }

                            // If the operation name already contains the suffix, don't append it again
                            if (operation.Name.Contains(extractedSuffix))
                            {
                                suffix = string.Empty;
                            }
                            else
                            {
                                suffix = extractedSuffix;
                            }
                        }
                        else if (operation.Input?.Name != null && !string.IsNullOrEmpty(operation.Input.Name))
                        {
                            suffix = "_" + operation.Input.Name;
                        }
                        else
                        {
                            suffix = "_" + methodNames[methodName];
                        }
                    }
                    else
                    {
                        suffix = "_" + methodNames[methodName];
                    }

                    methodName = $"{operation.Name}{suffix}Async";
                }
                else
                {
                    // If not, add it to the dictionary with a count of 1
                    methodNames[methodName] = 1;
                }

                // Create the method implementation with the unique name
                var methodDeclaration = MethodDeclaration(
                    ParseTypeName(returnType),
                    Identifier(methodName)
                )
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .AddParameterListParameters(parameters.ToArray());

                // Create the method body
                List<StatementSyntax> statements = new List<StatementSyntax>();

                // No need to create headers array anymore - we'll pass the header directly

                // Create the return statement
                ExpressionSyntax sendSoapRequestExpression;
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

                    var arguments = new List<ArgumentSyntax>
                    {
                        Argument(
                            LiteralExpression(
                                SyntaxKind.StringLiteralExpression,
                                Literal(soapAction)
                            )
                        ),
                        Argument(IdentifierName("request"))
                    };

                    if (headerParameters.Count > 0)
                    {
                        arguments.Add(Argument(IdentifierName(headerParameters[0].Identifier.Text)));
                    }
                    else
                    {
                        arguments.Add(Argument(LiteralExpression(SyntaxKind.NullLiteralExpression)));
                    }

                    arguments.Add(Argument(IdentifierName("cancellationToken")));

                    sendSoapRequestExpression = InvocationExpression(genericName)
                        .WithArgumentList(
                            ArgumentList(
                                SeparatedList(arguments)
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

                    var arguments = new List<ArgumentSyntax>
                    {
                        Argument(
                            LiteralExpression(
                                SyntaxKind.StringLiteralExpression,
                                Literal(soapAction)
                            )
                        ),
                        Argument(IdentifierName("request"))
                    };

                    if (headerParameters.Count > 0)
                    {
                        arguments.Add(Argument(IdentifierName(headerParameters[0].Identifier.Text)));
                    }
                    else
                    {
                        arguments.Add(Argument(LiteralExpression(SyntaxKind.NullLiteralExpression)));
                    }

                    arguments.Add(Argument(IdentifierName("cancellationToken")));

                    sendSoapRequestExpression = InvocationExpression(genericName)
                        .WithArgumentList(
                            ArgumentList(
                                SeparatedList(arguments)
                            )
                        );
                }

                statements.Add(ReturnStatement(sendSoapRequestExpression));

                methodDeclaration = methodDeclaration.WithBody(
                    Block(statements)
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
                    UsingDirective(ParseName($"{outputNamespace}.Models.Responses")),
                    UsingDirective(ParseName($"{outputNamespace}.Models.Headers"))
                )
                .AddMembers(
                    NamespaceDeclaration(ParseName($"{outputNamespace}.Client"))
                        .AddMembers(classDeclaration)
                );

            // Format the code
            var code = compilationUnit
                .NormalizeWhitespace()
                .ToFullString();

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
