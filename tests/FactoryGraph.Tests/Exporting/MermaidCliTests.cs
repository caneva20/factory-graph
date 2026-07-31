using System.Text.Json;
using System.Text.Json.Nodes;
using FactoryGraph.Exporter.Mermaid;
using FactoryGraph.Exporter.Mermaid.Cli;
using FactoryGraph.Tests.ScenarioFramework;
using FactoryGraph.Validation;

namespace FactoryGraph.Tests.Exporting;

public sealed class MermaidCliTests {
    [Fact]
    public async Task RequestFileProducesComputedDiagramOnStandardOutput() {
        var scenario = ScenarioLoader.Load("Framework/SimpleChain", "balanced");
        var inputPath = Path.GetTempFileName();

        try {
            await File.WriteAllTextAsync(
                inputPath,
                JsonSerializer.Serialize(scenario.Request, ScenarioJson.Options),
                TestContext.Current.CancellationToken);

            var result = await RunAsync([inputPath]);

            Assert.Equal(0, result.ExitCode);
            Assert.Equal(Golden("simple-chain-result.mmd"), result.Output);
            Assert.Empty(result.Error);
        } finally {
            File.Delete(inputPath);
        }
    }

    [Fact]
    public async Task InitialModeAndRenderingOptionsApplyToStandardInput() {
        var scenario = ScenarioLoader.Load("Framework/SimpleChain", "balanced");

        var expected = new MermaidExporter(new MermaidExportOptions {
            Direction = MermaidFlowchartDirection.TopToBottom,
            NumberFormat = "0.00",
            IncludeCapacities = false,
            IncludeStyles = false
        }).Export(scenario.Request);

        var result = await RunAsync(
            ["-", "--initial", "--direction", "tb", "--number-format", "0.00", "--no-capacities", "--no-styles"],
            JsonSerializer.Serialize(scenario.Request, ScenarioJson.Options));

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(expected, result.Output);
        Assert.Empty(result.Error);
    }

