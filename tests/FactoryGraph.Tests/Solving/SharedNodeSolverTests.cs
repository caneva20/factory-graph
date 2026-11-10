using FactoryGraph.Contracts;
using FactoryGraph.Validation;

namespace FactoryGraph.Tests.Solving;

public sealed class SharedNodeSolverTests {
    private static readonly Guid MachineA = Id(1);
    private static readonly Guid MachineB = Id(2);
    private static readonly Guid OutletA = Id(3);
    private static readonly Guid OutletB = Id(4);
    private static readonly Guid SharedSupply = Id(5);

    [Fact]
    public void SharedSupplyLimitAppliesToCombinedConsumption() {
        var request = Request(
            [new Supply(SharedSupply, [MachineA, MachineB], "ore", Capacity: 7)],
            [new Target(Id(10), [OutletA], 3), new Target(Id(11), [OutletB], 4)]);

        var result = Solve(request);

        Assert.Equal(SolveStatus.Optimal, result.Status);
        var supply = Assert.Single(result.Graph!.Supplies);
        Assert.Equal(7, supply.ProvidedRate, 7);
        Assert.Equal(3, supply.Destinations.Single(destination => destination.MachineGroupId == MachineA).FlowRate, 7);
        Assert.Equal(4, supply.Destinations.Single(destination => destination.MachineGroupId == MachineB).FlowRate, 7);
    }

    [Fact]
    public void SharedSupplyBelowCombinedDemandCannotMeetTargets() {
        var request = Request(
            [new Supply(SharedSupply, [MachineA, MachineB], "ore", Capacity: 6)],
            [new Target(Id(10), [OutletA], 3), new Target(Id(11), [OutletB], 4)]);

        var result = Solve(request);

        Assert.NotEqual(SolveStatus.Optimal, result.Status);
    }

    [Fact]
    public void UnlimitedSharedSupplyFeedsEveryDestination() {
        var request = Request(
            [new Supply(SharedSupply, [MachineA, MachineB], "ore")],
            [new Target(Id(10), [OutletA], 30), new Target(Id(11), [OutletB], 40)]);

        var result = Solve(request);

        Assert.Equal(SolveStatus.Optimal, result.Status);
        Assert.Equal(70, result.Graph!.Supplies.Single().ProvidedRate, 7);
    }

    [Fact]
    public void SharedTargetIsMetByCombinedProducerDelivery() {
        var request = Request(
            [new Supply(Id(5), [MachineA], "ore", Capacity: 4), new Supply(Id(6), [MachineB], "ore", Capacity: 4)],
            [new Target(Id(10), [OutletA, OutletB], 6)]);

        var result = Solve(request);

        Assert.Equal(SolveStatus.Optimal, result.Status);
        var target = Assert.Single(result.Graph!.Targets);
        Assert.Equal([OutletA, OutletB], target.ProductOutletIds);
        Assert.Equal(6, target.AchievedRate, 7);
        Assert.Equal(6, result.Graph.ProductOutlets.Sum(outlet => outlet.DeliveredRate), 7);
    }

    [Fact]
    public void SharedTargetRateAboveCombinedSupplyIsNotMet() {
        var request = Request(
            [new Supply(Id(5), [MachineA], "ore", Capacity: 2), new Supply(Id(6), [MachineB], "ore", Capacity: 2)],
            [new Target(Id(10), [OutletA, OutletB], 6)]);

        var result = Solve(request);

        Assert.NotEqual(SolveStatus.Optimal, result.Status);
    }

    [Fact]
    public void CapacityDrivenSharedTargetUsesEveryProducer() {
        var request = Request(
                [new Supply(Id(5), [MachineA], "ore"), new Supply(Id(6), [MachineB], "ore")],
                [new Target(Id(10), [OutletA, OutletB], 1)],
                [new MachineGroup(MachineA, "make", 1, ActivityCapacity: 4), new MachineGroup(MachineB, "make", 1, ActivityCapacity: 3)])
            with {
                Mode = PlanningMode.CapacityDriven,
                Objective = SolveObjective.MaximizeTargetOutput
            };

        var result = Solve(request);

        Assert.Equal(SolveStatus.Optimal, result.Status);
        Assert.Equal(7, result.Graph!.Targets.Single().AchievedRate, 7);
    }

    [Fact]
    public void ValidationRequiresADestinationAndRejectsDuplicates() {
        var empty = Solve(Request([new Supply(SharedSupply, [], "ore")], [new Target(Id(10), [OutletA], 1)]));
        var duplicate = Solve(Request(
            [new Supply(SharedSupply, [MachineA, MachineA], "ore")],
            [new Target(Id(10), [OutletA], 1)]));

        Assert.Contains(empty.Diagnostics, d => d.Code == ValidationDiagnosticCodes.Supply.DestinationRequired);
        Assert.Contains(duplicate.Diagnostics, d => d.Code == ValidationDiagnosticCodes.Supply.DuplicateDestination);
    }

    [Fact]
    public void ValidationRequiresAnOutletAndRejectsDuplicates() {
        var supplies = new[] { new Supply(SharedSupply, [MachineA, MachineB], "ore") };
        var empty = Solve(Request(supplies, [new Target(Id(10), [], 1)]));
        var duplicate = Solve(Request(supplies, [new Target(Id(10), [OutletA, OutletA], 1)]));

        Assert.Contains(empty.Diagnostics, d => d.Code == ValidationDiagnosticCodes.Target.ProductOutletRequired);
        Assert.Contains(duplicate.Diagnostics, d => d.Code == ValidationDiagnosticCodes.Target.DuplicateProductOutlet);
    }

    [Fact]
    public void ValidationRejectsTargetOutletsWithDifferentItems() {
        var request = Request(
            [new Supply(SharedSupply, [MachineA, MachineB], "ore")],
            [new Target(Id(10), [OutletA, OutletB], 1)]);

        request = request with {
            Graph = request.Graph with {
                ProductOutlets = [request.Graph.ProductOutlets[0], request.Graph.ProductOutlets[1] with { ItemId = "ore" }]
            }
        };

        Assert.Contains(Solve(request).Diagnostics, d => d.Code == ValidationDiagnosticCodes.Item.Incompatible);
    }

    private static SolveResult Solve(SolveRequest request) => FactoryGraphProcessorFactory.Create().Solve(request);

    private static SolveRequest Request(
        IReadOnlyList<Supply> supplies,
        IReadOnlyList<Target> targets,
        IReadOnlyList<MachineGroup>? groups = null) {
        return new SolveRequest(
            new FactoryGraphDefinition(
                [new Item("ore", "ore"), new Item("iron", "iron")],
                [new Recipe("make", "make", [new RecipeItem("ore", 1)], [new RecipeItem("iron", 1)])],
                groups ?? [new MachineGroup(MachineA, "make", 1), new MachineGroup(MachineB, "make", 1)],
                [],
                supplies,
                [new ProductOutlet(OutletA, MachineA, "iron"), new ProductOutlet(OutletB, MachineB, "iron")],
                []),
            targets,
            PlanningMode.TargetDriven,
            SolveObjective.MeetTargetsMinimizeExternalSupply,
            new SolverOptions(new NumericPolicy()));
    }

    private static Guid Id(int value) => Guid.Parse($"{value:x8}-0000-0000-0000-000000000000");
}
