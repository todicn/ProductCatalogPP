using ProductCatalog.Exceptions;
using ProductCatalog.Models;

namespace ProductCatalog.Services;

/// <summary>
/// Implementation of product catalog operations
/// </summary>
public class ProductCatalogService : IProductCatalogService
{
    private readonly Dictionary<string, Product> _products;

    public ProductCatalogService()
    {
        _products = new Dictionary<string, Product>(StringComparer.OrdinalIgnoreCase);
    }

    public void AddProduct(string name, int quantity)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name cannot be null or empty.", nameof(name));

        if (quantity < 0)
            throw new ArgumentException("Product quantity cannot be negative.", nameof(quantity));

        var trimmedName = name.Trim();
        
        if (_products.ContainsKey(trimmedName))
            throw new ProductAlreadyExistsException(trimmedName);

        var product = new Product(trimmedName, quantity);
        _products.Add(trimmedName, product);
    }

    public void RemoveProduct(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name cannot be null or empty.", nameof(name));

        var trimmedName = name.Trim();
        
        if (!_products.ContainsKey(trimmedName))
            throw new ProductNotFoundException(trimmedName);

        _products.Remove(trimmedName);
    }

    public void PurchaseProduct(string name, int quantity)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name cannot be null or empty.", nameof(name));

        if (quantity <= 0)
            throw new ArgumentException("Purchase quantity must be positive.", nameof(quantity));

        var trimmedName = name.Trim();
        
        if (!_products.TryGetValue(trimmedName, out var product))
            throw new ProductNotFoundException(trimmedName);

        if (product.Quantity < quantity)
            throw new InsufficientQuantityException(trimmedName, product.Quantity, quantity);

        product.Quantity -= quantity;
    }

    public IReadOnlyList<Product> ListProductsByQuantity()
    {
        return _products.Values
            .OrderByDescending(p => p.Quantity)
            .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToList()
            .AsReadOnly();
    }

    public Product GetProduct(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name cannot be null or empty.", nameof(name));

        var trimmedName = name.Trim();
        
        if (!_products.TryGetValue(trimmedName, out var product))
            throw new ProductNotFoundException(trimmedName);

        return product;
    }

    public int GetProductCount()
    {
        return _products.Count;
    }
} 