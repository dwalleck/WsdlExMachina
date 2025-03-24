using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Polly;
using Polly.Extensions.Http;
using Polly.Retry;

namespace WsdlExMachina.CSharpGenerator
{
    /// <summary>
    /// Base class for SOAP clients.
    /// </summary>
    public abstract class SoapClientBase
    {
        private static readonly HttpClient _httpClient;
        private static readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;

        /// <summary>
        /// Static constructor to initialize the HttpClient with retry policies.
        /// </summary>
        static SoapClientBase()
        {
            // Create a retry policy for transient HTTP errors
            _retryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

            // Create an HttpClient with the retry policy
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// Gets or sets the endpoint URL of the SOAP service.
        /// </summary>
        protected string EndpointUrl { get; set; }

        /// <summary>
        /// Gets or sets the username for authentication.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets the password for authentication.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Initializes the SOAP client with the specified endpoint URL.
        /// </summary>
        /// <param name="endpointUrl">The endpoint URL of the SOAP service.</param>
        protected void Initialize(string endpointUrl)
        {
            EndpointUrl = endpointUrl ?? throw new ArgumentNullException(nameof(endpointUrl));
        }

        /// <summary>
        /// Creates a SOAP envelope for the specified request object.
        /// </summary>
        /// <param name="request">The request object.</param>
        /// <param name="soapAction">The SOAP action.</param>
        /// <returns>The SOAP envelope as a string.</returns>
        protected string CreateSoapEnvelope(object request, string soapAction)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            sb.AppendLine("<soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">");

            // Add SOAP header if authentication is provided
            if (!string.IsNullOrEmpty(Username) || !string.IsNullOrEmpty(Password))
            {
                sb.AppendLine("  <soap:Header>");
                sb.AppendLine("    <SWBCAuthHeader xmlns=\"http://www.swbc.com/\">");
                sb.AppendLine($"      <Username>{Username}</Username>");
                sb.AppendLine($"      <Password>{Password}</Password>");
                sb.AppendLine("    </SWBCAuthHeader>");
                sb.AppendLine("  </soap:Header>");
            }

            sb.AppendLine("  <soap:Body>");

            // Serialize the request object to XML
            using (var stringWriter = new StringWriter())
            {
                var serializer = new XmlSerializer(request.GetType());
                serializer.Serialize(stringWriter, request);

                // Extract the XML content (skip the XML declaration)
                var xml = stringWriter.ToString();
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xml);

                // Append the serialized request to the SOAP envelope
                sb.AppendLine(xmlDoc.DocumentElement.OuterXml);
            }

            sb.AppendLine("  </soap:Body>");
            sb.AppendLine("</soap:Envelope>");

            return sb.ToString();
        }

        /// <summary>
        /// Sends a SOAP request to the endpoint.
        /// </summary>
        /// <param name="soapEnvelope">The SOAP envelope.</param>
        /// <param name="soapAction">The SOAP action.</param>
        /// <returns>The response content as a string.</returns>
        protected async Task<string> SendSoapRequestAsync(string soapEnvelope, string soapAction)
        {
            if (string.IsNullOrEmpty(soapEnvelope))
                throw new ArgumentNullException(nameof(soapEnvelope));

            // Create the HTTP content
            var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");

            // Add the SOAP action header
            content.Headers.Add("SOAPAction", soapAction ?? string.Empty);

            // Send the request with retry policy
            var response = await _retryPolicy.ExecuteAsync(() => _httpClient.PostAsync(EndpointUrl, content));

            // Ensure success status code
            response.EnsureSuccessStatusCode();

            // Read the response content
            return await response.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// Deserializes the response content to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to deserialize to.</typeparam>
        /// <param name="responseContent">The response content.</param>
        /// <returns>The deserialized object.</returns>
        protected T DeserializeResponse<T>(string responseContent)
        {
            if (string.IsNullOrEmpty(responseContent))
                throw new ArgumentNullException(nameof(responseContent));

            // Extract the response from the SOAP envelope
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(responseContent);

            // Get the namespace manager
            var nsManager = new XmlNamespaceManager(xmlDoc.NameTable);
            nsManager.AddNamespace("soap", "http://schemas.xmlsoap.org/soap/envelope/");
            nsManager.AddNamespace("ns", "http://www.swbc.com/");

            // Find the response element in the SOAP body
            var responseNode = xmlDoc.SelectSingleNode("//soap:Body/*[1]", nsManager);
            if (responseNode == null)
                throw new InvalidOperationException("Response element not found in SOAP body.");

            // Deserialize the response
            using (var reader = new StringReader(responseNode.OuterXml))
            {
                var serializer = new XmlSerializer(typeof(T));
                return (T)serializer.Deserialize(reader);
            }
        }

        /// <summary>
        /// Deserializes the response content to the specified type.
        /// </summary>
        /// <param name="responseContent">The response content.</param>
        /// <param name="type">The type to deserialize to.</param>
        /// <returns>The deserialized object.</returns>
        protected object DeserializeResponse(string responseContent, Type type)
        {
            if (string.IsNullOrEmpty(responseContent))
                throw new ArgumentNullException(nameof(responseContent));

            if (type == null)
                throw new ArgumentNullException(nameof(type));

            // Extract the response from the SOAP envelope
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(responseContent);

            // Get the namespace manager
            var nsManager = new XmlNamespaceManager(xmlDoc.NameTable);
            nsManager.AddNamespace("soap", "http://schemas.xmlsoap.org/soap/envelope/");
            nsManager.AddNamespace("ns", "http://www.swbc.com/");

            // Find the response element in the SOAP body
            var responseNode = xmlDoc.SelectSingleNode("//soap:Body/*[1]", nsManager);
            if (responseNode == null)
                throw new InvalidOperationException("Response element not found in SOAP body.");

            // Deserialize the response
            using (var reader = new StringReader(responseNode.OuterXml))
            {
                var serializer = new XmlSerializer(type);
                return serializer.Deserialize(reader);
            }
        }
    }
}
