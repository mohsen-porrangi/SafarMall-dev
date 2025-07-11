# PowerShell script for managing Docker services

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("dev", "prod", "stop", "clean", "logs", "help", "fix", "status")]
    [string]$Action = "help",
    
    [string]$Service = ""
)

function Write-ColorOutput {
    param(
        [string]$Color,
        [string]$Message
    )
    
    switch ($Color) {
        "Red" { Write-Host $Message -ForegroundColor Red }
        "Green" { Write-Host $Message -ForegroundColor Green }
        "Yellow" { Write-Host $Message -ForegroundColor Yellow }
        "Cyan" { Write-Host $Message -ForegroundColor Cyan }
        "White" { Write-Host $Message -ForegroundColor White }
        "Gray" { Write-Host $Message -ForegroundColor Gray }
        default { Write-Host $Message }
    }
}

function Show-Help {
    Write-ColorOutput "Green" "SafarMall Docker Management Script"
    Write-Host ""
    Write-Host "Usage: .\docker-run.ps1 <action> [service]"
    Write-Host ""
    Write-Host "Actions:"
    Write-Host "  dev     - Start development environment (HTTP only)"
    Write-Host "  prod    - Start production environment (HTTPS)" 
    Write-Host "  stop    - Stop all services"
    Write-Host "  clean   - Clean up Docker resources"
    Write-Host "  fix     - Fix Docker issues and rebuild"
    Write-Host "  status  - Show service status"
    Write-Host "  logs    - Show logs (optionally for specific service)"
    Write-Host "  help    - Show this help message"
    Write-Host ""
    Write-Host "Examples:"
    Write-Host "  .\docker-run.ps1 dev"
    Write-Host "  .\docker-run.ps1 fix"
    Write-Host "  .\docker-run.ps1 logs usermanagement-api"
    Write-Host "  .\docker-run.ps1 status"
    Write-Host "  .\docker-run.ps1 stop"
}

function Set-ProjectRoot {
    $projectRoot = Split-Path -Parent $PSScriptRoot
    Set-Location $projectRoot
    Write-ColorOutput "Yellow" "Working directory: $(Get-Location)"
}

function Test-DockerRunning {
    try {
        docker info | Out-Null
        return $true
    } catch {
        Write-ColorOutput "Red" "Docker is not running. Please start Docker Desktop first."
        return $false
    }
}

function Stop-AllServices {
    Write-ColorOutput "Yellow" "Stopping all services..."
    try {
        # Stop development environment
        docker-compose -f docker-compose.yml -f docker-compose.dev.yml down --volumes --remove-orphans 2>$null
        # Stop production environment
        docker-compose down --volumes --remove-orphans 2>$null
        
        Write-ColorOutput "Green" "Services stopped successfully"
    } catch {
        Write-ColorOutput "Gray" "No services to stop or services already stopped"
    }
}

function Clean-DockerResources {
    Write-ColorOutput "Red" "Cleaning Docker cache and images..."
    try {
        # Stop all running containers
        $containers = docker ps -q
        if ($containers) {
            Write-ColorOutput "Yellow" "Stopping running containers..."
            docker stop $containers | Out-Null
            docker rm $containers | Out-Null
        }
        
        # Remove SafarMall images
        $images = docker images --filter "reference=*safarmall*" -q
        if ($images) {
            Write-ColorOutput "Yellow" "Removing SafarMall images..."
            docker rmi -f $images | Out-Null
        }
        
        # Clean Docker system
        Write-ColorOutput "Yellow" "Cleaning Docker system..."
        docker system prune -f --volumes | Out-Null
        
        Write-ColorOutput "Green" "Docker cleanup completed"
    } catch {
        Write-ColorOutput "Red" "Error during cleanup: $($_.Exception.Message)"
    }
}

function Show-ServiceStatus {
    Write-ColorOutput "Cyan" "Service Status:"
    Write-Host ""
    
    try {
        $containers = docker ps -a --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" | Where-Object { $_ -match "safarmall" }
        
        if ($containers) {
            Write-Host $containers
        } else {
            Write-ColorOutput "Yellow" "No SafarMall containers found"
        }
    } catch {
        Write-ColorOutput "Red" "Error getting service status: $($_.Exception.Message)"
    }
    
    Write-Host ""
}

