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
            // We need to handle operations with the same name but different input/output messages
            // Create a dictionary to track method names to avoid duplicates
            var methodNames = new Dictionary<string, int>();

            foreach (var operation in portType.Operations)
            {
                // Find the input and output messages
                var inputMessage = operation.Input != null
                    ? wsdlDefinition.Messages.FirstOrDefault(m => m.Name == operation.Input.Message)
                    : null;

                var outputMessage = operation.Output != null
                    ? wsdlDefinition.Messages.FirstOrDefault(m => m.Name == operation.Output.Message)
                    : null;

                // Create a unique method name for this operation
                string methodName = $"{operation.Name}Async";

                // Determine return type and parameters
                string returnType = "Task";
                var parameters = new List<ParameterSyntax>();

                if (inputMessage != null && inputMessage.Parts.Count > 0)
                {
                    // Create a unique request class name for this operation
                    string requestClassName = $"{operation.Name}Request";

                    // Check if we already have a method with this name
                    if (methodNames.ContainsKey(methodName))
                    {
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
                                    // Check if the suffix contains a version (like V3_1) that's already in the operation name
                                    // to avoid duplicates like PostSinglePaymentV3_1WithApplyToV3_1
                                    if (extractedSuffix.Contains("V3_1") && operation.Name.Contains("V3_1"))
                                    {
                                        // Replace the version in the suffix to avoid duplication
                                        extractedSuffix = extractedSuffix.Replace("V3_1", "");
                                    }

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

                        // Use the same suffix for the request class name as we do for the method name
                        if (!string.IsNullOrEmpty(suffix))
                        {
                            requestClassName = $"{operation.Name}{suffix}Request";
                        }
                    }

                    // Use the request class
                    parameters.Add(
                        Parameter(
                            Identifier("request")
                        )
                        .WithType(
                            IdentifierName(requestClassName)
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
                    // Create a unique response class name for this operation
                    string responseClassName = $"{operation.Name}Response";

                    // Check if we already have a method with this name
                    if (methodNames.ContainsKey(methodName))
                    {
                        // For overloaded operations, append a suffix based on the output message name
                        string suffix = string.Empty;
                        if (outputMessage != null)
                        {
                            // Check if the operation name already contains the suffix we would extract
                            string messageName = outputMessage.Name;
                            if (messageName.Contains("With"))
                            {
                                string extractedSuffix = messageName.Substring(messageName.IndexOf("With"));
                                if (extractedSuffix.EndsWith("SoapOut"))
                                {
                                    extractedSuffix = extractedSuffix.Substring(0, extractedSuffix.Length - 7);
                                }

                                // If the operation name already contains the suffix, don't append it again
                                if (operation.Name.Contains(extractedSuffix))
                                {
                                    suffix = string.Empty;
                                }
                                else
                                {
                                    // Check if the suffix contains a version (like V3_1) that's already in the operation name
                                    // to avoid duplicates like PostSinglePaymentV3_1WithApplyToV3_1
                                    if (extractedSuffix.Contains("V3_1") && operation.Name.Contains("V3_1"))
                                    {
                                        // Replace the version in the suffix to avoid duplication
                                        extractedSuffix = extractedSuffix.Replace("V3_1", "");
                                    }

                                    suffix = extractedSuffix;
                                }
                            }
                            else if (operation.Output?.Name != null && !string.IsNullOrEmpty(operation.Output.Name))
                            {
                                suffix = "_" + operation.Output.Name;
                            }
                            else
                            {
                                suffix = "_" + methodNames[methodName];
                            }
                        }

                        // Use the same suffix for the response class name as we do for the method name
                        if (!string.IsNullOrEmpty(suffix))
                        {
                            responseClassName = $"{operation.Name}{suffix}Response";
                        }
                    }

                    // Use the response class
                    returnType = $"Task<{responseClassName}>";
                }

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

                // Create the method declaration with the unique name
                var methodDeclaration = MethodDeclaration(
                    ParseTypeName(returnType),
                    Identifier(methodName)
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
