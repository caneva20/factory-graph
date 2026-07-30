using FactoryGraph.Contracts;

namespace FactoryGraph.Benchmarks;

internal static class BenchmarkRequestFactory {
    public static SolveRequest Create(string family, PlanningMode mode, int machineGroupCount) {
        if (machineGroupCount < 2) {
            throw new ArgumentOutOfRangeException(nameof(machineGroupCount), "A benchmark chain requires at least two machine groups.");
        }

        var constrained = family switch {
            BenchmarkFamilies.AcyclicChain => false,
            BenchmarkFamilies.ConstrainedChain => true,
            _ => throw new ArgumentOutOfRangeException(nameof(family), family, "Unknown benchmark family.")
        };

        var items = Enumerable.Range(0, machineGroupCount + 1)
            .Select(index => new Item($"item-{index}", $"Item {index}"))
            .ToArray();

        var recipes = Enumerable.Range(0, machineGroupCount)
            .Select(index => new Recipe(
                $"recipe-{index}",
                $"Recipe {index}",
                [new RecipeItem(items[index].Id, 1)],
                [new RecipeItem(items[index + 1].Id, 1)]))
            .ToArray();

        var machineGroups = Enumerable.Range(0, machineGroupCount)
            .Select(index => new MachineGroup(
                Id(1, index),
                recipes[index].Id,
                ActivityRatePerMachine: 1,
                AvailableMachineCount: constrained ? 4 : mode == PlanningMode.CapacityDriven ? 10 : null))
            .ToArray();

        var connections = Enumerable.Range(0, machineGroupCount - 1)
            .Select(index => new ItemConnection(
                Id(2, index),
                machineGroups[index].Id,
                machineGroups[index + 1].Id,
                items[index + 1].Id,
                Capacity: constrained ? 2 : null))
            .ToArray();

        var supply = new Supply(
            Id(3, 0),
            machineGroups[0].Id,
            items[0].Id,
            Capacity: constrained ? 3 : null);

        var productOutlet = new ProductOutlet(
            Id(4, 0),
            machineGroups[^1].Id,
            items[^1].Id,
            Capacity: constrained ? 3 : null);

        var graph = new FactoryGraphDefinition(
            items,
            recipes,
            machineGroups,
            connections,
            [supply],
            [productOutlet],
            []);

        return new SolveRequest(
            graph,
            [new Target(Id(5, 0), productOutlet.Id, 1)],
            mode,
            mode == PlanningMode.TargetDriven
                ? SolveObjective.MeetTargetsMinimizeExternalSupply
                : SolveObjective.MaximizeTargetOutput,
            new SolverOptions(new NumericPolicy()));
    }

    private static Guid Id(int kind, int index) {
        return new Guid(kind, checked((short)index), 0, 0, 0, 0, 0, 0, 0, 0, 0);
    }
}