function Wait-ForHealthy {
    param([string]$ServiceName, [int]$TimeoutSeconds = 60)
    
    Write-ColorOutput "Yellow" "Waiting for $ServiceName to be healthy..."
    $elapsed = 0
    
    while ($elapsed -lt $TimeoutSeconds) {
        try {
            $status = docker inspect --format='{{.State.Health.Status}}' "safarmall-$ServiceName-dev" 2>$null
            if ($status -eq "healthy") {
                Write-ColorOutput "Green" "$ServiceName is healthy"
                return $true
            }
        } catch {
            # Service might not have health check
        }
        
        Start-Sleep 2
        $elapsed += 2
        Write-Host "." -NoNewline
    }
    
    Write-Host ""
    Write-ColorOutput "Yellow" "$ServiceName health check timeout (this might be normal for services without health checks)"
    return $false
}

if ($Action -eq "help") {
    Show-Help
    exit 0
}

Write-ColorOutput "Green" "SafarMall Docker Management Script"
Write-ColorOutput "Yellow" "Action: $Action"

if (-not (Test-DockerRunning)) {
    exit 1
}

Set-ProjectRoot

switch ($Action) {
    "dev" {
        Write-ColorOutput "Cyan" "Starting Development Environment (HTTP Only)..."
        
        # Stop existing containers
        Write-ColorOutput "Yellow" "Stopping existing containers..."
        Stop-AllServices
        
        # Build and start development environment
        Write-ColorOutput "Cyan" "Building and starting services..."
        try {
            # Build first
            Write-ColorOutput "Yellow" "Building images..."
            docker-compose -f docker-compose.yml -f docker-compose.dev.yml build
            
            # Start SQL Server first
            Write-ColorOutput "Yellow" "Starting SQL Server..."
            docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d sqlserver
            
            # Wait for SQL Server
            Wait-ForHealthy "sqlserver"
            
            # Start other services
            Write-ColorOutput "Yellow" "Starting application services..."
            docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d
            
            Write-Host ""
            Write-ColorOutput "Green" "Development environment started!"
            Write-ColorOutput "Yellow" "Services available at (HTTP):"
            Write-ColorOutput "White" "  - API Gateway:     http://localhost:8080"
            Write-ColorOutput "White" "  - User Management: http://localhost:7070"
            Write-ColorOutput "White" "  - Wallet Payment:  http://localhost:7241"
            Write-ColorOutput "White" "  - Order Service:   http://localhost:60103"
            Write-ColorOutput "White" "  - Payment Gateway: http://localhost:7002"
            Write-ColorOutput "White" "  - SQL Server:      localhost:1433"
            Write-Host ""
            Write-ColorOutput "Cyan" "Use 'docker-run.ps1 logs' to view logs"
            Write-ColorOutput "Cyan" "Use 'docker-run.ps1 status' to check service status"
        }
        catch {
            Write-ColorOutput "Red" "Failed to start services: $($_.Exception.Message)"
            Write-ColorOutput "Yellow" "Try running: .\docker-run.ps1 fix"
            exit 1
        }
    }
    
    "prod" {
        Write-ColorOutput "Cyan" "Starting Production Environment (HTTPS)..."
        
        # Check for certificates
        if (!(Test-Path "certs/aspnetapp.pfx")) {
            Write-ColorOutput "Yellow" "Creating development certificates..."
            if (!(Test-Path "certs")) {
                New-Item -ItemType Directory -Path "certs" | Out-Null
            }
            try {
                dotnet dev-certs https -ep ./certs/aspnetapp.pfx -p SafarMall123! --trust
                Write-ColorOutput "Green" "Certificate created successfully"
            }
            catch {
                Write-ColorOutput "Red" "Failed to create certificate: $($_.Exception.Message)"
                exit 1
            }
        }
        
        # Stop existing containers
        Stop-AllServices
        
        # Build and start production environment
        try {
            docker-compose up --build -d
            Write-ColorOutput "Green" "Production environment started!"
            Write-ColorOutput "Yellow" "Services available at (HTTPS):"
            Write-ColorOutput "White" "  - API Gateway: https://localhost:8081"
        }
        catch {
            Write-ColorOutput "Red" "Failed to start production environment: $($_.Exception.Message)"
            Write-ColorOutput "Yellow" "Try running: .\docker-run.ps1 fix"
            exit 1
        }
    }
    
    "stop" {
        Stop-AllServices
    }
    
    "clean" {
        Write-ColorOutput "Red" "Cleaning up Docker resources..."
        Stop-AllServices
        Clean-DockerResources
    }
    
    "fix" {
        Write-ColorOutput "Cyan" "Docker Fix Mode - Rebuilding from scratch..."
        
        # Complete cleanup
        Stop-AllServices
        Clean-DockerResources
        
        Write-ColorOutput "Cyan" "Rebuilding containers from scratch..."
        try {
            docker-compose -f docker-compose.yml -f docker-compose.dev.yml build --no-cache
            Write-ColorOutput "Green" "Build completed successfully"
        } catch {
            Write-ColorOutput "Red" "Build failed: $($_.Exception.Message)"
            exit 1
        }
        
        Write-ColorOutput "Cyan" "Starting services..."
        try {
            # Start SQL Server first
            docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d sqlserver
            Wait-ForHealthy "sqlserver"
            
            # Start other services
            docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d
            Write-ColorOutput "Green" "Services started successfully"
        } catch {
            Write-ColorOutput "Red" "Failed to start services: $($_.Exception.Message)"
            exit 1
        }
        
        Show-ServiceStatus
        Write-ColorOutput "Green" "Docker fix completed!"
    }
    
    "status" {
        Show-ServiceStatus
    }
    
    "logs" {
        if ($Service) {
            Write-ColorOutput "Cyan" "Showing logs for service: $Service"
            try {
                # Try development first, then production
                $devContainers = docker ps --filter "name=safarmall" --filter "name=dev" --format "{{.Names}}"
                if ($devContainers) {
                    docker-compose -f docker-compose.yml -f docker-compose.dev.yml logs -f $Service
                } else {
                    docker-compose logs -f $Service
                }
            }
            catch {
                Write-ColorOutput "Red" "Error showing logs for service $Service"
                Write-ColorOutput "Yellow" "Available services:"
                try {
                    docker-compose -f docker-compose.yml -f docker-compose.dev.yml config --services
                } catch {
                    docker-compose config --services
                }
            }
        } else {
            Write-ColorOutput "Cyan" "Showing logs for all services..."
            Write-ColorOutput "Yellow" "Press Ctrl+C to stop following logs"
            Write-Host ""
            try {
                # Check if development environment is running
                $devContainers = docker ps --filter "name=safarmall" --filter "name=dev" --format "{{.Names}}"
                if ($devContainers) {
                    Write-ColorOutput "Green" "Development environment detected"
                    docker-compose -f docker-compose.yml -f docker-compose.dev.yml logs -f --tail=50
                } else {
                    Write-ColorOutput "Green" "Production environment detected (or no containers running)"
                    docker-compose logs -f --tail=50
                }
            }
            catch {
                Write-ColorOutput "Red" "Error showing logs"
                Write-ColorOutput "Yellow" "Make sure services are running. Use 'docker-run.ps1 status' to check."
            }
        }
    }
    
    default {
        Write-ColorOutput "Red" "Error: Invalid action '$Action'"
        Write-Host ""
        Show-Help
        exit 1
    }
}

