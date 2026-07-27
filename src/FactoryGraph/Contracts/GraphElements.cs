namespace FactoryGraph.Contracts;

/// <summary>
///     Defines an item in the factory catalog.
/// </summary>
/// <param name="Id">The caller-assigned item ID.</param>
/// <param name="Name">The display name.</param>
public sealed record Item(
    string Id,
    string Name);

/// <summary>
///     Defines an item quantity consumed or produced by one recipe activity.
/// </summary>
/// <param name="ItemId">The ID of the consumed or produced item.</param>
/// <param name="Amount">The item quantity per activity.</param>
public sealed record RecipeItem(
    string ItemId,
    double Amount);

/// <summary>
///     Defines the inputs and outputs for one recipe activity.
/// </summary>
/// <param name="Id">The caller-assigned recipe ID.</param>
/// <param name="Name">The display name.</param>
/// <param name="Inputs">The items consumed by one activity.</param>
/// <param name="Outputs">The items produced by one activity.</param>
public sealed record Recipe(
    string Id,
    string Name,
    IReadOnlyList<RecipeItem> Inputs,
    IReadOnlyList<RecipeItem> Outputs);

/// <summary>
///     Defines a homogeneous group of machines that runs one recipe.
/// </summary>
/// <param name="Id">The machine-group ID.</param>
/// <param name="RecipeId">The ID of the recipe run by the group.</param>
/// <param name="ActivityRatePerMachine">The activities one fully utilized machine completes per time unit.</param>
/// <param name="AvailableMachineCount">
///     The installed machine count, or <c>null</c> when machine count does not limit
///     activity.
/// </param>
/// <param name="ActivityCapacity">
///     The maximum activities per time unit, or <c>null</c> when no direct activity limit
///     applies.
/// </param>
public sealed record MachineGroup(
    Guid Id,
    string RecipeId,
    double ActivityRatePerMachine,
    int? AvailableMachineCount = null,
    double? ActivityCapacity = null);

/// <summary>
///     Defines a directed flow of one item between two machine groups.
/// </summary>
/// <param name="Id">The connection ID.</param>
/// <param name="SourceMachineGroupId">The producing machine-group ID.</param>
/// <param name="DestinationMachineGroupId">The consuming machine-group ID.</param>
/// <param name="ItemId">The ID of the transferred item.</param>
/// <param name="Capacity">The maximum flow per time unit, or <c>null</c> for no connection limit.</param>
public sealed record ItemConnection(
    Guid Id,
    Guid SourceMachineGroupId,
    Guid DestinationMachineGroupId,
    string ItemId,
    double? Capacity = null);

/// <summary>
///     Defines an external item source for a machine-group input.
/// </summary>
/// <param name="Id">The supply ID.</param>
/// <param name="DestinationMachineGroupId">The receiving machine-group ID.</param>
/// <param name="ItemId">The supplied item ID.</param>
/// <param name="Capacity">The maximum supplied rate, or <c>null</c> for unlimited supply.</param>
public sealed record Supply(
    Guid Id,
    Guid DestinationMachineGroupId,
    string ItemId,
    double? Capacity = null);

/// <summary>
///     Defines an external consumer for a machine-group output.
/// </summary>
/// <param name="Id">The demand ID.</param>
/// <param name="SourceMachineGroupId">The producing machine-group ID.</param>
/// <param name="ItemId">The demanded item ID.</param>
/// <param name="Capacity">The maximum delivered rate, or <c>null</c> for unlimited demand.</param>
public sealed record Demand(
    Guid Id,
    Guid SourceMachineGroupId,
    string ItemId,
    double? Capacity = null);

/// <summary>
///     Defines an allowed destination for excess output.
/// </summary>
/// <param name="Id">The surplus-sink ID.</param>
/// <param name="SourceMachineGroupId">The producing machine-group ID.</param>
/// <param name="ItemId">The accepted item ID.</param>
/// <param name="Capacity">The maximum accepted rate, or <c>null</c> for unlimited surplus.</param>
public sealed record SurplusSink(
    Guid Id,
    Guid SourceMachineGroupId,
    string ItemId,
    double? Capacity = null);

/// <summary>
///     Defines a requested output rate for a demand.
/// </summary>
/// <param name="Id">The target ID.</param>
/// <param name="DemandId">The target demand ID.</param>
/// <param name="Rate">The requested rate per time unit.</param>
public sealed record Target(
    Guid Id,
    Guid DemandId,
    double Rate);

/// <summary>
///     Defines a self-contained factory topology.
/// </summary>
/// <param name="Items">The item catalog used by the graph.</param>
/// <param name="Recipes">The recipe catalog used by the graph.</param>
/// <param name="MachineGroups">The machine groups.</param>
/// <param name="Connections">The item connections between machine groups.</param>
/// <param name="Supplies">The external item sources.</param>
/// <param name="Demands">The external item consumers.</param>
/// <param name="SurplusSinks">The allowed destinations for excess output.</param>
public sealed record FactoryGraphDefinition(
    IReadOnlyList<Item> Items,
    IReadOnlyList<Recipe> Recipes,
    IReadOnlyList<MachineGroup> MachineGroups,
    IReadOnlyList<ItemConnection> Connections,
    IReadOnlyList<Supply> Supplies,
    IReadOnlyList<Demand> Demands,
    IReadOnlyList<SurplusSink> SurplusSinks);
