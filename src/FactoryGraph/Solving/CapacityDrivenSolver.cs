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
            using var session = linearProgramSolver.CreateSession(model.FactoryModel.Program, request.Options.NumericPolicy);
            var runner = new LexicographicStageRunner(
                session,
                request,
                stopwatch,
                "No solution satisfies the graph balances, target proportions, and capacities.");

            return Result(request, model, runner.Optimize(Stages(model)));
        } catch (Exception exception) {
            return new SolveResult(
                Status: SolveStatus.Failed,
                Graph: null,
                Diagnostics: [Diagnostic(SolverDiagnosticCodes.Failed, $"The capacity-driven solver failed: {exception.Message}")],
                Bottlenecks: []);
        }
    }

    private static List<CostStage> Stages(CapacityDrivenModel model) {
        var stages = new List<CostStage> {
            new("target scale maximization", model.PrimaryCosts(), value => new ModelRow(
                new Dictionary<int, double> { [model.ScaleVariable] = 1 },
                -value,
                -value))
        };

        if (model.FactoryModel.FallbackSupplies.Count > 0) {
            stages.Add(new CostStage("fallback supply minimization", model.FactoryModel.FallbackSupplyCosts()));
        }

        stages.Add(new CostStage("activity minimization", model.FactoryModel.ActivityCosts()));

        return stages;
    }

    private SolveResult Result(SolveRequest request, CapacityDrivenModel model, StageResult stage) {
        if (stage.Status is not (SolveStatus.Optimal or SolveStatus.Feasible)) {
            return new SolveResult(stage.Status, null, stage.Diagnostics, []);
        }

        var graph = resultProjector.Project(request, model.FactoryModel, stage.Values);
        var bottlenecks = bottleneckReporter.Report(request, model.FactoryModel, stage.Values);

        return new SolveResult(stage.Status, graph, stage.Diagnostics, bottlenecks);
    }

    private static SolveDiagnostic Diagnostic(string code, string message) =>
        new(code, DiagnosticSeverity.Error, message, []);
}
