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

    public void AddProduct(string name, int quantity, string? category = null, IEnumerable<string>? tags = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name cannot be null or empty.", nameof(name));

        if (quantity < 0)
            throw new ArgumentException("Product quantity cannot be negative.", nameof(quantity));

        var trimmedName = name.Trim();
        
        if (_products.ContainsKey(trimmedName))
            throw new ProductAlreadyExistsException(trimmedName);

        var product = new Product(trimmedName, quantity, category, tags);
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

    public IReadOnlyList<Product> SearchByCategory(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Category cannot be null or empty.", nameof(category));

        return _products.Values
            .Where(p => string.Equals(p.Category, category.Trim(), StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(p => p.Quantity)
            .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToList()
            .AsReadOnly();
    }

    public IReadOnlyList<Product> SearchByTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            throw new ArgumentException("Tag cannot be null or empty.", nameof(tag));

        return _products.Values
            .Where(p => p.Tags.Contains(tag.Trim()))
            .OrderByDescending(p => p.Quantity)
            .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToList()
            .AsReadOnly();
    }

    public IReadOnlyList<Product> SearchByTags(IEnumerable<string> tags)
    {
        if (tags == null)
            throw new ArgumentNullException(nameof(tags));

        var tagList = tags.Where(t => !string.IsNullOrWhiteSpace(t))
                         .Select(t => t.Trim())
                         .ToList();

        if (!tagList.Any())
            throw new ArgumentException("At least one valid tag must be provided.", nameof(tags));

        return _products.Values
            .Where(p => tagList.Any(tag => p.Tags.Contains(tag)))
            .OrderByDescending(p => p.Quantity)
            .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToList()
            .AsReadOnly();
    }

    public IReadOnlyList<string> GetAllCategories()
    {
        return _products.Values
            .Select(p => p.Category)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
            .ToList()
            .AsReadOnly();
    }

    public IReadOnlyList<string> GetAllTags()
    {
        return _products.Values
            .SelectMany(p => p.Tags)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
            .ToList()
            .AsReadOnly();
    }
} 