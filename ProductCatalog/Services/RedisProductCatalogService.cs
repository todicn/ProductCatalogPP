using ProductCatalog.Exceptions;
using ProductCatalog.Models;
using StackExchange.Redis;
using System.Text.Json;

namespace ProductCatalog.Services;

/// <summary>
/// Redis implementation of the product catalog service
/// </summary>
public class RedisProductCatalogService : IProductCatalogService
{
    private readonly IDatabase _database;
    private readonly IConnectionMultiplexer _redis;
    private readonly StorageConfiguration _config;
    private readonly string _keyPrefix;
    private readonly TimeSpan _expiration;

    public RedisProductCatalogService(StorageConfiguration config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _keyPrefix = config.RedisKeyPrefix;
        _expiration = TimeSpan.FromMinutes(config.RedisCacheExpirationMinutes);

        var connectionString = config.RedisConnectionString ?? "localhost:6379";
        
        try
        {
            _redis = ConnectionMultiplexer.Connect(connectionString);
            _database = _redis.GetDatabase();
            Console.WriteLine("Redis Product Catalog Service initialized");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to connect to Redis: {ex.Message}");
            throw new InvalidOperationException($"Failed to connect to Redis: {ex.Message}", ex);
        }
    }

    private string GetProductKey(string productName) => $"{_keyPrefix}product:{productName.ToLowerInvariant()}";
    private string GetAllProductsKey() => $"{_keyPrefix}products:all";
    private string GetCategoriesKey() => $"{_keyPrefix}categories";
    private string GetTagsKey() => $"{_keyPrefix}tags";

    public void AddProduct(string name, int quantity, string? category = null, IEnumerable<string>? tags = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name cannot be null or empty.", nameof(name));

        if (quantity < 0)
            throw new ArgumentException("Product quantity cannot be negative.", nameof(quantity));

        var trimmedName = name.Trim();
        var productKey = GetProductKey(trimmedName);

        try
        {
            // Check if product already exists
            if (_database.KeyExists(productKey))
                throw new ProductAlreadyExistsException(trimmedName);

            var product = new Product(trimmedName, quantity, category ?? "General", tags);
            var productJson = JsonSerializer.Serialize(new RedisProduct
            {
                Name = product.Name,
                Quantity = product.Quantity,
                Category = product.Category,
                Tags = product.Tags.ToList()
            });

            // Store product
            _database.StringSet(productKey, productJson, _expiration);

            // Add to products set
            _database.SetAdd(GetAllProductsKey(), trimmedName.ToLowerInvariant());
            _database.KeyExpire(GetAllProductsKey(), _expiration);

            // Add category to categories set
            _database.SetAdd(GetCategoriesKey(), product.Category);
            _database.KeyExpire(GetCategoriesKey(), _expiration);

            // Add tags to tags set
            foreach (var tag in product.Tags)
            {
                _database.SetAdd(GetTagsKey(), tag);
            }
            _database.KeyExpire(GetTagsKey(), _expiration);

            Console.WriteLine($"Product {trimmedName} added to Redis");
        }
        catch (RedisException ex)
        {
            Console.WriteLine($"Failed to add product {trimmedName} to Redis: {ex.Message}");
            throw new InvalidOperationException($"Failed to add product: {ex.Message}", ex);
        }
    }

