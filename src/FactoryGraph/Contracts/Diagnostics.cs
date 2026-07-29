using System.Text.Json.Serialization;

namespace FactoryGraph.Contracts;

/// <summary>
///     Specifies the effect of a solve diagnostic.
/// </summary>
public enum DiagnosticSeverity
{
    /// <summary>
    ///     Provides context without indicating a problem.
    /// </summary>
    Information,

    /// <summary>
    ///     Identifies a condition that may need caller attention.
    /// </summary>
    Warning,

    /// <summary>
    ///     Identifies a condition that prevents the requested result.
    /// </summary>
    Error
}

/// <summary>
///     Identifies the type of graph element referenced by a diagnostic.
/// </summary>
public enum GraphElementKind
{
    /// <summary>
    ///     An item catalog entry.
    /// </summary>
    Item,

    /// <summary>
    ///     A recipe catalog entry.
    /// </summary>
    Recipe,

    /// <summary>
    ///     A machine group.
    /// </summary>
    MachineGroup,

    /// <summary>
    ///     An item connection.
    /// </summary>
    Connection,

    /// <summary>
    ///     An external supply.
    /// </summary>
    Supply,

    /// <summary>
    ///     An intended-product outlet.
    /// </summary>
    ProductOutlet,

    /// <summary>
    ///     A product-outlet target.
    /// </summary>
    Target,

    /// <summary>
    ///     A surplus outlet.
    /// </summary>
    SurplusOutlet
}

/// <summary>
///     Identifies one graph element without depending on its ID type.
/// </summary>
/// <param name="Kind">The graph-element type.</param>
/// <param name="Id">The string or canonical GUID representation of the element ID.</param>
[method: JsonConstructor]
public sealed record GraphElementReference(
    GraphElementKind Kind,
    string Id)
{
    /// <summary>
    ///     Creates a reference to an element with a GUID ID.
    /// </summary>
    /// <param name="kind">The graph-element type.</param>
    /// <param name="id">The element ID.</param>
    public GraphElementReference(GraphElementKind kind, Guid id)
        : this(kind, id.ToString("D"))
    {
    }
}

/// <summary>
///     Describes one condition reported while validating or solving a graph.
/// </summary>
/// <param name="Code">The stable, machine-readable diagnostic code.</param>
/// <param name="Severity">The diagnostic severity.</param>
/// <param name="Message">The caller-facing description.</param>
/// <param name="Elements">The graph elements associated with the condition.</param>
public sealed record SolveDiagnostic(
    string Code,
    DiagnosticSeverity Severity,
    string Message,
    IReadOnlyList<GraphElementReference> Elements);

/// <summary>
///     Identifies the type of capacity that limits production.
/// </summary>
public enum BottleneckKind
{
    /// <summary>
    ///     A machine count or activity capacity.
    /// </summary>
    MachineGroup,

    /// <summary>
    ///     An item-connection capacity.
    /// </summary>
    Connection,

    /// <summary>
    ///     An external-supply capacity.
    /// </summary>
    Supply
}

/// <summary>
///     Describes a capacity that limits achieved target output.
/// </summary>
/// <param name="Kind">The bottleneck type.</param>
/// <param name="Element">The graph element that owns the capacity.</param>
/// <param name="Capacity">The binding capacity.</param>
/// <param name="Utilization">The used fraction of the capacity.</param>
/// <param name="Message">The caller-facing description.</param>
public sealed record Bottleneck(
    BottleneckKind Kind,
    GraphElementReference Element,
    double Capacity,
    double Utilization,
    string Message);
