using System.IO;
using Xunit;
using WsdlExMachina.Generator.Generators;

namespace WsdlExMachina.Generator.Tests.Generators
{
    public class DirectoryStructureGeneratorTests : GeneratorTestBase, IDisposable
    {
        private readonly DirectoryStructureGenerator _generator;

        public DirectoryStructureGeneratorTests()
        {
            _generator = new DirectoryStructureGenerator();
        }

        [Fact]
        public void Generate_CreatesExpectedDirectories()
        {
            // Act
            _generator.Generate(WsdlDefinition, OutputNamespace, OutputDir);

            // Assert
            Assert.True(Directory.Exists(OutputDir), "Main output directory should exist");
            Assert.True(Directory.Exists(Path.Combine(OutputDir, "Models")), "Models directory should exist");
            Assert.True(Directory.Exists(Path.Combine(OutputDir, "Models", "Common")), "Models/Common directory should exist");
            Assert.True(Directory.Exists(Path.Combine(OutputDir, "Models", "Requests")), "Models/Requests directory should exist");
            Assert.True(Directory.Exists(Path.Combine(OutputDir, "Models", "Responses")), "Models/Responses directory should exist");
            Assert.True(Directory.Exists(Path.Combine(OutputDir, "Interfaces")), "Interfaces directory should exist");
            Assert.True(Directory.Exists(Path.Combine(OutputDir, "Client")), "Client directory should exist");
            Assert.True(Directory.Exists(Path.Combine(OutputDir, "Extensions")), "Extensions directory should exist");
        }

        [Fact]
        public void Generate_WithExistingDirectories_DoesNotThrowException()
        {
            // Arrange - Create directories that would normally be created by the generator
            Directory.CreateDirectory(Path.Combine(OutputDir, "Models"));
            Directory.CreateDirectory(Path.Combine(OutputDir, "Models", "Common"));

            // Act & Assert - Should not throw an exception
            var exception = Record.Exception(() => _generator.Generate(WsdlDefinition, OutputNamespace, OutputDir));
            Assert.Null(exception);
        }

        public void Dispose()
        {
            CleanupOutputDirectory();
        }
    }
}
