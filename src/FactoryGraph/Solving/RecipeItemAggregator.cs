using FactoryGraph.Contracts;

namespace FactoryGraph.Solving;

internal static class RecipeItemAggregator {
    public static RecipeItem[] Aggregate(IReadOnlyList<RecipeItem> items) {
        var indexes = new Dictionary<string, int>(items.Count, StringComparer.Ordinal);
        var result = new List<RecipeItem>(items.Count);

        foreach (var item in items) {
            if (indexes.TryGetValue(item.ItemId, out var index)) {
                result[index] = result[index] with { Amount = result[index].Amount + item.Amount };
            } else {
                indexes.Add(item.ItemId, result.Count);
                result.Add(item);
            }
        }

        return result.ToArray();
    }
}
