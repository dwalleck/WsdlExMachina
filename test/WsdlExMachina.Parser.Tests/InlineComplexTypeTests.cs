using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;
using WsdlExMachina.Parser;
using WsdlExMachina.Parser.Models;
using WsdlExMachina.CSharpGenerator;
using WsdlExMachina.Parser.Tests.Utilities;

namespace WsdlExMachina.Parser.Tests;

public class InlineComplexTypeTests
{
  [Fact]
  public void ParseElement_WithInlineComplexType_ExtractsComplexType()
  {
    // Arrange
    var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<definitions xmlns:s=""http://www.w3.org/2001/XMLSchema"" xmlns:soap=""http://schemas.xmlsoap.org/wsdl/soap/"" xmlns:tns=""http://www.example.com/"" targetNamespace=""http://www.example.com/"" xmlns=""http://schemas.xmlsoap.org/wsdl/"">
  <types>
    <s:schema elementFormDefault=""qualified"" targetNamespace=""http://www.example.com/"">
      <s:element name=""TestElement"">
        <s:complexType>
          <s:sequence>
            <s:element minOccurs=""1"" maxOccurs=""1"" name=""Property1"" type=""s:string"" />
            <s:element minOccurs=""1"" maxOccurs=""1"" name=""Property2"" type=""s:int"" />
          </s:sequence>
        </s:complexType>
      </s:element>
    </s:schema>
  </types>
</definitions>";

    // Act
    var parser = new WsdlParser();
    var wsdl = parser.ParseXml(xml);

    // Assert
    var testElement = wsdl.Types.Elements.Find(e => e.Name == "TestElement");
    Assert.NotNull(testElement);
    Assert.True(testElement.IsComplexType);
    Assert.Equal("TestElementType", testElement.Type);

    var complexType = wsdl.Types.ComplexTypes.Find(ct => ct.Name == "TestElementType");
    Assert.NotNull(complexType);
    Assert.Equal(2, complexType.Elements.Count);
    Assert.Contains(complexType.Elements, e => e.Name == "Property1");
    Assert.Contains(complexType.Elements, e => e.Name == "Property2");
  }

  [Fact]
  public void ParseFile_ValidWsdl_ExtractsInlineComplexTypes()
  {
    // Arrange
    var parser = new WsdlParser();
    var filePath = TestFileHelper.GetSamplePath("sample.wsdl");

    // Act
    var wsdlDefinition = parser.ParseFile(filePath);

    // Assert
    Assert.NotNull(wsdlDefinition.Types);

    // Verify that the PostSinglePayment element has its Type property set
    var postSinglePaymentElement = wsdlDefinition.Types.Elements.Find(e => e.Name == "PostSinglePayment");
    Assert.NotNull(postSinglePaymentElement);
    Assert.True(postSinglePaymentElement.IsComplexType);
    Assert.Equal("PostSinglePaymentType", postSinglePaymentElement.Type);

    // Verify that the extracted complex type exists
    var postSinglePaymentType = wsdlDefinition.Types.ComplexTypes.Find(ct => ct.Name == "PostSinglePaymentType");
    Assert.NotNull(postSinglePaymentType);

    // Verify that the complex type has the expected elements
    Assert.Contains(postSinglePaymentType.Elements, e => e.Name == "Amount");
    Assert.Contains(postSinglePaymentType.Elements, e => e.Name == "ConvenienceFee");
    Assert.Contains(postSinglePaymentType.Elements, e => e.Name == "TransDate");
    Assert.Contains(postSinglePaymentType.Elements, e => e.Name == "ABA");
    Assert.Contains(postSinglePaymentType.Elements, e => e.Name == "AccountNumber");
  }

