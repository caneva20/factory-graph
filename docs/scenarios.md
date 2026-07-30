# Scenarios

Each executable scenario has a dedicated page under `docs/scenarios` with its purpose, machine-readable fixtures, and initial and expected Mermaid diagrams.

Scenario diagrams are hand-authored explanations. Exporter expected-output files under `tests/FactoryGraph.Tests/ExporterGoldens` test the exact generated Mermaid format and do not replace these diagrams.

See the [scenario authoring guide](scenarios/authoring.md) for required fixture contents, coverage dimensions, diagrams, and case-selection guidance.

## Index

- [Single node](scenarios/single-node.md) proves fixture loading, parameterized cases, runner execution, and complete-result comparison on one machine group.
- [Simple chain](scenarios/simple-chain.md) proves the same path for a connection between two machine groups.
- [Branched assembly](scenarios/branched-assembly.md) covers fan-in, fan-out, multiple targets, co-product surplus, binding capacities, and a disconnected section.
- [Objective tradeoff](scenarios/objective-tradeoff.md) verifies route selection under both target-driven objectives.
- [Resource limits](scenarios/resource-limits.md) verifies infeasible machine, connection, supply, product-outlet, and surplus-outlet capacities.
- [Capacity-driven single node](scenarios/capacity-driven-single-node.md) compares achieved output below and above the expected target rate.
- [Capacity-driven constrained chain](scenarios/capacity-driven-constrained-chain.md) varies every supported capacity across a two-stage chain and verifies bottleneck reporting.
- [Capacity-driven branched assembly](scenarios/capacity-driven-branched-assembly.md) covers proportional multiple targets, fan-in, fan-out, and coproduct surplus.
