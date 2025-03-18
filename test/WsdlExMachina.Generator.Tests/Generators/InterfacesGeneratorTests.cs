using System.IO;
using System.Linq;
using Xunit;
using WsdlExMachina.Generator.Generators;

namespace WsdlExMachina.Generator.Tests.Generators
{
    public class InterfacesGeneratorTests : GeneratorTestBase, IDisposable
    {
        private readonly InterfacesGenerator _generator;
        private readonly DirectoryStructureGenerator _directoryGenerator;

        public InterfacesGeneratorTests()
        {
            _generator = new InterfacesGenerator();
            _directoryGenerator = new DirectoryStructureGenerator();

            // Create the required directory structure
            _directoryGenerator.Generate(WsdlDefinition, OutputNamespace, OutputDir);
        }

        [Fact]
        public void Generate_CreatesInterfaceFiles()
        {
            // Act
            _generator.Generate(WsdlDefinition, OutputNamespace, OutputDir);

            // Assert
            foreach (var portType in WsdlDefinition.PortTypes)
            {
                var filePath = Path.Combine(OutputDir, "Interfaces", $"I{portType.Name}.cs");
                Assert.True(File.Exists(filePath), $"Interface file {filePath} should exist");
            }
        }

        [Fact]
        public void Generate_InterfaceFilesContainCorrectNamespace()
        {
            // Act
            _generator.Generate(WsdlDefinition, OutputNamespace, OutputDir);

            // Assert
            foreach (var portType in WsdlDefinition.PortTypes)
            {
                var filePath = Path.Combine(OutputDir, "Interfaces", $"I{portType.Name}.cs");
                var fileContent = File.ReadAllText(filePath);
                Assert.Contains($"namespace {OutputNamespace}.Interfaces", fileContent);
            }
        }

        [Fact]
        public void Generate_InterfaceFilesContainPublicInterface()
        {
            // Act
            _generator.Generate(WsdlDefinition, OutputNamespace, OutputDir);

            // Assert
            foreach (var portType in WsdlDefinition.PortTypes)
            {
                var filePath = Path.Combine(OutputDir, "Interfaces", $"I{portType.Name}.cs");
                var fileContent = File.ReadAllText(filePath);
                Assert.Contains($"public interface I{portType.Name}", fileContent);
            }
        }

        [Fact]
        public void Generate_InterfaceFilesContainAsyncMethods()
        {
            // Act
            _generator.Generate(WsdlDefinition, OutputNamespace, OutputDir);

            // Assert
            foreach (var portType in WsdlDefinition.PortTypes)
            {
                var filePath = Path.Combine(OutputDir, "Interfaces", $"I{portType.Name}.cs");
                var fileContent = File.ReadAllText(filePath);

                // Check for at least one async method
                if (portType.Operations.Any())
                {
                    var operation = portType.Operations.First();
                    Assert.Contains($"{operation.Name}Async", fileContent);
                }
            }
        }

        [Fact]
        public void Generate_HandlesAllOperations()
        {
            // Act
            _generator.Generate(WsdlDefinition, OutputNamespace, OutputDir);

            // Assert
            foreach (var portType in WsdlDefinition.PortTypes)
            {
                var filePath = Path.Combine(OutputDir, "Interfaces", $"I{portType.Name}.cs");
                var fileContent = File.ReadAllText(filePath);

                // Count the number of operations in the WSDL
                int operationCount = portType.Operations.Count;

                // Count the number of Async methods in the generated code
                int asyncMethodCount = 0;
                int index = 0;
                while ((index = fileContent.IndexOf("Async(", index + 1)) != -1)
                {
                    asyncMethodCount++;
                }

                // The number of Async methods should match the number of operations
                Assert.Equal(operationCount, asyncMethodCount);
            }
        }

        [Fact]
        public void Generate_HandlesOperationsWithSOR()
        {
            // Act
            _generator.Generate(WsdlDefinition, OutputNamespace, OutputDir);

            // Assert
            foreach (var portType in WsdlDefinition.PortTypes)
            {
                var filePath = Path.Combine(OutputDir, "Interfaces", $"I{portType.Name}.cs");
                var fileContent = File.ReadAllText(filePath);

                // Check for the specific SOR operation
                Assert.Contains("PostSinglePaymentWithAchWebValidationWithACHWebValidationSORAsync", fileContent);
            }
        }

        [Fact]
        public void Generate_InterfaceFilesContainCancellationTokenParameters()
        {
            // Act
            _generator.Generate(WsdlDefinition, OutputNamespace, OutputDir);

            // Assert
            foreach (var portType in WsdlDefinition.PortTypes)
            {
                var filePath = Path.Combine(OutputDir, "Interfaces", $"I{portType.Name}.cs");
                var fileContent = File.ReadAllText(filePath);

                // Check for cancellation token parameter
                Assert.Contains("CancellationToken cancellationToken = default", fileContent);
            }
        }

        [Fact]
        public void Generate_InterfaceFilesContainRequiredUsings()
        {
            // Act
            _generator.Generate(WsdlDefinition, OutputNamespace, OutputDir);

            // Assert
            foreach (var portType in WsdlDefinition.PortTypes)
            {
                var filePath = Path.Combine(OutputDir, "Interfaces", $"I{portType.Name}.cs");
                var fileContent = File.ReadAllText(filePath);

                // Check for required usings
                Assert.Contains("using System;", fileContent);
                Assert.Contains("using System.Threading;", fileContent);
                Assert.Contains("using System.Threading.Tasks;", fileContent);
                Assert.Contains($"using {OutputNamespace}.Models.Requests;", fileContent);
                Assert.Contains($"using {OutputNamespace}.Models.Responses;", fileContent);
            }
        }

        [Fact]
        public void Generate_InterfaceFilesContainNoDuplicateMethods()
        {
            // Act
            _generator.Generate(WsdlDefinition, OutputNamespace, OutputDir);

            // Assert
            foreach (var portType in WsdlDefinition.PortTypes)
            {
                var filePath = Path.Combine(OutputDir, "Interfaces", $"I{portType.Name}.cs");
                var fileContent = File.ReadAllText(filePath);

                // Check for duplicate methods
                var uniqueOperations = portType.Operations
                    .GroupBy(o => o.Name)
                    .Select(g => g.First())
                    .ToList();

                foreach (var operation in uniqueOperations)
                {
                    var methodName = $"{operation.Name}Async";
                    var firstIndex = fileContent.IndexOf(methodName);
                    var lastIndex = fileContent.LastIndexOf(methodName);

                    // The method name should appear only once (or not at all if there are no operations)
                    Assert.True(firstIndex == lastIndex || firstIndex == -1, $"Method {methodName} appears multiple times in the interface");
                }
            }
        }

        public void Dispose()
        {
            CleanupOutputDirectory();
        }
    }
}
