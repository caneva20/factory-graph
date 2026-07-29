# Scenario Authoring

Executable scenarios are the specification for processor behavior. The catalog grows from minimal graphs to interacting constraints and cycles while avoiding cases that repeat behavior without adding coverage.

## Required Contents

Each scenario provides:

- A `scenario.json` fixture containing a self-contained solve request and complete expected result.
- Explicit numeric tolerances in `request.options.numericPolicy`.
- Named cases with typed parameter values when one fixture covers meaningful variations.
- Hand-authored `initial.mmd` and `expected.mmd` diagrams for human review.
- A dedicated page under `docs/scenarios` explaining the behavior and linking its fixtures.

Expected results include every status, diagnostic, node calculation, connection flow, and boundary flow relevant to the scenario. Assertion failures identify the scenario, graph element, metric, expected value, and actual value.

Store fixtures under `tests/FactoryGraph.Tests/Scenarios/<behavior>/<scenario>`. The shared xUnit data source discovers every `scenario.json` in that tree and reports each named case as a separate theory row. Scenario tests pass a `SolveRequest` to the processor and compare its `SolveResult` through the shared runner.

## Fixture Format

The top-level JSON object contains:

- `name` and `description` for test output and documentation.
- `dimensions.graphShape`, `dimensions.constraints`, and `dimensions.resourceConditions` for coverage review.
- `request`, containing a complete `SolveRequest`.
- `expected`, containing a complete `SolveResult`.
- `cases`, containing one or more named parameter maps.

JSON uses camel-case contract property names and camel-case enum values. The loader rejects missing required properties and properties that do not belong to the contract. Use the JSON strings `"NaN"`, `"Infinity"`, and `"-Infinity"` for non-finite numeric validation cases.

An exact string such as `"${rate}"` in `request` or `expected` is a typed parameter placeholder. Each case must provide every referenced parameter and may not provide unused parameters. The loader substitutes the JSON value before deserializing the contracts, so values retain their number, string, Boolean, object, array, or null type.

```json
{
  "name": "example",
  "description": "Describes the behavior under test.",
  "dimensions": {
    "graphShape": "single-node",
    "constraints": ["target"],
    "resourceConditions": ["unlimited-supply"]
  },
  "request": {
    "targets": [{ "rate": "${rate}" }]
  },
  "expected": {
    "status": "optimal"
  },
  "cases": [
    { "name": "whole-rate", "parameters": { "rate": 4 } },
    { "name": "fractional-rate", "parameters": { "rate": 2.5 } }
  ]
}
```

The shortened example shows the fixture structure; committed requests and expected results must contain every required contract property.

The assertion helper compares identity-bearing collections by `id`, item-rate collections by `itemId`, diagnostics by `code`, and bottlenecks by kind and graph element. It compares other arrays in fixture order. Numeric comparisons normalize near-zero values and apply the tolerances from `request.options.numericPolicy`; all other values require exact equality.

Diagrams remain independent review artifacts. Do not invoke exporter code while loading or running a scenario. Once fixture-to-exporter generation exists, contributors may regenerate diagrams explicitly and review the changes.

## Workflow

1. Add or change a fixture and its diagrams.
2. Add the fixture to the scenario index and document the behavior.
3. Run the focused scenario and confirm that it fails for the expected metric.
4. Implement the behavior and rerun the focused scenario.
5. Run the complete test suite, then refactor while it remains green.

## Coverage Dimensions

The scenario catalog makes these dimensions explicit:

- Graph shape, including single nodes, chains, fan-in, fan-out, splits, merges, disconnected sections, and cycles.
- Planning mode and objective.
- Machine, supply, outlet, target, and connection constraints.
- Resource conditions such as surplus, deficit, and limited supply.
- Feasible, invalid, infeasible, and unbounded outcomes.
- Numeric behavior including zero, fractions, near-zero normalization, tolerance boundaries, large magnitude differences, and machine-count ceiling.

Use parameterized or combinatorial cases when they expose meaningful interactions. Prefer one clearly named scenario over multiple equivalent fixtures.
