using ProductCatalog.Models;

namespace ProductCatalog.Logging;

/// <summary>
/// Observer interface for product catalog events
/// </summary>
public interface IProductCatalogObserver
{
    /// <summary>
    /// Called when a product is added to the catalog
    /// </summary>
    /// <param name="eventData">Event data containing product information</param>
    void OnProductAdded(ProductEventData eventData);

    /// <summary>
    /// Called when a product is removed from the catalog
    /// </summary>
    /// <param name="eventData">Event data containing product information</param>
    void OnProductRemoved(ProductEventData eventData);

    /// <summary>
    /// Called when a product is purchased
    /// </summary>
    /// <param name="eventData">Event data containing product and purchase information</param>
    void OnProductPurchased(PurchaseEventData eventData);

    /// <summary>
    /// Called when an operation fails with an exception
    /// </summary>
    /// <param name="eventData">Event data containing error information</param>
    void OnOperationFailed(ErrorEventData eventData);

    /// <summary>
    /// Called when products are listed/queried
    /// </summary>
    /// <param name="eventData">Event data containing query information</param>
    void OnProductsQueried(QueryEventData eventData);
} 