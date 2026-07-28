using FactoryGraph.Contracts;

namespace FactoryGraph.Tests.ScenarioFramework;

internal static class ScenarioRunner {
    public static void Run(ScenarioFixture scenario, Func<SolveRequest, SolveResult> solve) {
        var actual = solve(scenario.Request);
        SolveResultAssertions.Equal(scenario, actual);
    }
}
