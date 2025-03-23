using System.Xml.Linq;
using Xunit;
using WsdlExMachina.Parser.Models;
using WsdlExMachina.Parser.Utilities;

namespace WsdlExMachina.Parser.Tests;

public class WsdlSpecialCaseHandlerTests
{
    #region HandleComplexTypeInheritance Tests

    [Fact]
    public void HandleComplexTypeInheritance_WithValidExtensionElement_SetsBaseTypeProperties()
    {
        // Arrange
        var complexType = new WsdlComplexType();
        var extensionElement = XElement.Parse(@"
            <extension xmlns:s=""http://www.w3.org/2001/XMLSchema"" base=""s:string"">
                <sequence />
            </extension>");
        var schemaNamespace = "http://example.com/";

        // Act
        WsdlSpecialCaseHandler.HandleComplexTypeInheritance(complexType, extensionElement, schemaNamespace);

        // Assert
        Assert.Equal("string", complexType.BaseType);
        Assert.Equal("http://www.w3.org/2001/XMLSchema", complexType.BaseTypeNamespace);
    }

    [Fact]
    public void HandleComplexTypeInheritance_WithNullExtensionElement_DoesNothing()
    {
        // Arrange
        var complexType = new WsdlComplexType();
        XElement? extensionElement = null;
        var schemaNamespace = "http://example.com/";

        // Act
        WsdlSpecialCaseHandler.HandleComplexTypeInheritance(complexType, extensionElement, schemaNamespace);

        // Assert
        Assert.Null(complexType.BaseType);
        Assert.Null(complexType.BaseTypeNamespace);
    }

    [Fact]
    public void HandleComplexTypeInheritance_WithMissingBaseAttribute_DoesNotSetBaseTypeProperties()
    {
        // Arrange
        var complexType = new WsdlComplexType();
        var extensionElement = XElement.Parse(@"
            <extension xmlns:s=""http://www.w3.org/2001/XMLSchema"">
                <sequence />
            </extension>");
        var schemaNamespace = "http://example.com/";

        // Act
        WsdlSpecialCaseHandler.HandleComplexTypeInheritance(complexType, extensionElement, schemaNamespace);

        // Assert
        Assert.Null(complexType.BaseType);
        Assert.Null(complexType.BaseTypeNamespace);
    }

    #endregion

    #region DetectArrayType Tests

    [Fact]
    public void DetectArrayType_WithArrayOfXNaming_ReturnsTrue()
    {
        // Arrange
        var complexType = new WsdlComplexType { Name = "ArrayOfString" };
        var complexTypeElement = XElement.Parse(@"
            <complexType name=""ArrayOfString"" xmlns:s=""http://www.w3.org/2001/XMLSchema"">
                <sequence>
                    <element name=""string"" type=""s:string"" minOccurs=""0"" maxOccurs=""unbounded"" />
                </sequence>
            </complexType>");
        var schemaNamespace = "http://example.com/";

        // Act
        bool result = WsdlSpecialCaseHandler.DetectArrayType(complexType, complexTypeElement, schemaNamespace);

        // Assert
        Assert.True(result);
        Assert.True(complexType.IsArray);
        Assert.Equal("String", complexType.ArrayItemType);
        Assert.Equal(schemaNamespace, complexType.ArrayItemTypeNamespace);
        Assert.Single(complexType.Elements);
        Assert.True(complexType.Elements[0].IsArray);
    }

    [Fact]
    public void DetectArrayType_WithArrayOfXNamingCaseInsensitive_ReturnsTrue()
    {
        // Arrange
        var complexType = new WsdlComplexType { Name = "arrayofstring" };
        var complexTypeElement = XElement.Parse(@"
            <complexType name=""arrayofstring"" xmlns:s=""http://www.w3.org/2001/XMLSchema"">
                <sequence>
                    <element name=""string"" type=""s:string"" minOccurs=""0"" maxOccurs=""unbounded"" />
                </sequence>
            </complexType>");
        var schemaNamespace = "http://example.com/";

        // Act
        bool result = WsdlSpecialCaseHandler.DetectArrayType(complexType, complexTypeElement, schemaNamespace);

        // Assert
        Assert.True(result);
        Assert.True(complexType.IsArray);
        Assert.Equal("string", complexType.ArrayItemType);
        Assert.Equal(schemaNamespace, complexType.ArrayItemTypeNamespace);
    }

    [Fact]
    public void DetectArrayType_WithArrayOfXNamingButMultipleElements_ReturnsFalse()
    {
        // Arrange
        var complexType = new WsdlComplexType { Name = "ArrayOfString" };
        var complexTypeElement = XElement.Parse(@"
            <complexType name=""ArrayOfString"" xmlns:s=""http://www.w3.org/2001/XMLSchema"">
                <sequence>
                    <element name=""string1"" type=""s:string"" minOccurs=""0"" maxOccurs=""unbounded"" />
                    <element name=""string2"" type=""s:string"" minOccurs=""0"" maxOccurs=""unbounded"" />
                </sequence>
            </complexType>");
        var schemaNamespace = "http://example.com/";

        // Act
        bool result = WsdlSpecialCaseHandler.DetectArrayType(complexType, complexTypeElement, schemaNamespace);

        // Assert
        Assert.False(result);
        Assert.False(complexType.IsArray);
        Assert.Null(complexType.ArrayItemType);
        Assert.Null(complexType.ArrayItemTypeNamespace);
    }

    [Fact]
    public void DetectArrayType_WithNonArrayName_ReturnsFalse()
    {
        // Arrange
        var complexType = new WsdlComplexType { Name = "Person" };
        var complexTypeElement = XElement.Parse(@"
            <complexType name=""Person"" xmlns:s=""http://www.w3.org/2001/XMLSchema"">
                <sequence>
                    <element name=""Name"" type=""s:string"" />
                    <element name=""Age"" type=""s:int"" />
                </sequence>
            </complexType>");
        var schemaNamespace = "http://example.com/";

        // Act
        bool result = WsdlSpecialCaseHandler.DetectArrayType(complexType, complexTypeElement, schemaNamespace);

        // Assert
        Assert.False(result);
        Assert.False(complexType.IsArray);
        Assert.Null(complexType.ArrayItemType);
        Assert.Null(complexType.ArrayItemTypeNamespace);
    }

    [Fact]
    public void DetectArrayType_WithUnboundedMaxOccurs_ReturnsTrue()
    {
        // Arrange
        var complexType = new WsdlComplexType { Name = "StringList" };
        var complexTypeElement = XElement.Parse(@"
            <complexType name=""StringList"" xmlns:s=""http://www.w3.org/2001/XMLSchema"">
                <sequence>
                    <element name=""string"" type=""s:string"" minOccurs=""0"" maxOccurs=""unbounded"" />
                </sequence>
            </complexType>");
        var schemaNamespace = "http://example.com/";

        // Act
        bool result = WsdlSpecialCaseHandler.DetectArrayType(complexType, complexTypeElement, schemaNamespace);

        // Assert
        Assert.True(result);
        Assert.True(complexType.IsArray);
        Assert.Equal("string", complexType.ArrayItemType);
        Assert.Equal("http://www.w3.org/2001/XMLSchema", complexType.ArrayItemTypeNamespace);
    }

    [Fact]
    public void DetectArrayType_WithNumericMaxOccurs_ReturnsTrue()
    {
        // Arrange
        var complexType = new WsdlComplexType { Name = "StringList" };
        var complexTypeElement = XElement.Parse(@"
            <complexType name=""StringList"" xmlns:s=""http://www.w3.org/2001/XMLSchema"">
                <sequence>
                    <element name=""string"" type=""s:string"" minOccurs=""0"" maxOccurs=""5"" />
                </sequence>
            </complexType>");
        var schemaNamespace = "http://example.com/";

        // Act
        bool result = WsdlSpecialCaseHandler.DetectArrayType(complexType, complexTypeElement, schemaNamespace);

        // Assert
        Assert.True(result);
        Assert.True(complexType.IsArray);
        Assert.Equal("string", complexType.ArrayItemType);
        Assert.Equal("http://www.w3.org/2001/XMLSchema", complexType.ArrayItemTypeNamespace);
    }

    [Fact]
    public void DetectArrayType_WithNoTypeAttribute_ReturnsFalse()
    {
        // Arrange
        var complexType = new WsdlComplexType { Name = "StringList" };
        var complexTypeElement = XElement.Parse(@"
            <complexType name=""StringList"" xmlns:s=""http://www.w3.org/2001/XMLSchema"">
                <sequence>
                    <element name=""string"" minOccurs=""0"" maxOccurs=""unbounded"" />
                </sequence>
            </complexType>");
        var schemaNamespace = "http://example.com/";

        // Act
        bool result = WsdlSpecialCaseHandler.DetectArrayType(complexType, complexTypeElement, schemaNamespace);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void DetectArrayType_WithArrayOfXNamingAndNoTypeAttribute_ReturnsTrue()
    {
        // Arrange
        var complexType = new WsdlComplexType { Name = "ArrayOfString" };
        var complexTypeElement = XElement.Parse(@"
            <complexType name=""ArrayOfString"" xmlns:s=""http://www.w3.org/2001/XMLSchema"">
                <sequence>
                    <element name=""string"" minOccurs=""0"" maxOccurs=""unbounded"" />
                </sequence>
            </complexType>");
        var schemaNamespace = "http://example.com/";

        // Act
        bool result = WsdlSpecialCaseHandler.DetectArrayType(complexType, complexTypeElement, schemaNamespace);

        // Assert
        Assert.True(result);
        Assert.True(complexType.IsArray);
        Assert.Equal("String", complexType.ArrayItemType);
        Assert.Equal(schemaNamespace, complexType.ArrayItemTypeNamespace);
        Assert.Single(complexType.Elements);
        Assert.True(complexType.Elements[0].IsArray);
        Assert.Equal("String", complexType.Elements[0].Type);
    }

    #endregion

    #region ProcessImportedSchemas Tests

    [Fact]
    public void ProcessImportedSchemas_WithValidImports_AddsToImportedNamespaces()
    {
        // Arrange
        var schemaElement = XElement.Parse(@"
            <schema xmlns=""http://www.w3.org/2001/XMLSchema"" targetNamespace=""http://example.com/"">
                <import namespace=""http://imported1.example.com/"" />
                <import namespace=""http://imported2.example.com/"" />
            </schema>");
        var types = new WsdlTypes();

        // Act
        WsdlSpecialCaseHandler.ProcessImportedSchemas(schemaElement, types);

        // Assert
        Assert.Equal(2, types.ImportedNamespaces.Count);
        Assert.Contains("http://imported1.example.com/", types.ImportedNamespaces);
        Assert.Contains("http://imported2.example.com/", types.ImportedNamespaces);
    }

    [Fact]
    public void ProcessImportedSchemas_WithNoImports_DoesNotAddToImportedNamespaces()
    {
        // Arrange
        var schemaElement = XElement.Parse(@"
            <schema xmlns=""http://www.w3.org/2001/XMLSchema"" targetNamespace=""http://example.com/"">
                <element name=""test"" type=""string"" />
            </schema>");
        var types = new WsdlTypes();

        // Act
        WsdlSpecialCaseHandler.ProcessImportedSchemas(schemaElement, types);

        // Assert
        Assert.Empty(types.ImportedNamespaces);
    }

    [Fact]
    public void ProcessImportedSchemas_WithImportMissingNamespace_DoesNotAddToImportedNamespaces()
    {
        // Arrange
        var schemaElement = XElement.Parse(@"
            <schema xmlns=""http://www.w3.org/2001/XMLSchema"" targetNamespace=""http://example.com/"">
                <import schemaLocation=""some-schema.xsd"" />
            </schema>");
        var types = new WsdlTypes();

        // Act
        WsdlSpecialCaseHandler.ProcessImportedSchemas(schemaElement, types);

        // Assert
        Assert.Empty(types.ImportedNamespaces);
    }

    #endregion

    #region HandleOperationOverloading Tests

    [Fact]
    public void HandleOperationOverloading_WithNoOverloading_AddsOperationsNormally()
    {
        // Arrange
        var portTypeElement = XElement.Parse(@"
            <portType name=""TestPortType"" xmlns=""http://schemas.xmlsoap.org/wsdl/"">
                <operation name=""Operation1"">
                    <input name=""Operation1Input"" message=""tns:Operation1InputMessage"" />
                    <output name=""Operation1Output"" message=""tns:Operation1OutputMessage"" />
                </operation>
                <operation name=""Operation2"">
                    <input name=""Operation2Input"" message=""tns:Operation2InputMessage"" />
                    <output name=""Operation2Output"" message=""tns:Operation2OutputMessage"" />
                </operation>
            </portType>");
        var portType = new WsdlPortType();

        // Act
        WsdlSpecialCaseHandler.HandleOperationOverloading(portTypeElement, portType);

        // Assert
        Assert.Equal(2, portType.Operations.Count);
        Assert.Equal("Operation1", portType.Operations[0].Name);
        Assert.Equal("Operation2", portType.Operations[1].Name);
    }

    [Fact]
    public void HandleOperationOverloading_WithOverloadedOperations_AppendsSuffixToNames()
    {
        // Arrange
        var portTypeElement = XElement.Parse(@"
            <portType name=""TestPortType"" xmlns=""http://schemas.xmlsoap.org/wsdl/"">
                <operation name=""Operation"">
                    <input name=""Operation1Input"" message=""tns:Operation1InputMessage"" />
                    <output name=""Operation1Output"" message=""tns:Operation1OutputMessage"" />
                </operation>
                <operation name=""Operation"">
                    <input name=""Operation2Input"" message=""tns:Operation2InputMessage"" />
                    <output name=""Operation2Output"" message=""tns:Operation2OutputMessage"" />
                </operation>
            </portType>");
        var portType = new WsdlPortType();

        // Act
        WsdlSpecialCaseHandler.HandleOperationOverloading(portTypeElement, portType);

        // Assert
        Assert.Equal(2, portType.Operations.Count);
        Assert.Equal("Operation", portType.Operations[0].Name);
        Assert.Equal("Operation_1", portType.Operations[1].Name);
    }

    [Fact]
    public void HandleOperationOverloading_WithMultipleOverloadedOperations_AppendsSuffixToNames()
    {
        // Arrange
        var portTypeElement = XElement.Parse(@"
            <portType name=""TestPortType"" xmlns=""http://schemas.xmlsoap.org/wsdl/"">
                <operation name=""Operation"">
                    <input name=""Operation1Input"" message=""tns:Operation1InputMessage"" />
                    <output name=""Operation1Output"" message=""tns:Operation1OutputMessage"" />
                </operation>
                <operation name=""Operation"">
                    <input name=""Operation2Input"" message=""tns:Operation2InputMessage"" />
                    <output name=""Operation2Output"" message=""tns:Operation2OutputMessage"" />
                </operation>
                <operation name=""Operation"">
                    <input name=""Operation3Input"" message=""tns:Operation3InputMessage"" />
                    <output name=""Operation3Output"" message=""tns:Operation3OutputMessage"" />
                </operation>
            </portType>");
        var portType = new WsdlPortType();

        // Act
        WsdlSpecialCaseHandler.HandleOperationOverloading(portTypeElement, portType);

        // Assert
        Assert.Equal(3, portType.Operations.Count);
        Assert.Equal("Operation", portType.Operations[0].Name);
        Assert.Equal("Operation_1", portType.Operations[1].Name);
        Assert.Equal("Operation_2", portType.Operations[2].Name);
    }

    [Fact]
    public void HandleOperationOverloading_WithEmptyPortType_DoesNothing()
    {
        // Arrange
        var portTypeElement = XElement.Parse(@"
            <portType name=""TestPortType"" xmlns=""http://schemas.xmlsoap.org/wsdl/"">
            </portType>");
        var portType = new WsdlPortType();

        // Act
        WsdlSpecialCaseHandler.HandleOperationOverloading(portTypeElement, portType);

        // Assert
        Assert.Empty(portType.Operations);
    }

    [Fact]
    public void HandleOperationOverloading_WithMissingNameAttribute_HandlesGracefully()
    {
        // Arrange
        var portTypeElement = XElement.Parse(@"
            <portType name=""TestPortType"" xmlns=""http://schemas.xmlsoap.org/wsdl/"">
                <operation>
                    <input name=""Operation1Input"" message=""tns:Operation1InputMessage"" />
                    <output name=""Operation1Output"" message=""tns:Operation1OutputMessage"" />
                </operation>
                <operation name=""Operation2"">
                    <input name=""Operation2Input"" message=""tns:Operation2InputMessage"" />
                    <output name=""Operation2Output"" message=""tns:Operation2OutputMessage"" />
                </operation>
            </portType>");
        var portType = new WsdlPortType();

        // Act
        WsdlSpecialCaseHandler.HandleOperationOverloading(portTypeElement, portType);

        // Assert
        Assert.Equal(2, portType.Operations.Count);
        Assert.Equal("", portType.Operations[0].Name);
        Assert.Equal("Operation2", portType.Operations[1].Name);
    }

    #endregion
}
