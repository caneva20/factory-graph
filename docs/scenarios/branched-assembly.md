# Branched Assembly

The branched-assembly scenario combines two raw-material branches, a multi-input merge, an intermediate-output split, two targets, a co-product, and a disconnected machine group. Several capacities equal the required solved rates.

Coverage:

- Graph shape: fan-in and fan-out.
- Planning mode: target-driven.
- Objective: meet targets while minimizing external supply.
- Constraints: machine count, activity, connection, supply, product outlet, surplus outlet, and multiple targets.
- Resource conditions: binding capacities, co-product surplus, and a disconnected section.
- Status: optimal.

Files:

- [Fixture](../../tests/FactoryGraph.Tests/Scenarios/Framework/BranchedAssembly/scenario.json)
- [Initial diagram](../../tests/FactoryGraph.Tests/Scenarios/Framework/BranchedAssembly/initial.mmd)
- [Expected diagram](../../tests/FactoryGraph.Tests/Scenarios/Framework/BranchedAssembly/expected.mmd)
