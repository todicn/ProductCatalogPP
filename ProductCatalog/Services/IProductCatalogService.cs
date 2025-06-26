using ProductCatalog.Models;

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
    /// <exception cref="ProductAlreadyExistsException">Thrown when product already exists</exception>
    /// <exception cref="ArgumentException">Thrown when name is invalid or quantity is negative</exception>
    void AddProduct(string name, int quantity);

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
} 