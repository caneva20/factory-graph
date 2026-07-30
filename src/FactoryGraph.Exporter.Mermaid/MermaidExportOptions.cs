namespace FactoryGraph.Exporter.Mermaid;

/// <summary>
///     Specifies a Mermaid flowchart direction.
/// </summary>
public enum MermaidFlowchartDirection {
    /// <summary>Places sources left of their destinations.</summary>
    LeftToRight,

    /// <summary>Places sources above their destinations.</summary>
    TopToBottom
}

/// <summary>
///     Configures Mermaid output.
/// </summary>
public sealed record MermaidExportOptions {
    /// <summary>Gets the flowchart direction.</summary>
    public MermaidFlowchartDirection Direction { get; init; } = MermaidFlowchartDirection.LeftToRight;

    /// <summary>Gets the invariant-culture numeric format.</summary>
    public string NumberFormat { get; init; } = "0.###";

    /// <summary>Gets whether labels include configured capacities.</summary>
    public bool IncludeCapacities { get; init; } = true;

    /// <summary>Gets whether output includes built-in node and bottleneck styles.</summary>
    public bool IncludeStyles { get; init; } = true;
}
