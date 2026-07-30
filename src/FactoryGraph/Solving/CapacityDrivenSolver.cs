using System.Diagnostics;
using FactoryGraph.Contracts;

namespace FactoryGraph.Solving;

internal sealed class CapacityDrivenSolver(
    CapacityDrivenModelBuilder modelBuilder,
    ILinearProgramSolver linearProgramSolver,
    LinearFactoryResultProjector resultProjector,
    CapacityBottleneckReporter bottleneckReporter) : IPlanningModeSolver {
    public PlanningMode Mode => PlanningMode.CapacityDriven;

    public SolveResult Solve(SolveRequest request) {
        var stopwatch = Stopwatch.StartNew();

        try {
            var model = modelBuilder.Build(request);
            var primaryCosts = model.PrimaryCosts();
            var primary = Run(model.FactoryModel, primaryCosts, null, request, stopwatch);

            if (primary.Status != SolveStatus.Optimal) return Result(request, model, primary);

            var primaryValue = Dot(primaryCosts, primary.Values);

            var objectiveLimit = new ModelRow(
                new Dictionary<int, double> { [model.ScaleVariable] = 1 },
                -primaryValue,
                -primaryValue);

            if (Remaining(request.Options.TimeLimit, stopwatch.Elapsed) is { } duration && duration <= TimeSpan.Zero) {
                return Result(request, model, PrimaryFallback(primary, "The time limit expired before activity minimization."));
            }

            var secondary = Run(
                model.FactoryModel,
                model.FactoryModel.ActivityCosts(),
                objectiveLimit,
                request,
                stopwatch);

            return secondary.Status is SolveStatus.Optimal or SolveStatus.Feasible
                ? Result(request, model, secondary)
                : Result(request, model, PrimaryFallback(primary, "Activity minimization did not produce a usable solution."));
        } catch (Exception exception) {
            return new SolveResult(
                Status: SolveStatus.Failed,
                Graph: null,
                Diagnostics: [Diagnostic(SolverDiagnosticCodes.Failed, $"The capacity-driven solver failed: {exception.Message}")],
                Bottlenecks: []);
        }
    }

    private StageResult Run(
        LinearFactoryModel model,
        double[] costs,
        ModelRow? objectiveLimit,
        SolveRequest request,
        Stopwatch stopwatch) {
        var result = linearProgramSolver.Solve(
            model.Program,
            costs,
            objectiveLimit,
            Remaining(request.Options.TimeLimit, stopwatch.Elapsed),
            request.Options.NumericPolicy);

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
                "No solution satisfies the graph balances, target proportions, and capacities."),
            SolveStatus.Unbounded => Diagnostic(SolverDiagnosticCodes.Unbounded, "The planning objective is unbounded."),
            SolveStatus.Failed => Diagnostic(SolverDiagnosticCodes.Failed, $"The linear solver stopped with status '{result.Detail}'."),
            _ => null
        };

        return new StageResult(status, result.Values, diagnostic is null ? [] : [diagnostic]);
    }

    private SolveResult Result(SolveRequest request, CapacityDrivenModel model, StageResult stage) {
        if (stage.Status is not (SolveStatus.Optimal or SolveStatus.Feasible)) {
            return new SolveResult(stage.Status, null, stage.Diagnostics, []);
        }

        var graph = resultProjector.Project(request, model.FactoryModel, stage.Values);
        var bottlenecks = bottleneckReporter.Report(request, model.FactoryModel, stage.Values);

        return new SolveResult(stage.Status, graph, stage.Diagnostics, bottlenecks);
    }

    private static StageResult PrimaryFallback(StageResult primary, string message) {
        return primary with {
            Status = SolveStatus.Feasible,
            Diagnostics = [Diagnostic(SolverDiagnosticCodes.SecondaryOptimizationIncomplete, message, DiagnosticSeverity.Warning)]
        };
    }

    private static TimeSpan? Remaining(TimeSpan? limit, TimeSpan elapsed) => limit - elapsed;

    private static double Dot(IReadOnlyList<double> left, IReadOnlyList<double> right) {
        var result = 0d;
        for (var index = 0; index < left.Count; index++) result += left[index] * right[index];

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
