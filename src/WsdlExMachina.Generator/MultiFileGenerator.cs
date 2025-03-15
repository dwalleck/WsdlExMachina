using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.IO;
using WsdlExMachina.Parser.Models;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace WsdlExMachina.Generator;

/// <summary>
/// Generates C# SOAP client code across multiple files.
/// </summary>
public class MultiFileGenerator
{
    private readonly SoapClientGenerator _generator;

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiFileGenerator"/> class.
    /// </summary>
    /// <param name="generator">The SOAP client generator.</param>
    public MultiFileGenerator(SoapClientGenerator generator)
    {
        _generator = generator;
    }

    /// <summary>
    /// Generates C# SOAP client code across multiple files.
    /// </summary>
    /// <param name="wsdlDefinition">The WSDL definition.</param>
    /// <param name="outputNamespace">The namespace to use for the generated code.</param>
    /// <param name="outputDirectory">The directory where the files will be created.</param>
    public void Generate(WsdlDefinition wsdlDefinition, string outputNamespace, string outputDirectory)
    {
        // Create the directory structure
        CreateDirectoryStructure(outputDirectory);

        // Generate the base client class
        GenerateSoapClientBase(outputNamespace, outputDirectory);

        // Generate the service collection extensions
        GenerateServiceCollectionExtensions(outputNamespace, outputDirectory);

        // Generate the client options
        GenerateClientOptions(outputNamespace, outputDirectory);

        // Generate the models
        GenerateModels(wsdlDefinition, outputNamespace, outputDirectory);

        // Generate the interfaces
        GenerateInterfaces(wsdlDefinition, outputNamespace, outputDirectory);

        // Generate the client implementations
        GenerateClients(wsdlDefinition, outputNamespace, outputDirectory);
    }

    private void CreateDirectoryStructure(string outputDirectory)
    {
        // Create the main directory
        Directory.CreateDirectory(outputDirectory);

        // Create subdirectories
        Directory.CreateDirectory(Path.Combine(outputDirectory, "Models"));
        Directory.CreateDirectory(Path.Combine(outputDirectory, "Models", "Common"));
        Directory.CreateDirectory(Path.Combine(outputDirectory, "Models", "Requests"));
        Directory.CreateDirectory(Path.Combine(outputDirectory, "Models", "Responses"));
        Directory.CreateDirectory(Path.Combine(outputDirectory, "Interfaces"));
        Directory.CreateDirectory(Path.Combine(outputDirectory, "Client"));
        Directory.CreateDirectory(Path.Combine(outputDirectory, "Extensions"));
    }

    private void GenerateSoapClientBase(string outputNamespace, string outputDirectory)
    {
        // Create the SoapClientBase.cs file directly
        var content = @"using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;

namespace " + outputNamespace + @".Client;

/// <summary>
/// Base class for SOAP clients that use HttpClient with Polly resilience policies.
/// </summary>
public abstract class SoapClientBase
{
    protected readonly HttpClient _httpClient;
    protected readonly string _endpoint;
    protected IAsyncPolicy<HttpResponseMessage> _retryPolicy;
    protected IAsyncPolicy<HttpResponseMessage> _timeoutPolicy;
    protected IAsyncPolicy<HttpResponseMessage> _combinedPolicy;

    /// <summary>
    /// Initializes a new instance of the <see cref=""SoapClientBase""/> class.
    /// </summary>
    /// <param name=""endpoint"">The SOAP service endpoint URL.</param>
    /// <param name=""httpClient"">Optional HttpClient instance. If not provided, a new instance will be created.</param>
    protected SoapClientBase(string endpoint, HttpClient? httpClient = null)
    {
        _endpoint = endpoint;
        _httpClient = httpClient ?? new HttpClient();

        // Default timeout policy (30 seconds)
        _timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(30));

        // Default retry policy (3 retries with polynomial backoff)
        _retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(retryAttempt, 2)));

