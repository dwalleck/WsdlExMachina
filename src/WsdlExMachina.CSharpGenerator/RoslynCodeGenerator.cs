using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using WsdlExMachina.Parser.Models;

namespace WsdlExMachina.CSharpGenerator;

/// <summary>
/// Generates C# code using the Roslyn API.
/// </summary>
public class RoslynCodeGenerator
{
    private readonly TypeMapper _typeMapper;
    private readonly NamingHelper _namingHelper;
    private readonly Dictionary<string, string> _generatedTypes;

    /// <summary>
    /// Initializes a new instance of the <see cref="RoslynCodeGenerator"/> class.
    /// </summary>
    public RoslynCodeGenerator()
    {
        _typeMapper = new TypeMapper();
        _namingHelper = new NamingHelper();
        _generatedTypes = new Dictionary<string, string>();
    }

    /// <summary>
    /// Creates a file-scoped namespace declaration with using directives stored for later use.
    /// </summary>
    /// <param name="namespaceName">The namespace name.</param>
    /// <param name="usingDirectives">The using directives.</param>
    /// <returns>A namespace declaration syntax with using directives stored as annotations.</returns>
    public BaseNamespaceDeclarationSyntax CreateNamespace(string namespaceName, params string[] usingDirectives)
    {
        // Create file-scoped namespace declaration (C# 10 style)
        var namespaceDeclaration = SyntaxFactory.FileScopedNamespaceDeclaration(
            SyntaxFactory.ParseName(namespaceName));

        // Store the using directives in a property that can be accessed later
        namespaceDeclaration = namespaceDeclaration.WithAdditionalAnnotations(
            new SyntaxAnnotation("UsingDirectives", string.Join(",", usingDirectives)));

        return namespaceDeclaration;
    }

    /// <summary>
    /// Gets the using directives stored in a namespace declaration.
    /// </summary>
    /// <param name="namespaceDeclaration">The namespace declaration.</param>
    /// <returns>A list of using directive syntax nodes.</returns>
    public List<UsingDirectiveSyntax> GetUsingDirectives(BaseNamespaceDeclarationSyntax namespaceDeclaration)
    {
        var usingDirectives = new List<UsingDirectiveSyntax>();
        var annotation = namespaceDeclaration.GetAnnotations("UsingDirectives").FirstOrDefault();

        if (annotation != null)
        {
            var usingDirectiveNames = annotation.Data.Split(',');
            foreach (var usingDirectiveName in usingDirectiveNames)
            {
                usingDirectives.Add(
                    SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(usingDirectiveName)));
            }
        }

        return usingDirectives;
    }

    /// <summary>
    /// Creates an XML documentation comment.
    /// </summary>
    /// <param name="summary">The summary text.</param>
    /// <returns>A documentation comment trivia syntax.</returns>
    public DocumentationCommentTriviaSyntax CreateDocumentationComment(string summary)
    {
        var summaryElement = SyntaxFactory.XmlElement(
            SyntaxFactory.XmlElementStartTag(SyntaxFactory.XmlName("summary")),
            SyntaxFactory.List(new XmlNodeSyntax[] {
                SyntaxFactory.XmlText(
                    SyntaxFactory.XmlTextLiteral(
                        SyntaxFactory.TriviaList(),
                        summary,
                        summary,
                        SyntaxFactory.TriviaList()))
            }),
            SyntaxFactory.XmlElementEndTag(SyntaxFactory.XmlName("summary")));

        return SyntaxFactory.DocumentationCommentTrivia(
            SyntaxKind.SingleLineDocumentationCommentTrivia,
            SyntaxFactory.List(new XmlNodeSyntax[] { summaryElement }));
    }

    /// <summary>
    /// Creates an XML attribute.
    /// </summary>
    /// <param name="attributeName">The attribute name.</param>
    /// <param name="arguments">The attribute arguments.</param>
    /// <returns>An attribute syntax.</returns>
    public AttributeSyntax CreateXmlAttribute(string attributeName, params (string name, string value)[] arguments)
    {
        var attributeArguments = new List<AttributeArgumentSyntax>();

        foreach (var (name, value) in arguments)
        {
            attributeArguments.Add(
                SyntaxFactory.AttributeArgument(
                    SyntaxFactory.NameEquals(SyntaxFactory.IdentifierName(name)),
                    null,
                    SyntaxFactory.LiteralExpression(
                        SyntaxKind.StringLiteralExpression,
                        SyntaxFactory.Literal(value))));
        }

        return SyntaxFactory.Attribute(
            SyntaxFactory.ParseName(attributeName),
            SyntaxFactory.AttributeArgumentList(
                SyntaxFactory.SeparatedList(attributeArguments)));
    }

    /// <summary>
    /// Formats a syntax node.
    /// </summary>
    /// <param name="node">The syntax node to format.</param>
    /// <returns>The formatted syntax node as a string.</returns>
    public string FormatNode(SyntaxNode node)
    {
        var workspace = new AdhocWorkspace();
        var options = workspace.Options
            .WithChangedOption(FormattingOptions.IndentationSize, LanguageNames.CSharp, 4)
            .WithChangedOption(FormattingOptions.UseTabs, LanguageNames.CSharp, false);

        var formattedNode = Formatter.Format(node, workspace, options);

        // Convert the formatted node to a string
        var code = formattedNode.ToFullString();

        // Fix XML documentation comments
        code = FixXmlDocumentationComments(code);

        return code;
    }

    /// <summary>
    /// Fixes XML documentation comments in the generated code.
    /// </summary>
    /// <param name="code">The code to fix.</param>
    /// <returns>The fixed code.</returns>
    private static string FixXmlDocumentationComments(string code)
    {
        // Replace <summary> tags with /// <summary> tags
        code = System.Text.RegularExpressions.Regex.Replace(
            code,
            @"(\s+)<summary>",
            "$1/// <summary>");

        // Replace [XmlRoot and [XmlElement attributes that are on the same line as the summary
        code = System.Text.RegularExpressions.Regex.Replace(
            code,
            @"</summary>\s+\[(Xml\w+)",
            "</summary>\r\n    [$1");

        // Fix indentation of XML attributes
        code = System.Text.RegularExpressions.Regex.Replace(
            code,
            @"(\s+)/// <summary>(.*)</summary>\r?\n\s+\[(Xml\w+)",
            "$1/// <summary>$2</summary>\r\n$1[$3");

        // Remove all blank lines between documentation comments and attributes
        code = System.Text.RegularExpressions.Regex.Replace(
            code,
            @"(/// <summary>.*</summary>)\r?\n\r?\n(\s+\[Xml)",
            "$1\r\n$2");

        // Remove all blank lines between attributes and class/property declarations
        code = System.Text.RegularExpressions.Regex.Replace(
            code,
            @"(\[Xml\w+.*\])\r?\n\r?\n(\s+public)",
            "$1\r\n$2");

        // Fix any remaining blank lines
        code = System.Text.RegularExpressions.Regex.Replace(
            code,
            @"(\r?\n\r?\n)(\s+///)",
            "\r\n$2");

        code = System.Text.RegularExpressions.Regex.Replace(
            code,
            @"(\r?\n\r?\n)(\s+\[Xml)",
            "\r\n$2");

        return code;
    }
}
