using System.Diagnostics;
using FactoryGraph.Contracts;

namespace FactoryGraph.Solving;

internal sealed class TargetDrivenSolver(
    LinearFactoryModelBuilder modelBuilder,
    ILinearProgramSolver linearProgramSolver,
    LinearFactoryResultProjector resultProjector) : IPlanningModeSolver {
    public PlanningMode Mode => PlanningMode.TargetDriven;

    public SolveResult Solve(SolveRequest request) {
        var stopwatch = Stopwatch.StartNew();

        try {
            var model = modelBuilder.Build(request).WithTargetMinimums(request.Targets);

            if (model.VariableCount == 0) {
                return new SolveResult(SolveStatus.Optimal, resultProjector.Project(request, model, []), [], []);
            }

            using var session = linearProgramSolver.CreateSession(model.Program, request.Options.NumericPolicy);
            var runner = new LexicographicStageRunner(
                session,
                request,
                stopwatch,
                "No solution satisfies the graph balances, targets, and capacities.");

            return Result(request, model, runner.Optimize(Stages(model, request.Objective)));
        } catch (Exception exception) {
            return new SolveResult(
                Status: SolveStatus.Failed,
                Graph: null,
                Diagnostics: [Diagnostic(SolverDiagnosticCodes.Failed, $"The target-driven solver failed: {exception.Message}")],
                Bottlenecks: []);
        }
    }

    private static List<CostStage> Stages(LinearFactoryModel model, SolveObjective objective) {
        var stages = new List<CostStage>();
        var primaryCosts = model.PrimaryCosts(objective);

        if (primaryCosts.Any(cost => cost != 0)) stages.Add(new CostStage("objective minimization", primaryCosts));
        if (model.FallbackSupplies.Count > 0) stages.Add(new CostStage("fallback supply minimization", model.FallbackSupplyCosts()));

        stages.Add(new CostStage("activity minimization", model.ActivityCosts()));

        return stages;
    }

    private SolveResult Result(SolveRequest request, LinearFactoryModel model, StageResult stage) {
        var graph = stage.Status is SolveStatus.Optimal or SolveStatus.Feasible
            ? resultProjector.Project(request, model, stage.Values)
            : null;

        return new SolveResult(stage.Status, graph, stage.Diagnostics, []);
    }

    private static SolveDiagnostic Diagnostic(string code, string message) =>
        new(code, DiagnosticSeverity.Error, message, []);
}
