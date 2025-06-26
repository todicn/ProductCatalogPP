namespace ProductCatalog.Services;

/// <summary>
/// Configuration for storage backend selection
/// </summary>
public class StorageConfiguration
{
    /// <summary>
    /// Type of storage backend to use
    /// </summary>
    public StorageType StorageType { get; set; } = StorageType.InMemory;

    /// <summary>
    /// Cosmos DB connection string
    /// </summary>
    public string? CosmosDbConnectionString { get; set; }

    /// <summary>
    /// Cosmos DB database name
    /// </summary>
    public string CosmosDbDatabaseName { get; set; } = "TinyUrlDB";

    /// <summary>
    /// Cosmos DB container name
    /// </summary>
    public string CosmosDbContainerName { get; set; } = "UrlMappings";

    /// <summary>
    /// Redis connection string
    /// </summary>
    public string? RedisConnectionString { get; set; }

    /// <summary>
    /// Redis key prefix for product catalog data
    /// </summary>
    public string RedisKeyPrefix { get; set; } = "productcatalog:";

    /// <summary>
    /// Cache expiration time in minutes for Redis
    /// </summary>
    public int RedisCacheExpirationMinutes { get; set; } = 60;
}

/// <summary>
/// Enum for storage type selection
/// </summary>
public enum StorageType
{
    InMemory,
    CosmosDb,
    Redis
}
