using FactoryGraph;
using FactoryGraph.Contracts;
using FactoryGraph.Exporter.Mermaid;

var machineGroupId = Guid.NewGuid();
var supplyId = Guid.NewGuid();
var productOutletId = Guid.NewGuid();

var graph = new FactoryGraphDefinition(
    Items:
    [
        new Item("raw", "Raw material"),
        new Item("product", "Product")
    ],
    Recipes:
    [
        new Recipe(
            "produce",
            "Produce",
            Inputs: [new RecipeItem("raw", 1)],
            Outputs: [new RecipeItem("product", 1)])
    ],
    MachineGroups: [new MachineGroup(machineGroupId, "produce", ActivityRatePerMachine: 1)],
    Connections: [],
    Supplies: [new Supply(supplyId, [machineGroupId], "raw")],
    ProductOutlets: [new ProductOutlet(productOutletId, machineGroupId, "product")],
    SurplusOutlets: []);

var request = new SolveRequest(
    graph,
    Targets: [new Target(Guid.NewGuid(), [productOutletId], Rate: 2)],
    Mode: PlanningMode.TargetDriven,
    Objective: SolveObjective.MeetTargetsMinimizeExternalSupply,
    Options: new SolverOptions(new NumericPolicy()));

var result = FactoryGraphProcessorFactory.Create().Solve(request);
if (result.Status != SolveStatus.Optimal || result.Graph is null) {
    throw new InvalidOperationException($"Package solve returned {result.Status}.");
}

var deliveredRate = result.Graph.ProductOutlets.Single().DeliveredRate;
if (Math.Abs(deliveredRate - 2) > 1e-9) {
    throw new InvalidOperationException($"Package solve delivered {deliveredRate} instead of 2.");
}

var diagram = new MermaidExporter().Export(result);
if (!diagram.StartsWith("flowchart LR", StringComparison.Ordinal)) {
    throw new InvalidOperationException("Package exporter did not produce a Mermaid flowchart.");
}

Console.WriteLine("Package consumer solve and export succeeded.");
