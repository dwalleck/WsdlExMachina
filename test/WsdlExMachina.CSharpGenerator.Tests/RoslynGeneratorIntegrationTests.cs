using System;
using System.Collections.Generic;
using System.Linq;
using WsdlExMachina.Parser;
using WsdlExMachina.Parser.Models;
using Xunit;

namespace WsdlExMachina.CSharpGenerator.Tests
{
    public class RoslynGeneratorIntegrationTests
    {
        private readonly RoslynCodeGenerator _roslynCodeGenerator;
        private readonly RoslynEnumGenerator _roslynEnumGenerator;
        private readonly RoslynComplexTypeGenerator _roslynComplexTypeGenerator;
        private readonly RoslynRequestModelGenerator _roslynRequestModelGenerator;
        private readonly RoslynGeneratorFacade _roslynGeneratorFacade;

        public RoslynGeneratorIntegrationTests()
        {
            _roslynCodeGenerator = new RoslynCodeGenerator();
            _roslynEnumGenerator = new RoslynEnumGenerator(_roslynCodeGenerator);
            _roslynComplexTypeGenerator = new RoslynComplexTypeGenerator(_roslynCodeGenerator);
            _roslynRequestModelGenerator = new RoslynRequestModelGenerator(_roslynCodeGenerator, _roslynComplexTypeGenerator, _roslynEnumGenerator);
            _roslynGeneratorFacade = new RoslynGeneratorFacade();
        }

        [Fact]
        public void EnumGeneration_ShouldProduceCorrectCode()
        {
            // Arrange
            var simpleType = new WsdlSimpleType
            {
                Name = "TestEnum",
                Namespace = "http://test.com/",
                EnumerationValues = new List<string> { "Value1", "Value2", "Value3" }
            };

            var wsdl = new WsdlDefinition
            {
                TargetNamespace = "http://test.com/",
                Types = new WsdlTypes
                {
                    SimpleTypes = new List<WsdlSimpleType> { simpleType }
                }
            };

            var namespaceName = "Test.Namespace";

            // Act
            var roslynCode = _roslynEnumGenerator.GenerateEnumCode(simpleType, namespaceName);
            var facadeResult = _roslynGeneratorFacade.GenerateSimpleTypes(wsdl, namespaceName);
            var facadeCode = facadeResult[$"{simpleType.Name}.cs"];

            // Assert
            // Check that both contain the enum name
            Assert.Contains("enum TestEnum", roslynCode);
            Assert.Contains("enum TestEnum", facadeCode);

            // Check that both contain the XML type attribute
            Assert.Contains("[XmlType(TypeName = \"TestEnum\", Namespace = \"http://test.com/\")]", roslynCode);
            Assert.Contains("[XmlType(TypeName = \"TestEnum\", Namespace = \"http://test.com/\")]", facadeCode);

            // Check that both contain all enum values with XML enum attributes
            foreach (var value in simpleType.EnumerationValues)
            {
                Assert.Contains($"[XmlEnum(Name = \"{value}\")]", roslynCode);
                Assert.Contains($"[XmlEnum(Name = \"{value}\")]", facadeCode);
            }
        }

        [Fact]
        public void ComplexTypeGeneration_ShouldProduceCorrectCode()
        {
            // Arrange
            var complexType = new WsdlComplexType
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
            };

            var wsdl = new WsdlDefinition
            {
                TargetNamespace = "http://test.com/",
                Types = new WsdlTypes
                {
                    ComplexTypes = new List<WsdlComplexType> { complexType }
                }
            };

            var namespaceName = "Test.Namespace";

            // Act
            var roslynCode = _roslynComplexTypeGenerator.GenerateComplexTypeCode(complexType, namespaceName);
            var facadeResult = _roslynGeneratorFacade.GenerateComplexTypes(wsdl, namespaceName);
            var facadeCode = facadeResult[$"{complexType.Name}.cs"];

            // Assert
            // Check that both contain the class name
            Assert.Contains("class TestType", roslynCode);
            Assert.Contains("class TestType", facadeCode);

            // Check that both contain the XML type attribute
            Assert.Contains("[XmlType(TypeName = \"TestType\", Namespace = \"http://test.com/\")]", roslynCode);
            Assert.Contains("[XmlType(TypeName = \"TestType\", Namespace = \"http://test.com/\")]", facadeCode);

            // Check that both contain all properties with XML element attributes
            foreach (var element in complexType.Elements)
            {
                var propertyName = new NamingHelper().GetSafePropertyName(element.Name);
                Assert.Contains($"public string {propertyName} {{ get; set; }}", roslynCode);
                Assert.Contains($"public string {propertyName} {{ get; set; }}", facadeCode);
                Assert.Contains($"[XmlElement(ElementName = \"{element.Name}\")]", roslynCode);
                Assert.Contains($"[XmlElement(ElementName = \"{element.Name}\")]", facadeCode);
            }
        }

        [Fact]
        public void RequestModelGeneration_ShouldProduceCorrectCode()
        {
            // Arrange
            var complexType = new WsdlComplexType
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
            };

            var element = new WsdlElement
            {
                Name = "TestElement",
                Type = "TestType",
                IsComplexType = true
            };

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

            var wsdl = new WsdlDefinition
            {
                TargetNamespace = "http://test.com/",
                Types = new WsdlTypes
                {
                    Elements = new List<WsdlElement> { element },
                    ComplexTypes = new List<WsdlComplexType> { complexType }
                },
                Messages = new List<WsdlMessage> { message },
                PortTypes = new List<WsdlPortType>
                {
                    new WsdlPortType
                    {
                        Name = "TestPortType",
                        Operations = new List<WsdlOperation>
                        {
                            new WsdlOperation
                            {
                                Name = "TestOperation",
                                Input = new WsdlOperationMessage { Message = "TestMessage" }
                            }
                        }
                    }
                }
            };

            var operationName = "TestOperation";
            var namespaceName = "Test.Namespace";

            // Act
            var roslynCode = _roslynRequestModelGenerator.GenerateRequestModelCode(wsdl, message, operationName, namespaceName);
            var facadeResult = _roslynGeneratorFacade.GenerateRequestModels(wsdl, namespaceName);
            var facadeCode = facadeResult[$"{operationName}Request.cs"];

            // Assert
            // Check that both contain the class name
            Assert.Contains("class TestOperationRequest", roslynCode);
            Assert.Contains("class TestOperationRequest", facadeCode);

            // Check that both contain the XML root attribute
            Assert.Contains("[XmlRoot(ElementName = \"TestMessage\", Namespace = \"http://test.com/\")]", roslynCode);
            Assert.Contains("[XmlRoot(ElementName = \"TestMessage\", Namespace = \"http://test.com/\")]", facadeCode);

            // Check that both contain all properties with XML element attributes
            foreach (var prop in complexType.Elements)
            {
                var propertyName = new NamingHelper().GetSafePropertyName(prop.Name);
                Assert.Contains($"public string {propertyName} {{ get; set; }}", roslynCode);
                Assert.Contains($"public string {propertyName} {{ get; set; }}", facadeCode);
                Assert.Contains($"[XmlElement(ElementName = \"{prop.Name}\")]", roslynCode);
                Assert.Contains($"[XmlElement(ElementName = \"{prop.Name}\")]", facadeCode);
            }
        }
    }
}
