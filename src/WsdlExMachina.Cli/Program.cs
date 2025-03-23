﻿﻿﻿using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using WsdlExMachina.CSharpGenerator;
using WsdlExMachina.Parser;

namespace WsdlExMachina.Cli;

public static class Program
{
    public static int Main(string[] args)
    {
        var app = new CommandApp<GenerateCommand>();

        app.Configure(config =>
        {
            config.SetApplicationName("wsdl-ex-machina");
            config.SetApplicationVersion("1.0.0");

            config.AddExample(new[] { "generate", "--wsdl", "path/to/service.wsdl", "--output", "Generated", "--namespace", "MyCompany.ServiceClient" });
        });

        return app.Run(args);
    }
}

public class GenerateCommandSettings : CommandSettings
{
    [CommandArgument(0, "[wsdl]")]
    [Description("Path to the WSDL file or URL")]
    public string? WsdlLocation { get; set; }

    [CommandOption("-w|--wsdl")]
    [Description("Path to the WSDL file or URL")]
    public string? WsdlOption { get; set; }

    [CommandOption("-o|--output")]
    [Description("Output directory for generated code")]
    [DefaultValue("Generated")]
    public string OutputDirectory { get; set; } = "Generated";

    [CommandOption("-n|--namespace")]
    [Description("Namespace for generated code")]
    public string? Namespace { get; set; }

    public string GetWsdlLocation()
    {
        return WsdlLocation ?? WsdlOption ?? string.Empty;
    }
}

public class GenerateCommand : Command<GenerateCommandSettings>
{
    public override int Execute(CommandContext context, GenerateCommandSettings settings)
    {
        var wsdlLocation = settings.GetWsdlLocation();

        if (string.IsNullOrWhiteSpace(wsdlLocation))
        {
            AnsiConsole.MarkupLine("[red]Error:[/] WSDL location is required");
            return 1;
        }

        var outputDirectory = settings.OutputDirectory;
        var namespaceName = settings.Namespace;

        // If namespace is not provided, generate one from the WSDL location
        if (string.IsNullOrWhiteSpace(namespaceName))
        {
            var fileName = Path.GetFileNameWithoutExtension(wsdlLocation);
            namespaceName = $"Generated.{fileName}";
            AnsiConsole.MarkupLine($"[yellow]Warning:[/] Namespace not provided, using [green]{namespaceName}[/]");
        }

        try
        {
            AnsiConsole.Status()
                .Start("Parsing WSDL...", ctx =>
                {
                    // Parse the WSDL
                    ctx.Status("Parsing WSDL...");
                    ctx.Spinner(Spinner.Known.Dots);

                    var parser = new WsdlParser();
                    var wsdl = parser.ParseFile(wsdlLocation);

                    // Generate code
                    ctx.Status("Generating C# code...");
                    var generator = new WsdlExMachina.CSharpGenerator.CSharpGenerator();
                    var result = generator.Generate(wsdl, namespaceName);

                    // Save the generated code
                    ctx.Status("Saving generated code...");
                    result.SaveToDirectory(outputDirectory);

                    // Generate summary
                    ctx.Status("Generating summary...");
                    var summary = generator.GenerateSummary(result);

                    // Display summary
                    AnsiConsole.MarkupLine($"[green]Success![/] Generated {result.Files.Count} files");
                    AnsiConsole.WriteLine();

                    var table = new Table();
                    table.AddColumn("File");

                    foreach (var fileName in result.Files.Keys.OrderBy(f => f))
                    {
                        table.AddRow(fileName);
                    }

                    AnsiConsole.Write(table);
                    AnsiConsole.WriteLine();

                    AnsiConsole.MarkupLine($"Files saved to: [blue]{Path.GetFullPath(outputDirectory)}[/]");
                });

            return 0;
        }
        catch (FileNotFoundException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
        catch (WsdlParserException ex)
        {
            AnsiConsole.MarkupLine($"[red]WSDL Parsing Error:[/] {ex.Message}");
            if (ex.InnerException != null)
            {
                AnsiConsole.MarkupLine($"[gray]Details: {ex.InnerException.Message}[/]");
            }
            return 1;
        }
        catch (CodeGenerationException ex)
        {
            AnsiConsole.MarkupLine($"[red]Code Generation Error:[/] {ex.Message}");
            if (ex.InnerException != null)
            {
                AnsiConsole.MarkupLine($"[gray]Details: {ex.InnerException.Message}[/]");
            }
            return 1;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Unexpected Error:[/] {ex.Message}");
            if (Debugger.IsAttached)
            {
                AnsiConsole.WriteException(ex);
            }
            return 1;
        }
    }
}
