using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WsdlExMachina.Parser.Models;
using Xunit;

namespace WsdlExMachina.CSharpGenerator.Tests
{
    public class RoslynRequestModelGeneratorTests
    {
        private readonly RoslynRequestModelGenerator _generator;
        private readonly RoslynCodeGenerator _codeGenerator;
        private readonly RoslynComplexTypeGenerator _complexTypeGenerator;
        private readonly RoslynEnumGenerator _enumGenerator;

        public RoslynRequestModelGeneratorTests()
        {
            _codeGenerator = new RoslynCodeGenerator();
            _complexTypeGenerator = new RoslynComplexTypeGenerator(_codeGenerator);
            _enumGenerator = new RoslynEnumGenerator(_codeGenerator);
            _generator = new RoslynRequestModelGenerator(_codeGenerator, _complexTypeGenerator, _enumGenerator);
        }

        [Fact]
        public void GenerateRequestModel_ShouldThrowArgumentNullException_WhenWsdlIsNull()
        {
            // Arrange
            var message = new WsdlMessage { Name = "TestMessage" };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _generator.GenerateRequestModel(null, message, "TestOperation", "Test.Namespace"));
        }

        [Fact]
        public void GenerateRequestModel_ShouldThrowArgumentNullException_WhenMessageIsNull()
        {
            // Arrange
            var wsdl = CreateTestWsdl();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _generator.GenerateRequestModel(wsdl, null, "TestOperation", "Test.Namespace"));
        }

        [Fact]
        public void GenerateRequestModel_ShouldThrowArgumentException_WhenOperationNameIsEmpty()
        {
            // Arrange
            var wsdl = CreateTestWsdl();
            var message = new WsdlMessage { Name = "TestMessage" };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _generator.GenerateRequestModel(wsdl, message, string.Empty, "Test.Namespace"));
        }

        [Fact]
        public void GenerateRequestModel_ShouldThrowArgumentException_WhenNamespaceIsEmpty()
        {
            // Arrange
            var wsdl = CreateTestWsdl();
            var message = new WsdlMessage { Name = "TestMessage" };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _generator.GenerateRequestModel(wsdl, message, "TestOperation", string.Empty));
        }

        [Fact]
        public void GenerateRequestModel_ShouldCreateClassWithCorrectName()
        {
            // Arrange
            var wsdl = CreateTestWsdl();
            var message = CreateTestMessage();
            var operationName = "TestOperation";
            var namespaceName = "Test.Namespace";

            // Act
            var result = _generator.GenerateRequestModel(wsdl, message, operationName, namespaceName);

            // Assert
            var namespaceDeclaration = result.Members.OfType<FileScopedNamespaceDeclarationSyntax>().First();
            var classDeclaration = namespaceDeclaration.Members.OfType<ClassDeclarationSyntax>().First();

            Assert.Equal(operationName + "Request", classDeclaration.Identifier.Text);
        }

        [Fact]
        public void GenerateRequestModel_ShouldCreateClassWithCorrectXmlRootAttribute()
        {
            // Arrange
            var wsdl = CreateTestWsdl();
            var message = CreateTestMessage();
            var operationName = "TestOperation";
            var namespaceName = "Test.Namespace";

            // Act
            var result = _generator.GenerateRequestModel(wsdl, message, operationName, namespaceName);

            // Assert
            var namespaceDeclaration = result.Members.OfType<FileScopedNamespaceDeclarationSyntax>().First();
            var classDeclaration = namespaceDeclaration.Members.OfType<ClassDeclarationSyntax>().First();
            var xmlRootAttribute = classDeclaration.AttributeLists
                .SelectMany(al => al.Attributes)
                .FirstOrDefault(a => a.Name.ToString() == "XmlRoot");

            Assert.NotNull(xmlRootAttribute);

            var elementNameArg = xmlRootAttribute.ArgumentList.Arguments
                .FirstOrDefault(a => a.NameEquals.Name.ToString() == "ElementName");
            var namespaceArg = xmlRootAttribute.ArgumentList.Arguments
                .FirstOrDefault(a => a.NameEquals.Name.ToString() == "Namespace");

            Assert.NotNull(elementNameArg);
            Assert.NotNull(namespaceArg);
            Assert.Equal(message.Name, ((LiteralExpressionSyntax)elementNameArg.Expression).Token.ValueText);
            Assert.Equal(wsdl.TargetNamespace, ((LiteralExpressionSyntax)namespaceArg.Expression).Token.ValueText);
        }

        [Fact]
        public void GenerateRequestModel_ShouldCreateClassWithCorrectProperties_WhenPartReferencesElement()
        {
            // Arrange
            var wsdl = CreateTestWsdl();
            var message = new WsdlMessage
            {
                Name = "TestMessage",
                Parts = new List<WsdlMessagePart>
                {
                    new WsdlMessagePart
                    {
                        Name = "parameters",
                        Element = "TestElement"
                    }
                }
            };
            var operationName = "TestOperation";
            var namespaceName = "Test.Namespace";

            // Act
            var result = _generator.GenerateRequestModel(wsdl, message, operationName, namespaceName);

            // Assert
            var namespaceDeclaration = result.Members.OfType<FileScopedNamespaceDeclarationSyntax>().First();
            var classDeclaration = namespaceDeclaration.Members.OfType<ClassDeclarationSyntax>().First();
            var properties = classDeclaration.Members.OfType<PropertyDeclarationSyntax>().ToList();

            // The complex type associated with the element has 2 properties
            Assert.Equal(2, properties.Count);

            // Check first property
            Assert.Equal("Property1", properties[0].Identifier.Text);
            Assert.Equal("string", properties[0].Type.ToString());

            // Check XML attribute
            var xmlElementAttribute = properties[0].AttributeLists
                .SelectMany(al => al.Attributes)
                .FirstOrDefault(a => a.Name.ToString() == "XmlElement");

            Assert.NotNull(xmlElementAttribute);

            var elementNameArg = xmlElementAttribute.ArgumentList.Arguments
                .FirstOrDefault(a => a.NameEquals.Name.ToString() == "ElementName");

            Assert.NotNull(elementNameArg);
            Assert.Equal("Property1", ((LiteralExpressionSyntax)elementNameArg.Expression).Token.ValueText);

            // Check second property
            Assert.Equal("Property2", properties[1].Identifier.Text);
            Assert.Equal("string", properties[1].Type.ToString());
        }

        [Fact]
        public void GenerateRequestModel_ShouldCreateClassWithCorrectProperties_WhenPartReferencesType()
        {
            // Arrange
            var wsdl = CreateTestWsdl();
            var message = new WsdlMessage
            {
                Name = "TestMessage",
                Parts = new List<WsdlMessagePart>
                {
                    new WsdlMessagePart
                    {
                        Name = "parameter",
                        Type = "string"
                    }
                }
            };
            var operationName = "TestOperation";
            var namespaceName = "Test.Namespace";

            // Act
            var result = _generator.GenerateRequestModel(wsdl, message, operationName, namespaceName);

            // Assert
            var namespaceDeclaration = result.Members.OfType<FileScopedNamespaceDeclarationSyntax>().First();
            var classDeclaration = namespaceDeclaration.Members.OfType<ClassDeclarationSyntax>().First();
            var properties = classDeclaration.Members.OfType<PropertyDeclarationSyntax>().ToList();

            Assert.Equal(1, properties.Count);
            Assert.Equal("Parameter", properties[0].Identifier.Text);
            Assert.Equal("string", properties[0].Type.ToString());

            // Check XML attribute
            var xmlElementAttribute = properties[0].AttributeLists
                .SelectMany(al => al.Attributes)
                .FirstOrDefault(a => a.Name.ToString() == "XmlElement");

            Assert.NotNull(xmlElementAttribute);

            var elementNameArg = xmlElementAttribute.ArgumentList.Arguments
                .FirstOrDefault(a => a.NameEquals.Name.ToString() == "ElementName");

            Assert.NotNull(elementNameArg);
            Assert.Equal("parameter", ((LiteralExpressionSyntax)elementNameArg.Expression).Token.ValueText);
        }

        [Fact]
        public void GenerateRequestModel_ShouldHandleArrayTypes()
        {
            // Arrange
            var wsdl = CreateTestWsdl();

            // Add an array element to the complex type
            wsdl.Types.ComplexTypes[0].Elements.Add(new WsdlElement
            {
                Name = "ArrayProperty",
                Type = "ArrayOfString",
                IsComplexType = true
            });

            var message = new WsdlMessage
            {
                Name = "TestMessage",
                Parts = new List<WsdlMessagePart>
                {
                    new WsdlMessagePart
                    {
                        Name = "parameters",
                        Element = "TestElement"
                    }
                }
            };
            var operationName = "TestOperation";
            var namespaceName = "Test.Namespace";

            // Act
            var result = _generator.GenerateRequestModel(wsdl, message, operationName, namespaceName);

            // Assert
            var namespaceDeclaration = result.Members.OfType<FileScopedNamespaceDeclarationSyntax>().First();
            var classDeclaration = namespaceDeclaration.Members.OfType<ClassDeclarationSyntax>().First();
            var properties = classDeclaration.Members.OfType<PropertyDeclarationSyntax>().ToList();

            // Now we have 3 properties (2 original + 1 array)
            Assert.Equal(3, properties.Count);

            // Check array property
            Assert.Equal("ArrayProperty", properties[2].Identifier.Text);
            Assert.Equal("List<string>", properties[2].Type.ToString());
        }

        [Fact]
        public void GenerateRequestModelCode_ShouldReturnFormattedCode()
        {
            // Arrange
            var wsdl = CreateTestWsdl();
            var message = CreateTestMessage();
            var operationName = "TestOperation";
            var namespaceName = "Test.Namespace";

            // Act
            var result = _generator.GenerateRequestModelCode(wsdl, message, operationName, namespaceName);

            // Assert
            Assert.Contains($"namespace {namespaceName}", result);
            Assert.Contains("[XmlRoot(ElementName = \"TestMessage\", Namespace = \"http://test.com/\")]", result);
            Assert.Contains("public class TestOperationRequest", result);
            Assert.Contains("public string Property1 { get; set; }", result);
            Assert.Contains("public string Property2 { get; set; }", result);
            Assert.Contains("[XmlElement(ElementName = \"Property1\")]", result);
            Assert.Contains("[XmlElement(ElementName = \"Property2\")]", result);
        }

        [Fact]
        public void GenerateRequestModels_ShouldGenerateAllRequestModels()
        {
            // Arrange
            var wsdl = CreateTestWsdlWithOperations();
            var namespaceName = "Test.Namespace";

            // Act
            var result = _generator.GenerateRequestModels(wsdl, namespaceName);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains("Operation1Request.cs", result.Keys);
            Assert.Contains("Operation2Request.cs", result.Keys);
        }

        private static WsdlDefinition CreateTestWsdl()
        {
            return new WsdlDefinition
            {
                TargetNamespace = "http://test.com/",
                Types = new WsdlTypes
                {
                    Elements = new List<WsdlElement>
                    {
                        new WsdlElement
                        {
                            Name = "TestElement",
                            Type = "TestType",
                            IsComplexType = true
                        }
                    },
                    ComplexTypes = new List<WsdlComplexType>
                    {
                        new WsdlComplexType
                        {
                            Name = "TestType",
                            Namespace = "http://test.com/",
                            Elements = new List<WsdlElement>
                            {
                                new WsdlElement
                                {
                                    Name = "Property1",
                                    Type = "string",
                                    IsComplexType = false
                                },
                                new WsdlElement
                                {
                                    Name = "Property2",
                                    Type = "string",
                                    IsComplexType = false
                                }
                            }
                        }
                    }
                }
            };
        }

        private static WsdlMessage CreateTestMessage()
        {
            return new WsdlMessage
            {
                Name = "TestMessage",
                Parts = new List<WsdlMessagePart>
                {
                    new WsdlMessagePart
                    {
                        Name = "parameters",
                        Element = "TestElement"
                    }
                }
            };
        }

        private static WsdlDefinition CreateTestWsdlWithOperations()
        {
            var wsdl = CreateTestWsdl();

            wsdl.Messages = new List<WsdlMessage>
            {
                new WsdlMessage
                {
                    Name = "Operation1Request",
                    Parts = new List<WsdlMessagePart>
                    {
                        new WsdlMessagePart
                        {
                            Name = "parameters",
                            Element = "TestElement"
                        }
                    }
                },
                new WsdlMessage
                {
                    Name = "Operation2Request",
                    Parts = new List<WsdlMessagePart>
                    {
                        new WsdlMessagePart
                        {
                            Name = "parameters",
                            Element = "TestElement"
                        }
                    }
                }
            };

            wsdl.PortTypes = new List<WsdlPortType>
            {
                new WsdlPortType
                {
                    Name = "TestPortType",
                    Operations = new List<WsdlOperation>
                    {
                        new WsdlOperation
                        {
                            Name = "Operation1",
                            Input = new WsdlOperationMessage { Message = "Operation1Request" }
                        },
                        new WsdlOperation
                        {
                            Name = "Operation2",
                            Input = new WsdlOperationMessage { Message = "Operation2Request" }
                        }
                    }
                }
            };

            return wsdl;
        }
    }
}
