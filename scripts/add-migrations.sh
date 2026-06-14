#!/usr/bin/env bash
# Creates the InitialCreate EF migration for every service (schema only, no data needed).
set -euo pipefail
cd "$(dirname "$0")/.."

dotnet tool install --global dotnet-ef 2>/dev/null || true

for s in Orders Identity Carriers Planning Tracking Billing Documents Notifications Analytics Compliance Integration Intelligence; do
  echo "==> Migration for $s"
  dotnet ef migrations add InitialCreate \
    --project "src/Services/$s/FleetIQ.$s.Infrastructure" \
    --startup-project "src/Services/$s/FleetIQ.$s.Api" \
    --output-dir "Persistence/Migrations"
done
echo "Done. Tables are created automatically on first run (Database.Migrate in Development)."
