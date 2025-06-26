# Test Cosmos DB initialization only
param(
    [switch]$Verbose
)

if ($Verbose) {
    $VerbosePreference = "Continue"
}

Write-Host "=== Testing Cosmos DB Initialization Only ===" -ForegroundColor Magenta

function Write-Info {
    param([string]$Message)
    Write-Host "ℹ $Message" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "✓ $Message" -ForegroundColor Green
}

function Write-Error {
    param([string]$Message)
    Write-Host "✗ $Message" -ForegroundColor Red
}

# Check if Cosmos DB Emulator is accessible
Write-Info "Testing Cosmos DB Emulator connectivity..."

try {
    $response = Invoke-WebRequest -Uri "https://localhost:8081/_explorer/index.html" -UseBasicParsing -SkipCertificateCheck -TimeoutSec 10
    if ($response.StatusCode -eq 200) {
        Write-Success "Cosmos DB Emulator is accessible"
    } else {
        Write-Error "Cosmos DB Emulator returned status code: $($response.StatusCode)"
        exit 1
    }
} catch {
    Write-Error "Cannot connect to Cosmos DB Emulator: $($_.Exception.Message)"
    Write-Info "Please ensure:"
    Write-Info "1. Azure Cosmos DB Emulator is installed"
    Write-Info "2. The emulator is running"
    Write-Info "3. Port 8081 is accessible"
    exit 1
}

# Test Node.js availability
Write-Info "Testing Node.js availability..."
try {
    $nodeVersion = node --version 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Success "Node.js is available: $nodeVersion"
    } else {
        Write-Error "Node.js is not available"
        exit 1
    }
} catch {
    Write-Error "Node.js test failed: $($_.Exception.Message)"
    exit 1
}

# Test npm availability
Write-Info "Testing npm availability..."
try {
    $npmVersion = npm --version 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Success "npm is available: $npmVersion"
    } else {
        Write-Error "npm is not available"
        exit 1
    }
} catch {
    Write-Error "npm test failed: $($_.Exception.Message)"
    exit 1
}

# Create a simple test script
Write-Info "Creating test Cosmos DB initialization script..."

$tempDir = "$env:TEMP\cosmos-test-$(Get-Date -Format 'yyyyMMddHHmmss')"
Write-Verbose "Creating temporary directory: $tempDir"
New-Item -ItemType Directory -Force -Path $tempDir | Out-Null

$testScript = @'
const { CosmosClient } = require('@azure/cosmos');

async function testCosmosDB() {
    console.log('Testing Cosmos DB connection...');
    
    const endpoint = 'https://localhost:8081';
    const key = 'C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==';
    
    const client = new CosmosClient({ 
        endpoint, 
        key,
        connectionPolicy: {
            requestTimeout: 30000,
            enableEndpointDiscovery: false
        },
        agent: {
            rejectUnauthorized: false
        }
    });

    try {
        console.log('Creating database: TestDB...');
        const { database } = await client.databases.createIfNotExists({ 
            id: 'TestDB'
        });
        console.log('SUCCESS: Database TestDB created/verified');

        console.log('Creating container: TestContainer...');
        const { container } = await database.containers.createIfNotExists({
            id: 'TestContainer',
            partitionKey: { 
                paths: ['/id']
            }
        });
        console.log('SUCCESS: Container TestContainer created/verified');

        console.log('Adding test document...');
        const testDoc = {
            id: 'test-1',
            name: 'Test Document',
            timestamp: new Date().toISOString()
        };
        
        await container.items.upsert(testDoc);
        console.log('SUCCESS: Test document added');

        console.log('Reading test document...');
        const { resource } = await container.item('test-1', 'test-1').read();
        console.log('SUCCESS: Test document read:', resource.name);

        console.log('');
        console.log('SUCCESS: All Cosmos DB tests passed!');
        
    } catch (error) {
        console.error('ERROR: Cosmos DB test failed:', error.message);
        if (error.code) {
            console.error('Error code:', error.code);
        }
        process.exit(1);
    }
}

testCosmosDB().catch(err => {
    console.error('Fatal error:', err);
    process.exit(1);
});
'@

try {
    Write-Info "Setting up test environment..."
    Set-Location $tempDir

    # Initialize npm project
    Write-Verbose "Initializing npm project..."
    $npmInit = npm init -y 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to initialize npm project: $npmInit"
        throw "npm init failed"
    }

    # Install Azure Cosmos DB SDK
    Write-Info "Installing @azure/cosmos package..."
    $npmInstall = npm install @azure/cosmos 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to install @azure/cosmos: $npmInstall"
        throw "npm install failed"
    }
    Write-Success "@azure/cosmos package installed"

    # Save the test script with proper encoding
    Write-Verbose "Creating test script..."
    $testScript | Out-File -FilePath "test-cosmos.js" -Encoding ascii

    # Run the test
    Write-Info "Executing Cosmos DB test..."
    $nodeOutput = node test-cosmos.js 2>&1
    Write-Host "Node.js output:" -ForegroundColor Yellow
    Write-Host $nodeOutput

    if ($LASTEXITCODE -eq 0) {
        Write-Success "Cosmos DB test completed successfully!"
    } else {
        Write-Error "Cosmos DB test failed!"
        Write-Error "Exit code: $LASTEXITCODE"
    }

} catch {
    Write-Error "Test failed: $($_.Exception.Message)"
} finally {
    # Clean up
    try {
        Set-Location $PSScriptRoot
        if (Test-Path $tempDir) {
            Write-Verbose "Cleaning up temporary directory: $tempDir"
            Remove-Item -Recurse -Force $tempDir -ErrorAction SilentlyContinue
        }
    } catch {
        Write-Warning "Could not clean up temporary directory: $tempDir"
    }
}

Write-Host "Test completed!" -ForegroundColor Magenta
