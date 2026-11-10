using FactoryGraph.Contracts;

namespace FactoryGraph.Solving;

internal sealed class LinearFactoryModel(
    LinearProgram program,
    IReadOnlyDictionary<Guid, int> activities,
    IReadOnlyDictionary<Guid, int> connections,
    IReadOnlyDictionary<Guid, int> supplies,
    IReadOnlyDictionary<(Guid SupplyId, Guid MachineGroupId), int> supplyDestinations,
    IReadOnlyDictionary<Guid, int> productOutlets,
    IReadOnlyDictionary<Guid, int> surplusOutlets,
    IReadOnlyDictionary<Guid, int> targets,
    IReadOnlyList<int> fallbackSupplies) {
    public LinearProgram Program { get; } = program;

    public IReadOnlyDictionary<Guid, int> Activities { get; } = activities;

    public IReadOnlyDictionary<Guid, int> Connections { get; } = connections;

    public IReadOnlyDictionary<Guid, int> Supplies { get; } = supplies;

    /// <summary>Maps each supply and destination machine group to the variable for the flow between them.</summary>
    public IReadOnlyDictionary<(Guid SupplyId, Guid MachineGroupId), int> SupplyDestinations { get; } = supplyDestinations;

    public IReadOnlyDictionary<Guid, int> ProductOutlets { get; } = productOutlets;

    public IReadOnlyDictionary<Guid, int> SurplusOutlets { get; } = surplusOutlets;

    /// <summary>Maps each target to the variable for the combined delivery of its product outlets.</summary>
    public IReadOnlyDictionary<Guid, int> Targets { get; } = targets;

    public IReadOnlyList<int> FallbackSupplies { get; } = fallbackSupplies;

    public int VariableCount => Program.VariableCount;

    public LinearFactoryModel WithTargetMinimums(IEnumerable<Target> targets) {
        var lowerBounds = Program.LowerBounds.ToArray();

        foreach (var target in targets) {
            lowerBounds[Targets[target.Id]] = target.Rate;
        }

        return WithProgram(Program with { LowerBounds = lowerBounds });
    }

    public LinearFactoryModel WithProgram(LinearProgram program) {
        return new LinearFactoryModel(
            program,
            Activities,
            Connections,
            Supplies,
            SupplyDestinations,
            ProductOutlets,
            SurplusOutlets,
            Targets,
            FallbackSupplies);
    }

    public double[] PrimaryCosts(SolveObjective objective) {
        var costs = new double[VariableCount];

        var variables = objective == SolveObjective.MeetTargetsMinimizeExternalSupply
            ? Supplies.Values
            : SurplusOutlets.Values;

        foreach (var variable in variables) {
            costs[variable] = 1;
        }

        return costs;
    }

    public double[] FallbackSupplyCosts() {
        var costs = new double[VariableCount];

        foreach (var variable in FallbackSupplies) {
            costs[variable] = 1;
        }

        return costs;
    }

    public double[] ActivityCosts() {
        var costs = new double[VariableCount];

        foreach (var variable in Activities.Values) {
            costs[variable] = 1;
        }

        return costs;
    }
}
