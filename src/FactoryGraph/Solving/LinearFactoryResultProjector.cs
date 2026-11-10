using FactoryGraph.Contracts;

namespace FactoryGraph.Solving;

internal sealed class LinearFactoryResultProjector {
    public ComputedFactoryGraph Project(
        SolveRequest request,
        LinearFactoryModel model,
        IReadOnlyList<double> values) {
        var graph = request.Graph;
        var policy = request.Options.NumericPolicy;
        var recipes = graph.Recipes.ToDictionary(
            recipe => recipe.Id,
            recipe => new AggregatedRecipe(
                RecipeItemAggregator.Aggregate(recipe.Inputs),
                RecipeItemAggregator.Aggregate(recipe.Outputs)),
            StringComparer.Ordinal);

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

            var destinations = supply.DestinationMachineGroupIds.Select(destinationId => new ComputedSupplyDestination(
                destinationId,
                policy.NormalizeNearZero(values[model.SupplyDestinations[(supply.Id, destinationId)]]))).ToArray();

            return new ComputedSupply(
                supply.Id,
                destinations,
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

        var surplusOutlets = graph.SurplusOutlets.Select(outlet => new ComputedSurplusOutlet(
            outlet.Id,
            outlet.SourceMachineGroupId,
            outlet.ItemId,
            outlet.Capacity,
            policy.NormalizeNearZero(values[model.SurplusOutlets[outlet.Id]]))).ToArray();

        var targets = request.Targets.Select(target => {
            var achieved = policy.NormalizeNearZero(values[model.Targets[target.Id]]);

            return new ComputedTarget(
                target.Id,
                target.ProductOutletIds,
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

    private static ItemRate[] Rates(
        IReadOnlyList<RecipeItem> recipeItems,
        double activity,
        NumericPolicy policy) {
        var result = new ItemRate[recipeItems.Count];

        for (var index = 0; index < recipeItems.Count; index++) {
            var item = recipeItems[index];
            result[index] = new ItemRate(item.ItemId, policy.NormalizeNearZero(item.Amount * activity));
        }

        return result;
    }

    private static int WholeMachineCount(double effectiveMachineCount, NumericPolicy policy) {
        var nearestInteger = Math.Round(effectiveMachineCount);

        var value = policy.NearlyEquals(effectiveMachineCount, nearestInteger)
            ? nearestInteger
            : Math.Ceiling(effectiveMachineCount);

        return checked((int)value);
    }

    private sealed record AggregatedRecipe(RecipeItem[] Inputs, RecipeItem[] Outputs);
}
