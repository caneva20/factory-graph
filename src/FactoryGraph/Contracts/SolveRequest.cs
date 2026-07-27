namespace FactoryGraph.Contracts;

/// <summary>
///     Specifies how the processor plans production.
/// </summary>
public enum PlanningMode
{
    /// <summary>
    ///     Computes the activity and resources needed to satisfy requested targets.
    /// </summary>
    TargetDriven,

    /// <summary>
    ///     Computes the maximum target output allowed by configured capacities.
    /// </summary>
    CapacityDriven
}

/// <summary>
///     Specifies the value optimized by the processor.
/// </summary>
public enum SolveObjective
{
    /// <summary>
    ///     Maximizes achieved target output.
    /// </summary>
    MaximizeTargetOutput,

    /// <summary>
    ///     Meets targets while minimizing external supply.
    /// </summary>
    MeetTargetsMinimizeExternalSupply,

    /// <summary>
    ///     Meets targets while minimizing surplus output.
    /// </summary>
    MeetTargetsMinimizeSurplus
}

/// <summary>
///     Configures numeric behavior and solver execution.
/// </summary>
/// <param name="NumericPolicy">The floating-point comparison and normalization policy.</param>
/// <param name="TimeLimit">The maximum solve duration, or <c>null</c> for no caller-defined limit.</param>
public sealed record SolverOptions(
    NumericPolicy NumericPolicy,
    TimeSpan? TimeLimit = null);

/// <summary>
///     Defines one factory-planning request.
/// </summary>
/// <param name="Graph">The factory topology to solve.</param>
/// <param name="Targets">The requested demand rates.</param>
/// <param name="Mode">The planning mode.</param>
/// <param name="Objective">The optimization objective.</param>
/// <param name="Options">The solver options.</param>
public sealed record SolveRequest(
    FactoryGraphDefinition Graph,
    IReadOnlyList<Target> Targets,
    PlanningMode Mode,
    SolveObjective Objective,
    SolverOptions Options);
