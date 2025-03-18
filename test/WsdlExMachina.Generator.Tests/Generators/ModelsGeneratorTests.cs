using System.IO;
using System.Linq;
using Xunit;
using WsdlExMachina.Generator.Generators;
using WsdlExMachina.Parser.Models;

namespace WsdlExMachina.Generator.Tests.Generators
{
    public class ModelsGeneratorTests : GeneratorTestBase, IDisposable
    {
        private readonly ModelsGenerator _generator;
        private readonly DirectoryStructureGenerator _directoryGenerator;
        private readonly SoapClientGenerator _soapClientGenerator;

        public ModelsGeneratorTests()
        {
            _soapClientGenerator = new SoapClientGenerator();
            _generator = new ModelsGenerator(_soapClientGenerator);
            _directoryGenerator = new DirectoryStructureGenerator();

            // Create the required directory structure
            _directoryGenerator.Generate(WsdlDefinition, OutputNamespace, OutputDir);
        }

        [Fact]
        public void Generate_CreatesEnumFiles()
        {
            // Act
            _generator.Generate(WsdlDefinition, OutputNamespace, OutputDir);

            // Assert
            if (WsdlDefinition.Types.SimpleTypes.Any(st => st.IsEnum))
            {
                var filePath = Path.Combine(OutputDir, "Models", "Common", "Enums.cs");
                Assert.True(File.Exists(filePath), "Enums.cs file should exist");
            }
        }

        [Fact]
        public void Generate_CreatesComplexTypeFiles()
        {
            // Act
            _generator.Generate(WsdlDefinition, OutputNamespace, OutputDir);

            // Assert
            foreach (var complexType in WsdlDefinition.Types.ComplexTypes)
            {
                var filePath = Path.Combine(OutputDir, "Models", "Common", $"{complexType.Name}.cs");
                Assert.True(File.Exists(filePath), $"Complex type file {filePath} should exist");
            }
        }

        [Fact]
        public void Generate_CreatesRequestFiles()
        {
            // Act
            _generator.Generate(WsdlDefinition, OutputNamespace, OutputDir);

            // Assert
            foreach (var portType in WsdlDefinition.PortTypes)
            {
                foreach (var operation in portType.Operations)
                {
                    // Find the input message
                    var inputMessage = operation.Input != null
                        ? WsdlDefinition.Messages.FirstOrDefault(m => m.Name == operation.Input.Message)
                        : null;

                    if (inputMessage != null && inputMessage.Parts.Count > 0)
                    {
                        var requestClassName = $"{operation.Name}Request";
                        var filePath = Path.Combine(OutputDir, "Models", "Requests", $"{requestClassName}.cs");
                        Assert.True(File.Exists(filePath), $"Request file {filePath} should exist");
                    }
                }
            }
        }

        [Fact]
        public void Generate_CreatesResponseFiles()
        {
            // Act
            _generator.Generate(WsdlDefinition, OutputNamespace, OutputDir);

            // Assert
            foreach (var portType in WsdlDefinition.PortTypes)
            {
                foreach (var operation in portType.Operations)
                {
                    // Find the output message
                    var outputMessage = operation.Output != null
                        ? WsdlDefinition.Messages.FirstOrDefault(m => m.Name == operation.Output.Message)
                        : null;

                    if (outputMessage != null && outputMessage.Parts.Count > 0)
                    {
                        var responseClassName = $"{operation.Name}Response";
                        var filePath = Path.Combine(OutputDir, "Models", "Responses", $"{responseClassName}.cs");
                        Assert.True(File.Exists(filePath), $"Response file {filePath} should exist");
                    }
                }
            }
        }

        [Fact]
        public void Generate_ComplexTypeFilesContainCorrectNamespace()
        {
            // Act
            _generator.Generate(WsdlDefinition, OutputNamespace, OutputDir);

            // Assert
            foreach (var complexType in WsdlDefinition.Types.ComplexTypes)
            {
                var filePath = Path.Combine(OutputDir, "Models", "Common", $"{complexType.Name}.cs");
                var fileContent = File.ReadAllText(filePath);
                Assert.Contains($"namespace {OutputNamespace}.Models.Common", fileContent);
            }
        }

