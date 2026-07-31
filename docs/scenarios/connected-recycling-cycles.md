# Connected Recycling Cycles

This scenario connects a three-machine cycle to a downstream self-loop. A configured startup supply makes the first cycle structurally startable, and its token output makes the downstream cycle startable. The supply is unused in the steady-state solution, which separates startup classification from solved flow.

Coverage:

- Graph shape: one multi-node component connected to a self-loop.
- Planning modes: target-driven and capacity-driven.
- Targets: two outputs in a 1:4 ratio.
- Cycle analysis: separate components, internal connections, declaration order, and propagated startability.
- Status: optimal.

Files:

- [Fixture](../../tests/FactoryGraph.Tests/Scenarios/Cycles/ConnectedCycles/scenario.json)
- [Initial diagram](../../tests/FactoryGraph.Tests/Scenarios/Cycles/ConnectedCycles/initial.mmd)
- [Expected diagram](../../tests/FactoryGraph.Tests/Scenarios/Cycles/ConnectedCycles/expected.mmd)
