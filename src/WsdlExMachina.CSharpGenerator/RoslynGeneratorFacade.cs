using System;
using System.Collections.Generic;
using WsdlExMachina.Parser.Models;

namespace WsdlExMachina.CSharpGenerator
{
    /// <summary>
    /// Provides a facade for generating C# code from WSDL definitions using Roslyn.
    /// </summary>
    public class RoslynGeneratorFacade
    {
        private readonly RoslynCodeGenerator _codeGenerator;
        private readonly RoslynEnumGenerator _enumGenerator;
        private readonly RoslynComplexTypeGenerator _complexTypeGenerator;
        private readonly RoslynRequestModelGenerator _requestModelGenerator;

        /// <summary>
        /// Initializes a new instance of the <see cref="RoslynGeneratorFacade"/> class.
        /// </summary>
        public RoslynGeneratorFacade()
        {
            _codeGenerator = new RoslynCodeGenerator();
            _enumGenerator = new RoslynEnumGenerator(_codeGenerator);
            _complexTypeGenerator = new RoslynComplexTypeGenerator(_codeGenerator);
            _requestModelGenerator = new RoslynRequestModelGenerator(_codeGenerator, _complexTypeGenerator, _enumGenerator);
        }

        /// <summary>
        /// Generates C# code for all request models in a WSDL definition.
        /// </summary>
        /// <param name="wsdl">The WSDL definition.</param>
        /// <param name="namespaceName">The namespace name.</param>
        /// <returns>A dictionary of file names to generated code.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="wsdl"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="namespaceName"/> is null or empty.</exception>
        public Dictionary<string, string> GenerateRequestModels(WsdlDefinition wsdl, string namespaceName)
        {
            ArgumentNullException.ThrowIfNull(wsdl);

            if (string.IsNullOrEmpty(namespaceName))
                throw new ArgumentException("Namespace name cannot be null or empty.", nameof(namespaceName));

            try
            {
                return _requestModelGenerator.GenerateRequestModels(wsdl, namespaceName);
            }
            catch (Exception ex)
            {
                throw new CodeGenerationException("Failed to generate request models.", ex);
            }
        }

        /// <summary>
        /// Generates C# code for all complex types in a WSDL definition.
        /// </summary>
        /// <param name="wsdl">The WSDL definition.</param>
        /// <param name="namespaceName">The namespace name.</param>
        /// <returns>A dictionary of file names to generated code.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="wsdl"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="namespaceName"/> is null or empty.</exception>
        /// <exception cref="CodeGenerationException">Thrown when an error occurs during code generation.</exception>
        public Dictionary<string, string> GenerateComplexTypes(WsdlDefinition wsdl, string namespaceName)
        {
            ArgumentNullException.ThrowIfNull(wsdl);

            if (string.IsNullOrEmpty(namespaceName))
                throw new ArgumentException("Namespace name cannot be null or empty.", nameof(namespaceName));

            try
            {
                return _complexTypeGenerator.GenerateComplexTypes(wsdl, namespaceName);
            }
            catch (Exception ex)
            {
                throw new CodeGenerationException("Failed to generate complex types.", ex);
            }
        }

        /// <summary>
        /// Generates C# code for all simple types in a WSDL definition.
        /// </summary>
        /// <param name="wsdl">The WSDL definition.</param>
        /// <param name="namespaceName">The namespace name.</param>
        /// <returns>A dictionary of file names to generated code.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="wsdl"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="namespaceName"/> is null or empty.</exception>
        /// <exception cref="CodeGenerationException">Thrown when an error occurs during code generation.</exception>
        public Dictionary<string, string> GenerateSimpleTypes(WsdlDefinition wsdl, string namespaceName)
        {
            ArgumentNullException.ThrowIfNull(wsdl);

            if (string.IsNullOrEmpty(namespaceName))
                throw new ArgumentException("Namespace name cannot be null or empty.", nameof(namespaceName));

            try
            {
                var result = new Dictionary<string, string>();

                // Check if Types is null
                if (wsdl.Types == null)
                {
                    return result;
                }

                // Check if SimpleTypes is null
                if (wsdl.Types.SimpleTypes == null)
                {
                    return result;
                }

                foreach (var simpleType in wsdl.Types.SimpleTypes)
                {
                    // Skip null simple types
                    if (simpleType == null)
                    {
                        continue;
                    }

                    // Only generate enums for simple types that are enumerations
                    if (simpleType.IsEnum)
                    {
                        try
                        {
                            var enumCode = _enumGenerator.GenerateEnumCode(simpleType, namespaceName);
                            result[$"{simpleType.Name}.cs"] = enumCode;
                        }
                        catch (Exception ex)
                        {
                            // Log the error but continue processing other simple types
                            Console.Error.WriteLine($"Error generating enum for {simpleType.Name}: {ex.Message}");
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new CodeGenerationException("Failed to generate simple types.", ex);
            }
        }

        /// <summary>
        /// Generates all C# code for a WSDL definition.
        /// </summary>
        /// <param name="wsdl">The WSDL definition.</param>
        /// <param name="namespaceName">The namespace name.</param>
        /// <returns>A dictionary of file names to generated code.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="wsdl"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="namespaceName"/> is null or empty.</exception>
        /// <exception cref="CodeGenerationException">Thrown when an error occurs during code generation.</exception>
        public Dictionary<string, string> GenerateAll(WsdlDefinition wsdl, string namespaceName)
        {
            ArgumentNullException.ThrowIfNull(wsdl);

            if (string.IsNullOrEmpty(namespaceName))
                throw new ArgumentException("Namespace name cannot be null or empty.", nameof(namespaceName));

            try
            {
                var result = new Dictionary<string, string>();
                var errors = new List<Exception>();

                // Generate simple types (enums)
                try
                {
                    var simpleTypes = GenerateSimpleTypes(wsdl, namespaceName);
                    foreach (var (fileName, code) in simpleTypes)
                    {
                        result[fileName] = code;
                    }
                }
                catch (Exception ex)
                {
                    errors.Add(ex);
                    Console.Error.WriteLine($"Error generating simple types: {ex.Message}");
                }

                // Generate complex types
                try
                {
                    var complexTypes = GenerateComplexTypes(wsdl, namespaceName);
                    foreach (var (fileName, code) in complexTypes)
                    {
                        result[fileName] = code;
                    }
                }
                catch (Exception ex)
                {
                    errors.Add(ex);
                    Console.Error.WriteLine($"Error generating complex types: {ex.Message}");
                }

                // Generate request models
                try
                {
                    var requestModels = GenerateRequestModels(wsdl, namespaceName);
                    foreach (var (fileName, code) in requestModels)
                    {
                        result[fileName] = code;
                    }
                }
                catch (Exception ex)
                {
                    errors.Add(ex);
                    Console.Error.WriteLine($"Error generating request models: {ex.Message}");
                }

                // If there were errors but we still generated some code, log a warning
                if (errors.Count > 0 && result.Count > 0)
                {
                    Console.Error.WriteLine($"Generated {result.Count} files with {errors.Count} errors.");
                }
                // If there were errors and we didn't generate any code, throw an exception
                else if (errors.Count > 0 && result.Count == 0)
                {
                    throw new CodeGenerationException("Failed to generate any code.", errors[0]);
                }

                return result;
            }
            catch (CodeGenerationException)
            {
                // Re-throw CodeGenerationException as is
                throw;
            }
            catch (Exception ex)
            {
                throw new CodeGenerationException("Failed to generate code.", ex);
            }
        }
    }
}
