using System.Text;
using Microsoft.CodeAnalysis;
using WsdlExMachina.Parser.Models;

namespace WsdlExMachina.CSharpGenerator;

/// <summary>
/// Generates C# SOAP client code using Roslyn.
/// </summary>
public class RoslynClientGenerator
{
    private readonly RoslynCodeGenerator _codeGenerator;
    private readonly TypeMapper _typeMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="RoslynClientGenerator"/> class.
    /// </summary>
    /// <param name="codeGenerator">The code generator.</param>
    public RoslynClientGenerator(RoslynCodeGenerator codeGenerator)
    {
        _codeGenerator = codeGenerator ?? throw new ArgumentNullException(nameof(codeGenerator));
        _typeMapper = new TypeMapper();
    }

    /// <summary>
    /// Generates a SOAP client for the specified WSDL definition.
    /// </summary>
    /// <param name="wsdl">The WSDL definition.</param>
    /// <param name="namespaceName">The namespace name.</param>
    /// <returns>A dictionary of file names to generated code.</returns>
    public Dictionary<string, string> GenerateClient(WsdlDefinition wsdl, string namespaceName)
    {
        ArgumentNullException.ThrowIfNull(wsdl, nameof(wsdl));

        if (string.IsNullOrWhiteSpace(namespaceName))
        {
            throw new ArgumentNullException(nameof(namespaceName));
        }

        var result = new Dictionary<string, string>();

        // Generate a client for each service
        foreach (var service in wsdl.Services ?? Enumerable.Empty<WsdlService>())
        {
            foreach (var port in service.Ports ?? Enumerable.Empty<WsdlPort>())
            {
                var binding = wsdl.Bindings?.FirstOrDefault(b => b.Name == port.Binding);
                if (binding == null)
                {
                    continue;
                }

                var portType = wsdl.PortTypes?.FirstOrDefault(pt => pt.Name == binding.Type);
                if (portType == null)
                {
                    continue;
                }

                var clientCode = GenerateClientClass(wsdl, service, binding, portType, namespaceName);
                var fileName = $"{service.Name}Client.cs";
                result[fileName] = clientCode;
            }
        }

        return result;
    }

    private string GenerateClientClass(
        WsdlDefinition wsdl,
        WsdlService service,
        WsdlBinding binding,
        WsdlPortType portType,
        string namespaceName)
    {
        var sb = new StringBuilder();

        // Add using directives
        sb.AppendLine("using System;");
        sb.AppendLine("using System.IO;");
        sb.AppendLine("using System.Net.Http;");
        sb.AppendLine("using System.Text;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine("using System.Xml;");
        sb.AppendLine("using System.Xml.Serialization;");
        sb.AppendLine();

        // Add namespace
        sb.AppendLine($"namespace {namespaceName}");
        sb.AppendLine("{");

        // Add class declaration
        var className = $"{service.Name}Client";
        sb.AppendLine($"    /// <summary>");
        sb.AppendLine($"    /// Client for the {service.Name} SOAP service.");
        sb.AppendLine($"    /// </summary>");
        sb.AppendLine($"    public class {className} : SoapClientBase");
        sb.AppendLine("    {");

        // Add constructor
        sb.AppendLine($"        /// <summary>");
        sb.AppendLine($"        /// Initializes a new instance of the <see cref=\"{className}\"/> class.");
        sb.AppendLine($"        /// </summary>");
        sb.AppendLine($"        /// <param name=\"endpointUrl\">The endpoint URL of the SOAP service.</param>");
        sb.AppendLine($"        public {className}(string endpointUrl)");
        sb.AppendLine("        {");
        sb.AppendLine("            Initialize(endpointUrl);");
        sb.AppendLine("        }");
        sb.AppendLine();

        // Add methods for each operation
        //foreach (var operation in portType.Operations ?? Enumerable.Empty<WsdlOperation>())
        foreach (var operation in binding.Operations ?? Enumerable.Empty<WsdlBindingOperation>())
        {
            var portOperation = portType.Operations?.FirstOrDefault(po => po.Name == operation.Name);
            // if (bindingOperation == null)
            // {
            //     continue;
            // }

            // Get the input and output messages
            // var inputMessage = wsdl.Messages?.FirstOrDefault(m => m.Name == operation.Input?.Message);
            // var outputMessage = wsdl.Messages?.FirstOrDefault(m => m.Name == operation.Output?.Message);

            // if (inputMessage == null || outputMessage == null)
            // {
            //     continue;
            // }

            // Generate async method for this operation
            GenerateAsyncOperationMethod(sb, portOperation, operation);
        }

        // Close class and namespace
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static void GenerateAsyncOperationMethod(
        StringBuilder sb,
        WsdlOperation operation,
        WsdlBindingOperation bindingOperation)
    {
        // Always use the operation name for consistency
        var operationName = string.IsNullOrEmpty(bindingOperation.Input?.Name)
            ? operation.Name
            : bindingOperation.Input.Name;
        var methodName = $"{operationName}Async";
        var requestTypeName = $"{operationName}Request";
        var responseTypeName = "ACHTransResponse";
        var soapAction = bindingOperation.SoapAction;

        // Add method declaration
        sb.AppendLine($"        /// <summary>");
        sb.AppendLine($"        /// {operation.Documentation}");
        sb.AppendLine($"        /// </summary>");
        sb.AppendLine($"        /// <param name=\"request\">The request object.</param>");
        sb.AppendLine($"        /// <returns>A task that represents the asynchronous operation. The task result contains the {responseTypeName} response.</returns>");
        sb.AppendLine($"        public async Task<{responseTypeName}> {methodName}({requestTypeName} request)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (request == null)");
        sb.AppendLine("                throw new ArgumentNullException(nameof(request));");
        sb.AppendLine();
        sb.AppendLine($"            var soapEnvelope = CreateSoapEnvelope(request, \"{soapAction}\");");
        sb.AppendLine($"            var responseContent = await SendSoapRequestAsync(soapEnvelope, \"{soapAction}\");");
        sb.AppendLine($"            return DeserializeResponse<{responseTypeName}>(responseContent);");
        sb.AppendLine("        }");
        sb.AppendLine();
    }
}
