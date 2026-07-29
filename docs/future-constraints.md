# Future Graph Constraints

This document defines requirements and compatibility boundaries for a future capability that fixes or bounds values throughout a factory solve graph. It does not define a public constraint API or commit the processor to a particular solver representation.

## Scope

A future solve request must support hard fixed, minimum, and maximum bounds on these metrics:

| Graph element | Addressed metric | Fixed example | Minimum example | Maximum example |
|---|---|---|---|---|
| Machine group | Effective machine count | Run exactly 3 machine equivalents | Keep at least 2 machine equivalents active | Use at most 5 machine equivalents |
| Machine group | Activity | Run exactly 12 activities per minute | Maintain at least 4 activities per minute | Limit activity to 20 per minute |
| Machine-group input | Aggregate item input rate | Consume exactly 30 iron ore per minute | Consume at least 10 water per minute | Consume at most 40 coal per minute |
| Machine-group output | Aggregate item output rate | Produce exactly 20 plates per minute | Produce at least 5 fuel per minute | Produce at most 50 steam per minute |
| Supply | Provided rate | Import exactly 10 ore per minute | Import at least 2 water per minute | Import at most 25 crude oil per minute |
| Product outlet | Delivered rate | Deliver exactly 15 plates per minute | Deliver at least 8 fuel per minute | Deliver at most 30 plastic per minute |
| Item connection | Flow rate | Carry exactly 12 items per minute | Carry at least 3 items per minute | Carry at most 20 items per minute |

Bounds are request-level requirements over a graph. Existing capacity properties remain concise ways to express common upper limits; a future generalized capability must define how duplicate limits on the same metric are combined and reported.

Surplus-sink flow and target achievement may use the same addressing principles in a later expansion, but are not required by this specification.

## Stable Addressing

A constraint address must identify a graph element and metric without referring to editor nodes, ports, controls, or other UI types.

- Machine count and activity use the machine-group `Guid`.
- Supply, outlet, and connection rates use the corresponding element `Guid`.
- A machine-group input or output rate uses the machine-group `Guid`, an input/output direction, and the item string ID.
- Item-rate addressing refers to the aggregate rate for an item and direction. It does not refer to a recipe-list position or UI port.
- A constraint address remains stable when collections are reordered and is invalid when its element or item does not exist in the request graph.

The future public contract needs a stable metric vocabulary in addition to the existing graph-element identities. Names used by UI clients may be mapped to that vocabulary but must not become part of the processor contract.

## Machine Count Semantics

Machine count has three distinct meanings:

- `AvailableMachineCount` is a configured integer capacity. It limits activity but does not require every installed machine to run.
- `EffectiveMachineCount` is solved activity divided by activity per machine. This continuous value is the addressable metric for future fixed or bounded machine-count constraints.
- `RequiredWholeMachineCount` is a tolerance-aware ceiling reported from the effective count.

The continuous first-release solver does not directly constrain `RequiredWholeMachineCount`. Doing so would introduce discrete semantics. A future mixed-integer capability may add physical whole-machine constraints under a separate requirement.

## Constraint Semantics

Constraints are hard feasibility requirements:

- A fixed bound requires the addressed metric to equal its value within the numeric policy.
- A minimum bound requires the metric to be no lower than its value, within tolerance.
- A maximum bound requires the metric to be no higher than its value, within tolerance.
- Supplying minimum and maximum bounds that are equal within tolerance is equivalent to fixing the value.
- Objectives rank feasible solutions and never relax a constraint.
- Metrics and bound values use the request's common time unit.
- Bounds must be finite and non-negative. Validation rejects unknown addresses, unsupported metric and element combinations, and contradictory bounds.

Machine-group item input and output rates are derived from activity and recipe amounts. Constraining those rates therefore constrains the associated activity expression; it does not make recipe proportions independently variable.

## Planning Modes And Objectives

The same constraint semantics apply in both planning modes.

