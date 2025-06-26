namespace ProductCatalog.Services;

/// <summary>
/// Factory interface for creating product catalog service instances
/// </summary>
public interface IProductCatalogServiceFactory
{
    /// <summary>
    /// Creates a product catalog service instance based on configuration
    /// </summary>
    /// <returns>Product catalog service instance</returns>
    IProductCatalogService CreateService();
}
