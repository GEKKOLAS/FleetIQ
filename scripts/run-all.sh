#!/usr/bin/env bash
# Launches IdentityServer + Gateway + all 12 services in the background.
# Prereqs: docker compose up -d  AND  ./scripts/add-migrations.sh run once.
set -euo pipefail
cd "$(dirname "$0")/.."

dotnet dev-certs https --trust 2>/dev/null || true

pids=()
run() { ASPNETCORE_URLS="$2" ASPNETCORE_ENVIRONMENT=Development dotnet run --project "$1" & pids+=($!); sleep 1; }

run "src/IdentityServer/FleetIQ.IdentityServer" "https://localhost:5001"
run "src/Gateway/FleetIQ.Gateway" "http://localhost:5080"
run "src/Services/Orders/FleetIQ.Orders.Api" "https://localhost:5081"
run "src/Services/Identity/FleetIQ.Identity.Api" "https://localhost:5082"
run "src/Services/Carriers/FleetIQ.Carriers.Api" "https://localhost:5083"
run "src/Services/Planning/FleetIQ.Planning.Api" "https://localhost:5084"
run "src/Services/Tracking/FleetIQ.Tracking.Api" "https://localhost:5085"
run "src/Services/Billing/FleetIQ.Billing.Api" "https://localhost:5086"
run "src/Services/Documents/FleetIQ.Documents.Api" "https://localhost:5087"
run "src/Services/Notifications/FleetIQ.Notifications.Api" "https://localhost:5088"
run "src/Services/Analytics/FleetIQ.Analytics.Api" "https://localhost:5089"
run "src/Services/Compliance/FleetIQ.Compliance.Api" "https://localhost:5090"
run "src/Services/Integration/FleetIQ.Integration.Api" "https://localhost:5091"
run "src/Services/Intelligence/FleetIQ.Intelligence.Api" "https://localhost:5092"

echo ""
echo "All services starting. Gateway (front-end entry): http://localhost:5080"
echo "Press Ctrl+C to stop everything."
trap 'kill ${pids[*]} 2>/dev/null || true' INT TERM
wait
