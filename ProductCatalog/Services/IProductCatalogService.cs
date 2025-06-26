using ProductCatalog.Models;
using ProductCatalog.Logging;
using ProductCatalog.Exceptions;

namespace ProductCatalog.Services;

/// <summary>
/// Interface for product catalog operations
/// </summary>
public interface IProductCatalogService
{
    /// <summary>
    /// Adds a new product to the catalog
    /// </summary>
    /// <param name="name">Product name</param>
    /// <param name="quantity">Initial quantity</param>
    /// <param name="category">Product category (optional, defaults to "General")</param>
    /// <param name="tags">Product tags (optional)</param>
    /// <exception cref="ProductAlreadyExistsException">Thrown when product already exists</exception>
    /// <exception cref="ArgumentException">Thrown when name is invalid or quantity is negative</exception>
    void AddProduct(string name, int quantity, string? category = null, IEnumerable<string>? tags = null);

    /// <summary>
    /// Removes a product from the catalog
    /// </summary>
    /// <param name="name">Product name to remove</param>
    /// <exception cref="ProductNotFoundException">Thrown when product doesn't exist</exception>
    void RemoveProduct(string name);

    /// <summary>
    /// Purchases a product (decreases quantity)
    /// </summary>
    /// <param name="name">Product name</param>
    /// <param name="quantity">Quantity to purchase</param>
    /// <exception cref="ProductNotFoundException">Thrown when product doesn't exist</exception>
    /// <exception cref="InsufficientQuantityException">Thrown when not enough quantity available</exception>
    /// <exception cref="ArgumentException">Thrown when quantity is not positive</exception>
    void PurchaseProduct(string name, int quantity);

    /// <summary>
    /// Lists all products sorted by quantity in descending order
    /// </summary>
    /// <returns>List of products sorted by quantity (highest first)</returns>
    IReadOnlyList<Product> ListProductsByQuantity();

    /// <summary>
    /// Gets a product by name
    /// </summary>
    /// <param name="name">Product name</param>
    /// <returns>The product if found</returns>
    /// <exception cref="ProductNotFoundException">Thrown when product doesn't exist</exception>
    Product GetProduct(string name);

    /// <summary>
    /// Gets the total number of products in the catalog
    /// </summary>
    /// <returns>Number of unique products</returns>
    int GetProductCount();

    /// <summary>
    /// Searches for products by category
    /// </summary>
    /// <param name="category">Category to search for</param>
    /// <returns>List of products in the specified category</returns>
    IReadOnlyList<Product> SearchByCategory(string category);

    /// <summary>
    /// Searches for products by tag
    /// </summary>
    /// <param name="tag">Tag to search for</param>
    /// <returns>List of products that have the specified tag</returns>
    IReadOnlyList<Product> SearchByTag(string tag);

    /// <summary>
    /// Searches for products that have any of the specified tags
    /// </summary>
    /// <param name="tags">Tags to search for</param>
    /// <returns>List of products that have any of the specified tags</returns>
    IReadOnlyList<Product> SearchByTags(IEnumerable<string> tags);

    /// <summary>
    /// Gets all unique categories in the catalog
    /// </summary>
    /// <returns>List of all categories</returns>
    IReadOnlyList<string> GetAllCategories();

    /// <summary>
    /// Gets all unique tags in the catalog
    /// </summary>
    /// <returns>List of all tags</returns>
    IReadOnlyList<string> GetAllTags();

    /// <summary>
    /// Adds an observer to receive notifications of catalog operations
    /// </summary>
    /// <param name="observer">The observer to add</param>
    void AddObserver(IProductCatalogObserver observer);

    /// <summary>
    /// Removes an observer from receiving notifications
    /// </summary>
    /// <param name="observer">The observer to remove</param>
    void RemoveObserver(IProductCatalogObserver observer);
} 