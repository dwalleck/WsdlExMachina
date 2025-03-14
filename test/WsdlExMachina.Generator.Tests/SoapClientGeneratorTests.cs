using System.IO;
using Xunit;
using WsdlExMachina.Generator;

namespace WsdlExMachina.Generator.Tests;

public class SoapClientGeneratorTests
{
    [Fact]
    public void GenerateFromFile_ValidWsdl_GeneratesCode()
    {
        // Arrange
        var generator = new SoapClientGenerator();
        var filePath = Path.Combine("..", "..", "..", "..", "..", "samples", "ACH.wsdl");
        var outputNamespace = "TestNamespace";

        // Act
        var code = generator.GenerateFromFile(filePath, outputNamespace);

        // Assert
        Assert.NotNull(code);
        Assert.NotEmpty(code);
        Assert.Contains("namespace TestNamespace", code);
        Assert.Contains("public class ACHTransactionClient", code);
        Assert.Contains("public interface IACHTransactionSoap", code);
    }

    [Fact]
    public void Generate_ContainsRequiredUsings()
    {
        // Arrange
        var generator = new SoapClientGenerator();
        var filePath = Path.Combine("..", "..", "..", "..", "..", "samples", "ACH.wsdl");
        var outputNamespace = "TestNamespace";

        // Act
        var code = generator.GenerateFromFile(filePath, outputNamespace);

        // Assert
        Assert.Contains("using System;", code);
        Assert.Contains("using System.Collections.Generic;", code);
        Assert.Contains("using System.Threading.Tasks;", code);
        Assert.Contains("using System.ServiceModel;", code);
    }

    [Fact]
    public void Generate_ContainsDataContracts()
    {
        // Arrange
        var generator = new SoapClientGenerator();
        var filePath = Path.Combine("..", "..", "..", "..", "..", "samples", "ACH.wsdl");
        var outputNamespace = "TestNamespace";

        // Act
        var code = generator.GenerateFromFile(filePath, outputNamespace);

        // Assert
        Assert.Contains("[DataContract", code);
        Assert.Contains("[DataMember", code);
        Assert.Contains("public class ACHTransResponse", code);
    }

    [Fact]
    public void Generate_ContainsServiceContracts()
    {
        // Arrange
        var generator = new SoapClientGenerator();
        var filePath = Path.Combine("..", "..", "..", "..", "..", "samples", "ACH.wsdl");
        var outputNamespace = "TestNamespace";

        // Act
        var code = generator.GenerateFromFile(filePath, outputNamespace);

        // Assert
        Assert.Contains("[ServiceContract", code);
        Assert.Contains("[OperationContract", code);
        Assert.Contains("public interface IACHTransactionSoap", code);
    }

    [Fact]
    public void Generate_ContainsClientClass()
    {
        // Arrange
        var generator = new SoapClientGenerator();
        var filePath = Path.Combine("..", "..", "..", "..", "..", "samples", "ACH.wsdl");
        var outputNamespace = "TestNamespace";

        // Act
        var code = generator.GenerateFromFile(filePath, outputNamespace);

        // Assert
        Assert.Contains("public class ACHTransactionClient : ClientBase<IACHTransactionSoap>, IACHTransactionSoap", code);
        Assert.Contains("public ACHTransactionClient()", code);
        Assert.Contains("public ACHTransactionClient(string endpointConfigurationName)", code);
        Assert.Contains("public ACHTransactionClient(string endpointConfigurationName, string remoteAddress)", code);
        Assert.Contains("public ACHTransactionClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress)", code);
    }
}
