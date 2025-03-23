using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;
using WsdlExMachina.Parser;
using WsdlExMachina.Parser.Models;

namespace WsdlExMachina.Parser.Tests;

public class WsdlParserComprehensiveTests
{
    private readonly string _sampleWsdlPath = Path.Combine("..", "..", "..", "..", "..", "samples", "ACH.wsdl");

    #region Public Methods Tests

    [Fact]
    public void ParseFile_ValidWsdl_ReturnsWsdlDefinition()
    {
        // Arrange
        var parser = new WsdlParser();

        // Act
        var wsdlDefinition = parser.ParseFile(_sampleWsdlPath);

        // Assert
        Assert.NotNull(wsdlDefinition);
        Assert.Equal("http://www.swbc.com/", wsdlDefinition.TargetNamespace);
    }

    [Fact]
    public void ParseFile_FileNotFound_ThrowsFileNotFoundException()
    {
        // Arrange
        var parser = new WsdlParser();
        var nonExistentFilePath = "non_existent_file.wsdl";

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => parser.ParseFile(nonExistentFilePath));
    }

    [Fact]
    public void ParseXml_ValidWsdlXml_ReturnsWsdlDefinition()
    {
        // Arrange
        var parser = new WsdlParser();
        var wsdlXml = File.ReadAllText(_sampleWsdlPath);

        // Act
        var wsdlDefinition = parser.ParseXml(wsdlXml);

        // Assert
        Assert.NotNull(wsdlDefinition);
        Assert.Equal("http://www.swbc.com/", wsdlDefinition.TargetNamespace);
    }

    [Fact]
    public void ParseXml_InvalidXml_ThrowsXmlException()
    {
        // Arrange
        var parser = new WsdlParser();
        var invalidXml = "<invalid>xml</invalid";

        // Act & Assert
        Assert.Throws<System.Xml.XmlException>(() => parser.ParseXml(invalidXml));
    }

    [Fact]
    public void Parse_NullDocument_ThrowsArgumentNullException()
    {
        // Arrange
        var parser = new WsdlParser();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => parser.Parse(null));
    }

    [Fact]
    public void Parse_NotWsdlDocument_ThrowsInvalidOperationException()
    {
        // Arrange
        var parser = new WsdlParser();
        var notWsdlDocument = XDocument.Parse("<root><child>value</child></root>");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => parser.Parse(notWsdlDocument));
    }

    #endregion

    #region Namespace Handling Tests

    [Fact]
    public void Parse_CorrectlyHandlesNamespaces()
    {
        // Arrange
        var parser = new WsdlParser();
        var wsdlXml = File.ReadAllText(_sampleWsdlPath);

        // Act
        var wsdlDefinition = parser.ParseXml(wsdlXml);

        // Assert
        Assert.NotNull(wsdlDefinition.Namespaces);
        Assert.True(wsdlDefinition.Namespaces.Count > 0);
        Assert.Equal("http://www.w3.org/2001/XMLSchema", wsdlDefinition.Namespaces["s"]);
        Assert.Equal("http://schemas.xmlsoap.org/wsdl/soap12/", wsdlDefinition.Namespaces["soap12"]);
        Assert.Equal("http://www.swbc.com/", wsdlDefinition.Namespaces["tns"]);
    }

    [Fact]
    public void Parse_CorrectlyHandlesDefaultNamespace()
    {
        // Arrange
        var parser = new WsdlParser();
        var wsdlWithDefaultNamespace = @"
            <definitions xmlns=""http://schemas.xmlsoap.org/wsdl/""
                         xmlns:s=""http://www.w3.org/2001/XMLSchema""
                         targetNamespace=""http://example.com/"">
                <types></types>
            </definitions>";

        // Act
        var wsdlDefinition = parser.ParseXml(wsdlWithDefaultNamespace);

        // Assert
        Assert.NotNull(wsdlDefinition.Namespaces);
        Assert.True(wsdlDefinition.Namespaces.ContainsKey(string.Empty));
        Assert.Equal("http://schemas.xmlsoap.org/wsdl/", wsdlDefinition.Namespaces[string.Empty]);
    }

    #endregion

    #region QualifiedName Handling Tests

    [Fact]
    public void Parse_CorrectlyHandlesQualifiedNames()
    {
        // Arrange
        var parser = new WsdlParser();

        // Act
        var wsdlDefinition = parser.ParseFile(_sampleWsdlPath);

        // Assert - Check a complex type with qualified name reference
        var complexType = wsdlDefinition.Types.ComplexTypes.Find(ct => ct.Name == "SWBCAuthHeader");
        Assert.NotNull(complexType);

        // Check a message with qualified name reference
        var message = wsdlDefinition.Messages.Find(m => m.Name == "PostSinglePaymentSoapIn");
        Assert.NotNull(message);
        Assert.Single(message.Parts);
        Assert.Equal("PostSinglePayment", message.Parts[0].Element);
        Assert.Equal("http://www.swbc.com/", message.Parts[0].ElementNamespace);
    }

    [Fact]
    public void Parse_HandlesUnqualifiedNames()
    {
        // Arrange
        var parser = new WsdlParser();
        var wsdlWithUnqualifiedNames = @"
            <definitions xmlns=""http://schemas.xmlsoap.org/wsdl/""
                         xmlns:s=""http://www.w3.org/2001/XMLSchema""
                         xmlns:tns=""http://example.com/""
                         targetNamespace=""http://example.com/"">
                <types>
                    <s:schema targetNamespace=""http://example.com/"">
                        <s:element name=""TestElement"">
                            <s:complexType>
                                <s:sequence>
                                    <s:element name=""Field"" type=""string"" />
                                </s:sequence>
                            </s:complexType>
                        </s:element>
                    </s:schema>
                </types>
            </definitions>";

        // Act
        var wsdlDefinition = parser.ParseXml(wsdlWithUnqualifiedNames);

        // Assert
        Assert.NotNull(wsdlDefinition.Types);
        Assert.NotEmpty(wsdlDefinition.Types.Elements);
        var element = wsdlDefinition.Types.Elements.FirstOrDefault(e => e.Name == "TestElement");
        Assert.NotNull(element);
    }

    #endregion

    #region Types Parsing Tests

    [Fact]
    public void Parse_CorrectlyParsesComplexTypes()
    {
        // Arrange
        var parser = new WsdlParser();

        // Act
        var wsdlDefinition = parser.ParseFile(_sampleWsdlPath);

        // Assert
        Assert.NotNull(wsdlDefinition.Types);
        Assert.NotEmpty(wsdlDefinition.Types.ComplexTypes);

        // Check a specific complex type
        var complexType = wsdlDefinition.Types.ComplexTypes.Find(ct => ct.Name == "ACHTransResponse");
        Assert.NotNull(complexType);
        Assert.Equal(3, complexType.Elements.Count);
        Assert.Equal("ResponseCode", complexType.Elements[0].Name);
        Assert.Equal("ResponseMessage", complexType.Elements[1].Name);
        Assert.Equal("ResponseStringRaw", complexType.Elements[2].Name);
    }

    [Fact]
    public void Parse_CorrectlyParsesSimpleTypes()
    {
        // Arrange
        var parser = new WsdlParser();

        // Act
        var wsdlDefinition = parser.ParseFile(_sampleWsdlPath);

        // Assert
        Assert.NotNull(wsdlDefinition.Types);
        Assert.NotEmpty(wsdlDefinition.Types.SimpleTypes);

        // Check a specific simple type
        var simpleType = wsdlDefinition.Types.SimpleTypes.Find(st => st.Name == "PaymentSource");
        Assert.NotNull(simpleType);
        Assert.Equal("string", simpleType.BaseType);
        Assert.True(simpleType.EnumerationValues.Count > 0);
        Assert.Contains("OnlineAkcelerant", simpleType.EnumerationValues);
    }

    [Fact]
    public void Parse_CorrectlyParsesElements()
    {
        // Arrange
        var parser = new WsdlParser();

        // Act
        var wsdlDefinition = parser.ParseFile(_sampleWsdlPath);

        // Assert
        Assert.NotNull(wsdlDefinition.Types);
        Assert.NotEmpty(wsdlDefinition.Types.Elements);

        // Check a specific element
        var element = wsdlDefinition.Types.Elements.Find(e => e.Name == "PostSinglePayment");
        Assert.NotNull(element);
        Assert.True(element.IsComplexType);
    }

    [Fact]
    public void Parse_CorrectlyHandlesMinMaxOccurs()
    {
        // Arrange
        var parser = new WsdlParser();

        // Act
        var wsdlDefinition = parser.ParseFile(_sampleWsdlPath);

        // Assert
        var complexType = wsdlDefinition.Types.ComplexTypes.Find(ct => ct.Name == "ACHTransResponse");
        Assert.NotNull(complexType);

        // Check minOccurs=0 (optional)
        var responseCodeElement = complexType.Elements.Find(e => e.Name == "ResponseCode");
        Assert.NotNull(responseCodeElement);
        Assert.True(responseCodeElement.IsOptional);
        Assert.Equal(0, responseCodeElement.MinOccurs);

        // Find an element with maxOccurs > 1 (array)
        var arrayElement = wsdlDefinition.Types.ComplexTypes
            .SelectMany(ct => ct.Elements)
            .FirstOrDefault(e => e.IsArray);

        if (arrayElement != null)
        {
            Assert.True(arrayElement.MaxOccurs > 1);
        }
    }

    #endregion

    #region Messages Parsing Tests

    [Fact]
    public void Parse_CorrectlyParsesMessages()
    {
        // Arrange
        var parser = new WsdlParser();

        // Act
        var wsdlDefinition = parser.ParseFile(_sampleWsdlPath);

        // Assert
        Assert.NotNull(wsdlDefinition.Messages);
        Assert.NotEmpty(wsdlDefinition.Messages);

        // Check a specific message
        var message = wsdlDefinition.Messages.Find(m => m.Name == "PostSinglePaymentSoapIn");
        Assert.NotNull(message);
        Assert.Single(message.Parts);
        Assert.Equal("parameters", message.Parts[0].Name);
        Assert.Equal("PostSinglePayment", message.Parts[0].Element);
    }

    #endregion

    #region PortTypes Parsing Tests

    [Fact]
    public void Parse_CorrectlyParsesPortTypes()
    {
        // Arrange
        var parser = new WsdlParser();

        // Act
        var wsdlDefinition = parser.ParseFile(_sampleWsdlPath);

        // Assert
        Assert.NotNull(wsdlDefinition.PortTypes);
        Assert.NotEmpty(wsdlDefinition.PortTypes);

        // Check a specific port type
        var portType = wsdlDefinition.PortTypes.Find(pt => pt.Name == "ACHTransactionSoap");
        Assert.NotNull(portType);
        Assert.NotEmpty(portType.Operations);

        // Check a specific operation
        var operation = portType.Operations.Find(o => o.Name == "PostSinglePayment");
        Assert.NotNull(operation);
        Assert.NotNull(operation.Input);
        Assert.NotNull(operation.Output);
    }

    #endregion

    #region Bindings Parsing Tests

    [Fact]
    public void Parse_CorrectlyParsesBindings()
    {
        // Arrange
        var parser = new WsdlParser();

        // Act
        var wsdlDefinition = parser.ParseFile(_sampleWsdlPath);

        // Assert
        Assert.NotNull(wsdlDefinition.Bindings);
        Assert.NotEmpty(wsdlDefinition.Bindings);

        // Check a specific binding
        var binding = wsdlDefinition.Bindings.Find(b => b.Name == "ACHTransactionSoap");
        Assert.NotNull(binding);
        Assert.Equal("ACHTransactionSoap", binding.Type);
        Assert.Equal("http://www.swbc.com/", binding.TypeNamespace);
        Assert.Equal("1.1", binding.SoapVersion);
        Assert.Equal("http://schemas.xmlsoap.org/soap/http", binding.Transport);
        Assert.Equal("document", binding.Style);
    }

    [Fact]
    public void Parse_CorrectlyParsesBindingOperations()
    {
        // Arrange
        var parser = new WsdlParser();

        // Act
        var wsdlDefinition = parser.ParseFile(_sampleWsdlPath);

        // Assert
        var binding = wsdlDefinition.Bindings.Find(b => b.Name == "ACHTransactionSoap");
        Assert.NotNull(binding);
        Assert.NotEmpty(binding.Operations);

        // Check a specific binding operation
        var operation = binding.Operations.Find(o => o.Name == "PostSinglePayment");
        Assert.NotNull(operation);
        Assert.Equal("http://www.swbc.com/PostSinglePayment", operation.SoapAction);
        Assert.Equal("document", operation.Style);
    }

    #endregion

    #region Services Parsing Tests

    [Fact]
    public void Parse_CorrectlyParsesServices()
    {
        // Arrange
        var parser = new WsdlParser();

        // Act
        var wsdlDefinition = parser.ParseFile(_sampleWsdlPath);

        // Assert
        Assert.NotNull(wsdlDefinition.Services);
        Assert.NotEmpty(wsdlDefinition.Services);

        // Check a specific service
        var service = wsdlDefinition.Services.Find(s => s.Name == "ACHTransaction");
        Assert.NotNull(service);
        Assert.NotEmpty(service.Ports);

        // Check a specific port
        var port = service.Ports.Find(p => p.Name == "ACHTransactionSoap");
        Assert.NotNull(port);
        Assert.Equal("ACHTransactionSoap", port.Binding);
        Assert.Equal("http://www.swbc.com/", port.BindingNamespace);
        Assert.Equal("http://localhost:2785/ach.asmx", port.Location);
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public void Parse_EmptyWsdl_ReturnsMinimalWsdlDefinition()
    {
        // Arrange
        var parser = new WsdlParser();
        var emptyWsdl = "<definitions xmlns=\"http://schemas.xmlsoap.org/wsdl/\"></definitions>";

        // Act & Assert
        // This should not throw an exception, but return a minimal WSDL definition
        var result = parser.ParseXml(emptyWsdl);
        Assert.NotNull(result);
        Assert.Empty(result.Messages);
        Assert.Empty(result.PortTypes);
        Assert.Empty(result.Bindings);
        Assert.Empty(result.Services);
    }

    [Fact]
    public void Parse_MissingTargetNamespace_ReturnsEmptyString()
    {
        // Arrange
        var parser = new WsdlParser();
        var wsdlWithoutTargetNamespace = @"
            <definitions xmlns=""http://schemas.xmlsoap.org/wsdl/"">
                <types></types>
            </definitions>";

        // Act
        var wsdlDefinition = parser.ParseXml(wsdlWithoutTargetNamespace);

        // Assert
        Assert.NotNull(wsdlDefinition);
        Assert.Equal(string.Empty, wsdlDefinition.TargetNamespace);
    }

    [Fact]
    public void Parse_MissingTypes_ReturnsNullTypes()
    {
        // Arrange
        var parser = new WsdlParser();
        var wsdlWithoutTypes = @"
            <definitions xmlns=""http://schemas.xmlsoap.org/wsdl/""
                         targetNamespace=""http://example.com/"">
            </definitions>";

        // Act
        var wsdlDefinition = parser.ParseXml(wsdlWithoutTypes);

        // Assert
        Assert.NotNull(wsdlDefinition);
        Assert.Null(wsdlDefinition.Types);
    }

    [Fact]
    public void Parse_WithUnknownNamespacePrefix_HandlesGracefully()
    {
        // Arrange
        var parser = new WsdlParser();
        var wsdlWithUnknownPrefix = @"
            <definitions xmlns=""http://schemas.xmlsoap.org/wsdl/""
                         xmlns:s=""http://www.w3.org/2001/XMLSchema""
                         targetNamespace=""http://example.com/"">
                <types>
                    <s:schema targetNamespace=""http://example.com/"">
                        <s:element name=""TestElement"" type=""unknown:TestType"" />
                    </s:schema>
                </types>
            </definitions>";

        // Act
        var wsdlDefinition = parser.ParseXml(wsdlWithUnknownPrefix);

        // Assert
        Assert.NotNull(wsdlDefinition.Types);
        Assert.NotEmpty(wsdlDefinition.Types.Elements);
        var element = wsdlDefinition.Types.Elements.FirstOrDefault(e => e.Name == "TestElement");
        Assert.NotNull(element);
        Assert.Equal("TestType", element.Type);
        Assert.Equal(string.Empty, element.TypeNamespace); // Unknown prefix results in empty namespace
    }

    [Fact]
    public void Parse_WithEmptyQualifiedName_HandlesGracefully()
    {
        // Arrange
        var parser = new WsdlParser();
        var wsdlWithEmptyQualifiedName = @"
            <definitions xmlns=""http://schemas.xmlsoap.org/wsdl/""
                         xmlns:s=""http://www.w3.org/2001/XMLSchema""
                         targetNamespace=""http://example.com/"">
                <types>
                    <s:schema targetNamespace=""http://example.com/"">
                        <s:element name=""TestElement"" type="""" />
                    </s:schema>
                </types>
            </definitions>";

        // Act
        var wsdlDefinition = parser.ParseXml(wsdlWithEmptyQualifiedName);

        // Assert
        Assert.NotNull(wsdlDefinition.Types);
        Assert.NotEmpty(wsdlDefinition.Types.Elements);
        var element = wsdlDefinition.Types.Elements.FirstOrDefault(e => e.Name == "TestElement");
        Assert.NotNull(element);
        Assert.Equal(string.Empty, element.Type);
        Assert.Equal("http://example.com/", element.TypeNamespace); // Default namespace is used
    }

    [Fact]
    public void Parse_WithMalformedQualifiedName_HandlesGracefully()
    {
        // Arrange
        var parser = new WsdlParser();
        var wsdlWithMalformedQualifiedName = @"
            <definitions xmlns=""http://schemas.xmlsoap.org/wsdl/""
                         xmlns:s=""http://www.w3.org/2001/XMLSchema""
                         xmlns:tns=""http://example.com/""
                         targetNamespace=""http://example.com/"">
                <types>
                    <s:schema targetNamespace=""http://example.com/"">
                        <s:element name=""TestElement"" type=""tns:Type:With:Extra:Colons"" />
                    </s:schema>
                </types>
            </definitions>";

        // Act
        var wsdlDefinition = parser.ParseXml(wsdlWithMalformedQualifiedName);

        // Assert
        Assert.NotNull(wsdlDefinition.Types);
        Assert.NotEmpty(wsdlDefinition.Types.Elements);
        var element = wsdlDefinition.Types.Elements.FirstOrDefault(e => e.Name == "TestElement");
        Assert.NotNull(element);
        // The ParseQualifiedName method should take the first part as the prefix and the rest as the local name
        Assert.Equal("Type:With:Extra:Colons", element.Type);
        Assert.Equal("http://example.com/", element.TypeNamespace);
    }

    #endregion

    #region Helper Methods Tests

    [Fact]
    public void ParseQualifiedName_WithPrefix_ReturnsCorrectParts()
    {
        // This test indirectly tests the ParseQualifiedName method through the public API

        // Arrange
        var parser = new WsdlParser();
        var wsdlXml = @"
            <definitions xmlns=""http://schemas.xmlsoap.org/wsdl/""
                         xmlns:tns=""http://example.com/""
                         targetNamespace=""http://example.com/"">
                <types>
                    <schema xmlns=""http://www.w3.org/2001/XMLSchema"" targetNamespace=""http://example.com/"">
                        <element name=""TestElement"" type=""tns:TestType"" />
                    </schema>
                </types>
            </definitions>";

        // Act
        var wsdlDefinition = parser.ParseXml(wsdlXml);

        // Assert
        Assert.NotNull(wsdlDefinition.Types);
        Assert.NotEmpty(wsdlDefinition.Types.Elements);
        var element = wsdlDefinition.Types.Elements.FirstOrDefault(e => e.Name == "TestElement");
        Assert.NotNull(element);
        Assert.Equal("TestType", element.Type);
        Assert.Equal("http://example.com/", element.TypeNamespace);
    }

    [Fact]
    public void ParseQualifiedName_WithoutPrefix_UsesDefaultNamespace()
    {
        // This test indirectly tests the ParseQualifiedName method through the public API

        // Arrange
        var parser = new WsdlParser();
        var wsdlXml = @"
            <definitions xmlns=""http://schemas.xmlsoap.org/wsdl/""
                         xmlns:s=""http://www.w3.org/2001/XMLSchema""
                         targetNamespace=""http://example.com/"">
                <types>
                    <s:schema targetNamespace=""http://example.com/"">
                        <s:element name=""TestElement"" type=""string"" />
                    </s:schema>
                </types>
            </definitions>";

        // Act
        var wsdlDefinition = parser.ParseXml(wsdlXml);

        // Assert
        Assert.NotNull(wsdlDefinition.Types);
        Assert.NotEmpty(wsdlDefinition.Types.Elements);
        var element = wsdlDefinition.Types.Elements.FirstOrDefault(e => e.Name == "TestElement");
        Assert.NotNull(element);
        Assert.Equal("string", element.Type);
        Assert.Equal("http://example.com/", element.TypeNamespace); // Default namespace is the schema's targetNamespace
    }

    [Fact]
    public void ParseQualifiedName_WithUnknownPrefix_ReturnsEmptyNamespace()
    {
        // This test indirectly tests the ParseQualifiedName method through the public API

        // Arrange
        var parser = new WsdlParser();
        var wsdlXml = @"
            <definitions xmlns=""http://schemas.xmlsoap.org/wsdl/""
                         xmlns:s=""http://www.w3.org/2001/XMLSchema""
                         targetNamespace=""http://example.com/"">
                <types>
                    <s:schema targetNamespace=""http://example.com/"">
                        <s:element name=""TestElement"" type=""unknown:TestType"" />
                    </s:schema>
                </types>
            </definitions>";

        // Act
        var wsdlDefinition = parser.ParseXml(wsdlXml);

        // Assert
        Assert.NotNull(wsdlDefinition.Types);
        Assert.NotEmpty(wsdlDefinition.Types.Elements);
        var element = wsdlDefinition.Types.Elements.FirstOrDefault(e => e.Name == "TestElement");
        Assert.NotNull(element);
        Assert.Equal("TestType", element.Type);
        Assert.Equal(string.Empty, element.TypeNamespace); // Unknown prefix results in empty namespace
    }

    [Fact]
    public void ProcessQualifiedAttribute_WithNullAttribute_DoesNotSetProperties()
    {
        // This test indirectly tests the ProcessQualifiedAttribute method through the public API

        // Arrange
        var parser = new WsdlParser();
        var wsdlXml = @"
            <definitions xmlns=""http://schemas.xmlsoap.org/wsdl/""
                         xmlns:s=""http://www.w3.org/2001/XMLSchema""
                         targetNamespace=""http://example.com/"">
                <types>
                    <s:schema targetNamespace=""http://example.com/"">
                        <s:complexType name=""TestComplexType"">
                            <s:sequence>
                                <s:element name=""Field"" />
                            </s:sequence>
                        </s:complexType>
                        <s:element name=""TestElement"" type=""tns:TestComplexType"" />
                    </s:schema>
                </types>
            </definitions>";

        // Act
        var wsdlDefinition = parser.ParseXml(wsdlXml);

        // Assert
        Assert.NotNull(wsdlDefinition.Types);
        Assert.NotEmpty(wsdlDefinition.Types.ComplexTypes);

        // Find the complex type
        var complexType = wsdlDefinition.Types.ComplexTypes.FirstOrDefault(ct => ct.Name == "TestComplexType");
        Assert.NotNull(complexType);

        // Find the Field element inside the complex type
        var fieldElement = complexType.Elements.FirstOrDefault(e => e.Name == "Field");
        Assert.NotNull(fieldElement);
        Assert.Equal(string.Empty, fieldElement.Type); // Type attribute was not provided
    }

    [Fact]
    public void ProcessQualifiedAttribute_WithEmptyAttribute_SetsEmptyProperties()
    {
        // This test indirectly tests the ProcessQualifiedAttribute method through the public API

        // Arrange
        var parser = new WsdlParser();
        var wsdlXml = @"
            <definitions xmlns=""http://schemas.xmlsoap.org/wsdl/""
                         xmlns:s=""http://www.w3.org/2001/XMLSchema""
                         targetNamespace=""http://example.com/"">
                <types>
                    <s:schema targetNamespace=""http://example.com/"">
                        <s:complexType name=""TestComplexType"">
                            <s:sequence>
                                <s:element name=""Field"" type="""" />
                            </s:sequence>
                        </s:complexType>
                        <s:element name=""TestElement"" type=""tns:TestComplexType"" />
                    </s:schema>
                </types>
            </definitions>";

        // Act
        var wsdlDefinition = parser.ParseXml(wsdlXml);

        // Assert
        Assert.NotNull(wsdlDefinition.Types);
        Assert.NotEmpty(wsdlDefinition.Types.ComplexTypes);

        // Find the complex type
        var complexType = wsdlDefinition.Types.ComplexTypes.FirstOrDefault(ct => ct.Name == "TestComplexType");
        Assert.NotNull(complexType);

        // Find the Field element inside the complex type
        var fieldElement = complexType.Elements.FirstOrDefault(e => e.Name == "Field");
        Assert.NotNull(fieldElement);
        Assert.Equal(string.Empty, fieldElement.Type); // Empty type attribute
        Assert.Equal("http://example.com/", fieldElement.TypeNamespace); // Default namespace is used
    }

    #endregion
}
