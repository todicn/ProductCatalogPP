using ProductCatalog.Exceptions;
using ProductCatalog.Services;

namespace ProductCatalog;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== Product Catalog Demo ===\n");

        var catalog = new ProductCatalogService();

        try
        {
            // Demonstrate adding products
            Console.WriteLine("Adding products to catalog...");
            catalog.AddProduct("Laptop", 10);
            catalog.AddProduct("Mouse", 25);
            catalog.AddProduct("Keyboard", 15);
            catalog.AddProduct("Monitor", 8);
            catalog.AddProduct("Headphones", 20);
            Console.WriteLine($"Added 5 products. Total products: {catalog.GetProductCount()}\n");

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

        Console.WriteLine("\nDemo completed successfully!");
    }
} 