using System.Globalization;
using System.Net;

namespace FactoryGraph.Exporter.Mermaid;

internal sealed record MermaidDiagram(
    IReadOnlyList<MermaidNode> Nodes,
    IReadOnlyList<MermaidEdge> Edges);

internal sealed record MermaidDiagramSection(
    string Name,
    MermaidDiagram Diagram);

internal sealed record MermaidNode(
    int Order,
    string Id,
    string Label,
    string Style,
    bool IsBottleneck = false);

internal sealed record MermaidEdge(
    int Order,
    string Id,
    string SourceId,
    string DestinationId,
    string Label,
    bool IsDotted = false,
    bool IsBottleneck = false);

internal static class MermaidText {
    public static string Id(string prefix, Guid id) => $"{prefix}_{id:N}";

    public static string Label(params IEnumerable<string> lines) {
        return string.Join("<br/>", lines.Select(WebUtility.HtmlEncode));
    }

    public static string Number(double value, MermaidExportOptions options) {
        return value.ToString(options.NumberFormat, CultureInfo.InvariantCulture);
    }

    public static string Percentage(double value, MermaidExportOptions options) {
        return Number(value * 100, options) + "%";
    }

    public static string WithCapacity(
        string label,
        double? capacity,
        MermaidExportOptions options) {
        return options.IncludeCapacities && capacity is { } value
            ? Label(label, $"capacity: {Number(value, options)}")
            : Label(label);
    }
}
