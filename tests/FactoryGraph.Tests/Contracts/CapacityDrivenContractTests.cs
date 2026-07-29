using FactoryGraph.Contracts;

namespace FactoryGraph.Tests.Contracts;

public sealed class CapacityDrivenContractTests
{
    [Fact]
    public void RequestAndResultRepresentCapacityConstraintAndDeficit()
    {
        var machineGroupId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var supplyId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var productOutletId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var targetId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

        var items = new[] {
            new Item("copper-ore", "Copper ore"),
            new Item("copper-plate", "Copper plate")
        };

        var recipes = new[] {
            new Recipe(
                Id: "smelt-copper",
                Name: "Smelt copper",
                Inputs: [new RecipeItem("copper-ore", 1)],
                Outputs: [new RecipeItem("copper-plate", 1)])
        };

        var request = new SolveRequest(
            new FactoryGraphDefinition(
                Items: items,
                Recipes: recipes,
                MachineGroups: [
                    new MachineGroup(
                        machineGroupId,
                        "smelt-copper",
                        ActivityRatePerMachine: 2,
                        AvailableMachineCount: 3)
                ],
                Connections: [],
                Supplies: [new Supply(supplyId, machineGroupId, "copper-ore")],
                ProductOutlets: [new ProductOutlet(productOutletId, machineGroupId, "copper-plate")],
                SurplusOutlets: []),
            [new Target(targetId, productOutletId, 10)],
            PlanningMode.CapacityDriven,
            SolveObjective.MaximizeTargetOutput,
            new SolverOptions(new NumericPolicy()));

        var machineReference = new GraphElementReference(
            GraphElementKind.MachineGroup,
            machineGroupId);

        var result = new SolveResult(
            SolveStatus.Optimal,
            new ComputedFactoryGraph(
                Items: items,
                Recipes: recipes,
                MachineGroups: [
                    new ComputedMachineGroup(
                        machineGroupId,
                        "smelt-copper",
                        ActivityRatePerMachine: 2,
                        AvailableMachineCount: 3,
                        ActivityCapacity: null,
                        Activity: 6,
                        EffectiveMachineCount: 3,
                        RequiredWholeMachineCount: 3,
                        Utilization: 1,
                        [new ItemRate("copper-ore", 6)],
                        [new ItemRate("copper-plate", 6)])
                ],
                Connections: [],
                Supplies: [new ComputedSupply(supplyId, machineGroupId, "copper-ore", null, 6, 6, 0)],
                ProductOutlets: [new ComputedProductOutlet(productOutletId, machineGroupId, "copper-plate", null, 6)],
                SurplusOutlets: [],
                Targets: [new ComputedTarget(targetId, productOutletId, 10, 6, 0, 4)]),
            [],
            [
                new Bottleneck(
                    Kind: BottleneckKind.MachineGroup,
                    Element: machineReference,
                    Capacity: 3,
                    Utilization: 1,
                    Message: "Available machine count limits target output.")
            ]);

        Assert.Equal(PlanningMode.CapacityDriven, request.Mode);
        Assert.Equal(3, request.Graph.MachineGroups.Single().AvailableMachineCount);
        Assert.Equal(6, result.Graph!.Targets.Single().AchievedRate);
        Assert.Equal(4, result.Graph.Targets.Single().DeficitRate);
        Assert.Equal(machineGroupId.ToString("D"), result.Bottlenecks.Single().Element.Id);
    }
}
