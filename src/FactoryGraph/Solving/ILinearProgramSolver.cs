using FactoryGraph.Contracts;

namespace FactoryGraph.Solving;

internal interface ILinearProgramSolver {
    ILinearProgramSession CreateSession(
        LinearProgram program,
        NumericPolicy numericPolicy);
}

internal interface ILinearProgramSession : IDisposable {
    LinearProgramResult Solve(IReadOnlyList<double> costs, TimeSpan? timeLimit);

    LinearProgramResult AddRowAndSolve(
        IReadOnlyList<double> costs,
        ModelRow additionalRow,
        TimeSpan? timeLimit);
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
