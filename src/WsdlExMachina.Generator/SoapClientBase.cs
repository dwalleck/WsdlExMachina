using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;

namespace WsdlExMachina.Generator;

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
    /// Initializes a new instance of the <see cref="SoapClientBase"/> class.
    /// </summary>
    /// <param name="endpoint">The SOAP service endpoint URL.</param>
    /// <param name="httpClient">Optional HttpClient instance. If not provided, a new instance will be created.</param>
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
    /// <param name="retryPolicy">The retry policy to use.</param>
    public void ConfigureRetryPolicy(IAsyncPolicy<HttpResponseMessage> retryPolicy)
    {
        _retryPolicy = retryPolicy;
        _combinedPolicy = Policy.WrapAsync(_retryPolicy, _timeoutPolicy);
    }

    /// <summary>
    /// Configures the timeout policy.
    /// </summary>
    /// <param name="timeoutPolicy">The timeout policy to use.</param>
    public void ConfigureTimeoutPolicy(IAsyncPolicy<HttpResponseMessage> timeoutPolicy)
    {
        _timeoutPolicy = timeoutPolicy;
        _combinedPolicy = Policy.WrapAsync(_retryPolicy, _timeoutPolicy);
    }

    /// <summary>
    /// Configures a custom policy that will replace the combined policy.
    /// </summary>
    /// <param name="policy">The policy to use.</param>
    public void ConfigurePolicy(IAsyncPolicy<HttpResponseMessage> policy)
    {
        _combinedPolicy = policy;
    }

    /// <summary>
    /// Sends a SOAP request and returns the deserialized response.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="action">The SOAP action.</param>
    /// <param name="request">The request object.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The deserialized response.</returns>
    protected async Task<TResponse> SendSoapRequestAsync<TRequest, TResponse>(
        string action,
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        var soapEnvelope = CreateSoapEnvelope(request);
        var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
        content.Headers.Add("SOAPAction", action);

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
    /// <typeparam name="T">The request type.</typeparam>
    /// <param name="request">The request object.</param>
    /// <returns>The SOAP envelope XML string.</returns>
    protected virtual string CreateSoapEnvelope<T>(T request)
    {
        var namespaces = new XmlSerializerNamespaces();
        namespaces.Add("soap", "http://schemas.xmlsoap.org/soap/envelope/");

        var serializer = new XmlSerializer(typeof(T));
        var settings = new XmlWriterSettings
        {
            Encoding = Encoding.UTF8,
            OmitXmlDeclaration = true,
            Indent = true
        };

        var soapEnvelope = new StringBuilder();
        soapEnvelope.AppendLine(@"<?xml version=""1.0"" encoding=""utf-8""?>");
        soapEnvelope.AppendLine(@"<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">");
        soapEnvelope.AppendLine(@"  <soap:Body>");

        using (var stringWriter = new StringWriter())
        using (var xmlWriter = XmlWriter.Create(stringWriter, settings))
        {
            serializer.Serialize(xmlWriter, request, namespaces);
            soapEnvelope.AppendLine(stringWriter.ToString());
        }

        soapEnvelope.AppendLine(@"  </soap:Body>");
        soapEnvelope.AppendLine(@"</soap:Envelope>");

        return soapEnvelope.ToString();
    }

    /// <summary>
    /// Deserializes the SOAP response.
    /// </summary>
    /// <typeparam name="T">The response type.</typeparam>
    /// <param name="soapResponse">The SOAP response XML string.</param>
    /// <returns>The deserialized response object.</returns>
    protected virtual T DeserializeSoapResponse<T>(string soapResponse)
    {
        var serializer = new XmlSerializer(typeof(T));

        using var stringReader = new StringReader(soapResponse);
        using var xmlReader = XmlReader.Create(stringReader);

        // Move to the Body element
        xmlReader.ReadToFollowing("Body", "http://schemas.xmlsoap.org/soap/envelope/");
        xmlReader.Read(); // Move to the first child of Body

        // Check for SOAP fault
        if (xmlReader.LocalName == "Fault" && xmlReader.NamespaceURI == "http://schemas.xmlsoap.org/soap/envelope/")
        {
            throw new InvalidOperationException("SOAP Fault: " + soapResponse);
        }

        return (T)serializer.Deserialize(xmlReader)!;
    }
}
