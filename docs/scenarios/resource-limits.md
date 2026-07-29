# Resource Limits

The resource-limits scenario sets one capacity to 4 while the target requires a flow of 5. Six cases apply the limit to machine count, activity, connection, supply, product outlet, or surplus outlet capacity.

Coverage:

- Graph shape: simple chain with a co-product.
- Planning mode: target-driven.
- Objective: meet targets while minimizing external supply.
- Constraints: machine count, activity, connection, supply, product outlet, and surplus outlet capacities.
- Resource condition: insufficient capacity.
- Status: infeasible.

Files:

- [Fixture](../../tests/FactoryGraph.Tests/Scenarios/Framework/ResourceLimits/scenario.json)
- [Initial diagram](../../tests/FactoryGraph.Tests/Scenarios/Framework/ResourceLimits/initial.mmd)
- [Expected diagram](../../tests/FactoryGraph.Tests/Scenarios/Framework/ResourceLimits/expected.mmd)
