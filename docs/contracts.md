# Public Contracts

`FactoryGraph.Contracts` defines requests and results for steady-state factory planning. The contracts do not validate or solve graphs.

## Identity

Items and recipes use caller-assigned string IDs. Other entities use `Guid` IDs.

| Entity | ID type |
|---|---|
| Item | `string` |
| Recipe | `string` |
| Machine group | `Guid` |
| Item connection | `Guid` |
| Supply | `Guid` |
| Demand | `Guid` |
| Target | `Guid` |
| Surplus sink | `Guid` |

IDs must remain stable within a request. Results retain input IDs and topology references.

## Rates And Activity

All rates in a request use one time unit, such as items per minute.

- `RecipeItem.Amount` is the quantity consumed or produced per activity unit.
- `MachineGroup.ActivityRatePerMachine` is the activity per machine per time unit.
- Effective machine count is activity divided by activity rate per machine.
- Required whole-machine count is the tolerance-aware ceiling of effective machine count.
- Utilization is effective machine count divided by required whole-machine count.
- `AvailableMachineCount` limits activity through the number of installed machines.
- `ActivityCapacity` directly limits activity per time unit.
- When both machine count and activity capacity are present, both constraints apply.
- Connection and boundary capacities are item rates. A `null` capacity is unbounded.

## Topology And Boundaries

An `ItemConnection` carries one item between machine groups. The source recipe must produce the item, and the destination recipe must consume it.

A `Supply` feeds a machine-group input. A `Demand` drains a machine-group output. A `Target` sets a requested rate for a demand. A `SurplusSink` accepts excess output.

Contracts permit malformed values. Validation reports duplicate IDs, missing endpoints, incompatible items, and invalid numbers.

## Planning Modes

`TargetDriven` computes the activity and resources needed to meet target rates within configured limits.

```csharp
var request = new SolveRequest(
    graph,
    [new Target(targetId, demandId, Rate: 10)],
    PlanningMode.TargetDriven,
    SolveObjective.MeetTargetsMinimizeExternalSupply,
    new SolverOptions(new NumericPolicy()));
```

`CapacityDriven` computes the maximum target output allowed by machine, supply, and connection capacities.

```csharp
var request = new SolveRequest(
    graph,
    [new Target(targetId, demandId, Rate: 10)],
    PlanningMode.CapacityDriven,
    SolveObjective.MaximizeTargetOutput,
    new SolverOptions(new NumericPolicy()));
```

See `tests/FactoryGraph.Tests/Contracts/TargetDrivenContractTests.cs` and `tests/FactoryGraph.Tests/Contracts/CapacityDrivenContractTests.cs` for complete examples.

## Results

`SolveResult` contains a status, an optional computed graph, diagnostics, and bottlenecks. The computed graph includes the catalog and topology.

Machine-group results include activity, machine counts, utilization, and item rates. Boundary and target results include required, provided, achieved, surplus, and deficit rates. Connections include flow rates. Bottlenecks reference a machine group, connection, or supply.

The graph may be absent for invalid or failed requests.

## Numeric Policy

`NumericPolicy` defines floating-point comparisons:

- Two values are nearly equal when `abs(left - right) <= max(absoluteTolerance, relativeTolerance * max(abs(left), abs(right)))`.
- A value normalizes to positive zero when `abs(value) <= nearZeroThreshold`.
- Solvers normalize reported rates and residuals before constructing results.
- Whole-machine ceiling must treat values nearly equal to an integer as that integer before applying `Ceiling`.

The default absolute and relative tolerances are `1e-9`; the default near-zero threshold is `1e-12`.

## Collection Ownership

Contract records expose collections as `IReadOnlyList<T>` without copying them. Callers must not modify collections while a request is in use. Processors must not mutate requests or their collections.
