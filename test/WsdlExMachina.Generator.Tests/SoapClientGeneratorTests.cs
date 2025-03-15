using System.IO;
using Xunit;
using WsdlExMachina.Generator;
using WsdlExMachina.Parser;

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
    }

    [Fact]
    public void MultiFileGenerator_CreatesFiles()
    {
        // Arrange
        var generator = new SoapClientGenerator();
        var multiFileGenerator = new MultiFileGenerator(generator);
        var filePath = Path.Combine("..", "..", "..", "..", "..", "samples", "ACH.wsdl");
        var outputNamespace = "TestNamespace";
        var outputDir = Path.Combine(Path.GetTempPath(), "WsdlExMachina_Test_" + Guid.NewGuid());
        Directory.CreateDirectory(outputDir);

        try
        {
            // Act
            var parser = new WsdlParser();
            var wsdl = parser.ParseFile(filePath);
            multiFileGenerator.Generate(wsdl, outputNamespace, outputDir);

            // Assert
            Assert.True(Directory.Exists(Path.Combine(outputDir, "Models")));
            Assert.True(Directory.Exists(Path.Combine(outputDir, "Client")));
            Assert.True(Directory.Exists(Path.Combine(outputDir, "Interfaces")));
            Assert.True(Directory.Exists(Path.Combine(outputDir, "Extensions")));

            // Check for specific files
            Assert.True(File.Exists(Path.Combine(outputDir, "Client", "SoapClientBase.cs")));
            Assert.True(File.Exists(Path.Combine(outputDir, "Client", "SoapClientOptions.cs")));
            Assert.True(File.Exists(Path.Combine(outputDir, "Extensions", "ServiceCollectionExtensions.cs")));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, true);
            }
        }
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
        Assert.Contains("public class ACHTransactionClient", code);
        Assert.Contains("public ACHTransactionClient(", code);
    }
}
