﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using Spectre.Console;
using System.CommandLine;
using System.CommandLine.Invocation;
using WsdlExMachina.Generator;
using WsdlExMachina.Parser;

namespace WsdlExMachina.Cli;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("WSDL Ex Machina - WSDL Parser and SOAP Client Generator");

        // Parse command
        var parseCommand = new Command("parse", "Parse a WSDL file and display its structure");
        var parseFileOption = new Option<FileInfo>(
            name: "--file",
            description: "The WSDL file to parse")
        {
            IsRequired = true
        };
        parseCommand.AddOption(parseFileOption);
        parseCommand.SetHandler(ParseWsdl, parseFileOption);
        rootCommand.AddCommand(parseCommand);

        // Generate command
        var generateCommand = new Command("generate", "Generate a C# SOAP client from a WSDL file");
        var generateFileOption = new Option<FileInfo>(
            name: "--file",
            description: "The WSDL file to parse")
        {
            IsRequired = true
        };
        var namespaceOption = new Option<string>(
            name: "--namespace",
            description: "The namespace to use for the generated code")
        {
            IsRequired = true
        };
        var outputOption = new Option<FileInfo?>(
            name: "--output",
            description: "The output file path (defaults to [ServiceName]Client.cs)")
        {
            IsRequired = false
        };
        var multiFileOption = new Option<bool>(
            name: "--multi-file",
            description: "Generate code across multiple files instead of a single file")
        {
            IsRequired = false
        };
        var outputDirOption = new Option<DirectoryInfo?>(
            name: "--output-dir",
            description: "The output directory for multi-file generation (required when --multi-file is specified)")
        {
            IsRequired = false
        };
        var httpClientOption = new Option<bool>(
            name: "--http-client",
            description: "Use HttpClient instead of System.ServiceModel")
        {
            IsRequired = false
        };
        generateCommand.AddOption(generateFileOption);
        generateCommand.AddOption(namespaceOption);
        generateCommand.AddOption(outputOption);
        generateCommand.AddOption(multiFileOption);
        generateCommand.AddOption(outputDirOption);
        generateCommand.AddOption(httpClientOption);

        // Add validation rule for --multi-file and --output-dir
        generateCommand.AddValidator(result =>
        {
            if (result.GetValueForOption(multiFileOption) && result.GetValueForOption(outputDirOption) == null)
            {
                result.ErrorMessage = "--output-dir is required when --multi-file is specified";
                return;
            }
        });

        generateCommand.SetHandler((file, ns, output, multiFile, outputDir, httpClient) =>
            GenerateClient(file, ns, output, multiFile, outputDir, httpClient),
            generateFileOption, namespaceOption, outputOption, multiFileOption, outputDirOption, httpClientOption);
        rootCommand.AddCommand(generateCommand);

        return await rootCommand.InvokeAsync(args);
    }

    private static void ParseWsdl(FileInfo file)
    {
        try
        {
            AnsiConsole.MarkupLine($"[bold green]Parsing WSDL file:[/] {file.FullName}");

            var parser = new WsdlParser();
            var wsdl = parser.ParseFile(file.FullName);

            AnsiConsole.MarkupLine($"[bold]Target Namespace:[/] {wsdl.TargetNamespace}");

            // Display services
            var serviceTable = new Table()
                .Title("[yellow]Services[/]")
                .AddColumn("Name")
                .AddColumn("Ports");

            foreach (var service in wsdl.Services)
            {
                var ports = string.Join(", ", service.Ports.Select(p => p.Name));
                serviceTable.AddRow(service.Name, ports);
            }

            AnsiConsole.Write(serviceTable);

            // Display port types
            var portTypeTable = new Table()
                .Title("[yellow]Port Types[/]")
                .AddColumn("Name")
                .AddColumn("Operations");

            foreach (var portType in wsdl.PortTypes)
            {
                var operations = string.Join(", ", portType.Operations.Select(o => o.Name));
                portTypeTable.AddRow(portType.Name, operations);
            }

            AnsiConsole.Write(portTypeTable);

            // Display messages
            var messageTable = new Table()
                .Title("[yellow]Messages[/]")
                .AddColumn("Name")
                .AddColumn("Parts");

            foreach (var message in wsdl.Messages)
            {
                var parts = string.Join(", ", message.Parts.Select(p => p.Name));
                messageTable.AddRow(message.Name, parts);
            }

            AnsiConsole.Write(messageTable);

            // Display complex types
            var complexTypeTable = new Table()
                .Title("[yellow]Complex Types[/]")
                .AddColumn("Name")
                .AddColumn("Elements");

            foreach (var complexType in wsdl.Types.ComplexTypes)
            {
                var elements = string.Join(", ", complexType.Elements.Select(e => e.Name));
                complexTypeTable.AddRow(complexType.Name, elements);
            }

            AnsiConsole.Write(complexTypeTable);

            // Display simple types
            var simpleTypeTable = new Table()
                .Title("[yellow]Simple Types[/]")
                .AddColumn("Name")
                .AddColumn("Base Type")
                .AddColumn("Is Enum");

            foreach (var simpleType in wsdl.Types.SimpleTypes)
            {
                simpleTypeTable.AddRow(simpleType.Name, simpleType.BaseType, simpleType.IsEnum.ToString());
            }

            AnsiConsole.Write(simpleTypeTable);

            AnsiConsole.MarkupLine("[bold green]WSDL parsing completed successfully.[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[bold red]Error parsing WSDL:[/] {ex.Message}");
            AnsiConsole.WriteException(ex);
        }
    }

    private static int GenerateClient(FileInfo file, string outputNamespace, FileInfo? output, bool multiFile, DirectoryInfo? outputDir, bool useHttpClient)
    {
        try
        {
            AnsiConsole.MarkupLine($"[bold green]Generating SOAP client from WSDL file:[/] {file.FullName}");
            AnsiConsole.MarkupLine($"[bold]Namespace:[/] {outputNamespace}");

            if (useHttpClient)
            {
                AnsiConsole.MarkupLine($"[bold]Using:[/] HttpClient with Polly resilience");
            }
            else
            {
                AnsiConsole.MarkupLine($"[bold]Using:[/] System.ServiceModel");
            }

            if (multiFile)
            {
                if (outputDir == null)
                {
                    AnsiConsole.MarkupLine($"[bold red]Error:[/] --output-dir is required when --multi-file is specified");
                    return 1;
                }

                AnsiConsole.MarkupLine($"[bold]Output directory:[/] {outputDir.FullName}");

                // Parse the WSDL
                var parser = new WsdlParser();
                var wsdl = parser.ParseFile(file.FullName);

                // Generate code across multiple files
                var generator = new SoapClientGenerator();
                var multiFileGenerator = new MultiFileGenerator(generator);
                multiFileGenerator.Generate(wsdl, outputNamespace, outputDir.FullName);

                AnsiConsole.MarkupLine($"[bold green]SOAP client generated successfully.[/]");
                AnsiConsole.MarkupLine($"[bold]Output directory:[/] {outputDir.FullName}");

                // List the generated files
                AnsiConsole.MarkupLine("[bold]Generated files:[/]");
                var files = Directory.GetFiles(outputDir.FullName, "*.cs", SearchOption.AllDirectories);
                var table = new Table()
                    .AddColumn("File")
                    .AddColumn("Path");

                foreach (var f in files)
                {
                    var relativePath = Path.GetRelativePath(outputDir.FullName, f);
                    table.AddRow(Path.GetFileName(f), relativePath);
                }

                AnsiConsole.Write(table);
            }
            else
            {
                var generator = new SoapClientGenerator();
                var code = generator.GenerateFromFile(file.FullName, outputNamespace);

                // Determine output file path
                string outputPath;
                if (output != null)
                {
                    outputPath = output.FullName;
                }
                else
                {
                    // Parse the WSDL to get the service name
                    var parser = new WsdlParser();
                    var wsdl = parser.ParseFile(file.FullName);
                    var serviceName = wsdl.Services.FirstOrDefault()?.Name ?? "Service";
                    outputPath = Path.Combine(Directory.GetCurrentDirectory(), $"{serviceName}Client.cs");
                }

                // Write the generated code to the output file
                File.WriteAllText(outputPath, code);

                AnsiConsole.MarkupLine($"[bold green]SOAP client generated successfully.[/]");
                AnsiConsole.MarkupLine($"[bold]Output file:[/] {outputPath}");

                // Display a preview of the generated code
                AnsiConsole.MarkupLine("[bold]Preview of generated code:[/]");
                var preview = code.Length > 1000 ? code.Substring(0, 1000) + "..." : code;

                // Create a panel with plain text (not markup)
                var panel = new Panel(new Text(preview))
                    .Header("Code Preview")
                    .Expand()
                    .BorderColor(Color.Yellow);

                AnsiConsole.Write(panel);
            }

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[bold red]Error generating SOAP client:[/] {ex.Message}");
            AnsiConsole.WriteException(ex);
            return 1;
        }
    }
}
