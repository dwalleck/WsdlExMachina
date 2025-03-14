using System.IO;
using Xunit;
using WsdlExMachina.Parser;
using WsdlExMachina.Parser.Models;

namespace WsdlExMachina.Parser.Tests;

public class WsdlParserTests
{
    [Fact]
    public void ParseFile_ValidWsdl_ReturnsWsdlDefinition()
    {
        // Arrange
        var parser = new WsdlParser();
        var filePath = Path.Combine("..", "..", "..", "..", "..", "samples", "ACH.wsdl");

        // Act
        var wsdlDefinition = parser.ParseFile(filePath);

        // Assert
        Assert.NotNull(wsdlDefinition);
        Assert.Equal("http://www.swbc.com/", wsdlDefinition.TargetNamespace);

        // Verify services
        Assert.Single(wsdlDefinition.Services);
        Assert.Equal("ACHTransaction", wsdlDefinition.Services[0].Name);
        Assert.Equal(2, wsdlDefinition.Services[0].Ports.Count);
        Assert.Equal("ACHTransactionSoap", wsdlDefinition.Services[0].Ports[0].Name);
        Assert.Equal("ACHTransactionSoap12", wsdlDefinition.Services[0].Ports[1].Name);

        // Verify port types
        Assert.Single(wsdlDefinition.PortTypes);
        Assert.Equal("ACHTransactionSoap", wsdlDefinition.PortTypes[0].Name);
        Assert.True(wsdlDefinition.PortTypes[0].Operations.Count > 0);

        // Verify bindings
        Assert.Equal(2, wsdlDefinition.Bindings.Count);
        Assert.Equal("ACHTransactionSoap", wsdlDefinition.Bindings[0].Name);
        Assert.Equal("ACHTransactionSoap12", wsdlDefinition.Bindings[1].Name);

        // Verify messages
        Assert.True(wsdlDefinition.Messages.Count > 0);

        // Verify types
        Assert.NotNull(wsdlDefinition.Types);
        Assert.True(wsdlDefinition.Types.Schemas.Count > 0);

        // Verify namespaces
        Assert.True(wsdlDefinition.Namespaces.Count > 0);
        Assert.True(wsdlDefinition.Namespaces.ContainsKey("s"));
        Assert.Equal("http://www.w3.org/2001/XMLSchema", wsdlDefinition.Namespaces["s"]);
    }

    [Fact]
    public void ParseFile_ValidWsdl_ParsesComplexTypes()
    {
        // Arrange
        var parser = new WsdlParser();
        var filePath = Path.Combine("..", "..", "..", "..", "..", "samples", "ACH.wsdl");

        // Act
        var wsdlDefinition = parser.ParseFile(filePath);

        // Assert
        Assert.NotNull(wsdlDefinition.Types);

        // Verify complex types
        var complexTypes = wsdlDefinition.Types.ComplexTypes;
        Assert.True(complexTypes.Count > 0);

        // Check for specific complex type
        var achTransResponseType = complexTypes.Find(ct => ct.Name == "ACHTransResponse");
        Assert.NotNull(achTransResponseType);
        Assert.Equal(3, achTransResponseType.Elements.Count);

        // Verify elements in complex type
        var responseCodeElement = achTransResponseType.Elements.Find(e => e.Name == "ResponseCode");
        Assert.NotNull(responseCodeElement);
        Assert.Equal("string", responseCodeElement.Type);
        Assert.True(responseCodeElement.IsOptional);
    }

    [Fact]
    public void ParseFile_ValidWsdl_ParsesSimpleTypes()
    {
        // Arrange
        var parser = new WsdlParser();
        var filePath = Path.Combine("..", "..", "..", "..", "..", "samples", "ACH.wsdl");

        // Act
        var wsdlDefinition = parser.ParseFile(filePath);

        // Assert
        Assert.NotNull(wsdlDefinition.Types);

        // Verify simple types
        var simpleTypes = wsdlDefinition.Types.SimpleTypes;
        Assert.True(simpleTypes.Count > 0);

        // Check for specific simple type
        var paymentSourceType = simpleTypes.Find(st => st.Name == "PaymentSource");
        Assert.NotNull(paymentSourceType);
        Assert.Equal("string", paymentSourceType.BaseType);
        Assert.True(paymentSourceType.IsEnum);
        Assert.True(paymentSourceType.EnumerationValues.Count > 0);
    }

    [Fact]
    public void ParseFile_ValidWsdl_ParsesOperations()
    {
        // Arrange
        var parser = new WsdlParser();
        var filePath = Path.Combine("..", "..", "..", "..", "..", "samples", "ACH.wsdl");

        // Act
        var wsdlDefinition = parser.ParseFile(filePath);

        // Assert
        Assert.NotNull(wsdlDefinition.PortTypes);
        Assert.Single(wsdlDefinition.PortTypes);

        // Verify operations
        var operations = wsdlDefinition.PortTypes[0].Operations;
        Assert.True(operations.Count > 0);

        // Check for specific operation
        var postSinglePaymentOperation = operations.Find(op => op.Name == "PostSinglePayment");
        Assert.NotNull(postSinglePaymentOperation);
        Assert.NotNull(postSinglePaymentOperation.Input);
        Assert.NotNull(postSinglePaymentOperation.Output);
    }
}
