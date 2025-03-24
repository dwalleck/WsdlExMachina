using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace WsdlExMachina.CSharpGenerator.Tests;

/// <summary>
/// Integration tests for SOAP clients using WireMock.NET to mock SOAP services.
/// </summary>
public class WireMockIntegrationTests : IDisposable
{
    private readonly WireMockServer _mockServer;

    public WireMockIntegrationTests()
    {
        // Start WireMock server
        _mockServer = WireMockServer.Start();
    }

    public void Dispose()
    {
        // Stop WireMock server
        _mockServer.Stop();
    }

    #region Test Models

    // Request model
    [XmlRoot(ElementName = "CalculateRequest", Namespace = "http://tempuri.org/")]
    public class CalculateRequest
    {
        [XmlElement(ElementName = "Value1")]
        public int Value1 { get; set; }

        [XmlElement(ElementName = "Value2")]
        public int Value2 { get; set; }

        [XmlElement(ElementName = "Operation")]
        public string Operation { get; set; }
    }

    // Response model
    [XmlRoot(ElementName = "CalculateResponse", Namespace = "http://tempuri.org/")]
    public class CalculateResponse
    {
        [XmlElement(ElementName = "Result")]
        public double Result { get; set; }

        [XmlElement(ElementName = "Success")]
        public bool Success { get; set; }

        [XmlElement(ElementName = "ErrorMessage")]
        public string ErrorMessage { get; set; }
    }

    // SOAP fault model
    [XmlRoot(ElementName = "Fault", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
    public class SoapFault
    {
        [XmlElement(ElementName = "faultcode")]
        public string FaultCode { get; set; }

        [XmlElement(ElementName = "faultstring")]
        public string FaultString { get; set; }

        [XmlElement(ElementName = "detail")]
        public string Detail { get; set; }
    }

    #endregion

    #region Test Client

    // Test SOAP client implementation
    private class CalculatorClient : SoapClientBase
    {
        public CalculatorClient(string endpointUrl)
        {
            Initialize(endpointUrl);
        }

        public async Task<CalculateResponse> CalculateAsync(int value1, int value2, string operation)
        {
            var request = new CalculateRequest
            {
                Value1 = value1,
                Value2 = value2,
                Operation = operation
            };

            var soapAction = "http://tempuri.org/Calculate";
            var soapEnvelope = CreateSoapEnvelope(request, soapAction);
            var responseContent = await SendSoapRequestAsync(soapEnvelope, soapAction).ConfigureAwait(false);
            return DeserializeResponse<CalculateResponse>(responseContent);
        }
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task CalculateAsync_Addition_ShouldReturnCorrectResult()
    {
        // Arrange
        SetupMockCalculateEndpoint("add", 5, 3, 8);
        var client = new CalculatorClient(_mockServer.Urls[0]);

        // Act
        var response = await client.CalculateAsync(5, 3, "add").ConfigureAwait(false);

        // Assert
        Assert.True(response.Success);
        Assert.Equal(8, response.Result);
        Assert.Null(response.ErrorMessage);
    }

    [Fact]
    public async Task CalculateAsync_Subtraction_ShouldReturnCorrectResult()
    {
        // Arrange
        SetupMockCalculateEndpoint("subtract", 10, 4, 6);
        var client = new CalculatorClient(_mockServer.Urls[0]);

        // Act
        var response = await client.CalculateAsync(10, 4, "subtract").ConfigureAwait(false);

        // Assert
        Assert.True(response.Success);
        Assert.Equal(6, response.Result);
        Assert.Null(response.ErrorMessage);
    }

    [Fact]
    public async Task CalculateAsync_Multiplication_ShouldReturnCorrectResult()
    {
        // Arrange
        SetupMockCalculateEndpoint("multiply", 6, 7, 42);
        var client = new CalculatorClient(_mockServer.Urls[0]);

        // Act
        var response = await client.CalculateAsync(6, 7, "multiply").ConfigureAwait(false);

        // Assert
        Assert.True(response.Success);
        Assert.Equal(42, response.Result);
        Assert.Null(response.ErrorMessage);
    }

    [Fact]
    public async Task CalculateAsync_Division_ShouldReturnCorrectResult()
    {
        // Arrange
        SetupMockCalculateEndpoint("divide", 20, 4, 5);
        var client = new CalculatorClient(_mockServer.Urls[0]);

        // Act
        var response = await client.CalculateAsync(20, 4, "divide").ConfigureAwait(false);

        // Assert
        Assert.True(response.Success);
        Assert.Equal(5, response.Result);
        Assert.Null(response.ErrorMessage);
    }

    [Fact]
    public async Task CalculateAsync_DivisionByZero_ShouldReturnError()
    {
        // Arrange
        SetupMockCalculateErrorEndpoint("divide", 10, 0, "Division by zero is not allowed");
        var client = new CalculatorClient(_mockServer.Urls[0]);

        // Act
        var response = await client.CalculateAsync(10, 0, "divide").ConfigureAwait(false);

        // Assert
        Assert.False(response.Success);
        Assert.Equal(0, response.Result);
        Assert.Equal("Division by zero is not allowed", response.ErrorMessage);
    }

    [Fact]
    public async Task CalculateAsync_InvalidOperation_ShouldThrowSoapFault()
    {
        // Arrange
        SetupMockSoapFaultEndpoint("power", "Client", "Invalid operation: power");
        var client = new CalculatorClient(_mockServer.Urls[0]);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.CalculateAsync(2, 3, "power")).ConfigureAwait(false);

        Assert.Contains("500", exception.Message);
    }

    [Fact]
    public async Task CalculateAsync_WithAuthentication_ShouldSendCredentials()
    {
        // Arrange
        SetupMockAuthenticatedEndpoint("add", 5, 3, 8);
        var client = new CalculatorClient(_mockServer.Urls[0])
        {
            Username = "testuser",
            Password = "testpassword"
        };

        // Act
        var response = await client.CalculateAsync(5, 3, "add").ConfigureAwait(false);

        // Assert
        Assert.True(response.Success);
        Assert.Equal(8, response.Result);
    }

    #endregion

    #region Helper Methods

    private void SetupMockCalculateEndpoint(string operation, int value1, int value2, double result)
    {
        var responseXml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <soap:Body>
    <CalculateResponse xmlns=""http://tempuri.org/"">
      <Result>{result}</Result>
      <Success>true</Success>
    </CalculateResponse>
  </soap:Body>
</soap:Envelope>";

        _mockServer
            .Given(Request.Create()
                .WithPath("/")
                .WithHeader("SOAPAction", "http://tempuri.org/Calculate")
                .WithBody(body => body.Contains($"<Operation>{operation}</Operation>") &&
                                  body.Contains($"<Value1>{value1}</Value1>") &&
                                  body.Contains($"<Value2>{value2}</Value2>"))
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "text/xml; charset=utf-8")
                .WithBody(responseXml));
    }

    private void SetupMockCalculateErrorEndpoint(string operation, int value1, int value2, string errorMessage)
    {
        var responseXml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <soap:Body>
    <CalculateResponse xmlns=""http://tempuri.org/"">
      <Result>0</Result>
      <Success>false</Success>
      <ErrorMessage>{errorMessage}</ErrorMessage>
    </CalculateResponse>
  </soap:Body>
</soap:Envelope>";

        _mockServer
            .Given(Request.Create()
                .WithPath("/")
                .WithHeader("SOAPAction", "http://tempuri.org/Calculate")
                .WithBody(body => body.Contains($"<Operation>{operation}</Operation>") &&
                                  body.Contains($"<Value1>{value1}</Value1>") &&
                                  body.Contains($"<Value2>{value2}</Value2>"))
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "text/xml; charset=utf-8")
                .WithBody(responseXml));
    }

