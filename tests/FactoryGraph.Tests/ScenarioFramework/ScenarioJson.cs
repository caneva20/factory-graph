using System.Text.Json;
using System.Text.Json.Serialization;

namespace FactoryGraph.Tests.ScenarioFramework;

internal static class ScenarioJson {
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    private static JsonSerializerOptions CreateOptions() {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web) {
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
            RespectRequiredConstructorParameters = true,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            WriteIndented = true
        };

        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

        return options;
    }
}
