using System;
using System.Collections.Generic;
using System.Linq;
using WsdlExMachina.Parser.Models;
using Xunit;

namespace WsdlExMachina.CSharpGenerator.Tests;

public class RoslynGeneratorFacadeTests
{
    private readonly RoslynGeneratorFacade _facade;

    public RoslynGeneratorFacadeTests()
    {
        _facade = new RoslynGeneratorFacade();
    }

    [Fact]
    public void GenerateSimpleTypes_ShouldGenerateEnumTypes()
    {
        // Arrange
        var wsdl = CreateTestWsdl();
        var namespaceName = "Test.Namespace";

        // Act
        var result = _facade.GenerateSimpleTypes(wsdl, namespaceName);

        // Assert
        Assert.Equal(1, result.Count);
        Assert.Contains("TestEnum.cs", result.Keys);
        Assert.Contains("enum TestEnum", result["TestEnum.cs"]);
        Assert.Contains("[XmlType(TypeName = \"TestEnum\", Namespace = \"http://test.com/\")]", result["TestEnum.cs"]);
    }

    [Fact]
    public void GenerateComplexTypes_ShouldGenerateComplexTypes()
    {
        // Arrange
        var wsdl = CreateTestWsdl();
        var namespaceName = "Test.Namespace";

        // Act
        var result = _facade.GenerateComplexTypes(wsdl, namespaceName);

        // Assert
        Assert.Equal(1, result.Count);
        Assert.Contains("TestType.cs", result.Keys);
        Assert.Contains("class TestType", result["TestType.cs"]);
        Assert.Contains("[XmlType(TypeName = \"TestType\", Namespace = \"http://test.com/\")]", result["TestType.cs"]);
    }

    [Fact]
    public void GenerateRequestModels_ShouldGenerateRequestModels()
    {
        // Arrange
        var wsdl = CreateTestWsdlWithOperations();
        var namespaceName = "Test.Namespace";

        // Act
        var result = _facade.GenerateRequestModels(wsdl, namespaceName);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains("Operation1Request.cs", result.Keys);
        Assert.Contains("Operation2Request.cs", result.Keys);
        Assert.Contains("class Operation1Request", result["Operation1Request.cs"]);
        Assert.Contains("class Operation2Request", result["Operation2Request.cs"]);
    }

    [Fact]
    public void GenerateAll_ShouldGenerateAllTypes()
    {
        // Arrange
        var wsdl = CreateTestWsdlWithOperations();
        var namespaceName = "Test.Namespace";

        // Act
        var result = _facade.GenerateAll(wsdl, namespaceName);

        // Assert
        Assert.Equal(4, result.Count);
        Assert.Contains("TestEnum.cs", result.Keys);
        Assert.Contains("TestType.cs", result.Keys);
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
                SimpleTypes = new List<WsdlSimpleType>
                {
                    new WsdlSimpleType
                    {
                        Name = "TestEnum",
                        Namespace = "http://test.com/",
                        EnumerationValues = new List<string> { "Value1", "Value2", "Value3" }
                    }
                },
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
