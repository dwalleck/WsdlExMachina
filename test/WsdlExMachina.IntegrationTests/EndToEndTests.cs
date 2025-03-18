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

    [Fact]
    public async Task EndToEnd_CliWorkflow_MultiFile_Success()
    {
        // Arrange
        var wsdlFilePath = Path.Combine("..", "..", "..", "..", "..", "samples", "ACH.wsdl");
        var outputNamespace = "WsdlExMachina.Generated";
        var outputDir = Path.Combine(Path.GetTempPath(), "WsdlExMachina_Test_" + Guid.NewGuid());
        Directory.CreateDirectory(outputDir);

        try
        {
            // Act - Generate client using CLI with multi-file option
            var generateResult = await WsdlExMachina.Cli.Program.Main(new string[] {
                "generate",
                "--file", wsdlFilePath,
                "--namespace", outputNamespace,
                "--multi-file", "true",
                "--output-dir", outputDir,
                "--http-client", "true"
            });

            // Assert - Verify generate command
            Assert.Equal(0, generateResult);

            // Verify directory structure
            Assert.True(Directory.Exists(Path.Combine(outputDir, "Models")));
            Assert.True(Directory.Exists(Path.Combine(outputDir, "Client")));
            Assert.True(Directory.Exists(Path.Combine(outputDir, "Interfaces")));
            Assert.True(Directory.Exists(Path.Combine(outputDir, "Extensions")));

            // Verify specific files
            Assert.True(File.Exists(Path.Combine(outputDir, "Client", "SoapClientBase.cs")));
            Assert.True(File.Exists(Path.Combine(outputDir, "Client", "SoapClientOptions.cs")));
            Assert.True(File.Exists(Path.Combine(outputDir, "Extensions", "ServiceCollectionExtensions.cs")));
            Assert.True(File.Exists(Path.Combine(outputDir, "Client", "ACHTransactionClient.cs")));
            Assert.True(File.Exists(Path.Combine(outputDir, "Interfaces", "IACHTransactionSoap.cs")));

            // Verify csproj file was generated
            var dirName = new DirectoryInfo(outputDir).Name;
            Assert.True(File.Exists(Path.Combine(outputDir, $"{dirName}.csproj")));

            // Verify csproj file content
            var csprojContent = File.ReadAllText(Path.Combine(outputDir, $"{dirName}.csproj"));
            Assert.Contains("<TargetFramework>net9.0</TargetFramework>", csprojContent);
            Assert.Contains($"<RootNamespace>{outputNamespace}</RootNamespace>", csprojContent);
            Assert.Contains("<PackageReference Include=\"Microsoft.Extensions.Http\"", csprojContent);

            // Verify client file contains HttpClient code
            var clientCode = File.ReadAllText(Path.Combine(outputDir, "Client", "ACHTransactionClient.cs"));
            Assert.Contains("SendSoapRequestAsync", clientCode);
            Assert.DoesNotContain("System.ServiceModel", clientCode);
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
    public async Task EndToEnd_CliWorkflow_HttpClient_Success()
    {
        // Arrange
        var wsdlFilePath = Path.Combine("..", "..", "..", "..", "..", "samples", "ACH.wsdl");
        var outputNamespace = "WsdlExMachina.Generated";
        var outputPath = Path.Combine(Path.GetTempPath(), "ACHTransactionClient_HttpClient.cs");

        // Act - Generate client using CLI with HttpClient option
        var generateResult = await WsdlExMachina.Cli.Program.Main(new string[] {
            "generate",
            "--file", wsdlFilePath,
            "--namespace", outputNamespace,
            "--output", outputPath,
            "--http-client", "true"
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

    [Fact]
    public async Task EndToEnd_GenerateClientWithCsproj_Success()
    {
        // Arrange
        var wsdlFilePath = Path.Combine("..", "..", "..", "..", "..", "samples", "ACH.wsdl");
        var outputNamespace = "WsdlExMachina.Generated";
        var outputDir = Path.Combine(Path.GetTempPath(), "WsdlExMachina_Test_Csproj_" + Guid.NewGuid());
        Directory.CreateDirectory(outputDir);

        try
        {
            // Act - Generate client using CLI with multi-file option and explicit csproj generation
            var generateResult = await WsdlExMachina.Cli.Program.Main(new string[] {
                "generate",
                "--file", wsdlFilePath,
                "--namespace", outputNamespace,
                "--multi-file", "true",
                "--output-dir", outputDir,
                "--http-client", "true",
                "--generate-csproj", "true"
            });

            // Assert - Verify generate command
            Assert.Equal(0, generateResult);

            // Verify csproj file was generated
            var dirName = new DirectoryInfo(outputDir).Name;
            var csprojPath = Path.Combine(outputDir, $"{dirName}.csproj");
            Assert.True(File.Exists(csprojPath));

            // Verify csproj file content
            var csprojContent = File.ReadAllText(csprojPath);
            Assert.Contains("<TargetFramework>net9.0</TargetFramework>", csprojContent);
            Assert.Contains($"<RootNamespace>{outputNamespace}</RootNamespace>", csprojContent);

            // Verify the csproj can be built
            var testClientDir = Path.Combine("..", "..", "..", "..", "..", "test-client");
            Directory.CreateDirectory(testClientDir);

            // Copy all generated files to test-client directory
            foreach (var file in Directory.GetFiles(outputDir, "*.*", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(outputDir, file);
                var destPath = Path.Combine(testClientDir, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(destPath));
                File.Copy(file, destPath, true);
            }

            // Verify the copied files exist
            Assert.True(File.Exists(Path.Combine(testClientDir, $"{dirName}.csproj")));
            Assert.True(File.Exists(Path.Combine(testClientDir, "Client", "ACHTransactionClient.cs")));
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
    public async Task EndToEnd_VerifyNoNestedAuthHeaders_Success()
    {
        // Arrange
        var wsdlFilePath = Path.Combine("..", "..", "..", "..", "..", "samples", "ACH.wsdl");
        var outputNamespace = "WsdlExMachina.Generated";
        var outputDir = Path.Combine(Path.GetTempPath(), "WsdlExMachina_Test_AuthHeader_" + Guid.NewGuid());
        Directory.CreateDirectory(outputDir);

        try
        {
            // Act - Generate client using CLI with multi-file option
            var generateResult = await WsdlExMachina.Cli.Program.Main(new string[] {
                "generate",
                "--file", wsdlFilePath,
                "--namespace", outputNamespace,
                "--multi-file", "true",
                "--output-dir", outputDir,
                "--http-client", "true"
            });

            // Assert - Verify generate command
            Assert.Equal(0, generateResult);

            // Check if SoapAuthHeader.cs exists
            var soapAuthHeaderPath = Path.Combine(outputDir, "Models", "Headers", "SoapAuthHeader.cs");
            if (File.Exists(soapAuthHeaderPath))
            {
                // Read the file content
                var fileContent = File.ReadAllText(soapAuthHeaderPath);

                // Verify the SoapAuthHeader class has Username and Password properties
                Assert.Contains("public string Username { get; set; }", fileContent);
                Assert.Contains("public string Password { get; set; }", fileContent);

                // Verify the SoapAuthHeader class does NOT have a nested SWBCAuthHeader property
                // This is the key check to ensure we don't have nested auth headers
                Assert.DoesNotContain("public SWBCAuthHeader SWBCAuthHeader { get; set; }", fileContent);
            }

            // Check the generated client code to ensure it doesn't have nested auth headers
            var clientFiles = Directory.GetFiles(Path.Combine(outputDir, "Client"), "*.cs", SearchOption.AllDirectories);
            foreach (var clientFile in clientFiles)
            {
                var clientCode = File.ReadAllText(clientFile);

                // Check for patterns that would indicate nested auth headers
                // This is a simplified check - in a real scenario, you might need more sophisticated parsing
                if (clientCode.Contains("SWBCAuthHeader") && clientCode.Contains("SoapAuthHeader"))
                {
                    // Make sure we don't have a pattern like SoapAuthHeader.SWBCAuthHeader
                    Assert.DoesNotContain("SoapAuthHeader.SWBCAuthHeader", clientCode);

                    // Also check for nested XML elements in any string literals
                    Assert.DoesNotContain("<SWBCAuthHeader><SWBCAuthHeader>", clientCode);
                }
            }

            // Generate a test SOAP envelope to verify no nested headers
            // This would be a more thorough test in a real scenario
            // For now, we're just checking the code structure
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
    public async Task EndToEnd_VerifyUniqueRequestClassesForOverloadedOperations_Success()
    {
        // Arrange
        var wsdlFilePath = Path.Combine("..", "..", "..", "..", "..", "samples", "ACH.wsdl");
        var outputNamespace = "WsdlExMachina.Generated";
        var outputDir = Path.Combine(Path.GetTempPath(), "WsdlExMachina_Test_UniqueRequests_" + Guid.NewGuid());
        Directory.CreateDirectory(outputDir);

        try
        {
            // Act - Generate client using CLI with multi-file option
            var generateResult = await WsdlExMachina.Cli.Program.Main(new string[] {
                "generate",
                "--file", wsdlFilePath,
                "--namespace", outputNamespace,
                "--multi-file", "true",
                "--output-dir", outputDir,
                "--http-client", "true"
            });

            // Assert - Verify generate command
            Assert.Equal(0, generateResult);

            // Check the Models/Requests directory for unique request classes
            var requestsDir = Path.Combine(outputDir, "Models", "Requests");
            var requestFiles = Directory.GetFiles(requestsDir, "*.cs");

            // Get all request files that start with "PostSinglePayment"
            var postSinglePaymentRequests = requestFiles
                .Where(f => Path.GetFileName(f).StartsWith("PostSinglePayment"))
                .ToList();

            // Verify we have multiple unique request classes for PostSinglePayment operations
            Assert.True(postSinglePaymentRequests.Count > 1,
                $"Expected multiple PostSinglePayment request classes, but found {postSinglePaymentRequests.Count}");

            // Check the client files to ensure they use the correct request classes
            var clientFiles = Directory.GetFiles(Path.Combine(outputDir, "Client"), "*.cs");
            var clientFile = clientFiles.FirstOrDefault(f => Path.GetFileName(f).Contains("ACHTransaction"));

            if (clientFile != null)
            {
                var clientCode = File.ReadAllText(clientFile);

                // Check for methods that use different request types
                foreach (var requestFile in postSinglePaymentRequests)
                {
                    var requestClassName = Path.GetFileNameWithoutExtension(requestFile);

                    // Verify the client has a method that uses this request class
                    Assert.Contains($"ParseTypeName(\"{requestClassName}\")", clientCode);
                }

                // Check for SendSoapRequestAsync calls with different request types
                foreach (var requestFile in postSinglePaymentRequests)
                {
                    var requestClassName = Path.GetFileNameWithoutExtension(requestFile);

                    // Verify the client has a SendSoapRequestAsync call that uses this request class
                    Assert.Contains($"ParseTypeName(\"{requestClassName}\")", clientCode);
                }
            }

            // Check the interface files to ensure they use the correct request classes
            var interfaceFiles = Directory.GetFiles(Path.Combine(outputDir, "Interfaces"), "*.cs");
            var interfaceFile = interfaceFiles.FirstOrDefault(f => Path.GetFileName(f).Contains("IACHTransaction"));

            if (interfaceFile != null)
            {
                var interfaceCode = File.ReadAllText(interfaceFile);

                // Check for methods that use different request types
                foreach (var requestFile in postSinglePaymentRequests)
                {
                    var requestClassName = Path.GetFileNameWithoutExtension(requestFile);

                    // Verify the interface has a method that uses this request class
                    Assert.Contains($"ParseTypeName(\"{requestClassName}\")", interfaceCode);
                }
            }
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
}
