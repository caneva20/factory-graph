using FactoryGraph.Contracts;

namespace FactoryGraph.Validation;

internal sealed class SolverSettingsValidator {
    public NumericPolicy Validate(SolveRequest request, ICollection<SolveDiagnostic> diagnostics) {
        ValidateModeAndObjective(request, diagnostics);
        var policy = request.Options.NumericPolicy;
        ValidateNonnegative(policy.AbsoluteTolerance, "NumericPolicy.AbsoluteTolerance", diagnostics);
        ValidateNonnegative(policy.RelativeTolerance, "NumericPolicy.RelativeTolerance", diagnostics);
        ValidateNonnegative(policy.NearZeroThreshold, "NumericPolicy.NearZeroThreshold", diagnostics);

        if (request.Options.TimeLimit is { } timeLimit && timeLimit <= TimeSpan.Zero) {
            Add(diagnostics, ValidationDiagnosticCodes.Number.PositiveRequired, "SolverOptions.TimeLimit must be positive.");
        }

        return policy with {
            NearZeroThreshold = double.IsFinite(policy.NearZeroThreshold) && policy.NearZeroThreshold >= 0
                ? policy.NearZeroThreshold
                : 0
        };
    }

    private static void ValidateModeAndObjective(SolveRequest request, ICollection<SolveDiagnostic> diagnostics) {
        var modeSupported = Enum.IsDefined(request.Mode);
        var objectiveSupported = Enum.IsDefined(request.Objective);

        if (!modeSupported) {
            Add(diagnostics, ValidationDiagnosticCodes.Mode.Unsupported, $"Planning mode '{request.Mode}' is unsupported.");
        }

        if (!objectiveSupported) {
            Add(diagnostics, ValidationDiagnosticCodes.Objective.Unsupported, $"Solve objective '{request.Objective}' is unsupported.");
        } else if (modeSupported && !Supports(request.Mode, request.Objective)) {
            Add(
                diagnostics,
                ValidationDiagnosticCodes.Objective.Unsupported,
                $"Solve objective '{request.Objective}' is unsupported for planning mode '{request.Mode}'.");
        }
    }

    private static bool Supports(PlanningMode mode, SolveObjective objective) {
        return mode switch {
            PlanningMode.TargetDriven => objective is SolveObjective.MeetTargetsMinimizeExternalSupply
                or SolveObjective.MeetTargetsMinimizeSurplus,
            PlanningMode.CapacityDriven => objective is SolveObjective.MaximizeTargetOutput,
            _ => false
        };
    }

    private static void ValidateNonnegative(
        double value,
        string property,
        ICollection<SolveDiagnostic> diagnostics) {
        if (!double.IsFinite(value)) {
            Add(diagnostics, ValidationDiagnosticCodes.Number.NonFinite, $"{property} must be finite.");
        } else if (value < 0) {
            Add(diagnostics, ValidationDiagnosticCodes.Number.Negative, $"{property} must be nonnegative.");
        }
    }

    private static void Add(ICollection<SolveDiagnostic> diagnostics, string code, string message) {
        diagnostics.Add(new SolveDiagnostic(code, DiagnosticSeverity.Error, message, []));
    }
}
