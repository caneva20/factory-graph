using FactoryGraph.Contracts;

namespace FactoryGraph.Exporter.Mermaid;

/// <summary>
///     Names one solve result in a combined Mermaid flowchart.
/// </summary>
/// <param name="Name">The subgraph label.</param>
/// <param name="Result">The result to export.</param>
public sealed record MermaidResultSection(
    string Name,
    SolveResult Result);
