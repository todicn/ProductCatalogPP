# Product Catalog - Storage Configuration Guide

This document explains how to use the Product Catalog application with different storage backends using the Factory Pattern.

## Available Storage Types

The application supports three storage backends:

1. **In-Memory Storage** - Default, no external dependencies
2. **Cosmos DB Storage** - Uses Azure Cosmos DB (local emulator supported)
3. **Redis Storage** - Uses Redis for caching/storage

## Factory Pattern Implementation

The application uses the Factory Pattern to create appropriate service instances based on configuration:

```csharp
// Create configuration
var config = StorageConfigurationHelper.CreateInMemoryConfiguration();

// Create factory
var factory = new ProductCatalogServiceFactory(config);

// Create service instance
var catalog = factory.CreateService();
```

## Storage Configuration Options

### 1. In-Memory Storage (Default)

**Use Case**: Development, testing, or when no external storage is needed.

```csharp
var config = StorageConfigurationHelper.CreateInMemoryConfiguration();
```

**Pros**:
- No external dependencies
- Fast performance
- No setup required

**Cons**:
- Data is lost when application stops
- Limited to single instance

### 2. Cosmos DB Storage

**Use Case**: Production scenarios requiring scalable NoSQL database.

#### Local Development (Cosmos DB Emulator)

```csharp
var config = StorageConfigurationHelper.CreateCosmosDbConfiguration();
```

#### Production with Custom Connection String

```csharp
var config = StorageConfigurationHelper.CreateCosmosDbConfiguration(
    connectionString: "AccountEndpoint=https://your-account.documents.azure.com:443/;AccountKey=your-key",
    databaseName: "ProductCatalogDB",
    containerName: "Products"
);
```

**Pros**:
- Highly scalable
- Global distribution
- Rich querying capabilities
- Automatic indexing

**Cons**:
- Requires Azure Cosmos DB account (for production)
- Higher latency than in-memory
- Cost associated with cloud usage

#### Prerequisites for Cosmos DB

1. **Local Development**: Install Azure Cosmos DB Emulator
2. **Production**: Azure Cosmos DB account with database and container

### 3. Redis Storage

**Use Case**: High-performance caching, session storage, real-time applications.

#### Local Development

```csharp
var config = StorageConfigurationHelper.CreateRedisConfiguration();
```

#### Production with Custom Settings

```csharp
var config = StorageConfigurationHelper.CreateRedisConfiguration(
    connectionString: "your-redis-connection-string",
    keyPrefix: "productcatalog:",
    expirationMinutes: 120
);
```

**Pros**:
- Very fast performance
- Built-in expiration
- Memory efficient
- Support for complex data structures

**Cons**:
- Data may expire (by design)
- Memory-based (limited by RAM)
- Requires Redis server

#### Prerequisites for Redis

1. **Local Development**: Redis server running on localhost:6379
2. **Production**: Redis instance (Azure Cache for Redis, AWS ElastiCache, etc.)

## Environment Variable Configuration

You can configure storage using environment variables:

```bash
# Storage type selection
PRODUCT_CATALOG_STORAGE_TYPE=InMemory|CosmosDb|Redis

# Cosmos DB settings
COSMOS_DB_CONNECTION_STRING=your-connection-string
COSMOS_DB_DATABASE_NAME=TinyUrlDB
COSMOS_DB_CONTAINER_NAME=UrlMappings

# Redis settings
REDIS_CONNECTION_STRING=localhost:6379
REDIS_KEY_PREFIX=productcatalog:
REDIS_EXPIRATION_MINUTES=60
```

Then use:

```csharp
var config = StorageConfigurationHelper.CreateFromEnvironment();
```

## Setting Up Local Development Environment

### For Cosmos DB Development

1. **Install Cosmos DB Emulator**:
   - Download from Microsoft website
   - Or use the PowerShell script: `.\initialize-local-storage.ps1`

2. **Initialize Database**:
   Run the provided PowerShell script to set up the database and container:
   ```powershell
   .\initialize-local-storage.ps1
   ```

### For Redis Development

1. **Install Redis**:
   - **Windows**: Use WSL or Redis for Windows
   - **macOS**: `brew install redis`
   - **Linux**: `sudo apt-get install redis-server`

2. **Start Redis**:
   ```bash
   redis-server
   ```

3. **Verify Connection**:
   ```bash
   redis-cli ping
   # Should return: PONG
   ```

## Running the Application

1. **Build the application**:
   ```bash
   dotnet build
   ```

2. **Run the application**:
   ```bash
   dotnet run
   ```

3. **Choose storage type** when prompted:
   - Press 1 for In-Memory
   - Press 2 for Cosmos DB (requires emulator/database)
   - Press 3 for Redis (requires Redis server)

## Error Handling and Fallback

The application includes robust error handling:

- If the selected storage type fails to initialize, it automatically falls back to in-memory storage
- Connection failures are logged with appropriate error messages
- All operations include proper exception handling

## Best Practices

1. **Development**: Use in-memory storage for quick testing
2. **Integration Testing**: Use local Cosmos DB emulator or Redis
3. **Production**: Use managed services (Azure Cosmos DB, Azure Cache for Redis)
4. **Configuration**: Use environment variables for production deployments
5. **Error Handling**: Always implement fallback strategies
6. **Security**: Never hardcode connection strings in production code

## Performance Considerations

| Storage Type | Read Performance | Write Performance | Scalability | Durability |
|-------------|------------------|-------------------|-------------|------------|
| In-Memory   | Excellent        | Excellent         | Limited     | None       |
| Cosmos DB   | Good             | Good              | Excellent   | Excellent  |
| Redis       | Excellent        | Excellent         | Good        | Good       |

## Troubleshooting

### Cosmos DB Issues
- Ensure emulator is running on https://localhost:8081
- Check if database and container exist
- Verify connection string format

### Redis Issues
- Ensure Redis server is running on localhost:6379
- Check Redis logs for connection issues
- Verify network connectivity

### General Issues
- Check NuGet package references
- Ensure .NET 8.0 is installed
- Review console output for specific error messages
