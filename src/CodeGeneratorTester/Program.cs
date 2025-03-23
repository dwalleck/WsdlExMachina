using WsdlExMachina.Parser;
using WsdlExMachina.CSharpGenerator;

namespace CodeGeneratorTester;

/// <summary>
/// A console application to test the WSDL to C# code generator.
/// </summary>
public class Program
{
    /// <summary>
    /// The entry point for the application.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    public static void Main(string[] args)
    {
        var wsdlFilePath = "/home/dwalleck/ACH.wsdl";
        var outputDirectory = args.Length > 1 ? args[1] : "generated-code";

        try
        {
            // Parse the WSDL file
            Console.WriteLine($"Parsing WSDL file: {wsdlFilePath}");
            var parser = new WsdlParser();
            var wsdl = parser.ParseFile(wsdlFilePath);

            // // Debug output for WSDL structure
            // Console.WriteLine("\nDebug: WSDL Structure");
            // Console.WriteLine("====================");

            // // Debug messages
            // Console.WriteLine("\nMessages:");
            // foreach (var message in wsdl.Messages)
            // {
            //     Console.WriteLine($"  Message: {message.Name}");
            //     foreach (var part in message.Parts)
            //     {
            //         Console.WriteLine($"    Part: {part.Name}, Element: {part.Element}, Type: {part.Type}");
            //     }
            // }

            // // Debug elements
            // Console.WriteLine("\nElements:");
            // foreach (var element in wsdl.Types.Elements)
            // {
            //     Console.WriteLine($"  Element: {element.Name}, Type: {element.Type}, IsComplexType: {element.IsComplexType}");

            //     // Note: WsdlElement doesn't have a Children property
            // }

            // // Debug complex types
            // Console.WriteLine("\nComplex Types:");
            // foreach (var complexType in wsdl.Types.ComplexTypes)
            // {
            //     Console.WriteLine($"  ComplexType: {complexType.Name}");
            //     foreach (var element in complexType.Elements)
            //     {
            //         Console.WriteLine($"    Element: {element.Name}, Type: {element.Type}, IsComplexType: {element.IsComplexType}");
            //     }
            // }

            // Generate C# code
            Console.WriteLine("\nGenerating C# code...");
            var generator = new CSharpGenerator();
            var result = generator.Generate(wsdl);

            // Display summary
            Console.WriteLine();
            Console.WriteLine(generator.GenerateSummary(result));

            // Save generated code to files
            if (!string.IsNullOrEmpty(outputDirectory))
            {
                Console.WriteLine($"Saving generated code to: {outputDirectory}");
                result.SaveToDirectory(outputDirectory);
                Console.WriteLine("Code generation completed successfully.");
            }

            // Display the generated code
            // Console.WriteLine();
            // Console.WriteLine("Generated Code:");
            // Console.WriteLine("===============");
            // Console.WriteLine();
            // Console.WriteLine(result.GetAllContent());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
}
