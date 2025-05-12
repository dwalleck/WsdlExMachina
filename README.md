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
./generate.sh MySoapApi.wsdl Generated.MySoapApi MyCompany.MyNamespae
```

**On Windows:**
```batch
# Generate code from a WSDL file
generate.bat Service.wsdl Generated.Service MyCompany.ServiceClient
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

// Add back in

See the `examples` directory for more examples.

## Project Structure

- `src/WsdlExMachina.Parser`: WSDL parsing library
- `src/WsdlExMachina.CSharpGenerator`: C# code generation library
- `src/WsdlExMachina.Cli`: Command-line interface for the tool
- `src/WsdlExMachina.Cli.Generator`: Command-line tool for generating code
- `test/WsdlExMachina.Parser.Tests`: Tests for the parser
- `test/WsdlExMachina.CSharpGenerator.Tests`: Tests for the code generator

## License

This project is licensed under the MIT License - see the LICENSE file for details.