        [Fact]
        public void Generate_RequestFilesContainCorrectNamespace()
        {
            // Act
            _generator.Generate(WsdlDefinition, OutputNamespace, OutputDir);

            // Assert
            foreach (var portType in WsdlDefinition.PortTypes)
            {
                foreach (var operation in portType.Operations)
                {
                    // Find the input message
                    var inputMessage = operation.Input != null
                        ? WsdlDefinition.Messages.FirstOrDefault(m => m.Name == operation.Input.Message)
                        : null;

                    if (inputMessage != null && inputMessage.Parts.Count > 0)
                    {
                        var requestClassName = $"{operation.Name}Request";
                        var filePath = Path.Combine(OutputDir, "Models", "Requests", $"{requestClassName}.cs");
                        var fileContent = File.ReadAllText(filePath);
                        Assert.Contains($"namespace {OutputNamespace}.Models.Requests", fileContent);
                    }
                }
            }
        }

        [Fact]
        public void Generate_ResponseFilesContainCorrectNamespace()
        {
            // Act
            _generator.Generate(WsdlDefinition, OutputNamespace, OutputDir);

            // Assert
            foreach (var portType in WsdlDefinition.PortTypes)
            {
                foreach (var operation in portType.Operations)
                {
                    // Find the output message
                    var outputMessage = operation.Output != null
                        ? WsdlDefinition.Messages.FirstOrDefault(m => m.Name == operation.Output.Message)
                        : null;

                    if (outputMessage != null && outputMessage.Parts.Count > 0)
                    {
                        var responseClassName = $"{operation.Name}Response";
                        var filePath = Path.Combine(OutputDir, "Models", "Responses", $"{responseClassName}.cs");
                        var fileContent = File.ReadAllText(filePath);
                        Assert.Contains($"namespace {OutputNamespace}.Models.Responses", fileContent);
                    }
                }
            }
        }

        [Fact]
        public void Generate_RequestFilesContainXmlRootAttribute()
        {
            // Act
            _generator.Generate(WsdlDefinition, OutputNamespace, OutputDir);

            // Assert
            foreach (var portType in WsdlDefinition.PortTypes)
            {
                foreach (var operation in portType.Operations)
                {
                    // Find the input message
                    var inputMessage = operation.Input != null
                        ? WsdlDefinition.Messages.FirstOrDefault(m => m.Name == operation.Input.Message)
                        : null;

                    if (inputMessage != null && inputMessage.Parts.Count > 0)
                    {
                        var requestClassName = $"{operation.Name}Request";
                        var filePath = Path.Combine(OutputDir, "Models", "Requests", $"{requestClassName}.cs");
                        var fileContent = File.ReadAllText(filePath);
                        Assert.Contains("[XmlRoot(", fileContent);
                        Assert.Contains($"ElementName = \"{operation.Name}\"", fileContent);
                        Assert.Contains($"Namespace = \"{WsdlDefinition.TargetNamespace}\"", fileContent);
                    }
                }
            }
        }

        [Fact]
        public void Generate_ResponseFilesContainXmlRootAttribute()
        {
            // Act
            _generator.Generate(WsdlDefinition, OutputNamespace, OutputDir);

            // Assert
            foreach (var portType in WsdlDefinition.PortTypes)
            {
                foreach (var operation in portType.Operations)
                {
                    // Find the output message
                    var outputMessage = operation.Output != null
                        ? WsdlDefinition.Messages.FirstOrDefault(m => m.Name == operation.Output.Message)
                        : null;

                    if (outputMessage != null && outputMessage.Parts.Count > 0)
                    {
                        var responseClassName = $"{operation.Name}Response";
                        var filePath = Path.Combine(OutputDir, "Models", "Responses", $"{responseClassName}.cs");
                        var fileContent = File.ReadAllText(filePath);
                        Assert.Contains("[XmlRoot(", fileContent);
                        Assert.Contains($"ElementName = \"{operation.Name}Response\"", fileContent);
                        Assert.Contains($"Namespace = \"{WsdlDefinition.TargetNamespace}\"", fileContent);
                    }
                }
            }
        }

