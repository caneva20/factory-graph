# Recycling Statuses

The recycling-statuses scenario varies catalyst replenishment around a `1e-9` near-zero threshold. Zero, below-threshold, threshold, and above-threshold supplies remain too small for the target-driven steady-state requirement and return `Infeasible`. The startup flag changes only above the threshold. A perfect recycle without a production bound returns `Unbounded` in capacity-driven mode.

Coverage:

- Graph shape: one-machine self-loop.
- Planning modes: target-driven and capacity-driven.
- Numeric cases: zero, below-threshold, exact-threshold, and above-threshold supply capacity.
- Cycle analysis: threshold-aware structural startup classification.
- Statuses: infeasible and unbounded.

Files:

- [Fixture](../../tests/FactoryGraph.Tests/Scenarios/Cycles/Statuses/scenario.json)
- [Initial diagram](../../tests/FactoryGraph.Tests/Scenarios/Cycles/Statuses/initial.mmd)
- [Expected diagram](../../tests/FactoryGraph.Tests/Scenarios/Cycles/Statuses/expected.mmd)
