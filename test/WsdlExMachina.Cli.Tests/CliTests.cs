using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace WsdlExMachina.Cli.Tests;

public class CliTests
{
    [Fact]
    public async Task Main_WithNoArgs_ReturnsNonZeroExitCode()
    {
        // Act
        var result = await Program.Main(new string[] { });

        // Assert
        Assert.NotEqual(0, result);
    }

    [Fact]
    public async Task Main_WithHelpArg_ReturnsZeroExitCode()
    {
        // Act
        var result = await Program.Main(new string[] { "--help" });

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task Main_WithParseCommandAndNoFile_ReturnsNonZeroExitCode()
    {
        // Act
        var result = await Program.Main(new string[] { "parse" });

        // Assert
        Assert.NotEqual(0, result);
    }

    [Fact]
    public async Task Main_WithGenerateCommandAndNoFile_ReturnsNonZeroExitCode()
    {
        // Act
        var result = await Program.Main(new string[] { "generate" });

        // Assert
        Assert.NotEqual(0, result);
    }

    [Fact]
    public async Task Main_WithGenerateCommandAndMultiFileButNoOutputDir_ReturnsNonZeroExitCode()
    {
        // Arrange
        var filePath = Path.Combine("..", "..", "..", "..", "..", "samples", "ACH.wsdl");

        // Act
        var result = await Program.Main(new string[] {
            "generate",
            "--file", filePath,
            "--namespace", "TestNamespace",
            "--multi-file"
        });

        // Assert
        Assert.NotEqual(0, result);
    }

    [Fact]
    public async Task Main_WithGenerateCommandAndHttpClient_ReturnsZeroExitCode()
    {
        // Arrange
        var filePath = Path.Combine("..", "..", "..", "..", "..", "samples", "ACH.wsdl");
        var outputPath = Path.Combine(Path.GetTempPath(), "TestClient.cs");

        // Act
        var result = await Program.Main(new string[] {
            "generate",
            "--file", filePath,
            "--namespace", "TestNamespace",
            "--output", outputPath,
            "--http-client", "true"
        });

        // Assert
        Assert.Equal(0, result);
        Assert.True(File.Exists(outputPath));

        // Cleanup
        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
        }
    }
}
