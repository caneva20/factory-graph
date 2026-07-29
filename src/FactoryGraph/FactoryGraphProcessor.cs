using FactoryGraph.Contracts;
using FactoryGraph.Solving;
using FactoryGraph.Validation;

namespace FactoryGraph;

/// <summary>
///     Validates factory graph requests before passing them to a solve stage.
/// </summary>
public sealed class FactoryGraphProcessor {
    private readonly ISolveRequestValidator _validator;
    private readonly ISolveRequestSolver _solver;

    internal FactoryGraphProcessor(ISolveRequestValidator validator, ISolveRequestSolver solver) {
        _validator = validator;
        _solver = solver;
    }

    /// <summary>
    ///     Validates and solves a factory graph request.
    /// </summary>
    /// <param name="request">The request to validate and solve.</param>
    /// <returns>An invalid result or the result produced by the configured solve stage.</returns>
    public SolveResult Solve(SolveRequest request) {
        var diagnostics = _validator.Validate(request);

        return diagnostics.Count > 0
            ? new SolveResult(SolveStatus.InvalidRequest, null, diagnostics, [])
            : _solver.Solve(request);
    }
}
