using Microsoft.CodeAnalysis.CSharp.Syntax;
using WsdlExMachina.Parser.Models;

namespace WsdlExMachina.Generator.Generators.SoapClient;

/// <summary>
/// Interface for generators that create components of the SoapClientBase class.
/// </summary>
public interface ISoapClientComponentGenerator
{
    /// <summary>
    /// Generates a component of the SoapClientBase class.
    /// </summary>
    /// <param name="wsdlDefinition">The WSDL definition.</param>
    /// <returns>A collection of member declarations to add to the SoapClientBase class.</returns>
    MemberDeclarationSyntax[] Generate(WsdlDefinition wsdlDefinition);
}
