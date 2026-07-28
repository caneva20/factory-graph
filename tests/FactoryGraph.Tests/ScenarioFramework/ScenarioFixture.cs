using FactoryGraph.Contracts;

namespace FactoryGraph.Tests.ScenarioFramework;

internal sealed record ScenarioDimensions(
    string GraphShape,
    IReadOnlyList<string> Constraints,
    IReadOnlyList<string> ResourceConditions);

internal sealed record ScenarioFixture(
    string Name,
    string CaseName,
    string Description,
    ScenarioDimensions Dimensions,
    SolveRequest Request,
    SolveResult Expected);
