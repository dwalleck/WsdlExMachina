using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WsdlExMachina.Parser.Models;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace WsdlExMachina.Generator.Generators;

/// <summary>
/// Generates model classes for the SOAP client.
/// </summary>
public class ModelsGenerator : ICodeGenerator
{
    private readonly SoapClientGenerator _generator;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelsGenerator"/> class.
    /// </summary>
    /// <param name="generator">The SOAP client generator.</param>
    public ModelsGenerator(SoapClientGenerator generator)
    {
        _generator = generator;
    }

    /// <summary>
    /// Generates model classes.
    /// </summary>
    /// <param name="wsdlDefinition">The WSDL definition.</param>
    /// <param name="outputNamespace">The namespace to use for the generated code.</param>
    /// <param name="outputDirectory">The directory where the files will be created.</param>
    public void Generate(WsdlDefinition wsdlDefinition, string outputNamespace, string outputDirectory)
    {
        GenerateEnums(wsdlDefinition, outputNamespace, outputDirectory);
        GenerateComplexTypes(wsdlDefinition, outputNamespace, outputDirectory);
        GenerateRequestResponseClasses(wsdlDefinition, outputNamespace, outputDirectory);
    }

    private void GenerateEnums(WsdlDefinition wsdlDefinition, string outputNamespace, string outputDirectory)
    {
        // Generate enums
        var enums = wsdlDefinition.Types.SimpleTypes
            .Where(st => st.IsEnum)
            .Select(st => (EnumDeclarationSyntax)_generator.GenerateEnum(st))
            .ToList();

        if (enums.Any())
        {
            // Create the compilation unit
            var compilationUnit = CompilationUnit()
                .AddUsings(
                    UsingDirective(ParseName("System")),
                    UsingDirective(ParseName("System.Runtime.Serialization"))
                )
                .AddMembers(
                    NamespaceDeclaration(ParseName($"{outputNamespace}.Models.Common"))
                        .AddMembers(enums.ToArray())
                );

            // Format the code
            var code = compilationUnit
                .NormalizeWhitespace()
                .ToFullString();

            // Write the file
            File.WriteAllText(Path.Combine(outputDirectory, "Models", "Common", "Enums.cs"), code);
        }
    }

    private void GenerateComplexTypes(WsdlDefinition wsdlDefinition, string outputNamespace, string outputDirectory)
    {
        // Generate complex types
        foreach (var complexType in wsdlDefinition.Types.ComplexTypes)
        {
            // Create the compilation unit
            var compilationUnit = CompilationUnit()
                .AddUsings(
                    UsingDirective(ParseName("System")),
                    UsingDirective(ParseName("System.Collections.Generic")),
                    UsingDirective(ParseName("System.Runtime.Serialization")),
                    UsingDirective(ParseName("System.Xml.Serialization"))
                )
                .AddMembers(
                    NamespaceDeclaration(ParseName($"{outputNamespace}.Models.Common"))
                        .AddMembers(_generator.GenerateDataContract(complexType))
                );

            // Format the code
            var code = compilationUnit
                .NormalizeWhitespace()
                .ToFullString();

            // Write the file
            File.WriteAllText(Path.Combine(outputDirectory, "Models", "Common", $"{complexType.Name}.cs"), code);
        }
    }

    private void GenerateRequestResponseClasses(WsdlDefinition wsdlDefinition, string outputNamespace, string outputDirectory)
    {
        // Generate request and response classes
        foreach (var portType in wsdlDefinition.PortTypes)
        {
            foreach (var operation in portType.Operations)
            {
                // Find the input and output messages
                var inputMessage = operation.Input != null
                    ? wsdlDefinition.Messages.FirstOrDefault(m => m.Name == operation.Input.Message)
                    : null;

                var outputMessage = operation.Output != null
                    ? wsdlDefinition.Messages.FirstOrDefault(m => m.Name == operation.Output.Message)
                    : null;

                // Generate request class
                if (inputMessage != null && inputMessage.Parts.Count > 0)
                {
                    GenerateRequestClass(wsdlDefinition, outputNamespace, outputDirectory, operation, inputMessage);
                }

                // Generate response class
                if (outputMessage != null && outputMessage.Parts.Count > 0)
                {
                    GenerateResponseClass(wsdlDefinition, outputNamespace, outputDirectory, operation, outputMessage);
                }
            }
        }
    }

