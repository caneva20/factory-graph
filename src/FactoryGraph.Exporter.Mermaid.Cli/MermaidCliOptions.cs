using System.Globalization;

namespace FactoryGraph.Exporter.Mermaid.Cli;

internal sealed record MermaidCliOptions(
    string? InputPath,
    string? ScenarioPath,
    string? OutputPath,
    string? OutputDirectory,
    bool Initial,
    MermaidExportOptions ExportOptions) {
    public static (MermaidCliOptions? Options, string? Error, bool ShowHelp) Parse(IReadOnlyList<string> arguments) {
        if (arguments.Any(argument => argument is "--help" or "-h")) {
            return (null, null, true);
        }

        string? inputPath = null;
        string? scenarioPath = null;
        string? outputPath = null;
        string? outputDirectory = null;
        var initial = false;
        var direction = MermaidFlowchartDirection.LeftToRight;
        var numberFormat = "0.###";
        var includeCapacities = true;
        var includeStyles = true;

        for (var index = 0; index < arguments.Count; index++) {
            var argument = arguments[index];

            switch (argument) {
                case "--initial":
                    initial = true;

                    break;
                case "--no-capacities":
                    includeCapacities = false;

                    break;
                case "--no-styles":
                    includeStyles = false;

                    break;
                case "--output" or "-o":
                    if (!TryReadValue(arguments, ref index, out outputPath)) {
                        return (null, $"{argument} requires a path.", false);
                    }

                    break;
                case "--scenarios":
                    if (!TryReadValue(arguments, ref index, out scenarioPath)) {
                        return (null, "--scenarios requires a scenario file or directory.", false);
                    }

                    break;
                case "--output-dir":
                    if (!TryReadValue(arguments, ref index, out outputDirectory)) {
                        return (null, "--output-dir requires a directory.", false);
                    }

                    break;
                case "--direction":
                    if (!TryReadValue(arguments, ref index, out var directionValue)) {
                        return (null, "--direction requires lr or tb.", false);
                    }

                    var parsedDirection = directionValue switch {
                        "lr" => MermaidFlowchartDirection.LeftToRight,
                        "tb" => MermaidFlowchartDirection.TopToBottom,
                        _ => (MermaidFlowchartDirection?)null
                    };

                    if (parsedDirection is null) {
                        return (null, "--direction requires lr or tb.", false);
                    }

                    direction = parsedDirection.Value;

                    break;
                case "--number-format":
                    if (!TryReadValue(arguments, ref index, out numberFormat)) {
                        return (null, "--number-format requires a .NET numeric format string.", false);
                    }

                    try {
                        _ = 1d.ToString(numberFormat, CultureInfo.InvariantCulture);
                    } catch (FormatException) {
                        return (null, $"Invalid numeric format: {numberFormat}", false);
                    }

                    break;
                default:
                    if (argument.StartsWith('-') && argument != "-") {
                        return (null, $"Unknown option: {argument}", false);
                    }

                    if (inputPath is not null) {
                        return (null, "Specify one request JSON input.", false);
                    }

                    inputPath = argument;

                    break;
            }
        }

        if (scenarioPath is not null) {
            if (inputPath is not null) {
                return (null, "Do not combine a request input with --scenarios.", false);
            }

            if (initial) {
                return (null, "--initial does not apply to scenario results.", false);
            }

            if (outputPath is not null) {
                return (null, "Use --output-dir with --scenarios.", false);
            }

            if (outputDirectory is null) {
                return (null, "--scenarios requires --output-dir.", false);
            }
        } else {
            if (inputPath is null) {
                return (null, "Specify a request JSON input or --scenarios path.", false);
            }

            if (outputDirectory is not null) {
                return (null, "--output-dir requires --scenarios.", false);
            }
        }

        return (new MermaidCliOptions(
            inputPath,
            scenarioPath,
            outputPath,
            outputDirectory,
            initial,
            new MermaidExportOptions {
                Direction = direction,
                NumberFormat = numberFormat,
                IncludeCapacities = includeCapacities,
                IncludeStyles = includeStyles
            }), null, false);
    }

    private static bool TryReadValue(
        IReadOnlyList<string> arguments,
        ref int index,
        out string value) {
        index++;

        if (index >= arguments.Count) {
            value = string.Empty;

            return false;
        }

        value = arguments[index];

        return true;
    }
}
