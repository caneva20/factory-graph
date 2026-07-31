# Capacity-Driven Planning

The README explains the [factory graph](../README.md#graph-concepts) and [solving concepts](../README.md#solving-a-graph) used here. This document defines the capacity-driven optimization model.

`FactoryGraphProcessor` computes the largest target mix allowed by machine, flow, and boundary capacities.

## Proportional Targets

Target rates define an expected output mix. The solver creates one nonnegative multiplier, λ, and requires:

```text
achieved target rate = requested target rate * λ
```

It maximizes λ without capping it at 1. For target rates of 10 plates and 5 screws:

| λ | Plates | Screws | Result against expectation |
|---:|---:|---:|---|
| 0.6 | 6 | 3 | Both targets report deficits. |
| 1 | 10 | 5 | Both targets meet their expected rates. |
| 1.6 | 16 | 8 | Both targets report surplus. |

Scaling every requested rate by the same factor does not change the physical plan. Requests for 10:5, 2:1, and 20:10 select the same output mix; only λ changes.

At least one target rate must be positive. A zero-rate target stays at zero while positive targets scale. Recipe output speed does not determine target proportions. When a recipe creates an extra coproduct, the graph must route it through another product outlet or a surplus outlet.

## Model

The solver uses the same nonnegative activity and flow variables, exact item balances, and capacity bounds as [target-driven planning](target-driven-planning.md). It adds λ and one proportional equality for each target.

The shared balance model solves acyclic paths and recycling cycles simultaneously. Cyclic flow contributes to the same target multiplier and capacity constraints as other flow.

The primary solve maximizes λ. The solver then fixes λ at its optimum and minimizes total machine activity. This leaves disconnected and optional production inactive. If no capacity bounds the target-producing paths, the result is `Unbounded`.

## Results

Optimal and feasible results use the same computed graph as target-driven planning. Target surplus and deficit compare achieved output with the requested λ = 1 baseline.

Unused capacity follows from configured and computed values:

- unused machine count is `AvailableMachineCount - EffectiveMachineCount`;
- unused activity capacity is `ActivityCapacity - Activity`; and
- unused connection or supply capacity is `Capacity - FlowRate` or `Capacity - ProvidedRate`.

## Bottlenecks

The solver reports saturated machine-group, connection, and supply capacities on reverse paths from positive targets. Entries retain request order and identify elements by their stable IDs.

For a machine group with both `AvailableMachineCount` and `ActivityCapacity`, the tighter effective activity limit determines the bottleneck. Equal limits report the machine count. Product-outlet and surplus-outlet capacities still constrain the plan, but the current `BottleneckKind` contract does not represent them.

## Status And Time Limit

FactoryGraph maps HiGHS statuses to `Optimal`, `Feasible`, `Infeasible`, `Unbounded`, or `Failed`. A time or iteration limit returns `Feasible` when the backend supplies values that satisfy the model under the request's numeric policy. Every non-optimal result includes a diagnostic.

`SolverOptions.TimeLimit` covers target maximization and follow-up activity minimization. If the second stage cannot produce a usable result, FactoryGraph returns the primary solution as `Feasible` with `solver.secondary-optimization-incomplete`.
