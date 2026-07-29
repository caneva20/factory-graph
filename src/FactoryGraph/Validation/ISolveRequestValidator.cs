using FactoryGraph.Contracts;

namespace FactoryGraph.Validation;

internal interface ISolveRequestValidator {
    IReadOnlyList<SolveDiagnostic> Validate(SolveRequest request);
}
