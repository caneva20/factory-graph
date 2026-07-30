using FactoryGraph.Contracts;

namespace FactoryGraph.Validation;

internal sealed class SolveRequestValidator(
    SolverSettingsValidator settingsValidator,
    FactoryGraphValidator graphValidator,
    TargetValidator targetValidator) : ISolveRequestValidator {
    public IReadOnlyList<SolveDiagnostic> Validate(SolveRequest request) {
        var diagnostics = new List<SolveDiagnostic>();
        var numericPolicy = settingsValidator.Validate(request, diagnostics);
        graphValidator.Validate(request.Graph, numericPolicy, diagnostics);
        targetValidator.Validate(request.Targets, request.Graph.ProductOutlets, request.Mode, numericPolicy, diagnostics);

        return diagnostics;
    }
}
