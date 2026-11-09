using FactoryGraph.Contracts;
using FactoryGraph.Solving;

namespace FactoryGraph.Tests.Solving;

public sealed class CapacityDrivenSolverTests {
    [Fact]
    public void ChainMaximizesOutputAndReportsAllBindingCapacities() {
        var source = Id(1);
        var final = Id(2);
        var connection = Id(3);
        var supply = Id(4);
        var outlet = Id(5);

        var request = Request(
            items: ["raw", "part", "product"],
            recipes: [
                Recipe("source", [("raw", 1)], [("part", 1)]),
                Recipe("final", [("part", 1)], [("product", 1)])
            ],
            groups: [
                new MachineGroup(source, "source", 2, AvailableMachineCount: 3),
                new MachineGroup(final, "final", 1, ActivityCapacity: 6)
            ],
            connections: [new ItemConnection(connection, source, final, "part", 6)],
            supplies: [new Supply(supply, source, "raw", 6)],
            productOutlets: [new ProductOutlet(outlet, final, "product")],
            surplusOutlets: [],
            targets: [new Target(Id(6), outlet, 10)]);

        var result = Solve(request);

        Assert.Equal(SolveStatus.Optimal, result.Status);
        AssertClose(6, result.Graph!.Targets.Single().AchievedRate);
        AssertClose(4, result.Graph.Targets.Single().DeficitRate);

        Assert.Collection(
            result.Bottlenecks,
            bottleneck => AssertBottleneck(bottleneck, BottleneckKind.MachineGroup, source, 3),
            bottleneck => AssertBottleneck(bottleneck, BottleneckKind.MachineGroup, final, 6),
            bottleneck => AssertBottleneck(bottleneck, BottleneckKind.Connection, connection, 6),
            bottleneck => AssertBottleneck(bottleneck, BottleneckKind.Supply, supply, 6));
    }

    [Theory]
    [InlineData(10, 5)]
    [InlineData(2, 1)]
    [InlineData(20, 10)]
    public void EquivalentTargetRatiosProduceTheSamePhysicalPlan(double targetA, double targetB) {
        var machineA = Id(1);
        var machineB = Id(2);
        var outletA = Id(3);
        var outletB = Id(4);

        var request = Request(
            items: ["a", "b"],
            recipes: [Recipe("a", [], [("a", 1)]), Recipe("b", [], [("b", 1)])],
            groups: [
                new MachineGroup(machineA, "a", 1, ActivityCapacity: 6),
                new MachineGroup(machineB, "b", 1, ActivityCapacity: 3)
            ],
            connections: [],
            supplies: [],
            productOutlets: [
                new ProductOutlet(outletA, machineA, "a"),
                new ProductOutlet(outletB, machineB, "b")
            ],
            surplusOutlets: [],
            targets: [new Target(Id(5), outletA, targetA), new Target(Id(6), outletB, targetB)]);

        var result = Solve(request);

        Assert.Equal(SolveStatus.Optimal, result.Status);
        AssertClose(6, result.Graph!.Targets[0].AchievedRate);
        AssertClose(3, result.Graph.Targets[1].AchievedRate);
    }

    [Fact]
    public void OutputAboveExpectedRateIsReportedAsSurplus() {
        var machine = Id(1);
        var outlet = Id(2);

        var request = Request(
            items: ["product"],
            recipes: [Recipe("make", [], [("product", 1)])],
            groups: [new MachineGroup(machine, "make", 1, ActivityCapacity: 8)],
            connections: [],
            supplies: [],
            productOutlets: [new ProductOutlet(outlet, machine, "product")],
            surplusOutlets: [],
            targets: [new Target(Id(3), outlet, 5)]);

        var target = Solve(request).Graph!.Targets.Single();

        AssertClose(8, target.AchievedRate);
        AssertClose(3, target.SurplusRate);
        Assert.Equal(0, target.DeficitRate);
    }

    [Fact]
    public void ExactTargetMixUsesSurplusOutletForExtraCoproduct() {
        var machine = Id(1);
        var outletA = Id(2);
        var outletB = Id(3);
        var surplusB = Id(4);

        var request = Request(
            items: ["a", "b"],
            recipes: [Recipe("make", [], [("a", 1), ("b", 2)])],
            groups: [new MachineGroup(machine, "make", 1, ActivityCapacity: 5)],
            connections: [],
            supplies: [],
            productOutlets: [
                new ProductOutlet(outletA, machine, "a"),
                new ProductOutlet(outletB, machine, "b")
            ],
            surplusOutlets: [new SurplusOutlet(surplusB, machine, "b")],
            targets: [new Target(Id(5), outletA, 1), new Target(Id(6), outletB, 1)]);

        var result = Solve(request);

        AssertClose(5, result.Graph!.Targets[0].AchievedRate);
        AssertClose(5, result.Graph.Targets[1].AchievedRate);
        AssertClose(5, result.Graph.SurplusOutlets.Single().FlowRate);
    }

