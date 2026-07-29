namespace FactoryGraph.Contracts;

/// <summary>
///     Stable diagnostic codes reported while solving a validated request.
/// </summary>
public static class SolverDiagnosticCodes {
    private const string Path = "solver";

    /// <summary>The solver failed before it could classify or return a solution.</summary>
    public const string Failed = Path + ".failed";

    /// <summary>The solver stopped at a configured limit after finding a feasible solution.</summary>
    public const string LimitReached = Path + ".limit-reached";

    /// <summary>No solution satisfies the model constraints.</summary>
    public const string Infeasible = Path + ".infeasible";

    /// <summary>The objective can improve without a finite bound.</summary>
    public const string Unbounded = Path + ".unbounded";

    /// <summary>The primary solution is valid, but secondary optimization did not complete.</summary>
    public const string SecondaryOptimizationIncomplete = Path + ".secondary-optimization-incomplete";

    /// <summary>Diagnostic codes for planning-mode execution.</summary>
    public static class Mode {
        /// <summary>The built-in processor does not implement the requested planning mode.</summary>
        public const string NotImplemented = Path + ".mode.not-implemented";
    }
}
