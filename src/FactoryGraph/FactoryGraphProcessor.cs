using FactoryGraph.Contracts;
using FactoryGraph.Validation;

namespace FactoryGraph;

/// <summary>
///     Validates factory graph requests before passing them to a solve stage.
/// </summary>
public sealed class FactoryGraphProcessor {
    private readonly Func<SolveRequest, SolveResult> _solveValidRequest;

    /// <summary>
    ///     Creates a processor that delegates valid requests to the supplied solve stage.
    /// </summary>
    /// <param name="solveValidRequest">The stage that solves a validated request.</param>
    public FactoryGraphProcessor(Func<SolveRequest, SolveResult> solveValidRequest) {
        ArgumentNullException.ThrowIfNull(solveValidRequest);
        _solveValidRequest = solveValidRequest;
    }

    /// <summary>
    ///     Validates and solves a factory graph request.
    /// </summary>
    /// <param name="request">The request to validate and solve.</param>
    /// <returns>An invalid result or the result produced by the configured solve stage.</returns>
    public SolveResult Solve(SolveRequest request) {
        var diagnostics = SolveRequestValidator.Validate(request);

        return diagnostics.Count > 0
            ? new SolveResult(SolveStatus.InvalidRequest, null, diagnostics, [])
            : _solveValidRequest(request);
    }
}
