#!/usr/bin/env bash

set -euo pipefail

skip_smoke=false
if [[ "${1:-}" == "--skip-smoke" ]]; then
  skip_smoke=true
elif [[ -n "${1:-}" ]]; then
  echo "usage: $0 [--skip-smoke]" >&2
  exit 1
fi

script_directory="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repository_directory="$(cd "$script_directory/.." && pwd)"
feed_directory="$repository_directory/artifacts/local-packages"
staging_directory="$(mktemp -d)"
trap 'rm -rf "$staging_directory"' EXIT

version="$("$script_directory/package-version.sh")-local.$(date -u +%Y%m%dT%H%M%S)"

dotnet pack "$repository_directory/FactoryGraph.slnx" \
  --configuration Release \
  --output "$staging_directory" \
  -p:Version="$version"

"$script_directory/verify-packages.sh" "$staging_directory" "$version"

if [[ "$skip_smoke" == false ]]; then
  "$script_directory/test-packages.sh" "$staging_directory" "$version"
fi

mkdir -p "$feed_directory"
find "$feed_directory" -name '*-local.*' -delete
cp "$staging_directory"/*.*nupkg "$feed_directory/"

echo
echo "Local feed: $feed_directory"
echo "Version:    $version"
