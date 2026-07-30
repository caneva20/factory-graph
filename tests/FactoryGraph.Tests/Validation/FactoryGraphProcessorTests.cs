using FactoryGraph.Contracts;
using FactoryGraph.Validation;

namespace FactoryGraph.Tests.Validation;

public sealed class FactoryGraphProcessorTests {
    [Fact]
    public void InvalidRequestReturnsDiagnosticsWithoutInvokingSolveStage() {
        var request = CreateValidRequest();

        request = request with {
            Graph = request.Graph with {
                Items = [request.Graph.Items[0], request.Graph.Items[0]],
                MachineGroups = [request.Graph.MachineGroups[0] with { Id = Guid.Empty }]
            }
        };

        var invoked = false;

        var processor = Processor(_ => {
            invoked = true;

            return SuccessfulResult();
        });

        var result = processor.Solve(request);

        Assert.Equal(SolveStatus.InvalidRequest, result.Status);
        Assert.Null(result.Graph);
        Assert.Empty(result.Bottlenecks);
        Assert.False(invoked);

        Assert.Collection(
            result.Diagnostics,
            diagnostic => Assert.Equal(ValidationDiagnosticCodes.Id.Duplicate, diagnostic.Code),
            diagnostic => Assert.Equal(ValidationDiagnosticCodes.Id.Required, diagnostic.Code),
            diagnostic => Assert.Equal(ValidationDiagnosticCodes.Reference.NotFound, diagnostic.Code),
            diagnostic => Assert.Equal(ValidationDiagnosticCodes.Reference.NotFound, diagnostic.Code),
            diagnostic => Assert.Equal(ValidationDiagnosticCodes.Reference.NotFound, diagnostic.Code),
            diagnostic => Assert.Equal(ValidationDiagnosticCodes.Reference.NotFound, diagnostic.Code));

        Assert.Equal("ore", result.Diagnostics[0].Elements.Single().Id);
    }

    [Fact]
    public void MissingEndpointDoesNotProduceCascadingCompatibilityDiagnostic() {
        var request = CreateValidRequest();
        var sourceId = request.Graph.MachineGroups.Single().Id;
        var connectionId = Guid.Parse("55555555-5555-5555-5555-555555555555");

        request = request with {
            Graph = request.Graph with {
                Connections = [
                    new ItemConnection(
                        connectionId,
                        sourceId,
                        Guid.Parse("99999999-9999-9999-9999-999999999999"),
                        "plate")
                ]
            }
        };

        var result = Processor().Solve(request);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(ValidationDiagnosticCodes.Reference.NotFound, diagnostic.Code);
        Assert.Equal(connectionId.ToString("D"), diagnostic.Elements[0].Id);
    }

    [Fact]
    public void IncompatibleBoundaryItemIdentifiesTheBoundaryAndMachineGroup() {
        var request = CreateValidRequest();
        var supply = request.Graph.Supplies.Single() with { ItemId = "plate" };
        request = request with { Graph = request.Graph with { Supplies = [supply] } };

        var diagnostic = Assert.Single(Processor().Solve(request).Diagnostics);

        Assert.Equal(ValidationDiagnosticCodes.Item.Incompatible, diagnostic.Code);

        Assert.Collection(
            diagnostic.Elements,
            element => Assert.Equal(GraphElementKind.Supply, element.Kind),
            element => Assert.Equal(GraphElementKind.MachineGroup, element.Kind));
    }

    [Fact]
    public void UnsupportedObjectiveForModeReturnsStableDiagnostic() {
        var request = CreateValidRequest() with { Objective = SolveObjective.MaximizeTargetOutput };

        var diagnostic = Assert.Single(Processor().Solve(request).Diagnostics);

        Assert.Equal(ValidationDiagnosticCodes.Objective.Unsupported, diagnostic.Code);
        Assert.Contains(nameof(PlanningMode.TargetDriven), diagnostic.Message);
    }

    [Fact]
    public void MultipleTargetsForOneProductOutletReturnStableDiagnostic() {
        var request = CreateValidRequest();
        request = request with {
            Targets = [
                request.Targets.Single(),
                new Target(Guid.Parse("77777777-7777-7777-7777-777777777777"), request.Targets.Single().ProductOutletId, 2)
            ]
        };

        var diagnostic = Assert.Single(Processor().Solve(request).Diagnostics);

        Assert.Equal(ValidationDiagnosticCodes.Target.DuplicateProductOutlet, diagnostic.Code);
        Assert.Collection(
            diagnostic.Elements,
            element => Assert.Equal(GraphElementKind.Target, element.Kind),
            element => Assert.Equal(GraphElementKind.ProductOutlet, element.Kind));
    }

    [Fact]
    public void CapacityDrivenModeRequiresAPositiveTargetRate() {
        var request = CreateValidRequest() with {
            Mode = PlanningMode.CapacityDriven,
            Objective = SolveObjective.MaximizeTargetOutput,
            Targets = [CreateValidRequest().Targets.Single() with { Rate = 0 }]
        };

        var diagnostic = Assert.Single(Processor().Solve(request).Diagnostics);

        Assert.Equal(ValidationDiagnosticCodes.Target.PositiveRateRequired, diagnostic.Code);
    }

