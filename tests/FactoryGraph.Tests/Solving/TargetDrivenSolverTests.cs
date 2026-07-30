using FactoryGraph.Contracts;
using FactoryGraph.Solving;

namespace FactoryGraph.Tests.Solving;

public sealed class TargetDrivenSolverTests {
    [Fact]
    public void FanInBalancesMultipleInputs() {
        var sourceA = Id(1);
        var sourceB = Id(2);
        var merge = Id(3);
        var productOutlet = Id(4);

        var request = Request(
            items: ["raw-a", "raw-b", "part-a", "part-b", "product"],
            recipes: [
                Recipe("a", [("raw-a", 1)], [("part-a", 2)]),
                Recipe("b", [("raw-b", 3)], [("part-b", 1)]),
                Recipe("merge", [("part-a", 2), ("part-b", 1)], [("product", 1)])
            ],
            groups: [Group(sourceA, "a"), Group(sourceB, "b"), Group(merge, "merge")],
            connections: [
                new ItemConnection(Id(10), sourceA, merge, "part-a"),
                new ItemConnection(Id(11), sourceB, merge, "part-b")
            ],
            supplies: [
                new Supply(Id(20), sourceA, "raw-a"),
                new Supply(Id(21), sourceB, "raw-b")
            ],
            productOutlets: [new ProductOutlet(productOutlet, merge, "product")],
            surplusOutlets: [],
            targets: [new Target(Id(30), productOutlet, 5)]);

        var result = FactoryGraphProcessorFactory.Create().Solve(request);

        Assert.Equal(SolveStatus.Optimal, result.Status);
        Assert.Equal(5, Machine(result, sourceA).Activity);
        Assert.Equal(5, Machine(result, sourceB).Activity);
        Assert.Equal(5, Machine(result, merge).Activity);
        Assert.Equal([10d, 5d], result.Graph!.Connections.Select(connection => connection.FlowRate));
        Assert.Equal([5d, 15d], result.Graph.Supplies.Select(supply => supply.ProvidedRate));
    }

    [Fact]
    public void FanOutMeetsMultipleTargetsAndLeavesDisconnectedGroupInactive() {
        var source = Id(1);
        var consumerA = Id(2);
        var consumerB = Id(3);
        var disconnected = Id(4);
        var productOutletA = Id(5);
        var productOutletB = Id(6);

        var request = Request(
            items: ["raw", "shared", "a", "b", "unused"],
            recipes: [
                Recipe("source", [("raw", 1)], [("shared", 1)]),
                Recipe("a", [("shared", 1)], [("a", 1)]),
                Recipe("b", [("shared", 1)], [("b", 1)]),
                Recipe("unused", [], [("unused", 1)])
            ],
            groups: [
                Group(source, "source"),
                Group(consumerA, "a"),
                Group(consumerB, "b"),
                Group(disconnected, "unused")
            ],
            connections: [
                new ItemConnection(Id(10), source, consumerA, "shared"),
                new ItemConnection(Id(11), source, consumerB, "shared")
            ],
            supplies: [new Supply(Id(20), source, "raw")],
            productOutlets: [
                new ProductOutlet(productOutletA, consumerA, "a"),
                new ProductOutlet(productOutletB, consumerB, "b"),
                new ProductOutlet(Id(7), disconnected, "unused")
            ],
            surplusOutlets: [],
            targets: [new Target(Id(30), productOutletA, 3), new Target(Id(31), productOutletB, 4)]);

        var result = FactoryGraphProcessorFactory.Create().Solve(request);

        Assert.Equal(SolveStatus.Optimal, result.Status);
        Assert.Equal(7, Machine(result, source).Activity);
        Assert.Equal([3d, 4d], result.Graph!.Connections.Select(connection => connection.FlowRate));
        Assert.Equal(0, Machine(result, disconnected).Activity);
        Assert.Equal(0, Machine(result, disconnected).RequiredWholeMachineCount);
        Assert.Equal([3d, 4d], result.Graph.Targets.Select(target => target.AchievedRate));
    }