  [Fact]
  public void ParseElement_WithArrayOfXType_HandlesCorrectly()
  {
    // Arrange
    var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<definitions xmlns:s=""http://www.w3.org/2001/XMLSchema"" xmlns:soap=""http://schemas.xmlsoap.org/wsdl/soap/"" xmlns:tns=""http://www.example.com/"" targetNamespace=""http://www.example.com/"" xmlns=""http://schemas.xmlsoap.org/wsdl/"">
  <types>
    <s:schema elementFormDefault=""qualified"" targetNamespace=""http://www.example.com/"">
      <s:complexType name=""TestType"">
        <s:sequence>
          <s:element minOccurs=""0"" maxOccurs=""1"" name=""Items"" type=""tns:ArrayOfString"" />
        </s:sequence>
      </s:complexType>
      <s:complexType name=""ArrayOfString"">
        <s:sequence>
          <s:element minOccurs=""0"" maxOccurs=""unbounded"" name=""string"" nillable=""true"" type=""s:string"" />
        </s:sequence>
      </s:complexType>
    </s:schema>
  </types>
</definitions>";

    // Act
    var parser = new WsdlParser();
    var wsdl = parser.ParseXml(xml);

    // Assert
    var testType = wsdl.Types.ComplexTypes.Find(ct => ct.Name == "TestType");
    Assert.NotNull(testType);
    Assert.Single(testType.Elements);

    var itemsElement = testType.Elements[0];
    Assert.Equal("Items", itemsElement.Name);
    Assert.Equal("ArrayOfString", itemsElement.Type);

    var arrayOfStringType = wsdl.Types.ComplexTypes.Find(ct => ct.Name == "ArrayOfString");
    Assert.NotNull(arrayOfStringType);
    Assert.True(arrayOfStringType.IsArray);
    Assert.Equal("String", arrayOfStringType.ArrayItemType, ignoreCase: true);
  }

  [Fact]
  public void ParseElement_WithNestedInlineComplexType_ExtractsAllComplexTypes()
  {
    // Arrange
    var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<definitions xmlns:s=""http://www.w3.org/2001/XMLSchema"" xmlns:soap=""http://schemas.xmlsoap.org/wsdl/soap/"" xmlns:tns=""http://www.example.com/"" targetNamespace=""http://www.example.com/"" xmlns=""http://schemas.xmlsoap.org/wsdl/"">
  <types>
    <s:schema elementFormDefault=""qualified"" targetNamespace=""http://www.example.com/"">
      <s:element name=""ParentElement"">
        <s:complexType>
          <s:sequence>
            <s:element minOccurs=""1"" maxOccurs=""1"" name=""Property1"" type=""s:string"" />
            <s:element minOccurs=""1"" maxOccurs=""1"" name=""ChildElement"">
              <s:complexType>
                <s:sequence>
                  <s:element minOccurs=""1"" maxOccurs=""1"" name=""NestedProperty1"" type=""s:string"" />
                  <s:element minOccurs=""1"" maxOccurs=""1"" name=""NestedProperty2"" type=""s:int"" />
                </s:sequence>
              </s:complexType>
            </s:element>
          </s:sequence>
        </s:complexType>
      </s:element>
    </s:schema>
  </types>
</definitions>";

    // Act
    var parser = new WsdlParser();
    var wsdl = parser.ParseXml(xml);

    // Assert
    var parentElement = wsdl.Types.Elements.Find(e => e.Name == "ParentElement");
    Assert.NotNull(parentElement);
    Assert.True(parentElement.IsComplexType);
    Assert.Equal("ParentElementType", parentElement.Type);

    var parentComplexType = wsdl.Types.ComplexTypes.Find(ct => ct.Name == "ParentElementType");
    Assert.NotNull(parentComplexType);
    Assert.Equal(2, parentComplexType.Elements.Count);

    var childElement = parentComplexType.Elements.Find(e => e.Name == "ChildElement");
    Assert.NotNull(childElement);
    Assert.True(childElement.IsComplexType);
    Assert.Equal("ChildElementType", childElement.Type);

    var childComplexType = wsdl.Types.ComplexTypes.Find(ct => ct.Name == "ChildElementType");
    Assert.NotNull(childComplexType);
    Assert.Equal(2, childComplexType.Elements.Count);
    Assert.Contains(childComplexType.Elements, e => e.Name == "NestedProperty1");
    Assert.Contains(childComplexType.Elements, e => e.Name == "NestedProperty2");
  }

