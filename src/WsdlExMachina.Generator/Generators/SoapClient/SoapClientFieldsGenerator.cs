using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WsdlExMachina.Parser.Models;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace WsdlExMachina.Generator.Generators.SoapClient;

/// <summary>
/// Generates fields and constructor for the SoapClientBase class.
/// </summary>
public class SoapClientFieldsGenerator : ISoapClientComponentGenerator
{
    /// <summary>
    /// Generates fields and constructor for the SoapClientBase class.
    /// </summary>
    /// <param name="wsdlDefinition">The WSDL definition.</param>
    /// <returns>A collection of member declarations to add to the SoapClientBase class.</returns>
    public MemberDeclarationSyntax[] Generate(WsdlDefinition wsdlDefinition)
    {
        return new MemberDeclarationSyntax[]
        {
            // Add fields
            FieldDeclaration(
                VariableDeclaration(
                    IdentifierName("HttpClient")
                )
                .WithVariables(
                    SingletonSeparatedList(
                        VariableDeclarator(
                            Identifier("_httpClient")
                        )
                    )
                )
            )
            .AddModifiers(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.ReadOnlyKeyword)),

            FieldDeclaration(
                VariableDeclaration(
                    PredefinedType(Token(SyntaxKind.StringKeyword))
                )
                .WithVariables(
                    SingletonSeparatedList(
                        VariableDeclarator(
                            Identifier("_endpoint")
                        )
                    )
                )
            )
            .AddModifiers(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.ReadOnlyKeyword)),

            // Add constructor
            ConstructorDeclaration(Identifier("SoapClientBase"))
                .AddModifiers(Token(SyntaxKind.ProtectedKeyword))
                .AddParameterListParameters(
                    Parameter(Identifier("endpoint"))
                        .WithType(PredefinedType(Token(SyntaxKind.StringKeyword))),
                    Parameter(Identifier("httpClient"))
                        .WithType(IdentifierName("HttpClient"))
                )
                .WithBody(
                    Block(
                        ExpressionStatement(
                            AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                IdentifierName("_endpoint"),
                                IdentifierName("endpoint")
                            )
                        ),
                        ExpressionStatement(
                            AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                IdentifierName("_httpClient"),
                                IdentifierName("httpClient")
                            )
                        )
                    )
                )
        };
    }
}
