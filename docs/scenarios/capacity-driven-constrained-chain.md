# Capacity-Driven Constrained Chain

The constrained-chain scenario limits a two-stage process to 4 units against an expected target rate of 5. Six cases apply the limit through machine count, activity, connection, supply, product-outlet, or coproduct surplus-outlet capacity. Machine, connection, and supply cases also verify stable-ID bottleneck reporting.

Coverage:

- Graph shape: simple chain with a coproduct.
- Planning mode: capacity-driven.
- Objective: maximize target output.
- Constraints: machine count, activity, connection, supply, product outlet, and surplus outlet capacities.
- Resource conditions: target deficit, coproduct surplus, and binding capacity.
- Status: optimal.

Files:

- [Fixture](../../tests/FactoryGraph.Tests/Scenarios/CapacityDriven/ConstrainedChain/scenario.json)
- [Initial diagram](../../tests/FactoryGraph.Tests/Scenarios/CapacityDriven/ConstrainedChain/initial.mmd)
- [Expected diagram](../../tests/FactoryGraph.Tests/Scenarios/CapacityDriven/ConstrainedChain/expected.mmd)
