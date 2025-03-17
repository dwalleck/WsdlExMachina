using System.IO;
using Xunit;
using WsdlExMachina.Generator.Generators;

namespace WsdlExMachina.Generator.Tests.Generators
{
    public class ClientOptionsGeneratorTests : GeneratorTestBase, IDisposable
    {
        private readonly ClientOptionsGenerator _generator;
        private readonly DirectoryStructureGenerator _directoryGenerator;

        public ClientOptionsGeneratorTests()
        {
            _generator = new ClientOptionsGenerator();
            _directoryGenerator = new DirectoryStructureGenerator();

            // Create the required directory structure
            _directoryGenerator.Generate(WsdlDefinition, OutputNamespace, OutputDir);
        }

        [Fact]
        public void Generate_CreatesOptionsFile()
        {
            // Act
            _generator.Generate(WsdlDefinition, OutputNamespace, OutputDir);

            // Assert
            var optionsFilePath = Path.Combine(OutputDir, "Client", "SoapClientOptions.cs");
            Assert.True(File.Exists(optionsFilePath), "SoapClientOptions.cs file should exist");
        }

        [Fact]
        public void Generate_OptionsFileContainsCorrectNamespace()
        {
            // Act
            _generator.Generate(WsdlDefinition, OutputNamespace, OutputDir);

            // Assert
            var optionsFilePath = Path.Combine(OutputDir, "Client", "SoapClientOptions.cs");
            var optionsFileContent = File.ReadAllText(optionsFilePath);
            Assert.Contains($"namespace {OutputNamespace}.Client", optionsFileContent);
        }

        [Fact]
        public void Generate_OptionsFileContainsEndpointProperty()
        {
            // Act
            _generator.Generate(WsdlDefinition, OutputNamespace, OutputDir);

            // Assert
            var optionsFilePath = Path.Combine(OutputDir, "Client", "SoapClientOptions.cs");
            var optionsFileContent = File.ReadAllText(optionsFilePath);

            // Check for the Endpoint property
            Assert.Contains("public string Endpoint { get; set; }", optionsFileContent);
        }

        [Fact]
        public void Generate_OptionsFileContainsPublicClass()
        {
            // Act
            _generator.Generate(WsdlDefinition, OutputNamespace, OutputDir);

            // Assert
            var optionsFilePath = Path.Combine(OutputDir, "Client", "SoapClientOptions.cs");
            var optionsFileContent = File.ReadAllText(optionsFilePath);

            // Check for the public class declaration
            Assert.Contains("public class SoapClientOptions", optionsFileContent);
        }

        public void Dispose()
        {
            CleanupOutputDirectory();
        }
    }
}
