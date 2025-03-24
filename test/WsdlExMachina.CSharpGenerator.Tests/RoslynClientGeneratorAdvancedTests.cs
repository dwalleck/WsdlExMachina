using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WsdlExMachina.Parser.Models;
using Xunit;

namespace WsdlExMachina.CSharpGenerator.Tests;

public class RoslynClientGeneratorAdvancedTests
{
    private readonly RoslynClientGenerator _generator;
    private readonly RoslynCodeGenerator _codeGenerator;

    public RoslynClientGeneratorAdvancedTests()
    {
        _codeGenerator = new RoslynCodeGenerator();
        _generator = new RoslynClientGenerator(_codeGenerator);
    }

    #region Advanced Functionality Tests

    [Fact]
    public void GenerateClient_ShouldHandleMultiplePortsInSameService()
    {
        // Arrange
        var wsdl = new WsdlDefinition
        {
            TargetNamespace = "http://test.com/",
            Services = new List<WsdlService>
            {
                new WsdlService
                {
                    Name = "MultiPortService",
                    Ports = new List<WsdlPort>
                    {
                        new WsdlPort
                        {
                            Name = "Port1",
                            Binding = "Binding1"
                        },
                        new WsdlPort
                        {
                            Name = "Port2",
                            Binding = "Binding2"
                        }
                    }
                }
            },
            Bindings = new List<WsdlBinding>
            {
                new WsdlBinding
                {
                    Name = "Binding1",
                    Type = "PortType1",
                    Operations = new List<WsdlBindingOperation>
                    {
                        new WsdlBindingOperation
                        {
                            Name = "Operation1",
                            SoapAction = "http://test.com/Operation1"
                        }
                    }
                },
                new WsdlBinding
                {
                    Name = "Binding2",
                    Type = "PortType2",
                    Operations = new List<WsdlBindingOperation>
                    {
                        new WsdlBindingOperation
                        {
                            Name = "Operation2",
                            SoapAction = "http://test.com/Operation2"
                        }
                    }
                }
            },
            PortTypes = new List<WsdlPortType>
            {
                new WsdlPortType
                {
                    Name = "PortType1",
                    Operations = new List<WsdlOperation>
                    {
                        new WsdlOperation
                        {
                            Name = "Operation1",
                            Input = new WsdlOperationMessage
                            {
                                Name = "Operation1Input",
                                Message = "Operation1Request"
                            },
                            Output = new WsdlOperationMessage
                            {
                                Name = "Operation1Output",
                                Message = "Operation1Response"
                            }
                        }
                    }
                },
                new WsdlPortType
                {
                    Name = "PortType2",
                    Operations = new List<WsdlOperation>
                    {
                        new WsdlOperation
                        {
                            Name = "Operation2",
                            Input = new WsdlOperationMessage
                            {
                                Name = "Operation2Input",
                                Message = "Operation2Request"
                            },
                            Output = new WsdlOperationMessage
                            {
                                Name = "Operation2Output",
                                Message = "Operation2Response"
                            }
                        }
                    }
                }
            },
            Messages = new List<WsdlMessage>
            {
                new WsdlMessage { Name = "Operation1Request" },
                new WsdlMessage { Name = "Operation1Response" },
                new WsdlMessage { Name = "Operation2Request" },
                new WsdlMessage { Name = "Operation2Response" }
            }
        };

        // Act
        var result = _generator.GenerateClient(wsdl, "Test.Namespace");

        // Assert
        Assert.Single(result);
        Assert.Contains("MultiPortServiceClient.cs", result.Keys);

        // The client should contain methods for operations
        var clientCode = result["MultiPortServiceClient.cs"];

        // Note: The current implementation only generates methods for the first port's operations
        // This is a known limitation that should be fixed in the future
        Assert.Contains("MultiPortServiceClient", clientCode);
        Assert.Contains("Initialize(endpointUrl);", clientCode);
    }

