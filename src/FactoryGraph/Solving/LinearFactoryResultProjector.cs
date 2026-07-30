using FactoryGraph.Contracts;

namespace FactoryGraph.Solving;

internal sealed class LinearFactoryResultProjector {
    public ComputedFactoryGraph Project(
        SolveRequest request,
        LinearFactoryModel model,
        IReadOnlyList<double> values) {
        var graph = request.Graph;
        var policy = request.Options.NumericPolicy;
        var recipes = graph.Recipes.ToDictionary(recipe => recipe.Id, StringComparer.Ordinal);

        var machineGroups = graph.MachineGroups.Select(group => {
            var activity = policy.NormalizeNearZero(values[model.Activities[group.Id]]);
            var effectiveMachineCount = policy.NormalizeNearZero(activity / group.ActivityRatePerMachine);
            var requiredWholeMachineCount = WholeMachineCount(effectiveMachineCount, policy);

            var utilization = requiredWholeMachineCount == 0
                ? 0
                : policy.NormalizeNearZero(effectiveMachineCount / requiredWholeMachineCount);

            var recipe = recipes[group.RecipeId];

            return new ComputedMachineGroup(
                group.Id,
                group.RecipeId,
                group.ActivityRatePerMachine,
                group.AvailableMachineCount,
                group.ActivityCapacity,
                activity,
                effectiveMachineCount,
                requiredWholeMachineCount,
                utilization,
                Rates(recipe.Inputs, activity, policy),
                Rates(recipe.Outputs, activity, policy));
        }).ToArray();

        var connections = graph.Connections.Select(connection => new ComputedConnection(
            connection.Id,
            connection.SourceMachineGroupId,
            connection.DestinationMachineGroupId,
            connection.ItemId,
            connection.Capacity,
            policy.NormalizeNearZero(values[model.Connections[connection.Id]]))).ToArray();

        var supplies = graph.Supplies.Select(supply => {
            var provided = policy.NormalizeNearZero(values[model.Supplies[supply.Id]]);

            return new ComputedSupply(
                supply.Id,
                supply.DestinationMachineGroupId,
                supply.ItemId,
                supply.Capacity,
                provided,
                provided,
                0);
        }).ToArray();

        var productOutlets = graph.ProductOutlets.Select(outlet => new ComputedProductOutlet(
            outlet.Id,
            outlet.SourceMachineGroupId,
            outlet.ItemId,
            outlet.Capacity,
            policy.NormalizeNearZero(values[model.ProductOutlets[outlet.Id]]))).ToArray();

        var deliveredRates = productOutlets.ToDictionary(outlet => outlet.Id, outlet => outlet.DeliveredRate);

        var surplusOutlets = graph.SurplusOutlets.Select(outlet => new ComputedSurplusOutlet(
            outlet.Id,
            outlet.SourceMachineGroupId,
            outlet.ItemId,
            outlet.Capacity,
            policy.NormalizeNearZero(values[model.SurplusOutlets[outlet.Id]]))).ToArray();

        var targets = request.Targets.Select(target => {
            var achieved = deliveredRates[target.ProductOutletId];

            return new ComputedTarget(
                target.Id,
                target.ProductOutletId,
                target.Rate,
                achieved,
                policy.NormalizeNearZero(Math.Max(0, achieved - target.Rate)),
                policy.NormalizeNearZero(Math.Max(0, target.Rate - achieved)));
        }).ToArray();

        return new ComputedFactoryGraph(
            graph.Items,
            graph.Recipes,
            machineGroups,
            connections,
            supplies,
            productOutlets,
            surplusOutlets,
            targets);
    }

    private static IReadOnlyList<ItemRate> Rates(
        IEnumerable<RecipeItem> recipeItems,
        double activity,
        NumericPolicy policy) {
        return recipeItems
            .GroupBy(item => item.ItemId, StringComparer.Ordinal)
            .Select(group => new ItemRate(
                group.Key,
                policy.NormalizeNearZero(group.Sum(item => item.Amount) * activity)))
            .ToArray();
    }

    private static int WholeMachineCount(double effectiveMachineCount, NumericPolicy policy) {
        var nearestInteger = Math.Round(effectiveMachineCount);

        var value = policy.NearlyEquals(effectiveMachineCount, nearestInteger)
            ? nearestInteger
            : Math.Ceiling(effectiveMachineCount);

        return checked((int)value);
    }
}
