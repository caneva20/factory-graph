using FactoryGraph.Contracts;
using static FactoryGraph.Exporter.Mermaid.MermaidText;

namespace FactoryGraph.Exporter.Mermaid;

internal static class SolveResultSummaryDiagramBuilder {
    public static MermaidDiagram Build(SolveResult result) {
        var nodes = new List<MermaidNode> {
            new(0, "result_status", Label("Result", $"status: {result.Status}"), "status")
        };

        var edges = new List<MermaidEdge>();

        foreach (var (diagnostic, index) in result.Diagnostics
                     .OrderBy(value => value.Code, StringComparer.Ordinal)
                     .ThenBy(value => value.Message, StringComparer.Ordinal)
                     .Select((value, index) => (value, index))) {
            var id = $"diagnostic_{index}";

            nodes.Add(new MermaidNode(
                1,
                id,
                Label(diagnostic.Severity.ToString(), diagnostic.Code, diagnostic.Message),
                "diagnostic"));

            edges.Add(new MermaidEdge(0, id, "result_status", id, string.Empty));
        }

        return new MermaidDiagram(nodes, edges);
    }
}
