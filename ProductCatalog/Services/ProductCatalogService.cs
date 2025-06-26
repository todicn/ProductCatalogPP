using ProductCatalog.Exceptions;
using ProductCatalog.Models;
using ProductCatalog.Logging;
using System.Diagnostics;

namespace ProductCatalog.Services;

/// <summary>
/// Implementation of product catalog operations with observer pattern support
/// </summary>
public class ProductCatalogService : IProductCatalogService
{
    private readonly Dictionary<string, Product> _products;
    private readonly List<IProductCatalogObserver> _observers;
    private readonly object _observerLock = new();

    public ProductCatalogService()
    {
        _products = new Dictionary<string, Product>(StringComparer.OrdinalIgnoreCase);
        _observers = new List<IProductCatalogObserver>();
    }

    /// <summary>
    /// Adds an observer to receive notifications of catalog operations
    /// </summary>
    /// <param name="observer">The observer to add</param>
    public void AddObserver(IProductCatalogObserver observer)
    {
        if (observer == null) throw new ArgumentNullException(nameof(observer));
        
        lock (_observerLock)
        {
            if (!_observers.Contains(observer))
            {
                _observers.Add(observer);
            }
        }
    }

    /// <summary>
    /// Removes an observer from receiving notifications
    /// </summary>
    /// <param name="observer">The observer to remove</param>
    public void RemoveObserver(IProductCatalogObserver observer)
    {
        if (observer == null) return;
        
        lock (_observerLock)
        {
            _observers.Remove(observer);
        }
    }

    public void AddProduct(string name, int quantity, string? category = null, IEnumerable<string>? tags = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Product name cannot be null or empty.", nameof(name));

            if (quantity < 0)
                throw new ArgumentException("Product quantity cannot be negative.", nameof(quantity));

            var trimmedName = name.Trim();
            
            if (_products.ContainsKey(trimmedName))
                throw new ProductAlreadyExistsException(trimmedName);

            var product = new Product(trimmedName, quantity, category ?? "General", tags);
            _products.Add(trimmedName, product);

            // Notify observers of successful addition
            NotifyObservers(observer => observer.OnProductAdded(
                new ProductEventData(trimmedName, quantity, "AddProduct")));
        }
        catch (Exception ex)
        {
            // Notify observers of failure
            NotifyObservers(observer => observer.OnOperationFailed(
                new ErrorEventData("AddProduct", ex, name?.Trim())));
            throw;
        }
    }

    public void RemoveProduct(string name)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Product name cannot be null or empty.", nameof(name));

            var trimmedName = name.Trim();
            
            if (!_products.ContainsKey(trimmedName))
                throw new ProductNotFoundException(trimmedName);

            _products.Remove(trimmedName);

            // Notify observers of successful removal
            NotifyObservers(observer => observer.OnProductRemoved(
                new ProductEventData(trimmedName, 0, "RemoveProduct")));
        }
        catch (Exception ex)
        {
            // Notify observers of failure
            NotifyObservers(observer => observer.OnOperationFailed(
                new ErrorEventData("RemoveProduct", ex, name?.Trim())));
            throw;
        }
    }

    public void PurchaseProduct(string name, int quantity)
    {
        try
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

            var originalQuantity = product.Quantity;
            product.Quantity -= quantity;

            // Notify observers of successful purchase
            NotifyObservers(observer => observer.OnProductPurchased(
                new PurchaseEventData(trimmedName, quantity, product.Quantity, originalQuantity)));
        }
        catch (Exception ex)
        {
            // Notify observers of failure
            NotifyObservers(observer => observer.OnOperationFailed(
                new ErrorEventData("PurchaseProduct", ex, name?.Trim())));
            throw;
        }
    }

    public IReadOnlyList<Product> ListProductsByQuantity()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = _products.Values
                .OrderByDescending(p => p.Quantity)
                .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();

            stopwatch.Stop();

            // Notify observers of successful query
            NotifyObservers(observer => observer.OnProductsQueried(
                new QueryEventData("ListProductsByQuantity", result.Count, stopwatch.Elapsed)));

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            // Notify observers of failure
            NotifyObservers(observer => observer.OnOperationFailed(
                new ErrorEventData("ListProductsByQuantity", ex)));
            throw;
        }
    }

    public Product GetProduct(string name)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Product name cannot be null or empty.", nameof(name));

            var trimmedName = name.Trim();
            
            if (!_products.TryGetValue(trimmedName, out var product))
                throw new ProductNotFoundException(trimmedName);

            stopwatch.Stop();

            // Notify observers of successful query
            NotifyObservers(observer => observer.OnProductsQueried(
                new QueryEventData("GetProduct", 1, stopwatch.Elapsed, trimmedName)));

            return product;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            // Notify observers of failure
            NotifyObservers(observer => observer.OnOperationFailed(
                new ErrorEventData("GetProduct", ex, name?.Trim())));
            throw;
        }
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

    /// <summary>
    /// Notifies all registered observers with the specified action
    /// </summary>
    /// <param name="notifyAction">Action to perform on each observer</param>
    private void NotifyObservers(Action<IProductCatalogObserver> notifyAction)
    {
        List<IProductCatalogObserver> observersCopy;
        
        lock (_observerLock)
        {
            observersCopy = new List<IProductCatalogObserver>(_observers);
        }

        foreach (var observer in observersCopy)
        {
            try
            {
                notifyAction(observer);
            }
            catch (Exception ex)
            {
                // Log observer notification failure, but don't let it affect the main operation
                Console.WriteLine($"Observer notification failed: {ex.Message}");
            }
        }
    }
} 