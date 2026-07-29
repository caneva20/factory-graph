# Simple Chain

The simple-chain scenario exercises two machine groups connected by an item-specific plate flow. The expected result covers both machine groups, the connection, external supply, product outlet, and target.

Coverage:

- Graph shape: simple chain.
- Planning mode: target-driven.
- Objective: meet targets while minimizing external supply.
- Constraint: output target.
- Resource condition: unlimited input with balanced intermediate and final output.
- Status: optimal.

Files:

- [Fixture](../../tests/FactoryGraph.Tests/Scenarios/Framework/SimpleChain/scenario.json)
- [Initial diagram](../../tests/FactoryGraph.Tests/Scenarios/Framework/SimpleChain/initial.mmd)
- [Expected diagram](../../tests/FactoryGraph.Tests/Scenarios/Framework/SimpleChain/expected.mmd)
