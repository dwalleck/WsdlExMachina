using System;
using System.Collections.Generic;
using WsdlExMachina.Parser.Models;

namespace WsdlExMachina.CSharpGenerator;

/// <summary>
/// Provides a facade for generating C# code from WSDL definitions using Roslyn.
/// </summary>
public class RoslynGeneratorFacade
{
    private readonly RoslynCodeGenerator _codeGenerator;
    private readonly RoslynEnumGenerator _enumGenerator;
    private readonly RoslynComplexTypeGenerator _complexTypeGenerator;
    private readonly RoslynRequestModelGenerator _requestModelGenerator;
    private readonly RoslynClientGenerator _clientGenerator;

    /// <summary>
    /// Initializes a new instance of the <see cref="RoslynGeneratorFacade"/> class.
    /// </summary>
    public RoslynGeneratorFacade()
    {
        _codeGenerator = new RoslynCodeGenerator();
        _enumGenerator = new RoslynEnumGenerator(_codeGenerator);
        _complexTypeGenerator = new RoslynComplexTypeGenerator(_codeGenerator);
        _requestModelGenerator = new RoslynRequestModelGenerator(_codeGenerator, _complexTypeGenerator, _enumGenerator);
        _clientGenerator = new RoslynClientGenerator(_codeGenerator);
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
        ArgumentNullException.ThrowIfNull(wsdl, nameof(wsdl));
        ArgumentNullException.ThrowIfNullOrEmpty(namespaceName, nameof(namespaceName));

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
        ArgumentNullException.ThrowIfNull(wsdl, nameof(wsdl));
        ArgumentNullException.ThrowIfNullOrEmpty(namespaceName, nameof(namespaceName));

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
        ArgumentNullException.ThrowIfNull(wsdl, nameof(wsdl));
        ArgumentNullException.ThrowIfNullOrEmpty(namespaceName, nameof(namespaceName));

        try
        {
            var result = new Dictionary<string, string>();

            // Process enum types if they exist
            var simpleTypes = wsdl.Types?.SimpleTypes;
            if (simpleTypes == null)
            {
                return result;
            }

            // Filter for non-null enum types and generate code
            foreach (var simpleType in simpleTypes.Where(st => st != null && st.IsEnum))
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

            return result;
        }
        catch (Exception ex)
        {
            throw new CodeGenerationException("Failed to generate simple types.", ex);
        }
    }

    /// <summary>
    /// Generates C# code for SOAP clients in a WSDL definition.
    /// </summary>
    /// <param name="wsdl">The WSDL definition.</param>
    /// <param name="namespaceName">The namespace name.</param>
    /// <returns>A dictionary of file names to generated code.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="wsdl"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="namespaceName"/> is null or empty.</exception>
    /// <exception cref="CodeGenerationException">Thrown when an error occurs during code generation.</exception>
    public Dictionary<string, string> GenerateClients(WsdlDefinition wsdl, string namespaceName)
    {
        ArgumentNullException.ThrowIfNull(wsdl, nameof(wsdl));
        ArgumentNullException.ThrowIfNullOrEmpty(namespaceName, nameof(namespaceName));

        try
        {
            return _clientGenerator.GenerateClient(wsdl, namespaceName);
        }
        catch (Exception ex)
        {
            throw new CodeGenerationException("Failed to generate SOAP clients.", ex);
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
        ArgumentNullException.ThrowIfNull(wsdl, nameof(wsdl));
        ArgumentNullException.ThrowIfNullOrEmpty(namespaceName, nameof(namespaceName));

        try
        {
            var result = new Dictionary<string, string>();
            var errors = new List<Exception>();

            // Define generation tasks with their descriptions
            var generationTasks = new (string description, Func<Dictionary<string, string>> generator)[]
            {
                ("simple types", () => GenerateSimpleTypes(wsdl, namespaceName)),
                ("complex types", () => GenerateComplexTypes(wsdl, namespaceName)),
                ("request models", () => GenerateRequestModels(wsdl, namespaceName)),
                ("SOAP clients", () => GenerateClients(wsdl, namespaceName))
            };

            // Execute each generation task
            foreach (var (description, generator) in generationTasks)
            {
                try
                {
                    var generatedFiles = generator();
                    foreach (var (fileName, code) in generatedFiles)
                    {
                        result[fileName] = code;
                    }
                }
                catch (Exception ex)
                {
                    errors.Add(ex);
                    Console.Error.WriteLine($"Error generating {description}: {ex.Message}");
                }
            }

            // Handle errors
            if (errors.Count > 0)
            {
                if (result.Count > 0)
                {
                    // If we generated some code despite errors, log a warning
                    Console.Error.WriteLine($"Generated {result.Count} files with {errors.Count} errors.");
                }
                else
                {
                    // If we didn't generate any code, throw an exception
                    throw new CodeGenerationException("Failed to generate any code.", errors[0]);
                }
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
