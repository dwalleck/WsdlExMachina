using System;
using System.IO;
using System.Linq;
using Xunit;
using WsdlExMachina.Generator.Generators;
using WsdlExMachina.Parser.Models;

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
                            if (portType.Operations.Any())
                            {
                                var operation = portType.Operations.First();
                                Assert.Contains($"public {(operation.Output != null ? $"Task<{operation.Name}Response>" : "Task")} {operation.Name}Async", fileContent);
                            }
                        }
                    }
                }
            }
        }

        [Fact]
        public void Generate_HandlesAllOperations()
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
                            // Count the number of operations in the WSDL
                            int operationCount = portType.Operations.Count;

                            // Count the number of Async methods in the generated code
                            int asyncMethodCount = 0;
                            int index = 0;
                            while ((index = fileContent.IndexOf("Async(", index + 1)) != -1)
                            {
                                asyncMethodCount++;
                            }

                            // The number of Async methods should match the number of operations
                            Assert.Equal(operationCount, asyncMethodCount);
                        }
                    }
                }
            }
        }

        [Fact]
        public void Generate_HandlesOperationsWithSOR()
        {
            // Act
            _generator.Generate(WsdlDefinition, OutputNamespace, OutputDir);

            // Assert
            foreach (var service in WsdlDefinition.Services)
            {
                var filePath = Path.Combine(OutputDir, "Client", $"{service.Name}Client.cs");
                var fileContent = File.ReadAllText(filePath);

                // Check for the specific SOR operation
                Assert.Contains("PostSinglePaymentWithAchWebValidationWithACHWebValidationSORAsync", fileContent);
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

        [Fact]
        public void Generate_ClientMethodsUseCorrectResponseTypes()
        {
            // Arrange
            // Add two operations with the same name but different input/output messages
            var portType = new WsdlPortType
            {
                Name = "TestPortType"
            };

            // First operation: PostSinglePayment
            var operation1 = new WsdlOperation
            {
                Name = "PostSinglePayment",
                Input = new WsdlOperationMessage { Message = "PostSinglePaymentSoapIn" },
                Output = new WsdlOperationMessage { Message = "PostSinglePaymentSoapOut" }
            };

            // Second operation: PostSinglePaymentWithApplyTo
            var operation2 = new WsdlOperation
            {
                Name = "PostSinglePaymentWithApplyTo",
                Input = new WsdlOperationMessage { Message = "PostSinglePaymentWithApplyToSoapIn" },
                Output = new WsdlOperationMessage { Message = "PostSinglePaymentWithApplyToSoapOut" }
            };

            // Add the operations to the port type
            portType.Operations.Add(operation1);
            portType.Operations.Add(operation2);

            // Add the port type to the WSDL definition if it doesn't exist
            if (!WsdlDefinition.PortTypes.Any(pt => pt.Name == portType.Name))
            {
                WsdlDefinition.PortTypes.Add(portType);
            }

            // Add the input and output messages
            var inputMessage1 = new WsdlMessage
            {
                Name = "PostSinglePaymentSoapIn"
            };
            inputMessage1.Parts.Add(new WsdlMessagePart { Name = "parameters", Element = "PostSinglePayment" });

            var outputMessage1 = new WsdlMessage
            {
                Name = "PostSinglePaymentSoapOut"
            };
            outputMessage1.Parts.Add(new WsdlMessagePart { Name = "parameters", Element = "PostSinglePaymentResponse" });

            var inputMessage2 = new WsdlMessage
            {
                Name = "PostSinglePaymentWithApplyToSoapIn"
            };
            inputMessage2.Parts.Add(new WsdlMessagePart { Name = "parameters", Element = "PostSinglePaymentWithApplyTo" });

            var outputMessage2 = new WsdlMessage
            {
                Name = "PostSinglePaymentWithApplyToSoapOut"
            };
            outputMessage2.Parts.Add(new WsdlMessagePart { Name = "parameters", Element = "PostSinglePaymentWithApplyToResponse" });

            // Add the messages to the WSDL definition if they don't exist
            if (!WsdlDefinition.Messages.Any(m => m.Name == inputMessage1.Name))
            {
                WsdlDefinition.Messages.Add(inputMessage1);
            }
            if (!WsdlDefinition.Messages.Any(m => m.Name == outputMessage1.Name))
            {
                WsdlDefinition.Messages.Add(outputMessage1);
            }
            if (!WsdlDefinition.Messages.Any(m => m.Name == inputMessage2.Name))
            {
                WsdlDefinition.Messages.Add(inputMessage2);
            }
            if (!WsdlDefinition.Messages.Any(m => m.Name == outputMessage2.Name))
            {
                WsdlDefinition.Messages.Add(outputMessage2);
            }

            // Add the elements to the types
            var element1 = new WsdlElement
            {
                Name = "PostSinglePayment",
                Type = "PostSinglePaymentType"
            };
            var element2 = new WsdlElement
            {
                Name = "PostSinglePaymentResponse",
                Type = "PostSinglePaymentResponseType"
            };
            var element3 = new WsdlElement
            {
                Name = "PostSinglePaymentWithApplyTo",
                Type = "PostSinglePaymentWithApplyToType"
            };
            var element4 = new WsdlElement
            {
                Name = "PostSinglePaymentWithApplyToResponse",
                Type = "PostSinglePaymentWithApplyToResponseType"
            };

            if (!WsdlDefinition.Types.Elements.Any(e => e.Name == element1.Name))
            {
                WsdlDefinition.Types.Elements.Add(element1);
            }
            if (!WsdlDefinition.Types.Elements.Any(e => e.Name == element2.Name))
            {
                WsdlDefinition.Types.Elements.Add(element2);
            }
            if (!WsdlDefinition.Types.Elements.Any(e => e.Name == element3.Name))
            {
                WsdlDefinition.Types.Elements.Add(element3);
            }
            if (!WsdlDefinition.Types.Elements.Any(e => e.Name == element4.Name))
            {
                WsdlDefinition.Types.Elements.Add(element4);
            }

            // Add a binding for the port type
            var binding = new WsdlBinding
            {
                Name = "TestBinding",
                Type = portType.Name
            };

            // Add operations to the binding
            var bindingOperation1 = new WsdlBindingOperation
            {
                Name = operation1.Name,
                SoapAction = $"http://example.com/{operation1.Name}",
                Input = new WsdlBindingOperationMessage(),
                Output = new WsdlBindingOperationMessage()
            };

            var bindingOperation2 = new WsdlBindingOperation
            {
                Name = operation2.Name,
                SoapAction = $"http://example.com/{operation2.Name}",
                Input = new WsdlBindingOperationMessage(),
                Output = new WsdlBindingOperationMessage()
            };

            binding.Operations.Add(bindingOperation1);
            binding.Operations.Add(bindingOperation2);

            if (!WsdlDefinition.Bindings.Any(b => b.Name == binding.Name))
            {
                WsdlDefinition.Bindings.Add(binding);
            }

            // Add a service with a port that uses the binding
            var service = new WsdlService
            {
                Name = "TestService"
            };

            var port = new WsdlPort
            {
                Name = "TestPort",
                Binding = binding.Name,
                Location = "http://example.com/service"
            };

            service.Ports.Add(port);

            if (!WsdlDefinition.Services.Any(s => s.Name == service.Name))
            {
                WsdlDefinition.Services.Add(service);
            }

            // Act
            _generator.Generate(WsdlDefinition, OutputNamespace, OutputDir);

            // Assert
            var filePath = Path.Combine(OutputDir, "Client", $"{service.Name}Client.cs");
            var fileContent = File.ReadAllText(filePath);

            // Check that the client methods use the correct response types
            Assert.Contains("Task<PostSinglePaymentResponse> PostSinglePaymentAsync", fileContent);
            Assert.Contains("Task<PostSinglePaymentWithApplyToResponse> PostSinglePaymentWithApplyToAsync", fileContent);

            // Check that the SendSoapRequestAsync calls use the correct request and response types
            Assert.Contains("SendSoapRequestAsync<PostSinglePaymentRequest, PostSinglePaymentResponse>", fileContent);
            Assert.Contains("SendSoapRequestAsync<PostSinglePaymentWithApplyToRequest, PostSinglePaymentWithApplyToResponse>", fileContent);
        }

        public void Dispose()
        {
            CleanupOutputDirectory();
        }
    }
}
