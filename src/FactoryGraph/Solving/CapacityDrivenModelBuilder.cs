using FactoryGraph.Contracts;

namespace FactoryGraph.Solving;

internal sealed class CapacityDrivenModelBuilder(LinearFactoryModelBuilder factoryModelBuilder) {
    private const double Infinity = 1e30;

    public CapacityDrivenModel Build(SolveRequest request) {
        var factoryModel = factoryModelBuilder.Build(request);
        var scaleVariable = factoryModel.VariableCount;
        var lowerBounds = new double[scaleVariable + 1];
        var upperBounds = new double[scaleVariable + 1];

        for (var index = 0; index < scaleVariable; index++) {
            lowerBounds[index] = factoryModel.Program.LowerBounds[index];
            upperBounds[index] = factoryModel.Program.UpperBounds[index];
        }

        upperBounds[scaleVariable] = Infinity;

        var factoryRows = factoryModel.Program.Rows;
        var rows = new ModelRow[factoryRows.Count + request.Targets.Count];

        for (var index = 0; index < factoryRows.Count; index++) {
            rows[index] = factoryRows[index];
        }

        for (var index = 0; index < request.Targets.Count; index++) {
            var target = request.Targets[index];
            rows[factoryRows.Count + index] = ModelRow.Equality(new Dictionary<int, double>(2) {
                [factoryModel.ProductOutlets[target.ProductOutletId]] = 1,
                [scaleVariable] = -target.Rate
            });
        }

        var program = new LinearProgram(lowerBounds, upperBounds, rows);

        return new CapacityDrivenModel(factoryModel.WithProgram(program), scaleVariable);
    }
}
