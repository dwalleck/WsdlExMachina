using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WsdlExMachina.Parser.Models;
using Xunit;

namespace WsdlExMachina.CSharpGenerator.Tests;

public class RoslynClientGeneratorTests
{
    private readonly RoslynClientGenerator _generator;
    private readonly RoslynCodeGenerator _codeGenerator;

    public RoslynClientGeneratorTests()
    {
        _codeGenerator = new RoslynCodeGenerator();
        _generator = new RoslynClientGenerator(_codeGenerator);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenCodeGeneratorIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RoslynClientGenerator(null));
    }

    #endregion

    #region GenerateClient Tests

    [Fact]
    public void GenerateClient_ShouldThrowArgumentNullException_WhenWsdlIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _generator.GenerateClient(null, "Test.Namespace"));
    }

    [Fact]
    public void GenerateClient_ShouldThrowArgumentNullException_WhenNamespaceIsNull()
    {
        // Arrange
        var wsdl = CreateMinimalWsdlDefinition();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _generator.GenerateClient(wsdl, null));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void GenerateClient_ShouldThrowArgumentNullException_WhenNamespaceIsEmpty(string namespaceName)
    {
        // Arrange
        var wsdl = CreateMinimalWsdlDefinition();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _generator.GenerateClient(wsdl, namespaceName));
    }

    [Fact]
    public void GenerateClient_ShouldReturnEmptyDictionary_WhenWsdlHasNoServices()
    {
        // Arrange
        var wsdl = new WsdlDefinition
        {
            Services = new List<WsdlService>()
        };

        // Act
        var result = _generator.GenerateClient(wsdl, "Test.Namespace");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GenerateClient_ShouldReturnEmptyDictionary_WhenServiceHasNoPorts()
    {
        // Arrange
        var wsdl = new WsdlDefinition
        {
            Services = new List<WsdlService>
            {
                new WsdlService
                {
                    Name = "TestService",
                    Ports = new List<WsdlPort>()
                }
            }
        };

        // Act
        var result = _generator.GenerateClient(wsdl, "Test.Namespace");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GenerateClient_ShouldSkipPort_WhenBindingNotFound()
    {
        // Arrange
        var wsdl = new WsdlDefinition
        {
            Services = new List<WsdlService>
            {
                new WsdlService
                {
                    Name = "TestService",
                    Ports = new List<WsdlPort>
                    {
                        new WsdlPort
                        {
                            Name = "TestPort",
                            Binding = "NonExistentBinding"
                        }
                    }
                }
            },
            Bindings = new List<WsdlBinding>()
        };

        // Act
        var result = _generator.GenerateClient(wsdl, "Test.Namespace");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GenerateClient_ShouldSkipPort_WhenPortTypeNotFound()
    {
        // Arrange
        var wsdl = new WsdlDefinition
        {
            Services = new List<WsdlService>
            {
                new WsdlService
                {
                    Name = "TestService",
                    Ports = new List<WsdlPort>
                    {
                        new WsdlPort
                        {
                            Name = "TestPort",
                            Binding = "TestBinding"
                        }
                    }
                }
            },
            Bindings = new List<WsdlBinding>
            {
                new WsdlBinding
                {
                    Name = "TestBinding",
                    Type = "NonExistentPortType"
                }
            },
            PortTypes = new List<WsdlPortType>()
        };

        // Act
        var result = _generator.GenerateClient(wsdl, "Test.Namespace");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GenerateClient_ShouldGenerateClientForEachService()
    {
        // Arrange
        var wsdl = CreateTestWsdlDefinition();

        // Act
        var result = _generator.GenerateClient(wsdl, "Test.Namespace");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains("Service1Client.cs", result.Keys);
        Assert.Contains("Service2Client.cs", result.Keys);
    }

    #endregion

    #region Generated Code Tests

    [Fact]
    public void GenerateClient_ShouldGenerateClientWithCorrectNamespace()
    {
        // Arrange
        var wsdl = CreateTestWsdlDefinition();
        var namespaceName = "Test.CustomNamespace";

        // Act
        var result = _generator.GenerateClient(wsdl, namespaceName);

        // Assert
        foreach (var code in result.Values)
        {
            Assert.Contains($"namespace {namespaceName}", code);
        }
    }

    [Fact]
    public void GenerateClient_ShouldGenerateClientWithCorrectClassName()
    {
        // Arrange
        var wsdl = CreateTestWsdlDefinition();

        // Act
        var result = _generator.GenerateClient(wsdl, "Test.Namespace");

        // Assert
        Assert.Contains("public class Service1Client : SoapClientBase", result["Service1Client.cs"]);
        Assert.Contains("public class Service2Client : SoapClientBase", result["Service2Client.cs"]);
    }

    [Fact]
    public void GenerateClient_ShouldGenerateClientWithConstructor()
    {
        // Arrange
        var wsdl = CreateTestWsdlDefinition();

        // Act
        var result = _generator.GenerateClient(wsdl, "Test.Namespace");

        // Assert
        Assert.Contains("public Service1Client(string endpointUrl)", result["Service1Client.cs"]);
        Assert.Contains("Initialize(endpointUrl);", result["Service1Client.cs"]);
    }

    [Fact]
    public void GenerateClient_ShouldGenerateAsyncMethodsForOperations()
    {
        // Arrange
        var wsdl = CreateTestWsdlDefinition();

        // Act
        var result = _generator.GenerateClient(wsdl, "Test.Namespace");

        // Assert
        Assert.Contains("public async Task<ACHTransResponse> Operation1Async(Operation1Request request)", result["Service1Client.cs"]);
        Assert.Contains("public async Task<ACHTransResponse> Operation2Async(Operation2Request request)", result["Service1Client.cs"]);
    }

    [Fact]
    public void GenerateClient_ShouldGenerateMethodsWithNullCheck()
    {
        // Arrange
        var wsdl = CreateTestWsdlDefinition();

        // Act
        var result = _generator.GenerateClient(wsdl, "Test.Namespace");

        // Assert
        Assert.Contains("if (request == null)", result["Service1Client.cs"]);
        Assert.Contains("throw new ArgumentNullException(nameof(request));", result["Service1Client.cs"]);
    }

    [Fact]
    public void GenerateClient_ShouldGenerateMethodsWithSoapEnvelopeCreation()
    {
        // Arrange
        var wsdl = CreateTestWsdlDefinition();

        // Act
        var result = _generator.GenerateClient(wsdl, "Test.Namespace");

        // Assert
        Assert.Contains("var soapEnvelope = CreateSoapEnvelope(request, \"http://test.com/Operation1\");", result["Service1Client.cs"]);
        Assert.Contains("var responseContent = await SendSoapRequestAsync(soapEnvelope, \"http://test.com/Operation1\");", result["Service1Client.cs"]);
        Assert.Contains("return DeserializeResponse<ACHTransResponse>(responseContent);", result["Service1Client.cs"]);
    }

    [Fact]
    public void GenerateClient_ShouldSkipOperationsWithMissingMessages()
    {
        // Arrange
        var wsdl = CreateWsdlWithMissingMessages();

        // Act
        var result = _generator.GenerateClient(wsdl, "Test.Namespace");

        // Assert
        Assert.Contains("Service1Client.cs", result.Keys);
        Assert.DoesNotContain("public async Task<ACHTransResponse> MissingInputAsync", result["Service1Client.cs"]);
        Assert.DoesNotContain("public async Task<ACHTransResponse> MissingOutputAsync", result["Service1Client.cs"]);
        Assert.Contains("public async Task<ACHTransResponse> ValidOperationAsync", result["Service1Client.cs"]);
    }

    [Fact]
    public void GenerateClient_ShouldSkipOperationsWithMissingBindingOperations()
    {
        // Arrange
        var wsdl = CreateWsdlWithMissingBindingOperations();

        // Act
        var result = _generator.GenerateClient(wsdl, "Test.Namespace");

        // Assert
        Assert.Contains("Service1Client.cs", result.Keys);
        Assert.DoesNotContain("public async Task<ACHTransResponse> MissingBindingOperationAsync", result["Service1Client.cs"]);
        Assert.Contains("public async Task<ACHTransResponse> ValidOperationAsync", result["Service1Client.cs"]);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void GenerateClient_ShouldHandleEmptyOperationsList()
    {
        // Arrange
        var wsdl = new WsdlDefinition
        {
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
                    Type = "EmptyPortType"
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
        Assert.Contains("EmptyServiceClient.cs", result.Keys);
        Assert.Contains("public class EmptyServiceClient : SoapClientBase", result["EmptyServiceClient.cs"]);
        Assert.DoesNotContain("public async Task<", result["EmptyServiceClient.cs"]);
    }

    [Fact]
    public void GenerateClient_ShouldHandleNullOperationsList()
    {
        // Arrange
        var wsdl = new WsdlDefinition
        {
            Services = new List<WsdlService>
            {
                new WsdlService
                {
                    Name = "NullService",
                    Ports = new List<WsdlPort>
                    {
                        new WsdlPort
                        {
                            Name = "NullPort",
                            Binding = "NullBinding"
                        }
                    }
                }
            },
            Bindings = new List<WsdlBinding>
            {
                new WsdlBinding
                {
                    Name = "NullBinding",
                    Type = "NullPortType"
                }
            },
            PortTypes = new List<WsdlPortType>
            {
                new WsdlPortType
                {
                    Name = "NullPortType",
                    Operations = null
                }
            }
        };

        // Act
        var result = _generator.GenerateClient(wsdl, "Test.Namespace");

        // Assert
        Assert.Contains("NullServiceClient.cs", result.Keys);
        Assert.Contains("public class NullServiceClient : SoapClientBase", result["NullServiceClient.cs"]);
        Assert.DoesNotContain("public async Task<", result["NullServiceClient.cs"]);
    }

    [Fact]
    public void GenerateClient_ShouldHandleNullServicesList()
    {
        // Arrange
        var wsdl = new WsdlDefinition
        {
            Services = null
        };

        // Act
        var result = _generator.GenerateClient(wsdl, "Test.Namespace");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GenerateClient_ShouldHandleNullPortsList()
    {
        // Arrange
        var wsdl = new WsdlDefinition
        {
            Services = new List<WsdlService>
            {
                new WsdlService
                {
                    Name = "NullPortsService",
                    Ports = null
                }
            }
        };

        // Act
        var result = _generator.GenerateClient(wsdl, "Test.Namespace");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GenerateClient_ShouldHandleNullBindingsList()
    {
        // Arrange
        var wsdl = new WsdlDefinition
        {
            Services = new List<WsdlService>
            {
                new WsdlService
                {
                    Name = "TestService",
                    Ports = new List<WsdlPort>
                    {
                        new WsdlPort
                        {
                            Name = "TestPort",
                            Binding = "TestBinding"
                        }
                    }
                }
            },
            Bindings = null
        };

        // Act
        var result = _generator.GenerateClient(wsdl, "Test.Namespace");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GenerateClient_ShouldHandleNullPortTypesList()
    {
        // Arrange
        var wsdl = new WsdlDefinition
        {
            Services = new List<WsdlService>
            {
                new WsdlService
                {
                    Name = "TestService",
                    Ports = new List<WsdlPort>
                    {
                        new WsdlPort
                        {
                            Name = "TestPort",
                            Binding = "TestBinding"
                        }
                    }
                }
            },
            Bindings = new List<WsdlBinding>
            {
                new WsdlBinding
                {
                    Name = "TestBinding",
                    Type = "TestPortType"
                }
            },
            PortTypes = null
        };

        // Act
        var result = _generator.GenerateClient(wsdl, "Test.Namespace");

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region Helper Methods

    private static WsdlDefinition CreateMinimalWsdlDefinition()
    {
        return new WsdlDefinition
        {
            TargetNamespace = "http://test.com/",
            Services = new List<WsdlService>()
        };
    }

    private static WsdlDefinition CreateTestWsdlDefinition()
    {
        return new WsdlDefinition
        {
            TargetNamespace = "http://test.com/",
            Services = new List<WsdlService>
            {
                new WsdlService
                {
                    Name = "Service1",
                    Ports = new List<WsdlPort>
                    {
                        new WsdlPort
                        {
                            Name = "Port1",
                            Binding = "Binding1"
                        }
                    }
                },
                new WsdlService
                {
                    Name = "Service2",
                    Ports = new List<WsdlPort>
                    {
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
                        },
                        new WsdlBindingOperation
                        {
                            Name = "Operation2",
                            SoapAction = "http://test.com/Operation2"
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
                            Name = "Operation3",
                            SoapAction = "http://test.com/Operation3"
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
                        },
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
                },
                new WsdlPortType
                {
                    Name = "PortType2",
                    Operations = new List<WsdlOperation>
                    {
                        new WsdlOperation
                        {
                            Name = "Operation3",
                            Input = new WsdlOperationMessage
                            {
                                Name = "Operation3Input",
                                Message = "Operation3Request"
                            },
                            Output = new WsdlOperationMessage
                            {
                                Name = "Operation3Output",
                                Message = "Operation3Response"
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
                new WsdlMessage { Name = "Operation2Response" },
                new WsdlMessage { Name = "Operation3Request" },
                new WsdlMessage { Name = "Operation3Response" }
            }
        };
    }

    private static WsdlDefinition CreateWsdlWithMissingMessages()
    {
        return new WsdlDefinition
        {
            TargetNamespace = "http://test.com/",
            Services = new List<WsdlService>
            {
                new WsdlService
                {
                    Name = "Service1",
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
                            Name = "MissingInput",
                            SoapAction = "http://test.com/MissingInput"
                        },
                        new WsdlBindingOperation
                        {
                            Name = "MissingOutput",
                            SoapAction = "http://test.com/MissingOutput"
                        },
                        new WsdlBindingOperation
                        {
                            Name = "ValidOperation",
                            SoapAction = "http://test.com/ValidOperation"
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
                            Name = "MissingInput",
                            Input = new WsdlOperationMessage
                            {
                                Name = "MissingInputInput",
                                Message = "MissingInputRequest" // This message doesn't exist
                            },
                            Output = new WsdlOperationMessage
                            {
                                Name = "MissingInputOutput",
                                Message = "MissingInputResponse"
                            }
                        },
                        new WsdlOperation
                        {
                            Name = "MissingOutput",
                            Input = new WsdlOperationMessage
                            {
                                Name = "MissingOutputInput",
                                Message = "MissingOutputRequest"
                            },
                            Output = new WsdlOperationMessage
                            {
                                Name = "MissingOutputOutput",
                                Message = "MissingOutputResponse" // This message doesn't exist
                            }
                        },
                        new WsdlOperation
                        {
                            Name = "ValidOperation",
                            Input = new WsdlOperationMessage
                            {
                                Name = "ValidOperationInput",
                                Message = "ValidOperationRequest"
                            },
                            Output = new WsdlOperationMessage
                            {
                                Name = "ValidOperationOutput",
                                Message = "ValidOperationResponse"
                            }
                        }
                    }
                }
            },
            Messages = new List<WsdlMessage>
            {
                // Missing MissingInputRequest
                new WsdlMessage { Name = "MissingInputResponse" },
                new WsdlMessage { Name = "MissingOutputRequest" },
                // Missing MissingOutputResponse
                new WsdlMessage { Name = "ValidOperationRequest" },
                new WsdlMessage { Name = "ValidOperationResponse" }
            }
        };
    }

    private static WsdlDefinition CreateWsdlWithMissingBindingOperations()
    {
        return new WsdlDefinition
        {
            TargetNamespace = "http://test.com/",
            Services = new List<WsdlService>
            {
                new WsdlService
                {
                    Name = "Service1",
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
                        // Missing binding operation for "MissingBindingOperation"
                        new WsdlBindingOperation
                        {
                            Name = "ValidOperation",
                            SoapAction = "http://test.com/ValidOperation"
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
                            Name = "MissingBindingOperation",
                            Input = new WsdlOperationMessage
                            {
                                Name = "MissingBindingOperationInput",
                                Message = "MissingBindingOperationRequest"
                            },
                            Output = new WsdlOperationMessage
                            {
                                Name = "MissingBindingOperationOutput",
                                Message = "MissingBindingOperationResponse"
                            }
                        },
                        new WsdlOperation
                        {
                            Name = "ValidOperation",
                            Input = new WsdlOperationMessage
                            {
                                Name = "ValidOperationInput",
                                Message = "ValidOperationRequest"
                            },
                            Output = new WsdlOperationMessage
                            {
                                Name = "ValidOperationOutput",
                                Message = "ValidOperationResponse"
                            }
                        }
                    }
                }
            },
            Messages = new List<WsdlMessage>
            {
                new WsdlMessage { Name = "MissingBindingOperationRequest" },
                new WsdlMessage { Name = "MissingBindingOperationResponse" },
                new WsdlMessage { Name = "ValidOperationRequest" },
                new WsdlMessage { Name = "ValidOperationResponse" }
            }
        };
    }

    #endregion
}