        [Fact]
        public void Generate_ModelFilesContainRequiredUsings()
        {
            // Act
            _generator.Generate(WsdlDefinition, OutputNamespace, OutputDir);

            // Assert - Check complex types
            foreach (var complexType in WsdlDefinition.Types.ComplexTypes)
            {
                var filePath = Path.Combine(OutputDir, "Models", "Common", $"{complexType.Name}.cs");
                var fileContent = File.ReadAllText(filePath);

                // Check for required usings
                Assert.Contains("using System;", fileContent);
                Assert.Contains("using System.Collections.Generic;", fileContent);
                Assert.Contains("using System.Runtime.Serialization;", fileContent);
                Assert.Contains("using System.Xml.Serialization;", fileContent);
            }

            // Assert - Check request files
            foreach (var portType in WsdlDefinition.PortTypes)
            {
                foreach (var operation in portType.Operations)
                {
                    // Find the input message
                    var inputMessage = operation.Input != null
                        ? WsdlDefinition.Messages.FirstOrDefault(m => m.Name == operation.Input.Message)
                        : null;

                    if (inputMessage != null && inputMessage.Parts.Count > 0)
                    {
                        var requestClassName = $"{operation.Name}Request";
                        var filePath = Path.Combine(OutputDir, "Models", "Requests", $"{requestClassName}.cs");
                        var fileContent = File.ReadAllText(filePath);

                        // Check for required usings
                        Assert.Contains("using System;", fileContent);
                        Assert.Contains("using System.Xml.Serialization;", fileContent);
                    }
                }
            }

            // Assert - Check response files
            foreach (var portType in WsdlDefinition.PortTypes)
            {
                foreach (var operation in portType.Operations)
                {
                    // Find the output message
                    var outputMessage = operation.Output != null
                        ? WsdlDefinition.Messages.FirstOrDefault(m => m.Name == operation.Output.Message)
                        : null;

                    if (outputMessage != null && outputMessage.Parts.Count > 0)
                    {
                        var responseClassName = $"{operation.Name}Response";
                        var filePath = Path.Combine(OutputDir, "Models", "Responses", $"{responseClassName}.cs");
                        var fileContent = File.ReadAllText(filePath);

                        // Check for required usings
                        Assert.Contains("using System;", fileContent);
                        Assert.Contains("using System.Xml.Serialization;", fileContent);
                    }
                }
            }
        }

        [Fact]
        public void Generate_SoapAuthHeaderHasCorrectStructure()
        {
            // Arrange
            // Modify the WsdlDefinition to include a SWBCAuthHeader
            // This is a simplified approach to ensure the test runs even if the sample WSDL doesn't have auth headers
            var hasSWBCAuthHeader = false;

            // Check if any binding operation has a SWBCAuthHeader
            foreach (var binding in WsdlDefinition.Bindings)
            {
                foreach (var operation in binding.Operations)
                {
                    if (operation.Input != null && operation.Input.Headers.Count > 0)
                    {
                        // Add a header that will trigger SoapAuthHeader generation
                        var headerMessage = new WsdlMessage
                        {
                            Name = "SWBCAuthHeaderMessage"
                        };

                        var headerPart = new WsdlMessagePart
                        {
                            Name = "SWBCAuthHeaderPart",
                            Element = "SWBCAuthHeader"
                        };

                        headerMessage.Parts.Add(headerPart);

                        if (!WsdlDefinition.Messages.Any(m => m.Name == headerMessage.Name))
                        {
                            WsdlDefinition.Messages.Add(headerMessage);
                        }

                        var header = new WsdlBindingOperationMessageHeader
                        {
                            Message = headerMessage.Name,
                            Part = headerPart.Name
                        };

                        operation.Input.Headers.Add(header);

                        // Add the element to the types
                        var element = new WsdlElement
                        {
                            Name = "SWBCAuthHeader",
                            Type = "SWBCAuthHeaderType"
                        };

                        if (!WsdlDefinition.Types.Elements.Any(e => e.Name == element.Name))
                        {
                            WsdlDefinition.Types.Elements.Add(element);
                        }

                        hasSWBCAuthHeader = true;
                        break;
                    }
                }

                if (hasSWBCAuthHeader)
                    break;
            }

            // Act
            _generator.Generate(WsdlDefinition, OutputNamespace, OutputDir);

            // Assert
            var soapAuthHeaderPath = Path.Combine(OutputDir, "Models", "Headers", "SoapAuthHeader.cs");

            // If we have a SWBCAuthHeader, the SoapAuthHeader.cs file should exist
            if (hasSWBCAuthHeader)
            {
                Assert.True(File.Exists(soapAuthHeaderPath), "SoapAuthHeader.cs file should exist");

                var fileContent = File.ReadAllText(soapAuthHeaderPath);

                // Check that the SoapAuthHeader class has Username and Password properties
                Assert.Contains("public string Username { get; set; }", fileContent);
                Assert.Contains("public string Password { get; set; }", fileContent);

                // Check that the SoapAuthHeader class does NOT have a nested SWBCAuthHeader property
                // This is the key check to ensure we don't have nested auth headers
                Assert.DoesNotContain("public SWBCAuthHeader SWBCAuthHeader { get; set; }", fileContent);

                // Check that the class has the correct XmlRoot attribute
                Assert.Contains("[XmlRoot(ElementName = \"SWBCAuthHeader\"", fileContent);
            }
        }

