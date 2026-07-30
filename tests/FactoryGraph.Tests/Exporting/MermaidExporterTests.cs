using FactoryGraph.Contracts;
using FactoryGraph.Exporter.Mermaid;
using FactoryGraph.Tests.ScenarioFramework;

namespace FactoryGraph.Tests.Exporting;

public sealed class MermaidExporterTests {
    private readonly MermaidExporter _exporter = new();

    [Fact]
    public void ValidRequestMatchesExpectedOutput() {
        var scenario = ScenarioLoader.Load("Framework/SimpleChain", "balanced");

        AssertGolden("simple-chain-request.mmd", _exporter.Export(scenario.Request));
    }

    [Fact]
    public void ComputedResultMatchesExpectedOutput() {
        var scenario = ScenarioLoader.Load("Framework/SimpleChain", "balanced");

        AssertGolden("simple-chain-result.mmd", _exporter.Export(scenario.Expected));
    }

    [Fact]
    public void CyclicResultWithDeficitsSurplusAndBottlenecksMatchesExpectedOutput() {
        AssertGolden("cyclic-constrained-result.mmd", _exporter.Export(CyclicConstrainedResult()));
    }

    [Fact]
    public void CollectionOrderDoesNotAffectOutput() {
        var scenario = ScenarioLoader.Load("Framework/SimpleChain", "balanced");
        var request = scenario.Request;
        var requestGraph = request.Graph;
        var result = scenario.Expected;
        var resultGraph = result.Graph!;

        var reorderedRequest = request with {
            Graph = requestGraph with {
                Items = requestGraph.Items.Reverse().ToArray(),
                Recipes = requestGraph.Recipes.Reverse().ToArray(),
                MachineGroups = requestGraph.MachineGroups.Reverse().ToArray(),
                Connections = requestGraph.Connections.Reverse().ToArray(),
                Supplies = requestGraph.Supplies.Reverse().ToArray(),
                ProductOutlets = requestGraph.ProductOutlets.Reverse().ToArray(),
                SurplusOutlets = requestGraph.SurplusOutlets.Reverse().ToArray()
            },
            Targets = request.Targets.Reverse().ToArray()
        };

        var reorderedResult = result with {
            Graph = resultGraph with {
                Items = resultGraph.Items.Reverse().ToArray(),
                Recipes = resultGraph.Recipes.Reverse().ToArray(),
                MachineGroups = resultGraph.MachineGroups.Reverse().ToArray(),
                Connections = resultGraph.Connections.Reverse().ToArray(),
                Supplies = resultGraph.Supplies.Reverse().ToArray(),
                ProductOutlets = resultGraph.ProductOutlets.Reverse().ToArray(),
                SurplusOutlets = resultGraph.SurplusOutlets.Reverse().ToArray(),
                Targets = resultGraph.Targets.Reverse().ToArray()
            },
            Bottlenecks = result.Bottlenecks.Reverse().ToArray()
        };

        Assert.Equal(_exporter.Export(request), _exporter.Export(reorderedRequest));
        Assert.Equal(_exporter.Export(result), _exporter.Export(reorderedResult));
    }

    [Fact]
    public void OptionsControlDirectionLabelsAndStyles() {
        var scenario = ScenarioLoader.Load("Framework/SimpleChain", "balanced");
        var recipe = scenario.Request.Graph.Recipes[0] with { Name = "Smelt [ore] & <heat> \"fast\"" };

        var request = scenario.Request with {
            Graph = scenario.Request.Graph with {
                Recipes = [recipe, .. scenario.Request.Graph.Recipes.Skip(1)]
            }
        };

        var exporter = new MermaidExporter(new MermaidExportOptions {
            Direction = MermaidFlowchartDirection.TopToBottom,
            NumberFormat = "0.00",
            IncludeCapacities = false,
            IncludeStyles = false
        });

        var output = exporter.Export(request);

        Assert.StartsWith("flowchart TB\n", output);
        Assert.Contains("Smelt [ore] &amp; &lt;heat&gt; &quot;fast&quot;", output);
        Assert.Contains("activity/machine: 2.00", output);
        Assert.DoesNotContain("capacity:", output);
        Assert.DoesNotContain("classDef", output);
    }

    [Fact]
    public void ResultWithoutComputedGraphIsRejected() {
        var result = new SolveResult(SolveStatus.InvalidRequest, null, [], []);

        var exception = Assert.Throws<ArgumentException>(() => _exporter.Export(result));

        Assert.Equal("result", exception.ParamName);
        Assert.Contains("computed graph", exception.Message);
    }

