# End-to-end test of the Planning -> Tracking flow (the "9 steps").
# Prereqs (in separate windows):
#   docker compose up -d postgres rabbitmq
#   .\scripts\start-core.ps1                 # IdentityServer (5001) + Gateway (5080)
#   .\scripts\start-service.ps1 Planning     # 5084
#   .\scripts\start-service.ps1 Tracking     # 5085
# Then run:  .\scripts\test-planning-flow.ps1
#
# Needs PowerShell 7+ (for -SkipCertificateCheck).

$ErrorActionPreference = 'Stop'
$idp = 'https://localhost:5001'
$gw  = 'http://localhost:5080'

function Step($n, $msg) { Write-Host "`n[$n] $msg" -ForegroundColor Cyan }

# --- Token (client_credentials, service client) ---
$clientSecret = (Get-Content (Join-Path $PSScriptRoot "../.env") | Where-Object { $_ -match "^FLEETIQ_SERVICE_CLIENT_SECRET=" }) -replace "^[^=]+=", ""
if (-not $clientSecret) { throw "FLEETIQ_SERVICE_CLIENT_SECRET missing - run scripts/setup-secrets.ps1" }

Step 0 "Requesting service token from IdentityServer..."
$tok = Invoke-RestMethod -Method Post -Uri "$idp/connect/token" -SkipCertificateCheck `
    -ContentType 'application/x-www-form-urlencoded' -Body @{
        grant_type    = 'client_credentials'
        client_id     = 'fleetiq.service'
        client_secret = $clientSecret
        scope         = 'fleetiq.api'
    }
$H = @{ Authorization = "Bearer $($tok.access_token)" }
Write-Host "   token acquired." -ForegroundColor DarkGray

function Post($path, $body) {
    $json = if ($null -ne $body) { $body | ConvertTo-Json -Compress } else { $null }
    return Invoke-RestMethod -Method Post -Uri "$gw$path" -Headers $H -ContentType 'application/json' -Body $json -SkipCertificateCheck
}
function Get-($path) { return Invoke-RestMethod -Method Get -Uri "$gw$path" -Headers $H -SkipCertificateCheck }

try {
    # 1. Create load
    Step 1 "Create load (Lisboa -> Porto)"
    $load = Post '/loads' @{ originCity = 'Lisboa'; destinationCity = 'Porto' }
    $loadId = $load.id
    Write-Host "   loadId = $loadId" -ForegroundColor Green

    # 2. Add an order
    Step 2 "Add order to load"
    Post "/loads/$loadId/orders" @{ orderId = [guid]::NewGuid(); description = 'Pallet electronics'; weightKg = 320 } | Out-Null

    # 3. Set up a real, verified carrier (Carriers service) and assign it.
    Step 3 "Create + verify a carrier, then assign it"
    $carrier = Post '/carriers' @{ legalName = 'Demo Freight SL'; registrationNumber = "ES-$(Get-Random)"; country = 'ES' }
    $carrierId = $carrier.id
    Post "/carriers/$carrierId/vehicles" @{ plate = 'DEMO-01'; type = 'Truck'; capacityKg = 24000 } | Out-Null
    Post "/carriers/$carrierId/drivers"  @{ fullName = 'Demo Driver'; licenceNumber = 'ES-D-0001' } | Out-Null
    Post "/carriers/$carrierId/verify" $null | Out-Null
    Write-Host "   carrierId = $carrierId (verified)" -ForegroundColor Green

    # Give Planning's read-model a moment to consume CarrierVerified before assigning.
    Start-Sleep -Seconds 2
    Post "/loads/$loadId/assign-carrier" @{ carrierId = $carrierId } | Out-Null

    # 4. Create route for the load
    Step 4 "Create route"
    $route = Post '/routes' @{ loadId = $loadId }
    $routeId = $route.id
    Write-Host "   routeId = $routeId" -ForegroundColor Green

    # 5. Add two stops
    Step 5 "Add 2 stops (pickup + delivery)"
    Post "/routes/$routeId/stops" @{ kind = 'Pickup';   city = 'Lisboa'; address = 'Av. Liberdade 1';      latitude = 38.7223; longitude = -9.1393 } | Out-Null
    Post "/routes/$routeId/stops" @{ kind = 'Delivery'; city = 'Porto';  address = 'Rua Santa Catarina 10'; latitude = 41.1496; longitude = -8.6109 } | Out-Null

    # 6. Finalize route
    Step 6 "Finalize route"
    Post "/routes/$routeId/finalize" $null | Out-Null

    # 7. Attach route to load
    Step 7 "Attach route to load"
    Post "/loads/$loadId/attach-route" @{ routeId = $routeId } | Out-Null

    # 8. Plan the load
    Step 8 "Plan load (Draft -> Planned)"
    Post "/loads/$loadId/plan" $null | Out-Null

    # 9. Confirm the load -> publishes LoadConfirmed -> Tracking creates the Shipment
    Step 9 "Confirm load (Planned -> Confirmed)"
    Post "/loads/$loadId/confirm" $null | Out-Null

    $loadAfter = Get- "/loads/$loadId"
    Write-Host "`n   Load status = $($loadAfter.status), reference = $($loadAfter.reference)" -ForegroundColor Green

    # Verify Tracking picked it up (give the bus a moment)
    Step '*' "Waiting for Tracking to create the shipment..."
    $shipment = $null
    foreach ($i in 1..10) {
        Start-Sleep -Milliseconds 800
        $page = Get- "/shipments?pageSize=200"
        $shipment = $page.items | Where-Object { $_.loadId -eq $loadId } | Select-Object -First 1
        if ($shipment) { break }
    }

    if ($shipment) {
        Write-Host "`n SUCCESS: Tracking created shipment $($shipment.id) for load $loadId (status $($shipment.status))." -ForegroundColor Green
        Write-Host " Open the live map (frontend /app/tracking) to watch it move." -ForegroundColor Green
    } else {
        Write-Host "`n Load confirmed, but no shipment appeared yet." -ForegroundColor Yellow
        Write-Host " Check: is RabbitMQ up (docker ps) and is the Tracking service running? Look at the Tracking console for the '[Tracking] Load ... -> shipment ... created' log." -ForegroundColor Yellow
    }
}
catch {
    Write-Host "`n FAILED at the step above." -ForegroundColor Red
    if ($_.ErrorDetails.Message) { Write-Host $_.ErrorDetails.Message -ForegroundColor Red }
    else { Write-Host $_.Exception.Message -ForegroundColor Red }
    exit 1
}
