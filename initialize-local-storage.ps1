# Initialize Cosmos DB and Redis for Product Catalog local development
# This script sets up the local development environment for the Product Catalog application
# with support for Cosmos DB Emulator and Redis

param(
    [switch]$SkipCosmosDB,
    [switch]$SkipRedis,
    [switch]$Verbose
)

# Set verbose preference if requested
if ($Verbose) {
    $VerbosePreference = "Continue"
}

Write-Host "=== Product Catalog Local Storage Initialization ===" -ForegroundColor Magenta
Write-Host "This script will initialize Cosmos DB and Redis for local development" -ForegroundColor Cyan
Write-Host ""

# Function to write section headers
function Write-Section {
    param([string]$Title)
    Write-Host ""
    Write-Host "--- $Title ---" -ForegroundColor Yellow
}

# Function to write success messages
function Write-Success {
    param([string]$Message)
    Write-Host "✓ $Message" -ForegroundColor Green
}

# Function to write error messages
function Write-Error {
    param([string]$Message)
    Write-Host "✗ $Message" -ForegroundColor Red
}

# Function to write warning messages
function Write-Warning {
    param([string]$Message)
    Write-Host "⚠ $Message" -ForegroundColor Yellow
}

# Function to write info messages
function Write-Info {
    param([string]$Message)
    Write-Host "ℹ $Message" -ForegroundColor Cyan
}

# Function to check prerequisites
function Test-Prerequisites {
    Write-Section "Checking Prerequisites"
    $allGood = $true
    
    # Check PowerShell version
    Write-Verbose "Checking PowerShell version..."
    if ($PSVersionTable.PSVersion.Major -lt 5) {
        Write-Error "PowerShell 5.0 or higher is required"
        $allGood = $false
    } else {
        Write-Success "PowerShell version $($PSVersionTable.PSVersion) is supported"
    }
    
    # Check if running as administrator
    Write-Verbose "Checking administrator privileges..."
    $isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")
    if (-not $isAdmin) {
        Write-Warning "Not running as administrator. Some operations may fail."
        Write-Info "Consider running PowerShell as Administrator for best results"
    } else {
        Write-Success "Running with administrator privileges"
    }
    
    # Check network connectivity
    Write-Verbose "Testing network connectivity..."
    try {
        $null = Test-NetConnection -ComputerName "www.microsoft.com" -Port 80 -InformationLevel Quiet -WarningAction SilentlyContinue
        Write-Success "Network connectivity verified"
    } catch {
        Write-Warning "Network connectivity test failed. Some downloads may not work."
    }
    
    return $allGood
}

# Function to check if Cosmos DB Emulator is installed
function Test-CosmosDBEmulatorInstalled {
    Write-Section "Checking Cosmos DB Emulator Installation"
    
    # Check if emulator is installed in common locations
    $emulatorPaths = @(
        "${env:ProgramFiles}\Azure Cosmos DB Emulator\CosmosDB.Emulator.exe",
        "${env:ProgramFiles(x86)}\Microsoft Azure Cosmos Emulator\CosmosDB.Emulator.exe",
        "${env:LOCALAPPDATA}\Programs\Azure Cosmos DB Emulator\CosmosDB.Emulator.exe"
    )
    
    Write-Verbose "Searching for Cosmos DB Emulator in standard locations..."
    foreach ($path in $emulatorPaths) {
        Write-Verbose "Checking: $path"
        if (Test-Path $path) {
            Write-Success "Found Cosmos DB Emulator at: $path"
            
            # Try to get version information
            try {
                $fileInfo = Get-Item $path
                Write-Info "Version: $($fileInfo.VersionInfo.ProductVersion)"
                Write-Info "File size: $([math]::Round($fileInfo.Length / 1MB, 2)) MB"
            } catch {
                Write-Verbose "Could not retrieve version information"
            }
            
            return @{ Installed = $true; Path = $path }
        }
    }
    
    Write-Error "Cosmos DB Emulator not found in standard locations"
    Write-Info "Searched locations:"
    foreach ($path in $emulatorPaths) {
        Write-Info "  - $path"
    }
    Write-Warning "Please install Azure Cosmos DB Emulator from:"
    Write-Warning "https://docs.microsoft.com/en-us/azure/cosmos-db/local-emulator"
    Write-Info "After installation, restart this script"
    
    return @{ Installed = $false; Path = $null }
}

