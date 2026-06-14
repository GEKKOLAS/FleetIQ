# Launches IdentityServer + Gateway + all 12 services, each in its own window.
# Prereqs: docker compose up -d  (infra)  AND  .\scripts\add-migrations.ps1 run once.
$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')

# Trust the local HTTPS dev certificate (services run on https://localhost:50xx)
dotnet dev-certs https --trust 2>$null | Out-Null

# NOTE: elements are separated by NEW LINES (no commas) - avoids trailing-comma errors.
$apps = @(
    @{ name = 'IdentityServer'; proj = 'src/IdentityServer/FleetIQ.IdentityServer';        url = 'https://localhost:5001' }
    @{ name = 'Gateway';        proj = 'src/Gateway/FleetIQ.Gateway';                       url = 'http://localhost:5080'  }
    @{ name = 'Orders';         proj = 'src/Services/Orders/FleetIQ.Orders.Api';            url = 'https://localhost:5081' }
    @{ name = 'Identity';       proj = 'src/Services/Identity/FleetIQ.Identity.Api';        url = 'https://localhost:5082' }
    @{ name = 'Carriers';       proj = 'src/Services/Carriers/FleetIQ.Carriers.Api';        url = 'https://localhost:5083' }
    @{ name = 'Planning';       proj = 'src/Services/Planning/FleetIQ.Planning.Api';        url = 'https://localhost:5084' }
    @{ name = 'Tracking';       proj = 'src/Services/Tracking/FleetIQ.Tracking.Api';        url = 'https://localhost:5085' }
    @{ name = 'Billing';        proj = 'src/Services/Billing/FleetIQ.Billing.Api';          url = 'https://localhost:5086' }
    @{ name = 'Documents';      proj = 'src/Services/Documents/FleetIQ.Documents.Api';      url = 'https://localhost:5087' }
    @{ name = 'Notifications';  proj = 'src/Services/Notifications/FleetIQ.Notifications.Api'; url = 'https://localhost:5088' }
    @{ name = 'Analytics';      proj = 'src/Services/Analytics/FleetIQ.Analytics.Api';      url = 'https://localhost:5089' }
    @{ name = 'Compliance';     proj = 'src/Services/Compliance/FleetIQ.Compliance.Api';    url = 'https://localhost:5090' }
    @{ name = 'Integration';    proj = 'src/Services/Integration/FleetIQ.Integration.Api';  url = 'https://localhost:5091' }
    @{ name = 'Intelligence';   proj = 'src/Services/Intelligence/FleetIQ.Intelligence.Api'; url = 'https://localhost:5092' }
)

foreach ($a in $apps) {
    $env:ASPNETCORE_URLS        = $a.url
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    Write-Host "Starting $($a.name) -> $($a.url)" -ForegroundColor Cyan
    Start-Process -FilePath 'dotnet' -ArgumentList "run --project `"$($a.proj)`"" -WorkingDirectory (Get-Location)
    Start-Sleep -Seconds 1
}

Write-Host ""
Write-Host "All services starting. Front-end entry point (gateway): http://localhost:5080" -ForegroundColor Green
Write-Host "IdentityServer: https://localhost:5001   (token endpoint: /connect/token)" -ForegroundColor Green