Write-Host ""
Write-ColorOutput "Green" "Script completed!"

# Show final status for certain actions
if ($Action -in @("dev", "prod", "fix")) {
    Write-Host ""
    Write-ColorOutput "Cyan" "Final Status Check:"
    Show-ServiceStatus
    
    if ($Action -eq "dev") {
        Write-Host ""
        Write-ColorOutput "Yellow" "Quick Health Check:"
        Write-ColorOutput "White" "Testing API Gateway connection..."
        
        try {
            $response = Invoke-WebRequest -Uri "http://localhost:8080" -TimeoutSec 5 -UseBasicParsing 2>$null
            Write-ColorOutput "Green" "✓ API Gateway is responding"
        } catch {
            Write-ColorOutput "Red" "✗ API Gateway is not responding yet (might still be starting up)"
            Write-ColorOutput "Yellow" "  Wait a few moments and try: http://localhost:8080"
        }
        
        Write-Host ""
        Write-ColorOutput "Cyan" "Development URLs:"
        Write-ColorOutput "White" "  🌐 API Gateway:     http://localhost:8080"
        Write-ColorOutput "White" "  👥 User Management: http://localhost:7070"
        Write-ColorOutput "White" "  💰 Wallet Payment:  http://localhost:7241"
        Write-ColorOutput "White" "  📦 Order Service:   http://localhost:60103"
        Write-ColorOutput "White" "  💳 Payment Gateway: http://localhost:7002"
        Write-ColorOutput "White" "  🗄️  SQL Server:      localhost:1433"
        
        Write-Host ""
        Write-ColorOutput "Green" "🚀 Development environment is ready!"
        Write-ColorOutput "Yellow" "💡 Use 'docker-run.ps1 logs [service-name]' to view specific service logs"
        Write-ColorOutput "Yellow" "💡 Use 'docker-run.ps1 status' to check service health"
    }
    
    if ($Action -eq "prod") {
        Write-Host ""
        Write-ColorOutput "Cyan" "Production URLs:"
        Write-ColorOutput "White" "  🌐 API Gateway: https://localhost:8081"
        Write-ColorOutput "Yellow" "  ⚠️  Accept certificate warnings in browser for development"
    }
}