    private void GenerateRequestClass(WsdlDefinition wsdlDefinition, string outputNamespace, string outputDirectory,
        WsdlOperation operation, WsdlMessage inputMessage)
    {
        var requestClassName = $"{operation.Name}Request";
        var requestClassMembers = new List<MemberDeclarationSyntax>();

        foreach (var part in inputMessage.Parts)
        {
            string paramType;
            if (!string.IsNullOrEmpty(part.Element))
            {
                // Find the element in the types
                var element = wsdlDefinition.Types.Elements.FirstOrDefault(e => e.Name == part.Element);
                if (element != null && !string.IsNullOrEmpty(element.Type))
                {
                    paramType = _generator.MapXsdTypeToClrType(element.Type);
                }
                else
                {
                    paramType = "object";
                }
            }
            else if (!string.IsNullOrEmpty(part.Type))
            {
                paramType = _generator.MapXsdTypeToClrType(part.Type);
            }
            else
            {
                paramType = "object";
            }

            // Create the property
            var propertyName = _generator.ToPascalCase(part.Name);
            var property = PropertyDeclaration(
                ParseTypeName(paramType),
                Identifier(propertyName)
            )
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .AddAccessorListAccessors(
                AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                    .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                    .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
            )
            .AddAttributeLists(
                AttributeList(
                    SingletonSeparatedList(
                        Attribute(
                            IdentifierName("XmlElement"),
                            AttributeArgumentList(
                                SeparatedList(
                                    new[] {
                                        AttributeArgument(
                                            NameEquals(IdentifierName("ElementName")),
                                            null,
                                            LiteralExpression(
                                                SyntaxKind.StringLiteralExpression,
                                                Literal(part.Name)
                                            )
                                        )
                                    }
                                )
                            )
                        )
                    )
                )
            );

            requestClassMembers.Add(property);
        }

        // Create the class
        var requestClass = ClassDeclaration(requestClassName)
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .AddMembers(requestClassMembers.ToArray())
            .AddAttributeLists(
                AttributeList(
                    SingletonSeparatedList(
                        Attribute(
                            IdentifierName("XmlRoot"),
                            AttributeArgumentList(
                                SeparatedList(
                                    new[] {
                                        AttributeArgument(
                                            NameEquals(IdentifierName("ElementName")),
                                            null,
                                            LiteralExpression(
                                                SyntaxKind.StringLiteralExpression,
                                                Literal(operation.Name)
                                            )
                                        ),
                                        AttributeArgument(
                                            NameEquals(IdentifierName("Namespace")),
                                            null,
                                            LiteralExpression(
                                                SyntaxKind.StringLiteralExpression,
                                                Literal(wsdlDefinition.TargetNamespace)
                                            )
                                        )
                                    }
                                )
                            )
                        )
                    )
                )
            );

        // Create the compilation unit
        var compilationUnit = CompilationUnit()
            .AddUsings(
                UsingDirective(ParseName("System")),
                UsingDirective(ParseName("System.Xml.Serialization"))
            )
            .AddMembers(
                NamespaceDeclaration(ParseName($"{outputNamespace}.Models.Requests"))
                    .AddMembers(requestClass)
            );

        // Format the code
        var code = compilationUnit
            .NormalizeWhitespace()
            .ToFullString();

        // Write the file
        File.WriteAllText(Path.Combine(outputDirectory, "Models", "Requests", $"{requestClassName}.cs"), code);
    }

    private void GenerateResponseClass(WsdlDefinition wsdlDefinition, string outputNamespace, string outputDirectory,
        WsdlOperation operation, WsdlMessage outputMessage)
    {
        var responseClassName = $"{operation.Name}Response";
        var responseClassMembers = new List<MemberDeclarationSyntax>();

        foreach (var part in outputMessage.Parts)
        {
            string paramType;
            if (!string.IsNullOrEmpty(part.Element))
            {
                // Find the element in the types
                var element = wsdlDefinition.Types.Elements.FirstOrDefault(e => e.Name == part.Element);
                if (element != null && !string.IsNullOrEmpty(element.Type))
                {
                    paramType = _generator.MapXsdTypeToClrType(element.Type);
                }
                else
                {
                    paramType = "object";
                }
            }
            else if (!string.IsNullOrEmpty(part.Type))
            {
                paramType = _generator.MapXsdTypeToClrType(part.Type);
            }
            else
            {
                paramType = "object";
            }

            // Create the property
            var propertyName = _generator.ToPascalCase(part.Name);
            var property = PropertyDeclaration(
                ParseTypeName(paramType),
                Identifier(propertyName)
            )
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .AddAccessorListAccessors(
                AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                    .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                    .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
            )
            .AddAttributeLists(
                AttributeList(
                    SingletonSeparatedList(
                        Attribute(
                            IdentifierName("XmlElement"),
                            AttributeArgumentList(
                                SeparatedList(
                                    new[] {
                                        AttributeArgument(
                                            NameEquals(IdentifierName("ElementName")),
                                            null,
                                            LiteralExpression(
                                                SyntaxKind.StringLiteralExpression,
                                                Literal(part.Name)
                                            )
                                        )
                                    }
                                )
                            )
                        )
                    )
                )
            );

            responseClassMembers.Add(property);
        }

        // Create the class
        var responseClass = ClassDeclaration(responseClassName)
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .AddMembers(responseClassMembers.ToArray())
            .AddAttributeLists(
                AttributeList(
                    SingletonSeparatedList(
                        Attribute(
                            IdentifierName("XmlRoot"),
                            AttributeArgumentList(
                                SeparatedList(
                                    new[] {
                                        AttributeArgument(
                                            NameEquals(IdentifierName("ElementName")),
                                            null,
                                            LiteralExpression(
                                                SyntaxKind.StringLiteralExpression,
                                                Literal($"{operation.Name}Response")
                                            )
                                        ),
                                        AttributeArgument(
                                            NameEquals(IdentifierName("Namespace")),
                                            null,
                                            LiteralExpression(
                                                SyntaxKind.StringLiteralExpression,
                                                Literal(wsdlDefinition.TargetNamespace)
                                            )
                                        )
                                    }
                                )
                            )
                        )
                    )
                )
            );

        // Create the compilation unit
        var compilationUnit = CompilationUnit()
            .AddUsings(
                UsingDirective(ParseName("System")),
                UsingDirective(ParseName("System.Xml.Serialization"))
            )
            .AddMembers(
                NamespaceDeclaration(ParseName($"{outputNamespace}.Models.Responses"))
                    .AddMembers(responseClass)
            );

        // Format the code
        var code = compilationUnit
            .NormalizeWhitespace()
            .ToFullString();

        // Write the file
        File.WriteAllText(Path.Combine(outputDirectory, "Models", "Responses", $"{responseClassName}.cs"), code);
    }
}