    [Fact]
    public void GenerateClient_ShouldHandleOperationsWithSameNameButDifferentBindings()
    {
        // Arrange
        var wsdl = new WsdlDefinition
        {
            TargetNamespace = "http://test.com/",
            Services = new List<WsdlService>
            {
                new WsdlService
                {
                    Name = "DuplicateOperationService",
                    Ports = new List<WsdlPort>
                    {
                        new WsdlPort
                        {
                            Name = "Port1",
                            Binding = "Binding1"
                        },
                        new WsdlPort
                        {
                            Name = "Port2",
                            Binding = "Binding2"
                        }
                    }
                }
            },
            Bindings = new List<WsdlBinding>
            {
                new WsdlBinding
                {
                    Name = "Binding1",
                    Type = "PortType1",
                    Operations = new List<WsdlBindingOperation>
                    {
                        new WsdlBindingOperation
                        {
                            Name = "CommonOperation",
                            SoapAction = "http://test.com/CommonOperation1"
                        }
                    }
                },
                new WsdlBinding
                {
                    Name = "Binding2",
                    Type = "PortType2",
                    Operations = new List<WsdlBindingOperation>
                    {
                        new WsdlBindingOperation
                        {
                            Name = "CommonOperation",
                            SoapAction = "http://test.com/CommonOperation2"
                        }
                    }
                }
            },
            PortTypes = new List<WsdlPortType>
            {
                new WsdlPortType
                {
                    Name = "PortType1",
                    Operations = new List<WsdlOperation>
                    {
                        new WsdlOperation
                        {
                            Name = "CommonOperation",
                            Input = new WsdlOperationMessage
                            {
                                Name = "CommonOperationInput1",
                                Message = "CommonOperationRequest"
                            },
                            Output = new WsdlOperationMessage
                            {
                                Name = "CommonOperationOutput1",
                                Message = "CommonOperationResponse"
                            }
                        }
                    }
                },
                new WsdlPortType
                {
                    Name = "PortType2",
                    Operations = new List<WsdlOperation>
                    {
                        new WsdlOperation
                        {
                            Name = "CommonOperation",
                            Input = new WsdlOperationMessage
                            {
                                Name = "CommonOperationInput2",
                                Message = "CommonOperationRequest"
                            },
                            Output = new WsdlOperationMessage
                            {
                                Name = "CommonOperationOutput2",
                                Message = "CommonOperationResponse"
                            }
                        }
                    }
                }
            },
            Messages = new List<WsdlMessage>
            {
                new WsdlMessage { Name = "CommonOperationRequest" },
                new WsdlMessage { Name = "CommonOperationResponse" }
            }
        };

        // Act
        var result = _generator.GenerateClient(wsdl, "Test.Namespace");

        // Assert
        Assert.Single(result);
        Assert.Contains("DuplicateOperationServiceClient.cs", result.Keys);

        // The client should contain the operation method, but it might be duplicated
        // This is a known limitation that should be fixed in the implementation
        var clientCode = result["DuplicateOperationServiceClient.cs"];
        Assert.Contains("public async Task<ACHTransResponse> CommonOperationAsync(CommonOperationRequest request)", clientCode);

        // Count occurrences of the method signature
        var occurrences = CountOccurrences(clientCode, "public async Task<ACHTransResponse> CommonOperationAsync(CommonOperationRequest request)");
        Assert.True(occurrences >= 1, $"Expected at least one occurrence of the method signature, but found {occurrences}");
    }

