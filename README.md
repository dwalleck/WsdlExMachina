# WsdlExMachina

WsdlExMachina is a tool for generating C# client code from WSDL files. It parses WSDL files and generates strongly-typed C# classes for SOAP services, including request and response models, enums, and client classes.

## Features

- Parse WSDL files and generate C# code
- Generate strongly-typed request and response models
- Generate enum classes for enumeration types
- Generate client classes with synchronous and asynchronous methods
- Support for complex types and nested structures
- Customizable namespace for generated code

## Getting Started

### Prerequisites

- .NET 9.0 SDK or later

### Installation

Clone the repository and build the solution:

```bash
git clone https://github.com/yourusername/WsdlExMachina.git
cd WsdlExMachina
dotnet build
```

### Usage

#### Using the Convenience Scripts

The easiest way to use WsdlExMachina is with the provided convenience scripts:

**On Linux/macOS:**
```bash
# Make the script executable
chmod +x generate.sh

# Generate code from a WSDL file
./generate.sh samples/ACH.wsdl Generated.ACH MyCompany.ACH
```

**On Windows:**
```batch
# Generate code from a WSDL file
generate.bat samples\ACH.wsdl Generated.ACH MyCompany.ACH
```

The scripts accept the following parameters:
1. WSDL file path (required)
2. Output directory (optional, defaults to "Generated")
3. Namespace (optional, defaults to "Generated.[WSDL filename]")

#### Using the CLI Directly

You can also use the WsdlExMachina.Cli.Generator tool directly:

```bash
dotnet run --project src/WsdlExMachina.Cli.Generator/WsdlExMachina.Cli.Generator.csproj -- --wsdl path/to/service.wsdl --output Generated --namespace MyCompany.ServiceClient
```

#### Installing as a Global Tool

You can install the tool globally:

```bash
dotnet pack src/WsdlExMachina.Cli.Generator/WsdlExMachina.Cli.Generator.csproj -o ./nupkg
dotnet tool install --global --add-source ./nupkg wsdl-generator
```

Then use it from anywhere:

```bash
wsdl-generator --wsdl path/to/service.wsdl --output Generated --namespace MyCompany.ServiceClient
```

### Command-line Options

- `--wsdl` or `-w`: Path to the WSDL file or URL (required)
- `--output` or `-o`: Output directory for generated code (default: "Generated")
- `--namespace` or `-n`: Namespace for generated code (if not provided, a namespace will be generated from the WSDL file name)

## Using the Generated Code

Once you've generated the code, you can use it in your project to call SOAP services. Here's a simple example:

```csharp
using System;
using System.Threading.Tasks;
using Generated.ACH;

namespace Examples
{
    public class ClientUsageExample
    {
        public static async Task Main(string[] args)
        {
            // Create a new client with the endpoint URL
            var client = new ACHTransactionClient("https://api.example.com/soap/ach");

            // Set up basic authentication if needed
            client.Username = "username";
            client.Password = "password";

            try
            {
                // Create a request
                var request = new PostSinglePaymentRequest
                {
                    // Set request properties
                    PostSinglePayment = new PostSinglePaymentType
                    {
                        // Fill in required fields
                        AccountNumber = "123456789",
                        RoutingNumber = "987654321",
                        Amount = 100.00m,
                        // Add other required fields
                    }
                };

                // Call the service asynchronously
                var response = await client.PostSinglePaymentAsync(request);

                // Process the response
                Console.WriteLine($"Transaction ID: {response.TransactionId}");
                Console.WriteLine($"Status: {response.Status}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Error: {ex.InnerException.Message}");
                }
            }
        }
    }
}
```

See the `examples` directory for more examples.

## Project Structure

- `src/WsdlExMachina.Parser`: WSDL parsing library
- `src/WsdlExMachina.CSharpGenerator`: C# code generation library
- `src/WsdlExMachina.Cli`: Command-line interface for the tool
- `src/WsdlExMachina.Cli.Generator`: Command-line tool for generating code
- `test/WsdlExMachina.Parser.Tests`: Tests for the parser
- `test/WsdlExMachina.CSharpGenerator.Tests`: Tests for the code generator
- `examples`: Example code showing how to use the generated code

## License

This project is licensed under the MIT License - see the LICENSE file for details.
