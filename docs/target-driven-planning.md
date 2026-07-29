# Target-Driven Planning

The README explains the [factory graph](../README.md#graph-concepts) and [solving concepts](../README.md#solving-a-graph) used here. This document defines the target-driven optimization model.

`FactoryGraphProcessor` computes steady-state machine activity and item flows that meet every target within configured capacities.

## Model

The solver creates a nonnegative variable for each machine-group activity, connection flow, supply rate, product-outlet delivery, and surplus-outlet flow. Incoming connections and supplies equal recipe consumption. Outgoing connections and outlets equal recipe production.

The solver applies these upper bounds when present:

- Machine activity is at most `AvailableMachineCount * ActivityRatePerMachine`.
- `ActivityCapacity` also limits machine activity. Both machine limits apply when both exist.
- Connection, supply, product-outlet, and surplus-outlet capacities limit their flow variables.

A target sets its product outlet's lower bound to the requested rate. Product outlets may share upstream production. The validator rejects multiple targets for one product outlet.

## Objectives

`MeetTargetsMinimizeExternalSupply` minimizes total external supply. `MeetTargetsMinimizeSurplus` minimizes total surplus-outlet flow. The solver then holds that optimum within the request's numeric tolerance and minimizes total machine activity, leaving unnecessary production inactive.

## Results

An optimal or feasible result retains the request's collection order and topology. Machine rates equal recipe amounts multiplied by solved activity. Effective machine count equals activity divided by `ActivityRatePerMachine`. The numeric policy normalizes near-zero values and controls whole-machine rounding.

The solver balances every machine input, so each supply reports equal required and provided rates with zero deficit. Target surplus and deficit compare product-outlet flow with the requested rate.

## Status And Time Limit

FactoryGraph reports `Optimal`, `Infeasible`, `Unbounded`, or `Failed` from the HiGHS model status. A time or iteration limit returns `Feasible` when the solution satisfies variable bounds and item balances under the request's numeric policy. Every non-optimal result includes a diagnostic.

The caller's `SolverOptions.TimeLimit` covers both optimization stages. See [Solver Backend](solver-backend.md) for package and runtime details.
