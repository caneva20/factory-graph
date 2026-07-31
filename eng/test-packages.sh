#!/usr/bin/env bash

set -euo pipefail

package_directory="${1:-}"
version="${2:-}"

if [[ -z "$package_directory" || -z "$version" ]]; then
  echo "usage: $0 <package-directory> <version>" >&2
  exit 1
fi

script_directory="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
package_directory="$(cd "$package_directory" && pwd)"
work_directory="$(mktemp -d)"
trap 'rm -rf "$work_directory"' EXIT

consumer_directory="$work_directory/consumer"
tool_directory="$work_directory/tools"
mkdir -p "$consumer_directory" "$tool_directory"
cp "$script_directory/package-smoke/PackageSmoke.csproj" "$consumer_directory/"
cp "$script_directory/package-smoke/Program.cs" "$consumer_directory/"

config="$work_directory/NuGet.Config"
cat >"$config" <<EOF
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="factory-graph-packages" value="$package_directory" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="factory-graph-packages">
      <package pattern="FactoryGraph*" />
    </packageSource>
    <packageSource key="nuget.org">
      <package pattern="Highs.Native" />
      <package pattern="Microsoft.*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
EOF

dotnet restore "$consumer_directory/PackageSmoke.csproj" \
  --configfile "$config" \
  --packages "$work_directory/packages" \
  --no-cache \
  -p:FactoryGraphPackageVersion="$version"

dotnet run \
  --project "$consumer_directory/PackageSmoke.csproj" \
  --configuration Release \
  --no-restore \
  -p:FactoryGraphPackageVersion="$version"

dotnet tool install FactoryGraph.Exporter.Mermaid.Cli \
  --tool-path "$tool_directory" \
  --version "$version" \
  --configfile "$config" \
  --no-cache

diagram="$work_directory/tool-output.mmd"
"$tool_directory/factory-graph-mermaid" \
  "$script_directory/package-smoke/request.json" \
  --output "$diagram"

if ! grep -Fq 'flowchart LR' "$diagram"; then
  echo "The packaged CLI did not produce a Mermaid flowchart." >&2
  exit 1
fi

echo "Verified clean package consumption for version $version."
