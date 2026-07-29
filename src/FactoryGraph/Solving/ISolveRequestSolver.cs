using FactoryGraph.Contracts;

namespace FactoryGraph.Solving;

internal interface ISolveRequestSolver {
    SolveResult Solve(SolveRequest request);
}

internal interface IPlanningModeSolver : ISolveRequestSolver {
    PlanningMode Mode { get; }
}
