using System.Text.Json;
using System.Text.Json.Nodes;
using FactoryGraph.Contracts;

namespace FactoryGraph.Tests.ScenarioFramework;

internal static class ScenarioLoader {
    private const string FixtureFileName = "scenario.json";

    public static IReadOnlyList<(string Scenario, string Case)> Discover() {
        var root = GetScenarioRoot();

        return Directory
            .EnumerateFiles(root, FixtureFileName, SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal)
            .SelectMany(path => ReadDocument(path).Cases.Select(@case => (
                Scenario: Path.GetRelativePath(root, Path.GetDirectoryName(path)!).Replace(Path.DirectorySeparatorChar, '/'),
                Case: @case.Name)))
            .ToArray();
    }

    public static ScenarioFixture Load(string scenario, string caseName) {
        var path = Path.GetFullPath(Path.Combine(GetScenarioRoot(), scenario, FixtureFileName));
        var root = GetScenarioRoot() + Path.DirectorySeparatorChar;

        if (!path.StartsWith(root, StringComparison.Ordinal) || !File.Exists(path)) {
            throw new InvalidOperationException($"Scenario '{scenario}' does not identify a fixture.");
        }

        var document = ReadDocument(path);

        var selectedCase = document.Cases.SingleOrDefault(@case => @case.Name == caseName)
                           ?? throw new InvalidOperationException($"Scenario '{document.Name}' has no case named '{caseName}'.");

        var usedParameters = new HashSet<string>(StringComparer.Ordinal);
        var requestNode = Expand(document.Request, selectedCase.Parameters, usedParameters);
        var expectedNode = Expand(document.Expected, selectedCase.Parameters, usedParameters);

        var unusedParameters = selectedCase.Parameters
            .Select(parameter => parameter.Key)
            .Where(parameter => !usedParameters.Contains(parameter))
            .Order(StringComparer.Ordinal)
            .ToArray();

        if (unusedParameters.Length > 0) {
            throw new InvalidOperationException(
                $"Scenario '{document.Name}/{caseName}' has unused parameters: {string.Join(", ", unusedParameters)}.");
        }

        var request = requestNode.Deserialize<SolveRequest>(ScenarioJson.Options)
                      ?? throw new InvalidOperationException($"Scenario '{document.Name}/{caseName}' has no solve request.");

        var expected = expectedNode.Deserialize<SolveResult>(ScenarioJson.Options)
                       ?? throw new InvalidOperationException($"Scenario '{document.Name}/{caseName}' has no expected result.");

        return new ScenarioFixture(
            document.Name,
            caseName,
            document.Description,
            document.Dimensions,
            request,
            expected);
    }

    private static ScenarioDocument ReadDocument(string path) {
        return JsonSerializer.Deserialize<ScenarioDocument>(File.ReadAllText(path), ScenarioJson.Options)
               ?? throw new InvalidOperationException($"Scenario fixture '{path}' is empty.");
    }

    private static JsonNode Expand(JsonNode template, JsonObject parameters, ISet<string> usedParameters) {
        if (template is JsonValue value && value.TryGetValue<string>(out var text) && IsParameter(text, out var name)) {
            if (!parameters.ContainsKey(name)) {
                throw new InvalidOperationException($"Scenario parameter '{name}' has no value.");
            }

            usedParameters.Add(name);

            return parameters[name]?.DeepClone() ?? JsonValue.Create((string?)null)!;
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

    private static string GetScenarioRoot() {
        var root = Path.Combine(AppContext.BaseDirectory, "Scenarios");

        return Directory.Exists(root)
            ? Path.GetFullPath(root)
            : throw new InvalidOperationException($"Scenario directory '{root}' does not exist.");
    }

    private sealed record ScenarioDocument(
        string Name,
        string Description,
        ScenarioDimensions Dimensions,
        JsonNode Request,
        JsonNode Expected,
        IReadOnlyList<ScenarioCaseDocument> Cases);

    private sealed record ScenarioCaseDocument(
        string Name,
        JsonObject Parameters);
}
