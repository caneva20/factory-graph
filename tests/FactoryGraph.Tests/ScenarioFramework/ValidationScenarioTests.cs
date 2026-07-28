namespace FactoryGraph.Tests.ScenarioFramework;

public sealed class ValidationScenarioTests {
    public static IEnumerable<object[]> Scenarios() {
        return ScenarioLoader.Discover()
            .Where(scenario => scenario.Scenario.StartsWith("Validation/", StringComparison.Ordinal))
            .Select(scenario => new object[] { scenario.Scenario, scenario.Case });
    }

    [Theory]
    [MemberData(nameof(Scenarios))]
    public void RequestValidationMatchesScenario(string scenarioName, string caseName) {
        var scenario = ScenarioLoader.Load(scenarioName, caseName);
        var processor = new FactoryGraphProcessor(_ => scenario.Expected);

        ScenarioRunner.Run(scenario, processor.Solve);
    }
}