  [Fact]
  public void CodeGenerator_WithArrayOfXType_GeneratesListProperty()
  {
    // Arrange
    var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<definitions xmlns:s=""http://www.w3.org/2001/XMLSchema"" xmlns:soap=""http://schemas.xmlsoap.org/wsdl/soap/"" xmlns:tns=""http://www.example.com/"" targetNamespace=""http://www.example.com/"" xmlns=""http://schemas.xmlsoap.org/wsdl/"">
  <types>
    <s:schema elementFormDefault=""qualified"" targetNamespace=""http://www.example.com/"">
      <s:element name=""TestRequest"">
        <s:complexType>
          <s:sequence>
            <s:element minOccurs=""0"" maxOccurs=""1"" name=""Items"" type=""tns:ArrayOfString"" />
          </s:sequence>
        </s:complexType>
      </s:element>
      <s:complexType name=""ArrayOfString"">
        <s:sequence>
          <s:element minOccurs=""0"" maxOccurs=""unbounded"" name=""string"" nillable=""true"" type=""s:string"" />
        </s:sequence>
      </s:complexType>
    </s:schema>
  </types>
  <message name=""TestRequestMessage"">
    <part name=""parameters"" element=""tns:TestRequest"" />
  </message>
  <portType name=""TestPortType"">
    <operation name=""Test"">
      <input message=""tns:TestRequestMessage"" />
      <output message=""tns:TestRequestMessage"" />
    </operation>
  </portType>
</definitions>";

    var parser = new WsdlParser();
    var wsdl = parser.ParseXml(xml);

    // Act
    var generator = new RequestModelGenerator();
    var generatedFiles = generator.GenerateRequestModels(wsdl);

    // Assert
    Assert.True(generatedFiles.ContainsKey("TestRequest.cs"));
    var fileContent = generatedFiles["TestRequest.cs"];

    // Verify that the Items property is a List<string>, not ArrayOfString
    Assert.Contains("public List<string> Items { get; set; }", fileContent);
    Assert.DoesNotContain("public ArrayOfString Items { get; set; }", fileContent);

    // Verify that no ArrayOfString class was generated
    var complexTypeFiles = generator.GenerateComplexTypes(wsdl);
    Assert.DoesNotContain("ArrayOfString.cs", complexTypeFiles.Keys);
  }

  [Fact]
  public void CodeGenerator_WithComplexArrayOfXType_GeneratesListOfComplexType()
  {
    // Arrange
    var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<definitions xmlns:s=""http://www.w3.org/2001/XMLSchema"" xmlns:soap=""http://schemas.xmlsoap.org/wsdl/soap/"" xmlns:tns=""http://www.example.com/"" targetNamespace=""http://www.example.com/"" xmlns=""http://schemas.xmlsoap.org/wsdl/"">
  <types>
    <s:schema elementFormDefault=""qualified"" targetNamespace=""http://www.example.com/"">
      <s:element name=""TestRequest"">
        <s:complexType>
          <s:sequence>
            <s:element minOccurs=""0"" maxOccurs=""1"" name=""Items"" type=""tns:ArrayOfTestItem"" />
          </s:sequence>
        </s:complexType>
      </s:element>
      <s:complexType name=""TestItem"">
        <s:sequence>
          <s:element minOccurs=""1"" maxOccurs=""1"" name=""Id"" type=""s:int"" />
          <s:element minOccurs=""0"" maxOccurs=""1"" name=""Name"" type=""s:string"" />
        </s:sequence>
      </s:complexType>
      <s:complexType name=""ArrayOfTestItem"">
        <s:sequence>
          <s:element minOccurs=""0"" maxOccurs=""unbounded"" name=""TestItem"" nillable=""true"" type=""tns:TestItem"" />
        </s:sequence>
      </s:complexType>
    </s:schema>
  </types>
  <message name=""TestRequestMessage"">
    <part name=""parameters"" element=""tns:TestRequest"" />
  </message>
  <portType name=""TestPortType"">
    <operation name=""Test"">
      <input message=""tns:TestRequestMessage"" />
      <output message=""tns:TestRequestMessage"" />
    </operation>
  </portType>
</definitions>";

    var parser = new WsdlParser();
    var wsdl = parser.ParseXml(xml);

    // Act
    var generator = new RequestModelGenerator();
    var requestModels = generator.GenerateRequestModels(wsdl);
    var complexTypes = generator.GenerateComplexTypes(wsdl);

    // Assert
    // Verify TestRequest has a List<TestItem> property
    Assert.True(requestModels.ContainsKey("TestRequest.cs"));
    var requestContent = requestModels["TestRequest.cs"];
    Assert.Contains("public List<TestItem> Items { get; set; }", requestContent);
    Assert.DoesNotContain("public ArrayOfTestItem Items { get; set; }", requestContent);

    // Verify TestItem class was generated
    Assert.True(complexTypes.ContainsKey("TestItem.cs"));
    var testItemContent = complexTypes["TestItem.cs"];
    Assert.Contains("public class TestItem", testItemContent);
    Assert.Contains("public int Id { get; set; }", testItemContent);
    Assert.Contains("public string Name { get; set; }", testItemContent);

    // Verify ArrayOfTestItem class was NOT generated
    Assert.DoesNotContain("ArrayOfTestItem.cs", complexTypes.Keys);
  }

