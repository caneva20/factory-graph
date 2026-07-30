using BenchmarkDotNet.Attributes;
using FactoryGraph.Contracts;
using FactoryGraph.Solving;

namespace FactoryGraph.Benchmarks;

[MemoryDiagnoser]
public class LargeGraphBenchmarks {
    private const int MachineGroupCount = 4096;

    private SolveRequest _request;
    private CapacityDrivenModel _model;
    private HighsLinearProgramSolver _linearProgramSolver = new();
    private FactoryGraphProcessor _processor = FactoryGraphProcessorFactory.Create();

    public LargeGraphBenchmarks() {
        _request = BenchmarkRequestFactory.Create(
            BenchmarkFamilies.AcyclicChain,
            PlanningMode.CapacityDriven,
            MachineGroupCount);
        _model = new CapacityDrivenModelBuilder(new LinearFactoryModelBuilder()).Build(_request);
    }

    [Params(BenchmarkFamilies.AcyclicChain, BenchmarkFamilies.ConstrainedChain)]
    public string Family { get; set; } = BenchmarkFamilies.AcyclicChain;

    [GlobalSetup]
    public void Setup() {
        _request = BenchmarkRequestFactory.Create(Family, PlanningMode.CapacityDriven, MachineGroupCount);
        _model = new CapacityDrivenModelBuilder(new LinearFactoryModelBuilder()).Build(_request);
        _linearProgramSolver = new HighsLinearProgramSolver();
        _processor = FactoryGraphProcessorFactory.Create();

        var backendResult = SolveBackend();
        var processorResult = EndToEnd();

        if (backendResult.Status != LinearProgramStatus.Optimal || processorResult.Status != SolveStatus.Optimal) {
            throw new InvalidOperationException("The large-graph benchmark setup did not produce optimal results.");
        }
    }

    [Benchmark]
    [BenchmarkCategory("LargeGraph", "BackendSolve")]
    public object BackendSolve() {
        return SolveBackend();
    }

    private LinearProgramResult SolveBackend() {
        using var session = _linearProgramSolver.CreateSession(
            _model.FactoryModel.Program,
            _request.Options.NumericPolicy);

        return session.Solve(_model.PrimaryCosts(), null);
    }

    [Benchmark]
    [BenchmarkCategory("LargeGraph", "EndToEnd")]
    public SolveResult EndToEnd() {
        return _processor.Solve(_request);
    }
}
