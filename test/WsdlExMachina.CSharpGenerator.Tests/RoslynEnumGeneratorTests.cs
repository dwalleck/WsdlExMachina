using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WsdlExMachina.Parser.Models;
using Xunit;

namespace WsdlExMachina.CSharpGenerator.Tests;

public class RoslynEnumGeneratorTests
{
    private readonly RoslynEnumGenerator _generator;
    private readonly RoslynCodeGenerator _codeGenerator;

    public RoslynEnumGeneratorTests()
    {
        _codeGenerator = new RoslynCodeGenerator();
        _generator = new RoslynEnumGenerator(_codeGenerator);
    }

    [Fact]
    public void GenerateEnum_ShouldThrowArgumentNullException_WhenSimpleTypeIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _generator.GenerateEnum(null, "Test.Namespace"));
    }

    [Fact]
    public void GenerateEnum_ShouldThrowArgumentException_WhenNamespaceIsEmpty()
    {
        // Arrange
        var simpleType = CreateTestSimpleType();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _generator.GenerateEnum(simpleType, string.Empty));
    }

    [Fact]
    public void GenerateEnum_ShouldThrowArgumentException_WhenSimpleTypeIsNotEnum()
    {
        // Arrange
        var simpleType = new WsdlSimpleType
        {
            Name = "TestType",
            Namespace = "http://test.com/",
            EnumerationValues = new List<string>() // Empty list means IsEnum will be false
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _generator.GenerateEnum(simpleType, "Test.Namespace"));
    }

    [Fact]
    public void GenerateEnum_ShouldCreateEnumWithCorrectName()
    {
        // Arrange
        var simpleType = CreateTestSimpleType();
        var namespaceName = "Test.Namespace";

        // Act
        var result = _generator.GenerateEnum(simpleType, namespaceName);

        // Assert
        var namespaceDeclaration = result.Members.OfType<FileScopedNamespaceDeclarationSyntax>().First();
        var enumDeclaration = namespaceDeclaration.Members.OfType<EnumDeclarationSyntax>().First();

        Assert.Equal(simpleType.Name, enumDeclaration.Identifier.Text);
    }

    [Fact]
    public void GenerateEnum_ShouldCreateEnumWithCorrectXmlTypeAttribute()
    {
        // Arrange
        var simpleType = CreateTestSimpleType();
        var namespaceName = "Test.Namespace";

        // Act
        var result = _generator.GenerateEnum(simpleType, namespaceName);

        // Assert
        var namespaceDeclaration = result.Members.OfType<FileScopedNamespaceDeclarationSyntax>().First();
        var enumDeclaration = namespaceDeclaration.Members.OfType<EnumDeclarationSyntax>().First();
        var xmlTypeAttribute = enumDeclaration.AttributeLists
            .SelectMany(al => al.Attributes)
            .FirstOrDefault(a => a.Name.ToString() == "XmlType");

        Assert.NotNull(xmlTypeAttribute);

        var typeNameArg = xmlTypeAttribute.ArgumentList.Arguments
            .FirstOrDefault(a => a.NameEquals.Name.ToString() == "TypeName");
        var namespaceArg = xmlTypeAttribute.ArgumentList.Arguments
            .FirstOrDefault(a => a.NameEquals.Name.ToString() == "Namespace");

        Assert.NotNull(typeNameArg);
        Assert.NotNull(namespaceArg);
        Assert.Equal(simpleType.Name, ((LiteralExpressionSyntax)typeNameArg.Expression).Token.ValueText);
        Assert.Equal(simpleType.Namespace, ((LiteralExpressionSyntax)namespaceArg.Expression).Token.ValueText);
    }

    [Fact]
    public void GenerateEnum_ShouldCreateEnumWithCorrectMembers()
    {
        // Arrange
        var simpleType = CreateTestSimpleType();
        var namespaceName = "Test.Namespace";

        // Act
        var result = _generator.GenerateEnum(simpleType, namespaceName);

        // Assert
        var namespaceDeclaration = result.Members.OfType<FileScopedNamespaceDeclarationSyntax>().First();
        var enumDeclaration = namespaceDeclaration.Members.OfType<EnumDeclarationSyntax>().First();
        var enumMembers = enumDeclaration.Members;

        Assert.Equal(simpleType.EnumerationValues.Count, enumMembers.Count);

        for (int i = 0; i < simpleType.EnumerationValues.Count; i++)
        {
            var expectedValue = simpleType.EnumerationValues[i];
            var member = enumMembers[i];

            // Check XML attribute
            var xmlEnumAttribute = member.AttributeLists
                .SelectMany(al => al.Attributes)
                .FirstOrDefault(a => a.Name.ToString() == "XmlEnum");

            Assert.NotNull(xmlEnumAttribute);

            var nameArg = xmlEnumAttribute.ArgumentList.Arguments
                .FirstOrDefault(a => a.NameEquals.Name.ToString() == "Name");

            Assert.NotNull(nameArg);
            Assert.Equal(expectedValue, ((LiteralExpressionSyntax)nameArg.Expression).Token.ValueText);
        }
    }

    [Fact]
    public void GenerateEnumCode_ShouldReturnFormattedCode()
    {
        // Arrange
        var simpleType = CreateTestSimpleType();
        var namespaceName = "Test.Namespace";

        // Act
        var result = _generator.GenerateEnumCode(simpleType, namespaceName);

        // Assert
        Assert.Contains($"namespace {namespaceName}", result);
        Assert.Contains("[XmlType(TypeName = \"TestEnum\", Namespace = \"http://test.com/\")]", result);
        Assert.Contains("public enum TestEnum", result);

        foreach (var value in simpleType.EnumerationValues)
        {
            Assert.Contains($"[XmlEnum(Name = \"{value}\")]", result);
        }
    }

    private static WsdlSimpleType CreateTestSimpleType()
    {
        return new WsdlSimpleType
        {
            Name = "TestEnum",
            Namespace = "http://test.com/",
            EnumerationValues = new List<string> { "Value1", "Value2", "Value3" }
        };
    }
}