    private void SetupMockSoapFaultEndpoint(string operation, string faultCode, string faultString)
    {
        var responseXml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <soap:Body>
    <soap:Fault>
      <faultcode>soap:{faultCode}</faultcode>
      <faultstring>{faultString}</faultstring>
      <detail>
        <CalculateFault xmlns=""http://tempuri.org/"">
          <ErrorMessage>{faultString}</ErrorMessage>
        </CalculateFault>
      </detail>
    </soap:Fault>
  </soap:Body>
</soap:Envelope>";

        _mockServer
            .Given(Request.Create()
                .WithPath("/")
                .WithHeader("SOAPAction", "http://tempuri.org/Calculate")
                .WithBody(body => body.Contains($"<Operation>{operation}</Operation>"))
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.InternalServerError)
                .WithHeader("Content-Type", "text/xml; charset=utf-8")
                .WithBody(responseXml));
    }

    private void SetupMockAuthenticatedEndpoint(string operation, int value1, int value2, double result)
    {
        var responseXml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <soap:Body>
    <CalculateResponse xmlns=""http://tempuri.org/"">
      <Result>{result}</Result>
      <Success>true</Success>
    </CalculateResponse>
  </soap:Body>
</soap:Envelope>";

        _mockServer
            .Given(Request.Create()
                .WithPath("/")
                .WithHeader("SOAPAction", "http://tempuri.org/Calculate")
                .WithBody(body => body.Contains("<Username>testuser</Username>") &&
                                  body.Contains("<Password>testpassword</Password>") &&
                                  body.Contains($"<Operation>{operation}</Operation>") &&
                                  body.Contains($"<Value1>{value1}</Value1>") &&
                                  body.Contains($"<Value2>{value2}</Value2>"))
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "text/xml; charset=utf-8")
                .WithBody(responseXml));
    }

    #endregion
}
