# Objective Tradeoff

The objective-tradeoff scenario sends one product through either a supply-efficient route that creates waste or a surplus-free route that consumes more external material. Its two cases verify that each supported target-driven objective selects the expected route.

Coverage:

- Graph shape: alternate routes merging into one final machine group.
- Planning mode: target-driven.
- Objectives: minimize external supply and minimize surplus.
- Constraint: output target.
- Resource condition: external-supply versus surplus tradeoff.
- Status: optimal.

Files:

- [Fixture](../../tests/FactoryGraph.Tests/Scenarios/Framework/ObjectiveTradeoff/scenario.json)
- [Initial diagram](../../tests/FactoryGraph.Tests/Scenarios/Framework/ObjectiveTradeoff/initial.mmd)
- [Expected diagram](../../tests/FactoryGraph.Tests/Scenarios/Framework/ObjectiveTradeoff/expected.mmd)
