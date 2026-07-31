# Simple Recycling Loop

The simple recycling loop connects a reactor and a catalyst-recovery group. The reactor produces the intermediate consumed by recovery, and recovery returns the catalyst consumed by the reactor. Both planning modes solve the two flows simultaneously. External raw material alone cannot start either group, so the component requires startup inventory.

Coverage:

- Graph shape: two-machine strongly connected component.
- Planning modes: target-driven and capacity-driven.
- Resource condition: sustainable perfect recycle with external raw material.
- Cycle analysis: ordered machine and internal-connection IDs with a startup requirement.
- Status: optimal.

Files:

- [Fixture](../../tests/FactoryGraph.Tests/Scenarios/Cycles/SimpleLoop/scenario.json)
- [Initial diagram](../../tests/FactoryGraph.Tests/Scenarios/Cycles/SimpleLoop/initial.mmd)
- [Expected diagram](../../tests/FactoryGraph.Tests/Scenarios/Cycles/SimpleLoop/expected.mmd)
