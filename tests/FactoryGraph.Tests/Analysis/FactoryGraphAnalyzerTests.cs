using FactoryGraph.Analysis;
using FactoryGraph.Contracts;

namespace FactoryGraph.Tests.Analysis;

public sealed class FactoryGraphAnalyzerTests {
    private static readonly NumericPolicy NumericPolicy = new();

    [Fact]
    public void AcyclicGraphHasNoCyclicComponents() {
        var sourceId = Id(1);
        var destinationId = Id(2);
        var graph = Graph(
            [SourceRecipe("source", "material"), ConsumerRecipe("consumer", "material")],
            [Group(sourceId, "source"), Group(destinationId, "consumer")],
            [Connection(11, sourceId, destinationId, "material")]);

        var analysis = new FactoryGraphAnalyzer().Analyze(graph, NumericPolicy);

        Assert.Empty(analysis.CyclicComponents);
    }

    [Fact]
    public void CyclicComponentsAndInternalConnectionsFollowGraphOrder() {
        var firstId = Id(1);
        var secondId = Id(2);
        var selfLoopId = Id(3);
        var destinationId = Id(4);
        var firstConnectionId = Id(11);
        var secondConnectionId = Id(12);
        var selfConnectionId = Id(13);
        var externalConnectionId = Id(14);
        var graph = Graph(
            [RecyclingRecipe("recycle", "material")],
            [
                Group(firstId, "recycle"),
                Group(secondId, "recycle"),
                Group(selfLoopId, "recycle"),
                Group(destinationId, "recycle")
            ],
            [
                Connection(firstConnectionId, firstId, secondId, "material"),
                Connection(secondConnectionId, secondId, firstId, "material"),
                Connection(externalConnectionId, secondId, destinationId, "material"),
                Connection(selfConnectionId, selfLoopId, selfLoopId, "material")
            ]);

        var analysis = new FactoryGraphAnalyzer().Analyze(graph, NumericPolicy);

        Assert.Collection(
            analysis.CyclicComponents,
            component => {
                Assert.Equal([firstId, secondId], component.MachineGroupIds);
                Assert.Equal([firstConnectionId, secondConnectionId], component.ConnectionIds);
                Assert.True(component.RequiresStartupInventory);
            },
            component => {
                Assert.Equal([selfLoopId], component.MachineGroupIds);
                Assert.Equal([selfConnectionId], component.ConnectionIds);
                Assert.True(component.RequiresStartupInventory);
            });
    }

    [Fact]
    public void StartableUpstreamGroupBootstrapsAConnectedCycle() {
        var sourceId = Id(1);
        var firstId = Id(2);
        var secondId = Id(3);
        var graph = Graph(
            [
                SourceRecipe("source", "first"),
                TransformRecipe("first-to-second", "first", "second"),
                TransformRecipe("second-to-first", "second", "first")
            ],
            [Group(sourceId, "source"), Group(firstId, "first-to-second"), Group(secondId, "second-to-first")],
            [
                Connection(11, sourceId, firstId, "first"),
                Connection(12, firstId, secondId, "second"),
                Connection(13, secondId, firstId, "first")
            ]);

        var component = Assert.Single(new FactoryGraphAnalyzer().Analyze(graph, NumericPolicy).CyclicComponents);

        Assert.False(component.RequiresStartupInventory);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1e-12, true)]
    [InlineData(1.01e-12, false)]
    public void SupplyMustExceedNearZeroThresholdToBootstrapCycle(double capacity, bool requiresStartupInventory) {
        var firstId = Id(1);
        var secondId = Id(2);
        var graph = Graph(
            [
                TransformRecipe("first-to-second", "first", "second"),
                TransformRecipe("second-to-first", "second", "first")
            ],
            [Group(firstId, "first-to-second"), Group(secondId, "second-to-first")],
            [
                Connection(11, firstId, secondId, "second"),
                Connection(12, secondId, firstId, "first")
            ],
            [new Supply(Id(21), firstId, "first", capacity)]);

        var component = Assert.Single(new FactoryGraphAnalyzer().Analyze(graph, NumericPolicy).CyclicComponents);

        Assert.Equal(requiresStartupInventory, component.RequiresStartupInventory);
    }

    [Fact]
    public void UnlimitedSupplyBootstrapsCycle() {
        var groupId = Id(1);
        var graph = Graph(
            [RecyclingRecipe("recycle", "material")],
            [Group(groupId, "recycle")],
            [Connection(11, groupId, groupId, "material")],
            [new Supply(Id(21), groupId, "material")]);

        var component = Assert.Single(new FactoryGraphAnalyzer().Analyze(graph, NumericPolicy).CyclicComponents);

        Assert.False(component.RequiresStartupInventory);
    }

    private static FactoryGraphDefinition Graph(
        IReadOnlyList<Recipe> recipes,
        IReadOnlyList<MachineGroup> groups,
        IReadOnlyList<ItemConnection> connections,
        IReadOnlyList<Supply>? supplies = null) {
        return new FactoryGraphDefinition(
            [new Item("first", "First"), new Item("second", "Second"), new Item("material", "Material")],
            recipes,
            groups,
            connections,
            supplies ?? [],
            [],
            []);
    }

    private static Recipe SourceRecipe(string id, string output) {
        return new Recipe(id, id, [], [new RecipeItem(output, 1)]);
    }

    private static Recipe ConsumerRecipe(string id, string input) {
        return new Recipe(id, id, [new RecipeItem(input, 1)], []);
    }

    private static Recipe TransformRecipe(string id, string input, string output) {
        return new Recipe(id, id, [new RecipeItem(input, 1)], [new RecipeItem(output, 1)]);
    }

    private static Recipe RecyclingRecipe(string id, string item) {
        return TransformRecipe(id, item, item);
    }

    private static MachineGroup Group(Guid id, string recipeId) {
        return new MachineGroup(id, recipeId, 1);
    }

    private static ItemConnection Connection(int id, Guid sourceId, Guid destinationId, string itemId) {
        return new ItemConnection(Id(id), sourceId, destinationId, itemId);
    }

    private static ItemConnection Connection(Guid id, Guid sourceId, Guid destinationId, string itemId) {
        return new ItemConnection(id, sourceId, destinationId, itemId);
    }

    private static Guid Id(int value) {
        return Guid.Parse($"{value:x8}-0000-0000-0000-000000000000");
    }
}