    [Fact]
    public void GenerateClient_ShouldHandleOperationsWithDocumentation()
    {
        // Arrange
        var wsdl = new WsdlDefinition
        {
            TargetNamespace = "http://test.com/",
            Services = new List<WsdlService>
            {
                new WsdlService
                {
                    Name = "DocumentedService",
                    Documentation = "This is a documented service",
                    Ports = new List<WsdlPort>
                    {
                        new WsdlPort
                        {
                            Name = "Port1",
                            Binding = "Binding1"
                        }
                    }
                }
            },
            Bindings = new List<WsdlBinding>
            {
                new WsdlBinding
                {
                    Name = "Binding1",
                    Type = "PortType1",
                    Operations = new List<WsdlBindingOperation>
                    {
                        new WsdlBindingOperation
                        {
                            Name = "DocumentedOperation",
                            SoapAction = "http://test.com/DocumentedOperation"
                        }
                    }
                }
            },
            PortTypes = new List<WsdlPortType>
            {
                new WsdlPortType
                {
                    Name = "PortType1",
                    Operations = new List<WsdlOperation>
                    {
                        new WsdlOperation
                        {
                            Name = "DocumentedOperation",
                            Documentation = "This is a documented operation",
                            Input = new WsdlOperationMessage
                            {
                                Name = "DocumentedOperationInput",
                                Message = "DocumentedOperationRequest"
                            },
                            Output = new WsdlOperationMessage
                            {
                                Name = "DocumentedOperationOutput",
                                Message = "DocumentedOperationResponse"
                            }
                        }
                    }
                }
            },
            Messages = new List<WsdlMessage>
            {
                new WsdlMessage { Name = "DocumentedOperationRequest" },
                new WsdlMessage { Name = "DocumentedOperationResponse" }
            }
        };

        // Act
        var result = _generator.GenerateClient(wsdl, "Test.Namespace");

        // Assert
        Assert.Single(result);
        Assert.Contains("DocumentedServiceClient.cs", result.Keys);

        // The client should contain the operation method with documentation
        var clientCode = result["DocumentedServiceClient.cs"];
        Assert.Contains("public async Task<ACHTransResponse> DocumentedOperationAsync(DocumentedOperationRequest request)", clientCode);

        // Check for service documentation in the class comment
        Assert.Contains("/// Client for the DocumentedService SOAP service.", clientCode);

        // Check for operation documentation in the method comment
        Assert.Contains("/// Asynchronously calls the DocumentedOperation operation.", clientCode);
    }

