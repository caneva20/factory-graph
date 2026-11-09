using System.Diagnostics;
using FactoryGraph.Contracts;

namespace FactoryGraph.Solving;

/// <summary>
///     One cost vector minimized in a lexicographic sequence.
/// </summary>
/// <param name="Name">The stage description used in fallback diagnostics.</param>
/// <param name="Costs">The cost of each model variable.</param>
/// <param name="LockRow">
///     Builds the row that keeps later stages at this stage's optimal value, or <c>null</c> to bound the cost
///     by the optimal value within the numeric tolerance.
/// </param>
internal sealed record CostStage(string Name, double[] Costs, Func<double, ModelRow>? LockRow = null);

internal sealed record StageResult(
    SolveStatus Status,
    double[] Values,
    IReadOnlyList<SolveDiagnostic> Diagnostics);

internal sealed class LexicographicStageRunner(
    ILinearProgramSession session,
    SolveRequest request,
    Stopwatch stopwatch,
    string infeasibleMessage) {
    private const double Infinity = 1e30;

    public StageResult Optimize(IReadOnlyList<CostStage> stages) {
        var best = Run(stages[0].Costs, null);

        for (var index = 1; index < stages.Count; index++) {
            if (best.Status != SolveStatus.Optimal) return best;

            var next = stages[index];

            if (Remaining() is { } duration && duration <= TimeSpan.Zero) {
                return PrimaryFallback(best, $"The time limit expired before {next.Name}.");
            }

            var previous = stages[index - 1];
            var lockRow = previous.LockRow?.Invoke(Dot(previous.Costs, best.Values))
                          ?? ToleranceRow(previous.Costs, Dot(previous.Costs, best.Values));
            var stage = Run(next.Costs, lockRow);

            if (stage.Status is not (SolveStatus.Optimal or SolveStatus.Feasible)) {
                return PrimaryFallback(best, $"{char.ToUpperInvariant(next.Name[0])}{next.Name[1..]} did not produce a usable solution.");
            }

            best = stage;
        }

        return best;
    }

    private StageResult Run(double[] costs, ModelRow? lockRow) {
        var remaining = Remaining();
        var result = lockRow is null
            ? session.Solve(costs, remaining)
            : session.AddRowAndSolve(costs, lockRow, remaining);

        var status = result.Status switch {
            LinearProgramStatus.Optimal => SolveStatus.Optimal,
            LinearProgramStatus.Feasible => SolveStatus.Feasible,
            LinearProgramStatus.Infeasible => SolveStatus.Infeasible,
            LinearProgramStatus.Unbounded => SolveStatus.Unbounded,
            _ => SolveStatus.Failed
        };

        var diagnostic = status switch {
            SolveStatus.Feasible => Diagnostic(
                SolverDiagnosticCodes.LimitReached,
                $"The linear solver stopped with status '{result.Detail}' after finding a feasible solution.",
                DiagnosticSeverity.Warning),
            SolveStatus.Infeasible => Diagnostic(SolverDiagnosticCodes.Infeasible, infeasibleMessage),
            SolveStatus.Unbounded => Diagnostic(SolverDiagnosticCodes.Unbounded, "The planning objective is unbounded."),
            SolveStatus.Failed => Diagnostic(SolverDiagnosticCodes.Failed, $"The linear solver stopped with status '{result.Detail}'."),
            _ => null
        };

        return new StageResult(status, result.Values, diagnostic is null ? [] : [diagnostic]);
    }

    private ModelRow ToleranceRow(double[] costs, double value) {
        var policy = request.Options.NumericPolicy;
        var tolerance = Math.Max(policy.AbsoluteTolerance, policy.RelativeTolerance * Math.Abs(value));

        return new ModelRow(
            costs
                .Select((coefficient, variable) => (coefficient, variable))
                .Where(entry => entry.coefficient != 0)
                .ToDictionary(entry => entry.variable, entry => entry.coefficient),
            -Infinity,
            value + tolerance);
    }

    private TimeSpan? Remaining() => request.Options.TimeLimit - stopwatch.Elapsed;

    private static StageResult PrimaryFallback(StageResult best, string message) {
        return best with {
            Status = SolveStatus.Feasible,
            Diagnostics = [Diagnostic(SolverDiagnosticCodes.SecondaryOptimizationIncomplete, message, DiagnosticSeverity.Warning)]
        };
    }

    private static double Dot(IReadOnlyList<double> left, IReadOnlyList<double> right) {
        var result = 0d;
        for (var index = 0; index < left.Count; index++) result += left[index] * right[index];

        return result;
    }

    private static SolveDiagnostic Diagnostic(
        string code,
        string message,
        DiagnosticSeverity severity = DiagnosticSeverity.Error) => new(code, severity, message, []);
}
