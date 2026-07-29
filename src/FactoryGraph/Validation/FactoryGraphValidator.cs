using FactoryGraph.Contracts;

namespace FactoryGraph.Validation;

internal sealed class FactoryGraphValidator {
    public void Validate(
        FactoryGraphDefinition graph,
        NumericPolicy numericPolicy,
        ICollection<SolveDiagnostic> diagnostics) {
        var (items, recipes, machineGroups, connections, supplies, productOutlets, surplusOutlets) = graph;

        ValidateIds(items, recipes, machineGroups, connections, supplies, productOutlets, surplusOutlets, diagnostics);

        var itemIds = items.Where(item => !string.IsNullOrWhiteSpace(item.Id)).Select(item => item.Id).ToHashSet(StringComparer.Ordinal);
        var recipesById = FirstById(recipes, recipe => recipe.Id, id => !string.IsNullOrWhiteSpace(id));
        var machineGroupsById = FirstById(machineGroups, group => group.Id, id => id != Guid.Empty);
        ValidateRecipes(recipes, itemIds, numericPolicy, diagnostics);
        ValidateMachineGroups(machineGroups, recipesById, numericPolicy, diagnostics);
        ValidateConnections(connections, machineGroupsById, recipesById, itemIds, numericPolicy, diagnostics);
        ValidateSupplies(supplies, machineGroupsById, recipesById, itemIds, numericPolicy, diagnostics);
        ValidateProductOutlets(productOutlets, machineGroupsById, recipesById, itemIds, numericPolicy, diagnostics);
        ValidateSurplusOutlets(surplusOutlets, machineGroupsById, recipesById, itemIds, numericPolicy, diagnostics);
    }

    private static void ValidateIds(
        IReadOnlyList<Item> items,
        IReadOnlyList<Recipe> recipes,
        IReadOnlyList<MachineGroup> machineGroups,
        IReadOnlyList<ItemConnection> connections,
        IReadOnlyList<Supply> supplies,
        IReadOnlyList<ProductOutlet> productOutlets,
        IReadOnlyList<SurplusOutlet> surplusOutlets,
        ICollection<SolveDiagnostic> diagnostics) {
        ValidateStringIds(items, item => item.Id, GraphElementKind.Item, "item", diagnostics);
        ValidateStringIds(recipes, recipe => recipe.Id, GraphElementKind.Recipe, "recipe", diagnostics);
        ValidateGuidIds(machineGroups, group => group.Id, GraphElementKind.MachineGroup, "machine group", diagnostics);
        ValidateGuidIds(connections, connection => connection.Id, GraphElementKind.Connection, "connection", diagnostics);
        ValidateGuidIds(supplies, supply => supply.Id, GraphElementKind.Supply, "supply", diagnostics);
        ValidateGuidIds(productOutlets, outlet => outlet.Id, GraphElementKind.ProductOutlet, "product outlet", diagnostics);
        ValidateGuidIds(surplusOutlets, outlet => outlet.Id, GraphElementKind.SurplusOutlet, "surplus outlet", diagnostics);
    }

