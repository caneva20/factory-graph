using FactoryGraph.Contracts;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FactoryGraph.Exporter.Mermaid.Cli;

internal static class MermaidCli {
    public const string Usage = """
                                Usage: factory-graph-mermaid <request.json|-> [options]
                                       factory-graph-mermaid --scenarios <path> --output-dir <path> [options]

                                Options:
                                  --initial                 Export the validated request without solved values.
                                  -o, --output <path>       Write Mermaid output to a file instead of stdout.
                                  --scenarios <path>        Export every scenario file below a path.
                                  --output-dir <path>       Write scenario diagrams below a directory.
                                  --direction <lr|tb>       Set the flowchart direction. Default: lr.
                                  --number-format <format>  Set the invariant .NET numeric format. Default: 0.###.
                                  --no-capacities           Omit configured capacities.
                                  --no-styles               Omit built-in Mermaid styles.
                                  -h, --help                Show this help.
                                """;

    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    public static async Task<int> RunAsync(
        IReadOnlyList<string> arguments,
        TextReader input,
        TextWriter output,
        TextWriter error) {
        var parsed = MermaidCliOptions.Parse(arguments);

        if (parsed.ShowHelp) {
            await output.WriteLineAsync(Usage);

            return 0;
        }

        if (parsed.Error is not null) {
            await error.WriteLineAsync($"error: {parsed.Error}");
            await error.WriteLineAsync();
            await error.WriteLineAsync(Usage);

            return 1;
        }

        if (parsed.Options is not { } options) {
            await error.WriteLineAsync("error: Unable to parse command arguments.");

            return 1;
        }

        try {
            var exporter = new MermaidExporter(options.ExportOptions);

            if (options is { ScenarioPath: { } scenarioPath, OutputDirectory: { } outputDirectory }) {
                await ScenarioDiagramGenerator.GenerateAsync(
                    scenarioPath,
                    outputDirectory,
                    exporter,
                    JsonOptions);

                return 0;
            }

            if (options.InputPath is not { } inputPath) {
                await error.WriteLineAsync("error: Request input is missing.");

                return 1;
            }

            var request = await ReadRequestAsync(inputPath, input);
            var result = FactoryGraphProcessorFactory.Create().Solve(request);

            if (result.Status == SolveStatus.InvalidRequest) {
                await WriteSolveFailureAsync(result, error);

                return 1;
            }

            string diagram;

            if (options.Initial) {
                diagram = exporter.Export(request);
            } else {
                if (result.Graph is null) {
                    await WriteSolveFailureAsync(result, error);

                    return 1;
                }

                diagram = exporter.Export(result);
            }

            if (options.OutputPath is null) {
                await output.WriteAsync(diagram);
            } else {
                await File.WriteAllTextAsync(options.OutputPath, diagram);
            }

            return 0;
        } catch (JsonException exception) {
            await error.WriteLineAsync($"error: Invalid request JSON: {exception.Message}");

            return 1;
        } catch (IOException exception) {
            await error.WriteLineAsync($"error: {exception.Message}");

            return 1;
        } catch (UnauthorizedAccessException exception) {
            await error.WriteLineAsync($"error: {exception.Message}");

            return 1;
        } catch (InvalidDataException exception) {
            await error.WriteLineAsync($"error: {exception.Message}");

            return 1;
        }
    }

    private static async Task<SolveRequest> ReadRequestAsync(string path, TextReader input) {
        var json = path == "-"
            ? await input.ReadToEndAsync()
            : await File.ReadAllTextAsync(path);

        return JsonSerializer.Deserialize<SolveRequest>(json, JsonOptions)
               ?? throw new JsonException("The input is empty.");
    }

    private static async Task WriteSolveFailureAsync(SolveResult result, TextWriter error) {
        await error.WriteLineAsync($"error: Solve ended with status {result.Status}.");

        foreach (var diagnostic in result.Diagnostics) {
            await error.WriteLineAsync($"{diagnostic.Code}: {diagnostic.Message}");
        }
    }

    private static JsonSerializerOptions CreateJsonOptions() {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web) {
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
            RespectRequiredConstructorParameters = true,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
        };

        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

        return options;
    }
}
