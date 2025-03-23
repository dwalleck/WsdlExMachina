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
        private readonly RequestModelGenerator _stringBuilderGenerator;

        public RoslynGeneratorIntegrationTests()
        {
            _roslynCodeGenerator = new RoslynCodeGenerator();
            _roslynEnumGenerator = new RoslynEnumGenerator(_roslynCodeGenerator);
            _roslynComplexTypeGenerator = new RoslynComplexTypeGenerator(_roslynCodeGenerator);
            _roslynRequestModelGenerator = new RoslynRequestModelGenerator(_roslynCodeGenerator, _roslynComplexTypeGenerator, _roslynEnumGenerator);
            _stringBuilderGenerator = new RequestModelGenerator();
        }

        [Fact]
        public void CompareEnumGeneration_ShouldProduceFunctionallyEquivalentCode()
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

            // Use GenerateSimpleTypes which calls GenerateEnumType internally
            var stringBuilderResult = _stringBuilderGenerator.GenerateSimpleTypes(wsdl);
            var stringBuilderCode = stringBuilderResult[$"{simpleType.Name}.cs"];

            // Assert
            // Check that both contain the enum name
            Assert.Contains("enum TestEnum", roslynCode);
            Assert.Contains("enum TestEnum", stringBuilderCode);

            // Check that both contain the XML type attribute
            Assert.Contains("[XmlType(TypeName = \"TestEnum\", Namespace = \"http://test.com/\")]", roslynCode);
            Assert.Contains("[XmlType(TypeName = \"TestEnum\", Namespace = \"http://test.com/\")]", stringBuilderCode);

            // Check that both contain all enum values with XML enum attributes
            foreach (var value in simpleType.EnumerationValues)
            {
                Assert.Contains($"[XmlEnum(Name = \"{value}\")]", roslynCode);
                Assert.Contains($"[XmlEnum(Name = \"{value}\")]", stringBuilderCode);
            }
        }

        [Fact]
        public void CompareComplexTypeGeneration_ShouldProduceFunctionallyEquivalentCode()
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

            // Use GenerateComplexTypes which calls GenerateComplexTypeClass internally
            var stringBuilderResult = _stringBuilderGenerator.GenerateComplexTypes(wsdl);
            var stringBuilderCode = stringBuilderResult[$"{complexType.Name}.cs"];

            // Assert
            // Check that both contain the class name
            Assert.Contains("class TestType", roslynCode);
            Assert.Contains("class TestType", stringBuilderCode);

            // Check that both contain the XML type attribute
            Assert.Contains("[XmlType(TypeName = \"TestType\", Namespace = \"http://test.com/\")]", roslynCode);
            Assert.Contains("[XmlType(TypeName = \"TestType\", Namespace = \"http://test.com/\")]", stringBuilderCode);

            // Check that both contain all properties with XML element attributes
            foreach (var element in complexType.Elements)
            {
                var propertyName = new NamingHelper().GetSafePropertyName(element.Name);
                Assert.Contains($"public string {propertyName} {{ get; set; }}", roslynCode);
                Assert.Contains($"public string {propertyName} {{ get; set; }}", stringBuilderCode);
                Assert.Contains($"[XmlElement(ElementName = \"{element.Name}\")]", roslynCode);
                Assert.Contains($"[XmlElement(ElementName = \"{element.Name}\")]", stringBuilderCode);
            }
        }

        [Fact]
        public void CompareRequestModelGeneration_ShouldProduceFunctionallyEquivalentCode()
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

            // Use GenerateRequestModels which calls GenerateRequestModelClass internally
            var stringBuilderResult = _stringBuilderGenerator.GenerateRequestModels(wsdl);
            var stringBuilderCode = stringBuilderResult[$"{operationName}Request.cs"];

            // Assert
            // Check that both contain the class name
            Assert.Contains("class TestOperationRequest", roslynCode);
            Assert.Contains("class TestOperationRequest", stringBuilderCode);

            // Check that both contain the XML root attribute
            Assert.Contains("[XmlRoot(ElementName = \"TestMessage\", Namespace = \"http://test.com/\")]", roslynCode);
            Assert.Contains("[XmlRoot(ElementName = \"TestMessage\", Namespace = \"http://test.com/\")]", stringBuilderCode);

            // Check that both contain all properties with XML element attributes
            foreach (var prop in complexType.Elements)
            {
                var propertyName = new NamingHelper().GetSafePropertyName(prop.Name);
                Assert.Contains($"public string {propertyName} {{ get; set; }}", roslynCode);
                Assert.Contains($"public string {propertyName} {{ get; set; }}", stringBuilderCode);
                Assert.Contains($"[XmlElement(ElementName = \"{prop.Name}\")]", roslynCode);
                Assert.Contains($"[XmlElement(ElementName = \"{prop.Name}\")]", stringBuilderCode);
            }
        }
    }
}
