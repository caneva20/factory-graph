#!/usr/bin/env bash

set -euo pipefail

script_directory="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
props="$script_directory/../Directory.Build.props"

version="$(sed -n 's:.*<VersionPrefix>\(.*\)</VersionPrefix>.*:\1:p' "$props")"

if [[ -z "$version" ]]; then
  echo "VersionPrefix not found in $props" >&2
  exit 1
fi

echo "$version"
