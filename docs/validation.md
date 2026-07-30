# Request Validation

`FactoryGraphProcessor.Solve` validates each `SolveRequest` before invoking its configured solve stage. Invalid requests produce:

- `SolveStatus.InvalidRequest`
- a `null` computed graph
- one or more error diagnostics
- an empty bottleneck list

The processor does not invoke the solve stage when validation fails. It passes valid requests to the stage unchanged.

## Identity And References

String IDs must contain at least one non-whitespace character. `Guid` IDs must not equal `Guid.Empty`. IDs must be unique within each element kind.

References must resolve as follows:

| Owner | Reference |
|---|---|
| Recipe input or output | Item |
| Machine group | Recipe |
| Connection | Source machine group, destination machine group, and item |
| Supply | Destination machine group and item |
| Product outlet | Source machine group and item |
| Surplus outlet | Source machine group and item |
| Target | Product outlet |

Validation checks item compatibility after resolving the related item, machine group, and recipe:

- A connection source must produce the connection item.
- A connection destination must consume the connection item.
- A supply destination must consume the supplied item.
- A product-outlet or surplus-outlet source must produce its item.

An unresolved reference suppresses dependent compatibility diagnostics. This keeps one missing endpoint from producing unrelated item errors.

Disconnected graph components are valid. Validation does not require every item, recipe, or machine group to participate in a path.

Each product outlet may have at most one target. Separate product outlets represent separate external destinations or independently requested rates.

## Modes And Objectives

The processor accepts these combinations:

| Planning mode | Objectives |
|---|---|
| `TargetDriven` | `MeetTargetsMinimizeExternalSupply`, `MeetTargetsMinimizeSurplus` |
| `CapacityDriven` | `MaximizeTargetOutput` |

Undefined enum values and other combinations fail validation.

## Numeric Preconditions

All `double` values must be finite. Validation applies `NumericPolicy.NearZeroThreshold` before sign checks: values whose absolute magnitude is less than or equal to the threshold count as zero.

| Value | Requirement |
|---|---|
| `RecipeItem.Amount` | Positive |
| `MachineGroup.ActivityRatePerMachine` | Positive |
| `MachineGroup.AvailableMachineCount` | Nonnegative when present |
| Machine, connection, supply, product-outlet, and surplus-outlet capacities | Nonnegative when present |
| `Target.Rate` | Nonnegative; capacity-driven requests require at least one positive rate |
| Numeric-policy tolerances and threshold | Nonnegative |
| `SolverOptions.TimeLimit` | Positive when present |

Zero capacities, machine counts, and individual target rates are valid. Capacity-driven planning rejects a request when every target rate normalizes to zero. A small negative capacity within the near-zero threshold also passes its nonnegative check. A small positive recipe amount or activity rate within that threshold counts as zero and fails its positive check.

## Diagnostics

Use `SolveDiagnostic.Code` for program logic. Messages describe the error for callers and may change. Diagnostics identify related graph elements through `GraphElementReference` whenever a valid owner or referenced ID exists.

| Code | Meaning |
|---|---|
| `validation.id.required` | An element or reference ID is absent. |
| `validation.id.duplicate` | An element ID occurs more than once within its kind. |
| `validation.reference.not-found` | A referenced catalog or graph element does not exist. |
| `validation.item.incompatible` | An item does not match the recipe at a connection or boundary. |
| `validation.mode.unsupported` | The planning mode is undefined. |
| `validation.objective.unsupported` | The objective is undefined or incompatible with the planning mode. |
| `validation.target.duplicate-product-outlet` | More than one target references the same product outlet. |
| `validation.target.positive-rate-required` | A capacity-driven request has no positive target rate. |
| `validation.number.non-finite` | A numeric value is `NaN` or infinite. |
| `validation.number.negative` | A value is negative where zero is permitted. |
| `validation.number.positive-required` | A value is zero or negative where a positive value is required. |

`FactoryGraph.Validation.ValidationDiagnosticCodes` groups these constants by path segment. For example, `ValidationDiagnosticCodes.Id.Required` resolves to `validation.id.required`.