    [Fact]
    public void ObjectiveSelectsSupplyOrSurplusTradeoff() {
        var producerA = Id(1);
        var producerB = Id(2);
        var final = Id(3);
        var productOutlet = Id(4);

        var graph = new FactoryGraphDefinition(
            Items("raw", "intermediate", "waste", "product"),
            [
                Recipe("cheap", [("raw", 1)], [("intermediate", 1), ("waste", 10)]),
                Recipe("clean", [("raw", 2)], [("intermediate", 1)]),
                Recipe("final", [("intermediate", 1)], [("product", 1)])
            ],
            [Group(producerA, "cheap"), Group(producerB, "clean"), Group(final, "final")],
            [
                new ItemConnection(Id(10), producerA, final, "intermediate"),
                new ItemConnection(Id(11), producerB, final, "intermediate")
            ],
            [new Supply(Id(20), producerA, "raw"), new Supply(Id(21), producerB, "raw")],
            [new ProductOutlet(productOutlet, final, "product")],
            [new SurplusOutlet(Id(22), producerA, "waste")]);

        var target = new Target(Id(30), productOutlet, 1);

        var minimizeSupply = FactoryGraphProcessorFactory.Create().Solve(new SolveRequest(
            graph, [target], PlanningMode.TargetDriven, SolveObjective.MeetTargetsMinimizeExternalSupply, Options()));

        var minimizeSurplus = FactoryGraphProcessorFactory.Create().Solve(new SolveRequest(
            graph, [target], PlanningMode.TargetDriven, SolveObjective.MeetTargetsMinimizeSurplus, Options()));

        Assert.Equal([1d, 0d], minimizeSupply.Graph!.Supplies.Select(supply => supply.ProvidedRate));
        Assert.Equal(10, minimizeSupply.Graph.SurplusOutlets.Single().FlowRate);
        Assert.Equal([0d, 2d], minimizeSurplus.Graph!.Supplies.Select(supply => supply.ProvidedRate));
        Assert.Equal(0, minimizeSurplus.Graph.SurplusOutlets.Single().FlowRate);
    }

    [Fact]
    public void CoProductReportsSurplusAgainstEachDistinctTarget() {
        var machine = Id(1);
        var firstProductOutlet = Id(2);
        var secondProductOutlet = Id(3);

        var request = Request(
            items: ["raw", "a", "b"],
            recipes: [Recipe("co-product", [("raw", 1)], [("a", 1), ("b", 1)])],
            groups: [Group(machine, "co-product")],
            connections: [],
            supplies: [new Supply(Id(10), machine, "raw")],
            productOutlets: [
                new ProductOutlet(firstProductOutlet, machine, "a"),
                new ProductOutlet(secondProductOutlet, machine, "b")
            ],
            surplusOutlets: [],
            targets: [new Target(Id(20), firstProductOutlet, 5), new Target(Id(21), secondProductOutlet, 2)]);

        var result = FactoryGraphProcessorFactory.Create().Solve(request);

        Assert.Equal(SolveStatus.Optimal, result.Status);
        Assert.Equal([5d, 5d], result.Graph!.Targets.Select(target => target.AchievedRate));
        Assert.Equal([0d, 3d], result.Graph.Targets.Select(target => target.SurplusRate));
        Assert.All(result.Graph.Targets, target => Assert.Equal(0, target.DeficitRate));
    }

    [Fact]
    public void ReadmeExampleProducesDocumentedPlan() {
        var smelters = Id(1);
        var assemblers = Id(2);
        var productOutlet = Id(3);

        var request = Request(
            items: ["ore", "plate", "gear", "slag"],
            recipes: [
                Recipe("smelt", [("ore", 1)], [("plate", 1), ("slag", 0.25)]),
                Recipe("assemble-gear", [("plate", 2)], [("gear", 1)])
            ],
            groups: [
                new MachineGroup(smelters, "smelt", 2, AvailableMachineCount: 4, ActivityCapacity: 7),
                new MachineGroup(assemblers, "assemble-gear", 2, AvailableMachineCount: 2)
            ],
            connections: [new ItemConnection(Id(10), smelters, assemblers, "plate")],
            supplies: [new Supply(Id(20), smelters, "ore", Capacity: 10)],
            productOutlets: [new ProductOutlet(productOutlet, assemblers, "gear", Capacity: 5)],
            surplusOutlets: [new SurplusOutlet(Id(21), smelters, "slag", Capacity: 2)],
            targets: [new Target(Id(30), productOutlet, 3)]);

        var result = FactoryGraphProcessorFactory.Create().Solve(request);

        Assert.Equal(SolveStatus.Optimal, result.Status);
        Assert.Equal((6d, 3d, 3, 1d), Counts(Machine(result, smelters)));
        Assert.Equal((3d, 1.5, 2, 0.75), Counts(Machine(result, assemblers)));
        Assert.Equal(6, result.Graph!.Supplies.Single().ProvidedRate);
        Assert.Equal(6, result.Graph.Connections.Single().FlowRate);
        Assert.Equal(3, result.Graph.ProductOutlets.Single().DeliveredRate);
        Assert.Equal(1.5, result.Graph.SurplusOutlets.Single().FlowRate);
    }

