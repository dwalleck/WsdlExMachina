using System.Collections.Generic;
using WsdlExMachina.Generator.Generators;
using WsdlExMachina.Parser.Models;

namespace WsdlExMachina.Generator;

/// <summary>
/// Generates C# SOAP client code across multiple files.
/// </summary>
public class MultiFileGenerator
{
    private readonly SoapClientGenerator _soapClientGenerator;
    private readonly IEnumerable<ICodeGenerator> _generators;

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiFileGenerator"/> class.
    /// </summary>
    /// <param name="soapClientGenerator">The SOAP client generator.</param>
    public MultiFileGenerator(SoapClientGenerator soapClientGenerator)
    {
        _soapClientGenerator = soapClientGenerator;

        // Initialize all generators
        _generators = new List<ICodeGenerator>
        {
            new DirectoryStructureGenerator(),
            new SoapClientBaseGenerator(),
            new ServiceCollectionExtensionsGenerator(),
            new ClientOptionsGenerator(),
            new ModelsGenerator(soapClientGenerator),
            new InterfacesGenerator(),
            new ClientsGenerator(),
            new ProjectFileGenerator()
        };
    }

    /// <summary>
    /// Generates C# SOAP client code across multiple files.
    /// </summary>
    /// <param name="wsdlDefinition">The WSDL definition.</param>
    /// <param name="outputNamespace">The namespace to use for the generated code.</param>
    /// <param name="outputDirectory">The directory where the files will be created.</param>
    public void Generate(WsdlDefinition wsdlDefinition, string outputNamespace, string outputDirectory)
    {
        // Run each generator in sequence
        foreach (var generator in _generators)
        {
            generator.Generate(wsdlDefinition, outputNamespace, outputDirectory);
        }
    }
}