    [Fact]
    public void NamedResultsProduceOneDeterministicFlowchart() {
        var externalSupply = ScenarioLoader.Load("Framework/ObjectiveTradeoff", "minimize-external-supply");
        var surplus = ScenarioLoader.Load("Framework/ObjectiveTradeoff", "minimize-surplus");

        var sections = new[] {
            new MermaidResultSection(surplus.CaseName, surplus.Expected),
            new MermaidResultSection(externalSupply.CaseName, externalSupply.Expected)
        };

        var output = _exporter.Export(sections);

        Assert.StartsWith("flowchart LR\n", output);
        Assert.Contains("subgraph section_0[\"minimize-external-supply\"]", output);
        Assert.Contains("subgraph section_1[\"minimize-surplus\"]", output);
        Assert.Contains("section_0_machine_", output);
        Assert.Contains("section_1_machine_", output);
        Assert.Equal(output, _exporter.Export(sections.Reverse()));
    }

    [Fact]
    public void NamedResultWithoutGraphShowsStatusAndDiagnostics() {
        var scenario = ScenarioLoader.Load("Validation/InvalidRequests", "duplicate-item-id");

        var output = _exporter.Export([new MermaidResultSection(scenario.CaseName, scenario.Expected)]);

        Assert.Contains("subgraph section_0[\"duplicate-item-id\"]", output);
        Assert.Contains("status: InvalidRequest", output);
        Assert.Contains("validation.id.duplicate", output);
        Assert.Contains("The item ID 'ore' is duplicated.", output);
        Assert.DoesNotContain("&#39;", output);
        Assert.Contains("classDef diagnostic", output);
    }

    [Fact]
    public void DuplicateSectionNamesAreRejected() {
        var result = ScenarioLoader.Load("Framework/SimpleChain", "balanced").Expected;

        var sections = new[] {
            new MermaidResultSection("same", result),
            new MermaidResultSection("same", result)
        };

        var exception = Assert.Throws<ArgumentException>(() => _exporter.Export(sections));

        Assert.Equal("sections", exception.ParamName);
        Assert.Contains("unique", exception.Message);
    }

    private static void AssertGolden(string fileName, string actual) {
        var path = Path.Combine(AppContext.BaseDirectory, "ExporterGoldens", fileName);
        var expected = File.ReadAllText(path).ReplaceLineEndings("\n");

        Assert.Equal(expected, actual);
    }

    private static SolveResult CyclicConstrainedResult() {
        var machineA = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var machineB = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var forward = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var recycle = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var supply = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var product = Guid.Parse("66666666-6666-6666-6666-666666666666");
        var surplus = Guid.Parse("77777777-7777-7777-7777-777777777777");
        var target = Guid.Parse("88888888-8888-8888-8888-888888888888");

        return new SolveResult(
            SolveStatus.Feasible,
            new ComputedFactoryGraph(
                [new Item("ore", "Ore"), new Item("catalyst", "Catalyst"), new Item("product", "Product")],
                [
                    new Recipe("process-a", "Process A", [new RecipeItem("ore", 1), new RecipeItem("catalyst", 0.25)],
                        [new RecipeItem("catalyst", 0.75)]),
                    new Recipe("process-b", "Process B", [new RecipeItem("catalyst", 0.5)],
                        [new RecipeItem("catalyst", 0.25), new RecipeItem("product", 1)])
                ],
                [
                    new ComputedMachineGroup(machineA, "process-a", 2, 2, null, 4, 2, 2, 1,
                        [new ItemRate("ore", 4), new ItemRate("catalyst", 1)], [new ItemRate("catalyst", 3)]),
                    new ComputedMachineGroup(machineB, "process-b", 2, null, null, 3, 1.5, 2, 0.75,
                        [new ItemRate("catalyst", 1.5)], [new ItemRate("catalyst", 0.75), new ItemRate("product", 3)])
                ],
                [
                    new ComputedConnection(forward, machineA, machineB, "catalyst", 2, 2),
                    new ComputedConnection(recycle, machineB, machineA, "catalyst", null, 1)
                ],
                [new ComputedSupply(supply, machineA, "ore", 5, 5, 4, 1)],
                [new ComputedProductOutlet(product, machineB, "product", null, 3)],
                [new ComputedSurplusOutlet(surplus, machineA, "catalyst", null, 1)],
                [new ComputedTarget(target, product, 4, 3, 0, 1)]),
            [],
            [
                new Bottleneck(BottleneckKind.MachineGroup, new GraphElementReference(GraphElementKind.MachineGroup, machineA), 4, 1,
                    "Machine capacity is binding."),
                new Bottleneck(BottleneckKind.Connection, new GraphElementReference(GraphElementKind.Connection, forward), 2, 1,
                    "Connection capacity is binding."),
                new Bottleneck(BottleneckKind.Supply, new GraphElementReference(GraphElementKind.Supply, supply), 5, 0.8,
                    "Supply capacity limits output.")
            ]);
    }
}
