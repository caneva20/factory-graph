using FactoryGraph.Contracts;

namespace FactoryGraph.Solving;

internal sealed class TargetDrivenModel(
    LinearProgram program,
    IReadOnlyDictionary<Guid, int> activities,
    IReadOnlyDictionary<Guid, int> connections,
    IReadOnlyDictionary<Guid, int> supplies,
    IReadOnlyDictionary<Guid, int> productOutlets,
    IReadOnlyDictionary<Guid, int> surplusOutlets) {
    public LinearProgram Program { get; } = program;

    public IReadOnlyDictionary<Guid, int> Activities { get; } = activities;

    public IReadOnlyDictionary<Guid, int> Connections { get; } = connections;

    public IReadOnlyDictionary<Guid, int> Supplies { get; } = supplies;

    public IReadOnlyDictionary<Guid, int> ProductOutlets { get; } = productOutlets;

    public IReadOnlyDictionary<Guid, int> SurplusOutlets { get; } = surplusOutlets;

    public int VariableCount => Program.VariableCount;

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

    public double[] ActivityCosts() {
        var costs = new double[VariableCount];

        foreach (var variable in Activities.Values) {
            costs[variable] = 1;
        }

        return costs;
    }
}
