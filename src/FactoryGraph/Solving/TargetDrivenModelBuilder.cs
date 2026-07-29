using FactoryGraph.Contracts;

namespace FactoryGraph.Solving;

internal sealed class TargetDrivenModelBuilder {
    private const double Infinity = 1e30;

    public TargetDrivenModel Build(SolveRequest request) {
        var graph = request.Graph;

        var variableCount = graph.MachineGroups.Count
                            + graph.Connections.Count
                            + graph.Supplies.Count
                            + graph.ProductOutlets.Count
                            + graph.SurplusOutlets.Count;

        var lower = new double[variableCount];
        var upper = Enumerable.Repeat(Infinity, variableCount).ToArray();
        var nextVariable = 0;

        var activities = Index(graph.MachineGroups, group => group.Id, ref nextVariable);
        var connections = Index(graph.Connections, connection => connection.Id, ref nextVariable);
        var supplies = Index(graph.Supplies, supply => supply.Id, ref nextVariable);
        var productOutlets = Index(graph.ProductOutlets, outlet => outlet.Id, ref nextVariable);
        var surplusOutlets = Index(graph.SurplusOutlets, outlet => outlet.Id, ref nextVariable);

        foreach (var group in graph.MachineGroups) {
            var capacity = Infinity;

            if (group.AvailableMachineCount is { } machineCount) {
                capacity = machineCount * group.ActivityRatePerMachine;
            }

            if (group.ActivityCapacity is { } activityCapacity) {
                capacity = Math.Min(capacity, activityCapacity);
            }

            upper[activities[group.Id]] = capacity;
        }

        SetCapacities(graph.Connections, connection => connection.Id, connection => connection.Capacity, connections, upper);
        SetCapacities(graph.Supplies, supply => supply.Id, supply => supply.Capacity, supplies, upper);
        SetCapacities(graph.ProductOutlets, outlet => outlet.Id, outlet => outlet.Capacity, productOutlets, upper);
        SetCapacities(graph.SurplusOutlets, outlet => outlet.Id, outlet => outlet.Capacity, surplusOutlets, upper);

        foreach (var target in request.Targets) {
            lower[productOutlets[target.ProductOutletId]] = target.Rate;
        }

        var recipes = graph.Recipes.ToDictionary(recipe => recipe.Id, StringComparer.Ordinal);
        var rows = new List<ModelRow>();

        foreach (var group in graph.MachineGroups) {
            var recipe = recipes[group.RecipeId];

            foreach (var item in Aggregate(recipe.Inputs)) {
                var coefficients = new Dictionary<int, double> { [activities[group.Id]] = item.Amount };

                AddFlows(
                    coefficients,
                    graph.Connections.Where(connection =>
                        connection.DestinationMachineGroupId == group.Id && connection.ItemId == item.ItemId),
                    connection => connection.Id,
                    connections,
                    -1);

                AddFlows(
                    coefficients,
                    graph.Supplies.Where(supply =>
                        supply.DestinationMachineGroupId == group.Id && supply.ItemId == item.ItemId),
                    supply => supply.Id,
                    supplies,
                    -1);

                rows.Add(ModelRow.Equality(coefficients));
            }

            foreach (var item in Aggregate(recipe.Outputs)) {
                var coefficients = new Dictionary<int, double> { [activities[group.Id]] = item.Amount };

                AddFlows(
                    coefficients,
                    graph.Connections.Where(connection =>
                        connection.SourceMachineGroupId == group.Id && connection.ItemId == item.ItemId),
                    connection => connection.Id,
                    connections,
                    -1);

                AddFlows(
                    coefficients,
                    graph.ProductOutlets.Where(outlet =>
                        outlet.SourceMachineGroupId == group.Id && outlet.ItemId == item.ItemId),
                    outlet => outlet.Id,
                    productOutlets,
                    -1);

                AddFlows(
                    coefficients,
                    graph.SurplusOutlets.Where(outlet =>
                        outlet.SourceMachineGroupId == group.Id && outlet.ItemId == item.ItemId),
                    outlet => outlet.Id,
                    surplusOutlets,
                    -1);

                rows.Add(ModelRow.Equality(coefficients));
            }
        }

        return new TargetDrivenModel(
            new LinearProgram(lower, upper, rows),
            activities,
            connections,
            supplies,
            productOutlets,
            surplusOutlets);
    }

    private static Dictionary<TKey, int> Index<T, TKey>(
        IEnumerable<T> values,
        Func<T, TKey> key,
        ref int nextVariable)
        where TKey : notnull {
        var result = new Dictionary<TKey, int>();

        foreach (var value in values) {
            result.Add(key(value), nextVariable++);
        }

        return result;
    }

    private static void SetCapacities<T>(
        IEnumerable<T> values,
        Func<T, Guid> id,
        Func<T, double?> capacity,
        IReadOnlyDictionary<Guid, int> variables,
        double[] upperBounds) {
        foreach (var value in values) {
            if (capacity(value) is { } limit) upperBounds[variables[id(value)]] = limit;
        }
    }

    private static IReadOnlyList<RecipeItem> Aggregate(IEnumerable<RecipeItem> items) {
        return items
            .GroupBy(item => item.ItemId, StringComparer.Ordinal)
            .Select(group => new RecipeItem(group.Key, group.Sum(item => item.Amount)))
            .ToArray();
    }

    private static void AddFlows<T>(
        IDictionary<int, double> coefficients,
        IEnumerable<T> flows,
        Func<T, Guid> id,
        IReadOnlyDictionary<Guid, int> variables,
        double coefficient) {
        foreach (var flow in flows) {
            coefficients[variables[id(flow)]] = coefficient;
        }
    }
}