    [Fact]
    public async Task OutputOptionWritesDiagramToFile() {
        var scenario = ScenarioLoader.Load("Framework/SimpleChain", "balanced");
        var outputPath = Path.GetTempFileName();

        try {
            var result = await RunAsync(
                ["-", "--output", outputPath],
                JsonSerializer.Serialize(scenario.Request, ScenarioJson.Options));

            Assert.Equal(0, result.ExitCode);
            Assert.Empty(result.Output);
            Assert.Empty(result.Error);

            Assert.Equal(
                Golden("simple-chain-result.mmd"),
                await File.ReadAllTextAsync(outputPath, TestContext.Current.CancellationToken));
        } finally {
            File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task InvalidRequestWritesDiagnosticToStandardError() {
        var scenario = ScenarioLoader.Load("Validation/InvalidRequests", "duplicate-item-id");

        var result = await RunAsync(["-"], JsonSerializer.Serialize(scenario.Request, ScenarioJson.Options));

        Assert.Equal(1, result.ExitCode);
        Assert.Empty(result.Output);
        Assert.Contains("InvalidRequest", result.Error);
        Assert.Contains(ValidationDiagnosticCodes.Id.Duplicate, result.Error);
    }

    [Fact]
    public async Task MalformedJsonWritesErrorToStandardError() {
        var result = await RunAsync(["-"], "{ invalid");

        Assert.Equal(1, result.ExitCode);
        Assert.Empty(result.Output);
        Assert.Contains("Invalid request JSON", result.Error);
    }

    [Fact]
    public async Task ScenarioDirectoryProducesOneDiagramPerScenarioWithEveryCase() {
        var scenarioRoot = Path.Combine(AppContext.BaseDirectory, "Scenarios");
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"factory-graph-mermaid-{Guid.NewGuid():N}");

        try {
            var result = await RunAsync(["--scenarios", scenarioRoot, "--output-dir", outputDirectory]);

            Assert.Equal(0, result.ExitCode);
            Assert.Empty(result.Output);
            Assert.Empty(result.Error);

            var files = Directory.EnumerateFiles(outputDirectory, "*.mmd", SearchOption.AllDirectories)
                .Select(path => Path.GetRelativePath(outputDirectory, path).Replace(Path.DirectorySeparatorChar, '/'))
                .Order(StringComparer.Ordinal)
                .ToArray();

            Assert.Equal([
                "CapacityDriven/BranchedAssembly.mmd",
                "CapacityDriven/ConstrainedChain.mmd",
                "CapacityDriven/SingleNode.mmd",
                "Cycles/ConnectedCycles.mmd",
                "Cycles/SelfLoop.mmd",
                "Cycles/SimpleLoop.mmd",
                "Cycles/Statuses.mmd",
                "Framework/BranchedAssembly.mmd",
                "Framework/ObjectiveTradeoff.mmd",
                "Framework/ResourceLimits.mmd",
                "Framework/SimpleChain.mmd",
                "Framework/SingleNode.mmd",
                "Validation/InvalidRequests.mmd"
            ], files);

            var objective = await File.ReadAllTextAsync(
                Path.Combine(outputDirectory, "Framework", "ObjectiveTradeoff.mmd"),
                TestContext.Current.CancellationToken);

            Assert.Equal(2, CountSections(objective));
            Assert.Contains("minimize-external-supply", objective);
            Assert.Contains("minimize-surplus", objective);

            var validation = await File.ReadAllTextAsync(
                Path.Combine(outputDirectory, "Validation", "InvalidRequests.mmd"),
                TestContext.Current.CancellationToken);

            Assert.Equal(9, CountSections(validation));
            Assert.Contains(ValidationDiagnosticCodes.Id.Duplicate, validation);
            Assert.Contains("status: InvalidRequest", validation);
        } finally {
            if (Directory.Exists(outputDirectory)) {
                Directory.Delete(outputDirectory, true);
            }
        }
    }

    [Fact]
    public async Task ScenarioWithUnusedParameterIsRejectedBeforeWritingOutput() {
        var sourcePath = Path.Combine(AppContext.BaseDirectory, "Scenarios", "Framework", "SimpleChain", "scenario.json");
        var workingDirectory = Path.Combine(Path.GetTempPath(), $"factory-graph-mermaid-{Guid.NewGuid():N}");
        var scenarioDirectory = Path.Combine(workingDirectory, "SimpleChain");
        var scenarioPath = Path.Combine(scenarioDirectory, "scenario.json");
        var outputDirectory = Path.Combine(workingDirectory, "output");

        try {
            Directory.CreateDirectory(scenarioDirectory);

            var document = Assert.IsType<JsonObject>(JsonNode.Parse(
                await File.ReadAllTextAsync(sourcePath, TestContext.Current.CancellationToken)));

            var cases = Assert.IsType<JsonArray>(document["cases"]);
            var firstCase = Assert.IsType<JsonObject>(cases[0]);
            var parameters = Assert.IsType<JsonObject>(firstCase["parameters"]);
            parameters["unused"] = 1;

            await File.WriteAllTextAsync(
                scenarioPath,
                document.ToJsonString(ScenarioJson.Options),
                TestContext.Current.CancellationToken);

            var result = await RunAsync(["--scenarios", scenarioPath, "--output-dir", outputDirectory]);

            Assert.Equal(1, result.ExitCode);
            Assert.Empty(result.Output);
            Assert.Contains("unused parameters: unused", result.Error);
            Assert.False(Directory.Exists(outputDirectory));
        } finally {
            if (Directory.Exists(workingDirectory)) {
                Directory.Delete(workingDirectory, true);
            }
        }
    }

    [Theory]
    [InlineData(new string[0], "Specify a request JSON input")]
    [InlineData(new[] { "--unknown" }, "Unknown option")]
    [InlineData(new[] { "-", "--direction", "down" }, "requires lr or tb")]
    [InlineData(new[] { "--scenarios", "Scenarios" }, "requires --output-dir")]
    public async Task ArgumentErrorsShowUsage(string[] arguments, string expectedError) {
        var result = await RunAsync(arguments);

        Assert.Equal(1, result.ExitCode);
        Assert.Empty(result.Output);
        Assert.Contains(expectedError, result.Error);
        Assert.Contains("Usage: factory-graph-mermaid", result.Error);
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunAsync(
        IReadOnlyList<string> arguments,
        string input = "") {
        using var reader = new StringReader(input);
        await using var output = new StringWriter();
        await using var error = new StringWriter();

        var exitCode = await MermaidCli.RunAsync(arguments, reader, output, error);

        return (exitCode, output.ToString(), error.ToString());
    }

    private static string Golden(string fileName) {
        var path = Path.Combine(AppContext.BaseDirectory, "ExporterGoldens", fileName);

        return File.ReadAllText(path).ReplaceLineEndings("\n");
    }

    private static int CountSections(string diagram) {
        return diagram.Split("    subgraph ").Length - 1;
    }
}
