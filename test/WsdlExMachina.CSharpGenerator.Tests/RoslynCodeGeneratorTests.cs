using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace WsdlExMachina.CSharpGenerator.Tests;

public class RoslynCodeGeneratorTests
{
    private readonly RoslynCodeGenerator _generator;

    public RoslynCodeGeneratorTests()
    {
        _generator = new RoslynCodeGenerator();
    }

    [Fact]
    public void CreateNamespace_ShouldCreateFileScopedNamespaceWithUsingDirectives()
    {
        // Arrange
        var namespaceName = "Test.Namespace";
        var usingDirectives = new[] { "System", "System.Collections.Generic" };

        // Act
        var result = _generator.CreateNamespace(namespaceName, usingDirectives);

        // Assert
        Assert.Equal(namespaceName, result.Name.ToString());
        Assert.IsType<FileScopedNamespaceDeclarationSyntax>(result);

        // Get the using directives from the annotation
        var retrievedUsingDirectives = _generator.GetUsingDirectives(result);
        Assert.Equal(usingDirectives.Length, retrievedUsingDirectives.Count);

        foreach (var usingDirective in usingDirectives)
        {
            Assert.Contains(retrievedUsingDirectives, u => u.Name.ToString() == usingDirective);
        }
    }

    [Fact]
    public void CreateDocumentationComment_ShouldCreateSummaryElement()
    {
        // Arrange
        var summary = "This is a test summary.";

        // Act
        var result = _generator.CreateDocumentationComment(summary);

        // Assert
        Assert.Equal(SyntaxKind.SingleLineDocumentationCommentTrivia, result.Kind());

        var summaryElement = result.Content.OfType<XmlElementSyntax>()
            .FirstOrDefault(e => e.StartTag.Name.ToString() == "summary");

        Assert.NotNull(summaryElement);
        Assert.Contains(summary, summaryElement.Content.ToString());
    }

    [Fact]
    public void CreateXmlAttribute_ShouldCreateAttributeWithArguments()
    {
        // Arrange
        var attributeName = "XmlType";
        var arguments = new[] { ("TypeName", "TestType"), ("Namespace", "http://test.com/") };

        // Act
        var result = _generator.CreateXmlAttribute(attributeName, arguments);

        // Assert
        Assert.Equal(attributeName, result.Name.ToString());
        Assert.Equal(arguments.Length, result.ArgumentList.Arguments.Count);

        foreach (var (name, value) in arguments)
        {
            Assert.Contains(result.ArgumentList.Arguments,
                a => a.NameEquals.Name.ToString() == name &&
                     ((LiteralExpressionSyntax)a.Expression).Token.ValueText == value);
        }
    }

    [Fact]
    public void FormatNode_ShouldFormatNodeCorrectly()
    {
        // Arrange
        var classDeclaration = SyntaxFactory.ClassDeclaration("TestClass")
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .AddMembers(
                SyntaxFactory.PropertyDeclaration(
                    SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword)),
                    SyntaxFactory.Identifier("TestProperty"))
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddAccessorListAccessors(
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))));

        // Act
        var result = _generator.FormatNode(classDeclaration);

        // Assert
        Assert.Contains("public class TestClass", result);
        Assert.Contains("public string TestProperty { get; set; }", result);
    }
}
