using FactoryGraph.Contracts;

namespace FactoryGraph.Solving;

internal sealed class BuiltInSolveRequestSolver : ISolveRequestSolver {
    private readonly IReadOnlyDictionary<PlanningMode, IPlanningModeSolver> _solvers;

    public BuiltInSolveRequestSolver(IEnumerable<IPlanningModeSolver> solvers) {
        _solvers = solvers.ToDictionary(solver => solver.Mode);
    }

    public SolveResult Solve(SolveRequest request) {
        return _solvers.TryGetValue(request.Mode, out var solver)
            ? solver.Solve(request)
            : new SolveResult(
                Status: SolveStatus.Failed,
                Graph: null,
                Diagnostics: [
                    new SolveDiagnostic(
                        SolverDiagnosticCodes.Mode.NotImplemented,
                        DiagnosticSeverity.Error,
                        $"Planning mode '{request.Mode}' is not implemented.",
                        [])
                ],
                Bottlenecks: []);
    }
}