    [Theory]
    [InlineData("machine")]
    [InlineData("machine-count")]
    [InlineData("connection")]
    [InlineData("supply")]
    [InlineData("product-outlet")]
    [InlineData("surplus-outlet")]
    public void CapacityBelowRequiredRateIsInfeasible(string limitedElement) {
        var source = Id(1);
        var final = Id(2);
        var productOutlet = Id(3);

        var group = Group(source, "source") with {
            AvailableMachineCount = limitedElement == "machine-count" ? 4 : null,
            ActivityCapacity = limitedElement == "machine" ? 4 : null
        };

        var request = Request(
            items: ["raw", "part", "waste", "product"],
            recipes: [
                Recipe("source", [("raw", 1)], [("part", 1), ("waste", 1)]),
                Recipe("final", [("part", 1)], [("product", 1)])
            ],
            groups: [group, Group(final, "final")],
            connections: [new ItemConnection(Id(10), source, final, "part", limitedElement == "connection" ? 4 : null)],
            supplies: [new Supply(Id(20), source, "raw", limitedElement == "supply" ? 4 : null)],
            productOutlets: [new ProductOutlet(productOutlet, final, "product", limitedElement == "product-outlet" ? 4 : null)],
            surplusOutlets: [new SurplusOutlet(Id(21), source, "waste", limitedElement == "surplus-outlet" ? 4 : null)],
            targets: [new Target(Id(30), productOutlet, 5)]);

        var result = FactoryGraphProcessorFactory.Create().Solve(request);

        Assert.Equal(SolveStatus.Infeasible, result.Status);
        Assert.Null(result.Graph);
        Assert.Equal(SolverDiagnosticCodes.Infeasible, Assert.Single(result.Diagnostics).Code);
    }

    [Fact]
    public void EmptyGraphProducesAnEmptyOptimalResult() {
        var request = Request([], [], [], [], [], [], [], []);

        var result = FactoryGraphProcessorFactory.Create().Solve(request);

        Assert.Equal(SolveStatus.Optimal, result.Status);
        Assert.NotNull(result.Graph);
        Assert.Empty(result.Graph.MachineGroups);
    }

    [Fact]
    public void SourceRecipeWithoutExternalSupplyMeetsTarget() {
        var machine = Id(1);
        var productOutlet = Id(2);

        var request = Request(
            items: ["product"],
            recipes: [Recipe("extract", [], [("product", 2)])],
            groups: [Group(machine, "extract")],
            connections: [],
            supplies: [],
            productOutlets: [new ProductOutlet(productOutlet, machine, "product")],
            surplusOutlets: [],
            targets: [new Target(Id(3), productOutlet, 6)]);

        var result = FactoryGraphProcessorFactory.Create().Solve(request);

        Assert.Equal(SolveStatus.Optimal, result.Status);
        Assert.Equal(3, Machine(result, machine).Activity);
        Assert.Equal(6, result.Graph!.ProductOutlets.Single().DeliveredRate);
    }

    [Fact]
    public void SecondaryFailureReturnsThePrimarySolution() {
        var machine = Id(1);
        var productOutlet = Id(2);

        var request = Request(
            items: ["raw", "product"],
            recipes: [Recipe("make", [("raw", 1)], [("product", 1)])],
            groups: [Group(machine, "make")],
            connections: [],
            supplies: [new Supply(Id(3), machine, "raw")],
            productOutlets: [new ProductOutlet(productOutlet, machine, "product")],
            surplusOutlets: [],
            targets: [new Target(Id(4), productOutlet, 1)]);

        var backend = new SequenceLinearProgramSolver(
            new LinearProgramResult(LinearProgramStatus.Optimal, [1, 1, 1], "primary optimal"),
            new LinearProgramResult(LinearProgramStatus.Failed, [], "secondary failure"));

        var solver = new TargetDrivenSolver(
            new LinearFactoryModelBuilder(),
            backend,
            new LinearFactoryResultProjector());

        var result = solver.Solve(request);

        Assert.Equal(SolveStatus.Feasible, result.Status);
        Assert.Equal(1, result.Graph!.ProductOutlets.Single().DeliveredRate);
        Assert.Equal(SolverDiagnosticCodes.SecondaryOptimizationIncomplete, Assert.Single(result.Diagnostics).Code);
        Assert.Equal(2, backend.CallCount);
    }

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
            PlanningMode.TargetDriven,
            SolveObjective.MeetTargetsMinimizeExternalSupply,
            Options());
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

    private static MachineGroup Group(Guid id, string recipe) => new(id, recipe, 1);

    private static SolverOptions Options() => new(new NumericPolicy());

    private static Guid Id(int value) => Guid.Parse($"{value:x8}-0000-0000-0000-000000000000");

    private static ComputedMachineGroup Machine(SolveResult result, Guid id) =>
        result.Graph!.MachineGroups.Single(group => group.Id == id);

    private static (double Activity, double Effective, int Whole, double Utilization) Counts(
        ComputedMachineGroup group) =>
        (group.Activity, group.EffectiveMachineCount, group.RequiredWholeMachineCount, group.Utilization);

    private sealed class SequenceLinearProgramSolver(params LinearProgramResult[] results) : ILinearProgramSolver {
        private readonly Queue<LinearProgramResult> _results = new(results);

        public int CallCount { get; private set; }

        public LinearProgramResult Solve(
            LinearProgram program,
            IReadOnlyList<double> costs,
            ModelRow? additionalRow,
            TimeSpan? timeLimit,
            NumericPolicy numericPolicy) {
            CallCount++;

            return _results.Dequeue();
        }
    }
}
