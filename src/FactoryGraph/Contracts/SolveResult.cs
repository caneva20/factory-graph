namespace FactoryGraph.Contracts;

/// <summary>
///     Specifies the outcome of a solve operation.
/// </summary>
public enum SolveStatus
{
    /// <summary>
    ///     The solver found a proven optimal solution.
    /// </summary>
    Optimal,

    /// <summary>
    ///     The solver found a feasible solution without proving optimality.
    /// </summary>
    Feasible,

    /// <summary>
    ///     The request failed validation.
    /// </summary>
    InvalidRequest,

    /// <summary>
    ///     No solution satisfies the request constraints.
    /// </summary>
    Infeasible,

    /// <summary>
    ///     The objective can improve without a finite bound.
    /// </summary>
    Unbounded,

    /// <summary>
    ///     The processor failed before it could classify the model.
    /// </summary>
    Failed
}

/// <summary>
///     Reports a solved rate for one item.
/// </summary>
/// <param name="ItemId">The item ID.</param>
/// <param name="Rate">The item rate per time unit.</param>
public sealed record ItemRate(
    string ItemId,
    double Rate);

/// <summary>
///     Reports the solved activity and item rates for a machine group.
/// </summary>
/// <param name="Id">The input machine-group ID.</param>
/// <param name="RecipeId">The input recipe ID.</param>
/// <param name="ActivityRatePerMachine">The configured activities per machine and time unit.</param>
/// <param name="AvailableMachineCount">The configured machine count, or <c>null</c> when absent.</param>
/// <param name="ActivityCapacity">The configured activity capacity, or <c>null</c> when absent.</param>
/// <param name="Activity">The solved activities per time unit.</param>
/// <param name="EffectiveMachineCount">The fractional machine count required for the solved activity.</param>
/// <param name="RequiredWholeMachineCount">The physical machine count required for the solved activity.</param>
/// <param name="Utilization">The effective machine count divided by the required whole-machine count.</param>
/// <param name="InputRates">The solved input rates by item.</param>
/// <param name="OutputRates">The solved output rates by item.</param>
public sealed record ComputedMachineGroup(
    Guid Id,
    string RecipeId,
    double ActivityRatePerMachine,
    int? AvailableMachineCount,
    double? ActivityCapacity,
    double Activity,
    double EffectiveMachineCount,
    int RequiredWholeMachineCount,
    double Utilization,
    IReadOnlyList<ItemRate> InputRates,
    IReadOnlyList<ItemRate> OutputRates);

/// <summary>
///     Reports the solved flow through an item connection.
/// </summary>
/// <param name="Id">The input connection ID.</param>
/// <param name="SourceMachineGroupId">The input source machine-group ID.</param>
/// <param name="DestinationMachineGroupId">The input destination machine-group ID.</param>
/// <param name="ItemId">The input item ID.</param>
/// <param name="Capacity">The configured flow capacity, or <c>null</c> when absent.</param>
/// <param name="FlowRate">The solved flow per time unit.</param>
public sealed record ComputedConnection(
    Guid Id,
    Guid SourceMachineGroupId,
    Guid DestinationMachineGroupId,
    string ItemId,
    double? Capacity,
    double FlowRate);

/// <summary>
///     Reports required and provided external supply.
/// </summary>
/// <param name="Id">The input supply ID.</param>
/// <param name="Destinations">The solved flow to each input destination, in input order.</param>
/// <param name="ItemId">The input item ID.</param>
/// <param name="Capacity">The configured combined supply capacity, or <c>null</c> when absent.</param>
/// <param name="RequiredRate">The combined supply rate required by solved machine activity.</param>
/// <param name="ProvidedRate">The combined supply rate available to the solved graph.</param>
/// <param name="DeficitRate">The required rate not provided by this supply.</param>
public sealed record ComputedSupply(
    Guid Id,
    IReadOnlyList<ComputedSupplyDestination> Destinations,
    string ItemId,
    double? Capacity,
    double RequiredRate,
    double ProvidedRate,
    double DeficitRate);

/// <summary>
///     Reports the solved flow from a supply to one machine group.
/// </summary>
/// <param name="MachineGroupId">The receiving machine-group ID.</param>
/// <param name="FlowRate">The supply rate consumed by the machine group.</param>
public sealed record ComputedSupplyDestination(
    Guid MachineGroupId,
    double FlowRate);

/// <summary>
///     Reports the solved output delivered to a product outlet.
/// </summary>
/// <param name="Id">The product-outlet ID.</param>
/// <param name="SourceMachineGroupId">The input source machine-group ID.</param>
/// <param name="ItemId">The input item ID.</param>
/// <param name="Capacity">The configured outlet capacity, or <c>null</c> when absent.</param>
/// <param name="DeliveredRate">The output rate delivered to the outlet.</param>
public sealed record ComputedProductOutlet(
    Guid Id,
    Guid SourceMachineGroupId,
    string ItemId,
    double? Capacity,
    double DeliveredRate);

