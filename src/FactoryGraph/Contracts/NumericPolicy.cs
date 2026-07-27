namespace FactoryGraph.Contracts;

/// <summary>
///     Defines floating-point comparison and normalization thresholds.
/// </summary>
/// <param name="AbsoluteTolerance">The minimum tolerance used when comparing two values.</param>
/// <param name="RelativeTolerance">The tolerance applied relative to the larger absolute value.</param>
/// <param name="NearZeroThreshold">The inclusive threshold for normalizing a value to positive zero.</param>
public sealed record NumericPolicy(
    double AbsoluteTolerance = 1e-9,
    double RelativeTolerance = 1e-9,
    double NearZeroThreshold = 1e-12)
{
    /// <summary>
    ///     Tests whether two values differ by no more than the absolute or relative tolerance.
    /// </summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true" /> when the values are within tolerance; otherwise, <see langword="false" />.</returns>
    public bool NearlyEquals(double left, double right)
    {
        var difference = Math.Abs(left - right);
        var scale = Math.Max(Math.Abs(left), Math.Abs(right));

        return difference <= Math.Max(AbsoluteTolerance, RelativeTolerance * scale);
    }

    /// <summary>
    ///     Normalizes a value within the near-zero threshold to positive zero.
    /// </summary>
    /// <param name="value">The value to normalize.</param>
    /// <returns>Positive zero when the value is within the threshold; otherwise, the original value.</returns>
    public double NormalizeNearZero(double value)
    {
        return Math.Abs(value) <= NearZeroThreshold ? 0d : value;
    }
}
