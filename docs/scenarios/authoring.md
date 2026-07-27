# Scenario Authoring

Executable scenarios are the specification for processor behavior. The catalog grows from minimal graphs to interacting constraints and cycles while avoiding cases that repeat behavior without adding coverage.

## Required Contents

Each scenario provides:

- A machine-readable, self-contained solve request.
- A complete machine-readable expected result.
- Explicit numeric tolerances where exact comparison is inappropriate.
- Initial and expected Mermaid diagrams for human review.
- A dedicated page under `docs/scenarios` explaining the behavior and linking its fixtures.

Expected results include every status, diagnostic, node calculation, connection flow, and boundary flow relevant to the scenario. Assertion failures identify the scenario, graph element, metric, expected value, and actual value.

Initially, diagrams may be written by hand. Once fixture-to-exporter generation is available, diagrams are generated from the machine-readable request and expected result through `FactoryGraph.Exporter.Mermaid` and reviewed alongside fixture changes.

## Coverage Dimensions

The scenario catalog makes these dimensions explicit:

- Graph shape, including single nodes, chains, fan-in, fan-out, splits, merges, disconnected sections, and cycles.
- Planning mode and objective.
- Machine, supply, demand, target, and connection constraints.
- Resource conditions such as surplus, deficit, and limited supply.
- Feasible, invalid, infeasible, and unbounded outcomes.
- Numeric behavior including zero, fractions, near-zero normalization, tolerance boundaries, large magnitude differences, and machine-count ceiling.

Use parameterized or combinatorial cases when they expose meaningful interactions. Prefer one clearly named scenario over multiple equivalent fixtures.

The executable fixture schema and runner will be introduced with the scenario-framework work. Do not establish an alternative ad hoc fixture format in feature tests.
