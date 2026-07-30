namespace FactoryGraph.Solving;

internal sealed class CapacityDrivenModel(
    LinearFactoryModel factoryModel,
    int scaleVariable) {
    public LinearFactoryModel FactoryModel { get; } = factoryModel;

    public int ScaleVariable { get; } = scaleVariable;

    public double[] PrimaryCosts() {
        var costs = new double[FactoryModel.VariableCount];
        costs[ScaleVariable] = -1;

        return costs;
    }
}
