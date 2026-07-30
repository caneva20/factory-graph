using System.Text;

namespace FactoryGraph.Exporter.Mermaid;

internal static class MermaidWriter {
    public static string Write(MermaidDiagram diagram, MermaidExportOptions options) {
        var nodes = diagram.Nodes.OrderBy(node => node.Order).ThenBy(node => node.Id, StringComparer.Ordinal).ToArray();
        var edges = diagram.Edges.OrderBy(edge => edge.Order).ThenBy(edge => edge.Id, StringComparer.Ordinal).ToArray();
        var builder = new StringBuilder();

        builder.Append("flowchart ").Append(Direction(options.Direction)).Append('\n');

        foreach (var node in nodes) {
            builder.Append("    ").Append(node.Id).Append("[\"").Append(node.Label).Append("\"]\n");
        }

        foreach (var edge in edges) {
            builder.Append("    ").Append(edge.SourceId).Append(edge.IsDotted ? " -.->" : " -->");

            if (edge.Label.Length > 0) {
                builder.Append("|\"").Append(edge.Label).Append("\"|");
            }

            builder.Append(' ').Append(edge.DestinationId).Append('\n');
        }

        if (options.IncludeStyles) {
            WriteStyles(builder, nodes, edges);
        }

        return builder.ToString();
    }

    public static string Write(IReadOnlyList<MermaidDiagramSection> sections, MermaidExportOptions options) {
        var builder = new StringBuilder();
        var allNodes = new List<MermaidNode>();
        var allEdges = new List<MermaidEdge>();

        builder.Append("flowchart ").Append(Direction(options.Direction)).Append('\n');

        for (var sectionIndex = 0; sectionIndex < sections.Count; sectionIndex++) {
            var section = sections[sectionIndex];
            var prefix = $"section_{sectionIndex}_";

            var nodes = section.Diagram.Nodes
                .Select(node => node with { Id = prefix + node.Id })
                .OrderBy(node => node.Order)
                .ThenBy(node => node.Id, StringComparer.Ordinal)
                .ToArray();

            var edges = section.Diagram.Edges
                .Select(edge => edge with {
                    Id = prefix + edge.Id,
                    SourceId = prefix + edge.SourceId,
                    DestinationId = prefix + edge.DestinationId
                })
                .OrderBy(edge => edge.Order)
                .ThenBy(edge => edge.Id, StringComparer.Ordinal)
                .ToArray();

            builder.Append("    subgraph section_").Append(sectionIndex).Append("[\"")
                .Append(MermaidText.Label(section.Name)).Append("\"]\n");

            foreach (var node in nodes) {
                builder.Append("        ").Append(node.Id).Append("[\"").Append(node.Label).Append("\"]\n");
            }

            foreach (var edge in edges) {
                builder.Append("        ").Append(edge.SourceId).Append(edge.IsDotted ? " -.->" : " -->");

                if (edge.Label.Length > 0) {
                    builder.Append("|\"").Append(edge.Label).Append("\"|");
                }

                builder.Append(' ').Append(edge.DestinationId).Append('\n');
            }

            builder.Append("    end\n");
            allNodes.AddRange(nodes);
            allEdges.AddRange(edges);
        }

        if (options.IncludeStyles) {
            WriteStyles(builder, allNodes, allEdges);
        }

        return builder.ToString();
    }

    private static string Direction(MermaidFlowchartDirection direction) {
        return direction switch {
            MermaidFlowchartDirection.LeftToRight => "LR",
            MermaidFlowchartDirection.TopToBottom => "TB",
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unsupported Mermaid flowchart direction.")
        };
    }

    private static void WriteStyles(StringBuilder builder, IReadOnlyList<MermaidNode> nodes, IReadOnlyList<MermaidEdge> edges) {
        builder.Append("    classDef machine fill:#f5f1e8,stroke:#4a5568\n");
        builder.Append("    classDef supply fill:#e6f4ea,stroke:#2f855a\n");
        builder.Append("    classDef product fill:#e8f0fe,stroke:#2b6cb0\n");
        builder.Append("    classDef surplus fill:#fff4e5,stroke:#b7791f\n");
        builder.Append("    classDef target fill:#f3e8ff,stroke:#805ad5\n");
        builder.Append("    classDef bottleneck stroke:#c53030,stroke-width:3px\n");

        if (nodes.Any(node => node.Style == "status")) {
            builder.Append("    classDef status fill:#edf2f7,stroke:#4a5568\n");
        }

        if (nodes.Any(node => node.Style == "diagnostic")) {
            builder.Append("    classDef diagnostic fill:#fff5f5,stroke:#c53030\n");
        }

        foreach (var style in nodes.Select(node => node.Style).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)) {
            var ids = nodes.Where(node => node.Style == style).Select(node => node.Id);
            builder.Append("    class ").AppendJoin(',', ids).Append(' ').Append(style).Append('\n');
        }

        var bottleneckNodes = nodes.Where(node => node.IsBottleneck).Select(node => node.Id).ToArray();

        if (bottleneckNodes.Length > 0) {
            builder.Append("    class ").AppendJoin(',', bottleneckNodes).Append(" bottleneck\n");
        }

        var bottleneckEdges = edges
            .Select((edge, index) => (edge, index))
            .Where(value => value.edge.IsBottleneck)
            .Select(value => value.index)
            .ToArray();

        if (bottleneckEdges.Length > 0) {
            builder.Append("    linkStyle ").AppendJoin(',', bottleneckEdges).Append(" stroke:#c53030,stroke-width:3px\n");
        }
    }
}
