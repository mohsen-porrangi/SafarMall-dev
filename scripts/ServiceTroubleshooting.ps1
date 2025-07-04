# Service Troubleshooting Script
# Diagnose and fix SafarMall service issues

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
        "Magenta" { Write-Host $Message -ForegroundColor Magenta }
        default  { Write-Host $Message }
    }
}

function Check-ServiceLogs {
    param([string]$ServiceName)
    
    Write-ColorOutput "Yellow" "Checking logs for $ServiceName..."
    docker-compose logs --tail=20 $ServiceName
}

function Restart-Service {
    param([string]$ServiceName)
    
    Write-ColorOutput "Yellow" "Restarting $ServiceName..."
    docker-compose restart $ServiceName
    Start-Sleep -Seconds 10
}

function Check-DatabaseConnection {
    Write-ColorOutput "Yellow" "Checking SQL Server connection..."
    
    try {
        $sqlCheck = docker-compose exec sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "SafarMall123!" -Q "SELECT 1" -C 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-ColorOutput "Green" "✓ SQL Server is accessible"
            return $true
        } else {
            Write-ColorOutput "Red" "✗ SQL Server connection failed"
            return $false
        }
    } catch {
        Write-ColorOutput "Red" "✗ Unable to check SQL Server"
        return $false
    }
}

function Wait-ForService {
    param(
        [string]$ServiceName,
        [string]$Url,
        [int]$MaxAttempts = 12
    )
    
    Write-ColorOutput "Yellow" "Waiting for $ServiceName to become healthy..."
    
    for ($i = 1; $i -le $MaxAttempts; $i++) {
        try {
            $response = Invoke-WebRequest -Uri "$Url/health" -Method Get -TimeoutSec 3 -UseBasicParsing
            if ($response.StatusCode -eq 200) {
                Write-ColorOutput "Green" "✓ $ServiceName is now healthy"
                return $true
            }
        } catch {
            Write-Host "." -NoNewline
        }
        
        Start-Sleep -Seconds 5
    }
    
    Write-ColorOutput "Red" "`n✗ $ServiceName did not become healthy"
    return $false
}

# Main troubleshooting script
Write-ColorOutput "Cyan" "SafarMall Service Troubleshooting"
Write-ColorOutput "Yellow" "================================="

# Step 1: Check database
Write-ColorOutput "Magenta" "`nStep 1: Checking database connectivity..."
$dbHealthy = Check-DatabaseConnection

if (-not $dbHealthy) {
    Write-ColorOutput "Yellow" "Database issue detected. Restarting SQL Server..."
    Restart-Service "sqlserver"
    Start-Sleep -Seconds 15
    $dbHealthy = Check-DatabaseConnection
}

# Step 2: Check and fix problematic services
$problemServices = @(
    @{ Name = "usermanagement-api"; Url = "http://localhost:7070"; DisplayName = "User Management" },
    @{ Name = "order-api"; Url = "http://localhost:60103"; DisplayName = "Order Service" }
)

foreach ($service in $problemServices) {
    Write-ColorOutput "Magenta" "`nStep 2: Diagnosing $($service.DisplayName)..."
    
    # Check logs first
    Check-ServiceLogs $service.Name
    
    # Restart the service
    Restart-Service $service.Name
    
    # Wait for it to become healthy
    $healthy = Wait-ForService $service.DisplayName $service.Url
    
    if (-not $healthy) {
        Write-ColorOutput "Red" "Failed to fix $($service.DisplayName). Manual intervention required."
        Write-ColorOutput "Yellow" "Try these steps:"
        Write-ColorOutput "White" "  1. docker-compose down"
        Write-ColorOutput "White" "  2. docker-compose up -d"
        Write-ColorOutput "White" "  3. Check logs: docker-compose logs $($service.Name)"
    }
}

# Step 3: Final health check
Write-ColorOutput "Magenta" "`nStep 3: Final health verification..."
Start-Sleep -Seconds 5

$services = @(
    @{ Name = "User Management"; Url = "http://localhost:7070" },
    @{ Name = "Wallet Payment";  Url = "http://localhost:7241" },
    @{ Name = "Order Service";   Url = "http://localhost:60103" },
    @{ Name = "Payment Gateway"; Url = "http://localhost:7002" }
)

$healthyCount = 0
foreach ($service in $services) {
    try {
        $response = Invoke-WebRequest -Uri "$($service.Url)/health" -Method Get -TimeoutSec 5 -UseBasicParsing
        if ($response.StatusCode -eq 200) {
            Write-ColorOutput "Green" "  ✓ $($service.Name) : Healthy"
            $healthyCount++
        } else {
            Write-ColorOutput "Red" "  ✗ $($service.Name) : Unhealthy"
        }
    } catch {
        Write-ColorOutput "Red" "  ✗ $($service.Name) : Not available"
    }
}

Write-ColorOutput "Yellow" "`nTroubleshooting Summary:"
Write-ColorOutput "Cyan"   "Healthy Services: $healthyCount/$($services.Count)"

if ($healthyCount -eq $services.Count) {
    Write-ColorOutput "Green" "`n✓ All services are now healthy!"
    Write-ColorOutput "Cyan" "You can now run integration tests:"
    Write-ColorOutput "White" "  dotnet test test/Integration/SafarMall.IntegrationTests/"
} else {
    Write-ColorOutput "Red" "`n✗ Some services still have issues."
    Write-ColorOutput "Yellow" "Additional steps to try:"
    Write-ColorOutput "White" "  1. Check Docker resources (CPU/Memory)"
    Write-ColorOutput "White" "  2. Restart Docker Desktop"
    Write-ColorOutput "White" "  3. Clean rebuild: docker-compose down && docker-compose build --no-cache"
    Write-ColorOutput "White" "  4. Check Windows Firewall/Antivirus"
}

Write-ColorOutput "Cyan" "`nFor detailed logs, run:"
Write-ColorOutput "White" "  docker-compose logs [service-name]"