        // Combine policies
        _combinedPolicy = Policy.WrapAsync(_retryPolicy, _timeoutPolicy);
    }

    /// <summary>
    /// Configures the retry policy.
    /// </summary>
    /// <param name=""retryPolicy"">The retry policy to use.</param>
    public void ConfigureRetryPolicy(IAsyncPolicy<HttpResponseMessage> retryPolicy)
    {
        _retryPolicy = retryPolicy;
        _combinedPolicy = Policy.WrapAsync(_retryPolicy, _timeoutPolicy);
    }

    /// <summary>
    /// Configures the timeout policy.
    /// </summary>
    /// <param name=""timeoutPolicy"">The timeout policy to use.</param>
    public void ConfigureTimeoutPolicy(IAsyncPolicy<HttpResponseMessage> timeoutPolicy)
    {
        _timeoutPolicy = timeoutPolicy;
        _combinedPolicy = Policy.WrapAsync(_retryPolicy, _timeoutPolicy);
    }

    /// <summary>
    /// Configures a custom policy that will replace the combined policy.
    /// </summary>
    /// <param name=""policy"">The policy to use.</param>
    public void ConfigurePolicy(IAsyncPolicy<HttpResponseMessage> policy)
    {
        _combinedPolicy = policy;
    }

    /// <summary>
    /// Sends a SOAP request and returns the deserialized response.
    /// </summary>
    /// <typeparam name=""TRequest"">The request type.</typeparam>
    /// <typeparam name=""TResponse"">The response type.</typeparam>
    /// <param name=""action"">The SOAP action.</param>
    /// <param name=""request"">The request object.</param>
    /// <param name=""cancellationToken"">A cancellation token.</param>
    /// <returns>The deserialized response.</returns>
    protected async Task<TResponse> SendSoapRequestAsync<TRequest, TResponse>(
        string action,
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        var soapEnvelope = CreateSoapEnvelope(request);
        var content = new StringContent(soapEnvelope, Encoding.UTF8, ""text/xml"");
        content.Headers.Add(""SOAPAction"", action);

        // Use combined Polly policy
        var response = await _combinedPolicy.ExecuteAsync(() =>
            _httpClient.PostAsync(_endpoint, content, cancellationToken));

        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        return DeserializeSoapResponse<TResponse>(responseContent);
    }

    /// <summary>
    /// Creates a SOAP envelope for the request.
    /// </summary>
    /// <typeparam name=""T"">The request type.</typeparam>
    /// <param name=""request"">The request object.</param>
    /// <returns>The SOAP envelope XML string.</returns>
    protected virtual string CreateSoapEnvelope<T>(T request)
    {
        var namespaces = new XmlSerializerNamespaces();
        namespaces.Add(""soap"", ""http://schemas.xmlsoap.org/soap/envelope/"");

        var serializer = new XmlSerializer(typeof(T));
        var settings = new XmlWriterSettings
        {
            Encoding = Encoding.UTF8,
            OmitXmlDeclaration = true,
            Indent = true
        };

        var soapEnvelope = new StringBuilder();
        soapEnvelope.AppendLine(@""<?xml version=""""1.0"""" encoding=""""utf-8""""?>"");
        soapEnvelope.AppendLine(@""<soap:Envelope xmlns:soap=""""http://schemas.xmlsoap.org/soap/envelope/"""" xmlns:xsi=""""http://www.w3.org/2001/XMLSchema-instance"""" xmlns:xsd=""""http://www.w3.org/2001/XMLSchema"""">"");
        soapEnvelope.AppendLine(@""  <soap:Body>"");

        using (var stringWriter = new StringWriter())
        using (var xmlWriter = XmlWriter.Create(stringWriter, settings))
        {
            serializer.Serialize(xmlWriter, request, namespaces);
            soapEnvelope.AppendLine(stringWriter.ToString());
        }

        soapEnvelope.AppendLine(@""  </soap:Body>"");
        soapEnvelope.AppendLine(@""</soap:Envelope>"");

        return soapEnvelope.ToString();
    }

    /// <summary>
    /// Deserializes the SOAP response.
    /// </summary>
    /// <typeparam name=""T"">The response type.</typeparam>
    /// <param name=""soapResponse"">The SOAP response XML string.</param>
    /// <returns>The deserialized response object.</returns>
    protected virtual T DeserializeSoapResponse<T>(string soapResponse)
    {
        var serializer = new XmlSerializer(typeof(T));

        using var stringReader = new StringReader(soapResponse);
        using var xmlReader = XmlReader.Create(stringReader);

        // Move to the Body element
        xmlReader.ReadToFollowing(""Body"", ""http://schemas.xmlsoap.org/soap/envelope/"");
        xmlReader.Read(); // Move to the first child of Body

        // Check for SOAP fault
        if (xmlReader.LocalName == ""Fault"" && xmlReader.NamespaceURI == ""http://schemas.xmlsoap.org/soap/envelope/"")
        {
            throw new InvalidOperationException(""SOAP Fault: "" + soapResponse);
        }

        return (T)serializer.Deserialize(xmlReader)!;
    }
}";

        // Write the file
        File.WriteAllText(Path.Combine(outputDirectory, "Client", "SoapClientBase.cs"), content);
    }

    private void GenerateServiceCollectionExtensions(string outputNamespace, string outputDirectory)
    {
        // Create XML documentation comments
        var classComment = SyntaxFactory.TriviaList(
            SyntaxFactory.Comment("/// <summary>"),
            SyntaxFactory.Comment("/// Extension methods for registering SOAP clients with dependency injection."),
            SyntaxFactory.Comment("/// </summary>")
        );

        var addSoapClientComment = SyntaxFactory.TriviaList(
            SyntaxFactory.Comment("/// <summary>"),
            SyntaxFactory.Comment("/// Adds a SOAP client to the service collection."),
            SyntaxFactory.Comment("/// </summary>"),
            SyntaxFactory.Comment("/// <typeparam name=\"TInterface\">The service interface type.</typeparam>"),
            SyntaxFactory.Comment("/// <typeparam name=\"TClient\">The client implementation type.</typeparam>"),
            SyntaxFactory.Comment("/// <param name=\"services\">The service collection.</param>"),
            SyntaxFactory.Comment("/// <param name=\"endpoint\">The SOAP service endpoint URL.</param>"),
            SyntaxFactory.Comment("/// <param name=\"configureClient\">Optional action to configure the HttpClient.</param>"),
            SyntaxFactory.Comment("/// <param name=\"configureBuilder\">Optional action to configure the HttpClientBuilder.</param>"),
            SyntaxFactory.Comment("/// <returns>The service collection.</returns>")
        );

        var addSoapClientWithPollyComment = SyntaxFactory.TriviaList(
            SyntaxFactory.Comment("/// <summary>"),
            SyntaxFactory.Comment("/// Adds a SOAP client to the service collection with Polly policies."),
            SyntaxFactory.Comment("/// </summary>"),
            SyntaxFactory.Comment("/// <typeparam name=\"TInterface\">The service interface type.</typeparam>"),
            SyntaxFactory.Comment("/// <typeparam name=\"TClient\">The client implementation type.</typeparam>"),
            SyntaxFactory.Comment("/// <param name=\"services\">The service collection.</param>"),
            SyntaxFactory.Comment("/// <param name=\"endpoint\">The SOAP service endpoint URL.</param>"),
            SyntaxFactory.Comment("/// <param name=\"configurePolly\">Optional action to configure the Polly policies.</param>"),
            SyntaxFactory.Comment("/// <param name=\"configureClient\">Optional action to configure the HttpClient.</param>"),
            SyntaxFactory.Comment("/// <returns>The service collection.</returns>")
        );

        // Create generic type parameters for the extension methods
        var tInterfaceParameter = TypeParameter("TInterface");
        var tClientParameter = TypeParameter("TClient");

        // Create the AddSoapClient method
        var addSoapClientMethod = MethodDeclaration(
                ParseTypeName("IServiceCollection"),
                Identifier("AddSoapClient")
            )
            .WithTypeParameterList(
                TypeParameterList(
                    SeparatedList(new[] {
                        tInterfaceParameter,
                        tClientParameter
                    })
                )
            )
            .WithModifiers(
                TokenList(
                    Token(SyntaxKind.PublicKeyword),
                    Token(SyntaxKind.StaticKeyword)
                )
            )
            .WithParameterList(
                ParameterList(
                    SeparatedList(new[] {
                        Parameter(Identifier("services"))
                            .WithModifiers(TokenList(Token(SyntaxKind.ThisKeyword)))
                            .WithType(ParseTypeName("IServiceCollection")),
                        Parameter(Identifier("endpoint"))
                            .WithType(ParseTypeName("string")),
                        Parameter(Identifier("configureClient"))
                            .WithType(ParseTypeName("Action<HttpClient>?"))
                            .WithDefault(EqualsValueClause(LiteralExpression(SyntaxKind.NullLiteralExpression))),
                        Parameter(Identifier("configureBuilder"))
                            .WithType(ParseTypeName("Action<IHttpClientBuilder>?"))
                            .WithDefault(EqualsValueClause(LiteralExpression(SyntaxKind.NullLiteralExpression)))
                    })
                )
            )
            .WithConstraintClauses(
                List(new[] {
                    TypeParameterConstraintClause(
                        IdentifierName("TClient"),
                        SeparatedList<TypeParameterConstraintSyntax>(new TypeParameterConstraintSyntax[] {
                            ClassOrStructConstraint(SyntaxKind.ClassConstraint),
                            TypeConstraint(ParseTypeName("TInterface"))
                        })
                    ),
                    TypeParameterConstraintClause(
                        IdentifierName("TInterface"),
                        SeparatedList<TypeParameterConstraintSyntax>(new TypeParameterConstraintSyntax[] {
                            ClassOrStructConstraint(SyntaxKind.ClassConstraint)
                        })
                    )
                })
            )
            .WithBody(
                Block(
                    // Register the client options
                    ExpressionStatement(
                        InvocationExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName("services"),
                                IdentifierName("AddSingleton")
                            )
                        )
                        .WithArgumentList(
                            ArgumentList(
                                SingletonSeparatedList(
                                    Argument(
                                        ObjectCreationExpression(
                                            ParseTypeName("SoapClientOptions")
                                        )
                                        .WithInitializer(
                                            InitializerExpression(
                                                SyntaxKind.ObjectInitializerExpression,
                                                SingletonSeparatedList<ExpressionSyntax>(
                                                    AssignmentExpression(
                                                        SyntaxKind.SimpleAssignmentExpression,
                                                        IdentifierName("Endpoint"),
                                                        IdentifierName("endpoint")
                                                    )
                                                )
                                            )
                                        )
                                    )
                                )
                            )
                        )
                    ),

                    // Register the HTTP client
                    LocalDeclarationStatement(
                        VariableDeclaration(
                            ParseTypeName("var"),
                            SingletonSeparatedList(
                                VariableDeclarator(
                                    Identifier("builder")
                                )
                                .WithInitializer(
                                    EqualsValueClause(
                                        InvocationExpression(
                                            MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                IdentifierName("services"),
                                                GenericName(
                                                    Identifier("AddHttpClient")
                                                )
                                                .WithTypeArgumentList(
                                                    TypeArgumentList(
                                                        SeparatedList<TypeSyntax>(
                                                            new[] {
                                                                ParseTypeName("TInterface"),
                                                                ParseTypeName("TClient")
                                                            }
                                                        )
                                                    )
                                                )
                                            )
                                        )
                                        .WithArgumentList(
                                            ArgumentList(
                                                SingletonSeparatedList(
                                                    Argument(
                                                        ParenthesizedLambdaExpression()
                                                            .WithParameterList(
                                                                ParameterList(
                                                                    SeparatedList(
                                                                        new[] {
                                                                            Parameter(Identifier("serviceProvider")),
                                                                            Parameter(Identifier("client"))
                                                                        }
                                                                    )
                                                                )
                                                            )
                                                            .WithBody(
                                                                Block(
                                                                    // Configure base address if endpoint is a base URL
                                                                    IfStatement(
                                                                        InvocationExpression(
                                                                            MemberAccessExpression(
                                                                                SyntaxKind.SimpleMemberAccessExpression,
                                                                                ParseTypeName("Uri"),
                                                                                IdentifierName("TryCreate")
                                                                            )
                                                                        )
                                                                        .WithArgumentList(
                                                                            ArgumentList(
                                                                                SeparatedList(
                                                                                    new[] {
                                                                                        Argument(IdentifierName("endpoint")),
                                                                                        Argument(
                                                                                            MemberAccessExpression(
                                                                                                SyntaxKind.SimpleMemberAccessExpression,
                                                                                                ParseTypeName("UriKind"),
                                                                                                IdentifierName("Absolute")
                                                                                            )
                                                                                        ),
                                                                                        Argument(
                                                                                            DeclarationExpression(
                                                                                                ParseTypeName("var"),
                                                                                                SingleVariableDesignation(
                                                                                                    Identifier("uri")
                                                                                                )
                                                                                            )
                                                                                        )
                                                                                    }
                                                                                )
                                                                            )
                                                                        ),
                                                                        Block(
                                                                            ExpressionStatement(
                                                                                AssignmentExpression(
                                                                                    SyntaxKind.SimpleAssignmentExpression,
                                                                                    MemberAccessExpression(
                                                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                                                        IdentifierName("client"),
                                                                                        IdentifierName("BaseAddress")
                                                                                    ),
                                                                                    IdentifierName("uri")
                                                                                )
                                                                            )
                                                                        )
                                                                    ),

                                                                    // Apply additional configuration
                                                                    ExpressionStatement(
                                                                        ConditionalAccessExpression(
                                                                            IdentifierName("configureClient"),
                                                                            InvocationExpression(
                                                                                MemberBindingExpression(
                                                                                    IdentifierName("Invoke")
                                                                                )
                                                                            )
                                                                            .WithArgumentList(
                                                                                ArgumentList(
                                                                                    SingletonSeparatedList(
                                                                                        Argument(
                                                                                            IdentifierName("client")
                                                                                        )
                                                                                    )
                                                                                )
                                                                            )
                                                                        )
                                                                    )
                                                                )
                                                            )
                                                    )
                                                )
                                            )
                                        )
                                    )
                                )
                            )
                        )
                    ),

                    // Apply additional builder configuration
                    ExpressionStatement(
                        ConditionalAccessExpression(
                            IdentifierName("configureBuilder"),
                            InvocationExpression(
                                MemberBindingExpression(
                                    IdentifierName("Invoke")
                                )
                            )
                            .WithArgumentList(
                                ArgumentList(
                                    SingletonSeparatedList(
                                        Argument(
                                            IdentifierName("builder")
                                        )
                                    )
                                )
                            )
                        )
                    ),

                    // Return the service collection
                    ReturnStatement(
                        IdentifierName("services")
                    )
                )
            )
            .WithLeadingTrivia(addSoapClientComment);

        // Create the AddSoapClientWithPolly method
        var addSoapClientWithPollyMethod = MethodDeclaration(
                ParseTypeName("IServiceCollection"),
                Identifier("AddSoapClientWithPolly")
            )
            .WithTypeParameterList(
                TypeParameterList(
                    SeparatedList(new[] {
                        TypeParameter("TInterface"),
                        TypeParameter("TClient")
                    })
                )
            )
            .WithModifiers(
                TokenList(
                    Token(SyntaxKind.PublicKeyword),
                    Token(SyntaxKind.StaticKeyword)
                )
            )
            .WithParameterList(
                ParameterList(
                    SeparatedList(new[] {
                        Parameter(Identifier("services"))
                            .WithModifiers(TokenList(Token(SyntaxKind.ThisKeyword)))
                            .WithType(ParseTypeName("IServiceCollection")),
                        Parameter(Identifier("endpoint"))
                            .WithType(ParseTypeName("string")),
                        Parameter(Identifier("configurePolly"))
                            .WithType(ParseTypeName("Action<PollyPolicyOptions>?"))
                            .WithDefault(EqualsValueClause(LiteralExpression(SyntaxKind.NullLiteralExpression))),
                        Parameter(Identifier("configureClient"))
                            .WithType(ParseTypeName("Action<HttpClient>?"))
                            .WithDefault(EqualsValueClause(LiteralExpression(SyntaxKind.NullLiteralExpression)))
                    })
                )
            )
            .WithConstraintClauses(
                List(new[] {
                    TypeParameterConstraintClause(
                        IdentifierName("TClient"),
                        SeparatedList<TypeParameterConstraintSyntax>(new TypeParameterConstraintSyntax[] {
                            ClassOrStructConstraint(SyntaxKind.ClassConstraint),
                            TypeConstraint(ParseTypeName("TInterface"))
                        })
                    ),
                    TypeParameterConstraintClause(
                        IdentifierName("TInterface"),
                        SeparatedList<TypeParameterConstraintSyntax>(new TypeParameterConstraintSyntax[] {
                            ClassOrStructConstraint(SyntaxKind.ClassConstraint)
                        })
                    )
                })
            )
            .WithBody(
                Block(
                    // Default policy options
                    LocalDeclarationStatement(
                        VariableDeclaration(
                            ParseTypeName("var"),
                            SingletonSeparatedList(
                                VariableDeclarator(
                                    Identifier("policyOptions")
                                )
                                .WithInitializer(
                                    EqualsValueClause(
                                        ObjectCreationExpression(
                                            ParseTypeName("PollyPolicyOptions")
                                        )
                                        .WithInitializer(
                                            InitializerExpression(
                                                SyntaxKind.ObjectInitializerExpression,
                                                SeparatedList<ExpressionSyntax>(
                                                    new[] {
                                                        AssignmentExpression(
                                                            SyntaxKind.SimpleAssignmentExpression,
                                                            IdentifierName("RetryCount"),
                                                            LiteralExpression(
                                                                SyntaxKind.NumericLiteralExpression,
                                                                Literal(3)
                                                            )
                                                        ),
                                                        AssignmentExpression(
                                                            SyntaxKind.SimpleAssignmentExpression,
                                                            IdentifierName("TimeoutSeconds"),
                                                            LiteralExpression(
                                                                SyntaxKind.NumericLiteralExpression,
                                                                Literal(30)
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
                    ),

                    // Apply custom configuration
                    ExpressionStatement(
                        ConditionalAccessExpression(
                            IdentifierName("configurePolly"),
                            InvocationExpression(
                                MemberBindingExpression(
                                    IdentifierName("Invoke")
                                )
                            )
                            .WithArgumentList(
                                ArgumentList(
                                    SingletonSeparatedList(
                                        Argument(
                                            IdentifierName("policyOptions")
                                        )
                                    )
                                )
                            )
                        )
                    ),

                    // Return services.AddSoapClient<TInterface, TClient>
                    ReturnStatement(
                        InvocationExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName("services"),
                                GenericName(
                                    Identifier("AddSoapClient")
                                )
                                .WithTypeArgumentList(
                                    TypeArgumentList(
                                        SeparatedList<TypeSyntax>(
                                            new[] {
                                                ParseTypeName("TInterface"),
                                                ParseTypeName("TClient")
                                            }
                                        )
                                    )
                                )
                            )
                        )
                        .WithArgumentList(
                            ArgumentList(
                                SeparatedList(
                                    new[] {
                                        Argument(IdentifierName("endpoint")),
                                        Argument(IdentifierName("configureClient")),
                                        Argument(
                                            SimpleLambdaExpression(
                                                Parameter(Identifier("builder")),
                                                Block(
                                                    // Add timeout policy
                                                    ExpressionStatement(
                                                        InvocationExpression(
                                                            MemberAccessExpression(
                                                                SyntaxKind.SimpleMemberAccessExpression,
                                                                IdentifierName("builder"),
                                                                IdentifierName("AddPolicyHandler")
                                                            )
                                                        )
                                                        .WithArgumentList(
                                                            ArgumentList(
                                                                SingletonSeparatedList(
                                                                    Argument(
                                                                        InvocationExpression(
                                                                            MemberAccessExpression(
                                                                                SyntaxKind.SimpleMemberAccessExpression,
                                                                                ParseTypeName("Policy"),
                                                                                GenericName(
                                                                                    Identifier("TimeoutAsync")
                                                                                )
                                                                                .WithTypeArgumentList(
                                                                                    TypeArgumentList(
                                                                                        SingletonSeparatedList<TypeSyntax>(
                                                                                            ParseTypeName("HttpResponseMessage")
                                                                                        )
                                                                                    )
                                                                                )
                                                                            )
                                                                        )
                                                                        .WithArgumentList(
                                                                            ArgumentList(
                                                                                SingletonSeparatedList(
                                                                                    Argument(
                                                                                        InvocationExpression(
                                                                                            MemberAccessExpression(
                                                                                                SyntaxKind.SimpleMemberAccessExpression,
                                                                                                ParseTypeName("TimeSpan"),
                                                                                                IdentifierName("FromSeconds")
                                                                                            )
                                                                                        )
                                                                                        .WithArgumentList(
                                                                                            ArgumentList(
                                                                                                SingletonSeparatedList(
                                                                                                    Argument(
                                                                                                        MemberAccessExpression(
                                                                                                            SyntaxKind.SimpleMemberAccessExpression,
                                                                                                            IdentifierName("policyOptions"),
                                                                                                            IdentifierName("TimeoutSeconds")
                                                                                                        )
                                                                                                    )
                                                                                                )
                                                                                            )
                                                                                        )
                                                                                    )
                                                                                )
                                                                            )
                                                                        )
                                                                    )
                                                                )
                                                            )
                                                        )
                                                    ),

                                                    // Add retry policy
                                                    ExpressionStatement(
                                                        InvocationExpression(
                                                            MemberAccessExpression(
                                                                SyntaxKind.SimpleMemberAccessExpression,
                                                                IdentifierName("builder"),
                                                                IdentifierName("AddTransientHttpErrorPolicy")
                                                            )
                                                        )
                                                        .WithArgumentList(
                                                            ArgumentList(
                                                                SingletonSeparatedList(
                                                                    Argument(
                                                                        SimpleLambdaExpression(
                                                                            Parameter(Identifier("policy")),
                                                                            InvocationExpression(
                                                                                MemberAccessExpression(
                                                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                                                    IdentifierName("policy"),
                                                                                    IdentifierName("WaitAndRetryAsync")
                                                                                )
                                                                            )
                                                                            .WithArgumentList(
                                                                                ArgumentList(
                                                                                    SeparatedList(
                                                                                        new[] {
                                                                                            Argument(
                                                                                                MemberAccessExpression(
                                                                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                                                                    IdentifierName("policyOptions"),
                                                                                                    IdentifierName("RetryCount")
                                                                                                )
                                                                                            ),
                                                                                            Argument(
                                                                                                SimpleLambdaExpression(
                                                                                                    Parameter(Identifier("retryAttempt")),
                                                                                                    InvocationExpression(
                                                                                                        MemberAccessExpression(
                                                                                                            SyntaxKind.SimpleMemberAccessExpression,
                                                                                                            ParseTypeName("TimeSpan"),
                                                                                                            IdentifierName("FromSeconds")
                                                                                                        )
                                                                                                    )
                                                                                                    .WithArgumentList(
                                                                                                        ArgumentList(
                                                                                                            SingletonSeparatedList(
                                                                                                                Argument(
                                                                                                                    InvocationExpression(
                                                                                                                        MemberAccessExpression(
                                                                                                                            SyntaxKind.SimpleMemberAccessExpression,
                                                                                                                            ParseTypeName("Math"),
                                                                                                                            IdentifierName("Pow")
                                                                                                                        )
                                                                                                                    )
                                                                                                                    .WithArgumentList(
                                                                                                                        ArgumentList(
                                                                                                                            SeparatedList(
                                                                                                                                new[] {
                                                                                                                                    Argument(IdentifierName("retryAttempt")),
                                                                                                                                    Argument(
                                                                                                                                        LiteralExpression(
                                                                                                                                            SyntaxKind.NumericLiteralExpression,
                                                                                                                                            Literal(2)
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
                                                        )
                                                    )
                                                )
                                            )
                                        )
                                    }
                                )
                            )
                        )
                    )
                )
            )
            .WithLeadingTrivia(addSoapClientWithPollyComment);

        // Create the ServiceCollectionExtensions class
        var serviceCollectionExtensionsClass = ClassDeclaration("ServiceCollectionExtensions")
            .WithModifiers(
                TokenList(
                    Token(SyntaxKind.PublicKeyword),
                    Token(SyntaxKind.StaticKeyword)
                )
            )
            .WithMembers(
                List<MemberDeclarationSyntax>(
                    new[] {
                        addSoapClientMethod,
                        addSoapClientWithPollyMethod
                    }
                )
            )
            .WithLeadingTrivia(classComment);

        // Create the compilation unit
        var compilationUnit = CompilationUnit()
            .AddUsings(
                UsingDirective(ParseName("Microsoft.Extensions.DependencyInjection")),
                UsingDirective(ParseName("Microsoft.Extensions.Http")),
                UsingDirective(ParseName("Polly")),
                UsingDirective(ParseName("Polly.Extensions.Http")),
                UsingDirective(ParseName("System")),
                UsingDirective(ParseName("System.Net.Http"))
            )
            .AddMembers(
                NamespaceDeclaration(
                    ParseName($"{outputNamespace}.Extensions")
                )
                .WithUsings(
                    SingletonList(
                        UsingDirective(
                            ParseName($"{outputNamespace}.Client")
                        )
                    )
                )
                .AddMembers(serviceCollectionExtensionsClass)
            );

        // Format the code
        var code = compilationUnit
            .NormalizeWhitespace()
            .ToFullString();

        // Write the file
        File.WriteAllText(Path.Combine(outputDirectory, "Extensions", "ServiceCollectionExtensions.cs"), code);
    }

    private void GenerateClientOptions(string outputNamespace, string outputDirectory)
    {
        // Create XML documentation comments
        var soapClientOptionsComment = SyntaxFactory.TriviaList(
            SyntaxFactory.Comment("/// <summary>"),
            SyntaxFactory.Comment("/// Options for configuring a SOAP client."),
            SyntaxFactory.Comment("/// </summary>")
        );

        var endpointPropertyComment = SyntaxFactory.TriviaList(
            SyntaxFactory.Comment("/// <summary>"),
            SyntaxFactory.Comment("/// Gets or sets the SOAP service endpoint URL."),
            SyntaxFactory.Comment("/// </summary>")
        );

        var pollyPolicyOptionsComment = SyntaxFactory.TriviaList(
            SyntaxFactory.Comment("/// <summary>"),
            SyntaxFactory.Comment("/// Options for configuring Polly policies."),
            SyntaxFactory.Comment("/// </summary>")
        );

        var retryCountPropertyComment = SyntaxFactory.TriviaList(
            SyntaxFactory.Comment("/// <summary>"),
            SyntaxFactory.Comment("/// Gets or sets the number of retry attempts."),
            SyntaxFactory.Comment("/// </summary>")
        );

        var timeoutSecondsPropertyComment = SyntaxFactory.TriviaList(
            SyntaxFactory.Comment("/// <summary>"),
            SyntaxFactory.Comment("/// Gets or sets the timeout in seconds."),
            SyntaxFactory.Comment("/// </summary>")
        );

        // Create the endpoint property
        var endpointProperty = PropertyDeclaration(
                ParseTypeName("string"),
                Identifier("Endpoint")
            )
            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
            .WithAccessorList(
                AccessorList(
                    List(new[] {
                        AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                        AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                    })
                )
            )
            .WithInitializer(
                EqualsValueClause(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        ParseTypeName("string"),
                        IdentifierName("Empty")
                    )
                )
            )
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
            .WithLeadingTrivia(endpointPropertyComment);

        // Create the RetryCount property
        var retryCountProperty = PropertyDeclaration(
                ParseTypeName("int"),
                Identifier("RetryCount")
            )
            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
            .WithAccessorList(
                AccessorList(
                    List(new[] {
                        AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                        AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                    })
                )
            )
            .WithInitializer(
                EqualsValueClause(
                    LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        Literal(3)
                    )
                )
            )
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
            .WithLeadingTrivia(retryCountPropertyComment);

        // Create the TimeoutSeconds property
        var timeoutSecondsProperty = PropertyDeclaration(
                ParseTypeName("int"),
                Identifier("TimeoutSeconds")
            )
            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
            .WithAccessorList(
                AccessorList(
                    List(new[] {
                        AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                        AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                    })
                )
            )
            .WithInitializer(
                EqualsValueClause(
                    LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        Literal(30)
                    )
                )
            )
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
            .WithLeadingTrivia(timeoutSecondsPropertyComment);

        // Create the SoapClientOptions class
        var soapClientOptionsClass = ClassDeclaration("SoapClientOptions")
            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
            .WithLeadingTrivia(soapClientOptionsComment)
            .WithMembers(List<MemberDeclarationSyntax>(new[] { endpointProperty }));

        // Create the PollyPolicyOptions class
        var pollyPolicyOptionsClass = ClassDeclaration("PollyPolicyOptions")
            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
            .WithLeadingTrivia(pollyPolicyOptionsComment)
            .WithMembers(List<MemberDeclarationSyntax>(new[] { retryCountProperty, timeoutSecondsProperty }));

        // Create the compilation unit
        var compilationUnit = CompilationUnit()
            .AddUsings(UsingDirective(ParseName("System")))
            .AddMembers(
                NamespaceDeclaration(ParseName($"{outputNamespace}.Client"))
                    .AddMembers(soapClientOptionsClass, pollyPolicyOptionsClass)
            );

        // Format the code
        var code = compilationUnit
            .NormalizeWhitespace()
            .ToFullString();

        // Write the file
        File.WriteAllText(Path.Combine(outputDirectory, "Client", "SoapClientOptions.cs"), code);
    }

    private void GenerateModels(WsdlDefinition wsdlDefinition, string outputNamespace, string outputDirectory)
    {
        // Generate enums
        var enums = new List<EnumDeclarationSyntax>();
        foreach (var simpleType in wsdlDefinition.Types.SimpleTypes.Where(st => st.IsEnum))
        {
            enums.Add((EnumDeclarationSyntax)_generator.GenerateEnum(simpleType));
        }

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
                    var requestClassName = $"{operation.Name}Request";
                    var requestClassMembers = new List<MemberDeclarationSyntax>();

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
                            paramType = _generator.MapXsdTypeToClrType(part.Type);
                        }
                        else
                        {
                            paramType = "object";
                        }

                        // Create the property
                        var property = PropertyDeclaration(
                            ParseTypeName(paramType),
                            Identifier(_generator.ToPascalCase(part.Name))
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

                // Generate response class
                if (outputMessage != null && outputMessage.Parts.Count > 0)
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
                            paramType = element != null ? element.Type : "object";
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
                        var property = PropertyDeclaration(
                            ParseTypeName(paramType),
                            Identifier(_generator.ToPascalCase(part.Name))
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
        }
    }

    private void GenerateInterfaces(WsdlDefinition wsdlDefinition, string outputNamespace, string outputDirectory)
    {
        foreach (var portType in wsdlDefinition.PortTypes)
        {
            // Create the interface
            var interfaceDeclaration = InterfaceDeclaration($"I{portType.Name}")
                .AddModifiers(Token(SyntaxKind.PublicKeyword));

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
                    Identifier($"{operation.Name}Async")
                )
                .AddParameterListParameters(parameters.ToArray());

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

    private void GenerateClients(WsdlDefinition wsdlDefinition, string outputNamespace, string outputDirectory)
    {
        foreach (var service in wsdlDefinition.Services)
        {
            // Find the port type for this service
            var port = service.Ports.FirstOrDefault();
            if (port == null)
            {
                continue;
            }

            var binding = wsdlDefinition.Bindings.FirstOrDefault(b => b.Name == port.Binding);
            if (binding == null)
            {
                continue;
            }

            var portType = wsdlDefinition.PortTypes.FirstOrDefault(pt => pt.Name == binding.Type);
            if (portType == null)
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

                // Find the SOAP action from the binding
                string soapAction = string.Empty;
                foreach (var b in wsdlDefinition.Bindings)
                {
                    var bindingOperation = b.Operations.FirstOrDefault(bo => bo.Name == operation.Name);
                    if (bindingOperation != null && !string.IsNullOrEmpty(bindingOperation.SoapAction))
                    {
                        soapAction = bindingOperation.SoapAction;
                        break;
                    }
                }

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
                .ToFullString();

            // Write the file
            File.WriteAllText(Path.Combine(outputDirectory, "Client", $"{service.Name}Client.cs"), code);
        }
    }
}
