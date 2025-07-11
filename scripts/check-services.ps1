# Service Health Check Script

function Write-ColorOutput {
    param(
        [string]$Color,
        [string]$Message
    )
    
    switch ($Color) {
        "Red"    { Write-Host $Message -ForegroundColor Red }
        "Green"  { Write-Host $Message -ForegroundColor Green }
        "Yellow" { Write-Host $Message -ForegroundColor Yellow }
        "Cyan"   { Write-Host $Message -ForegroundColor Cyan }
        default  { Write-Host $Message }
    }
}

function Test-ServiceHealth {
    param(
        [string]$ServiceName,
        [string]$Url
    )

    try {
        $response = Invoke-WebRequest -Uri "$Url/health" -Method Get -TimeoutSec 5 -UseBasicParsing
        if ($response.StatusCode -eq 200) {
            Write-ColorOutput "Green" "  ✓ $ServiceName : Healthy"
            return $true
        } else {
            Write-ColorOutput "Red" "  ✗ $ServiceName : Unhealthy (Status: $($response.StatusCode))"
            return $false
        }
    } catch {
        Write-ColorOutput "Red" "  ✗ $ServiceName : Not available ($($_.Exception.Message))"
        return $false
    }
}

Write-ColorOutput "Cyan" "SafarMall Service Health Check"
Write-ColorOutput "Yellow" "=============================="

# Change to project root
$projectRoot = Split-Path -Parent $PSScriptRoot
Set-Location $projectRoot

# Check Docker services
Write-ColorOutput "Yellow" "Checking Docker containers..."
try {
    $containers = docker-compose ps --format json | ConvertFrom-Json
    $runningCount = ($containers | Where-Object { $_.State -eq "running" }).Count
    $totalCount = $containers.Count

    Write-ColorOutput "Cyan" "Docker Status: $runningCount/$totalCount containers running"

    if ($runningCount -eq 0) {
        Write-ColorOutput "Red" "No containers are running. Start services with:"
        Write-ColorOutput "White" "  .\scripts\docker-run.ps1 dev"
        exit 1
    }
} catch {
    Write-ColorOutput "Red" "Unable to check Docker containers. Is Docker running?"
    exit 1
}

# Check service health endpoints
Write-ColorOutput "Yellow" "`nChecking service health endpoints..."

$services = @(
    @{ Name = "User Management"; Url = "http://localhost:7070" },
    @{ Name = "Wallet Payment";  Url = "http://localhost:7241" },
    @{ Name = "Order Service";   Url = "http://localhost:60103" },
    @{ Name = "Payment Gateway"; Url = "http://localhost:7002" }
)

$healthyCount = 0
foreach ($service in $services) {
    if (Test-ServiceHealth $service.Name $service.Url) {
        $healthyCount++
    }
}

Write-ColorOutput "Yellow" "`nHealth Summary:"
Write-ColorOutput "Cyan"   "Healthy Services: $healthyCount/$($services.Count)"

if ($healthyCount -eq $services.Count) {
    Write-ColorOutput "Green" "`n✓ All services are healthy! Ready for testing."
    exit 0
} else {
    Write-ColorOutput "Red" "`n✗ Some services are not healthy. Wait a moment and try again."
    Write-ColorOutput "White" "Or restart services: .\scripts\docker-run.ps1 fix"
    exit 1
}
