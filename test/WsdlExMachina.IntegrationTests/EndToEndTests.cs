using System.IO;
using System.Text;
using System.Threading.Tasks;
using WsdlExMachina.Generator;
using WsdlExMachina.Parser;
using Xunit;

namespace WsdlExMachina.IntegrationTests;

public class EndToEndTests
{
    [Fact]
    public void EndToEnd_ParseWsdlAndGenerateClient_Success()
    {
        // Arrange
        var wsdlFilePath = Path.Combine("..", "..", "..", "..", "..", "samples", "ACH.wsdl");
        var outputNamespace = "WsdlExMachina.Generated";
        var outputPath = Path.Combine(Path.GetTempPath(), "ACHTransactionClient.cs");

        // Act - Parse WSDL
        var parser = new WsdlParser();
        var wsdlDefinition = parser.ParseFile(wsdlFilePath);

        // Assert - Verify WSDL parsing
        Assert.NotNull(wsdlDefinition);
        Assert.Equal("http://www.swbc.com/", wsdlDefinition.TargetNamespace);
        Assert.NotEmpty(wsdlDefinition.Services);
        Assert.NotEmpty(wsdlDefinition.PortTypes);
        Assert.NotEmpty(wsdlDefinition.Bindings);
        Assert.NotEmpty(wsdlDefinition.Messages);
        Assert.NotEmpty(wsdlDefinition.Types.ComplexTypes);

        // Act - Generate client
        var generator = new SoapClientGenerator();
        var code = generator.Generate(wsdlDefinition, outputNamespace);
        File.WriteAllText(outputPath, code);

        // Assert - Verify client generation
        Assert.NotNull(code);
        Assert.NotEmpty(code);
        Assert.Contains("namespace WsdlExMachina.Generated", code);
        Assert.Contains("public class ACHTransactionClient", code);
        Assert.Contains("public interface IACHTransactionSoap", code);
        Assert.True(File.Exists(outputPath));

        // Cleanup
        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task EndToEnd_CliWorkflow_Success()
    {
        // Arrange
        var wsdlFilePath = Path.Combine("..", "..", "..", "..", "..", "samples", "ACH.wsdl");
        var outputNamespace = "WsdlExMachina.Generated";
        var outputPath = Path.Combine(Path.GetTempPath(), "ACHTransactionClient.cs");

        // Act - Parse WSDL using CLI
        var parseResult = await WsdlExMachina.Cli.Program.Main(new string[] { "parse", "--file", wsdlFilePath });

        // Assert - Verify parse command
        Assert.Equal(0, parseResult);

        // Act - Generate client using CLI
        var generateResult = await WsdlExMachina.Cli.Program.Main(new string[] {
            "generate",
            "--file", wsdlFilePath,
            "--namespace", outputNamespace,
            "--output", outputPath
        });

        // Assert - Verify generate command
        Assert.Equal(0, generateResult);
        Assert.True(File.Exists(outputPath));

        // Verify generated code
        var code = File.ReadAllText(outputPath);
        Assert.NotNull(code);
        Assert.NotEmpty(code);
        Assert.Contains("namespace WsdlExMachina.Generated", code);
        Assert.Contains("public class ACHTransactionClient", code);
        Assert.Contains("public interface IACHTransactionSoap", code);

        // Cleanup
        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
        }
    }
}
