namespace FactoryGraph.Tests.ScenarioFramework;

public sealed class ScenarioFrameworkTests {
    public static IEnumerable<object[]> Scenarios() {
        return ScenarioLoader.Discover().Select(scenario => new object[] { scenario.Scenario, scenario.Case });
    }

    [Theory]
    [MemberData(nameof(Scenarios))]
    public void FixtureExecutesThroughSharedRunner(string scenarioName, string caseName) {
        var scenario = ScenarioLoader.Load(scenarioName, caseName);
        var invoked = false;

        ScenarioRunner.Run(
            scenario,
            request => {
                invoked = true;
                Assert.Same(scenario.Request, request);

                return scenario.Expected;
            });

        Assert.True(invoked);
    }

    [Fact]
    public void NumericDifferencesWithinTheFixtureTolerancePass() {
        var scenario = ScenarioLoader.Load("Framework/SingleNode", "fractional-rate");
        var expectedTarget = scenario.Expected.Graph!.Targets.Single();

        var actual = scenario.Expected with {
            Graph = scenario.Expected.Graph with {
                Targets = [
                    expectedTarget with {
                        AchievedRate = expectedTarget.AchievedRate + scenario.Request.Options.NumericPolicy.AbsoluteTolerance / 2
                    }
                ]
            }
        };

        ScenarioRunner.Run(scenario, _ => actual);
    }

    [Fact]
    public void FailureIdentifiesScenarioElementMetricAndValues() {
        var scenario = ScenarioLoader.Load("Framework/SingleNode", "whole-rate");
        var expectedTarget = scenario.Expected.Graph!.Targets.Single();

        var actual = scenario.Expected with {
            Graph = scenario.Expected.Graph with {
                Targets = [expectedTarget with { AchievedRate = expectedTarget.AchievedRate + 1 }]
            }
        };

        var exception = Record.Exception(() => ScenarioRunner.Run(scenario, _ => actual));

        Assert.NotNull(exception);
        Assert.Contains("single-node/whole-rate", exception.Message);
        Assert.Contains(expectedTarget.Id.ToString("D"), exception.Message);
        Assert.Contains("achievedRate", exception.Message);
        Assert.Contains("expected 4", exception.Message);
        Assert.Contains("actual 5", exception.Message);
    }
}
