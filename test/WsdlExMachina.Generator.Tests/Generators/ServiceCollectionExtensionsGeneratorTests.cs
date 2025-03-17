using System.IO;
using Xunit;
using WsdlExMachina.Generator.Generators;

namespace WsdlExMachina.Generator.Tests.Generators
{
    public class ServiceCollectionExtensionsGeneratorTests : GeneratorTestBase, IDisposable
    {
        private readonly ServiceCollectionExtensionsGenerator _generator;
        private readonly DirectoryStructureGenerator _directoryGenerator;

        public ServiceCollectionExtensionsGeneratorTests()
        {
            _generator = new ServiceCollectionExtensionsGenerator();
            _directoryGenerator = new DirectoryStructureGenerator();

            // Create the required directory structure
            _directoryGenerator.Generate(WsdlDefinition, OutputNamespace, OutputDir);
        }

        [Fact]
        public void Generate_CreatesServiceCollectionExtensionsFile()
        {
            // Act
            _generator.Generate(WsdlDefinition, OutputNamespace, OutputDir);

            // Assert
            var filePath = Path.Combine(OutputDir, "Extensions", "ServiceCollectionExtensions.cs");
            Assert.True(File.Exists(filePath), "ServiceCollectionExtensions.cs file should exist");
        }

        [Fact]
        public void Generate_ServiceCollectionExtensionsFileContainsCorrectNamespace()
        {
            // Act
            _generator.Generate(WsdlDefinition, OutputNamespace, OutputDir);

            // Assert
            var filePath = Path.Combine(OutputDir, "Extensions", "ServiceCollectionExtensions.cs");
            var fileContent = File.ReadAllText(filePath);
            Assert.Contains($"namespace {OutputNamespace}.Extensions", fileContent);
        }

        [Fact]
        public void Generate_ServiceCollectionExtensionsFileContainsStaticClass()
        {
            // Act
            _generator.Generate(WsdlDefinition, OutputNamespace, OutputDir);

            // Assert
            var filePath = Path.Combine(OutputDir, "Extensions", "ServiceCollectionExtensions.cs");
            var fileContent = File.ReadAllText(filePath);
            Assert.Contains("public static class ServiceCollectionExtensions", fileContent);
        }

        [Fact]
        public void Generate_ServiceCollectionExtensionsFileContainsAddSoapClientMethod()
        {
            // Act
            _generator.Generate(WsdlDefinition, OutputNamespace, OutputDir);

            // Assert
            var filePath = Path.Combine(OutputDir, "Extensions", "ServiceCollectionExtensions.cs");
            var fileContent = File.ReadAllText(filePath);
            Assert.Contains("public static IServiceCollection AddSoapClient<TInterface, TClient>", fileContent);
        }

        [Fact]
        public void Generate_ServiceCollectionExtensionsFileContainsAddSoapClientWithPollyMethod()
        {
            // Act
            _generator.Generate(WsdlDefinition, OutputNamespace, OutputDir);

            // Assert
            var filePath = Path.Combine(OutputDir, "Extensions", "ServiceCollectionExtensions.cs");
            var fileContent = File.ReadAllText(filePath);
            Assert.Contains("public static IServiceCollection AddSoapClientWithPolly<TInterface, TClient>", fileContent);
        }

        [Fact]
        public void Generate_ServiceCollectionExtensionsFileContainsPollyPolicyOptionsClass()
        {
            // Act
            _generator.Generate(WsdlDefinition, OutputNamespace, OutputDir);

            // Assert
            var filePath = Path.Combine(OutputDir, "Extensions", "ServiceCollectionExtensions.cs");
            var fileContent = File.ReadAllText(filePath);
            Assert.Contains("public class PollyPolicyOptions", fileContent);
            Assert.Contains("public int RetryCount { get; set; }", fileContent);
            Assert.Contains("public int TimeoutSeconds { get; set; }", fileContent);
        }

        [Fact]
        public void Generate_ServiceCollectionExtensionsFileContainsRequiredUsings()
        {
            // Act
            _generator.Generate(WsdlDefinition, OutputNamespace, OutputDir);

            // Assert
            var filePath = Path.Combine(OutputDir, "Extensions", "ServiceCollectionExtensions.cs");
            var fileContent = File.ReadAllText(filePath);

            // Check for required usings
            Assert.Contains("using Microsoft.Extensions.DependencyInjection;", fileContent);
            Assert.Contains("using Microsoft.Extensions.Http;", fileContent);
            Assert.Contains("using Polly;", fileContent);
            Assert.Contains("using Polly.Extensions.Http;", fileContent);
            Assert.Contains("using System;", fileContent);
            Assert.Contains("using System.Net.Http;", fileContent);
            Assert.Contains($"using {OutputNamespace}.Client;", fileContent);
        }

        public void Dispose()
        {
            CleanupOutputDirectory();
        }
    }
}
