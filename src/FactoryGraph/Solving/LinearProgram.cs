namespace FactoryGraph.Solving;

internal sealed record LinearProgram(
    IReadOnlyList<double> LowerBounds,
    IReadOnlyList<double> UpperBounds,
    IReadOnlyList<ModelRow> Rows) {
    public int VariableCount => LowerBounds.Count;
}

internal sealed record ModelRow(
    IReadOnlyDictionary<int, double> Coefficients,
    double LowerBound,
    double UpperBound) {
    public static ModelRow Equality(IReadOnlyDictionary<int, double> coefficients) => new(coefficients, 0, 0);
}
