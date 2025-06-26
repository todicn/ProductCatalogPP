using Microsoft.Azure.Cosmos;
using ProductCatalog.Exceptions;
using ProductCatalog.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProductCatalog.Services;

/// <summary>
/// Cosmos DB implementation of the product catalog service
/// </summary>
public class CosmosDbProductCatalogService : IProductCatalogService
{
    private readonly CosmosClient _cosmosClient;
    private readonly Container _container;
    private readonly StorageConfiguration _config;

    public CosmosDbProductCatalogService(StorageConfiguration config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));

        if (string.IsNullOrWhiteSpace(config.CosmosDbConnectionString))
        {
            // Use Cosmos DB Emulator default connection for local development
            var connectionString = "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
            _cosmosClient = new CosmosClient(connectionString);
        }
        else
        {
            _cosmosClient = new CosmosClient(config.CosmosDbConnectionString);
        }

        var database = _cosmosClient.GetDatabase(config.CosmosDbDatabaseName);
        _container = database.GetContainer(config.CosmosDbContainerName);

        Console.WriteLine("CosmosDB Product Catalog Service initialized");
    }

    public void AddProduct(string name, int quantity, string? category = null, IEnumerable<string>? tags = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name cannot be null or empty.", nameof(name));

        if (quantity < 0)
            throw new ArgumentException("Product quantity cannot be negative.", nameof(quantity));

        var trimmedName = name.Trim();

        try
        {
            // Check if product already exists
            var existingItem = _container.ReadItemAsync<CosmosProduct>(
                trimmedName.ToLowerInvariant(),
                new PartitionKey(trimmedName.ToLowerInvariant())).Result;
            
            throw new ProductAlreadyExistsException(trimmedName);
        }
        catch (AggregateException aggEx) when (aggEx.InnerException is CosmosException ex && ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Product doesn't exist, we can create it
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Product doesn't exist, we can create it
        }

        var product = new Product(trimmedName, quantity, category ?? "General", tags);
        var cosmosProduct = new CosmosProduct
        {
            Id = trimmedName.ToLowerInvariant(),
            Name = product.Name,
            Quantity = product.Quantity,
            Category = product.Category,
            Tags = product.Tags.ToList()
        };

        try
        {
            _container.CreateItemAsync(cosmosProduct, new PartitionKey(cosmosProduct.Id)).Wait();
            Console.WriteLine($"Product {trimmedName} added to Cosmos DB");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to add product {trimmedName} to Cosmos DB: {ex.Message}");
            throw new InvalidOperationException($"Failed to add product: {ex.Message}", ex);
        }
    }

    public void RemoveProduct(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name cannot be null or empty.", nameof(name));

        var trimmedName = name.Trim();
        var id = trimmedName.ToLowerInvariant();

        try
        {
            _container.DeleteItemAsync<CosmosProduct>(id, new PartitionKey(id)).Wait();
            Console.WriteLine($"Product {trimmedName} removed from Cosmos DB");
        }
        catch (AggregateException aggEx) when (aggEx.InnerException is CosmosException ex && ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new ProductNotFoundException(trimmedName);
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new ProductNotFoundException(trimmedName);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to remove product {trimmedName} from Cosmos DB: {ex.Message}");
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
        var id = trimmedName.ToLowerInvariant();

        try
        {
            var response = _container.ReadItemAsync<CosmosProduct>(id, new PartitionKey(id)).Result;
            var cosmosProduct = response.Resource;

            if (cosmosProduct.Quantity < quantity)
                throw new InsufficientQuantityException(trimmedName, cosmosProduct.Quantity, quantity);

            cosmosProduct.Quantity -= quantity;

            _container.ReplaceItemAsync(cosmosProduct, id, new PartitionKey(id)).Wait();
            Console.WriteLine($"Purchased {quantity} units of {trimmedName}");
        }
        catch (AggregateException aggEx) when (aggEx.InnerException is CosmosException ex && ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new ProductNotFoundException(trimmedName);
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new ProductNotFoundException(trimmedName);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to purchase product {trimmedName} from Cosmos DB: {ex.Message}");
            throw new InvalidOperationException($"Failed to purchase product: {ex.Message}", ex);
        }
    }

    public IReadOnlyList<Product> ListProductsByQuantity()
    {
        try
        {
            var query = "SELECT * FROM c ORDER BY c.Quantity DESC, c.Name ASC";
            var queryDefinition = new QueryDefinition(query);
            var queryResultSetIterator = _container.GetItemQueryIterator<CosmosProduct>(queryDefinition);

            var products = new List<Product>();
            while (queryResultSetIterator.HasMoreResults)
            {
                var currentResultSet = queryResultSetIterator.ReadNextAsync().Result;
                foreach (var cosmosProduct in currentResultSet)
                {
                    var product = new Product(cosmosProduct.Name, cosmosProduct.Quantity, cosmosProduct.Category, cosmosProduct.Tags);
                    products.Add(product);
                }
            }

            return products.AsReadOnly();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to list products from Cosmos DB: {ex.Message}");
            throw new InvalidOperationException($"Failed to list products: {ex.Message}", ex);
        }
    }

    public Product GetProduct(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name cannot be null or empty.", nameof(name));

        var trimmedName = name.Trim();
        var id = trimmedName.ToLowerInvariant();

        try
        {
            var response = _container.ReadItemAsync<CosmosProduct>(id, new PartitionKey(id)).Result;
            var cosmosProduct = response.Resource;
            return new Product(cosmosProduct.Name, cosmosProduct.Quantity, cosmosProduct.Category, cosmosProduct.Tags);
        }
        catch (AggregateException aggEx) when (aggEx.InnerException is CosmosException ex && ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new ProductNotFoundException(trimmedName);
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new ProductNotFoundException(trimmedName);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to get product {trimmedName} from Cosmos DB: {ex.Message}");
            throw new InvalidOperationException($"Failed to get product: {ex.Message}", ex);
        }
    }

    public int GetProductCount()
    {
        try
        {
            var query = "SELECT VALUE COUNT(1) FROM c";
            var queryDefinition = new QueryDefinition(query);
            var queryResultSetIterator = _container.GetItemQueryIterator<int>(queryDefinition);

            var result = queryResultSetIterator.ReadNextAsync().Result;
            return result.FirstOrDefault();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to get product count from Cosmos DB: {ex.Message}");
            throw new InvalidOperationException($"Failed to get product count: {ex.Message}", ex);
        }
    }

    public IReadOnlyList<Product> SearchByCategory(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Category cannot be null or empty.", nameof(category));

        try
        {
            var query = "SELECT * FROM c WHERE UPPER(c.Category) = UPPER(@category) ORDER BY c.Quantity DESC, c.Name ASC";
            var queryDefinition = new QueryDefinition(query).WithParameter("@category", category.Trim());
            var queryResultSetIterator = _container.GetItemQueryIterator<CosmosProduct>(queryDefinition);

            var products = new List<Product>();
            while (queryResultSetIterator.HasMoreResults)
            {
                var currentResultSet = queryResultSetIterator.ReadNextAsync().Result;
                foreach (var cosmosProduct in currentResultSet)
                {
                    var product = new Product(cosmosProduct.Name, cosmosProduct.Quantity, cosmosProduct.Category, cosmosProduct.Tags);
                    products.Add(product);
                }
            }

            return products.AsReadOnly();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to search products by category from Cosmos DB: {ex.Message}");
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
            var query = "SELECT * FROM c WHERE EXISTS(SELECT VALUE t FROM t IN c.Tags WHERE UPPER(t) IN (" +
                       string.Join(",", tagList.Select((_, i) => $"@tag{i}")) + 
                       ")) ORDER BY c.Quantity DESC, c.Name ASC";
            
            var queryDefinition = new QueryDefinition(query);
            for (int i = 0; i < tagList.Count; i++)
            {
                queryDefinition = queryDefinition.WithParameter($"@tag{i}", tagList[i].ToUpperInvariant());
            }

            var queryResultSetIterator = _container.GetItemQueryIterator<CosmosProduct>(queryDefinition);

            var products = new List<Product>();
            while (queryResultSetIterator.HasMoreResults)
            {
                var currentResultSet = queryResultSetIterator.ReadNextAsync().Result;
                foreach (var cosmosProduct in currentResultSet)
                {
                    var product = new Product(cosmosProduct.Name, cosmosProduct.Quantity, cosmosProduct.Category, cosmosProduct.Tags);
                    products.Add(product);
                }
            }

            return products.AsReadOnly();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to search products by tags from Cosmos DB: {ex.Message}");
            throw new InvalidOperationException($"Failed to search by tags: {ex.Message}", ex);
        }
    }

    public IReadOnlyList<string> GetAllCategories()
    {
        try
        {
            var query = "SELECT DISTINCT VALUE c.Category FROM c ORDER BY c.Category";
            var queryDefinition = new QueryDefinition(query);
            var queryResultSetIterator = _container.GetItemQueryIterator<string>(queryDefinition);

            var categories = new List<string>();
            while (queryResultSetIterator.HasMoreResults)
            {
                var currentResultSet = queryResultSetIterator.ReadNextAsync().Result;
                categories.AddRange(currentResultSet);
            }

            return categories.AsReadOnly();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to get categories from Cosmos DB: {ex.Message}");
            throw new InvalidOperationException($"Failed to get categories: {ex.Message}", ex);
        }
    }

    public IReadOnlyList<string> GetAllTags()
    {
        try
        {
            var query = "SELECT DISTINCT VALUE t FROM c JOIN t IN c.Tags ORDER BY t";
            var queryDefinition = new QueryDefinition(query);
            var queryResultSetIterator = _container.GetItemQueryIterator<string>(queryDefinition);

            var tags = new List<string>();
            while (queryResultSetIterator.HasMoreResults)
            {
                var currentResultSet = queryResultSetIterator.ReadNextAsync().Result;
                tags.AddRange(currentResultSet);
            }

            return tags.AsReadOnly();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to get tags from Cosmos DB: {ex.Message}");
            throw new InvalidOperationException($"Failed to get tags: {ex.Message}", ex);
        }
    }
}

/// <summary>
/// Cosmos DB document model for products
/// </summary>
internal class CosmosProduct
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("category")]
    public string Category { get; set; } = "General";

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();
}
