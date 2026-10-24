# Exposes the locally running Gateway (5080) and IdentityServer (5001) through Cloudflare quick tunnels
# so the GitHub Pages frontend (https://gekkolas.github.io/FleetIQ/) can reach them.
# Prereqs: infra + all services running (docker compose up -d; .\scripts\run-all.ps1), cloudflared installed
#   (winget install Cloudflare.cloudflared).
# Quick-tunnel URLs change on every run, so this rewrites deploy/pages-config.json and pushes it, which
# redeploys the Pages site (~2 min). Pass -NoPush to only print the URLs. Ctrl+C stops the tunnels.
param([switch]$NoPush)
$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')

$cf = (Get-Command cloudflared -ErrorAction SilentlyContinue).Source
if (-not $cf) { $cf = 'C:\Program Files (x86)\cloudflared\cloudflared.exe' }
if (-not (Test-Path $cf)) { throw 'cloudflared not found. Install it: winget install Cloudflare.cloudflared' }

function Start-Tunnel([string]$name, [string]$origin) {
    $log = Join-Path $env:TEMP "fleetiq-tunnel-$name.log"
    Remove-Item $log -ErrorAction SilentlyContinue
    # --no-tls-verify: the local services use the self-signed ASP.NET dev certificate
    $proc = Start-Process $cf -ArgumentList "tunnel --no-autoupdate --url $origin --no-tls-verify --logfile `"$log`"" -PassThru -WindowStyle Hidden
    for ($i = 0; $i -lt 60; $i++) {
        Start-Sleep -Seconds 1
        if (Test-Path $log) {
            $m = Select-String -Path $log -Pattern 'https://[a-z0-9-]+\.trycloudflare\.com' | Select-Object -First 1
            if ($m) { return @{ proc = $proc; url = $m.Matches[0].Value } }
        }
    }
    Stop-Process $proc.Id -ErrorAction SilentlyContinue
    throw "Tunnel for $name did not come up (see $log)"
}

$gateway = Start-Tunnel 'gateway' 'http://localhost:5080'
$idp     = Start-Tunnel 'idp'     'https://localhost:5001'
Write-Host "Gateway        -> $($gateway.url)" -ForegroundColor Green
Write-Host "IdentityServer -> $($idp.url)"     -ForegroundColor Green

try {
    $config = [ordered]@{ apiBase = $gateway.url; idpAuthority = $idp.url } | ConvertTo-Json
    Set-Content -Path deploy/pages-config.json -Value $config -Encoding utf8NoBOM

    if (-not $NoPush) {
        git add deploy/pages-config.json
        git commit -m "chore: point GitHub Pages frontend at current demo tunnels" | Out-Null
        git push
        Write-Host 'Pushed. Pages redeploys in ~2 min: https://gekkolas.github.io/FleetIQ/' -ForegroundColor Cyan
    }

    Write-Host 'Tunnels running. Press Ctrl+C to stop.' -ForegroundColor Yellow
    while (-not $gateway.proc.HasExited -and -not $idp.proc.HasExited) { Start-Sleep -Seconds 5 }
    Write-Warning 'A tunnel process exited.'
}
finally {
    Stop-Process $gateway.proc.Id, $idp.proc.Id -ErrorAction SilentlyContinue
}
