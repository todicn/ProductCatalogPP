using ProductCatalog.Exceptions;
using ProductCatalog.Services;
using ProductCatalog.Logging;

namespace ProductCatalog;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== Product Catalog Demo with Factory Pattern & Logging ===\n");

        // Demonstrate the factory pattern with different storage types
        DemonstrateStorageTypes();
    }

    static void DemonstrateStorageTypes()
    {
        Console.WriteLine("Choose storage type:");
        Console.WriteLine("1. In-Memory (default)");
        Console.WriteLine("2. Cosmos DB (local emulator)");
        Console.WriteLine("3. Redis (local instance)");
        Console.Write("Enter choice (1-3): ");

        var choice = Console.ReadLine();
        
        StorageConfiguration config = choice?.Trim() switch
        {
            "2" => StorageConfigurationHelper.CreateCosmosDbConfiguration(),
            "3" => StorageConfigurationHelper.CreateRedisConfiguration(),
            _ => StorageConfigurationHelper.CreateInMemoryConfiguration()
        };

        Console.WriteLine($"\nUsing {config.StorageType} storage...\n");

        try
        {
            var factory = new ProductCatalogServiceFactory(config);
            var catalog = factory.CreateService();

            // Setup observers for logging and diagnostics (if catalog supports it)
            IDisposable? fileLogger = null;
            DiagnosticsCollector? diagnosticsCollector = null;
            
            if (catalog is ProductCatalogService observableService)
            {
                var consoleLogger = new ConsoleLogger(useColors: true);
                fileLogger = new FileLogger(); // Will use default path
                diagnosticsCollector = new DiagnosticsCollector();
                
                observableService.AddObserver(consoleLogger);
                observableService.AddObserver((IProductCatalogObserver)fileLogger);
                observableService.AddObserver(diagnosticsCollector);
                
                Console.WriteLine("Logging and diagnostics observers configured.\n");
            }

            RunProductCatalogDemo(catalog, fileLogger, diagnosticsCollector);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize storage: {ex.Message}");
            Console.WriteLine("Falling back to in-memory storage...\n");
            
            // Fallback to in-memory
            var fallbackFactory = new ProductCatalogServiceFactory(StorageConfigurationHelper.CreateInMemoryConfiguration());
            var fallbackCatalog = fallbackFactory.CreateService();
            RunProductCatalogDemo(fallbackCatalog, null, null);
        }
    }

    static void RunProductCatalogDemo(IProductCatalogService catalog, IDisposable? fileLogger = null, DiagnosticsCollector? diagnosticsCollector = null)
    {
        try
        {
            // Demonstrate adding products with categories and tags
            Console.WriteLine("Adding products to catalog...");
            catalog.AddProduct("Laptop", 10, "Electronics", new[] { "computer", "portable", "work" });
            catalog.AddProduct("Mouse", 25, "Electronics", new[] { "computer", "wireless", "gaming" });
            catalog.AddProduct("Keyboard", 15, "Electronics", new[] { "computer", "mechanical", "gaming" });
            catalog.AddProduct("Monitor", 8, "Electronics", new[] { "computer", "display", "work" });
            catalog.AddProduct("Headphones", 20, "Audio", new[] { "wireless", "music", "gaming" });
            catalog.AddProduct("Coffee Mug", 50, "Office", new[] { "drink", "ceramic", "office" });
            catalog.AddProduct("Desk Lamp", 12, "Furniture", new[] { "light", "work", "adjustable" });
            Console.WriteLine($"Added 7 products. Total products: {catalog.GetProductCount()}\n");

            // List products by quantity
            Console.WriteLine("Products listed by quantity (descending):");
            var products = catalog.ListProductsByQuantity();
            foreach (var product in products)
            {
                Console.WriteLine($"  {product}");
            }
            Console.WriteLine();

            // Demonstrate purchasing
            Console.WriteLine("Purchasing products...");
            catalog.PurchaseProduct("Mouse", 5);
            Console.WriteLine("Purchased 5 mice");
            catalog.PurchaseProduct("Laptop", 3);
            Console.WriteLine("Purchased 3 laptops");
            Console.WriteLine();

            // List products after purchases
            Console.WriteLine("Products after purchases:");
            products = catalog.ListProductsByQuantity();
            foreach (var product in products)
            {
                Console.WriteLine($"  {product}");
            }
            Console.WriteLine();

            // Demonstrate category and tag searches
            Console.WriteLine("=== Category and Tag Search Demonstrations ===\n");

            // Show all categories
            Console.WriteLine("All categories:");
            var categories = catalog.GetAllCategories();
            foreach (var category in categories)
            {
                Console.WriteLine($"  - {category}");
            }
            Console.WriteLine();

            // Show all tags
            Console.WriteLine("All tags:");
            var tags = catalog.GetAllTags();
            foreach (var tag in tags)
            {
                Console.WriteLine($"  - {tag}");
            }
            Console.WriteLine();

            // Search by category
            Console.WriteLine("Products in 'Electronics' category:");
            var electronicsProducts = catalog.SearchByCategory("Electronics");
            foreach (var product in electronicsProducts)
            {
                Console.WriteLine($"  {product}");
            }
            Console.WriteLine();

            // Search by single tag
            Console.WriteLine("Products with 'gaming' tag:");
            var gamingProducts = catalog.SearchByTag("gaming");
            foreach (var product in gamingProducts)
            {
                Console.WriteLine($"  {product}");
            }
            Console.WriteLine();

            // Search by multiple tags
            Console.WriteLine("Products with 'work' or 'office' tags:");
            var workOfficeProducts = catalog.SearchByTags(new[] { "work", "office" });
            foreach (var product in workOfficeProducts)
            {
                Console.WriteLine($"  {product}");
            }
            Console.WriteLine();

            // Demonstrate removing a product
            Console.WriteLine("Removing 'Headphones' from catalog...");
            catalog.RemoveProduct("Headphones");
            Console.WriteLine($"Product removed. Total products: {catalog.GetProductCount()}\n");

            // Final product list
            Console.WriteLine("Final product list:");
            products = catalog.ListProductsByQuantity();
            foreach (var product in products)
            {
                Console.WriteLine($"  {product}");
            }
            Console.WriteLine();

            // Demonstrate error handling
            Console.WriteLine("=== Error Handling Demonstrations ===\n");

            // Try to add duplicate product
            try
            {
                catalog.AddProduct("Laptop", 5);
            }
            catch (ProductAlreadyExistsException ex)
            {
                Console.WriteLine($"Expected error: {ex.Message}");
            }

            // Try to purchase non-existent product
            try
            {
                catalog.PurchaseProduct("Phone", 1);
            }
            catch (ProductNotFoundException ex)
            {
                Console.WriteLine($"Expected error: {ex.Message}");
            }

            // Try to purchase more than available
            try
            {
                catalog.PurchaseProduct("Monitor", 20);
            }
            catch (InsufficientQuantityException ex)
            {
                Console.WriteLine($"Expected error: {ex.Message}");
            }

            // Try to remove non-existent product
            try
            {
                catalog.RemoveProduct("Tablet");
            }
            catch (ProductNotFoundException ex)
            {
                Console.WriteLine($"Expected error: {ex.Message}");
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message}");
        }

        // Display diagnostics report if available
        if (diagnosticsCollector != null)
        {
            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine(diagnosticsCollector.GetSummary());
            Console.WriteLine(new string('=', 60));
        }

        // Cleanup
        fileLogger?.Dispose();
        
        Console.WriteLine("\nDemo completed successfully!");
        if (diagnosticsCollector != null)
        {
            Console.WriteLine("Check the log files for detailed operation history.");
        }
        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
} 