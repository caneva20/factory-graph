using FactoryGraph.Contracts;

namespace FactoryGraph.Solving;

internal interface ILinearProgramSolver {
    LinearProgramResult Solve(
        LinearProgram program,
        IReadOnlyList<double> costs,
        ModelRow? additionalRow,
        TimeSpan? timeLimit,
        NumericPolicy numericPolicy);
}

internal enum LinearProgramStatus {
    Optimal,
    Feasible,
    Infeasible,
    Unbounded,
    Failed
}

internal sealed record LinearProgramResult(
    LinearProgramStatus Status,
    double[] Values,
    string Detail);
