using FactoryGraph.Contracts;

namespace FactoryGraph.Solving;

internal sealed class CapacityDrivenModelBuilder(LinearFactoryModelBuilder factoryModelBuilder) {
    private const double Infinity = 1e30;

    public CapacityDrivenModel Build(SolveRequest request) {
        var factoryModel = factoryModelBuilder.Build(request);
        var scaleVariable = factoryModel.VariableCount;
        var lowerBounds = factoryModel.Program.LowerBounds.Append(0d).ToArray();
        var upperBounds = factoryModel.Program.UpperBounds.Append(Infinity).ToArray();

        var rows = factoryModel.Program.Rows
            .Concat(request.Targets.Select(target => ModelRow.Equality(new Dictionary<int, double> {
                [factoryModel.ProductOutlets[target.ProductOutletId]] = 1,
                [scaleVariable] = -target.Rate
            })))
            .ToArray();

        var program = new LinearProgram(lowerBounds, upperBounds, rows);

        return new CapacityDrivenModel(factoryModel.WithProgram(program), scaleVariable);
    }
}
