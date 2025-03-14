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
}
