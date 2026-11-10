# Contributing

## Development Workflow

Behavioral work follows red-green-refactor:

1. Add or change an executable scenario that demonstrates the required behavior.
2. Run the focused test and confirm that it fails for the expected reason.
3. Make the smallest production change that passes the scenario.
4. Run the complete test suite and refactor while it remains green.

Every defect fix retains its failing scenario as a regression test.

Focused unit tests should normally verify one behavior with one explicit assertion. If a test needs unrelated assertions, split it. Scenario tests call one complete-result assertion helper; that helper compares the full result and reports all relevant element and metric differences.

## Issue Tracking

Track FactoryGraph work in this repository's [issue tracker](https://github.com/caneva20/factory-graph/issues). The shared [Factory Planner Project](https://github.com/users/caneva20/projects/3) coordinates work across repositories.

Use local references such as `#18` for FactoryGraph issues. Qualify cross-repository references, such as `caneva20/factory-planner#41`. Use `Closes #18` when merging the change should close a FactoryGraph issue.

## Scenario Fixtures

Machine-readable scenarios live under `tests/FactoryGraph.Tests/Scenarios` and are grouped by behavior rather than implementation class. Each scenario also has a dedicated page under `docs/scenarios`.

Each scenario folder contains `scenario.json`, `initial.mmd`, and `expected.mmd`. The shared runner discovers named fixture cases, passes each request to the processor, and compares the complete result using the fixture's numeric policy. Use named parameter sets for meaningful variations instead of copying fixtures.

Use [docs/scenarios.md](docs/scenarios.md) as the catalog entry point. The [scenario authoring guide](docs/scenarios/authoring.md) defines the JSON format, fixture contents, coverage dimensions, diagrams, and red-green-refactor workflow.

## Packages and Versions

The public package IDs are:

- `FactoryGraph`
- `FactoryGraph.Exporter.Mermaid`
- `FactoryGraph.Exporter.Mermaid.Cli`

The repository releases all three packages together. Release tags use `vMAJOR.MINOR.PATCH`, and package versions omit the `v` prefix. [Packages and Versioning](docs/versioning.md) defines the compatibility and support policy.

Regular CI packs all three projects, verifies package metadata and contents, and runs clean-consumer tests without publishing. Run the same checks locally with:

```console
version="$(./eng/package-version.sh)"
dotnet pack FactoryGraph.slnx --configuration Release
./eng/verify-packages.sh artifacts/packages "$version"
./eng/test-packages.sh artifacts/packages "$version"
```

### Local package feed

NuGet.org takes a while to index a new push. To use unpublished changes from another project, pack into a local feed:

```console
./eng/pack-local.sh
```

The script packs a `<VersionPrefix>-local.<timestamp>` version, verifies it, runs the clean-consumer test, and replaces the previous local packages in `artifacts/local-packages`. Pass `--skip-smoke` to skip the consumer test. Register the folder once per machine:

```console
dotnet nuget add source "$PWD/artifacts/local-packages" --name factorygraph-local
```

A consumer opts in by passing the printed version, for example `dotnet restore -p:FactoryGraphVersion=0.2.0-local.20261109T153000`, if its project reads that property.

The release workflow publishes a verified `vMAJOR.MINOR.PATCH` tag to NuGet.org through Trusted Publishing. Configure the `release` environment and its `NUGET_USER` secret before creating a release tag. NuGet package versions are immutable.

## Benchmarks

Benchmarks use BenchmarkDotNet and belong in `benchmarks/FactoryGraph.Benchmarks`. CI compiles benchmarks but does not use timing results as pass/fail gates. Benchmark execution, retained baselines, profiling, and performance budgets require controlled, documented environments.
