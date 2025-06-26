namespace ProductCatalog.Services;

/// <summary>
/// Helper class for configuring and creating storage configurations
/// </summary>
public static class StorageConfigurationHelper
{
    /// <summary>
    /// Creates a configuration for in-memory storage
    /// </summary>
    /// <returns>Storage configuration for in-memory storage</returns>
    public static StorageConfiguration CreateInMemoryConfiguration()
    {
        return new StorageConfiguration
        {
            StorageType = StorageType.InMemory
        };
    }

    /// <summary>
    /// Creates a configuration for Cosmos DB storage using local emulator
    /// </summary>
    /// <returns>Storage configuration for Cosmos DB local emulator</returns>
    public static StorageConfiguration CreateCosmosDbConfiguration()
    {
        return new StorageConfiguration
        {
            StorageType = StorageType.CosmosDb,
            CosmosDbConnectionString = null, // Will use emulator default
            CosmosDbDatabaseName = "TinyUrlDB",
            CosmosDbContainerName = "UrlMappings"
        };
    }

    /// <summary>
    /// Creates a configuration for Cosmos DB storage with custom connection string
    /// </summary>
    /// <param name="connectionString">Custom Cosmos DB connection string</param>
    /// <param name="databaseName">Database name (optional, defaults to TinyUrlDB)</param>
    /// <param name="containerName">Container name (optional, defaults to UrlMappings)</param>
    /// <returns>Storage configuration for Cosmos DB</returns>
    public static StorageConfiguration CreateCosmosDbConfiguration(
        string connectionString, 
        string? databaseName = null, 
        string? containerName = null)
    {
        return new StorageConfiguration
        {
            StorageType = StorageType.CosmosDb,
            CosmosDbConnectionString = connectionString,
            CosmosDbDatabaseName = databaseName ?? "TinyUrlDB",
            CosmosDbContainerName = containerName ?? "UrlMappings"
        };
    }

    /// <summary>
    /// Creates a configuration for Redis storage using local instance
    /// </summary>
    /// <returns>Storage configuration for Redis local instance</returns>
    public static StorageConfiguration CreateRedisConfiguration()
    {
        return new StorageConfiguration
        {
            StorageType = StorageType.Redis,
            RedisConnectionString = "localhost:6379",
            RedisKeyPrefix = "productcatalog:",
            RedisCacheExpirationMinutes = 60
        };
    }

    /// <summary>
    /// Creates a configuration for Redis storage with custom settings
    /// </summary>
    /// <param name="connectionString">Redis connection string</param>
    /// <param name="keyPrefix">Key prefix for Redis keys (optional, defaults to productcatalog:)</param>
    /// <param name="expirationMinutes">Cache expiration in minutes (optional, defaults to 60)</param>
    /// <returns>Storage configuration for Redis</returns>
    public static StorageConfiguration CreateRedisConfiguration(
        string connectionString, 
        string? keyPrefix = null, 
        int? expirationMinutes = null)
    {
        return new StorageConfiguration
        {
            StorageType = StorageType.Redis,
            RedisConnectionString = connectionString,
            RedisKeyPrefix = keyPrefix ?? "productcatalog:",
            RedisCacheExpirationMinutes = expirationMinutes ?? 60
        };
    }

    /// <summary>
    /// Gets the storage type from environment variable or returns default
    /// </summary>
    /// <param name="defaultType">Default storage type if environment variable is not set</param>
    /// <returns>Storage type from environment or default</returns>
    public static StorageType GetStorageTypeFromEnvironment(StorageType defaultType = StorageType.InMemory)
    {
        var envValue = Environment.GetEnvironmentVariable("PRODUCT_CATALOG_STORAGE_TYPE");
        
        if (string.IsNullOrWhiteSpace(envValue))
            return defaultType;

        if (Enum.TryParse<StorageType>(envValue, true, out var storageType))
            return storageType;

        Console.WriteLine($"Invalid storage type in environment variable: {envValue}. Using default: {defaultType}");
        return defaultType;
    }

    /// <summary>
    /// Creates a storage configuration based on environment variables
    /// </summary>
    /// <returns>Storage configuration based on environment settings</returns>
    public static StorageConfiguration CreateFromEnvironment()
    {
        var storageType = GetStorageTypeFromEnvironment();
        
        return storageType switch
        {
            StorageType.InMemory => CreateInMemoryConfiguration(),
            StorageType.CosmosDb => CreateCosmosDbConfigurationFromEnvironment(),
            StorageType.Redis => CreateRedisConfigurationFromEnvironment(),
            _ => CreateInMemoryConfiguration()
        };
    }

    private static StorageConfiguration CreateCosmosDbConfigurationFromEnvironment()
    {
        var connectionString = Environment.GetEnvironmentVariable("COSMOS_DB_CONNECTION_STRING");
        var databaseName = Environment.GetEnvironmentVariable("COSMOS_DB_DATABASE_NAME");
        var containerName = Environment.GetEnvironmentVariable("COSMOS_DB_CONTAINER_NAME");

        return new StorageConfiguration
        {
            StorageType = StorageType.CosmosDb,
            CosmosDbConnectionString = connectionString, // null will use emulator default
            CosmosDbDatabaseName = databaseName ?? "TinyUrlDB",
            CosmosDbContainerName = containerName ?? "UrlMappings"
        };
    }

    private static StorageConfiguration CreateRedisConfigurationFromEnvironment()
    {
        var connectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING") ?? "localhost:6379";
        var keyPrefix = Environment.GetEnvironmentVariable("REDIS_KEY_PREFIX") ?? "productcatalog:";
        var expirationStr = Environment.GetEnvironmentVariable("REDIS_EXPIRATION_MINUTES");
        
        var expiration = 60;
        if (!string.IsNullOrWhiteSpace(expirationStr) && int.TryParse(expirationStr, out var parsedExpiration))
        {
            expiration = parsedExpiration;
        }

        return new StorageConfiguration
        {
            StorageType = StorageType.Redis,
            RedisConnectionString = connectionString,
            RedisKeyPrefix = keyPrefix,
            RedisCacheExpirationMinutes = expiration
        };
    }
}
