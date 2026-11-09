using FactoryGraph.Contracts;

namespace FactoryGraph.Solving;

internal sealed class LinearFactoryModelBuilder {
    private const double Infinity = 1e30;

    public LinearFactoryModel Build(SolveRequest request) {
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

        var recipes = graph.Recipes.ToDictionary(
            recipe => recipe.Id,
            recipe => new AggregatedRecipe(
                RecipeItemAggregator.Aggregate(recipe.Inputs),
                RecipeItemAggregator.Aggregate(recipe.Outputs)),
            StringComparer.Ordinal);

        var inputRowCount = 0;
        var outputRowCount = 0;

        foreach (var group in graph.MachineGroups) {
            var recipe = recipes[group.RecipeId];
            inputRowCount += recipe.Inputs.Length;
            outputRowCount += recipe.Outputs.Length;
        }

        var rows = new List<ModelRow>(inputRowCount + outputRowCount);
        var inputRows = new Dictionary<(Guid GroupId, string ItemId), Dictionary<int, double>>(inputRowCount);
        var outputRows = new Dictionary<(Guid GroupId, string ItemId), Dictionary<int, double>>(outputRowCount);

        foreach (var group in graph.MachineGroups) {
            var recipe = recipes[group.RecipeId];

            foreach (var item in recipe.Inputs) {
                var coefficients = new Dictionary<int, double>(1) { [activities[group.Id]] = item.Amount };
                inputRows.Add((group.Id, item.ItemId), coefficients);
                rows.Add(ModelRow.Equality(coefficients));
            }

            foreach (var item in recipe.Outputs) {
                var coefficients = new Dictionary<int, double>(1) { [activities[group.Id]] = item.Amount };
                outputRows.Add((group.Id, item.ItemId), coefficients);
                rows.Add(ModelRow.Equality(coefficients));
            }
        }

        foreach (var connection in graph.Connections) {
            AddFlow(outputRows, (connection.SourceMachineGroupId, connection.ItemId), connections[connection.Id]);
            AddFlow(inputRows, (connection.DestinationMachineGroupId, connection.ItemId), connections[connection.Id]);
        }

        foreach (var supply in graph.Supplies) {
            AddFlow(inputRows, (supply.DestinationMachineGroupId, supply.ItemId), supplies[supply.Id]);
        }

        foreach (var outlet in graph.ProductOutlets) {
            AddFlow(outputRows, (outlet.SourceMachineGroupId, outlet.ItemId), productOutlets[outlet.Id]);
        }

        foreach (var outlet in graph.SurplusOutlets) {
            AddFlow(outputRows, (outlet.SourceMachineGroupId, outlet.ItemId), surplusOutlets[outlet.Id]);
        }

        return new LinearFactoryModel(
            new LinearProgram(lower, upper, rows),
            activities,
            connections,
            supplies,
            productOutlets,
            surplusOutlets,
            graph.Supplies.Where(supply => supply.IsFallback).Select(supply => supplies[supply.Id]).ToArray());
    }

    private static Dictionary<TKey, int> Index<T, TKey>(
        IReadOnlyCollection<T> values,
        Func<T, TKey> key,
        ref int nextVariable)
        where TKey : notnull {
        var result = new Dictionary<TKey, int>(values.Count);

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

    private static void AddFlow(
        IReadOnlyDictionary<(Guid GroupId, string ItemId), Dictionary<int, double>> rows,
        (Guid GroupId, string ItemId) key,
        int variable) {
        if (rows.TryGetValue(key, out var coefficients)) {
            coefficients[variable] = -1;
        }
    }

    private sealed record AggregatedRecipe(RecipeItem[] Inputs, RecipeItem[] Outputs);
}
