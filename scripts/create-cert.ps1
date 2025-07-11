# PowerShell script for creating development certificates

Write-Host "🔐 Creating Development Certificates for SafarMall" -ForegroundColor Green
Write-Host "=================================================" -ForegroundColor Yellow

# Create certs directory if it doesn't exist
if (!(Test-Path "certs")) {
    Write-Host "📁 Creating certs directory..." -ForegroundColor Cyan
    New-Item -ItemType Directory -Path "certs" | Out-Null
}

try {
    # Remove existing certificates
    Write-Host "🗑️ Cleaning existing certificates..." -ForegroundColor Yellow
    & dotnet dev-certs https --clean

    # Create new development certificate
    Write-Host "🔑 Creating new development certificate..." -ForegroundColor Cyan
    & dotnet dev-certs https -ep ./certs/aspnetapp.pfx -p SafarMall123! --trust

    # Verify certificate
    Write-Host "✅ Verifying certificate..." -ForegroundColor Cyan
    & dotnet dev-certs https --check --trust

    Write-Host "🎉 Development certificate created and trusted successfully!" -ForegroundColor Green
    Write-Host "📍 Certificate location: ./certs/aspnetapp.pfx" -ForegroundColor Yellow
    Write-Host "🔒 Certificate password: SafarMall123!" -ForegroundColor Yellow
    
} catch {
    Write-Host "❌ Error creating certificate: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host "✨ Certificate setup completed!" -ForegroundColor Green