        [Fact]
        public void Generate_CreatesUniqueRequestClassesForOverloadedOperations()
        {
            // Arrange
            // Add two operations with the same name but different input messages
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

            // Second operation: PostSinglePayment with a different input message
            var operation2 = new WsdlOperation
            {
                Name = "PostSinglePayment",
                Input = new WsdlOperationMessage { Message = "PostSinglePaymentWithApplySoapIn" },
                Output = new WsdlOperationMessage { Message = "PostSinglePaymentWithApplySoapOut" }
            };

            // Add the operations to the port type
            portType.Operations.Add(operation1);
            portType.Operations.Add(operation2);

            // Add the port type to the WSDL definition
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
                Name = "PostSinglePaymentWithApplySoapIn"
            };
            inputMessage2.Parts.Add(new WsdlMessagePart { Name = "parameters", Element = "PostSinglePaymentWithApply" });

            var outputMessage2 = new WsdlMessage
            {
                Name = "PostSinglePaymentWithApplySoapOut"
            };
            outputMessage2.Parts.Add(new WsdlMessagePart { Name = "parameters", Element = "PostSinglePaymentWithApplyResponse" });

            // Add the messages to the WSDL definition
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
                Name = "PostSinglePaymentWithApply",
                Type = "PostSinglePaymentWithApplyType"
            };
            var element4 = new WsdlElement
            {
                Name = "PostSinglePaymentWithApplyResponse",
                Type = "PostSinglePaymentWithApplyResponseType"
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

            // Act
            _generator.Generate(WsdlDefinition, OutputNamespace, OutputDir);

            // Assert
            // Check that both request classes were created with unique names
            var requestPath1 = Path.Combine(OutputDir, "Models", "Requests", "PostSinglePaymentRequest.cs");
            var requestPath2 = Path.Combine(OutputDir, "Models", "Requests", "PostSinglePaymentWithApplyRequest.cs");

            Assert.True(File.Exists(requestPath1), "PostSinglePaymentRequest.cs file should exist");
            Assert.True(File.Exists(requestPath2), "PostSinglePaymentWithApplyRequest.cs file should exist");

            // Check that the response classes were created with unique names
            var responsePath1 = Path.Combine(OutputDir, "Models", "Responses", "PostSinglePaymentResponse.cs");
            var responsePath2 = Path.Combine(OutputDir, "Models", "Responses", "PostSinglePaymentWithApplyResponse.cs");

            Assert.True(File.Exists(responsePath1), "PostSinglePaymentResponse.cs file should exist");
            Assert.True(File.Exists(responsePath2), "PostSinglePaymentWithApplyResponse.cs file should exist");

            // Check the content of the request classes to ensure they have the correct class names
            var requestContent1 = File.ReadAllText(requestPath1);
            var requestContent2 = File.ReadAllText(requestPath2);

            Assert.Contains("public class PostSinglePaymentRequest", requestContent1);
            Assert.Contains("public class PostSinglePaymentWithApplyRequest", requestContent2);

            // Check the content of the response classes to ensure they have the correct class names
            var responseContent1 = File.ReadAllText(responsePath1);
            var responseContent2 = File.ReadAllText(responsePath2);

            Assert.Contains("public class PostSinglePaymentResponse", responseContent1);
            Assert.Contains("public class PostSinglePaymentWithApplyResponse", responseContent2);
        }

        public void Dispose()
        {
            CleanupOutputDirectory();
        }
    }
}
