# Creates the InitialCreate EF migration for every service.
# A migration builds the SCHEMA (tables) from the EF model - it needs NO data.
$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')

dotnet tool install --global dotnet-ef 2>$null | Out-Null

$services = @('Orders', 'Identity', 'Carriers', 'Planning', 'Tracking', 'Billing', 'Documents', 'Notifications', 'Analytics', 'Compliance', 'Integration', 'Intelligence')
foreach ($s in $services) {
    Write-Host "==> Migration for $s" -ForegroundColor Cyan
    dotnet ef migrations add InitialCreate `
        --project      "src/Services/$s/FleetIQ.$s.Infrastructure" `
        --startup-project "src/Services/$s/FleetIQ.$s.Api" `
        --output-dir   "Persistence/Migrations"
}
Write-Host "Done. Tables are created automatically on first run (Database.Migrate in Development)." -ForegroundColor Green
