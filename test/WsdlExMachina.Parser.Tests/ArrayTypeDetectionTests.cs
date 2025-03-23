using System.Xml.Linq;
using Xunit;
using WsdlExMachina.Parser;
using WsdlExMachina.Parser.Models;

namespace WsdlExMachina.Parser.Tests;

public class ArrayTypeDetectionTests
{
    [Fact]
    public void Parse_ArrayOfString_DetectsAsArray()
    {
        // Arrange
        var parser = new WsdlParser();
        var wsdlXml = @"
            <definitions xmlns=""http://schemas.xmlsoap.org/wsdl/""
                         xmlns:s=""http://www.w3.org/2001/XMLSchema""
                         xmlns:tns=""http://example.com/""
                         targetNamespace=""http://example.com/"">
                <types>
                    <s:schema targetNamespace=""http://example.com/"">
                        <s:complexType name=""ArrayOfString"">
                            <s:sequence>
                                <s:element name=""string"" type=""s:string"" minOccurs=""0"" maxOccurs=""unbounded"" />
                            </s:sequence>
                        </s:complexType>
                    </s:schema>
                </types>
            </definitions>";

        // Act
        var wsdlDefinition = parser.ParseXml(wsdlXml);

        // Assert
        Assert.NotNull(wsdlDefinition.Types);
        Assert.NotEmpty(wsdlDefinition.Types.ComplexTypes);

        var arrayType = wsdlDefinition.Types.ComplexTypes.Find(ct => ct.Name == "ArrayOfString");
        Assert.NotNull(arrayType);
        Assert.True(arrayType.IsArray);
        Assert.Equal("String", arrayType.ArrayItemType);
        Assert.Equal("http://example.com/", arrayType.ArrayItemTypeNamespace);

        // Check that the element is also marked as an array
        Assert.Single(arrayType.Elements);
        Assert.True(arrayType.Elements[0].IsArray);
    }

    [Fact]
    public void Parse_ArrayOfComplex_DetectsAsArray()
    {
        // Arrange
        var parser = new WsdlParser();
        var wsdlXml = @"
            <definitions xmlns=""http://schemas.xmlsoap.org/wsdl/""
                         xmlns:s=""http://www.w3.org/2001/XMLSchema""
                         xmlns:tns=""http://example.com/""
                         targetNamespace=""http://example.com/"">
                <types>
                    <s:schema targetNamespace=""http://example.com/"">
                        <s:complexType name=""Person"">
                            <s:sequence>
                                <s:element name=""Name"" type=""s:string"" />
                                <s:element name=""Age"" type=""s:int"" />
                            </s:sequence>
                        </s:complexType>
                        <s:complexType name=""ArrayOfPerson"">
                            <s:sequence>
                                <s:element name=""Person"" type=""tns:Person"" minOccurs=""0"" maxOccurs=""unbounded"" />
                            </s:sequence>
                        </s:complexType>
                    </s:schema>
                </types>
            </definitions>";

        // Act
        var wsdlDefinition = parser.ParseXml(wsdlXml);

        // Assert
        Assert.NotNull(wsdlDefinition.Types);
        Assert.NotEmpty(wsdlDefinition.Types.ComplexTypes);

        var personType = wsdlDefinition.Types.ComplexTypes.Find(ct => ct.Name == "Person");
        Assert.NotNull(personType);
        Assert.False(personType.IsArray);

        var arrayType = wsdlDefinition.Types.ComplexTypes.Find(ct => ct.Name == "ArrayOfPerson");
        Assert.NotNull(arrayType);
        Assert.True(arrayType.IsArray);
        Assert.Equal("Person", arrayType.ArrayItemType);
        Assert.Equal("http://example.com/", arrayType.ArrayItemTypeNamespace);

        // Check that the element is also marked as an array
        Assert.Single(arrayType.Elements);
        Assert.True(arrayType.Elements[0].IsArray);
        Assert.Equal("Person", arrayType.Elements[0].Type);
        Assert.Equal("http://example.com/", arrayType.Elements[0].TypeNamespace);
    }

    [Fact]
    public void Parse_ArrayWithoutMaxOccurs_StillDetectsAsArray()
    {
        // Arrange
        var parser = new WsdlParser();
        var wsdlXml = @"
            <definitions xmlns=""http://schemas.xmlsoap.org/wsdl/""
                         xmlns:s=""http://www.w3.org/2001/XMLSchema""
                         xmlns:tns=""http://example.com/""
                         targetNamespace=""http://example.com/"">
                <types>
                    <s:schema targetNamespace=""http://example.com/"">
                        <s:complexType name=""ArrayOfInt"">
                            <s:sequence>
                                <s:element name=""int"" type=""s:int"" minOccurs=""0"" />
                            </s:sequence>
                        </s:complexType>
                    </s:schema>
                </types>
            </definitions>";

        // Act
        var wsdlDefinition = parser.ParseXml(wsdlXml);

        // Assert
        Assert.NotNull(wsdlDefinition.Types);
        Assert.NotEmpty(wsdlDefinition.Types.ComplexTypes);

        var arrayType = wsdlDefinition.Types.ComplexTypes.Find(ct => ct.Name == "ArrayOfInt");
        Assert.NotNull(arrayType);
        Assert.True(arrayType.IsArray);
        Assert.Equal("Int", arrayType.ArrayItemType);
        Assert.Equal("http://example.com/", arrayType.ArrayItemTypeNamespace);
    }

    [Fact]
    public void Parse_NonArrayType_NotDetectedAsArray()
    {
        // Arrange
        var parser = new WsdlParser();
        var wsdlXml = @"
            <definitions xmlns=""http://schemas.xmlsoap.org/wsdl/""
                         xmlns:s=""http://www.w3.org/2001/XMLSchema""
                         xmlns:tns=""http://example.com/""
                         targetNamespace=""http://example.com/"">
                <types>
                    <s:schema targetNamespace=""http://example.com/"">
                        <s:complexType name=""NotAnArray"">
                            <s:sequence>
                                <s:element name=""Element1"" type=""s:string"" />
                                <s:element name=""Element2"" type=""s:int"" />
                            </s:sequence>
                        </s:complexType>
                    </s:schema>
                </types>
            </definitions>";

        // Act
        var wsdlDefinition = parser.ParseXml(wsdlXml);

        // Assert
        Assert.NotNull(wsdlDefinition.Types);
        Assert.NotEmpty(wsdlDefinition.Types.ComplexTypes);

        var nonArrayType = wsdlDefinition.Types.ComplexTypes.Find(ct => ct.Name == "NotAnArray");
        Assert.NotNull(nonArrayType);
        Assert.False(nonArrayType.IsArray);
        Assert.Null(nonArrayType.ArrayItemType);
        Assert.Null(nonArrayType.ArrayItemTypeNamespace);
    }
}
