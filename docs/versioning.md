# Packages And Versioning

FactoryGraph publishes three NuGet packages:

| Package                                    | Contents                                              |
|--------------------------------------------|-------------------------------------------------------|
| `FactoryGraph`                             | Public contracts, validation, analysis, and solving   |
| `FactoryGraph.Exporter.Mermaid`            | Mermaid export for requests and computed results      |
| `FactoryGraph.Exporter.Mermaid.Cli`        | The `factory-graph-mermaid` .NET tool                  |

## Release Versions

The repository releases all three packages with one version. A `vMAJOR.MINOR.PATCH` Git tag publishes package version `MAJOR.MINOR.PATCH`. The Mermaid library declares an exact dependency on the same `FactoryGraph` version. The tool carries the matching core and exporter assemblies in its package.

FactoryGraph follows Semantic Versioning. Before `1.0.0`, a minor release can contain incompatible changes to the supported surfaces below. Patch releases remain compatible with their minor release. Starting with `1.0.0`, incompatible changes increment the major version, compatible additions increment the minor version, and compatible fixes increment the patch version.

## Compatibility Surface

Versioning covers:

- public .NET types, members, constructor parameters, and documented behavior;
- serialized `SolveRequest` and `SolveResult` JSON names and enum values;
- diagnostic codes;
- package IDs, dependency relationships, and target frameworks;
- supported native runtime identifiers; and
- CLI command names, options, exit codes, and request format.

The textual wording of diagnostics is not an API contract. Use diagnostic codes in application logic.

FactoryGraph targets .NET 10. Its `Highs.Native` dependency supplies native libraries for `win-x64`, `win-x86`, `linux-x64`, `linux-arm64`, `osx-x64`, and `osx-arm64`. A change to the target framework or removal of a supported runtime follows the breaking-change rules.

## Support And Deprecation

During the `0.x` series, the project supports the latest patch of the current minor release. A deprecated API identifies its replacement in documentation and remains until a version that permits breaking changes. Security or correctness fixes can require an earlier removal when retaining the API would keep consumers exposed to the defect.

## Publication

Regular CI builds and verifies packages without publishing them. The release workflow accepts version tags, repeats the full build and package checks, obtains a short-lived NuGet.org key through Trusted Publishing, and publishes in dependency order.

NuGet.org setup requires:

1. A Trusted Publishing policy for owner `caneva20`, repository `factory-graph`, workflow `release.yml`, and environment `release`.
2. A `release` GitHub environment with the desired reviewer rules.
3. A `NUGET_USER` environment secret containing the NuGet.org profile username.

NuGet versions are immutable. Verify that all three package IDs remain available before pushing the first release tag. The workflow does not skip duplicate packages because a duplicate can indicate a partial earlier publication.
