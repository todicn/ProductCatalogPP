# Initialize Cosmos DB and Redis for local development

# Function to wait for Cosmos DB Emulator
function Wait-ForCosmosDB {
    $maxAttempts = 30
    $attempt = 0
    $ready = $false

    Write-Host "Waiting for Cosmos DB Emulator to be ready..."
    while (-not $ready -and $attempt -lt $maxAttempts) {
        try {
            $response = Invoke-WebRequest -Uri "https://localhost:8081/_explorer/index.html" -UseBasicParsing -SkipCertificateCheck
            if ($response.StatusCode -eq 200) {
                $ready = $true
                Write-Host "Cosmos DB Emulator is ready!"
            }
        }
        catch {
            $attempt++
            Write-Host "Attempt $attempt of $maxAttempts - Waiting for Cosmos DB Emulator..."
            Start-Sleep -Seconds 2
        }
    }

    if (-not $ready) {
        throw "Cosmos DB Emulator did not become ready in time"
    }
}

# Function to wait for Redis
function Wait-ForRedis {
    $maxAttempts = 10
    $attempt = 0
    $ready = $false

    Write-Host "Waiting for Redis to be ready..."
    while (-not $ready -and $attempt -lt $maxAttempts) {
        try {
            $pingResult = wsl -d Ubuntu -- redis-cli ping
            if ($pingResult -eq "PONG") {
                $ready = $true
                Write-Host "Redis is ready!"
            }
        }
        catch {
            $attempt++
            Write-Host "Attempt $attempt of $maxAttempts - Waiting for Redis..."
            Start-Sleep -Seconds 1
        }
    }

    if (-not $ready) {
        throw "Redis did not become ready in time"
    }
}

# Initialize Cosmos DB
function Initialize-CosmosDB {
    Write-Host "Initializing Cosmos DB..."
    
    # Create a temporary Node.js script to initialize Cosmos DB
    $scriptPath = "$env:TEMP\init-cosmos.js"
    $script = @"
const { CosmosClient } = require('@azure/cosmos');

async function initializeCosmosDB() {
    const endpoint = 'https://localhost:8081';
    const key = 'C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==';
    const client = new CosmosClient({ endpoint, key });

    // Create database
    console.log('Creating database...');
    const { database } = await client.databases.createIfNotExists({ id: 'TinyUrlDB' });
    console.log('Database ready');

    // Create container
    console.log('Creating container...');
    const { container } = await database.containers.createIfNotExists({
        id: 'UrlMappings',
        partitionKey: { paths: ['/id'] },
        indexingPolicy: {
            indexingMode: 'consistent',
            automatic: true,
            includedPaths: [{ path: '/*' }],
            compositeIndexes: [[
                { path: '/shortUrl', order: 'ascending' }
            ]]
        }
    });
    console.log('Container ready');
}

initializeCosmosDB().catch(err => {
    console.error('Error:', err);
    process.exit(1);
});
"@

    # Create temporary directory for npm project
    $tempDir = "$env:TEMP\cosmos-init"
    New-Item -ItemType Directory -Force -Path $tempDir | Out-Null
    Set-Location $tempDir

    # Initialize npm project and install dependencies
    npm init -y | Out-Null
    npm install @azure/cosmos | Out-Null

    # Save and run the script
    $script | Out-File -FilePath "init-cosmos.js" -Encoding UTF8
    node init-cosmos.js

    # Clean up
    Set-Location -Path $PSScriptRoot
    Remove-Item -Recurse -Force $tempDir
}

# Initialize Redis
function Initialize-Redis {
    Write-Host "Initializing Redis..."

    # Clear any existing data
    wsl -d Ubuntu -- redis-cli FLUSHALL

    # Set key expiration notification
    wsl -d Ubuntu -- redis-cli CONFIG SET notify-keyspace-events "Ex"

    Write-Host "Redis initialized!"
}

try {
    # Wait for services to be ready
    Wait-ForCosmosDB
    Wait-ForRedis

    # Initialize services
    Initialize-CosmosDB
    Initialize-Redis

    Write-Host "`nInitialization completed successfully!"
    Write-Host "- Cosmos DB database 'TinyUrlDB' and container 'UrlMappings' are ready"
    Write-Host "- Redis is cleared and configured for key expiration events"
}
catch {
    Write-Host "Error during initialization: $_" -ForegroundColor Red
    exit 1
} 