    public void RemoveProduct(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name cannot be null or empty.", nameof(name));

        var trimmedName = name.Trim();
        var productKey = GetProductKey(trimmedName);

        try
        {
            if (!_database.KeyExists(productKey))
                throw new ProductNotFoundException(trimmedName);

            // Get product data before deletion for cleanup
            var productJson = _database.StringGet(productKey);
            if (productJson.HasValue)
            {
                var redisProduct = JsonSerializer.Deserialize<RedisProduct>(productJson!);
                
                // Remove tags that are no longer used
                foreach (var tag in redisProduct!.Tags)
                {
                    var isTagUsed = CheckIfTagIsUsedByOtherProducts(tag, trimmedName);
                    if (!isTagUsed)
                    {
                        _database.SetRemove(GetTagsKey(), tag);
                    }
                }

                // Remove category if no longer used
                var isCategoryUsed = CheckIfCategoryIsUsedByOtherProducts(redisProduct.Category, trimmedName);
                if (!isCategoryUsed)
                {
                    _database.SetRemove(GetCategoriesKey(), redisProduct.Category);
                }
            }

            // Remove product
            _database.KeyDelete(productKey);
            _database.SetRemove(GetAllProductsKey(), trimmedName.ToLowerInvariant());

            Console.WriteLine($"Product {trimmedName} removed from Redis");
        }
        catch (RedisException ex)
        {
            Console.WriteLine($"Failed to remove product {trimmedName} from Redis: {ex.Message}");
            throw new InvalidOperationException($"Failed to remove product: {ex.Message}", ex);
        }
    }

    public void PurchaseProduct(string name, int quantity)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name cannot be null or empty.", nameof(name));

        if (quantity <= 0)
            throw new ArgumentException("Purchase quantity must be positive.", nameof(quantity));

        var trimmedName = name.Trim();
        var productKey = GetProductKey(trimmedName);