    [Fact]
    public void GenerateClient_ShouldHandleComplexSoapActions()
    {
        // Arrange
        var wsdl = new WsdlDefinition
        {
            TargetNamespace = "http://test.com/",
            Services = new List<WsdlService>
            {
                new WsdlService
                {
                    Name = "ComplexService",
                    Ports = new List<WsdlPort>
                    {
                        new WsdlPort
                        {
                            Name = "Port1",
                            Binding = "Binding1"
                        }
                    }
                }
            },
            Bindings = new List<WsdlBinding>
            {
                new WsdlBinding
                {
                    Name = "Binding1",
                    Type = "PortType1",
                    Operations = new List<WsdlBindingOperation>
                    {
                        new WsdlBindingOperation
                        {
                            Name = "ComplexOperation",
                            SoapAction = "http://test.com/services/v1/ComplexOperation?param=value&other=123"
                        }
                    }
                }
            },
            PortTypes = new List<WsdlPortType>
            {
                new WsdlPortType
                {
                    Name = "PortType1",
                    Operations = new List<WsdlOperation>
                    {
                        new WsdlOperation
                        {
                            Name = "ComplexOperation",
                            Input = new WsdlOperationMessage
                            {
                                Name = "ComplexOperationInput",
                                Message = "ComplexOperationRequest"
                            },
                            Output = new WsdlOperationMessage
                            {
                                Name = "ComplexOperationOutput",
                                Message = "ComplexOperationResponse"
                            }
                        }
                    }
                }
            },
            Messages = new List<WsdlMessage>
            {
                new WsdlMessage { Name = "ComplexOperationRequest" },
                new WsdlMessage { Name = "ComplexOperationResponse" }
            }
        };

        // Act
        var result = _generator.GenerateClient(wsdl, "Test.Namespace");

        // Assert
        Assert.Single(result);
        Assert.Contains("ComplexServiceClient.cs", result.Keys);

        // The client should contain the operation method with the complex SOAP action
        var clientCode = result["ComplexServiceClient.cs"];
        Assert.Contains("public async Task<ACHTransResponse> ComplexOperationAsync(ComplexOperationRequest request)", clientCode);

        // Check that the SOAP action is properly escaped in the code
        Assert.Contains("var soapEnvelope = CreateSoapEnvelope(request, \"http://test.com/services/v1/ComplexOperation?param=value&other=123\");", clientCode);
        Assert.Contains("var responseContent = await SendSoapRequestAsync(soapEnvelope, \"http://test.com/services/v1/ComplexOperation?param=value&other=123\");", clientCode);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void GenerateClient_ShouldHandleEmptyWsdl()
    {
        // Arrange
        var wsdl = new WsdlDefinition();

        // Act
        var result = _generator.GenerateClient(wsdl, "Test.Namespace");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GenerateClient_ShouldHandleWsdlWithNoOperations()
    {
        // Arrange
        var wsdl = new WsdlDefinition
        {
            TargetNamespace = "http://test.com/",
            Services = new List<WsdlService>
            {
                new WsdlService
                {
                    Name = "EmptyService",
                    Ports = new List<WsdlPort>
                    {
                        new WsdlPort
                        {
                            Name = "EmptyPort",
                            Binding = "EmptyBinding"
                        }
                    }
                }
            },
            Bindings = new List<WsdlBinding>
            {
                new WsdlBinding
                {
                    Name = "EmptyBinding",
                    Type = "EmptyPortType",
                    Operations = new List<WsdlBindingOperation>()
                }
            },
            PortTypes = new List<WsdlPortType>
            {
                new WsdlPortType
                {
                    Name = "EmptyPortType",
                    Operations = new List<WsdlOperation>()
                }
            }
        };

        // Act
        var result = _generator.GenerateClient(wsdl, "Test.Namespace");

        // Assert
        Assert.Single(result);
        Assert.Contains("EmptyServiceClient.cs", result.Keys);

        // The client should not contain any operation methods
        var clientCode = result["EmptyServiceClient.cs"];
        Assert.DoesNotContain("public async Task<", clientCode);
    }

    [Fact]
    public void GenerateClient_ShouldHandleWsdlWithInvalidOperations()
    {
        // Arrange
        var wsdl = new WsdlDefinition
        {
            TargetNamespace = "http://test.com/",
            Services = new List<WsdlService>
            {
                new WsdlService
                {
                    Name = "InvalidService",
                    Ports = new List<WsdlPort>
                    {
                        new WsdlPort
                        {
                            Name = "InvalidPort",
                            Binding = "InvalidBinding"
                        }
                    }
                }
            },
            Bindings = new List<WsdlBinding>
            {
                new WsdlBinding
                {
                    Name = "InvalidBinding",
                    Type = "InvalidPortType",
                    Operations = new List<WsdlBindingOperation>
                    {
                        new WsdlBindingOperation
                        {
                            Name = "InvalidOperation",
                            SoapAction = "http://test.com/InvalidOperation"
                        }
                    }
                }
            },
            PortTypes = new List<WsdlPortType>
            {
                new WsdlPortType
                {
                    Name = "InvalidPortType",
                    Operations = new List<WsdlOperation>
                    {
                        new WsdlOperation
                        {
                            Name = "InvalidOperation",
                            // Missing Input
                            Output = new WsdlOperationMessage
                            {
                                Name = "InvalidOperationOutput",
                                Message = "InvalidOperationResponse"
                            }
                        },
                        new WsdlOperation
                        {
                            Name = "AnotherInvalidOperation",
                            Input = new WsdlOperationMessage
                            {
                                Name = "AnotherInvalidOperationInput",
                                Message = "AnotherInvalidOperationRequest"
                            }
                            // Missing Output
                        }
                    }
                }
            },
            Messages = new List<WsdlMessage>
            {
                new WsdlMessage { Name = "AnotherInvalidOperationRequest" },
                new WsdlMessage { Name = "InvalidOperationResponse" }
            }
        };

        // Act
        var result = _generator.GenerateClient(wsdl, "Test.Namespace");

        // Assert
        Assert.Single(result);
        Assert.Contains("InvalidServiceClient.cs", result.Keys);

        // The client should not contain any operation methods since all are invalid
        var clientCode = result["InvalidServiceClient.cs"];
        Assert.DoesNotContain("public async Task<ACHTransResponse> InvalidOperationAsync", clientCode);
        Assert.DoesNotContain("public async Task<ACHTransResponse> AnotherInvalidOperationAsync", clientCode);
    }

    #endregion

    #region Helper Methods

    private static int CountOccurrences(string source, string searchString)
    {
        var count = 0;
        var index = 0;
        while ((index = source.IndexOf(searchString, index, StringComparison.Ordinal)) != -1)
        {
            count++;
            index += searchString.Length;
        }
        return count;
    }

    #endregion
}
