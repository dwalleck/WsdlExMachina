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
        GenerateHeaderClasses(wsdlDefinition, outputNamespace, outputDirectory);
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
            var directoryPath = Path.Combine(outputDirectory, "Models", "Common");
            Directory.CreateDirectory(directoryPath);
            File.WriteAllText(Path.Combine(directoryPath, "Enums.cs"), code);
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

            // Ensure the directory exists
            var directoryPath = Path.Combine(outputDirectory, "Models", "Common");
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // Write the file
            File.WriteAllText(Path.Combine(directoryPath, $"{complexType.Name}.cs"), code);
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

    private void GenerateHeaderClasses(WsdlDefinition wsdlDefinition, string outputNamespace, string outputDirectory)
    {
        // Check if we need to generate a SoapAuthHeader class
        bool hasSWBCAuthHeader = false;

        // First pass: check if any headers use SWBCAuthHeader
        foreach (var binding in wsdlDefinition.Bindings)
        {
            foreach (var operation in binding.Operations)
            {
                // Process input headers
                if (operation.Input != null && operation.Input.Headers.Count > 0)
                {
                    foreach (var header in operation.Input.Headers)
                    {
                        // Find the message referenced by the header
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

                    if (hasSWBCAuthHeader)
                        break;
                }

                if (hasSWBCAuthHeader)
                    break;
            }

            if (hasSWBCAuthHeader)
                break;
        }

        // If we found SWBCAuthHeader usage, generate a single SoapAuthHeader class
        if (hasSWBCAuthHeader)
        {
            GenerateSoapAuthHeaderClass(wsdlDefinition, outputNamespace, outputDirectory);
        }

        // We don't need to generate individual header classes for SWBCAuthHeader
        // since we're using a common SoapAuthHeader class for all of them
    }

    private void GenerateSoapAuthHeaderClass(WsdlDefinition wsdlDefinition, string outputNamespace, string outputDirectory)
    {
        // Create the SoapAuthHeader class
        var headerClass = ClassDeclaration("SoapAuthHeader")
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .AddMembers(
                PropertyDeclaration(
                    ParseTypeName("SWBCAuthHeader"),
                    Identifier("SWBCAuthHeader")
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
                                                    Literal("SWBCAuthHeader")
                                                )
                                            )
                                        }
                                    )
                                )
                            )
                        )
                    )
                )
            )
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
                                                Literal("SWBCAuthHeader")
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
                UsingDirective(ParseName("System.Xml.Serialization")),
                UsingDirective(ParseName("System.ComponentModel.DataAnnotations")),
                UsingDirective(ParseName($"{outputNamespace}.Models.Common"))
            )
            .AddMembers(
                NamespaceDeclaration(ParseName($"{outputNamespace}.Models.Headers"))
                    .AddMembers(headerClass)
            );

        // Format the code
        var code = compilationUnit
            .NormalizeWhitespace()
            .ToFullString();

        // Ensure the directory exists
        var directoryPath = Path.Combine(outputDirectory, "Models", "Headers");
        Directory.CreateDirectory(directoryPath);

        // Write the file
        File.WriteAllText(Path.Combine(directoryPath, "SoapAuthHeader.cs"), code);
    }

    private void GenerateHeaderClass(WsdlDefinition wsdlDefinition, string outputNamespace, string outputDirectory, WsdlMessage headerMessage)
    {
        var headerClassName = headerMessage.Name;
        var headerClassMembers = new List<MemberDeclarationSyntax>();

        foreach (var part in headerMessage.Parts)
        {
            if (!string.IsNullOrEmpty(part.Element))
            {
                // Find the element in the types
                var element = wsdlDefinition.Types.Elements.FirstOrDefault(e => e.Name == part.Element);
                if (element != null)
                {
                    // Check if this element is defined in the schema with child elements
                    var elementDefinition = FindElementDefinitionInSchema(wsdlDefinition, element.Name);
                    if (elementDefinition != null && elementDefinition.Count > 0)
                    {
                        // Generate properties for each child element
                        foreach (var childElement in elementDefinition)
                        {
                            string paramType = GetTypeForElement(wsdlDefinition, childElement);

                            // Skip if the element is an empty complex type
                            if (string.IsNullOrEmpty(paramType))
                            {
                                continue;
                            }

                            // Create the property
                            var propertyName = _generator.ToPascalCase(childElement.Name);
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
                                                                Literal(childElement.Name)
                                                            )
                                                        )
                                                    }
                                                )
                                            )
                                        )
                                    )
                                )
                            );

                            // Add IsRequired attribute if the element is required
                            if (!childElement.IsOptional)
                            {
                                property = property.AddAttributeLists(
                                    AttributeList(
                                        SingletonSeparatedList(
                                            Attribute(
                                                IdentifierName("Required"),
                                                AttributeArgumentList()
                                            )
                                        )
                                    )
                                );
                            }

                            headerClassMembers.Add(property);
                        }
                    }
                    else
                    {
                        // Fallback to the old behavior if we can't find child elements
                        string paramType = GetTypeForElement(wsdlDefinition, element);

                        // Skip generating a property if the element is an empty complex type
                        if (!string.IsNullOrEmpty(paramType))
                        {
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

                            headerClassMembers.Add(property);
                        }
                    }
                }
            }
            else if (!string.IsNullOrEmpty(part.Type))
            {
                string paramType = _generator.MapXsdTypeToClrType(part.Type);

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

                headerClassMembers.Add(property);
            }
        }

        // Create the class
        var headerClass = ClassDeclaration(headerClassName)
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .AddMembers(headerClassMembers.ToArray())
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
                                                Literal(headerMessage.Parts[0].Element ?? headerMessage.Name)
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
                UsingDirective(ParseName("System.Xml.Serialization")),
                UsingDirective(ParseName("System.ComponentModel.DataAnnotations")),
                UsingDirective(ParseName($"{outputNamespace}.Models.Common"))
            )
            .AddMembers(
                NamespaceDeclaration(ParseName($"{outputNamespace}.Models.Headers"))
                    .AddMembers(headerClass)
            );

        // Format the code
        var code = compilationUnit
            .NormalizeWhitespace()
            .ToFullString();

        // Ensure the directory exists
        var directoryPath = Path.Combine(outputDirectory, "Models", "Headers");
        Directory.CreateDirectory(directoryPath);

        // Write the file
        File.WriteAllText(Path.Combine(directoryPath, $"{headerClassName}.cs"), code);
    }

    private void GenerateRequestClass(WsdlDefinition wsdlDefinition, string outputNamespace, string outputDirectory,
        WsdlOperation operation, WsdlMessage inputMessage)
    {
        var requestClassName = $"{operation.Name}Request";
        var requestClassMembers = new List<MemberDeclarationSyntax>();

        foreach (var part in inputMessage.Parts)
        {
            if (!string.IsNullOrEmpty(part.Element))
            {
                // Find the element in the types
                var element = wsdlDefinition.Types.Elements.FirstOrDefault(e => e.Name == part.Element);
                if (element != null)
                {
                    // Check if this element is defined in the schema with child elements
                    var elementDefinition = FindElementDefinitionInSchema(wsdlDefinition, element.Name);
                    if (elementDefinition != null && elementDefinition.Count > 0)
                    {
                        // Generate properties for each child element
                        foreach (var childElement in elementDefinition)
                        {
                            string paramType = GetTypeForElement(wsdlDefinition, childElement);

                            // Create the property
                            var propertyName = _generator.ToPascalCase(childElement.Name);
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
                                                                Literal(childElement.Name)
                                                            )
                                                        )
                                                    }
                                                )
                                            )
                                        )
                                    )
                                )
                            );

                            // Add IsRequired attribute if the element is required
                            if (!childElement.IsOptional)
                            {
                                property = property.AddAttributeLists(
                                    AttributeList(
                                        SingletonSeparatedList(
                                            Attribute(
                                                IdentifierName("Required"),
                                                AttributeArgumentList()
                                            )
                                        )
                                    )
                                );
                            }

                            requestClassMembers.Add(property);
                        }
                    }
                    else
                    {
                        // Fallback to the old behavior if we can't find child elements
                        string paramType = GetTypeForElement(wsdlDefinition, element);

                        // Skip generating a property if the element is an empty complex type
                        if (!string.IsNullOrEmpty(paramType))
                        {
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
                    }
                }
            }
            else if (!string.IsNullOrEmpty(part.Type))
            {
                string paramType = _generator.MapXsdTypeToClrType(part.Type);

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
            else
            {
                // Look for the element in the schema
                var elementName = part.Element;
                if (!string.IsNullOrEmpty(elementName))
                {
                    // Find the element in the schema
                    var schemaElement = wsdlDefinition.Types.Elements.FirstOrDefault(e => e.Name == elementName);
                    if (schemaElement != null)
                    {
                        // Look for the element definition in the schema
                        var elementDefinition = FindElementDefinitionInSchema(wsdlDefinition, elementName);
                        if (elementDefinition != null && elementDefinition.Count > 0)
                        {
                            // Generate properties for each child element
                            foreach (var childElement in elementDefinition)
                            {
                                string paramType = GetTypeForElement(wsdlDefinition, childElement);

                                // Create the property
                                var propertyName = _generator.ToPascalCase(childElement.Name);
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
                                                                    Literal(childElement.Name)
                                                                )
                                                            )
                                                        }
                                                    )
                                                )
                                            )
                                        )
                                    )
                                );

                                // Add IsRequired attribute if the element is required
                                if (!childElement.IsOptional)
                                {
                                    property = property.AddAttributeLists(
                                        AttributeList(
                                            SingletonSeparatedList(
                                                Attribute(
                                                    IdentifierName("Required"),
                                                    AttributeArgumentList()
                                                )
                                            )
                                        )
                                    );
                                }

                                requestClassMembers.Add(property);
                            }
                        }
                        else
                        {
                            // Fallback for parts with no element definition
                            var propertyName = _generator.ToPascalCase(part.Name);
                            var property = PropertyDeclaration(
                                ParseTypeName("object"),
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
                    }
                    else
                    {
                        // Fallback for parts with no element or type
                        var propertyName = _generator.ToPascalCase(part.Name);
                        var property = PropertyDeclaration(
                            ParseTypeName("object"),
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
                }
                else
                {
                    // Fallback for parts with no element or type
                    var propertyName = _generator.ToPascalCase(part.Name);
                    var property = PropertyDeclaration(
                        ParseTypeName("object"),
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
            }
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
                    UsingDirective(ParseName("System.Xml.Serialization")),
                    UsingDirective(ParseName("System.ComponentModel.DataAnnotations")),
                    UsingDirective(ParseName($"{outputNamespace}.Models.Common"))
                )
            .AddMembers(
                NamespaceDeclaration(ParseName($"{outputNamespace}.Models.Requests"))
                    .AddMembers(requestClass)
            );

        // Format the code
        var code = compilationUnit
            .NormalizeWhitespace()
            .ToFullString();

        // Ensure the directory exists
        var requestDirectoryPath = Path.Combine(outputDirectory, "Models", "Requests");
        Directory.CreateDirectory(requestDirectoryPath);

        // Write the file
        File.WriteAllText(Path.Combine(requestDirectoryPath, $"{requestClassName}.cs"), code);
    }

    // Helper method to find element definition with child elements in schema
    private List<WsdlElement> FindElementDefinitionInSchema(WsdlDefinition wsdlDefinition, string elementName)
    {
        // Look for the element in the schema
        foreach (var schema in wsdlDefinition.Types.Schemas)
        {
            foreach (var item in schema.Items)
            {
                if (item is System.Xml.Schema.XmlSchemaElement schemaElement &&
                    schemaElement.Name == elementName &&
                    schemaElement.SchemaType is System.Xml.Schema.XmlSchemaComplexType complexType)
                {
                    var result = new List<WsdlElement>();

                    // Process the complex type's particle (sequence, choice, etc.)
                    if (complexType.Particle is System.Xml.Schema.XmlSchemaSequence sequence)
                    {
                        foreach (var sequenceItem in sequence.Items)
                        {
                            if (sequenceItem is System.Xml.Schema.XmlSchemaElement childElement)
                            {
                                var element = new WsdlElement
                                {
                                    Name = childElement.Name,
                                    Type = GetTypeNameFromSchemaElement(childElement),
                                    IsOptional = childElement.MinOccurs == 0,
                                    MinOccurs = (int)childElement.MinOccurs,
                                    MaxOccurs = childElement.MaxOccursString == "unbounded" ? int.MaxValue : (int)childElement.MaxOccurs,
                                    IsArray = childElement.MaxOccurs > 1 || childElement.MaxOccursString == "unbounded"
                                };

                                result.Add(element);
                            }
                        }
                    }

                    // Return an empty list for empty complex types (instead of null)
                    // This indicates that the element exists but has no child elements
                    return result;
                }
            }
        }

        return null;
    }

    private string GetTypeForElement(WsdlDefinition wsdlDefinition, WsdlElement element)
    {
        if (!string.IsNullOrEmpty(element.Type))
        {
            // Check if this is a simple type that might be an enum
            var simpleType = wsdlDefinition.Types.SimpleTypes.FirstOrDefault(st => st.Name == element.Type && st.IsEnum);
            if (simpleType != null)
            {
                return simpleType.Name;
            }

            // Check if this is a complex type
            var complexType = wsdlDefinition.Types.ComplexTypes.FirstOrDefault(ct => ct.Name == element.Type);
            if (complexType != null)
            {
                return complexType.Name;
            }

            return _generator.MapXsdTypeToClrType(element.Type);
        }
        else
        {
            // Try to find a complex type with this name
            var complexType = wsdlDefinition.Types.ComplexTypes.FirstOrDefault(ct => ct.Name == element.Name);
            if (complexType != null)
            {
                return complexType.Name;
            }

            // For elements with empty complex types (like TestF5Function),
            // we should return a more appropriate type than object
            // Check if this is an empty complex type by looking at the schema
            foreach (var schema in wsdlDefinition.Types.Schemas)
            {
                foreach (var item in schema.Items)
                {
                    if (item is System.Xml.Schema.XmlSchemaElement schemaElement &&
                        schemaElement.Name == element.Name &&
                        schemaElement.SchemaType is System.Xml.Schema.XmlSchemaComplexType emptyComplexType &&
                        (emptyComplexType.Particle == null ||
                         (emptyComplexType.Particle is System.Xml.Schema.XmlSchemaSequence sequence && sequence.Items.Count == 0)))
                    {
                        // This is an empty complex type, so we don't need a property for it
                        return string.Empty;
                    }
                }
            }

            return "object";
        }
    }

    private string GetTypeNameFromSchemaElement(System.Xml.Schema.XmlSchemaElement element)
    {
        if (element.SchemaTypeName != null && !string.IsNullOrEmpty(element.SchemaTypeName.Name))
        {
            // Check if this is a standard XSD type
            if (element.SchemaTypeName.Namespace == "http://www.w3.org/2001/XMLSchema")
            {
                return element.SchemaTypeName.Name;
            }

            // This is a custom type, return the name
            return element.SchemaTypeName.Name;
        }

        // Handle inline types
        if (element.SchemaType is System.Xml.Schema.XmlSchemaComplexType complexType)
        {
            // Try to determine if this is a complex type with elements
            if (complexType.Particle is System.Xml.Schema.XmlSchemaSequence sequence && sequence.Items.Count > 0)
            {
                // This is a complex type with elements, but we'll handle it as an object for now
                return "object";
            }

            return "object"; // Complex inline type
        }
        else if (element.SchemaType is System.Xml.Schema.XmlSchemaSimpleType simpleType)
        {
            // Try to determine the base type of the simple type
            if (simpleType.Content is System.Xml.Schema.XmlSchemaSimpleTypeRestriction restriction &&
                restriction.BaseTypeName != null)
            {
                return restriction.BaseTypeName.Name;
            }
        }

        return "object"; // Default fallback
    }

    private void GenerateResponseClass(WsdlDefinition wsdlDefinition, string outputNamespace, string outputDirectory,
        WsdlOperation operation, WsdlMessage outputMessage)
    {
        var responseClassName = $"{operation.Name}Response";
        var responseClassMembers = new List<MemberDeclarationSyntax>();

        foreach (var part in outputMessage.Parts)
        {
            if (!string.IsNullOrEmpty(part.Element))
            {
                // Find the element in the types
                var element = wsdlDefinition.Types.Elements.FirstOrDefault(e => e.Name == part.Element);
                if (element != null)
                {
                    // Check if this element is defined in the schema with child elements
                    var elementDefinition = FindElementDefinitionInSchema(wsdlDefinition, element.Name);
                    if (elementDefinition != null && elementDefinition.Count > 0)
                    {
                        // Generate properties for each child element
                        foreach (var childElement in elementDefinition)
                        {
                            string paramType = GetTypeForElement(wsdlDefinition, childElement);

                            // Create the property
                            var propertyName = _generator.ToPascalCase(childElement.Name);
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
                                                                Literal(childElement.Name)
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
                    }
                    else
                    {
                        // Fallback to the old behavior if we can't find child elements
                        string paramType = GetTypeForElement(wsdlDefinition, element);

                        // Skip generating a property if the element is an empty complex type
                        if (!string.IsNullOrEmpty(paramType))
                        {
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
                    }
                }
            }
            else if (!string.IsNullOrEmpty(part.Type))
            {
                string paramType = _generator.MapXsdTypeToClrType(part.Type);

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
            else
            {
                // Fallback for parts with no element or type
                var propertyName = _generator.ToPascalCase(part.Name);
                var property = PropertyDeclaration(
                    ParseTypeName("object"),
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
                    UsingDirective(ParseName("System.Xml.Serialization")),
                    UsingDirective(ParseName("System.ComponentModel.DataAnnotations")),
                    UsingDirective(ParseName($"{outputNamespace}.Models.Common"))
                )
            .AddMembers(
                NamespaceDeclaration(ParseName($"{outputNamespace}.Models.Responses"))
                    .AddMembers(responseClass)
            );

        // Format the code
        var code = compilationUnit
            .NormalizeWhitespace()
            .ToFullString();

        // Ensure the directory exists
        var directoryPath = Path.Combine(outputDirectory, "Models", "Responses");
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        // Write the file
        File.WriteAllText(Path.Combine(directoryPath, $"{responseClassName}.cs"), code);
    }
}
