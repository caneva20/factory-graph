using System.Diagnostics;
using FactoryGraph.Contracts;

namespace FactoryGraph.Solving;

internal sealed class TargetDrivenSolver(
    LinearFactoryModelBuilder modelBuilder,
    ILinearProgramSolver linearProgramSolver,
    LinearFactoryResultProjector resultProjector) : IPlanningModeSolver {
    private const double Infinity = 1e30;

    public PlanningMode Mode => PlanningMode.TargetDriven;

    public SolveResult Solve(SolveRequest request) {
        var stopwatch = Stopwatch.StartNew();

        try {
            var model = modelBuilder.Build(request).WithTargetMinimums(request.Targets);

            if (model.VariableCount == 0) {
                return new SolveResult(SolveStatus.Optimal, resultProjector.Project(request, model, []), [], []);
            }

            var primaryCosts = model.PrimaryCosts(request.Objective);
            using var session = linearProgramSolver.CreateSession(model.Program, request.Options.NumericPolicy);

            if (primaryCosts.All(cost => cost == 0)) {
                return Result(request, model, Run(session, model.ActivityCosts(), null, request, stopwatch));
            }

            var primary = Run(session, primaryCosts, null, request, stopwatch);

            if (primary.Status != SolveStatus.Optimal)
                return Result(request, model, primary);

            var primaryValue = Dot(primaryCosts, primary.Values);

            var objectiveTolerance = Math.Max(
                request.Options.NumericPolicy.AbsoluteTolerance,
                request.Options.NumericPolicy.RelativeTolerance * Math.Abs(primaryValue));

            var objectiveLimit = new ModelRow(
                primaryCosts
                    .Select((coefficient, variable) => (coefficient, variable))
                    .Where(entry => entry.coefficient != 0)
                    .ToDictionary(entry => entry.variable, entry => entry.coefficient),
                -Infinity,
                primaryValue + objectiveTolerance);

            var remaining = Remaining(request.Options.TimeLimit, stopwatch.Elapsed);

            if (remaining is { } duration && duration <= TimeSpan.Zero) {
                return Result(request, model, PrimaryFallback(primary, "The time limit expired before activity minimization."));
            }

            var secondary = Run(session, model.ActivityCosts(), objectiveLimit, request, stopwatch);

            return secondary.Status is SolveStatus.Optimal or SolveStatus.Feasible
                ? Result(request, model, secondary)
                : Result(request, model, PrimaryFallback(primary, "Activity minimization did not produce a usable solution."));
        } catch (Exception exception) {
            return new SolveResult(
                Status: SolveStatus.Failed,
                Graph: null,
                Diagnostics: [Diagnostic(SolverDiagnosticCodes.Failed, $"The target-driven solver failed: {exception.Message}")],
                Bottlenecks: []);
        }
    }

    private StageResult Run(
        ILinearProgramSession session,
        double[] costs,
        ModelRow? objectiveLimit,
        SolveRequest request,
        Stopwatch stopwatch) {
        var remaining = Remaining(request.Options.TimeLimit, stopwatch.Elapsed);
        var result = objectiveLimit is null
            ? session.Solve(costs, remaining)
            : session.AddRowAndSolve(costs, objectiveLimit, remaining);

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
            SolveStatus.Infeasible => Diagnostic(
                SolverDiagnosticCodes.Infeasible,
                "No solution satisfies the graph balances, targets, and capacities."),
            SolveStatus.Unbounded => Diagnostic(SolverDiagnosticCodes.Unbounded, "The planning objective is unbounded."),
            SolveStatus.Failed => Diagnostic(SolverDiagnosticCodes.Failed, $"The linear solver stopped with status '{result.Detail}'."),
            _ => null
        };

        return new StageResult(status, result.Values, diagnostic is null ? [] : [diagnostic]);
    }

    private SolveResult Result(SolveRequest request, LinearFactoryModel model, StageResult stage) {
        var graph = stage.Status is SolveStatus.Optimal or SolveStatus.Feasible
            ? resultProjector.Project(request, model, stage.Values)
            : null;

        return new SolveResult(stage.Status, graph, stage.Diagnostics, []);
    }

    private static StageResult PrimaryFallback(StageResult primary, string message) {
        return primary with {
            Status = SolveStatus.Feasible,
            Diagnostics = [Diagnostic(SolverDiagnosticCodes.SecondaryOptimizationIncomplete, message, DiagnosticSeverity.Warning)]
        };
    }

    private static TimeSpan? Remaining(TimeSpan? limit, TimeSpan elapsed) {
        return limit - elapsed;
    }

    private static double Dot(IReadOnlyList<double> left, IReadOnlyList<double> right) {
        var result = 0d;
        for (var index = 0; index < left.Count; index++) {
            result += left[index] * right[index];
        }

        return result;
    }

    private static SolveDiagnostic Diagnostic(
        string code,
        string message,
        DiagnosticSeverity severity = DiagnosticSeverity.Error) => new(code, severity, message, []);

    private sealed record StageResult(
        SolveStatus Status,
        double[] Values,
        IReadOnlyList<SolveDiagnostic> Diagnostics);
}
