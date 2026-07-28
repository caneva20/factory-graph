using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using FactoryGraph.Contracts;

namespace FactoryGraph.Tests.ScenarioFramework;

internal static class SolveResultAssertions {
    public static void Equal(ScenarioFixture scenario, SolveResult actual) {
        var expectedNode = JsonSerializer.SerializeToNode(scenario.Expected, ScenarioJson.Options)!;
        var actualNode = JsonSerializer.SerializeToNode(actual, ScenarioJson.Options)!;
        var differences = new List<string>();

        Compare(expectedNode, actualNode, "result", null, scenario.Request.Options.NumericPolicy, differences);

        Assert.True(
            differences.Count == 0,
            $"Scenario '{scenario.Name}/{scenario.CaseName}' result differs:{Environment.NewLine}{string.Join(Environment.NewLine, differences)}");
    }

    private static void Compare(
        JsonNode? expected,
        JsonNode? actual,
        string metric,
        string? element,
        NumericPolicy numericPolicy,
        ICollection<string> differences) {
        if (expected is null || actual is null) {
            if (expected is not null || actual is not null) {
                AddDifference(differences, element, metric, expected, actual);
            }

            return;
        }

        if (expected is JsonObject expectedObject && actual is JsonObject actualObject) {
            CompareObjects(expectedObject, actualObject, metric, element, numericPolicy, differences);

            return;
        }

        if (expected is JsonArray expectedArray && actual is JsonArray actualArray) {
            CompareArrays(expectedArray, actualArray, metric, element, numericPolicy, differences);

            return;
        }

        if (TryGetNumber(expected, out var expectedNumber) && TryGetNumber(actual, out var actualNumber)) {
            var normalizedExpected = numericPolicy.NormalizeNearZero(expectedNumber);
            var normalizedActual = numericPolicy.NormalizeNearZero(actualNumber);

            if (!numericPolicy.NearlyEquals(normalizedExpected, normalizedActual)) {
                AddDifference(differences, element, metric, expectedNumber, actualNumber);
            }

            return;
        }

        if (expected.ToJsonString() != actual.ToJsonString()) {
            AddDifference(differences, element, metric, expected, actual);
        }
    }

    private static void CompareObjects(
        JsonObject expected,
        JsonObject actual,
        string metric,
        string? element,
        NumericPolicy numericPolicy,
        ICollection<string> differences) {
        var currentElement = GetIdentity(expected) is { } identity ? $"{metric}/{identity}" : element;

        foreach (var propertyName in expected.Select(property => property.Key)
                     .Union(actual.Select(property => property.Key), StringComparer.Ordinal)
                     .Order(StringComparer.Ordinal)) {
            var propertyMetric = $"{metric}.{propertyName}";

            if (!expected.ContainsKey(propertyName) || !actual.ContainsKey(propertyName)) {
                AddDifference(differences, currentElement, propertyMetric, expected[propertyName], actual[propertyName]);

                continue;
            }

            Compare(expected[propertyName], actual[propertyName], propertyMetric, currentElement, numericPolicy, differences);
        }
    }

    private static void CompareArrays(
        JsonArray expected,
        JsonArray actual,
        string metric,
        string? element,
        NumericPolicy numericPolicy,
        ICollection<string> differences) {
        if (TryIndexByIdentity(expected, out var expectedByIdentity) && TryIndexByIdentity(actual, out var actualByIdentity)) {
            foreach (var identity in expectedByIdentity.Keys
                         .Union(actualByIdentity.Keys, StringComparer.Ordinal)
                         .Order(StringComparer.Ordinal)) {
                var itemMetric = $"{metric}[{identity}]";
                expectedByIdentity.TryGetValue(identity, out var expectedItem);
                actualByIdentity.TryGetValue(identity, out var actualItem);
                Compare(expectedItem, actualItem, itemMetric, $"{metric}/{identity}", numericPolicy, differences);
            }

            return;
        }

        var count = Math.Max(expected.Count, actual.Count);

        for (var index = 0; index < count; index++) {
            Compare(
                index < expected.Count ? expected[index] : null,
                index < actual.Count ? actual[index] : null,
                $"{metric}[{index}]",
                element,
                numericPolicy,
                differences);
        }
    }

    private static bool TryIndexByIdentity(JsonArray array, out IReadOnlyDictionary<string, JsonNode?> items) {
        var result = new Dictionary<string, JsonNode?>(StringComparer.Ordinal);

        foreach (var item in array) {
            if (item is not JsonObject value || GetIdentity(value) is not { } identity || !result.TryAdd(identity, item)) {
                items = new Dictionary<string, JsonNode?>();

                return false;
            }
        }

        items = result;

        return array.Count > 0;
    }

    private static string? GetIdentity(JsonObject value) {
        if (GetString(value["id"]) is { } id) {
            return id;
        }

        if (GetString(value["itemId"]) is { } itemId) {
            return itemId;
        }

        if (GetString(value["code"]) is { } code) {
            return code;
        }

        if (GetString(value["kind"]) is { } kind && value["element"] is JsonObject element && GetIdentity(element) is { } elementId) {
            return $"{kind}:{elementId}";
        }

        return null;
    }

    private static string? GetString(JsonNode? node) {
        return node is JsonValue value && value.TryGetValue<string>(out var result) ? result : null;
    }

    private static bool TryGetNumber(JsonNode node, out double value) {
        if (node is JsonValue jsonValue && jsonValue.TryGetValue(out value)) {
            return true;
        }

        value = 0;

        return false;
    }

    private static void AddDifference(
        ICollection<string> differences,
        string? element,
        string metric,
        object? expected,
        object? actual) {
        differences.Add(
            $"- element '{element ?? "result"}', metric '{metric}': expected {Format(expected)}, actual {Format(actual)}");
    }

    private static string Format(object? value) {
        return value switch {
            null => "<missing>",
            double number => number.ToString("G17", CultureInfo.InvariantCulture),
            JsonNode node => node.ToJsonString(),
            _ => value.ToString() ?? string.Empty
        };
    }
}
