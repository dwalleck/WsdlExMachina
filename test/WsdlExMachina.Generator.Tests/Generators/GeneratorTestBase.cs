using System;
using System.IO;
using WsdlExMachina.Parser;
using WsdlExMachina.Parser.Models;

namespace WsdlExMachina.Generator.Tests.Generators
{
    public abstract class GeneratorTestBase
    {
        protected readonly string SampleWsdlPath;
        protected readonly string OutputDir;
        protected readonly string OutputNamespace;
        protected readonly WsdlDefinition WsdlDefinition;

        protected GeneratorTestBase()
        {
            SampleWsdlPath = Path.Combine("..", "..", "..", "..", "..", "samples", "ACH.wsdl");
            OutputDir = Path.Combine(Path.GetTempPath(), "WsdlExMachina_Test_" + Guid.NewGuid());
            OutputNamespace = "TestNamespace";

            // Parse the WSDL file
            var parser = new WsdlParser();
            WsdlDefinition = parser.ParseFile(SampleWsdlPath);

            // Create the output directory
            Directory.CreateDirectory(OutputDir);
        }

        protected void CleanupOutputDirectory()
        {
            if (Directory.Exists(OutputDir))
            {
                Directory.Delete(OutputDir, true);
            }
        }
    }
}
