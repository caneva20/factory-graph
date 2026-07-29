using FactoryGraph.Contracts;
using FactoryGraph.Solving;

namespace FactoryGraph.Tests;

internal sealed class TestSolveRequestSolver(Func<SolveRequest, SolveResult> solve) : ISolveRequestSolver {
    public SolveResult Solve(SolveRequest request) => solve(request);
}