    [Fact]
    public void NumericPreconditionsRejectNonFiniteSignsAndProhibitedZero() {
        var baseline = CreateValidRequest();

        var cases = new (SolveRequest Request, string Code)[] {
            (
                baseline with {
                    Graph = baseline.Graph with {
                        MachineGroups = [baseline.Graph.MachineGroups.Single() with { ActivityRatePerMachine = double.NaN }]
                    }
                },
                ValidationDiagnosticCodes.Number.NonFinite),
            (
                baseline with {
                    Graph = baseline.Graph with {
                        MachineGroups = [baseline.Graph.MachineGroups.Single() with { ActivityRatePerMachine = 0 }]
                    }
                },
                ValidationDiagnosticCodes.Number.PositiveRequired),
            (
                baseline with {
                    Graph = baseline.Graph with {
                        Recipes = [
                            baseline.Graph.Recipes.Single() with {
                                Inputs = [baseline.Graph.Recipes.Single().Inputs.Single() with { Amount = -1 }]
                            }
                        ]
                    }
                },
                ValidationDiagnosticCodes.Number.PositiveRequired),
            (
                baseline with {
                    Graph = baseline.Graph with {
                        Supplies = [baseline.Graph.Supplies.Single() with { Capacity = -1 }]
                    }
                },
                ValidationDiagnosticCodes.Number.Negative),
            (
                baseline with { Targets = [baseline.Targets.Single() with { Rate = double.PositiveInfinity }] },
                ValidationDiagnosticCodes.Number.NonFinite),
            (
                baseline with {
                    Options = baseline.Options with {
                        NumericPolicy = baseline.Options.NumericPolicy with { AbsoluteTolerance = -1 }
                    }
                },
                ValidationDiagnosticCodes.Number.Negative)
        };

        foreach (var (request, code) in cases) {
            var diagnostic = Assert.Single(Processor().Solve(request).Diagnostics);
            Assert.Equal(code, diagnostic.Code);
        }
    }

    [Fact]
    public void ValidDisconnectedGraphAndPermittedZeroValuesReachSolveStage() {
        var request = CreateValidRequest();
        var disconnectedId = Guid.Parse("66666666-6666-6666-6666-666666666666");

        request = request with {
            Graph = request.Graph with {
                MachineGroups = [
                    request.Graph.MachineGroups.Single() with {
                        AvailableMachineCount = 0,
                        ActivityCapacity = 0
                    },
                    new MachineGroup(disconnectedId, "smelt", 1)
                ],
                Supplies = [request.Graph.Supplies.Single() with { Capacity = -5e-13 }],
                ProductOutlets = [request.Graph.ProductOutlets.Single() with { Capacity = 0 }]
            },
            Targets = [request.Targets.Single() with { Rate = 0 }]
        };

        var expected = SuccessfulResult();
        SolveRequest? delegatedRequest = null;

        var processor = Processor(validRequest => {
            delegatedRequest = validRequest;

            return expected;
        });

        var result = processor.Solve(request);

        Assert.Same(request, delegatedRequest);
        Assert.Same(expected, result);
    }

    [Fact]
    public void PositiveValueWithinNearZeroThresholdIsRejectedAsZero() {
        var request = CreateValidRequest();

        request = request with {
            Graph = request.Graph with {
                MachineGroups = [request.Graph.MachineGroups.Single() with { ActivityRatePerMachine = 5e-13 }]
            }
        };

        var diagnostic = Assert.Single(Processor().Solve(request).Diagnostics);

        Assert.Equal(ValidationDiagnosticCodes.Number.PositiveRequired, diagnostic.Code);
    }

    private static FactoryGraphProcessor Processor() {
        return Processor(_ => SuccessfulResult());
    }

    private static FactoryGraphProcessor Processor(Func<SolveRequest, SolveResult> solve) {
        return FactoryGraphProcessorFactory.Create(new TestSolveRequestSolver(solve));
    }

    private static SolveResult SuccessfulResult() {
        return new SolveResult(SolveStatus.Optimal, null, [], []);
    }

    private static SolveRequest CreateValidRequest() {
        var machineGroupId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var supplyId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var productOutletId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var targetId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        return new SolveRequest(
            new FactoryGraphDefinition(
                Items: [new Item("ore", "Ore"), new Item("plate", "Plate")],
                Recipes: [
                    new Recipe(
                        "smelt",
                        "Smelt",
                        Inputs: [new RecipeItem("ore", 1)],
                        Outputs: [new RecipeItem("plate", 1)])
                ],
                MachineGroups: [new MachineGroup(machineGroupId, "smelt", 1)],
                Connections: [],
                Supplies: [new Supply(supplyId, machineGroupId, "ore")],
                ProductOutlets: [new ProductOutlet(productOutletId, machineGroupId, "plate")],
                SurplusOutlets: []),
            Targets: [new Target(targetId, productOutletId, 1)],
            PlanningMode.TargetDriven,
            SolveObjective.MeetTargetsMinimizeExternalSupply,
            new SolverOptions(new NumericPolicy()));
    }
}
