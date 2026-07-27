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

Issues are tracked in [`caneva20/factory-planner`](https://github.com/caneva20/factory-planner/issues), not in this repository. Use fully qualified references such as `caneva20/factory-planner#28` in commits and pull requests. Use `Closes caneva20/factory-planner#28` when merging the change should close the issue.

## Scenario Fixtures

Machine-readable scenarios live under `tests/FactoryGraph.Tests/Scenarios` and are grouped by behavior rather than implementation class. Each scenario also has a dedicated page under `docs/scenarios`.

The executable fixture schema and runner will be introduced with the scenario-framework work. Do not establish an alternative ad hoc fixture format in feature tests.

Use [docs/scenarios.md](docs/scenarios.md) as the catalog entry point. The [scenario authoring guide](docs/scenarios/authoring.md) defines fixture contents, coverage dimensions, diagrams, and case-selection guidance.

## Packages and Versions

The public package IDs are:

- `FactoryGraph`
- `FactoryGraph.Exporter.Mermaid`

The repository follows Semantic Versioning. Release tags use `vMAJOR.MINOR.PATCH`, and package versions omit the `v` prefix. Before `1.0.0`, incompatible public API changes increment the minor version. Compatible fixes increment the patch version.

Regular CI builds package artifacts for inspection but never publishes them. Release publication, package-content verification, symbols, compatibility policy, and release documentation are completed by the release work tracked separately from repository bootstrap.

## Benchmarks

Benchmarks use BenchmarkDotNet and belong in `benchmarks/FactoryGraph.Benchmarks`. CI compiles benchmarks but does not use timing results as pass/fail gates. Benchmark execution, retained baselines, profiling, and performance budgets require controlled, documented environments.
