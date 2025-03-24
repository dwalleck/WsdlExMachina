using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WsdlExMachina.Parser.Models;

namespace WsdlExMachina.CSharpGenerator
{
    /// <summary>
    /// Generates C# enum types using the Roslyn API.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="RoslynEnumGenerator"/> class.
    /// </remarks>
    /// <param name="codeGenerator">The code generator.</param>
    public class RoslynEnumGenerator(RoslynCodeGenerator codeGenerator)
    {
        private readonly RoslynCodeGenerator _codeGenerator = codeGenerator ?? throw new ArgumentNullException(nameof(codeGenerator));
        private readonly NamingHelper _namingHelper = new NamingHelper();

        /// <summary>
        /// Generates a C# enum type from a WSDL simple type.
        /// </summary>
        /// <param name="simpleType">The WSDL simple type.</param>
        /// <param name="namespaceName">The namespace name.</param>
        /// <returns>A compilation unit syntax representing the enum type.</returns>
        public CompilationUnitSyntax GenerateEnum(WsdlSimpleType simpleType, string namespaceName)
        {
            if (simpleType == null)
                throw new ArgumentNullException(nameof(simpleType));

            if (string.IsNullOrEmpty(namespaceName))
                throw new ArgumentException("Namespace name cannot be null or empty.", nameof(namespaceName));

            if (!simpleType.IsEnum)
                throw new ArgumentException("Simple type must be an enumeration.", nameof(simpleType));

            // Create file-scoped namespace
            var namespaceDeclaration = _codeGenerator.CreateNamespace(
                namespaceName,
                "System",
                "System.Xml.Serialization");

            // Get using directives
            var usingDirectives = _codeGenerator.GetUsingDirectives(namespaceDeclaration);

            // Create XML type attribute
            var xmlTypeAttribute = _codeGenerator.CreateXmlAttribute(
                "XmlType",
                ("TypeName", simpleType.Name),
                ("Namespace", simpleType.Namespace ?? string.Empty));

            // Create enum members
            var enumMembers = new List<EnumMemberDeclarationSyntax>();

            for (int i = 0; i < simpleType.EnumerationValues.Count; i++)
            {
                var value = simpleType.EnumerationValues[i];
                var enumValueName = _namingHelper.GetSafePropertyName(value);

                // Create XML enum attribute
                var xmlEnumAttribute = _codeGenerator.CreateXmlAttribute(
                    "XmlEnum",
                    ("Name", value));

                // Create enum member with XML attribute
                var enumMember = SyntaxFactory.EnumMemberDeclaration(
                    SyntaxFactory.List<AttributeListSyntax>().Add(
                        SyntaxFactory.AttributeList(
                            SyntaxFactory.SingletonSeparatedList(xmlEnumAttribute))),
                    SyntaxFactory.Identifier(enumValueName),
                    null);

                enumMembers.Add(enumMember);
            }

            // Create enum declaration
            var enumDeclaration = SyntaxFactory.EnumDeclaration(
                SyntaxFactory.List<AttributeListSyntax>().Add(
                    SyntaxFactory.AttributeList(
                        SyntaxFactory.SingletonSeparatedList(xmlTypeAttribute))),
                SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)),
                SyntaxFactory.Identifier(_namingHelper.GetSafeClassName(simpleType.Name)),
                null,
                SyntaxFactory.SeparatedList(enumMembers));

            // Add documentation
            var documentationComment = _codeGenerator.CreateDocumentationComment($"Represents the {simpleType.Name} enumeration.");
            var documentation = SyntaxFactory.TriviaList(SyntaxFactory.Trivia(documentationComment));

            enumDeclaration = enumDeclaration.WithLeadingTrivia(documentation);

            // Add enum to namespace
            if (namespaceDeclaration is FileScopedNamespaceDeclarationSyntax fileScopedNamespace)
            {
                namespaceDeclaration = fileScopedNamespace.AddMembers(enumDeclaration);
            }
            else if (namespaceDeclaration is NamespaceDeclarationSyntax regularNamespace)
            {
                namespaceDeclaration = regularNamespace.AddMembers(enumDeclaration);
            }

            // Create compilation unit
            return SyntaxFactory.CompilationUnit()
                .AddUsings(usingDirectives.ToArray())
                .AddMembers(namespaceDeclaration);
        }

        /// <summary>
        /// Generates C# code for a WSDL simple type.
        /// </summary>
        /// <param name="simpleType">The WSDL simple type.</param>
        /// <param name="namespaceName">The namespace name.</param>
        /// <returns>The generated C# code as a string.</returns>
        public string GenerateEnumCode(WsdlSimpleType simpleType, string namespaceName)
        {
            var compilationUnit = GenerateEnum(simpleType, namespaceName);
            return _codeGenerator.FormatNode(compilationUnit);
        }
    }
}
