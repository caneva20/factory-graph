namespace FactoryGraph.Tests.ScenarioFramework;

public sealed class CycleScenarioTests {
    public static IEnumerable<object[]> Scenarios() {
        return ScenarioLoader.Discover()
            .Where(scenario => scenario.Scenario.StartsWith("Cycles/", StringComparison.Ordinal))
            .Select(scenario => new object[] { scenario.Scenario, scenario.Case });
    }

    [Theory]
    [MemberData(nameof(Scenarios))]
    public void BuiltInSolverMatchesScenario(string scenarioName, string caseName) {
        var scenario = ScenarioLoader.Load(scenarioName, caseName);
        var processor = FactoryGraphProcessorFactory.Create();

        ScenarioRunner.Run(scenario, processor.Solve);
    }
}
