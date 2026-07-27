using FactoryGraph.Contracts;
using System.Collections;

namespace FactoryGraph.Tests.Contracts;

public sealed class ContractShapeTests
{
    [Fact]
    public void ContractCollectionsAreExposedAsReadOnlyInterfaces()
    {
        var contractTypes = new[] {
            typeof(FactoryGraphDefinition),
            typeof(Recipe),
            typeof(SolveRequest),
            typeof(ComputedFactoryGraph),
            typeof(ComputedMachineGroup),
            typeof(SolveDiagnostic),
            typeof(SolveResult)
        };

        var collectionProperties = contractTypes
            .SelectMany(type => type.GetProperties())
            .Where(property => property.PropertyType != typeof(string))
            .Where(property => typeof(IEnumerable).IsAssignableFrom(property.PropertyType))
            .Select(property => property.PropertyType)
            .ToArray();

        Assert.NotEmpty(collectionProperties);

        Assert.All(
            collectionProperties,
            propertyType =>
            {
                Assert.True(propertyType.IsGenericType);
                Assert.Equal(typeof(IReadOnlyList<>), propertyType.GetGenericTypeDefinition());
            });
    }

    [Fact]
    public void MalformedGraphCanBeRepresentedForStructuredValidation()
    {
        var missingMachineGroupId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

        var graph = new FactoryGraphDefinition(
            Items: [new Item("", "Missing ID")],
            Recipes: [],
            MachineGroups: [],
            Connections: [new ItemConnection(Guid.Empty, missingMachineGroupId, missingMachineGroupId, "missing-item")],
            Supplies: [],
            Demands: [],
            SurplusSinks: []);

        Assert.Empty(graph.Items.Single().Id);
        Assert.Equal(missingMachineGroupId, graph.Connections.Single().SourceMachineGroupId);
    }

    [Fact]
    public void ComputedConnectionRetainsInputIdentityAndTopology()
    {
        var connectionId = Guid.Parse("12121212-1212-1212-1212-121212121212");
        var sourceId = Guid.Parse("13131313-1313-1313-1313-131313131313");
        var destinationId = Guid.Parse("14141414-1414-1414-1414-141414141414");
        var input = new ItemConnection(connectionId, sourceId, destinationId, "iron-ore", 20);

        var output = new ComputedConnection(
            connectionId,
            sourceId,
            destinationId,
            "iron-ore",
            Capacity: 20,
            FlowRate: 12);

        Assert.Equal(input.Id, output.Id);
        Assert.Equal(input.SourceMachineGroupId, output.SourceMachineGroupId);
        Assert.Equal(input.DestinationMachineGroupId, output.DestinationMachineGroupId);
        Assert.Equal(input.ItemId, output.ItemId);
        Assert.Equal(input.Capacity, output.Capacity);
    }
}
