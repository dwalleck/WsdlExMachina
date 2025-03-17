using System.IO;
using Xunit;
using WsdlExMachina.Generator.Generators;

namespace WsdlExMachina.Generator.Tests.Generators
{
    public class SoapClientBaseGeneratorTests : GeneratorTestBase, IDisposable
    {
        private readonly SoapClientBaseGenerator _generator;
        private readonly DirectoryStructureGenerator _directoryGenerator;

        public SoapClientBaseGeneratorTests()
        {
            _generator = new SoapClientBaseGenerator();
            _directoryGenerator = new DirectoryStructureGenerator();

            // Create the required directory structure
            _directoryGenerator.Generate(WsdlDefinition, OutputNamespace, OutputDir);
        }

        [Fact]
        public void Generate_CreatesSoapClientBaseFile()
        {
            // Act
            _generator.Generate(WsdlDefinition, OutputNamespace, OutputDir);

            // Assert
            var filePath = Path.Combine(OutputDir, "Client", "SoapClientBase.cs");
            Assert.True(File.Exists(filePath), "SoapClientBase.cs file should exist");
        }

        [Fact]
        public void Generate_SoapClientBaseFileContainsCorrectNamespace()
        {
            // Act
            _generator.Generate(WsdlDefinition, OutputNamespace, OutputDir);

            // Assert
            var filePath = Path.Combine(OutputDir, "Client", "SoapClientBase.cs");
            var fileContent = File.ReadAllText(filePath);
            Assert.Contains($"namespace {OutputNamespace}.Client", fileContent);
        }

        [Fact]
        public void Generate_SoapClientBaseFileContainsAbstractClass()
        {
            // Act
            _generator.Generate(WsdlDefinition, OutputNamespace, OutputDir);

            // Assert
            var filePath = Path.Combine(OutputDir, "Client", "SoapClientBase.cs");
            var fileContent = File.ReadAllText(filePath);
            Assert.Contains("public abstract class SoapClientBase", fileContent);
        }

        [Fact]
        public void Generate_SoapClientBaseFileContainsConstructor()
        {
            // Act
            _generator.Generate(WsdlDefinition, OutputNamespace, OutputDir);

            // Assert
            var filePath = Path.Combine(OutputDir, "Client", "SoapClientBase.cs");
            var fileContent = File.ReadAllText(filePath);
            Assert.Contains("protected SoapClientBase(string endpoint, HttpClient httpClient)", fileContent);
        }

        [Fact]
        public void Generate_SoapClientBaseFileContainsSendSoapRequestAsyncMethod()
        {
            // Act
            _generator.Generate(WsdlDefinition, OutputNamespace, OutputDir);

            // Assert
            var filePath = Path.Combine(OutputDir, "Client", "SoapClientBase.cs");
            var fileContent = File.ReadAllText(filePath);
            Assert.Contains("protected async Task<TResponse> SendSoapRequestAsync<TRequest, TResponse>", fileContent);
        }

        [Fact]
        public void Generate_SoapClientBaseFileContainsRequiredUsings()
        {
            // Act
            _generator.Generate(WsdlDefinition, OutputNamespace, OutputDir);

            // Assert
            var filePath = Path.Combine(OutputDir, "Client", "SoapClientBase.cs");
            var fileContent = File.ReadAllText(filePath);

            // Check for required usings
            Assert.Contains("using System;", fileContent);
            Assert.Contains("using System.IO;", fileContent);
            Assert.Contains("using System.Net.Http;", fileContent);
            Assert.Contains("using System.Text;", fileContent);
            Assert.Contains("using System.Threading;", fileContent);
            Assert.Contains("using System.Threading.Tasks;", fileContent);
            Assert.Contains("using System.Xml;", fileContent);
            Assert.Contains("using System.Xml.Serialization;", fileContent);
        }

        public void Dispose()
        {
            CleanupOutputDirectory();
        }
    }
}
