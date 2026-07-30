# Capacity-Driven Branched Assembly

The branched-assembly scenario splits one raw output between two processing branches, then merges both parts in a multi-input recipe. A shared multiplier doubles targets in a 2:1 product-to-coproduct ratio. The source produces more coproduct than the ratio permits, so the remaining flow leaves through a surplus outlet.

Coverage:

- Graph shape: fan-in, fan-out, split, and merge.
- Planning mode: capacity-driven.
- Objective: maximize target output.
- Constraints: multiple proportional targets and machine-group activity capacity.
- Resource conditions: multi-input recipe, multi-output recipe, target surplus, and coproduct surplus.
- Status: optimal.

Files:

- [Fixture](../../tests/FactoryGraph.Tests/Scenarios/CapacityDriven/BranchedAssembly/scenario.json)
- [Initial diagram](../../tests/FactoryGraph.Tests/Scenarios/CapacityDriven/BranchedAssembly/initial.mmd)
- [Expected diagram](../../tests/FactoryGraph.Tests/Scenarios/CapacityDriven/BranchedAssembly/expected.mmd)