In `TargetDriven` mode, constraints restrict the values that may be chosen while satisfying requested targets. A hard bound that conflicts with target fulfillment makes the request infeasible. A minimum or fixed production bound may require a compatible product or surplus outlet for resulting output.

In `CapacityDriven` mode, constraints restrict the feasible region in which target output is maximized. Lower bounds can require baseline activity or flow even when it does not improve the objective. Upper bounds can become production bottlenecks. An achieved target continues to report surplus or deficit against its requested rate.

`MaximizeTargetOutput`, `MeetTargetsMinimizeExternalSupply`, and `MeetTargetsMinimizeSurplus` operate only after hard feasibility is established. Minimization objectives may choose among multiple constrained solutions but may not violate a minimum, maximum, or fixed value. Existing mode and objective compatibility rules remain in force.

## Feasibility And Diagnostics

Constraint processing must distinguish malformed requirements from a valid but unsatisfiable model:

- `InvalidRequest` covers an unknown element or item, an unsupported metric for an element, a non-finite or negative bound, and contradictory bounds on the same address.
- `Infeasible` covers individually valid constraints that cannot be satisfied together with graph balances, capacities, targets, or other hard requirements.
- `Optimal` and `Feasible` results satisfy every hard constraint within the numeric policy.
- `Unbounded` remains an objective classification; adding a lower bound alone does not provide an upper bound for an improving objective.

Diagnostics for a constraint must identify its stable graph element and addressed metric. Machine-group item-rate diagnostics must also identify the item and direction. Messages remain caller-facing context, not the only machine-readable representation of the failed address.

Binding upper bounds may be reported as bottlenecks when they limit achieved target output. Constraint satisfaction, violation, and slack are broader concepts than bottlenecks and should not be forced into the existing `Bottleneck` model.

## Numeric Policy

Constraint comparison uses `SolverOptions.NumericPolicy` consistently with validation and result construction:

- Equality uses `NumericPolicy.NearlyEquals`.
- A value satisfies a minimum when it is greater than the bound or nearly equal to it.
- A value satisfies a maximum when it is less than the bound or nearly equal to it.
- Minimum and maximum bounds are contradictory only when the minimum exceeds the maximum beyond tolerance.
- Values within `NearZeroThreshold` normalize to positive zero in reported results.
- Normalization does not make a materially negative bound valid.
- Effective machine count retains the existing tolerance-aware conversion to required whole-machine count.

Scenario assertions must report the scenario, graph element, metric, expected bound, actual value, and applicable tolerance when constraint satisfaction fails.

## Public Contract Review

The current contracts do not make generalized constraints impractical:

- Machine groups, connections, supplies, product outlets, surplus outlets, and targets have stable IDs.
- Machine-group item rates can be addressed by machine-group ID, direction, and item ID.
- Computed results expose activity, effective machine count, item rates, boundary rates, and connection flow.
- Requests and results are independent from UI and exporter types.

Future contract work must address these gaps:

- Define a stable metric and address vocabulary.
- Carry constraints on a solve request without adding UI types.
- Validate that each metric is compatible with its addressed element.
- Extend machine-readable diagnostics to identify metrics and item context.
- Decide whether results expose constraint values, satisfaction, and slack independently from bottlenecks.
- Require at most one aggregate computed `ItemRate` per machine group, direction, and item, even if recipe inputs or outputs contain repeated item entries.

`SolveRequest`, `SolveDiagnostic`, and related types use positional record constructors. A future API should preserve those constructor signatures and use additive non-positional properties or dedicated types rather than inserting new positional parameters. No placeholder property is required before the constraint contract is designed.

## Deferred Implementation Work

After these requirements are accepted, implementation should be split into focused work for:

1. Public constraint addressing and request/result contracts.
2. Constraint validation and metric-specific diagnostics.
3. Solver-model translation for target-driven and capacity-driven planning.
4. Executable scenarios covering bounds, objectives, infeasibility, and numeric tolerances.

Generalized constraints should build on the scenario framework and both planning modes. They are not part of the first processor release unless separately scheduled.
