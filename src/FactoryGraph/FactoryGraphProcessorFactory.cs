using FactoryGraph.Solving;
using FactoryGraph.Validation;

namespace FactoryGraph;

/// <summary>
///     Creates configured factory graph processors.
/// </summary>
public static class FactoryGraphProcessorFactory {
    /// <summary>
    ///     Creates a processor with the built-in validation and planning pipeline.
    /// </summary>
    /// <returns>A configured factory graph processor.</returns>
    public static FactoryGraphProcessor Create() {
        return Create(
            new BuiltInSolveRequestSolver([
                new TargetDrivenSolver(
                    new LinearFactoryModelBuilder(),
                    new HighsLinearProgramSolver(),
                    new LinearFactoryResultProjector()),
                new CapacityDrivenSolver(
                    new CapacityDrivenModelBuilder(new LinearFactoryModelBuilder()),
                    new HighsLinearProgramSolver(),
                    new LinearFactoryResultProjector(),
                    new CapacityBottleneckReporter())
            ]));
    }

    internal static FactoryGraphProcessor Create(ISolveRequestSolver solver) {
        return new FactoryGraphProcessor(
            new SolveRequestValidator(
                new SolverSettingsValidator(),
                new FactoryGraphValidator(),
                new TargetValidator()),
            solver);
    }
}
