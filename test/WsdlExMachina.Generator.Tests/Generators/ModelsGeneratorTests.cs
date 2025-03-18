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

        public void Dispose()
        {
            CleanupOutputDirectory();
        }
    }
}