  [Fact]
  public void CodeGenerator_WithInlineComplexType_GeneratesCorrectClasses()
  {
    // Arrange
    var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<definitions xmlns:s=""http://www.w3.org/2001/XMLSchema"" xmlns:soap=""http://schemas.xmlsoap.org/wsdl/soap/"" xmlns:tns=""http://www.example.com/"" targetNamespace=""http://www.example.com/"" xmlns=""http://schemas.xmlsoap.org/wsdl/"">
  <types>
    <s:schema elementFormDefault=""qualified"" targetNamespace=""http://www.example.com/"">
      <s:element name=""TestRequest"">
        <s:complexType>
          <s:sequence>
            <s:element minOccurs=""1"" maxOccurs=""1"" name=""Id"" type=""s:int"" />
            <s:element minOccurs=""0"" maxOccurs=""1"" name=""Name"" type=""s:string"" />
            <s:element minOccurs=""0"" maxOccurs=""1"" name=""NestedElement"">
              <s:complexType>
                <s:sequence>
                  <s:element minOccurs=""1"" maxOccurs=""1"" name=""NestedId"" type=""s:int"" />
                  <s:element minOccurs=""0"" maxOccurs=""1"" name=""NestedName"" type=""s:string"" />
                </s:sequence>
              </s:complexType>
            </s:element>
          </s:sequence>
        </s:complexType>
      </s:element>
    </s:schema>
  </types>
  <message name=""TestRequestMessage"">
    <part name=""parameters"" element=""tns:TestRequest"" />
  </message>
  <portType name=""TestPortType"">
    <operation name=""Test"">
      <input message=""tns:TestRequestMessage"" />
      <output message=""tns:TestRequestMessage"" />
    </operation>
  </portType>
</definitions>";

    var parser = new WsdlParser();
    var wsdl = parser.ParseXml(xml);

    // Act
    var generator = new RequestModelGenerator();
    var requestModels = generator.GenerateRequestModels(wsdl);
    var complexTypes = generator.GenerateComplexTypes(wsdl);

    // Assert
    // Verify TestRequest has the expected properties
    Assert.True(requestModels.ContainsKey("TestRequest.cs"));
    var requestContent = requestModels["TestRequest.cs"];
    Assert.Contains("public int Id { get; set; }", requestContent);
    Assert.Contains("public string Name { get; set; }", requestContent);
    Assert.Contains("public NestedElementType NestedElement { get; set; }", requestContent);

    // Verify NestedElementType class was generated
    Assert.True(complexTypes.ContainsKey("NestedElementType.cs"));
    var nestedContent = complexTypes["NestedElementType.cs"];
    Assert.Contains("public class NestedElementType", nestedContent);
    Assert.Contains("public int NestedId { get; set; }", nestedContent);
    Assert.Contains("public string NestedName { get; set; }", nestedContent);
  }

