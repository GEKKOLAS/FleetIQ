#!/usr/bin/env bash
# Creates FleetIQ.sln and registers every project. Run once after cloning.
set -euo pipefail
cd "$(dirname "$0")/.."

dotnet new sln -n FleetIQ --force

find src tests -name "*.csproj" -print0 | while IFS= read -r -d '' proj; do
  echo "adding $proj"
  dotnet sln FleetIQ.sln add "$proj"
done

echo "Done. Now: dotnet build FleetIQ.sln"
