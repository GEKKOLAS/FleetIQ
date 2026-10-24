# Creates/updates the git-ignored .env with strong random secrets and loads them into the shared
# .NET user-secrets store (UserSecretsId 'fleetiq-local-dev', see Directory.Build.props) that every
# service, the Gateway and IdentityServer read in Development. Nothing secret is ever written to the repo.
#   .\scripts\setup-secrets.ps1           # fill in missing secrets, keep existing ones
#   .\scripts\setup-secrets.ps1 -Rotate   # replace every secret with a new random value
# If the infra containers already exist, the new Postgres/RabbitMQ/MinIO passwords are applied to them.
param([switch]$Rotate)
$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')

function New-Secret([int]$length = 32) {
    # alphanumeric only: values are embedded in connection strings and amqp:// URIs
    $chars = [char[]]'ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789'
    -join (1..$length | ForEach-Object { $chars[[System.Security.Cryptography.RandomNumberGenerator]::GetInt32($chars.Length)] })
}

if (-not (Test-Path .env)) { Copy-Item .env.example .env }
$envMap = [ordered]@{}
foreach ($line in Get-Content .env) {
    if ($line -match '^\s*([A-Z0-9_]+)\s*=(.*)$') { $envMap[$Matches[1]] = $Matches[2].Trim() }
}

$secretKeys = 'POSTGRES_PASSWORD', 'RABBITMQ_DEFAULT_PASS', 'MINIO_ROOT_PASSWORD',
              'FLEETIQ_SERVICE_CLIENT_SECRET', 'FLEETIQ_DEMO_USER_PASSWORD'
$leaked = 'REDACTED', 'REDACTED', 'REDACTED'   # values that were once committed - never reuse
$oldPostgres = $envMap['POSTGRES_PASSWORD']
foreach ($k in $secretKeys) {
    if ($Rotate -or -not $envMap[$k] -or $leaked -contains $envMap[$k]) {
        $envMap[$k] = New-Secret $(if ($k -eq 'FLEETIQ_DEMO_USER_PASSWORD') { 16 } else { 32 })
    }
}
foreach ($k in 'POSTGRES_USER', 'RABBITMQ_DEFAULT_USER', 'MINIO_ROOT_USER') { if (-not $envMap[$k]) { $envMap[$k] = 'fleetiq' } }

$header = '# Local secrets - git-ignored. Managed by scripts/setup-secrets.ps1; never commit this file.'
Set-Content .env -Encoding utf8NoBOM -Value (@($header) + ($envMap.GetEnumerator() | ForEach-Object { "$($_.Key)=$($_.Value)" }))
Write-Host '.env updated' -ForegroundColor Green

# --- apply to running infra ------------------------------------------------------------------------
$pgUser = $envMap['POSTGRES_USER']
$pgRunning = (docker ps --filter 'name=^fleetiq-postgres$' --format '{{.Names}}' 2>$null) -eq 'fleetiq-postgres'
if ($pgRunning -and $envMap['POSTGRES_PASSWORD'] -ne $oldPostgres) {
    # the data volume keeps the old password, so change it in place (local socket auth inside the container)
    docker exec fleetiq-postgres psql -U $pgUser -d fleetiq -c "ALTER USER $pgUser WITH PASSWORD '$($envMap['POSTGRES_PASSWORD'])';" | Out-Null
    Write-Host 'Postgres password rotated' -ForegroundColor Green
}
if (docker ps -a --filter 'name=^fleetiq-' --format '{{.Names}}' 2>$null) {
    # recreates containers whose env changed (RabbitMQ/MinIO pick up the new credentials)
    docker compose up -d --pull never 2>&1 | Out-Null
    Write-Host 'Infra containers updated' -ForegroundColor Green
}

# --- .NET user-secrets (shared by all apps) --------------------------------------------------------
$pg = $envMap['POSTGRES_PASSWORD']
$secrets = [ordered]@{}
foreach ($svc in 'Analytics', 'Billing', 'Carriers', 'Compliance', 'Documents', 'Identity', 'Integration',
                 'Intelligence', 'Notifications', 'Orders', 'Planning', 'Tracking') {
    $secrets["ConnectionStrings:$svc"] = "Host=localhost;Port=5432;Database=fleetiq_$($svc.ToLower());Username=$pgUser;Password=$pg"
}
$secrets['ConnectionStrings:RabbitMq'] = "amqp://$($envMap['RABBITMQ_DEFAULT_USER']):$($envMap['RABBITMQ_DEFAULT_PASS'])@localhost:5672"
$secrets['IdentityServer:ServiceClientSecret'] = $envMap['FLEETIQ_SERVICE_CLIENT_SECRET']
$secrets['IdentityServer:DemoUserPassword'] = $envMap['FLEETIQ_DEMO_USER_PASSWORD']

$dir = Join-Path $env:APPDATA 'Microsoft\UserSecrets\fleetiq-local-dev'
New-Item -ItemType Directory -Force $dir | Out-Null
$secrets | ConvertTo-Json | Set-Content (Join-Path $dir 'secrets.json') -Encoding utf8NoBOM
Write-Host "User-secrets written to $dir" -ForegroundColor Green
Write-Host 'Restart the services (scripts/run-all.ps1) to pick up changes.'
Write-Host "Demo login (alice / bob) password and the fleetiq.service client secret are in .env" -ForegroundColor Cyan
