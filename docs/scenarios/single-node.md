# Single Node

The single-node scenario exercises one machine group that converts an unlimited external ore supply into a targeted plate output. Its named cases cover whole and fractional machine requirements without duplicating the graph or expected result.

Coverage:

- Graph shape: single node.
- Planning mode: target-driven.
- Objective: meet targets while minimizing external supply.
- Constraint: output target.
- Resource condition: unlimited input with balanced output.
- Status: optimal.

Files:

- [Fixture](../../tests/FactoryGraph.Tests/Scenarios/Framework/SingleNode/scenario.json)
- [Initial diagram](../../tests/FactoryGraph.Tests/Scenarios/Framework/SingleNode/initial.mmd)
- [Expected diagram](../../tests/FactoryGraph.Tests/Scenarios/Framework/SingleNode/expected.mmd)
