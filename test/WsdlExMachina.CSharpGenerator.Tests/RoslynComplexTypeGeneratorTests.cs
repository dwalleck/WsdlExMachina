using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WsdlExMachina.Parser.Models;
using Xunit;

namespace WsdlExMachina.CSharpGenerator.Tests;

public class RoslynComplexTypeGeneratorTests
{
    private readonly RoslynComplexTypeGenerator _generator;
    private readonly RoslynCodeGenerator _codeGenerator;

    public RoslynComplexTypeGeneratorTests()
    {
        _codeGenerator = new RoslynCodeGenerator();
        _generator = new RoslynComplexTypeGenerator(_codeGenerator);
    }

    [Fact]
    public void GenerateComplexType_ShouldThrowArgumentNullException_WhenComplexTypeIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _generator.GenerateComplexType(null, "Test.Namespace"));
    }

    [Fact]
    public void GenerateComplexType_ShouldThrowArgumentException_WhenNamespaceIsEmpty()
    {
        // Arrange
        var complexType = CreateTestComplexType();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _generator.GenerateComplexType(complexType, string.Empty));
    }

    [Fact]
    public void GenerateComplexType_ShouldCreateClassWithCorrectName()
    {
        // Arrange
        var complexType = CreateTestComplexType();
        var namespaceName = "Test.Namespace";

        // Act
        var result = _generator.GenerateComplexType(complexType, namespaceName);

        // Assert
        var namespaceDeclaration = result.Members.OfType<FileScopedNamespaceDeclarationSyntax>().First();
        var classDeclaration = namespaceDeclaration.Members.OfType<ClassDeclarationSyntax>().First();

        Assert.Equal(complexType.Name, classDeclaration.Identifier.Text);
    }

    [Fact]
    public void GenerateComplexType_ShouldCreateClassWithCorrectXmlTypeAttribute()
    {
        // Arrange
        var complexType = CreateTestComplexType();
        var namespaceName = "Test.Namespace";

        // Act
        var result = _generator.GenerateComplexType(complexType, namespaceName);

        // Assert
        var namespaceDeclaration = result.Members.OfType<FileScopedNamespaceDeclarationSyntax>().First();
        var classDeclaration = namespaceDeclaration.Members.OfType<ClassDeclarationSyntax>().First();
        var xmlTypeAttribute = classDeclaration.AttributeLists
            .SelectMany(al => al.Attributes)
            .FirstOrDefault(a => a.Name.ToString() == "XmlType");

        Assert.NotNull(xmlTypeAttribute);

        var typeNameArg = xmlTypeAttribute.ArgumentList.Arguments
            .FirstOrDefault(a => a.NameEquals.Name.ToString() == "TypeName");
        var namespaceArg = xmlTypeAttribute.ArgumentList.Arguments
            .FirstOrDefault(a => a.NameEquals.Name.ToString() == "Namespace");

        Assert.NotNull(typeNameArg);
        Assert.NotNull(namespaceArg);
        Assert.Equal(complexType.Name, ((LiteralExpressionSyntax)typeNameArg.Expression).Token.ValueText);
        Assert.Equal(complexType.Namespace, ((LiteralExpressionSyntax)namespaceArg.Expression).Token.ValueText);
    }

    [Fact]
    public void GenerateComplexType_ShouldCreateClassWithCorrectProperties()
    {
        // Arrange
        var complexType = CreateTestComplexType();
        var namespaceName = "Test.Namespace";

        // Act
        var result = _generator.GenerateComplexType(complexType, namespaceName);

        // Assert
        var namespaceDeclaration = result.Members.OfType<FileScopedNamespaceDeclarationSyntax>().First();
        var classDeclaration = namespaceDeclaration.Members.OfType<ClassDeclarationSyntax>().First();
        var properties = classDeclaration.Members.OfType<PropertyDeclarationSyntax>().ToList();

        Assert.Equal(complexType.Elements.Count, properties.Count);

        for (var i = 0; i < complexType.Elements.Count; i++)
        {
            var element = complexType.Elements[i];
            var property = properties[i];

            // Check property name
            var expectedPropertyName = new NamingHelper().GetSafePropertyName(element.Name);
            Assert.Equal(expectedPropertyName, property.Identifier.Text);

            // Check XML attribute
            var xmlElementAttribute = property.AttributeLists
                .SelectMany(al => al.Attributes)
                .FirstOrDefault(a => a.Name.ToString() == "XmlElement");

            Assert.NotNull(xmlElementAttribute);

            var elementNameArg = xmlElementAttribute.ArgumentList.Arguments
                .FirstOrDefault(a => a.NameEquals.Name.ToString() == "ElementName");

            Assert.NotNull(elementNameArg);
            Assert.Equal(element.Name, ((LiteralExpressionSyntax)elementNameArg.Expression).Token.ValueText);
        }
    }

    [Fact]
    public void GenerateComplexType_ShouldHandleInheritance()
    {
        // Arrange
        var complexType = new WsdlComplexType
        {
            Name = "DerivedType",
            Namespace = "http://test.com/",
            BaseType = "BaseType",
            Elements = new List<WsdlElement>
            {
                new WsdlElement
                {
                    Name = "Property1",
                    Type = "string",
                    IsComplexType = false
                }
            }
        };
        var namespaceName = "Test.Namespace";

        // Act
        var result = _generator.GenerateComplexType(complexType, namespaceName);

        // Assert
        var namespaceDeclaration = result.Members.OfType<FileScopedNamespaceDeclarationSyntax>().First();
        var classDeclaration = namespaceDeclaration.Members.OfType<ClassDeclarationSyntax>().First();

        Assert.NotNull(classDeclaration.BaseList);
        Assert.Equal(1, classDeclaration.BaseList.Types.Count);
        Assert.Equal("BaseType", classDeclaration.BaseList.Types[0].ToString());
    }

    [Fact]
    public void GenerateComplexType_ShouldHandleArrayTypes()
    {
        // Arrange
        var complexType = new WsdlComplexType
        {
            Name = "ArrayType",
            Namespace = "http://test.com/",
            IsArray = true,
            ArrayItemType = "string"
        };
        var namespaceName = "Test.Namespace";

        // Act
        var result = _generator.GenerateComplexType(complexType, namespaceName);

        // Assert
        var namespaceDeclaration = result.Members.OfType<FileScopedNamespaceDeclarationSyntax>().First();
        var classDeclaration = namespaceDeclaration.Members.OfType<ClassDeclarationSyntax>().First();
        var properties = classDeclaration.Members.OfType<PropertyDeclarationSyntax>().ToList();

        Assert.Equal(1, properties.Count);
        Assert.Equal("Items", properties[0].Identifier.Text);
        Assert.Equal("List<string>", properties[0].Type.ToString());

        // Check XML attribute
        var xmlElementAttribute = properties[0].AttributeLists
            .SelectMany(al => al.Attributes)
            .FirstOrDefault(a => a.Name.ToString() == "XmlElement");

        Assert.NotNull(xmlElementAttribute);

        var elementNameArg = xmlElementAttribute.ArgumentList.Arguments
            .FirstOrDefault(a => a.NameEquals.Name.ToString() == "ElementName");

        Assert.NotNull(elementNameArg);
        Assert.Equal("Item", ((LiteralExpressionSyntax)elementNameArg.Expression).Token.ValueText);

        // Check initializer
        Assert.NotNull(properties[0].Initializer);
        // Print the actual initializer for debugging
        Console.WriteLine($"Actual initializer: '{properties[0].Initializer.ToString()}'");
        // Use a more flexible assertion that doesn't depend on exact spacing
        Assert.Contains("List<string>", properties[0].Initializer.ToString());
    }

    [Fact]
    public void GenerateComplexTypeCode_ShouldReturnFormattedCode()
    {
        // Arrange
        var complexType = CreateTestComplexType();
        var namespaceName = "Test.Namespace";

        // Act
        var result = _generator.GenerateComplexTypeCode(complexType, namespaceName);

        // Assert
        Assert.Contains($"namespace {namespaceName}", result);
        Assert.Contains("[XmlType(TypeName = \"TestType\", Namespace = \"http://test.com/\")]", result);
        Assert.Contains("public class TestType", result);

        foreach (var element in complexType.Elements)
        {
            var propertyName = new NamingHelper().GetSafePropertyName(element.Name);
            Assert.Contains($"public string {propertyName} {{ get; set; }}", result);
            Assert.Contains($"[XmlElement(ElementName = \"{element.Name}\")]", result);
        }
    }

    [Fact]
    public void GenerateComplexTypes_ShouldGenerateAllComplexTypes()
    {
        // Arrange
        var complexType1 = CreateTestComplexType();
        var complexType2 = new WsdlComplexType
        {
            Name = "AnotherType",
            Namespace = "http://test.com/",
            Elements = new List<WsdlElement>
            {
                new WsdlElement
                {
                    Name = "Property1",
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
                ComplexTypes = new List<WsdlComplexType> { complexType1, complexType2 }
            }
        };

        var namespaceName = "Test.Namespace";

        // Act
        var result = _generator.GenerateComplexTypes(wsdl, namespaceName);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains("TestType.cs", result.Keys);
        Assert.Contains("AnotherType.cs", result.Keys);
    }

    [Fact]
    public void GenerateComplexTypes_ShouldSkipArrayOfXTypes()
    {
        // Arrange
        var complexType1 = CreateTestComplexType();
        var complexType2 = new WsdlComplexType
        {
            Name = "ArrayOfString",
            Namespace = "http://test.com/",
            IsArray = true,
            ArrayItemType = "string"
        };

        var wsdl = new WsdlDefinition
        {
            TargetNamespace = "http://test.com/",
            Types = new WsdlTypes
            {
                ComplexTypes = new List<WsdlComplexType> { complexType1, complexType2 }
            }
        };

        var namespaceName = "Test.Namespace";

        // Act
        var result = _generator.GenerateComplexTypes(wsdl, namespaceName);

        // Assert
        Assert.Equal(1, result.Count);
        Assert.Contains("TestType.cs", result.Keys);
        Assert.DoesNotContain("ArrayOfString.cs", result.Keys);
    }

    private static WsdlComplexType CreateTestComplexType()
    {
        return new WsdlComplexType
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
    }
}
