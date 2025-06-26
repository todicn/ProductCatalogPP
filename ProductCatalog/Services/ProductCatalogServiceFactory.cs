namespace ProductCatalog.Services;

/// <summary>
/// Factory implementation for creating product catalog service instances
/// </summary>
public class ProductCatalogServiceFactory : IProductCatalogServiceFactory
{
    private readonly StorageConfiguration _config;

    public ProductCatalogServiceFactory(StorageConfiguration config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public IProductCatalogService CreateService()
    {
        Console.WriteLine($"Creating product catalog service with storage type: {_config.StorageType}");

        return _config.StorageType switch
        {
            StorageType.InMemory => new ProductCatalogService(),
            StorageType.CosmosDb => new CosmosDbProductCatalogService(_config),
            StorageType.Redis => new RedisProductCatalogService(_config),
            _ => throw new NotSupportedException($"Storage type {_config.StorageType} is not supported")
        };
    }
}
