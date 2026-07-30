using BenchmarkDotNet.Attributes;
using FactoryGraph.Contracts;
using FactoryGraph.Solving;
using FactoryGraph.Validation;

namespace FactoryGraph.Benchmarks;

[MemoryDiagnoser]
public class ProcessorStageBenchmarks {
    private BenchmarkState _state = BenchmarkState.Create(
        BenchmarkFamilies.AcyclicChain,
        PlanningMode.TargetDriven,
        8);

    [Params(BenchmarkFamilies.AcyclicChain, BenchmarkFamilies.ConstrainedChain)]
    public string Family { get; set; } = BenchmarkFamilies.AcyclicChain;

    [Params(PlanningMode.TargetDriven, PlanningMode.CapacityDriven)]
    public PlanningMode Mode { get; set; }

    [Params(8, 32, 128)]
    public int MachineGroups { get; set; }

    [GlobalSetup]
    public void Setup() {
        _state = BenchmarkState.Create(Family, Mode, MachineGroups);
    }

    [Benchmark]
    [BenchmarkCategory("Stage", "Validation")]
    public IReadOnlyList<SolveDiagnostic> Validation() {
        return _state.Validator.Validate(_state.Request);
    }

    [Benchmark]
    [BenchmarkCategory("Stage", "ModelConstruction")]
    public object ModelConstruction() {
        return _state.BuildModel();
    }

    [Benchmark]
    [BenchmarkCategory("Stage", "BackendSolve")]
    public object BackendSolve() {
        using var session = _state.LinearProgramSolver.CreateSession(
            _state.Model.Program,
            _state.Request.Options.NumericPolicy);

        return session.Solve(_state.Costs, null);
    }

    [Benchmark]
    [BenchmarkCategory("Stage", "ResultConstruction")]
    public SolveResult ResultConstruction() {
        var graph = _state.ResultProjector.Project(_state.Request, _state.Model, _state.Values);
        var bottlenecks = _state.Request.Mode == PlanningMode.CapacityDriven
            ? _state.BottleneckReporter.Report(_state.Request, _state.Model, _state.Values)
            : [];

        return new SolveResult(SolveStatus.Optimal, graph, [], bottlenecks);
    }

    [Benchmark]
    [BenchmarkCategory("EndToEnd")]
    public SolveResult EndToEnd() {
        return _state.Processor.Solve(_state.Request);
    }

    private sealed class BenchmarkState {
        private readonly LinearFactoryModelBuilder _modelBuilder = new();
        private readonly CapacityDrivenModelBuilder _capacityModelBuilder;

        private BenchmarkState() {
            _capacityModelBuilder = new CapacityDrivenModelBuilder(_modelBuilder);
        }

        public required SolveRequest Request { get; init; }

        public required SolveRequestValidator Validator { get; init; }

        public required HighsLinearProgramSolver LinearProgramSolver { get; init; }

        public required LinearFactoryResultProjector ResultProjector { get; init; }

        public required CapacityBottleneckReporter BottleneckReporter { get; init; }

        public required FactoryGraphProcessor Processor { get; init; }

        public required LinearFactoryModel Model { get; init; }

        public required double[] Costs { get; init; }

        public required double[] Values { get; init; }

        public object BuildModel() {
            return Request.Mode == PlanningMode.TargetDriven
                ? _modelBuilder.Build(Request).WithTargetMinimums(Request.Targets)
                : _capacityModelBuilder.Build(Request);
        }

        public static BenchmarkState Create(string family, PlanningMode mode, int machineGroupCount) {
            var request = BenchmarkRequestFactory.Create(family, mode, machineGroupCount);
            var validator = new SolveRequestValidator(
                new SolverSettingsValidator(),
                new FactoryGraphValidator(),
                new TargetValidator());

            var diagnostics = validator.Validate(request);

            if (diagnostics.Count > 0) {
                throw new InvalidOperationException(
                    $"Benchmark request is invalid: {string.Join(", ", diagnostics.Select(diagnostic => diagnostic.Code))}.");
            }

            var (model, costs) = CreateModel(request);
            var linearProgramSolver = new HighsLinearProgramSolver();
            using var session = linearProgramSolver.CreateSession(
                model.Program,
                request.Options.NumericPolicy);
            var solve = session.Solve(costs, null);

            if (solve.Status != LinearProgramStatus.Optimal) {
                throw new InvalidOperationException($"Benchmark model returned '{solve.Status}' during setup.");
            }

            var processor = FactoryGraphProcessorFactory.Create();
            var result = processor.Solve(request);

            if (result.Status != SolveStatus.Optimal) {
                throw new InvalidOperationException($"Benchmark request returned '{result.Status}' during setup.");
            }

            return new BenchmarkState {
                Request = request,
                Validator = validator,
                LinearProgramSolver = linearProgramSolver,
                ResultProjector = new LinearFactoryResultProjector(),
                BottleneckReporter = new CapacityBottleneckReporter(),
                Processor = processor,
                Model = model,
                Costs = costs,
                Values = solve.Values
            };
        }

        private static (LinearFactoryModel Model, double[] Costs) CreateModel(SolveRequest request) {
            var modelBuilder = new LinearFactoryModelBuilder();

            if (request.Mode == PlanningMode.TargetDriven) {
                var model = modelBuilder.Build(request).WithTargetMinimums(request.Targets);

                return (model, model.PrimaryCosts(request.Objective));
            }

            var capacityModel = new CapacityDrivenModelBuilder(modelBuilder).Build(request);

            return (capacityModel.FactoryModel, capacityModel.PrimaryCosts());
        }
    }
}
