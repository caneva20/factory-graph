using FactoryGraph.Contracts;

namespace FactoryGraph.Analysis;

internal sealed class FactoryGraphAnalyzer {
    public SolveAnalysis Analyze(FactoryGraphDefinition graph, NumericPolicy numericPolicy) {
        var groupIndexes = graph.MachineGroups
            .Select((group, index) => (group.Id, index))
            .ToDictionary(entry => entry.Id, entry => entry.index);

        var outgoing = Enumerable.Range(0, graph.MachineGroups.Count)
            .Select(_ => new List<int>())
            .ToArray();

        foreach (var connection in graph.Connections) {
            outgoing[groupIndexes[connection.SourceMachineGroupId]].Add(groupIndexes[connection.DestinationMachineGroupId]);
        }

        var startableGroups = FindStartableGroups(graph, numericPolicy);

        var components = FindStronglyConnectedComponents(outgoing)
            .Where(component => IsCycle(component, outgoing))
            .Select(component => component.Order().ToArray())
            .OrderBy(component => component[0])
            .Select(component => CreateComponent(graph, component, startableGroups))
            .ToArray();

        return components.Length == 0 ? SolveAnalysis.Empty : new SolveAnalysis(components);
    }

    private static HashSet<Guid> FindStartableGroups(FactoryGraphDefinition graph, NumericPolicy numericPolicy) {
        var recipes = graph.Recipes.ToDictionary(recipe => recipe.Id, StringComparer.Ordinal);

        var usableSupplies = graph.Supplies
            .Where(supply => supply.Capacity is null || numericPolicy.NormalizeNearZero(supply.Capacity.Value) > 0)
            .Select(supply => (supply.DestinationMachineGroupId, supply.ItemId))
            .ToHashSet();

        var incoming = graph.Connections
            .GroupBy(connection => (connection.DestinationMachineGroupId, connection.ItemId))
            .ToDictionary(group => group.Key, group => group.ToArray());

        var startable = new HashSet<Guid>();

        bool changed;

        do {
            changed = false;

            foreach (var group in graph.MachineGroups) {
                if (startable.Contains(group.Id)) continue;

                var inputs = recipes[group.RecipeId].Inputs.Select(input => input.ItemId).Distinct(StringComparer.Ordinal);

                var canStart = inputs.All(itemId =>
                    usableSupplies.Contains((group.Id, itemId))
                    || incoming.TryGetValue((group.Id, itemId), out var connections)
                    && connections.Any(connection => startable.Contains(connection.SourceMachineGroupId)));

                if (canStart) changed |= startable.Add(group.Id);
            }
        } while (changed);

        return startable;
    }

    private static IReadOnlyList<int[]> FindStronglyConnectedComponents(IReadOnlyList<List<int>> outgoing) {
        var nextIndex = 0;
        var indexes = Enumerable.Repeat(-1, outgoing.Count).ToArray();
        var lowLinks = new int[outgoing.Count];
        var onStack = new bool[outgoing.Count];
        var stack = new Stack<int>();
        var components = new List<int[]>();

        void Connect(int vertex) {
            indexes[vertex] = nextIndex;
            lowLinks[vertex] = nextIndex;
            nextIndex++;
            stack.Push(vertex);
            onStack[vertex] = true;

            foreach (var destination in outgoing[vertex]) {
                if (indexes[destination] < 0) {
                    Connect(destination);
                    lowLinks[vertex] = Math.Min(lowLinks[vertex], lowLinks[destination]);
                } else if (onStack[destination]) {
                    lowLinks[vertex] = Math.Min(lowLinks[vertex], indexes[destination]);
                }
            }

            if (lowLinks[vertex] != indexes[vertex]) return;

            var component = new List<int>();
            int member;

            do {
                member = stack.Pop();
                onStack[member] = false;
                component.Add(member);
            } while (member != vertex);

            components.Add(component.ToArray());
        }

        for (var vertex = 0; vertex < outgoing.Count; vertex++) {
            if (indexes[vertex] < 0) Connect(vertex);
        }

        return components;
    }

    private static bool IsCycle(IReadOnlyList<int> component, IReadOnlyList<List<int>> outgoing) {
        return component.Count > 1 || outgoing[component[0]].Contains(component[0]);
    }

    private static CyclicComponent CreateComponent(
        FactoryGraphDefinition graph,
        IReadOnlyList<int> component,
        IReadOnlySet<Guid> startableGroups) {
        var machineGroupIds = component.Select(index => graph.MachineGroups[index].Id).ToArray();
        var machineGroupIdSet = machineGroupIds.ToHashSet();

        var connectionIds = graph.Connections
            .Where(connection => machineGroupIdSet.Contains(connection.SourceMachineGroupId)
                                 && machineGroupIdSet.Contains(connection.DestinationMachineGroupId))
            .Select(connection => connection.Id)
            .ToArray();

        return new CyclicComponent(
            machineGroupIds,
            connectionIds,
            machineGroupIds.Any(id => !startableGroups.Contains(id)));
    }
}
