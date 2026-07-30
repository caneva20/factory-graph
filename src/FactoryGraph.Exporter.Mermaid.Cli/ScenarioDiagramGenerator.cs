using System.Text.Json;
using System.Text.Json.Nodes;
using FactoryGraph.Contracts;

namespace FactoryGraph.Exporter.Mermaid.Cli;

internal static class ScenarioDiagramGenerator {
    private const string ScenarioFileName = "scenario.json";

    public static async Task GenerateAsync(
        string inputPath,
        string outputDirectory,
        MermaidExporter exporter,
        JsonSerializerOptions jsonOptions) {
        var (root, paths) = Discover(inputPath);
        var outputs = new List<(string RelativePath, string Diagram)>();
        var outputPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in paths) {
            var document = await ReadDocumentAsync(path, jsonOptions);
            var relativePath = OutputPath(root, path);

            if (!outputPaths.Add(relativePath)) {
                throw new InvalidDataException($"Multiple scenarios produce output '{relativePath}'.");
            }

            var sections = document.Cases
                .Select(@case => LoadSection(document, @case, jsonOptions))
                .ToArray();

            outputs.Add((relativePath, exporter.Export(sections)));
        }

        foreach (var (relativePath, diagram) in outputs) {
            var path = Path.GetFullPath(Path.Combine(outputDirectory, relativePath));

            var directory = Path.GetDirectoryName(path)
                            ?? throw new InvalidDataException($"Output path '{path}' has no directory.");

            Directory.CreateDirectory(directory);
            await File.WriteAllTextAsync(path, diagram);
        }
    }

    private static (string Root, IReadOnlyList<string> Paths) Discover(string inputPath) {
        var fullPath = Path.GetFullPath(inputPath);

        if (File.Exists(fullPath)) {
            if (!string.Equals(Path.GetFileName(fullPath), ScenarioFileName, StringComparison.Ordinal)) {
                throw new InvalidDataException($"Scenario file must be named '{ScenarioFileName}'.");
            }

            var directory = Path.GetDirectoryName(fullPath)
                            ?? throw new InvalidDataException($"Scenario path '{inputPath}' has no directory.");

            return (directory, [fullPath]);
        }

        if (!Directory.Exists(fullPath)) {
            throw new InvalidDataException($"Scenario path '{inputPath}' does not exist.");
        }

        var paths = Directory
            .EnumerateFiles(fullPath, ScenarioFileName, SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal)
            .ToArray();

        if (paths.Length == 0) {
            throw new InvalidDataException($"Scenario directory '{inputPath}' contains no {ScenarioFileName} files.");
        }

        return (fullPath, paths);
    }

    private static async Task<ScenarioDocument> ReadDocumentAsync(string path, JsonSerializerOptions jsonOptions) {
        try {
            var json = await File.ReadAllTextAsync(path);

            return JsonSerializer.Deserialize<ScenarioDocument>(json, jsonOptions)
                   ?? throw new InvalidDataException($"Scenario fixture '{path}' is empty.");
        } catch (JsonException exception) {
            throw new InvalidDataException($"Invalid scenario JSON in '{path}': {exception.Message}", exception);
        }
    }

    private static MermaidResultSection LoadSection(
        ScenarioDocument document,
        ScenarioCaseDocument @case,
        JsonSerializerOptions jsonOptions) {
        var usedParameters = new HashSet<string>(StringComparer.Ordinal);

        var requestNode = Expand(document.Request, @case.Parameters, usedParameters)
                          ?? throw new InvalidDataException($"Scenario '{document.Name}/{@case.Name}' has no solve request.");

        var expectedNode = Expand(document.Expected, @case.Parameters, usedParameters)
                           ?? throw new InvalidDataException($"Scenario '{document.Name}/{@case.Name}' has no expected result.");

        var unusedParameters = @case.Parameters
            .Select(parameter => parameter.Key)
            .Where(parameter => !usedParameters.Contains(parameter))
            .Order(StringComparer.Ordinal)
            .ToArray();

        if (unusedParameters.Length > 0) {
            throw new InvalidDataException(
                $"Scenario '{document.Name}/{@case.Name}' has unused parameters: {string.Join(", ", unusedParameters)}.");
        }

        try {
            _ = requestNode.Deserialize<SolveRequest>(jsonOptions)
                ?? throw new InvalidDataException($"Scenario '{document.Name}/{@case.Name}' has no solve request.");

            var result = expectedNode.Deserialize<SolveResult>(jsonOptions)
                         ?? throw new InvalidDataException($"Scenario '{document.Name}/{@case.Name}' has no expected result.");

            return new MermaidResultSection(@case.Name, result);
        } catch (JsonException exception) {
            throw new InvalidDataException(
                $"Invalid contracts in scenario '{document.Name}/{@case.Name}': {exception.Message}",
                exception);
        }
    }

    private static JsonNode? Expand(JsonNode template, JsonObject parameters, ISet<string> usedParameters) {
        if (template is JsonValue value && value.TryGetValue<string>(out var text) && IsParameter(text, out var name)) {
            if (!parameters.ContainsKey(name)) {
                throw new InvalidDataException($"Scenario parameter '{name}' has no value.");
            }

            usedParameters.Add(name);

            return parameters[name]?.DeepClone();
        }

        if (template is JsonObject sourceObject) {
            var result = new JsonObject();

            foreach (var property in sourceObject) {
                result[property.Key] = property.Value is null
                    ? null
                    : Expand(property.Value, parameters, usedParameters);
            }

            return result;
        }

        if (template is JsonArray sourceArray) {
            return new JsonArray(sourceArray
                .Select(item => item is null ? null : Expand(item, parameters, usedParameters))
                .ToArray());
        }

        return template.DeepClone();
    }

    private static bool IsParameter(string value, out string name) {
        if (value.StartsWith("${", StringComparison.Ordinal) && value.EndsWith('}')) {
            name = value[2..^1];

            return name.Length > 0;
        }

        name = string.Empty;

        return false;
    }

    private static string OutputPath(string root, string scenarioPath) {
        var directory = Path.GetDirectoryName(scenarioPath)
                        ?? throw new InvalidDataException($"Scenario path '{scenarioPath}' has no directory.");

        var relativeDirectory = Path.GetRelativePath(root, directory);

        return relativeDirectory == "."
            ? Path.GetFileName(directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)) + ".mmd"
            : relativeDirectory + ".mmd";
    }

    private sealed record ScenarioDocument(
        string Name,
        string Description,
        JsonNode Dimensions,
        JsonNode Request,
        JsonNode Expected,
        IReadOnlyList<ScenarioCaseDocument> Cases);

    private sealed record ScenarioCaseDocument(
        string Name,
        JsonObject Parameters);
}
