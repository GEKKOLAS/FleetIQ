# Starts ONE service on its fixed port (foreground - open a new terminal per service).
# Usage:  .\scripts\start-service.ps1 Tracking
param([Parameter(Mandatory = $true)][string]$Name)
$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')

$ports = [ordered]@{
    Orders = 5081; Identity = 5082; Carriers = 5083; Planning = 5084; Tracking = 5085; Billing = 5086
    Documents = 5087; Notifications = 5088; Analytics = 5089; Compliance = 5090; Integration = 5091; Intelligence = 5092
}

if (-not $ports.Contains($Name)) {
    Write-Host "Unknown service '$Name'." -ForegroundColor Red
    Write-Host "Valid names: $($ports.Keys -join ', ')" -ForegroundColor Yellow
    exit 1
}

$port = $ports[$Name]
$proj = "src/Services/$Name/FleetIQ.$Name.Api"
$env:ASPNETCORE_ENVIRONMENT = 'Development'

Write-Host "Starting $Name -> https://localhost:$port" -ForegroundColor Cyan
dotnet run --project $proj --no-launch-profile --urls "https://localhost:$port"