# Function to check if Cosmos DB Emulator is running
function Test-CosmosDBEmulatorRunning {
    Write-Verbose "Checking if Cosmos DB Emulator process is running..."
    
    $processes = Get-Process -Name "CosmosDB.Emulator" -ErrorAction SilentlyContinue
    if ($processes) {
        Write-Success "Cosmos DB Emulator process is running (PID: $($processes[0].Id))"
        
        # Check memory usage
        $memoryMB = [math]::Round($processes[0].WorkingSet64 / 1MB, 2)
        Write-Info "Memory usage: $memoryMB MB"
        
        return $true
    } else {
        Write-Warning "Cosmos DB Emulator process is not running"
        return $false
    }
}

# Function to start Cosmos DB Emulator
function Start-CosmosDBEmulator {
    param([string]$EmulatorPath)
    
    Write-Section "Starting Cosmos DB Emulator"
    
    # Stop any existing emulator process first to ensure clean start
    Write-Info "Checking for existing Cosmos DB Emulator processes..."
    $existingProcesses = Get-Process -Name "CosmosDB.Emulator" -ErrorAction SilentlyContinue
    if ($existingProcesses) {
        Write-Info "Stopping existing Cosmos DB Emulator processes..."
        foreach ($process in $existingProcesses) {
            try {
                Write-Verbose "Stopping process PID: $($process.Id)"
                $process.Kill()
                $process.WaitForExit(10000)  # Wait up to 10 seconds
                Write-Success "Stopped existing emulator process (PID: $($process.Id))"
            } catch {
                Write-Warning "Could not stop process PID $($process.Id): $($_.Exception.Message)"
            }
        }
        # Wait a moment for cleanup
        Start-Sleep -Seconds 3
    }
    
    if (Test-CosmosDBEmulatorRunning) {
        Write-Info "Cosmos DB Emulator is still running after cleanup attempt"
        # Try once more to kill any remaining processes
        Get-Process -Name "CosmosDB.Emulator" -ErrorAction SilentlyContinue | ForEach-Object { 
            try { $_.Kill() } catch { }
        }
        Start-Sleep -Seconds 2
    }
    
    Write-Info "Starting Cosmos DB Emulator from: $EmulatorPath"
    Write-Info "This may take several minutes on first startup..."
    
    try {
        # Check if port 8081 is already in use
        Write-Verbose "Checking if port 8081 is available..."
        $portCheck = Get-NetTCPConnection -LocalPort 8081 -ErrorAction SilentlyContinue
        if ($portCheck) {
            Write-Warning "Port 8081 is already in use by process ID: $($portCheck.OwningProcess)"
            Write-Info "Attempting to identify the process..."
            try {
                $blockingProcess = Get-Process -Id $portCheck.OwningProcess -ErrorAction SilentlyContinue
                if ($blockingProcess) {
                    Write-Warning "Port 8081 is used by: $($blockingProcess.ProcessName) (PID: $($blockingProcess.Id))"
                }
            } catch {
                Write-Verbose "Could not identify the process using port 8081"
            }
        }
        
        # Start the emulator with basic parameters
        $arguments = @(
            "/NoUI",           # Run without UI
            "/NoExplorer"      # Don't open Data Explorer
        )
        
        Write-Verbose "Starting with arguments: $($arguments -join ' ')"
        Write-Info "Full command: `"$EmulatorPath`" $($arguments -join ' ')"
        
        # Try to start the process with more detailed error handling
        $processStartInfo = New-Object System.Diagnostics.ProcessStartInfo
        $processStartInfo.FileName = $EmulatorPath
        $processStartInfo.Arguments = $arguments -join ' '
        $processStartInfo.UseShellExecute = $false
        $processStartInfo.RedirectStandardOutput = $true
        $processStartInfo.RedirectStandardError = $true
        $processStartInfo.CreateNoWindow = $true
        
        $process = New-Object System.Diagnostics.Process
        $process.StartInfo = $processStartInfo
        
        Write-Info "Attempting to start Cosmos DB Emulator..."
        $started = $process.Start()
        
        if (-not $started) {
            Write-Error "Failed to start the emulator process"
            return $false
        }
        
        Write-Info "Emulator process started (PID: $($process.Id))"
        
        # Wait a bit longer for the process to initialize
        Start-Sleep -Seconds 10
        
        # Check if process is still running
        if ($process.HasExited) {
            $exitCode = $process.ExitCode
            $stderr = $process.StandardError.ReadToEnd()
            $stdout = $process.StandardOutput.ReadToEnd()
            
            Write-Error "Emulator process exited unexpectedly (Exit Code: $exitCode)"
            
            # Decode common exit codes
            switch ($exitCode) {
                -2147014848 { Write-Error "Exit code -2147014848: This typically indicates a permissions or configuration issue" }
                -1073741819 { Write-Error "Exit code -1073741819: Access violation - may need administrator privileges" }
                -1073741515 { Write-Error "Exit code -1073741515: Missing dependency or DLL" }
                default { Write-Error "Unknown exit code: $exitCode" }
            }
            
            if ($stderr) {
                Write-Error "Standard Error: $stderr"
            }
            if ($stdout) {
                Write-Info "Standard Output: $stdout"
            }
            
            # Additional diagnostics
            Write-Warning "Diagnostic suggestions:"
            Write-Warning "  1. Try running PowerShell as Administrator"
            Write-Warning "  2. Check if Windows Defender or antivirus is blocking the emulator"
            Write-Warning "  3. Verify the emulator installation is not corrupted"
            Write-Warning "  4. Try restarting Windows to clear any lingering processes"
            Write-Warning "  5. Check Event Viewer for additional error details"
            
            return $false
        }
        
        Write-Success "Cosmos DB Emulator startup initiated successfully"
        return $true
        
    } catch {
        Write-Error "Failed to start Cosmos DB Emulator: $($_.Exception.Message)"
        Write-Error "Exception Type: $($_.Exception.GetType().Name)"
        if ($_.Exception.InnerException) {
            Write-Error "Inner Exception: $($_.Exception.InnerException.Message)"
        }
        return $false
    }
}

# Function to wait for Cosmos DB Emulator to be ready
function Wait-ForCosmosDB {
    Write-Section "Waiting for Cosmos DB Emulator to be Ready"
    
    $maxAttempts = 60  # Increased from 30 for slower machines
    $attempt = 0
    $ready = $false
    $endpoint = "https://localhost:8081"
    $explorerUrl = "$endpoint/_explorer/index.html"

    Write-Info "Cosmos DB Emulator endpoint: $endpoint"
    Write-Info "Data Explorer URL: $explorerUrl"
    Write-Info "Maximum wait time: $($maxAttempts * 2) seconds"
    
    while (-not $ready -and $attempt -lt $maxAttempts) {
        $attempt++
        Write-Progress -Activity "Waiting for Cosmos DB Emulator" -Status "Attempt $attempt of $maxAttempts" -PercentComplete (($attempt / $maxAttempts) * 100)
        
        try {
            Write-Verbose "Attempt $attempt/$maxAttempts - Testing connection to $endpoint"
            
            # First check if port 8081 is listening
            Write-Verbose "Checking if port 8081 is accessible..."
            $portCheck = Test-NetConnection -ComputerName "localhost" -Port 8081 -InformationLevel Quiet -WarningAction SilentlyContinue
            
            if (-not $portCheck) {
                Write-Verbose "Port 8081 is not accessible yet"
                if ($attempt -eq 1) {
                    Write-Info "Port 8081 is not accessible yet. Emulator is starting up..."
                }
            } else {
                Write-Verbose "Port 8081 is accessible, testing HTTP endpoint..."
                
                # Try to access the Data Explorer endpoint
                $response = Invoke-WebRequest -Uri $explorerUrl -UseBasicParsing -SkipCertificateCheck -TimeoutSec 10 -ErrorAction Stop
                
                if ($response.StatusCode -eq 200) {
                    $ready = $true
                    Write-Progress -Activity "Waiting for Cosmos DB Emulator" -Completed
                    Write-Success "Cosmos DB Emulator is ready!"
                    Write-Info "Data Explorer is accessible at: $explorerUrl"
                    
                    # Try to get emulator information
                    try {
                        $infoResponse = Invoke-WebRequest -Uri "$endpoint/_explorer/emulator.js" -UseBasicParsing -SkipCertificateCheck -TimeoutSec 5 -ErrorAction SilentlyContinue
                        if ($infoResponse.StatusCode -eq 200) {
                            Write-Verbose "Emulator info endpoint is also accessible"
                        }
                    } catch {
                        Write-Verbose "Could not access emulator info endpoint (this is normal)"
                    }
                } else {
                    Write-Verbose "Received HTTP status code: $($response.StatusCode)"
                }
            }
        }
        catch {
            Write-Verbose "Connection failed: $($_.Exception.Message)"
            
            # Provide more specific error messages
            if ($_.Exception.Message -like "*timeout*") {
                Write-Verbose "Connection timed out - emulator may still be starting"
            } elseif ($_.Exception.Message -like "*refused*") {
                Write-Verbose "Connection refused - emulator process may not be running"
            } elseif ($_.Exception.Message -like "*SSL*" -or $_.Exception.Message -like "*certificate*") {
                Write-Verbose "SSL/Certificate issue - this is normal for the emulator"
            }
        }

        if (-not $ready -and $attempt -lt $maxAttempts) {
            # Show progress every 10 attempts
            if ($attempt % 10 -eq 0) {
                Write-Info "Still waiting... (attempt $attempt/$maxAttempts)"
                
                # Check if the emulator process is still running
                if (-not (Test-CosmosDBEmulatorRunning)) {
                    Write-Warning "Cosmos DB Emulator process is not running!"
                    Write-Info "The emulator may have crashed or failed to start properly"
                }
            }
            
            Start-Sleep -Seconds 2
        }
    }

    Write-Progress -Activity "Waiting for Cosmos DB Emulator" -Completed

    if (-not $ready) {
        Write-Error "Cosmos DB Emulator did not become ready after $maxAttempts attempts ($($maxAttempts * 2) seconds)"
        Write-Warning "Troubleshooting steps:"
        Write-Warning "  1. Verify Azure Cosmos DB Emulator is installed correctly"
        Write-Warning "  2. Check if the emulator process is running in Task Manager"
        Write-Warning "  3. Try restarting the emulator manually"
        Write-Warning "  4. Check Windows Event Logs for emulator errors"
        Write-Warning "  5. Verify port 8081 is not blocked by firewall or used by another application"
        Write-Warning "  6. Try running PowerShell as Administrator"
        
        # Check for common issues
        Write-Info "Diagnostic information:"
        $netstat = netstat -an | Select-String ":8081"
        if ($netstat) {
            Write-Info "Port 8081 status: $netstat"
        } else {
            Write-Info "Port 8081 is not in use"
        }
        
        throw "Cosmos DB Emulator did not become ready in time"
    }
    
    return $true
}

# Function to check Redis prerequisites
function Test-RedisPrerequisites {
    Write-Section "Checking Redis Prerequisites"
    
    # Check if WSL is available
    Write-Verbose "Checking WSL availability..."
    try {
        $wslVersion = wsl --version 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Success "WSL is available"
            Write-Verbose "WSL version info: $wslVersion"
        } else {
            Write-Error "WSL is not available or not properly configured"
            return $false
        }
    } catch {
        Write-Error "WSL command failed: $($_.Exception.Message)"
        return $false
    }
    
    # Check if Ubuntu distribution is available
    Write-Verbose "Checking Ubuntu distribution..."
    try {
        $distributions = wsl --list --quiet 2>$null
        if ($distributions -contains "Ubuntu") {
            Write-Success "Ubuntu distribution is available in WSL"
        } else {
            Write-Error "Ubuntu distribution not found in WSL"
            Write-Info "Available distributions: $($distributions -join ', ')"
            Write-Warning "Please install Ubuntu from Microsoft Store or use 'wsl --install -d Ubuntu'"
            return $false
        }
    } catch {
        Write-Error "Failed to list WSL distributions: $($_.Exception.Message)"
        return $false
    }
    
    # Check if Redis is installed in Ubuntu
    Write-Verbose "Checking if Redis is installed in Ubuntu..."
    try {
        $redisVersion = wsl -d Ubuntu -- redis-server --version 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Redis is installed in Ubuntu"
            Write-Info "Redis version: $redisVersion"
        } else {
            Write-Warning "Redis is not installed in Ubuntu"
            Write-Info "You can install Redis with: wsl -d Ubuntu -- sudo apt update && sudo apt install redis-server -y"
            return $false
        }
    } catch {
        Write-Error "Failed to check Redis installation: $($_.Exception.Message)"
        return $false
    }
    
    return $true
}

# Function to start Redis if not running
function Start-Redis {
    Write-Section "Starting Redis Server"
    
    # Check if Redis is already running
    try {
        $pingResult = wsl -d Ubuntu -- redis-cli ping 2>$null
        if ($pingResult -eq "PONG") {
            Write-Success "Redis is already running"
            return $true
        }
    } catch {
        Write-Verbose "Redis ping failed, will attempt to start"
    }
    
    Write-Info "Starting Redis server in Ubuntu..."
    try {
        # Start Redis server in background
        wsl -d Ubuntu -- sudo service redis-server start 2>$null
        
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Redis server start command executed"
            Start-Sleep -Seconds 2  # Give Redis a moment to start
            
            # Verify it's running
            $pingResult = wsl -d Ubuntu -- redis-cli ping 2>$null
            if ($pingResult -eq "PONG") {
                Write-Success "Redis server is now running"
                return $true
            } else {
                Write-Warning "Redis server may not have started properly"
                return $false
            }
        } else {
            Write-Error "Failed to start Redis server (exit code: $LASTEXITCODE)"
            return $false
        }
    } catch {
        Write-Error "Failed to start Redis: $($_.Exception.Message)"
        return $false
    }
}

# Function to wait for Redis to be ready
function Wait-ForRedis {
    Write-Section "Waiting for Redis to be Ready"
    
    $maxAttempts = 20  # Increased for better reliability
    $attempt = 0
    $ready = $false

    Write-Info "Testing Redis connectivity..."
    Write-Info "Command: wsl -d Ubuntu -- redis-cli ping"
    
    while (-not $ready -and $attempt -lt $maxAttempts) {
        $attempt++
        Write-Progress -Activity "Waiting for Redis" -Status "Attempt $attempt of $maxAttempts" -PercentComplete (($attempt / $maxAttempts) * 100)
        
        try {
            Write-Verbose "Attempt $attempt/$maxAttempts - Testing Redis connection..."
            $pingResult = wsl -d Ubuntu -- redis-cli ping 2>$null
            
            if ($pingResult -eq "PONG") {
                $ready = $true
                Write-Progress -Activity "Waiting for Redis" -Completed
                Write-Success "Redis is ready and responding to ping!"
                
                # Get additional Redis info
                try {
                    $redisInfo = wsl -d Ubuntu -- redis-cli info server 2>$null | Select-String "redis_version"
                    if ($redisInfo) {
                        Write-Info "Redis info: $redisInfo"
                    }
                } catch {
                    Write-Verbose "Could not retrieve Redis version info"
                }
            } else {
                Write-Verbose "Redis ping returned: '$pingResult' (expected 'PONG')"
            }
        }
        catch {
            Write-Verbose "Redis ping failed: $($_.Exception.Message)"
            
            # Try to start Redis if it's not running
            if ($attempt -eq 1) {
                Write-Info "Redis doesn't seem to be running. Attempting to start it..."
                if (Start-Redis) {
                    Write-Verbose "Redis start attempt completed, continuing with ping tests"
                    continue
                }
            }
        }

        if (-not $ready -and $attempt -lt $maxAttempts) {
            if ($attempt % 5 -eq 0) {
                Write-Info "Still waiting for Redis... (attempt $attempt/$maxAttempts)"
            }
            Start-Sleep -Seconds 1
        }
    }

    Write-Progress -Activity "Waiting for Redis" -Completed

    if (-not $ready) {
        Write-Error "Redis did not become ready after $maxAttempts attempts"
        Write-Warning "Troubleshooting steps:"
        Write-Warning "  1. Verify WSL and Ubuntu are installed and running"
        Write-Warning "  2. Install Redis in Ubuntu: wsl -d Ubuntu -- sudo apt update && sudo apt install redis-server -y"
        Write-Warning "  3. Start Redis manually: wsl -d Ubuntu -- sudo service redis-server start"
        Write-Warning "  4. Check Redis status: wsl -d Ubuntu -- sudo service redis-server status"
        Write-Warning "  5. Check Redis logs: wsl -d Ubuntu -- sudo journalctl -u redis-server"
        
        throw "Redis did not become ready in time"
    }
    
    return $true
}

# Function to initialize Cosmos DB for Product Catalog
function Initialize-CosmosDB {
    Write-Section "Initializing Cosmos DB for Product Catalog"
    
    Write-Info "Setting up database and containers for Product Catalog application..."
    
    # Create a temporary Node.js script to initialize Cosmos DB
    $tempDir = "$env:TEMP\product-catalog-cosmos-init-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
    $scriptPath = Join-Path $tempDir "init-cosmos.js"
    
    Write-Verbose "Creating temporary directory: $tempDir"
    New-Item -ItemType Directory -Force -Path $tempDir | Out-Null
    
    # JavaScript initialization script for Product Catalog
    $script = @"
const { CosmosClient } = require('@azure/cosmos');

async function initializeProductCatalogDB() {
    console.log('Initializing Cosmos DB for Product Catalog...');
    
    const endpoint = 'https://localhost:8081';
    const key = 'C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==';
    
    // Disable SSL verification for local emulator
    process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';
    
    const client = new CosmosClient({ 
        endpoint, 
        key,
        connectionPolicy: {
            requestTimeout: 30000,
            enableEndpointDiscovery: false
        }
    });

    try {
        // Create database
        console.log('Creating database: ProductCatalogDB...');
        const { database } = await client.databases.createIfNotExists({ 
            id: 'ProductCatalogDB',
            throughput: 400  // Minimum throughput for emulator
        });
        console.log('SUCCESS: Database ProductCatalogDB is ready');

        // Create Products container
        console.log('Creating container: Products...');
        const { container } = await database.containers.createIfNotExists({
            id: 'Products',
            partitionKey: { 
                paths: ['/id'],
                kind: 'Hash'
            },
            indexingPolicy: {
                indexingMode: 'consistent',
                automatic: true,
                includedPaths: [
                    { path: '/*' }
                ],
                excludedPaths: [
                    { path: '/\"_etag\"/?' }
                ],
                compositeIndexes: [
                    [
                        { path: '/name', order: 'ascending' },
                        { path: '/category', order: 'ascending' }
                    ],
                    [
                        { path: '/price', order: 'ascending' },
                        { path: '/category', order: 'ascending' }
                    ]
                ]
            },
            uniqueKeyPolicy: {
                uniqueKeys: [
                    { paths: ['/name'] }  // Ensure product names are unique
                ]
            }
        });
        console.log('SUCCESS: Container Products is ready');

        // Add some sample data
        console.log('Adding sample products...');
        const sampleProducts = [
            {
                id: '1',
                name: 'Sample Laptop',
                description: 'A high-performance laptop for development',
                price: 1299.99,
                category: 'Electronics',
                stock: 50,
                createdAt: new Date().toISOString()
            },
            {
                id: '2', 
                name: 'Sample Phone',
                description: 'A smartphone with advanced features',
                price: 899.99,
                category: 'Electronics',
                stock: 100,
                createdAt: new Date().toISOString()
            },
            {
                id: '3',
                name: 'Sample Book',
                description: 'Programming guide for developers',
                price: 49.99,
                category: 'Books',
                stock: 25,
                createdAt: new Date().toISOString()
            }
        ];

        for (const product of sampleProducts) {
            try {
                await container.items.upsert(product);
                console.log('SUCCESS: Added sample product: ' + product.name);
            } catch (error) {
                if (error.code === 409) {
                    console.log('INFO: Sample product already exists: ' + product.name);
                } else {
                    console.error('ERROR: Failed to add sample product ' + product.name + ':', error.message);
                }
            }
        }

        console.log('');
        console.log('SUCCESS: Cosmos DB initialization completed successfully!');
        console.log('Database: ProductCatalogDB');
        console.log('Container: Products');
        console.log('Connection: https://localhost:8081');
        console.log('Key: C2y6...Jw== (default emulator key)');
        
    } catch (error) {
        console.error('ERROR: Error during Cosmos DB initialization:', error.message);
        if (error.code) {
            console.error('Error code:', error.code);
        }
        process.exit(1);
    }
}

initializeProductCatalogDB().catch(err => {
    console.error('Fatal error:', err);
    process.exit(1);
});
"@

    try {
        Write-Info "Creating Node.js project for Cosmos DB initialization..."
        Set-Location $tempDir

        # Initialize npm project
        Write-Verbose "Initializing npm project..."
        $npmInit = npm init -y 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Failed to initialize npm project: $npmInit"
            throw "npm init failed"
        }
        Write-Verbose "npm project initialized successfully"

        # Install Azure Cosmos DB SDK
        Write-Info "Installing @azure/cosmos package..."
        $npmInstall = npm install @azure/cosmos 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Failed to install @azure/cosmos: $npmInstall"
            throw "npm install failed"
        }
        Write-Success "@azure/cosmos package installed"

        # Save and run the initialization script
        Write-Verbose "Creating initialization script: $scriptPath"
        $script | Out-File -FilePath "init-cosmos.js" -Encoding UTF8NoBOM

        Write-Info "Executing Cosmos DB initialization script..."
        $nodeOutput = node init-cosmos.js 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Cosmos DB initialization completed successfully!"
            Write-Verbose "Node.js output: $nodeOutput"
        } else {
            Write-Error "Cosmos DB initialization failed!"
            Write-Error "Node.js output: $nodeOutput"
            throw "Cosmos DB initialization script failed"
        }

    } catch {
        Write-Error "Error during Cosmos DB initialization: $($_.Exception.Message)"
        throw
    } finally {
        # Clean up temporary directory
        try {
            Set-Location -Path $PSScriptRoot
            if (Test-Path $tempDir) {
                Write-Verbose "Cleaning up temporary directory: $tempDir"
                Remove-Item -Recurse -Force $tempDir -ErrorAction SilentlyContinue
            }
        } catch {
            Write-Warning "Could not clean up temporary directory: $tempDir"
        }
    }
}

# Function to initialize Redis for Product Catalog
function Initialize-Redis {
    Write-Section "Initializing Redis for Product Catalog"

    Write-Info "Configuring Redis for Product Catalog application..."

    try {
        # Clear any existing data
        Write-Info "Clearing existing Redis data..."
        $flushResult = wsl -d Ubuntu -- redis-cli FLUSHALL 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Redis data cleared successfully"
        } else {
            Write-Warning "Failed to clear Redis data: $flushResult"
        }

        # Configure Redis for key expiration notifications
        Write-Info "Configuring Redis for key expiration events..."
        $configResult = wsl -d Ubuntu -- redis-cli CONFIG SET notify-keyspace-events "Ex" 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Redis configured for key expiration notifications"
        } else {
            Write-Warning "Failed to configure Redis notifications: $configResult"
        }

        # Set up some sample cached data for testing
        Write-Info "Adding sample cached data for testing..."
        $sampleData = @(
            @{ key = "product:1:name"; value = "Sample Laptop"; ttl = 3600 },
            @{ key = "product:2:name"; value = "Sample Phone"; ttl = 3600 },
            @{ key = "catalog:stats:total_products"; value = "3"; ttl = 1800 },
            @{ key = "catalog:category:Electronics:count"; value = "2"; ttl = 900 }
        )

        foreach ($item in $sampleData) {
            try {
                $setResult = wsl -d Ubuntu -- redis-cli SETEX $item.key $item.ttl $item.value 2>&1
                if ($LASTEXITCODE -eq 0) {
                    Write-Verbose "✓ Set cache entry: $($item.key) = $($item.value) (TTL: $($item.ttl)s)"
                } else {
                    Write-Warning "Failed to set cache entry $($item.key): $setResult"
                }
            } catch {
                Write-Warning "Error setting cache entry $($item.key): $($_.Exception.Message)"
            }
        }

        # Verify Redis is working
        Write-Info "Verifying Redis functionality..."
        $testKey = "test:connection:$(Get-Date -Format 'yyyyMMddHHmmss')"
        $testValue = "ProductCatalog-Test"
        
        $setTest = wsl -d Ubuntu -- redis-cli SET $testKey $testValue EX 60 2>&1
        if ($LASTEXITCODE -eq 0) {
            $getTest = wsl -d Ubuntu -- redis-cli GET $testKey 2>&1
            if ($getTest -eq $testValue) {
                Write-Success "Redis read/write test passed"
                # Clean up test key
                wsl -d Ubuntu -- redis-cli DEL $testKey 2>$null
            } else {
                Write-Warning "Redis read test failed. Expected '$testValue', got '$getTest'"
            }
        } else {
            Write-Warning "Redis write test failed: $setTest"
        }

        # Display Redis info
        Write-Info "Getting Redis server information..."
        try {
            $redisInfo = wsl -d Ubuntu -- redis-cli INFO memory 2>$null | Select-String "used_memory_human"
            if ($redisInfo) {
                Write-Info "Redis memory usage: $($redisInfo.ToString().Split(':')[1].Trim())"
            }
            
            $keyCount = wsl -d Ubuntu -- redis-cli DBSIZE 2>$null
            if ($keyCount) {
                Write-Info "Redis database contains $keyCount keys"
            }
        } catch {
            Write-Verbose "Could not retrieve Redis statistics"
        }

        Write-Success "Redis initialization completed successfully!"
        
    } catch {
        Write-Error "Error during Redis initialization: $($_.Exception.Message)"
        throw
    }
}

# Main execution flow
function Main {
    try {
        Write-Host "Product Catalog Local Storage Initialization Started" -ForegroundColor Magenta
        Write-Host "Start time: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray
        Write-Host ""
        
        # Check prerequisites
        if (-not (Test-Prerequisites)) {
            Write-Error "Prerequisites check failed. Please resolve the issues above and try again."
            exit 1
        }
        
        # Initialize Cosmos DB if not skipped
        if (-not $SkipCosmosDB) {
            Write-Section "Cosmos DB Setup"
            
            $cosmosCheck = Test-CosmosDBEmulatorInstalled
            if (-not $cosmosCheck.Installed) {
                Write-Error "Cosmos DB Emulator is not installed. Please install it and try again."
                Write-Info "Download from: https://aka.ms/cosmosdb-emulator"
                exit 1
            }
            
            # Try to start the emulator if it's not running
            if (-not (Test-CosmosDBEmulatorRunning)) {
                Write-Info "Cosmos DB Emulator is not running. Attempting to start it..."
                
                # First attempt with the automated method
                $startupSuccess = Start-CosmosDBEmulator -EmulatorPath $cosmosCheck.Path
                
                if (-not $startupSuccess) {
                    Write-Warning "Automated startup failed. Trying alternative method..."
                    
                    # Alternative: Try to start manually with simpler command
                    Write-Info "Attempting to start with basic parameters only..."
                    try {
                        $altProcess = Start-Process -FilePath $cosmosCheck.Path -ArgumentList "/NoUI" -PassThru -WindowStyle Hidden
                        Start-Sleep -Seconds 15
                        
                        if (-not $altProcess.HasExited) {
                            Write-Info "Alternative startup method initiated. Process PID: $($altProcess.Id)"
                            $startupSuccess = $true
                        } else {
                            Write-Error "Alternative startup also failed (Exit Code: $($altProcess.ExitCode))"
                        }
                    } catch {
                        Write-Error "Alternative startup method failed: $($_.Exception.Message)"
                    }
                }
                
                if (-not $startupSuccess) {
                    Write-Error "All automatic startup methods failed."
                    Write-Warning "Manual intervention required:"
                    Write-Warning "  1. Open Command Prompt as Administrator"
                    Write-Warning "  2. Navigate to the emulator directory"
                    Write-Warning "  3. Run: CosmosDB.Emulator.exe /NoUI"
                    Write-Warning "  4. Wait for the emulator to start, then re-run this script"
                    Write-Info "Emulator path: $($cosmosCheck.Path)"
                    exit 1
                }
            }
            
            # Wait for Cosmos DB to be ready
            if (Wait-ForCosmosDB) {
                Initialize-CosmosDB
                Write-Success "Cosmos DB setup completed successfully!"
            }
        } else {
            Write-Warning "Cosmos DB initialization skipped (-SkipCosmosDB flag used)"
        }
        
        # Initialize Redis if not skipped
        if (-not $SkipRedis) {
            Write-Section "Redis Setup"
            
            if (Test-RedisPrerequisites) {
                if (Wait-ForRedis) {
                    Initialize-Redis
                    Write-Success "Redis setup completed successfully!"
                }
            } else {
                Write-Error "Redis prerequisites check failed. Please resolve the issues above."
                exit 1
            }
        } else {
            Write-Warning "Redis initialization skipped (-SkipRedis flag used)"
        }
        
        # Final summary
        Write-Section "Initialization Summary"
        Write-Success "Local storage initialization completed successfully!"
        
        if (-not $SkipCosmosDB) {
            Write-Info "✅ Cosmos DB:"
            Write-Info "   - Database: ProductCatalogDB"
            Write-Info "   - Container: Products" 
            Write-Info "   - Endpoint: https://localhost:8081"
            Write-Info "   - Data Explorer: https://localhost:8081/_explorer/index.html"
        }
        
        if (-not $SkipRedis) {
            Write-Info "✅ Redis:"
            Write-Info "   - Server: localhost:6379 (via WSL Ubuntu)"
            Write-Info "   - Database: 0 (default)"
            Write-Info "   - Key expiration events: Enabled"
        }
        
        Write-Info ""
        Write-Info "🚀 Your Product Catalog application is now ready for local development!"
        Write-Info "   You can run the application and choose between:"
        Write-Info "   - In-Memory storage (no setup required)"
        Write-Info "   - Cosmos DB storage (configured above)"
        Write-Info "   - Redis storage (configured above)"
        Write-Info ""
        Write-Info "💡 To run the application:"
        Write-Info "   cd ProductCatalog"
        Write-Info "   dotnet run"
        Write-Info ""
        Write-Host "End time: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray
        
    } catch {
        Write-Section "Initialization Failed"
        Write-Error "Error during initialization: $($_.Exception.Message)"
        Write-Error "Stack trace: $($_.ScriptStackTrace)"
        
        Write-Warning "Troubleshooting tips:"
        Write-Warning "  1. Run PowerShell as Administrator"
        Write-Warning "  2. Check that all prerequisites are met"
        Write-Warning "  3. Verify network connectivity"
        Write-Warning "  4. Try running with -Verbose flag for more details"
        Write-Warning "  5. Check Windows Event Logs for system errors"
        
        exit 1
    }
}

# Script usage information
function Show-Usage {
    Write-Host "Product Catalog Local Storage Initialization Script" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "USAGE:" -ForegroundColor Yellow
    Write-Host "  .\initialize-local-storage.ps1 [OPTIONS]"
    Write-Host ""
    Write-Host "OPTIONS:" -ForegroundColor Yellow
    Write-Host "  -SkipCosmosDB    Skip Cosmos DB setup"
    Write-Host "  -SkipRedis       Skip Redis setup"
    Write-Host "  -Verbose         Enable verbose output"
    Write-Host "  -Help            Show this help message"
    Write-Host ""
    Write-Host "EXAMPLES:" -ForegroundColor Yellow
    Write-Host "  .\initialize-local-storage.ps1"
    Write-Host "  .\initialize-local-storage.ps1 -Verbose"
    Write-Host "  .\initialize-local-storage.ps1 -SkipCosmosDB"
    Write-Host "  .\initialize-local-storage.ps1 -SkipRedis -Verbose"
    Write-Host ""
}

# Check for help parameter
if ($args -contains "-Help" -or $args -contains "--help" -or $args -contains "/?" -or $args -contains "-h") {
    Show-Usage
    exit 0
}

# Run the main function
Main