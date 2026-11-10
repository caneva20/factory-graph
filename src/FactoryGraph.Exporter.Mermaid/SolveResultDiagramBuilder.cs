using FactoryGraph.Contracts;
using static FactoryGraph.Exporter.Mermaid.MermaidText;

namespace FactoryGraph.Exporter.Mermaid;

internal static class SolveResultDiagramBuilder {
    public static MermaidDiagram Build(
        ComputedFactoryGraph graph,
        IReadOnlyList<Bottleneck> resultBottlenecks,
        MermaidExportOptions options) {
        var items = graph.Items.ToDictionary(item => item.Id, StringComparer.Ordinal);
        var recipes = graph.Recipes.ToDictionary(recipe => recipe.Id, StringComparer.Ordinal);
        var bottlenecks = BottleneckIds.Create(resultBottlenecks);
        var nodes = new List<MermaidNode>();
        var edges = new List<MermaidEdge>();

        foreach (var group in graph.MachineGroups) {
            var label = new List<string> {
                recipes[group.RecipeId].Name,
                $"activity: {Number(group.Activity, options)}",
                $"machines: {Number(group.EffectiveMachineCount, options)} / {group.RequiredWholeMachineCount}",
                $"utilization: {Percentage(group.Utilization, options)}"
            };

            if (options.IncludeCapacities && group.AvailableMachineCount is { } machineCount) {
                label.Add($"machines available: {machineCount}");
            }

            if (options.IncludeCapacities && group.ActivityCapacity is { } activityCapacity) {
                label.Add($"activity capacity: {Number(activityCapacity, options)}");
            }

            nodes.Add(new MermaidNode(
                0,
                Id("machine", group.Id),
                Label(label),
                "machine",
                bottlenecks.MachineGroups.Contains(group.Id)));
        }

        foreach (var supply in graph.Supplies) {
            nodes.Add(new MermaidNode(
                1,
                Id("supply", supply.Id),
                Label(
                    "Supply",
                    items[supply.ItemId].Name,
                    $"required: {Number(supply.RequiredRate, options)}",
                    $"provided: {Number(supply.ProvidedRate, options)}",
                    $"deficit: {Number(supply.DeficitRate, options)}"),
                "supply",
                bottlenecks.Supplies.Contains(supply.Id)));

            var isShared = supply.Destinations.Count > 1;

            foreach (var destination in supply.Destinations) {
                edges.Add(new MermaidEdge(
                    0,
                    $"{Id("supply", supply.Id)}_{destination.MachineGroupId:N}",
                    Id("supply", supply.Id),
                    Id("machine", destination.MachineGroupId),
                    WithCapacity(
                        $"{items[supply.ItemId].Name}: {Number(destination.FlowRate, options)}",
                        isShared ? null : supply.Capacity,
                        options)));
            }
        }

        foreach (var outlet in graph.ProductOutlets) {
            nodes.Add(new MermaidNode(
                2,
                Id("product", outlet.Id),
                Label(
                    "Product",
                    items[outlet.ItemId].Name,
                    $"delivered: {Number(outlet.DeliveredRate, options)}"),
                "product"));

            edges.Add(new MermaidEdge(
                2,
                Id("product", outlet.Id),
                Id("machine", outlet.SourceMachineGroupId),
                Id("product", outlet.Id),
                WithCapacity(
                    $"{items[outlet.ItemId].Name}: {Number(outlet.DeliveredRate, options)}",
                    outlet.Capacity,
                    options)));
        }

        foreach (var outlet in graph.SurplusOutlets) {
            nodes.Add(new MermaidNode(
                3,
                Id("surplus", outlet.Id),
                Label(
                    "Surplus",
                    items[outlet.ItemId].Name,
                    $"flow: {Number(outlet.FlowRate, options)}"),
                "surplus"));

            edges.Add(new MermaidEdge(
                3,
                Id("surplus", outlet.Id),
                Id("machine", outlet.SourceMachineGroupId),
                Id("surplus", outlet.Id),
                WithCapacity(
                    $"{items[outlet.ItemId].Name}: {Number(outlet.FlowRate, options)}",
                    outlet.Capacity,
                    options)));
        }

        foreach (var connection in graph.Connections) {
            edges.Add(new MermaidEdge(
                1,
                Id("connection", connection.Id),
                Id("machine", connection.SourceMachineGroupId),
                Id("machine", connection.DestinationMachineGroupId),
                WithCapacity(
                    $"{items[connection.ItemId].Name}: {Number(connection.FlowRate, options)}",
                    connection.Capacity,
                    options),
                IsBottleneck: bottlenecks.Connections.Contains(connection.Id)));
        }

        foreach (var target in graph.Targets) {
            var outlet = graph.ProductOutlets.Single(value => value.Id == target.ProductOutletIds[0]);

            nodes.Add(new MermaidNode(
                4,
                Id("target", target.Id),
                Label(
                    "Target",
                    items[outlet.ItemId].Name,
                    $"requested: {Number(target.RequestedRate, options)}",
                    $"achieved: {Number(target.AchievedRate, options)}",
                    $"deficit: {Number(target.DeficitRate, options)}",
                    $"surplus: {Number(target.SurplusRate, options)}"),
                "target"));

            foreach (var outletId in target.ProductOutletIds) {
                edges.Add(new MermaidEdge(
                    4,
                    $"{Id("target", target.Id)}_{outletId:N}",
                    Id("target", target.Id),
                    Id("product", outletId),
                    string.Empty,
                    IsDotted: true));
            }
        }

        return new MermaidDiagram(nodes, edges);
    }

    private sealed record BottleneckIds(
        IReadOnlySet<Guid> MachineGroups,
        IReadOnlySet<Guid> Connections,
        IReadOnlySet<Guid> Supplies) {
        public static BottleneckIds Create(IEnumerable<Bottleneck> bottlenecks) {
            var machineGroups = new HashSet<Guid>();
            var connections = new HashSet<Guid>();
            var supplies = new HashSet<Guid>();

            foreach (var bottleneck in bottlenecks) {
                if (!Guid.TryParse(bottleneck.Element.Id, out var id)) {
                    continue;
                }

                _ = bottleneck.Kind switch {
                    BottleneckKind.MachineGroup => machineGroups.Add(id),
                    BottleneckKind.Connection => connections.Add(id),
                    BottleneckKind.Supply => supplies.Add(id),
                    _ => false
                };
            }

            return new BottleneckIds(machineGroups, connections, supplies);
        }
    }
}
