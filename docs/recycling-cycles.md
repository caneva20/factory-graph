# Recycling Cycles

A recycling cycle returns an item to a machine group on the same directed production path. Its machine groups form a strongly connected component: every group in the component can reach every other group through item connections. A self-loop is a one-group component.

## Steady-State Solve

FactoryGraph creates activity and flow variables for the whole graph and solves their balances in one linear program. Cyclic rates therefore resolve at the same time as external supplies, products, surplus, targets, and capacities. The solver does not cut a cycle or choose a traversal order.

For a lossy recycle, external supply must replace the material lost on each pass. A process that consumes 1 catalyst and recovers 0.75 catalyst needs 0.25 external catalyst per activity at steady state. Capacity limits can make a target-driven request infeasible. A capacity-driven cycle can be unbounded when no machine, flow, or boundary capacity limits target output.

## Cycle Analysis

`SolveResult.Analysis.CyclicComponents` identifies each multi-group strongly connected component and each self-loop. A component contains:

- `MachineGroupIds`, ordered by machine-group declaration;
- `ConnectionIds` for connections whose source and destination both belong to the component, ordered by connection declaration; and
- `RequiresStartupInventory`, a structural startup classification.

Components follow the declaration order of their first machine group. Connections between separate components do not belong to either component.

The processor performs this analysis after request validation and before solving. Optimal, feasible, infeasible, unbounded, and failed results retain it. Invalid requests return empty analysis.

## Startup Classification

Steady-state equations can describe a sustainable cycle that cannot begin from an empty factory. FactoryGraph marks such a component with `RequiresStartupInventory = true`.

The analysis starts with recipes that consume no inputs. It then marks a group startable when every distinct recipe input can come from either:

- an external supply with no capacity limit or a capacity above the request's near-zero threshold; or
- an incoming connection from a group already marked startable.

It repeats this step across the full graph so an upstream group or cycle can start a downstream cycle. A cyclic component requires startup inventory when this process cannot mark every member startable.

This classification tests structural availability, not quantity. It does not compare supply rates with recipe consumption, inspect connection or machine capacity, calculate bootstrap inventory, model startup timing, or alter feasibility. The linear solver remains responsible for steady-state rate and capacity constraints.