    [Fact]
    public void BranchedAssemblyMaximizesAcrossSplitsMergesAndCoproducts() {
        var source = Id(1);
        var left = Id(2);
        var right = Id(3);
        var assembly = Id(4);
        var outlet = Id(5);

        var request = Request(
            items: ["raw", "waste", "left", "right", "product"],
            recipes: [
                Recipe("source", [], [("raw", 2), ("waste", 1)]),
                Recipe("left", [("raw", 1)], [("left", 1)]),
                Recipe("right", [("raw", 1)], [("right", 1)]),
                Recipe("assembly", [("left", 1), ("right", 1)], [("product", 1)])
            ],
            groups: [
                new MachineGroup(source, "source", 1, ActivityCapacity: 4),
                new MachineGroup(left, "left", 1),
                new MachineGroup(right, "right", 1),
                new MachineGroup(assembly, "assembly", 1)
            ],
            connections: [
                new ItemConnection(Id(6), source, left, "raw"),
                new ItemConnection(Id(7), source, right, "raw"),
                new ItemConnection(Id(8), left, assembly, "left"),
                new ItemConnection(Id(9), right, assembly, "right")
            ],
            supplies: [],
            productOutlets: [new ProductOutlet(outlet, assembly, "product")],
            surplusOutlets: [new SurplusOutlet(Id(10), source, "waste")],
            targets: [new Target(Id(11), outlet, 10)]);

        var result = Solve(request);

        Assert.Equal(SolveStatus.Optimal, result.Status);
        AssertClose(4, result.Graph!.Targets.Single().AchievedRate);
        Assert.All(result.Graph.MachineGroups, group => AssertClose(4, group.Activity));
        AssertClose(4, result.Graph.SurplusOutlets.Single().FlowRate);
        AssertBottleneck(Assert.Single(result.Bottlenecks), BottleneckKind.MachineGroup, source, 4);
    }

    [Fact]
    public void DisconnectedCapacityDoesNotCreateActivityOrBottleneck() {
        var targetMachine = Id(1);
        var disconnected = Id(2);
        var outlet = Id(3);

        var request = Request(
            items: ["product", "unused"],
            recipes: [Recipe("make", [], [("product", 1)]), Recipe("unused", [], [("unused", 1)])],
            groups: [
                new MachineGroup(targetMachine, "make", 1, ActivityCapacity: 4),
                new MachineGroup(disconnected, "unused", 1, ActivityCapacity: 0)
            ],
            connections: [],
            supplies: [],
            productOutlets: [
                new ProductOutlet(outlet, targetMachine, "product"),
                new ProductOutlet(Id(4), disconnected, "unused")
            ],
            surplusOutlets: [],
            targets: [new Target(Id(5), outlet, 1)]);

        var result = Solve(request);

        Assert.Equal(0, result.Graph!.MachineGroups.Single(group => group.Id == disconnected).Activity);
        Assert.DoesNotContain(result.Bottlenecks, bottleneck => bottleneck.Element.Id == disconnected.ToString("D"));
    }

    [Fact]
    public void UnlimitedSourceReportsUnboundedObjective() {
        var machine = Id(1);
        var outlet = Id(2);

        var request = Request(
            items: ["product"],
            recipes: [Recipe("make", [], [("product", 1)])],
            groups: [new MachineGroup(machine, "make", 1)],
            connections: [],
            supplies: [],
            productOutlets: [new ProductOutlet(outlet, machine, "product")],
            surplusOutlets: [],
            targets: [new Target(Id(3), outlet, 1)]);

        var result = Solve(request);

        Assert.Equal(SolveStatus.Unbounded, result.Status);
        Assert.Null(result.Graph);
        Assert.Equal(SolverDiagnosticCodes.Unbounded, Assert.Single(result.Diagnostics).Code);
    }

    [Fact]
    public void SecondaryFailureReturnsThePrimarySolution() {
        var machine = Id(1);
        var outlet = Id(2);

        var request = Request(
            items: ["product"],
            recipes: [Recipe("make", [], [("product", 1)])],
            groups: [new MachineGroup(machine, "make", 1, ActivityCapacity: 1)],
            connections: [],
            supplies: [],
            productOutlets: [new ProductOutlet(outlet, machine, "product")],
            surplusOutlets: [],
            targets: [new Target(Id(3), outlet, 1)]);

        var backend = new SequenceLinearProgramSolver(
            new LinearProgramResult(LinearProgramStatus.Optimal, [1, 1, 1], "primary optimal"),
            new LinearProgramResult(LinearProgramStatus.Failed, [], "secondary failure"));

        var solver = new CapacityDrivenSolver(
            new CapacityDrivenModelBuilder(new LinearFactoryModelBuilder()),
            backend,
            new LinearFactoryResultProjector(),
            new CapacityBottleneckReporter());

        var result = solver.Solve(request);

        Assert.Equal(SolveStatus.Feasible, result.Status);
        AssertClose(1, result.Graph!.Targets.Single().AchievedRate);
        Assert.Equal(SolverDiagnosticCodes.SecondaryOptimizationIncomplete, Assert.Single(result.Diagnostics).Code);
        Assert.Equal(2, backend.CallCount);
        Assert.Equal(1, backend.SessionCount);
        Assert.Equal(1, backend.AddedRowCount);
    }