  [Fact]
  public void CodeGenerator_WithNestedInlineComplexTypeAndArrays_GeneratesCorrectClasses()
  {
    // Arrange
    var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<definitions xmlns:s=""http://www.w3.org/2001/XMLSchema"" xmlns:soap=""http://schemas.xmlsoap.org/wsdl/soap/"" xmlns:tns=""http://www.example.com/"" targetNamespace=""http://www.example.com/"" xmlns=""http://schemas.xmlsoap.org/wsdl/"">
  <types>
    <s:schema elementFormDefault=""qualified"" targetNamespace=""http://www.example.com/"">
      <s:element name=""ComplexRequest"">
        <s:complexType>
          <s:sequence>
            <s:element minOccurs=""1"" maxOccurs=""1"" name=""Id"" type=""s:int"" />
            <s:element minOccurs=""0"" maxOccurs=""1"" name=""Name"" type=""s:string"" />
            <s:element minOccurs=""0"" maxOccurs=""1"" name=""Tags"" type=""tns:ArrayOfString"" />
            <s:element minOccurs=""0"" maxOccurs=""1"" name=""NestedElement"">
              <s:complexType>
                <s:sequence>
                  <s:element minOccurs=""1"" maxOccurs=""1"" name=""NestedId"" type=""s:int"" />
                  <s:element minOccurs=""0"" maxOccurs=""1"" name=""NestedName"" type=""s:string"" />
                  <s:element minOccurs=""0"" maxOccurs=""1"" name=""Items"" type=""tns:ArrayOfItem"" />
                </s:sequence>
              </s:complexType>
            </s:element>
          </s:sequence>
        </s:complexType>
      </s:element>
      <s:complexType name=""Item"">
        <s:sequence>
          <s:element minOccurs=""1"" maxOccurs=""1"" name=""ItemId"" type=""s:int"" />
          <s:element minOccurs=""0"" maxOccurs=""1"" name=""ItemName"" type=""s:string"" />
        </s:sequence>
      </s:complexType>
      <s:complexType name=""ArrayOfItem"">
        <s:sequence>
          <s:element minOccurs=""0"" maxOccurs=""unbounded"" name=""Item"" nillable=""true"" type=""tns:Item"" />
        </s:sequence>
      </s:complexType>
      <s:complexType name=""ArrayOfString"">
        <s:sequence>
          <s:element minOccurs=""0"" maxOccurs=""unbounded"" name=""string"" nillable=""true"" type=""s:string"" />
        </s:sequence>
      </s:complexType>
    </s:schema>
  </types>
  <message name=""ComplexRequestMessage"">
    <part name=""parameters"" element=""tns:ComplexRequest"" />
  </message>
  <portType name=""ComplexPortType"">
    <operation name=""Complex"">
      <input message=""tns:ComplexRequestMessage"" />
      <output message=""tns:ComplexRequestMessage"" />
    </operation>
  </portType>
</definitions>";

    var parser = new WsdlParser();
    var wsdl = parser.ParseXml(xml);

    // Act
    var generator = new RequestModelGenerator();
    var requestModels = generator.GenerateRequestModels(wsdl);
    var complexTypes = generator.GenerateComplexTypes(wsdl);

    // Assert
    // Verify ComplexRequest has the expected properties
    Assert.True(requestModels.ContainsKey("ComplexRequest.cs"));
    var requestContent = requestModels["ComplexRequest.cs"];
    Assert.Contains("public int Id { get; set; }", requestContent);
    Assert.Contains("public string Name { get; set; }", requestContent);
    Assert.Contains("public List<string> Tags { get; set; }", requestContent);
    Assert.Contains("public NestedElementType NestedElement { get; set; }", requestContent);

    // Verify NestedElementType class was generated with correct properties
    Assert.True(complexTypes.ContainsKey("NestedElementType.cs"));
    var nestedContent = complexTypes["NestedElementType.cs"];
    Assert.Contains("public class NestedElementType", nestedContent);
    Assert.Contains("public int NestedId { get; set; }", nestedContent);
    Assert.Contains("public string NestedName { get; set; }", nestedContent);
    Assert.Contains("public List<Item> Items { get; set; }", nestedContent);

    // Verify Item class was generated
    Assert.True(complexTypes.ContainsKey("Item.cs"));
    var itemContent = complexTypes["Item.cs"];
    Assert.Contains("public class Item", itemContent);
    Assert.Contains("public int ItemId { get; set; }", itemContent);
    Assert.Contains("public string ItemName { get; set; }", itemContent);

    // Verify ArrayOfX classes were NOT generated
    Assert.DoesNotContain("ArrayOfItem.cs", complexTypes.Keys);
    Assert.DoesNotContain("ArrayOfString.cs", complexTypes.Keys);
  }
}