    private static void ValidateStringIds<T>(
        IEnumerable<T> values,
        Func<T, string> getId,
        GraphElementKind kind,
        string label,
        ICollection<SolveDiagnostic> diagnostics) {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var value in values) {
            var id = getId(value);

            if (string.IsNullOrWhiteSpace(id)) {
                Add(diagnostics, ValidationDiagnosticCodes.Id.Required, $"Each {label} must have a nonempty ID.");
            } else if (!seen.Add(id)) {
                Add(diagnostics, ValidationDiagnosticCodes.Id.Duplicate, $"The {label} ID '{id}' is duplicated.",
                    new GraphElementReference(kind, id));
            }
        }
    }

    private static void ValidateGuidIds<T>(
        IEnumerable<T> values,
        Func<T, Guid> getId,
        GraphElementKind kind,
        string label,
        ICollection<SolveDiagnostic> diagnostics) {
        var seen = new HashSet<Guid>();

        foreach (var value in values) {
            var id = getId(value);

            if (id == Guid.Empty) {
                Add(diagnostics, ValidationDiagnosticCodes.Id.Required, $"Each {label} must have a nonempty ID.");
            } else if (!seen.Add(id)) {
                Add(diagnostics, ValidationDiagnosticCodes.Id.Duplicate, $"The {label} ID '{id:D}' is duplicated.",
                    new GraphElementReference(kind, id));
            }
        }
    }

    private static void ValidateRecipes(
        IEnumerable<Recipe> recipes,
        ISet<string> itemIds,
        NumericPolicy numericPolicy,
        ICollection<SolveDiagnostic> diagnostics) {
        foreach (var recipe in recipes) {
            var reference = Reference(GraphElementKind.Recipe, recipe.Id);
            ValidateRecipeItems(recipe.Inputs, "Inputs", recipe, reference, itemIds, numericPolicy, diagnostics);
            ValidateRecipeItems(recipe.Outputs, "Outputs", recipe, reference, itemIds, numericPolicy, diagnostics);
        }
    }

    private static void ValidateRecipeItems(
        IReadOnlyList<RecipeItem> values,
        string collection,
        Recipe recipe,
        GraphElementReference? recipeReference,
        ISet<string> itemIds,
        NumericPolicy numericPolicy,
        ICollection<SolveDiagnostic> diagnostics) {
        foreach (var item in values) {
            if (string.IsNullOrWhiteSpace(item.ItemId)) {
                Add(diagnostics, ValidationDiagnosticCodes.Id.Required, $"Recipe '{recipe.Id}'.{collection} contains a missing item ID.",
                    recipeReference);
            } else if (!itemIds.Contains(item.ItemId)) {
                Add(
                    diagnostics,
                    ValidationDiagnosticCodes.Reference.NotFound,
                    $"Recipe '{recipe.Id}'.{collection} references missing item '{item.ItemId}'.",
                    recipeReference,
                    new(GraphElementKind.Item, item.ItemId));
            }

            ValidatePositive(item.Amount, $"Recipe '{recipe.Id}'.{collection} amount", recipeReference, numericPolicy, diagnostics);
        }
    }

    private static void ValidateMachineGroups(
        IEnumerable<MachineGroup> machineGroups,
        IReadOnlyDictionary<string, Recipe> recipesById,
        NumericPolicy numericPolicy,
        ICollection<SolveDiagnostic> diagnostics) {
        foreach (var group in machineGroups) {
            var reference = Reference(GraphElementKind.MachineGroup, group.Id);

            if (string.IsNullOrWhiteSpace(group.RecipeId)) {
                Add(diagnostics, ValidationDiagnosticCodes.Id.Required, $"Machine group '{group.Id:D}' must reference a recipe.",
                    reference);
            } else if (!recipesById.ContainsKey(group.RecipeId)) {
                Add(
                    diagnostics,
                    ValidationDiagnosticCodes.Reference.NotFound,
                    $"Machine group '{group.Id:D}' references missing recipe '{group.RecipeId}'.",
                    reference,
                    new(GraphElementKind.Recipe, group.RecipeId));
            }

            ValidatePositive(group.ActivityRatePerMachine, $"Machine group '{group.Id:D}'.ActivityRatePerMachine", reference,
                numericPolicy, diagnostics);

            if (group.AvailableMachineCount is < 0) {
                Add(diagnostics, ValidationDiagnosticCodes.Number.Negative,
                    $"Machine group '{group.Id:D}'.AvailableMachineCount must be nonnegative.", reference);
            }

            if (group.ActivityCapacity is { } capacity) {
                ValidateNonnegative(capacity, $"Machine group '{group.Id:D}'.ActivityCapacity", reference, numericPolicy, diagnostics);
            }
        }
    }

    private static void ValidateConnections(
        IEnumerable<ItemConnection> connections,
        IReadOnlyDictionary<Guid, MachineGroup> machineGroups,
        IReadOnlyDictionary<string, Recipe> recipes,
        ISet<string> itemIds,
        NumericPolicy numericPolicy,
        ICollection<SolveDiagnostic> diagnostics) {
        foreach (var connection in connections) {
            var reference = Reference(GraphElementKind.Connection, connection.Id);

            var source = ResolveMachineGroup(connection.SourceMachineGroupId, "source", "Connection", connection.Id, reference,
                machineGroups, diagnostics);

            var destination = ResolveMachineGroup(connection.DestinationMachineGroupId, "destination", "Connection", connection.Id,
                reference, machineGroups, diagnostics);

            var itemExists = ValidateItemReference(connection.ItemId, "Connection", connection.Id, reference, itemIds, diagnostics);
            var sourceRecipe = GetRecipe(source, recipes);
            var destinationRecipe = GetRecipe(destination, recipes);

            if (itemExists && sourceRecipe is not null && !Produces(sourceRecipe, connection.ItemId)) {
                Add(diagnostics, ValidationDiagnosticCodes.Item.Incompatible,
                    $"Connection '{connection.Id:D}' source does not produce item '{connection.ItemId}'.", reference,
                    Reference(GraphElementKind.MachineGroup, connection.SourceMachineGroupId));
            }

            if (itemExists && destinationRecipe is not null && !Consumes(destinationRecipe, connection.ItemId)) {
                Add(diagnostics, ValidationDiagnosticCodes.Item.Incompatible,
                    $"Connection '{connection.Id:D}' destination does not consume item '{connection.ItemId}'.", reference,
                    Reference(GraphElementKind.MachineGroup, connection.DestinationMachineGroupId));
            }

            if (connection.Capacity is { } capacity) {
                ValidateNonnegative(capacity, $"Connection '{connection.Id:D}'.Capacity", reference, numericPolicy, diagnostics);
            }
        }
    }

    private static void ValidateSupplies(
        IEnumerable<Supply> supplies,
        IReadOnlyDictionary<Guid, MachineGroup> machineGroups,
        IReadOnlyDictionary<string, Recipe> recipes,
        ISet<string> itemIds,
        NumericPolicy numericPolicy,
        ICollection<SolveDiagnostic> diagnostics) {
        foreach (var supply in supplies) {
            var reference = Reference(GraphElementKind.Supply, supply.Id);

            var destination = ResolveMachineGroup(supply.DestinationMachineGroupId, "destination", "Supply", supply.Id, reference,
                machineGroups, diagnostics);

            var itemExists = ValidateItemReference(supply.ItemId, "Supply", supply.Id, reference, itemIds, diagnostics);
            var recipe = GetRecipe(destination, recipes);

            if (itemExists && recipe is not null && !Consumes(recipe, supply.ItemId)) {
                Add(diagnostics, ValidationDiagnosticCodes.Item.Incompatible,
                    $"Supply '{supply.Id:D}' destination does not consume item '{supply.ItemId}'.", reference,
                    Reference(GraphElementKind.MachineGroup, supply.DestinationMachineGroupId));
            }

            if (supply.Capacity is { } capacity) {
                ValidateNonnegative(capacity, $"Supply '{supply.Id:D}'.Capacity", reference, numericPolicy, diagnostics);
            }
        }
    }

    private static void ValidateProductOutlets(
        IEnumerable<ProductOutlet> outlets,
        IReadOnlyDictionary<Guid, MachineGroup> machineGroups,
        IReadOnlyDictionary<string, Recipe> recipes,
        ISet<string> itemIds,
        NumericPolicy numericPolicy,
        ICollection<SolveDiagnostic> diagnostics) {
        foreach (var outlet in outlets) {
            var reference = Reference(GraphElementKind.ProductOutlet, outlet.Id);

            var source = ResolveMachineGroup(outlet.SourceMachineGroupId, "source", "Product outlet", outlet.Id, reference, machineGroups,
                diagnostics);

            var itemExists = ValidateItemReference(outlet.ItemId, "Product outlet", outlet.Id, reference, itemIds, diagnostics);
            var recipe = GetRecipe(source, recipes);

            if (itemExists && recipe is not null && !Produces(recipe, outlet.ItemId)) {
                Add(diagnostics, ValidationDiagnosticCodes.Item.Incompatible,
                    $"Product outlet '{outlet.Id:D}' source does not produce item '{outlet.ItemId}'.", reference,
                    Reference(GraphElementKind.MachineGroup, outlet.SourceMachineGroupId));
            }

            if (outlet.Capacity is { } capacity) {
                ValidateNonnegative(capacity, $"Product outlet '{outlet.Id:D}'.Capacity", reference, numericPolicy, diagnostics);
            }
        }
    }

    private static void ValidateSurplusOutlets(
        IEnumerable<SurplusOutlet> outlets,
        IReadOnlyDictionary<Guid, MachineGroup> machineGroups,
        IReadOnlyDictionary<string, Recipe> recipes,
        ISet<string> itemIds,
        NumericPolicy numericPolicy,
        ICollection<SolveDiagnostic> diagnostics) {
        foreach (var outlet in outlets) {
            var reference = Reference(GraphElementKind.SurplusOutlet, outlet.Id);

            var source = ResolveMachineGroup(outlet.SourceMachineGroupId, "source", "Surplus outlet", outlet.Id, reference, machineGroups,
                diagnostics);

            var itemExists = ValidateItemReference(outlet.ItemId, "Surplus outlet", outlet.Id, reference, itemIds, diagnostics);
            var recipe = GetRecipe(source, recipes);

            if (itemExists && recipe is not null && !Produces(recipe, outlet.ItemId)) {
                Add(diagnostics, ValidationDiagnosticCodes.Item.Incompatible,
                    $"Surplus outlet '{outlet.Id:D}' source does not produce item '{outlet.ItemId}'.", reference,
                    Reference(GraphElementKind.MachineGroup, outlet.SourceMachineGroupId));
            }

            if (outlet.Capacity is { } capacity) {
                ValidateNonnegative(capacity, $"Surplus outlet '{outlet.Id:D}'.Capacity", reference, numericPolicy, diagnostics);
            }
        }
    }

    private static MachineGroup? ResolveMachineGroup(
        Guid machineGroupId,
        string endpoint,
        string ownerKind,
        Guid ownerId,
        GraphElementReference? ownerReference,
        IReadOnlyDictionary<Guid, MachineGroup> machineGroups,
        ICollection<SolveDiagnostic> diagnostics) {
        if (machineGroupId != Guid.Empty && machineGroups.TryGetValue(machineGroupId, out var machineGroup)) {
            return machineGroup;
        }

        Add(
            diagnostics,
            ValidationDiagnosticCodes.Reference.NotFound,
            $"{ownerKind} '{ownerId:D}' references missing {endpoint} machine group '{machineGroupId:D}'.",
            ownerReference,
            Reference(GraphElementKind.MachineGroup, machineGroupId));

        return null;
    }

    private static bool ValidateItemReference(
        string itemId,
        string ownerKind,
        Guid ownerId,
        GraphElementReference? ownerReference,
        ISet<string> itemIds,
        ICollection<SolveDiagnostic> diagnostics) {
        if (string.IsNullOrWhiteSpace(itemId)) {
            Add(diagnostics, ValidationDiagnosticCodes.Id.Required, $"{ownerKind} '{ownerId:D}' must reference an item.", ownerReference);

            return false;
        }

        if (!itemIds.Contains(itemId)) {
            Add(
                diagnostics,
                ValidationDiagnosticCodes.Reference.NotFound,
                $"{ownerKind} '{ownerId:D}' references missing item '{itemId}'.",
                ownerReference,
                new(GraphElementKind.Item, itemId));

            return false;
        }

        return true;
    }

    private static Recipe? GetRecipe(
        MachineGroup? machineGroup,
        IReadOnlyDictionary<string, Recipe> recipes) {
        return machineGroup is not null && recipes.TryGetValue(machineGroup.RecipeId, out var recipe)
            ? recipe
            : null;
    }

    private static bool Produces(Recipe recipe, string itemId) => recipe.Outputs.Any(item => item.ItemId == itemId);

    private static bool Consumes(Recipe recipe, string itemId) => recipe.Inputs.Any(item => item.ItemId == itemId);

    private static void ValidatePositive(
        double value,
        string property,
        GraphElementReference? element,
        NumericPolicy numericPolicy,
        ICollection<SolveDiagnostic> diagnostics) {
        if (!double.IsFinite(value)) {
            Add(diagnostics, ValidationDiagnosticCodes.Number.NonFinite, $"{property} must be finite.", element);
        } else if (numericPolicy.NormalizeNearZero(value) <= 0) {
            Add(diagnostics, ValidationDiagnosticCodes.Number.PositiveRequired, $"{property} must be positive.", element);
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

    private static IReadOnlyDictionary<TKey, TValue> FirstById<TValue, TKey>(
        IEnumerable<TValue> values,
        Func<TValue, TKey> getId,
        Func<TKey, bool> include)
        where TKey : notnull {
        var result = new Dictionary<TKey, TValue>();

        foreach (var value in values) {
            var id = getId(value);
            if (include(id)) result.TryAdd(id, value);
        }

        return result;
    }

    private static GraphElementReference? Reference(GraphElementKind kind, string id) {
        return string.IsNullOrWhiteSpace(id) ? null : new GraphElementReference(kind, id);
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