/// <summary>
///     Reports the solved output sent to a surplus outlet.
/// </summary>
/// <param name="Id">The surplus-outlet ID.</param>
/// <param name="SourceMachineGroupId">The input source machine-group ID.</param>
/// <param name="ItemId">The input item ID.</param>
/// <param name="Capacity">The configured outlet capacity, or <c>null</c> when absent.</param>
/// <param name="FlowRate">The output rate sent to the outlet.</param>
public sealed record ComputedSurplusOutlet(
    Guid Id,
    Guid SourceMachineGroupId,
    string ItemId,
    double? Capacity,
    double FlowRate);

/// <summary>
///     Reports achieved output and deviation for one target.
/// </summary>
/// <param name="Id">The input target ID.</param>
/// <param name="ProductOutletIds">The input product-outlet IDs.</param>
/// <param name="RequestedRate">The requested output rate.</param>
/// <param name="AchievedRate">The solved output rate, summed over the product outlets.</param>
/// <param name="SurplusRate">The amount by which achieved output exceeds the request.</param>
/// <param name="DeficitRate">The amount by which achieved output falls short of the request.</param>
public sealed record ComputedTarget(
    Guid Id,
    IReadOnlyList<Guid> ProductOutletIds,
    double RequestedRate,
    double AchievedRate,
    double SurplusRate,
    double DeficitRate);

/// <summary>
///     Contains the input topology annotated with solved values.
/// </summary>
/// <param name="Items">The input item catalog.</param>
/// <param name="Recipes">The input recipe catalog.</param>
/// <param name="MachineGroups">The computed machine groups.</param>
/// <param name="Connections">The computed item connections.</param>
/// <param name="Supplies">The computed external supplies.</param>
/// <param name="ProductOutlets">The computed product outlets.</param>
/// <param name="SurplusOutlets">The computed surplus outlets.</param>
/// <param name="Targets">The computed targets.</param>
public sealed record ComputedFactoryGraph(
    IReadOnlyList<Item> Items,
    IReadOnlyList<Recipe> Recipes,
    IReadOnlyList<ComputedMachineGroup> MachineGroups,
    IReadOnlyList<ComputedConnection> Connections,
    IReadOnlyList<ComputedSupply> Supplies,
    IReadOnlyList<ComputedProductOutlet> ProductOutlets,
    IReadOnlyList<ComputedSurplusOutlet> SurplusOutlets,
    IReadOnlyList<ComputedTarget> Targets);

/// <summary>
///     Identifies one cyclic set of machine groups and whether it can start without external inventory.
/// </summary>
/// <param name="MachineGroupIds">The machine-group IDs in graph declaration order.</param>
/// <param name="ConnectionIds">The IDs of connections whose endpoints both belong to the component.</param>
/// <param name="RequiresStartupInventory">
///     <see langword="true" /> when the component cannot start from an empty state using configured supplies and startable upstream groups.
/// </param>
public sealed record CyclicComponent(
    IReadOnlyList<Guid> MachineGroupIds,
    IReadOnlyList<Guid> ConnectionIds,
    bool RequiresStartupInventory);

/// <summary>
///     Contains structural information derived from a validated factory graph.
/// </summary>
/// <param name="CyclicComponents">The graph's cyclic components in graph declaration order.</param>
public sealed record SolveAnalysis(IReadOnlyList<CyclicComponent> CyclicComponents) {
    /// <summary>
    ///     Gets an analysis with no cyclic components.
    /// </summary>
    public static SolveAnalysis Empty { get; } = new([]);
}

/// <summary>
///     Contains the status, computed graph, diagnostics, bottlenecks, and structural analysis from one solve.
/// </summary>
/// <param name="Status">The solve outcome.</param>
/// <param name="Graph">The computed graph, or <c>null</c> when the processor could not produce one.</param>
/// <param name="Diagnostics">The conditions reported during validation and solving.</param>
/// <param name="Bottlenecks">The capacities that limited achieved target output.</param>
public sealed record SolveResult(
    SolveStatus Status,
    ComputedFactoryGraph? Graph,
    IReadOnlyList<SolveDiagnostic> Diagnostics,
    IReadOnlyList<Bottleneck> Bottlenecks) {
    /// <summary>
    ///     Gets structural information about the validated input graph.
    /// </summary>
    public SolveAnalysis Analysis { get; init; } = SolveAnalysis.Empty;
}
