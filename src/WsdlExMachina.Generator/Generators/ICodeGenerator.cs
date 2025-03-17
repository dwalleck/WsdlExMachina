using WsdlExMachina.Parser.Models;

namespace WsdlExMachina.Generator.Generators;

/// <summary>
/// Interface for code generators.
/// </summary>
public interface ICodeGenerator
{
    /// <summary>
    /// Generates code.
    /// </summary>
    /// <param name="wsdlDefinition">The WSDL definition.</param>
    /// <param name="outputNamespace">The namespace to use for the generated code.</param>
    /// <param name="outputDirectory">The directory where the files will be created.</param>
    void Generate(WsdlDefinition wsdlDefinition, string outputNamespace, string outputDirectory);
}
