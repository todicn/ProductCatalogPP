# Simple script to start Cosmos DB Emulator with admin privileges
Write-Host "Starting Cosmos DB Emulator as Administrator..." -ForegroundColor Cyan

$emulatorPath = "C:\Program Files\Azure Cosmos DB Emulator\CosmosDB.Emulator.exe"

if (-not (Test-Path $emulatorPath)) {
    Write-Host "Cosmos DB Emulator not found at: $emulatorPath" -ForegroundColor Red
    exit 1
}

try {
    Write-Host "Attempting to start Cosmos DB Emulator..." -ForegroundColor Yellow
    $process = Start-Process -FilePath $emulatorPath -ArgumentList "/NoUI" -PassThru -WindowStyle Normal
    Write-Host "Emulator started with PID: $($process.Id)" -ForegroundColor Green
    
    Write-Host "Waiting for emulator to initialize..." -ForegroundColor Yellow
    Start-Sleep -Seconds 20
    
    # Test if it's running
    $runningProcess = Get-Process -Name "CosmosDB.Emulator" -ErrorAction SilentlyContinue
    if ($runningProcess) {
        Write-Host "✓ Cosmos DB Emulator is running (PID: $($runningProcess.Id))" -ForegroundColor Green
    } else {
        Write-Host "✗ Cosmos DB Emulator is not running" -ForegroundColor Red
    }
    
    # Test port
    Write-Host "Testing port 8081..." -ForegroundColor Yellow
    $portTest = Test-NetConnection -ComputerName localhost -Port 8081 -InformationLevel Quiet -WarningAction SilentlyContinue
    if ($portTest) {
        Write-Host "✓ Port 8081 is accessible" -ForegroundColor Green
    } else {
        Write-Host "✗ Port 8081 is not accessible" -ForegroundColor Red
    }
    
} catch {
    Write-Host "Error starting Cosmos DB Emulator: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "Press any key to continue..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
