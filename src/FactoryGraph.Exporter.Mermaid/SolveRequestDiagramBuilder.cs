using FactoryGraph.Contracts;
using static FactoryGraph.Exporter.Mermaid.MermaidText;

namespace FactoryGraph.Exporter.Mermaid;

internal static class SolveRequestDiagramBuilder {
    public static MermaidDiagram Build(SolveRequest request, MermaidExportOptions options) {
        var graph = request.Graph;
        var items = graph.Items.ToDictionary(item => item.Id, StringComparer.Ordinal);
        var recipes = graph.Recipes.ToDictionary(recipe => recipe.Id, StringComparer.Ordinal);
        var nodes = new List<MermaidNode>();
        var edges = new List<MermaidEdge>();

        foreach (var group in graph.MachineGroups) {
            var label = new List<string> {
                recipes[group.RecipeId].Name,
                $"activity/machine: {Number(group.ActivityRatePerMachine, options)}"
            };

            if (options.IncludeCapacities && group.AvailableMachineCount is { } machineCount) {
                label.Add($"machines available: {machineCount}");
            }

            if (options.IncludeCapacities && group.ActivityCapacity is { } activityCapacity) {
                label.Add($"activity capacity: {Number(activityCapacity, options)}");
            }

            nodes.Add(new MermaidNode(0, Id("machine", group.Id), Label(label), "machine"));
        }

        foreach (var supply in graph.Supplies) {
            nodes.Add(new MermaidNode(
                1,
                Id("supply", supply.Id),
                Label("Supply", items[supply.ItemId].Name),
                "supply"));

            edges.Add(new MermaidEdge(
                0,
                Id("supply", supply.Id),
                Id("supply", supply.Id),
                Id("machine", supply.DestinationMachineGroupId),
                WithCapacity(items[supply.ItemId].Name, supply.Capacity, options)));
        }

        foreach (var outlet in graph.ProductOutlets) {
            nodes.Add(new MermaidNode(
                2,
                Id("product", outlet.Id),
                Label("Product", items[outlet.ItemId].Name),
                "product"));

            edges.Add(new MermaidEdge(
                2,
                Id("product", outlet.Id),
                Id("machine", outlet.SourceMachineGroupId),
                Id("product", outlet.Id),
                WithCapacity(items[outlet.ItemId].Name, outlet.Capacity, options)));
        }

        foreach (var outlet in graph.SurplusOutlets) {
            nodes.Add(new MermaidNode(
                3,
                Id("surplus", outlet.Id),
                Label("Surplus", items[outlet.ItemId].Name),
                "surplus"));

            edges.Add(new MermaidEdge(
                3,
                Id("surplus", outlet.Id),
                Id("machine", outlet.SourceMachineGroupId),
                Id("surplus", outlet.Id),
                WithCapacity(items[outlet.ItemId].Name, outlet.Capacity, options)));
        }

        foreach (var connection in graph.Connections) {
            edges.Add(new MermaidEdge(
                1,
                Id("connection", connection.Id),
                Id("machine", connection.SourceMachineGroupId),
                Id("machine", connection.DestinationMachineGroupId),
                WithCapacity(items[connection.ItemId].Name, connection.Capacity, options)));
        }

        foreach (var target in request.Targets) {
            var outlet = graph.ProductOutlets.Single(value => value.Id == target.ProductOutletId);

            nodes.Add(new MermaidNode(
                4,
                Id("target", target.Id),
                Label("Target", items[outlet.ItemId].Name, $"requested: {Number(target.Rate, options)}"),
                "target"));

            edges.Add(new MermaidEdge(
                4,
                Id("target", target.Id),
                Id("target", target.Id),
                Id("product", target.ProductOutletId),
                string.Empty,
                IsDotted: true));
        }

        return new MermaidDiagram(nodes, edges);
    }
}
