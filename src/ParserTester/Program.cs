using System;
using System.IO;
using System.Linq;
using WsdlExMachina.Parser;
using WsdlExMachina.CSharpGenerator;

namespace ParserTester;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("WSDL Ex Machina - C# Code Generator");
        Console.WriteLine("===================================");

        // Check if a WSDL file path was provided
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: ParserTester <wsdl-file-path> [output-directory]");
            Console.WriteLine("Using default WSDL file: samples/ACH.wsdl");
            args = new[] { Path.Combine("samples", "ACH.wsdl") };
        }

        string wsdlFilePath = args[0];

        // Check if an output directory was provided
        string outputDirectory = args.Length > 1 ? args[1] : "generated-code";

        // Create the output directory if it doesn't exist
        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        try
        {
            // Parse the WSDL file
            Console.WriteLine($"Parsing WSDL file: {wsdlFilePath}");
            var parser = new WsdlParser();
            var wsdl = parser.ParseFile(wsdlFilePath);

            Console.WriteLine($"Found {wsdl.PortTypes?.Count ?? 0} port types with {wsdl.PortTypes?.Sum(pt => pt.Operations?.Count ?? 0) ?? 0} operations");
            Console.WriteLine($"Found {wsdl.Types?.ComplexTypes?.Count ?? 0} complex types");
            Console.WriteLine($"Found {wsdl.Types?.SimpleTypes?.Count ?? 0} simple types");
            Console.WriteLine($"Found {wsdl.Types?.Elements?.Count ?? 0} elements");

            // Generate C# code
            Console.WriteLine("Generating C# code...");

            try
            {
                // Extract namespace from WSDL target namespace
                string namespaceName = string.IsNullOrEmpty(wsdl.TargetNamespace)
                    ? "DefaultNamespace"
                    : new TypeMapper().GetCSharpNamespace(wsdl.TargetNamespace);
                Console.WriteLine($"Using namespace: {namespaceName}");

                // Create a generator facade
                var generator = new RoslynGeneratorFacade();

                // Generate all types at once
                var generatedCode = generator.GenerateAll(wsdl, namespaceName);
                Console.WriteLine($"Generated {generatedCode.Count} files");

                // Count by type
                int requestModelCount = 0;
                int complexTypeCount = 0;
                int simpleTypeCount = 0;

                foreach (var fileName in generatedCode.Keys)
                {
                    if (fileName.EndsWith("Request.cs"))
                    {
                        requestModelCount++;
                    }
                    else if (fileName.EndsWith(".cs") && !fileName.Contains("Enum"))
                    {
                        complexTypeCount++;
                    }
                    else
                    {
                        simpleTypeCount++;
                    }
                }

                Console.WriteLine($"Generated {requestModelCount} request model classes");
                Console.WriteLine($"Generated {complexTypeCount} complex type classes");
                Console.WriteLine($"Generated {simpleTypeCount} simple type enums");

                // Write the generated code to files
                Console.WriteLine($"Writing generated code to {outputDirectory}");

                // Create the output directory if it doesn't exist
                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                // Write all generated code to files
                foreach (var entry in generatedCode)
                {
                    string fileName = entry.Key;
                    string content = entry.Value;
                    File.WriteAllText(Path.Combine(outputDirectory, fileName), content);
                }

                Console.WriteLine("Code generation complete!");
                Console.WriteLine($"Generated {generatedCode.Count} files in {outputDirectory}");
            }
            catch (CodeGenerationException ex)
            {
                Console.WriteLine($"Code generation error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Caused by: {ex.InnerException.Message}");
                }
                Console.WriteLine(ex.StackTrace);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
}
