using FactoryGraph.Contracts;

namespace FactoryGraph.Tests.Contracts;

public sealed class TargetDrivenContractTests
{
    [Fact]
    public void RequestAndResultPreserveTargetDrivenGraphIdentityAndTopology()
    {
        var machineGroupId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var supplyId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var demandId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var targetId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        var items = new[] {
            new Item("iron-ore", "Iron ore"),
            new Item("iron-plate", "Iron plate")
        };

        var recipes = new[] {
            new Recipe(
                Id: "smelt-iron",
                Name: "Smelt iron",
                Inputs: [new RecipeItem("iron-ore", 1)],
                Outputs: [new RecipeItem("iron-plate", 1)])
        };

        var request = new SolveRequest(
            new FactoryGraphDefinition(
                Items: items,
                Recipes: recipes,
                MachineGroups: [new MachineGroup(machineGroupId, "smelt-iron", 2)],
                Connections: [],
                Supplies: [new Supply(supplyId, machineGroupId, "iron-ore")],
                Demands: [new Demand(demandId, machineGroupId, "iron-plate")],
                SurplusSinks: []),
            [new Target(targetId, demandId, 10)],
            PlanningMode.TargetDriven,
            SolveObjective.MeetTargetsMinimizeExternalSupply,
            new SolverOptions(new NumericPolicy()));

        var result = new SolveResult(
            SolveStatus.Optimal,
            new ComputedFactoryGraph(
                items,
                recipes,
                [
                    new ComputedMachineGroup(
                        Id: machineGroupId,
                        RecipeId: "smelt-iron",
                        ActivityRatePerMachine: 2,
                        AvailableMachineCount: null,
                        ActivityCapacity: null,
                        Activity: 10,
                        EffectiveMachineCount: 5,
                        RequiredWholeMachineCount: 5,
                        Utilization: 1,
                        InputRates: [new ItemRate("iron-ore", 10)],
                        OutputRates: [new ItemRate("iron-plate", 10)])
                ],
                [],
                [new ComputedSupply(supplyId, machineGroupId, "iron-ore", null, 10, 10, 0)],
                [new ComputedDemand(demandId, machineGroupId, "iron-plate", null, 10)],
                [],
                [new ComputedTarget(targetId, demandId, 10, 10, 0, 0)]),
            [],
            []);

        Assert.Equal(PlanningMode.TargetDriven, request.Mode);
        Assert.Equal(machineGroupId, result.Graph!.MachineGroups.Single().Id);
        Assert.Equal(supplyId, result.Graph.Supplies.Single().Id);
        Assert.Equal(demandId, result.Graph.Demands.Single().Id);
        Assert.Equal(targetId, result.Graph.Targets.Single().Id);
        Assert.Equal(demandId, result.Graph.Targets.Single().DemandId);
        Assert.Equal(5, result.Graph.MachineGroups.Single().EffectiveMachineCount);
        Assert.Equal(10, result.Graph.Targets.Single().AchievedRate);
    }
}
