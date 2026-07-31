# Recycling Self-Loop

The recycling self-loop consumes catalyst and ore, recovers 75% of the catalyst, and produces one product per activity. At four activities per time unit, the cycle carries three catalyst and external supply replaces the missing one. Target-driven and bounded capacity-driven cases produce the same complete result.

Coverage:

- Graph shape: one-machine self-loop.
- Planning modes: target-driven and capacity-driven.
- Resource condition: fractional recycle with partial external replenishment.
- Cycle analysis: one component that can start from external catalyst.
- Status: optimal.

Files:

- [Fixture](../../tests/FactoryGraph.Tests/Scenarios/Cycles/SelfLoop/scenario.json)
- [Initial diagram](../../tests/FactoryGraph.Tests/Scenarios/Cycles/SelfLoop/initial.mmd)
- [Expected diagram](../../tests/FactoryGraph.Tests/Scenarios/Cycles/SelfLoop/expected.mmd)