        try
        {
            var productJson = _database.StringGet(productKey);
            if (!productJson.HasValue)
                throw new ProductNotFoundException(trimmedName);

            var redisProduct = JsonSerializer.Deserialize<RedisProduct>(productJson!);
            if (redisProduct == null)
                throw new ProductNotFoundException(trimmedName);

            if (redisProduct.Quantity < quantity)
                throw new InsufficientQuantityException(trimmedName, redisProduct.Quantity, quantity);

            redisProduct.Quantity -= quantity;

            var updatedJson = JsonSerializer.Serialize(redisProduct);
            _database.StringSet(productKey, updatedJson, _expiration);

            Console.WriteLine($"Purchased {quantity} units of {trimmedName}");
        }
        catch (RedisException ex)
        {
            Console.WriteLine($"Failed to purchase product {trimmedName} from Redis: {ex.Message}");
            throw new InvalidOperationException($"Failed to purchase product: {ex.Message}", ex);
        }
    }

    public IReadOnlyList<Product> ListProductsByQuantity()
    {
        try
        {
            var productIds = _database.SetMembers(GetAllProductsKey());
            var products = new List<Product>();

            foreach (var productId in productIds)
            {
                var productKey = GetProductKey(productId!);
                var productJson = _database.StringGet(productKey);
                if (productJson.HasValue)
                {
                    var redisProduct = JsonSerializer.Deserialize<RedisProduct>(productJson!);
                    if (redisProduct != null)
                    {
                        var product = new Product(redisProduct.Name, redisProduct.Quantity, redisProduct.Category, redisProduct.Tags);
                        products.Add(product);
                    }
                }
            }

            return products.OrderByDescending(p => p.Quantity)
                          .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                          .ToList()
                          .AsReadOnly();
        }
        catch (RedisException ex)
        {
            Console.WriteLine($"Failed to list products from Redis: {ex.Message}");
            throw new InvalidOperationException($"Failed to list products: {ex.Message}", ex);
        }
    }

    public Product GetProduct(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name cannot be null or empty.", nameof(name));

        var trimmedName = name.Trim();
        var productKey = GetProductKey(trimmedName);

        try
        {
            var productJson = _database.StringGet(productKey);
            if (!productJson.HasValue)
                throw new ProductNotFoundException(trimmedName);

            var redisProduct = JsonSerializer.Deserialize<RedisProduct>(productJson!);
            if (redisProduct == null)
                throw new ProductNotFoundException(trimmedName);

            return new Product(redisProduct.Name, redisProduct.Quantity, redisProduct.Category, redisProduct.Tags);
        }
        catch (RedisException ex)
        {
            Console.WriteLine($"Failed to get product {trimmedName} from Redis: {ex.Message}");
            throw new InvalidOperationException($"Failed to get product: {ex.Message}", ex);
        }
    }

    public int GetProductCount()
    {
        try
        {
            return (int)_database.SetLength(GetAllProductsKey());
        }
        catch (RedisException ex)
        {
            Console.WriteLine($"Failed to get product count from Redis: {ex.Message}");
            throw new InvalidOperationException($"Failed to get product count: {ex.Message}", ex);
        }
    }

    public IReadOnlyList<Product> SearchByCategory(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Category cannot be null or empty.", nameof(category));

        try
        {
            var allProducts = ListProductsByQuantity();
            return allProducts.Where(p => string.Equals(p.Category, category.Trim(), StringComparison.OrdinalIgnoreCase))
                             .ToList()
                             .AsReadOnly();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to search products by category from Redis: {ex.Message}");
            throw new InvalidOperationException($"Failed to search by category: {ex.Message}", ex);
        }
    }

    public IReadOnlyList<Product> SearchByTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            throw new ArgumentException("Tag cannot be null or empty.", nameof(tag));

        return SearchByTags(new[] { tag.Trim() });
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

        try
        {
            var allProducts = ListProductsByQuantity();
            return allProducts.Where(p => tagList.Any(tag => p.Tags.Contains(tag)))
                             .ToList()
                             .AsReadOnly();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to search products by tags from Redis: {ex.Message}");
            throw new InvalidOperationException($"Failed to search by tags: {ex.Message}", ex);
        }
    }

    public IReadOnlyList<string> GetAllCategories()
    {
        try
        {
            var categories = _database.SetMembers(GetCategoriesKey());
            return categories.Select(c => c.ToString())
                           .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
                           .ToList()
                           .AsReadOnly();
        }
        catch (RedisException ex)
        {
            Console.WriteLine($"Failed to get categories from Redis: {ex.Message}");
            throw new InvalidOperationException($"Failed to get categories: {ex.Message}", ex);
        }
    }

    public IReadOnlyList<string> GetAllTags()
    {
        try
        {
            var tags = _database.SetMembers(GetTagsKey());
            return tags.Select(t => t.ToString())
                      .OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
                      .ToList()
                      .AsReadOnly();
        }
        catch (RedisException ex)
        {
            Console.WriteLine($"Failed to get tags from Redis: {ex.Message}");
            throw new InvalidOperationException($"Failed to get tags: {ex.Message}", ex);
        }
    }

    private bool CheckIfTagIsUsedByOtherProducts(string tag, string excludeProductName)
    {
        var productIds = _database.SetMembers(GetAllProductsKey());
        foreach (var productId in productIds)
        {
            if (string.Equals(productId!, excludeProductName, StringComparison.OrdinalIgnoreCase))
                continue;

            var productKey = GetProductKey(productId!);
            var productJson = _database.StringGet(productKey);
            if (productJson.HasValue)
            {
                var redisProduct = JsonSerializer.Deserialize<RedisProduct>(productJson!);
                if (redisProduct?.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase) == true)
                    return true;
            }
        }
        return false;
    }

    private bool CheckIfCategoryIsUsedByOtherProducts(string category, string excludeProductName)
    {
        var productIds = _database.SetMembers(GetAllProductsKey());
        foreach (var productId in productIds)
        {
            if (string.Equals(productId!, excludeProductName, StringComparison.OrdinalIgnoreCase))
                continue;

            var productKey = GetProductKey(productId!);
            var productJson = _database.StringGet(productKey);
            if (productJson.HasValue)
            {
                var redisProduct = JsonSerializer.Deserialize<RedisProduct>(productJson!);
                if (string.Equals(redisProduct?.Category, category, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        return false;
    }
}

/// <summary>
/// Redis document model for products
/// </summary>
internal class RedisProduct
{
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Category { get; set; } = "General";
    public List<string> Tags { get; set; } = new();
}
