using FactoryGraph.Contracts;

namespace FactoryGraph.Validation;

internal sealed class TargetValidator {
    public void Validate(
        IReadOnlyList<Target> targets,
        IReadOnlyList<ProductOutlet> productOutletList,
        PlanningMode mode,
        NumericPolicy numericPolicy,
        ICollection<SolveDiagnostic> diagnostics) {
        var productOutlets = productOutletList
            .Where(outlet => outlet.Id != Guid.Empty)
            .GroupBy(outlet => outlet.Id)
            .ToDictionary(group => group.Key, group => group.First());

        var targetIds = new HashSet<Guid>(targets.Count);
        var targetedProductOutlets = new HashSet<Guid>(targets.Count);

        foreach (var target in targets) {
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

            if (target.ProductOutletIds.Count == 0) {
                Add(
                    diagnostics,
                    ValidationDiagnosticCodes.Target.ProductOutletRequired,
                    $"Target '{target.Id:D}' must have at least one product outlet.",
                    reference);
            }

            var outletItems = new HashSet<string>(StringComparer.Ordinal);

            foreach (var productOutletId in target.ProductOutletIds) {
                if (productOutletId == Guid.Empty || !productOutlets.TryGetValue(productOutletId, out var outlet)) {
                    Add(
                        diagnostics,
                        ValidationDiagnosticCodes.Reference.NotFound,
                        $"Target '{target.Id:D}' references missing product outlet '{productOutletId:D}'.",
                        reference,
                        Reference(GraphElementKind.ProductOutlet, productOutletId));
                } else if (!targetedProductOutlets.Add(productOutletId)) {
                    Add(
                        diagnostics,
                        ValidationDiagnosticCodes.Target.DuplicateProductOutlet,
                        $"Product outlet '{productOutletId:D}' cannot be referenced more than once by targets.",
                        reference,
                        new GraphElementReference(GraphElementKind.ProductOutlet, productOutletId));
                } else if (outletItems.Add(outlet.ItemId) && outletItems.Count > 1) {
                    Add(
                        diagnostics,
                        ValidationDiagnosticCodes.Item.Incompatible,
                        $"Target '{target.Id:D}' product outlets must deliver the same item.",
                        reference,
                        new GraphElementReference(GraphElementKind.ProductOutlet, productOutletId));
                }
            }

            ValidateNonnegative(target.Rate, $"Target '{target.Id:D}'.Rate", reference, numericPolicy, diagnostics);
        }

        if (mode == PlanningMode.CapacityDriven
            && !targets.Any(target => double.IsFinite(target.Rate) && numericPolicy.NormalizeNearZero(target.Rate) > 0)) {
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
