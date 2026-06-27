# Starts the core: IdentityServer (5001) + Gateway (5080), each in its own window.
# Builds the solution once first, then runs with --no-build (avoids the 14-builds-at-once race).
# Prereq: docker compose up -d
$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')

dotnet dev-certs https --trust 2>$null | Out-Null

Write-Host "Building the solution once..." -ForegroundColor Yellow
dotnet build FleetIQ.slnx -c Debug --nologo
if ($LASTEXITCODE -ne 0) { Write-Host "Build failed. Fix errors and retry." -ForegroundColor Red; exit 1 }

$core = @(
    @{ name = 'IdentityServer'; proj = 'src/IdentityServer/FleetIQ.IdentityServer'; url = 'https://localhost:5001' }
    @{ name = 'Gateway';        proj = 'src/Gateway/FleetIQ.Gateway';                url = 'http://localhost:5080'  }
)

foreach ($a in $core) {
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    Write-Host "Starting $($a.name) -> $($a.url)" -ForegroundColor Cyan
    Start-Process -FilePath 'dotnet' -ArgumentList "run --project `"$($a.proj)`" --no-build --no-launch-profile --urls $($a.url)" -WorkingDirectory (Get-Location)
    Start-Sleep -Seconds 1
}

Write-Host ""
Write-Host "Core started.  IdentityServer: https://localhost:5001   Gateway: http://localhost:5080" -ForegroundColor Green
Write-Host "Now start services with:  .\scripts\start-service.ps1 <Name>" -ForegroundColor DarkGray
