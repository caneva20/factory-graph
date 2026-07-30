using FactoryGraph.Contracts;

namespace FactoryGraph.Validation;

internal sealed class TargetValidator {
    public void Validate(
        IEnumerable<Target> targets,
        IEnumerable<ProductOutlet> productOutlets,
        PlanningMode mode,
        NumericPolicy numericPolicy,
        ICollection<SolveDiagnostic> diagnostics) {
        var targetList = targets.ToArray();

        var productOutletIds = productOutlets
            .Where(outlet => outlet.Id != Guid.Empty)
            .Select(outlet => outlet.Id)
            .ToHashSet();

        var targetIds = new HashSet<Guid>();
        var targetedProductOutlets = new HashSet<Guid>();

        foreach (var target in targetList) {
            var reference = Reference(GraphElementKind.Target, target.Id);

            if (target.Id == Guid.Empty) {
                Add(diagnostics, ValidationDiagnosticCodes.Id.Required, "Each target must have a nonempty ID.");
            } else if (!targetIds.Add(target.Id)) {
                Add(
                    diagnostics,
                    ValidationDiagnosticCodes.Id.Duplicate,
                    $"The target ID '{target.Id:D}' is duplicated.",
                    reference);
            }

            if (target.ProductOutletId == Guid.Empty || !productOutletIds.Contains(target.ProductOutletId)) {
                Add(
                    diagnostics,
                    ValidationDiagnosticCodes.Reference.NotFound,
                    $"Target '{target.Id:D}' references missing product outlet '{target.ProductOutletId:D}'.",
                    reference,
                    Reference(GraphElementKind.ProductOutlet, target.ProductOutletId));
            } else if (!targetedProductOutlets.Add(target.ProductOutletId)) {
                Add(
                    diagnostics,
                    ValidationDiagnosticCodes.Target.DuplicateProductOutlet,
                    $"Product outlet '{target.ProductOutletId:D}' cannot have more than one target.",
                    reference,
                    new GraphElementReference(GraphElementKind.ProductOutlet, target.ProductOutletId));
            }

            ValidateNonnegative(target.Rate, $"Target '{target.Id:D}'.Rate", reference, numericPolicy, diagnostics);
        }

        if (mode == PlanningMode.CapacityDriven
            && !targetList.Any(target => double.IsFinite(target.Rate) && numericPolicy.NormalizeNearZero(target.Rate) > 0)) {
            Add(
                diagnostics,
                ValidationDiagnosticCodes.Target.PositiveRateRequired,
                "Capacity-driven planning requires at least one target with a positive rate.");
        }
    }

    private static void ValidateNonnegative(
        double value,
        string property,
        GraphElementReference? element,
        NumericPolicy numericPolicy,
        ICollection<SolveDiagnostic> diagnostics) {
        if (!double.IsFinite(value)) {
            Add(diagnostics, ValidationDiagnosticCodes.Number.NonFinite, $"{property} must be finite.", element);
        } else if (numericPolicy.NormalizeNearZero(value) < 0) {
            Add(diagnostics, ValidationDiagnosticCodes.Number.Negative, $"{property} must be nonnegative.", element);
        }
    }

    private static GraphElementReference? Reference(GraphElementKind kind, Guid id) {
        return id == Guid.Empty ? null : new GraphElementReference(kind, id);
    }

    private static void Add(
        ICollection<SolveDiagnostic> diagnostics,
        string code,
        string message,
        params GraphElementReference?[] elements) {
        diagnostics.Add(new SolveDiagnostic(
            code,
            DiagnosticSeverity.Error,
            message,
            elements.OfType<GraphElementReference>().ToArray()));
    }
}