    [Fact]
    public void FallbackSupplyCoversOnlyWhatExplicitSuppliesCannot() {
        var machine = Id(1);
        var fallback = Id(2);
        var explicitSupply = Id(3);
        var outlet = Id(4);

        var request = Request(
            items: ["raw", "product"],
            recipes: [Recipe("make", [("raw", 1)], [("product", 1)])],
            groups: [new MachineGroup(machine, "make", 1, ActivityCapacity: 6)],
            connections: [],
            supplies: [new Supply(fallback, machine, "raw", IsFallback: true), new Supply(explicitSupply, machine, "raw", 4)],
            productOutlets: [new ProductOutlet(outlet, machine, "product")],
            surplusOutlets: [],
            targets: [new Target(Id(5), outlet, 10)]);

        var result = Solve(request);

        Assert.Equal(SolveStatus.Optimal, result.Status);
        AssertClose(6, result.Graph!.Targets.Single().AchievedRate);
        AssertClose(4, result.Graph.Supplies.Single(supply => supply.Id == explicitSupply).ProvidedRate);
        AssertClose(2, result.Graph.Supplies.Single(supply => supply.Id == fallback).ProvidedRate);
    }

    private static SolveResult Solve(SolveRequest request) => FactoryGraphProcessorFactory.Create().Solve(request);

    private static SolveRequest Request(
        IReadOnlyList<string> items,
        IReadOnlyList<Recipe> recipes,
        IReadOnlyList<MachineGroup> groups,
        IReadOnlyList<ItemConnection> connections,
        IReadOnlyList<Supply> supplies,
        IReadOnlyList<ProductOutlet> productOutlets,
        IReadOnlyList<SurplusOutlet> surplusOutlets,
        IReadOnlyList<Target> targets) {
        return new SolveRequest(
            new FactoryGraphDefinition(Items(items.ToArray()), recipes, groups, connections, supplies, productOutlets, surplusOutlets),
            targets,
            PlanningMode.CapacityDriven,
            SolveObjective.MaximizeTargetOutput,
            new SolverOptions(new NumericPolicy()));
    }

    private static Item[] Items(params string[] ids) => ids.Select(id => new Item(id, id)).ToArray();

    private static Recipe Recipe(
        string id,
        IReadOnlyList<(string Item, double Amount)> inputs,
        IReadOnlyList<(string Item, double Amount)> outputs) {
        return new Recipe(
            id,
            id,
            inputs.Select(item => new RecipeItem(item.Item, item.Amount)).ToArray(),
            outputs.Select(item => new RecipeItem(item.Item, item.Amount)).ToArray());
    }

    private static void AssertBottleneck(Bottleneck bottleneck, BottleneckKind kind, Guid id, double capacity) {
        Assert.Equal(kind, bottleneck.Kind);
        Assert.Equal(id.ToString("D"), bottleneck.Element.Id);
        Assert.Equal(capacity, bottleneck.Capacity);
        AssertClose(1, bottleneck.Utilization);
    }

    private static void AssertClose(double expected, double actual) => Assert.Equal(expected, actual, 7);

    private static Guid Id(int value) => Guid.Parse($"{value:x8}-0000-0000-0000-000000000000");

    private sealed class SequenceLinearProgramSolver(params LinearProgramResult[] results)
        : ILinearProgramSolver, ILinearProgramSession {
        private readonly Queue<LinearProgramResult> _results = new(results);

        public int CallCount { get; private set; }

        public int SessionCount { get; private set; }

        public int AddedRowCount { get; private set; }

        public ILinearProgramSession CreateSession(LinearProgram program, NumericPolicy numericPolicy) {
            SessionCount++;

            return this;
        }

        public LinearProgramResult Solve(
            IReadOnlyList<double> costs,
            TimeSpan? timeLimit) {
            CallCount++;

            return _results.Dequeue();
        }

        public LinearProgramResult AddRowAndSolve(
            IReadOnlyList<double> costs,
            ModelRow additionalRow,
            TimeSpan? timeLimit) {
            AddedRowCount++;

            return Solve(costs, timeLimit);
        }

        public void Dispose() { }
    }
}
