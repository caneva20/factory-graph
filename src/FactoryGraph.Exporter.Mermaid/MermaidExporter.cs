using FactoryGraph.Contracts;

namespace FactoryGraph.Exporter.Mermaid;

/// <summary>
///     Exports solve requests and results as Mermaid flowcharts.
/// </summary>
public sealed class MermaidExporter {
    private readonly MermaidExportOptions _options;

    /// <summary>
    ///     Creates an exporter with default options.
    /// </summary>
    public MermaidExporter() : this(new MermaidExportOptions()) {
    }

    /// <summary>
    ///     Creates an exporter with the specified options.
    /// </summary>
    /// <param name="options">The Mermaid output options.</param>
    public MermaidExporter(MermaidExportOptions options) {
        _options = options;
    }

    /// <summary>
    ///     Exports an unsolved diagram from a request that passed core validation.
    /// </summary>
    /// <param name="request">The request to export.</param>
    /// <returns>A Mermaid flowchart.</returns>
    /// <remarks>This method does not validate the request.</remarks>
    public string Export(SolveRequest request) {
        return MermaidWriter.Write(SolveRequestDiagramBuilder.Build(request, _options), _options);
    }

    /// <summary>
    ///     Exports a computed result as a solved diagram.
    /// </summary>
    /// <param name="result">The result to export.</param>
    /// <returns>A Mermaid flowchart.</returns>
    /// <exception cref="ArgumentException">The result does not contain a computed graph.</exception>
    public string Export(SolveResult result) {
        if (result.Graph is null) {
            throw new ArgumentException("The solve result must contain a computed graph.", nameof(result));
        }

        return MermaidWriter.Write(SolveResultDiagramBuilder.Build(result.Graph, result.Bottlenecks, _options), _options);
    }

    /// <summary>
    ///     Exports named results as subgraphs in one flowchart.
    /// </summary>
    /// <param name="sections">The named results to export.</param>
    /// <returns>A Mermaid flowchart.</returns>
    /// <exception cref="ArgumentException">A section name is empty or duplicated.</exception>
    public string Export(IEnumerable<MermaidResultSection> sections) {
        var orderedSections = sections
            .OrderBy(section => section.Name, StringComparer.Ordinal)
            .ToArray();

        if (orderedSections.Any(section => string.IsNullOrWhiteSpace(section.Name))) {
            throw new ArgumentException("Section names must not be empty.", nameof(sections));
        }

        if (orderedSections.Select(section => section.Name).Distinct(StringComparer.Ordinal).Count() != orderedSections.Length) {
            throw new ArgumentException("Section names must be unique.", nameof(sections));
        }

        var diagrams = orderedSections.Select(section => new MermaidDiagramSection(
            section.Name,
            section.Result.Graph is { } graph
                ? SolveResultDiagramBuilder.Build(graph, section.Result.Bottlenecks, _options)
                : SolveResultSummaryDiagramBuilder.Build(section.Result))).ToArray();

        return MermaidWriter.Write(diagrams, _options);
    }
}
