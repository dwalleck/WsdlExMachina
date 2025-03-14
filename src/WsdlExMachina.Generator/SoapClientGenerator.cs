using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WsdlExMachina.Parser;
using WsdlExMachina.Parser.Models;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace WsdlExMachina.Generator;

/// <summary>
/// Generates C# SOAP client code from a WSDL definition.
/// </summary>
public class SoapClientGenerator
{
    private readonly WsdlParser _parser;

    /// <summary>
    /// Initializes a new instance of the <see cref="SoapClientGenerator"/> class.
    /// </summary>
    public SoapClientGenerator()
    {
        _parser = new WsdlParser();
    }

    /// <summary>
    /// Generates a C# SOAP client from a WSDL file.
    /// </summary>
    /// <param name="wsdlFilePath">The path to the WSDL file.</param>
    /// <param name="outputNamespace">The namespace to use for the generated code.</param>
    /// <returns>The generated C# code as a string.</returns>
    public string GenerateFromFile(string wsdlFilePath, string outputNamespace)
    {
        var wsdlDefinition = _parser.ParseFile(wsdlFilePath);
        return Generate(wsdlDefinition, outputNamespace);
    }

    /// <summary>
    /// Generates a C# SOAP client from a WSDL XML string.
    /// </summary>
    /// <param name="wsdlXml">The WSDL XML string.</param>
    /// <param name="outputNamespace">The namespace to use for the generated code.</param>
    /// <returns>The generated C# code as a string.</returns>
    public string GenerateFromXml(string wsdlXml, string outputNamespace)
    {
        var wsdlDefinition = _parser.ParseXml(wsdlXml);
        return Generate(wsdlDefinition, outputNamespace);
    }

    /// <summary>
    /// Generates a C# SOAP client from a WSDL definition.
    /// </summary>
    /// <param name="wsdlDefinition">The WSDL definition.</param>
    /// <param name="outputNamespace">The namespace to use for the generated code.</param>
    /// <returns>The generated C# code as a string.</returns>
    public string Generate(WsdlDefinition wsdlDefinition, string outputNamespace)
    {
        // Create the compilation unit (the root of the syntax tree)
        var compilationUnit = CompilationUnit()
            .AddUsings(
                UsingDirective(ParseName("System")),
                UsingDirective(ParseName("System.Collections.Generic")),
                UsingDirective(ParseName("System.Linq")),
                UsingDirective(ParseName("System.Text")),
                UsingDirective(ParseName("System.Threading.Tasks")),
                UsingDirective(ParseName("System.Xml")),
                UsingDirective(ParseName("System.Xml.Serialization")),
                UsingDirective(ParseName("System.ServiceModel")),
                UsingDirective(ParseName("System.ServiceModel.Channels"))
            );

        // Create the namespace
        var namespaceDeclaration = NamespaceDeclaration(ParseName(outputNamespace));

        // Add data contracts (complex types)
        foreach (var complexType in wsdlDefinition.Types.ComplexTypes)
        {
            namespaceDeclaration = namespaceDeclaration.AddMembers(GenerateDataContract(complexType));
        }

        // Add enum types
        foreach (var simpleType in wsdlDefinition.Types.SimpleTypes.Where(st => st.IsEnum))
        {
            namespaceDeclaration = namespaceDeclaration.AddMembers(GenerateEnum(simpleType));
        }

        // Add service contracts (interfaces)
        foreach (var portType in wsdlDefinition.PortTypes)
        {
            namespaceDeclaration = namespaceDeclaration.AddMembers(GenerateServiceContract(portType, wsdlDefinition));
        }

        // Add client classes
        foreach (var service in wsdlDefinition.Services)
        {
            namespaceDeclaration = namespaceDeclaration.AddMembers(GenerateClientClass(service, wsdlDefinition));
        }

        // Add the namespace to the compilation unit
        compilationUnit = compilationUnit.AddMembers(namespaceDeclaration);

        // Format the code
        var code = compilationUnit
            .NormalizeWhitespace()
            .ToFullString();

        return code;
    }

