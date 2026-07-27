# FactoryGraph

FactoryGraph is a .NET library for computing steady-state production graphs. The repository also hosts exporters for presenting computed graphs.

The repository is currently a foundation for the processor work tracked in [Factory Planner issue #28](https://github.com/caneva20/factory-planner/issues/28). Domain contracts and solver behavior will be added through focused follow-up issues.

## Projects

| Project                              | Purpose                             | Package                         |
|--------------------------------------|-------------------------------------|---------------------------------|
| `src/FactoryGraph`                   | Core graph contracts and processing | `FactoryGraph`                  |
| `src/FactoryGraph.Exporter.Mermaid`  | Mermaid output for computed graphs  | `FactoryGraph.Exporter.Mermaid` |
| `tests/FactoryGraph.Tests`           | Architecture and scenario tests     | Not packable                    |
| `benchmarks/FactoryGraph.Benchmarks` | Reproducible performance benchmarks | Not packable                    |

## Build

.NET 10 is required. The SDK feature band is pinned by `global.json`.

```console
dotnet restore FactoryGraph.slnx
dotnet build FactoryGraph.slnx --configuration Release --no-restore
dotnet test FactoryGraph.slnx --configuration Release --no-build
dotnet pack src/FactoryGraph/FactoryGraph.csproj --configuration Release --no-build
dotnet pack src/FactoryGraph.Exporter.Mermaid/FactoryGraph.Exporter.Mermaid.csproj --configuration Release --no-build
```

See [CONTRIBUTING.md](CONTRIBUTING.md) for development, scenario, package, and versioning conventions.
