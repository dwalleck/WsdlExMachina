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
        public Dictionary<string, string> GenerateRequestModels(WsdlDefinition wsdl, string namespaceName)
        {
            if (wsdl == null)
                throw new ArgumentNullException(nameof(wsdl));

            if (string.IsNullOrEmpty(namespaceName))
                throw new ArgumentException("Namespace name cannot be null or empty.", nameof(namespaceName));

            return _requestModelGenerator.GenerateRequestModels(wsdl, namespaceName);
        }

        /// <summary>
        /// Generates C# code for all complex types in a WSDL definition.
        /// </summary>
        /// <param name="wsdl">The WSDL definition.</param>
        /// <param name="namespaceName">The namespace name.</param>
        /// <returns>A dictionary of file names to generated code.</returns>
        public Dictionary<string, string> GenerateComplexTypes(WsdlDefinition wsdl, string namespaceName)
        {
            if (wsdl == null)
                throw new ArgumentNullException(nameof(wsdl));

            if (string.IsNullOrEmpty(namespaceName))
                throw new ArgumentException("Namespace name cannot be null or empty.", nameof(namespaceName));

            return _complexTypeGenerator.GenerateComplexTypes(wsdl, namespaceName);
        }

        /// <summary>
        /// Generates C# code for all simple types in a WSDL definition.
        /// </summary>
        /// <param name="wsdl">The WSDL definition.</param>
        /// <param name="namespaceName">The namespace name.</param>
        /// <returns>A dictionary of file names to generated code.</returns>
        public Dictionary<string, string> GenerateSimpleTypes(WsdlDefinition wsdl, string namespaceName)
        {
            if (wsdl == null)
                throw new ArgumentNullException(nameof(wsdl));

            if (string.IsNullOrEmpty(namespaceName))
                throw new ArgumentException("Namespace name cannot be null or empty.", nameof(namespaceName));

            var result = new Dictionary<string, string>();

            foreach (var simpleType in wsdl.Types.SimpleTypes)
            {
                // Only generate enums for simple types that are enumerations
                if (simpleType.IsEnum)
                {
                    var enumCode = _enumGenerator.GenerateEnumCode(simpleType, namespaceName);
                    result[$"{simpleType.Name}.cs"] = enumCode;
                }
            }

            return result;
        }

        /// <summary>
        /// Generates all C# code for a WSDL definition.
        /// </summary>
        /// <param name="wsdl">The WSDL definition.</param>
        /// <param name="namespaceName">The namespace name.</param>
        /// <returns>A dictionary of file names to generated code.</returns>
        public Dictionary<string, string> GenerateAll(WsdlDefinition wsdl, string namespaceName)
        {
            if (wsdl == null)
                throw new ArgumentNullException(nameof(wsdl));

            if (string.IsNullOrEmpty(namespaceName))
                throw new ArgumentException("Namespace name cannot be null or empty.", nameof(namespaceName));

            var result = new Dictionary<string, string>();

            // Generate simple types (enums)
            var simpleTypes = GenerateSimpleTypes(wsdl, namespaceName);
            foreach (var (fileName, code) in simpleTypes)
            {
                result[fileName] = code;
            }

            // Generate complex types
            var complexTypes = GenerateComplexTypes(wsdl, namespaceName);
            foreach (var (fileName, code) in complexTypes)
            {
                result[fileName] = code;
            }

            // Generate request models
            var requestModels = GenerateRequestModels(wsdl, namespaceName);
            foreach (var (fileName, code) in requestModels)
            {
                result[fileName] = code;
            }

            return result;
        }
    }
}