    private MemberDeclarationSyntax GenerateDataContract(WsdlComplexType complexType)
    {
        // Create the class declaration
        var classDeclaration = ClassDeclaration(complexType.Name)
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .AddAttributeLists(
                AttributeList(
                    SingletonSeparatedList(
                        Attribute(
                            IdentifierName("DataContract"),
                            AttributeArgumentList(
                                SeparatedList(
                                    new[] {
                                        AttributeArgument(
                                            NameEquals(IdentifierName("Namespace")),
                                            null,
                                            LiteralExpression(
                                                SyntaxKind.StringLiteralExpression,
                                                Literal(complexType.Namespace)
                                            )
                                        )
                                    }
                                )
                            )
                        )
                    )
                )
            );

        // Add base type if specified
        if (!string.IsNullOrEmpty(complexType.BaseType))
        {
            classDeclaration = classDeclaration.WithBaseList(
                BaseList(
                    SingletonSeparatedList<BaseTypeSyntax>(
                        SimpleBaseType(
                            IdentifierName(complexType.BaseType)
                        )
                    )
                )
            );
        }

        // Add properties for each element
        foreach (var element in complexType.Elements)
        {
            var propertyType = element.IsComplexType
                ? element.Type
                : MapXsdTypeToClrType(element.Type);

            if (element.IsArray)
            {
                propertyType = $"List<{propertyType}>";
            }

            var property = PropertyDeclaration(
                ParseTypeName(propertyType),
                Identifier(ToPascalCase(element.Name))
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
                            IdentifierName("DataMember"),
                            AttributeArgumentList(
                                SeparatedList(
                                    new[] {
                                        AttributeArgument(
                                            NameEquals(IdentifierName("Name")),
                                            null,
                                            LiteralExpression(
                                                SyntaxKind.StringLiteralExpression,
                                                Literal(element.Name)
                                            )
                                        ),
                                        AttributeArgument(
                                            NameEquals(IdentifierName("IsRequired")),
                                            null,
                                            element.IsOptional
                                                ? LiteralExpression(SyntaxKind.FalseLiteralExpression)
                                                : LiteralExpression(SyntaxKind.TrueLiteralExpression)
                                        )
                                    }
                                )
                            )
                        )
                    )
                )
            );

            // Add initialization for array properties
            if (element.IsArray)
            {
                property = property.WithInitializer(
                    EqualsValueClause(
                        ObjectCreationExpression(
                            ParseTypeName(propertyType)
                        )
                        .WithArgumentList(ArgumentList())
                    )
                )
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
            }

            classDeclaration = classDeclaration.AddMembers(property);
        }

        return classDeclaration;
    }

    private MemberDeclarationSyntax GenerateEnum(WsdlSimpleType simpleType)
    {
        // Create the enum declaration
        var enumDeclaration = EnumDeclaration(simpleType.Name)
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .AddAttributeLists(
                AttributeList(
                    SingletonSeparatedList(
                        Attribute(
                            IdentifierName("DataContract"),
                            AttributeArgumentList(
                                SeparatedList(
                                    new[] {
                                        AttributeArgument(
                                            NameEquals(IdentifierName("Namespace")),
                                            null,
                                            LiteralExpression(
                                                SyntaxKind.StringLiteralExpression,
                                                Literal(simpleType.Namespace)
                                            )
                                        )
                                    }
                                )
                            )
                        )
                    )
                )
            );

        // Add enum members
        for (int i = 0; i < simpleType.EnumerationValues.Count; i++)
        {
            var value = simpleType.EnumerationValues[i];
            var memberName = ToPascalCase(value);

            // Ensure the member name is a valid C# identifier
            if (char.IsDigit(memberName[0]))
            {
                memberName = "_" + memberName;
            }

            var enumMember = EnumMemberDeclaration(Identifier(memberName))
                .AddAttributeLists(
                    AttributeList(
                        SingletonSeparatedList(
                            Attribute(
                                IdentifierName("EnumMember"),
                                AttributeArgumentList(
                                    SeparatedList(
                                        new[] {
                                            AttributeArgument(
                                                NameEquals(IdentifierName("Value")),
                                                null,
                                                LiteralExpression(
                                                    SyntaxKind.StringLiteralExpression,
                                                    Literal(value)
                                                )
                                            )
                                        }
                                    )
                                )
                            )
                        )
                    )
                );

            enumDeclaration = enumDeclaration.AddMembers(enumMember);
        }

        return enumDeclaration;
    }

    private MemberDeclarationSyntax GenerateServiceContract(WsdlPortType portType, WsdlDefinition wsdlDefinition)
    {
        // Create the interface declaration
        var interfaceDeclaration = InterfaceDeclaration($"I{portType.Name}")
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .AddAttributeLists(
                AttributeList(
                    SingletonSeparatedList(
                        Attribute(
                            IdentifierName("ServiceContract"),
                            AttributeArgumentList(
                                SeparatedList(
                                    new[] {
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

        // Add methods for each operation
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
                foreach (var part in inputMessage.Parts)
                {
                    string paramType;
                    if (!string.IsNullOrEmpty(part.Element))
                    {
                        // Find the element in the types
                        var element = wsdlDefinition.Types.Elements.FirstOrDefault(e => e.Name == part.Element);
                        paramType = element != null ? element.Type : "object";
                    }
                    else if (!string.IsNullOrEmpty(part.Type))
                    {
                        paramType = MapXsdTypeToClrType(part.Type);
                    }
                    else
                    {
                        paramType = "object";
                    }

                    parameters.Add(
                        Parameter(
                            Identifier(ToCamelCase(part.Name))
                        )
                        .WithType(
                            ParseTypeName(paramType)
                        )
                    );
                }
            }

            if (outputMessage != null && outputMessage.Parts.Count > 0)
            {
                var part = outputMessage.Parts[0];
                string responseType;
                if (!string.IsNullOrEmpty(part.Element))
                {
                    // Find the element in the types
                    var element = wsdlDefinition.Types.Elements.FirstOrDefault(e => e.Name == part.Element);
                    responseType = element != null ? element.Type : "object";
                }
                else if (!string.IsNullOrEmpty(part.Type))
                {
                    responseType = MapXsdTypeToClrType(part.Type);
                }
                else
                {
                    responseType = "object";
                }

                returnType = $"Task<{responseType}>";
            }

            // Find the SOAP action from the binding
            string soapAction = string.Empty;
            foreach (var binding in wsdlDefinition.Bindings)
            {
                var bindingOperation = binding.Operations.FirstOrDefault(bo => bo.Name == operation.Name);
                if (bindingOperation != null && !string.IsNullOrEmpty(bindingOperation.SoapAction))
                {
                    soapAction = bindingOperation.SoapAction;
                    break;
                }
            }

            // Create the method declaration
            var methodDeclaration = MethodDeclaration(
                ParseTypeName(returnType),
                Identifier(operation.Name)
            )
            .AddParameterListParameters(parameters.ToArray())
            .AddAttributeLists(
                AttributeList(
                    SingletonSeparatedList(
                        Attribute(
                            IdentifierName("OperationContract"),
                            AttributeArgumentList(
                                SeparatedList(
                                    new[] {
                                        AttributeArgument(
                                            NameEquals(IdentifierName("Action")),
                                            null,
                                            LiteralExpression(
                                                SyntaxKind.StringLiteralExpression,
                                                Literal(soapAction)
                                            )
                                        )
                                    }
                                )
                            )
                        )
                    )
                )
            );

            interfaceDeclaration = interfaceDeclaration.AddMembers(methodDeclaration);
        }

        return interfaceDeclaration;
    }

    private MemberDeclarationSyntax GenerateClientClass(WsdlService service, WsdlDefinition wsdlDefinition)
    {
        // Find the port type for this service
        var port = service.Ports.FirstOrDefault();
        if (port == null)
        {
            return ClassDeclaration($"{service.Name}Client")
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .AddMembers(
                    ConstructorDeclaration(Identifier($"{service.Name}Client"))
                        .AddModifiers(Token(SyntaxKind.PublicKeyword))
                        .WithBody(Block())
                );
        }

        var binding = wsdlDefinition.Bindings.FirstOrDefault(b => b.Name == port.Binding);
        if (binding == null)
        {
            return ClassDeclaration($"{service.Name}Client")
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .AddMembers(
                    ConstructorDeclaration(Identifier($"{service.Name}Client"))
                        .AddModifiers(Token(SyntaxKind.PublicKeyword))
                        .WithBody(Block())
                );
        }

        var portType = wsdlDefinition.PortTypes.FirstOrDefault(pt => pt.Name == binding.Type);
        if (portType == null)
        {
            return ClassDeclaration($"{service.Name}Client")
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .AddMembers(
                    ConstructorDeclaration(Identifier($"{service.Name}Client"))
                        .AddModifiers(Token(SyntaxKind.PublicKeyword))
                        .WithBody(Block())
                );
        }

        // Create the class declaration
        var classDeclaration = ClassDeclaration($"{service.Name}Client")
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .AddBaseListTypes(
                SimpleBaseType(
                    ParseTypeName("ClientBase<I" + portType.Name + ">")
                ),
                SimpleBaseType(
                    ParseTypeName("I" + portType.Name)
                )
            );

        // Add constructors
        classDeclaration = classDeclaration.AddMembers(
            ConstructorDeclaration(Identifier($"{service.Name}Client"))
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .WithBody(Block()),

            ConstructorDeclaration(Identifier($"{service.Name}Client"))
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .AddParameterListParameters(
                    Parameter(Identifier("endpointConfigurationName"))
                        .WithType(ParseTypeName("string"))
                )
                .WithInitializer(
                    ConstructorInitializer(
                        SyntaxKind.BaseConstructorInitializer,
                        ArgumentList(
                            SingletonSeparatedList(
                                Argument(
                                    IdentifierName("endpointConfigurationName")
                                )
                            )
                        )
                    )
                )
                .WithBody(Block()),

            ConstructorDeclaration(Identifier($"{service.Name}Client"))
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .AddParameterListParameters(
                    Parameter(Identifier("endpointConfigurationName"))
                        .WithType(ParseTypeName("string")),
                    Parameter(Identifier("remoteAddress"))
                        .WithType(ParseTypeName("string"))
                )
                .WithInitializer(
                    ConstructorInitializer(
                        SyntaxKind.BaseConstructorInitializer,
                        ArgumentList(
                            SeparatedList(
                                new[] {
                                    Argument(IdentifierName("endpointConfigurationName")),
                                    Argument(IdentifierName("remoteAddress"))
                                }
                            )
                        )
                    )
                )
                .WithBody(Block()),

            ConstructorDeclaration(Identifier($"{service.Name}Client"))
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .AddParameterListParameters(
                    Parameter(Identifier("binding"))
                        .WithType(ParseTypeName("System.ServiceModel.Channels.Binding")),
                    Parameter(Identifier("remoteAddress"))
                        .WithType(ParseTypeName("System.ServiceModel.EndpointAddress"))
                )
                .WithInitializer(
                    ConstructorInitializer(
                        SyntaxKind.BaseConstructorInitializer,
                        ArgumentList(
                            SeparatedList(
                                new[] {
                                    Argument(IdentifierName("binding")),
                                    Argument(IdentifierName("remoteAddress"))
                                }
                            )
                        )
                    )
                )
                .WithBody(Block())
        );

        // Add method implementations
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
                foreach (var part in inputMessage.Parts)
                {
                    string paramType;
                    if (!string.IsNullOrEmpty(part.Element))
                    {
                        // Find the element in the types
                        var element = wsdlDefinition.Types.Elements.FirstOrDefault(e => e.Name == part.Element);
                        paramType = element != null ? element.Type : "object";
                    }
                    else if (!string.IsNullOrEmpty(part.Type))
                    {
                        paramType = MapXsdTypeToClrType(part.Type);
                    }
                    else
                    {
                        paramType = "object";
                    }

                    parameters.Add(
                        Parameter(
                            Identifier(ToCamelCase(part.Name))
                        )
                        .WithType(
                            ParseTypeName(paramType)
                        )
                    );
                }
            }

            if (outputMessage != null && outputMessage.Parts.Count > 0)
            {
                var part = outputMessage.Parts[0];
                string responseType;
                if (!string.IsNullOrEmpty(part.Element))
                {
                    // Find the element in the types
                    var element = wsdlDefinition.Types.Elements.FirstOrDefault(e => e.Name == part.Element);
                    responseType = element != null ? element.Type : "object";
                }
                else if (!string.IsNullOrEmpty(part.Type))
                {
                    responseType = MapXsdTypeToClrType(part.Type);
                }
                else
                {
                    responseType = "object";
                }

                returnType = $"Task<{responseType}>";
            }

            // Create the method implementation
            var methodDeclaration = MethodDeclaration(
                ParseTypeName(returnType),
                Identifier(operation.Name)
            )
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .AddParameterListParameters(parameters.ToArray());

            // Create the method body
            var parameterNames = parameters.Select(p => IdentifierName(p.Identifier.Text));

            ExpressionSyntax invocationExpression = InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        ThisExpression(),
                        IdentifierName("Channel")
                    ),
                    IdentifierName(operation.Name)
                )
            )
            .WithArgumentList(
                ArgumentList(
                    SeparatedList<ArgumentSyntax>(
                        parameterNames.Select(n => Argument(n))
                    )
                )
            );

            StatementSyntax returnStatement = ReturnStatement(invocationExpression);

            methodDeclaration = methodDeclaration.WithBody(
                Block(
                    returnStatement
                )
            );

            classDeclaration = classDeclaration.AddMembers(methodDeclaration);
        }

        return classDeclaration;
    }

    private string MapXsdTypeToClrType(string xsdType)
    {
        return xsdType switch
        {
            "string" => "string",
            "boolean" => "bool",
            "decimal" => "decimal",
            "float" => "float",
            "double" => "double",
            "duration" => "TimeSpan",
            "dateTime" => "DateTime",
            "time" => "TimeSpan",
            "date" => "DateTime",
            "gYearMonth" => "string",
            "gYear" => "string",
            "gMonthDay" => "string",
            "gDay" => "string",
            "gMonth" => "string",
            "hexBinary" => "byte[]",
            "base64Binary" => "byte[]",
            "anyURI" => "Uri",
            "QName" => "string",
            "NOTATION" => "string",
            "normalizedString" => "string",
            "token" => "string",
            "language" => "string",
            "IDREFS" => "string[]",
            "ENTITIES" => "string[]",
            "NMTOKEN" => "string",
            "NMTOKENS" => "string[]",
            "Name" => "string",
            "NCName" => "string",
            "ID" => "string",
            "IDREF" => "string",
            "ENTITY" => "string",
            "integer" => "int",
            "nonPositiveInteger" => "int",
            "negativeInteger" => "int",
            "long" => "long",
            "int" => "int",
            "short" => "short",
            "byte" => "byte",
            "nonNegativeInteger" => "uint",
            "unsignedLong" => "ulong",
            "unsignedInt" => "uint",
            "unsignedShort" => "ushort",
            "unsignedByte" => "byte",
            "positiveInteger" => "uint",
            "anyType" => "object",
            "anySimpleType" => "object",
            _ => "object"
        };
    }

    private string ToPascalCase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        // Remove any non-alphanumeric characters
        var sb = new StringBuilder();
        bool nextUpper = true;
        foreach (char c in name)
        {
            if (char.IsLetterOrDigit(c))
            {
                sb.Append(nextUpper ? char.ToUpper(c) : c);
                nextUpper = false;
            }
            else
            {
                nextUpper = true;
            }
        }

        return sb.ToString();
    }

    private string ToCamelCase(string name)
    {
        var pascal = ToPascalCase(name);
        if (string.IsNullOrEmpty(pascal))
        {
            return pascal;
        }

        return char.ToLower(pascal[0]) + pascal.Substring(1);
    }
}
