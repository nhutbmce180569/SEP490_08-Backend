# StayHub Windows Startup PowerShell Script
# Start Redis and all 10 microservices in separate PowerShell windows

Write-Host "==========================================================" -ForegroundColor Green
Write-Host "Starting StayHub Backend Microservices..." -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green

# 1. Start Redis
Write-Host "1. Starting local Redis server..." -ForegroundColor Cyan
if (Test-Path "StayHub\run-redis.bat") {
    Start-Process cmd -ArgumentList "/c", "run-redis.bat" -WorkingDirectory "StayHub" -WindowStyle Minimized
    Start-Sleep -Seconds 2
} else {
    Write-Host "WARNING: StayHub\run-redis.bat not found. Please start Redis manually." -ForegroundColor Yellow
}

# List of services to launch
$services = @(
  "AuthAPI",
  "BookingAPI",
  "PaymentAPI",
  "SocialAPI",
  "TourAPI",
  "VoucherAPI",
  "AIAPI",
  "ContentAPI",
  "SystemAPI",
  "GatewayAPI"
)

# 2. Launch Services
Write-Host "2. Launching 10 microservices..." -ForegroundColor Cyan
foreach ($service in $services) {
    Write-Host "Launching $service..." -ForegroundColor Green
    $csprojPath = "StayHub\$service\$service.csproj"
    if (Test-Path $csprojPath) {
        # Launch service in a separate PowerShell window
        Start-Process powershell -ArgumentList "-NoExit", "-Command", "`$Host.UI.RawUI.WindowTitle = '$service'; dotnet run --project $csprojPath --launch-profile https"
        Start-Sleep -Seconds 2
    } else {
        Write-Host "ERROR: Project file not found: $csprojPath" -ForegroundColor Red
    }
}

Write-Host "==========================================================" -ForegroundColor Green
Write-Host "All services launched! Please check the terminal windows." -ForegroundColor Green
Write-Host "GatewayAPI is running on: http://localhost:5046" -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
