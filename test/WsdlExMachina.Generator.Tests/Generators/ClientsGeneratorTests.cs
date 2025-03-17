using System.IO;
using System.Linq;
using Xunit;
using WsdlExMachina.Generator.Generators;

namespace WsdlExMachina.Generator.Tests.Generators
{
    public class ClientsGeneratorTests : GeneratorTestBase, IDisposable
    {
        private readonly ClientsGenerator _generator;
        private readonly DirectoryStructureGenerator _directoryGenerator;

        public ClientsGeneratorTests()
        {
            _generator = new ClientsGenerator();
            _directoryGenerator = new DirectoryStructureGenerator();

            // Create the required directory structure
            _directoryGenerator.Generate(WsdlDefinition, OutputNamespace, OutputDir);
        }

        [Fact]
        public void Generate_CreatesClientFiles()
        {
            // Act
            _generator.Generate(WsdlDefinition, OutputNamespace, OutputDir);

            // Assert
            foreach (var service in WsdlDefinition.Services)
            {
                var filePath = Path.Combine(OutputDir, "Client", $"{service.Name}Client.cs");
                Assert.True(File.Exists(filePath), $"Client file {filePath} should exist");
            }
        }

        [Fact]
        public void Generate_ClientFilesContainCorrectNamespace()
        {
            // Act
            _generator.Generate(WsdlDefinition, OutputNamespace, OutputDir);

            // Assert
            foreach (var service in WsdlDefinition.Services)
            {
                var filePath = Path.Combine(OutputDir, "Client", $"{service.Name}Client.cs");
                var fileContent = File.ReadAllText(filePath);
                Assert.Contains($"namespace {OutputNamespace}.Client", fileContent);
            }
        }

        [Fact]
        public void Generate_ClientFilesContainPublicClass()
        {
            // Act
            _generator.Generate(WsdlDefinition, OutputNamespace, OutputDir);

            // Assert
            foreach (var service in WsdlDefinition.Services)
            {
                var filePath = Path.Combine(OutputDir, "Client", $"{service.Name}Client.cs");
                var fileContent = File.ReadAllText(filePath);
                Assert.Contains($"public class {service.Name}Client", fileContent);
            }
        }

        [Fact]
        public void Generate_ClientFilesInheritFromSoapClientBase()
        {
            // Act
            _generator.Generate(WsdlDefinition, OutputNamespace, OutputDir);

            // Assert
            foreach (var service in WsdlDefinition.Services)
            {
                var filePath = Path.Combine(OutputDir, "Client", $"{service.Name}Client.cs");
                var fileContent = File.ReadAllText(filePath);

                // Find the port type for this service
                var port = service.Ports.FirstOrDefault();
                if (port != null)
                {
                    var binding = WsdlDefinition.Bindings.FirstOrDefault(b => b.Name == port.Binding);
                    if (binding != null)
                    {
                        var portType = WsdlDefinition.PortTypes.FirstOrDefault(pt => pt.Name == binding.Type);
                        if (portType != null)
                        {
                            Assert.Contains($": SoapClientBase, I{portType.Name}", fileContent);
                        }
                    }
                }
            }
        }

        [Fact]
        public void Generate_ClientFilesContainConstructor()
        {
            // Act
            _generator.Generate(WsdlDefinition, OutputNamespace, OutputDir);

            // Assert
            foreach (var service in WsdlDefinition.Services)
            {
                var filePath = Path.Combine(OutputDir, "Client", $"{service.Name}Client.cs");
                var fileContent = File.ReadAllText(filePath);
                Assert.Contains($"public {service.Name}Client(SoapClientOptions options, HttpClient httpClient)", fileContent);
                Assert.Contains("base(options.Endpoint, httpClient)", fileContent);
            }
        }

        [Fact]
        public void Generate_ClientFilesContainAsyncMethods()
        {
            // Act
            _generator.Generate(WsdlDefinition, OutputNamespace, OutputDir);

            // Assert
            foreach (var service in WsdlDefinition.Services)
            {
                var filePath = Path.Combine(OutputDir, "Client", $"{service.Name}Client.cs");
                var fileContent = File.ReadAllText(filePath);

                // Find the port type for this service
                var port = service.Ports.FirstOrDefault();
                if (port != null)
                {
                    var binding = WsdlDefinition.Bindings.FirstOrDefault(b => b.Name == port.Binding);
                    if (binding != null)
                    {
                        var portType = WsdlDefinition.PortTypes.FirstOrDefault(pt => pt.Name == binding.Type);
                        if (portType != null)
                        {
                            // Check for at least one async method
                            var uniqueOperations = portType.Operations
                                .GroupBy(o => o.Name)
                                .Select(g => g.First())
                                .ToList();

                            if (uniqueOperations.Any())
                            {
                                var operation = uniqueOperations.First();
                                Assert.Contains($"public {(operation.Output != null ? $"Task<{operation.Name}Response>" : "Task")} {operation.Name}Async", fileContent);
                            }
                        }
                    }
                }
            }
        }

        [Fact]
        public void Generate_ClientFilesContainSendSoapRequestAsyncCalls()
        {
            // Act
            _generator.Generate(WsdlDefinition, OutputNamespace, OutputDir);

            // Assert
            foreach (var service in WsdlDefinition.Services)
            {
                var filePath = Path.Combine(OutputDir, "Client", $"{service.Name}Client.cs");
                var fileContent = File.ReadAllText(filePath);
                Assert.Contains("return SendSoapRequestAsync", fileContent);
            }
        }

        [Fact]
        public void Generate_ClientFilesContainRequiredUsings()
        {
            // Act
            _generator.Generate(WsdlDefinition, OutputNamespace, OutputDir);

            // Assert
            foreach (var service in WsdlDefinition.Services)
            {
                var filePath = Path.Combine(OutputDir, "Client", $"{service.Name}Client.cs");
                var fileContent = File.ReadAllText(filePath);

                // Check for required usings
                Assert.Contains("using System;", fileContent);
                Assert.Contains("using System.Net.Http;", fileContent);
                Assert.Contains("using System.Threading;", fileContent);
                Assert.Contains("using System.Threading.Tasks;", fileContent);
                Assert.Contains($"using {OutputNamespace}.Interfaces;", fileContent);
                Assert.Contains($"using {OutputNamespace}.Models.Requests;", fileContent);
                Assert.Contains($"using {OutputNamespace}.Models.Responses;", fileContent);
            }
        }

        public void Dispose()
        {
            CleanupOutputDirectory();
        }
    }
}
