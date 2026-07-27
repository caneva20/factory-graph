using FactoryGraph.Contracts;

namespace FactoryGraph.Tests.Contracts;

public sealed class NumericPolicyTests
{
    [Fact]
    public void NearlyEqualsUsesTheLargerOfAbsoluteAndRelativeTolerance()
    {
        var policy = new NumericPolicy(
            AbsoluteTolerance: 0.001,
            RelativeTolerance: 0.01,
            NearZeroThreshold: 0.0001);

        Assert.True(policy.NearlyEquals(0, 0.0005));
        Assert.True(policy.NearlyEquals(1_000, 1_009));
        Assert.False(policy.NearlyEquals(1_000, 1_011));
    }

    [Theory]
    [InlineData(0.0001, 0)]
    [InlineData(-0.0001, 0)]
    [InlineData(0.00011, 0.00011)]
    public void NormalizeNearZeroUsesAnInclusiveThreshold(double value, double expected)
    {
        var policy = new NumericPolicy(NearZeroThreshold: 0.0001);

        Assert.Equal(expected, policy.NormalizeNearZero(value));
    }
}
