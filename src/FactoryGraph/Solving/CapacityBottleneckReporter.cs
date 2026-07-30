using FactoryGraph.Contracts;

namespace FactoryGraph.Solving;

internal sealed class CapacityBottleneckReporter {
    public IReadOnlyList<Bottleneck> Report(
        SolveRequest request,
        LinearFactoryModel model,
        IReadOnlyList<double> values) {
        var graph = request.Graph;
        var policy = request.Options.NumericPolicy;
        var relevantGroups = RelevantGroups(request, policy);
        var result = new List<Bottleneck>();

        foreach (var group in graph.MachineGroups.Where(group => relevantGroups.Contains(group.Id))) {
            var activity = values[model.Activities[group.Id]];
            var machineCapacity = group.AvailableMachineCount * group.ActivityRatePerMachine;

            var countLimits = machineCapacity is { } countCapacity
                              && (group.ActivityCapacity is null
                                  || countCapacity < group.ActivityCapacity
                                  || policy.NearlyEquals(countCapacity, group.ActivityCapacity.Value));

            var capacity = countLimits ? machineCapacity : group.ActivityCapacity;

            if (capacity is not { } activityCapacity || !policy.NearlyEquals(activity, activityCapacity)) {
                continue;
            }

            if (countLimits && group.AvailableMachineCount is { } machineCount) {
                result.Add(new Bottleneck(
                    BottleneckKind.MachineGroup,
                    new GraphElementReference(GraphElementKind.MachineGroup, group.Id),
                    machineCount,
                    Utilization(activity, activityCapacity, policy),
                    "Available machine count limits target output."));
            } else {
                result.Add(new Bottleneck(
                    BottleneckKind.MachineGroup,
                    new GraphElementReference(GraphElementKind.MachineGroup, group.Id),
                    activityCapacity,
                    Utilization(activity, activityCapacity, policy),
                    "Machine-group activity capacity limits target output."));
            }
        }

        foreach (var connection in graph.Connections.Where(connection =>
                     relevantGroups.Contains(connection.DestinationMachineGroupId))) {
            AddFlowBottleneck(
                result,
                connection.Capacity,
                values[model.Connections[connection.Id]],
                BottleneckKind.Connection,
                GraphElementKind.Connection,
                connection.Id,
                "Connection capacity limits target output.",
                policy);
        }

        foreach (var supply in graph.Supplies.Where(supply => relevantGroups.Contains(supply.DestinationMachineGroupId))) {
            AddFlowBottleneck(
                result,
                supply.Capacity,
                values[model.Supplies[supply.Id]],
                BottleneckKind.Supply,
                GraphElementKind.Supply,
                supply.Id,
                "Supply capacity limits target output.",
                policy);
        }

        return result;
    }

    private static HashSet<Guid> RelevantGroups(SolveRequest request, NumericPolicy policy) {
        var graph = request.Graph;
        var outlets = graph.ProductOutlets.ToDictionary(outlet => outlet.Id);

        var relevant = request.Targets
            .Where(target => policy.NormalizeNearZero(target.Rate) > 0)
            .Select(target => outlets[target.ProductOutletId].SourceMachineGroupId)
            .ToHashSet();

        var pending = new Queue<Guid>(relevant);

        while (pending.TryDequeue(out var destination)) {
            foreach (var source in graph.Connections
                         .Where(connection => connection.DestinationMachineGroupId == destination)
                         .Select(connection => connection.SourceMachineGroupId)) {
                if (relevant.Add(source)) {
                    pending.Enqueue(source);
                }
            }
        }

        return relevant;
    }

    private static void AddFlowBottleneck(
        ICollection<Bottleneck> result,
        double? capacity,
        double flow,
        BottleneckKind kind,
        GraphElementKind elementKind,
        Guid id,
        string message,
        NumericPolicy policy) {
        if (capacity is not { } limit || !policy.NearlyEquals(flow, limit)) {
            return;
        }

        result.Add(new Bottleneck(
            kind,
            new GraphElementReference(elementKind, id),
            limit,
            Utilization(flow, limit, policy),
            message));
    }

    private static double Utilization(double used, double capacity, NumericPolicy policy) {
        return policy.NormalizeNearZero(capacity) == 0 ? 1 : policy.NormalizeNearZero(used / capacity);
    }
}
