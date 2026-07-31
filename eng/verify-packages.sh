#!/usr/bin/env bash

set -euo pipefail

package_directory="${1:-}"
version="${2:-}"

if [[ -z "$package_directory" || -z "$version" ]]; then
  echo "usage: $0 <package-directory> <version>" >&2
  exit 1
fi

if [[ ! -d "$package_directory" ]]; then
  echo "Package directory does not exist: $package_directory" >&2
  exit 1
fi

if ! command -v unzip >/dev/null; then
  echo "Package verification requires unzip." >&2
  exit 1
fi

fail() {
  echo "Package verification failed: $1" >&2
  exit 1
}

require_entry() {
  local package="$1"
  local entry="$2"

  if ! unzip -Z1 "$package" | grep -Fqx "$entry"; then
    fail "$(basename "$package") does not contain $entry"
  fi
}

require_nuspec_text() {
  local package="$1"
  local expected="$2"
  local nuspec
  nuspec="$(unzip -p "$package" '*.nuspec')"

  if [[ "$nuspec" != *"$expected"* ]]; then
    fail "$(basename "$package") nuspec does not contain: $expected"
  fi
}

shopt -s nullglob
packages=("$package_directory"/*.nupkg)
symbol_packages=("$package_directory"/*.snupkg)

[[ ${#packages[@]} -eq 3 ]] || fail "expected 3 nupkg files, found ${#packages[@]}"
[[ ${#symbol_packages[@]} -eq 3 ]] || fail "expected 3 snupkg files, found ${#symbol_packages[@]}"

package_ids=(
  FactoryGraph
  FactoryGraph.Exporter.Mermaid
  FactoryGraph.Exporter.Mermaid.Cli
)

for package_id in "${package_ids[@]}"; do
  package="$package_directory/$package_id.$version.nupkg"
  symbol_package="$package_directory/$package_id.$version.snupkg"

  [[ -f "$package" ]] || fail "missing $(basename "$package")"
  [[ -f "$symbol_package" ]] || fail "missing $(basename "$symbol_package")"

  require_nuspec_text "$package" "<id>$package_id</id>"
  require_nuspec_text "$package" "<version>$version</version>"
  require_nuspec_text "$package" "<authors>caneva20</authors>"
  require_nuspec_text "$package" '<license type="expression">MIT</license>'
  require_nuspec_text "$package" '<readme>README.md</readme>'
  require_nuspec_text "$package" '<description>'
  require_nuspec_text "$package" '<repository type="git" url="https://github.com/caneva20/factory-graph" commit="'
  require_nuspec_text "$symbol_package" "<id>$package_id</id>"
  require_nuspec_text "$symbol_package" "<version>$version</version>"
  require_nuspec_text "$symbol_package" '<packageType name="SymbolsPackage" />'
  require_entry "$package" README.md

  entries="$(unzip -Z1 "$package")"
  if grep -Eq '(^|/)(src|tests?)/|\.(cs|csproj|sln|slnx)$' <<<"$entries"; then
    fail "$(basename "$package") contains source, test, or project files"
  fi
done

core="$package_directory/FactoryGraph.$version.nupkg"
core_symbols="$package_directory/FactoryGraph.$version.snupkg"
require_nuspec_text "$core" '<group targetFramework="net10.0">'
require_nuspec_text "$core" '<dependency id="Highs.Native" version="1.15.1" exclude="Build,Analyzers" />'
require_entry "$core" lib/net10.0/FactoryGraph.dll
require_entry "$core" lib/net10.0/FactoryGraph.xml
require_entry "$core_symbols" lib/net10.0/FactoryGraph.pdb

exporter="$package_directory/FactoryGraph.Exporter.Mermaid.$version.nupkg"
exporter_symbols="$package_directory/FactoryGraph.Exporter.Mermaid.$version.snupkg"
require_nuspec_text "$exporter" '<group targetFramework="net10.0">'
require_nuspec_text "$exporter" "<dependency id=\"FactoryGraph\" version=\"[$version]\" exclude=\"Build,Analyzers\" />"
require_entry "$exporter" lib/net10.0/FactoryGraph.Exporter.Mermaid.dll
require_entry "$exporter" lib/net10.0/FactoryGraph.Exporter.Mermaid.xml
require_entry "$exporter_symbols" lib/net10.0/FactoryGraph.Exporter.Mermaid.pdb

cli="$package_directory/FactoryGraph.Exporter.Mermaid.Cli.$version.nupkg"
cli_symbols="$package_directory/FactoryGraph.Exporter.Mermaid.Cli.$version.snupkg"
require_nuspec_text "$cli" '<packageType name="DotnetTool" />'
require_entry "$cli" tools/net10.0/any/DotnetToolSettings.xml
require_entry "$cli" tools/net10.0/any/FactoryGraph.Exporter.Mermaid.Cli.dll
require_entry "$cli" tools/net10.0/any/FactoryGraph.Exporter.Mermaid.dll
require_entry "$cli" tools/net10.0/any/FactoryGraph.Exporter.Mermaid.xml
require_entry "$cli" tools/net10.0/any/FactoryGraph.dll
require_entry "$cli" tools/net10.0/any/FactoryGraph.xml
require_entry "$cli" tools/net10.0/any/Highs.Native.dll

for runtime_asset in \
  tools/net10.0/any/runtimes/linux-arm64/native/highs.so \
  tools/net10.0/any/runtimes/linux-x64/native/highs.so \
  tools/net10.0/any/runtimes/osx-arm64/native/highs.dylib \
  tools/net10.0/any/runtimes/osx-x64/native/highs.dylib \
  tools/net10.0/any/runtimes/win-x64/native/highs.dll \
  tools/net10.0/any/runtimes/win-x86/native/highs.dll; do
  require_entry "$cli" "$runtime_asset"
done

require_entry "$cli_symbols" tools/net10.0/any/FactoryGraph.Exporter.Mermaid.Cli.pdb
require_entry "$cli_symbols" tools/net10.0/any/FactoryGraph.Exporter.Mermaid.pdb
require_entry "$cli_symbols" tools/net10.0/any/FactoryGraph.pdb

echo "Verified FactoryGraph package contents for version $version."
