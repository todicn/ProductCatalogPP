# Product Catalog Application

A C# console application that manages a product catalog with comprehensive error handling, unit tests, and **multiple storage backends using the Factory Pattern**.

## Features

- **Add Product**: Add new products with name, quantity, category, and tags
- **Remove Product**: Remove products from the catalog
- **Purchase Product**: Decrease product quantity when purchased
- **List Products**: Display all products sorted by quantity (descending order)
- **Search Products**: Search by category or tags
- **Multiple Storage Options**: In-memory, Cosmos DB, or Redis storage
- **Factory Pattern**: Clean architecture with pluggable storage backends
- **Error Handling**: Robust error handling with custom exceptions
- **Unit Tests**: Comprehensive test coverage using xUnit

## Storage Options

The application supports three storage backends through the Factory Pattern:

1. **In-Memory Storage** (Default) - Fast, no external dependencies
2. **Cosmos DB Storage** - Scalable NoSQL database (local emulator supported)
3. **Redis Storage** - High-performance caching solution

For detailed setup instructions, see [STORAGE_CONFIGURATION_GUIDE.md](STORAGE_CONFIGURATION_GUIDE.md).

## Project Structure

```
ProductCatalogPP1/
├── ProductCatalog/                           # Main application
│   ├── Models/
│   │   └── Product.cs                        # Product model class
│   ├── Services/
│   │   ├── IProductCatalogService.cs         # Service interface
│   │   ├── ProductCatalogService.cs          # In-memory implementation
│   │   ├── CosmosDbProductCatalogService.cs  # Cosmos DB implementation
│   │   ├── RedisProductCatalogService.cs     # Redis implementation
│   │   ├── IProductCatalogServiceFactory.cs  # Factory interface
│   │   ├── ProductCatalogServiceFactory.cs   # Factory implementation
│   │   ├── StorageConfiguration.cs           # Configuration models
│   │   └── StorageConfigurationHelper.cs     # Configuration helpers
│   ├── Exceptions/
│   │   └── ProductCatalogException.cs        # Custom exceptions
│   ├── Program.cs                            # Main entry point with storage selection
│   └── ProductCatalog.csproj                # Project file
├── ProductCatalog.Tests/                     # Unit tests
│   ├── Models/
│   │   └── ProductTests.cs                  # Product model tests
│   ├── Services/
│   │   └── ProductCatalogServiceTests.cs # Service tests
│   ├── Exceptions/
│   │   └── ExceptionTests.cs         # Exception tests
│   └── ProductCatalog.Tests.csproj   # Test project file
├── ProductCatalog.sln                 # Solution file
├── initialize-local-storage.ps1       # PowerShell script for local storage setup
├── STORAGE_CONFIGURATION_GUIDE.md     # Detailed storage configuration guide
└── README.md                          # This file
```

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- Visual Studio 2022, Visual Studio Code, or any C# compatible IDE

**For Cosmos DB Development (Optional):**
- Azure Cosmos DB Emulator

**For Redis Development (Optional):**
- Redis server (local installation or WSL)

### Building the Solution

1. Clone or download the project
2. Open a terminal/command prompt in the project root directory
3. Restore dependencies and build the solution:

```bash
dotnet restore
dotnet build
```

### Setting Up Local Storage (Optional)

If you want to test with Cosmos DB or Redis, you have several options for running the initialization script:

#### Option 1: Full Featured Script (VS Code Terminal Recommended)
```powershell
.\initialize-local-storage.ps1
```

#### Option 2: Simplified Script (External PowerShell)
```powershell
.\init-simple.ps1
```

#### Option 3: Batch Wrapper (Command Prompt)
```cmd
initialize-local-storage.bat
```

#### Option 4: Execution Policy Bypass
```powershell
powershell -ExecutionPolicy Bypass -File "initialize-local-storage.ps1"
```

**Note:** If you encounter issues running scripts outside VS Code, use the simplified script (`init-simple.ps1`) or see [TROUBLESHOOTING.md](TROUBLESHOOTING.md) for solutions.

These scripts will:
- Wait for Cosmos DB Emulator to be ready
- Initialize the database and container
- Wait for Redis to be ready
- Configure Redis for the application

### Running the Application

To run the console application:

```bash
dotnet run --project ProductCatalog
```

The application will prompt you to choose a storage backend:
1. In-Memory (default, no setup required)
2. Cosmos DB (requires emulator or cloud instance)
3. Redis (requires Redis server)

### Running Unit Tests

To execute all unit tests:

```bash
dotnet test
```

To run tests with detailed output:

```bash
dotnet test --verbosity normal
```

## Usage Examples

### Factory Pattern Usage

```csharp
// Create configuration for different storage types
var inMemoryConfig = StorageConfigurationHelper.CreateInMemoryConfiguration();
var cosmosConfig = StorageConfigurationHelper.CreateCosmosDbConfiguration();
var redisConfig = StorageConfigurationHelper.CreateRedisConfiguration();

// Create factory and service
var factory = new ProductCatalogServiceFactory(inMemoryConfig);
var catalog = factory.CreateService();
```

### Basic Operations

```csharp
// Add products with categories and tags
catalog.AddProduct("Laptop", 10, "Electronics", new[] { "computer", "portable", "work" });
catalog.AddProduct("Mouse", 25, "Electronics", new[] { "computer", "wireless", "gaming" });

// Purchase products
catalog.PurchaseProduct("Mouse", 5);  // Decreases quantity by 5

// Search by category
var electronics = catalog.SearchByCategory("Electronics");

// Search by tags
var gamingProducts = catalog.SearchByTag("gaming");
var workProducts = catalog.SearchByTags(new[] { "work", "office" });

// List products by quantity (descending)
var products = catalog.ListProductsByQuantity();
foreach (var product in products)
{
    Console.WriteLine(product);
}
```

### Environment Variable Configuration

```bash
# Set storage type
export PRODUCT_CATALOG_STORAGE_TYPE=CosmosDb

# Configure Cosmos DB
export COSMOS_DB_DATABASE_NAME=ProductCatalogDB
export COSMOS_DB_CONTAINER_NAME=Products

# Use environment configuration
var config = StorageConfigurationHelper.CreateFromEnvironment();
```

### Error Handling

The application handles various error scenarios:

- **ProductNotFoundException**: Thrown when trying to access a non-existent product
- **ProductAlreadyExistsException**: Thrown when adding a product that already exists
- **InsufficientQuantityException**: Thrown when trying to purchase more than available
- **ArgumentException**: Thrown for invalid inputs (null names, negative quantities, etc.)

### API Reference

### IProductCatalogService Interface

#### Methods

- `void AddProduct(string name, int quantity, string? category = null, IEnumerable<string>? tags = null)`: Adds a new product with optional category and tags
- `void RemoveProduct(string name)`: Removes an existing product
- `void PurchaseProduct(string name, int quantity)`: Purchases product (decreases quantity)
- `IReadOnlyList<Product> ListProductsByQuantity()`: Returns products sorted by quantity (desc)
- `Product GetProduct(string name)`: Gets a specific product by name
- `int GetProductCount()`: Returns total number of products
- `IReadOnlyList<Product> SearchByCategory(string category)`: Search products by category
- `IReadOnlyList<Product> SearchByTag(string tag)`: Search products by tag
- `IReadOnlyList<Product> SearchByTags(IEnumerable<string> tags)`: Search products by multiple tags
- `IReadOnlyList<string> GetAllCategories()`: Gets all unique categories
- `IReadOnlyList<string> GetAllTags()`: Gets all unique tags

### Factory Pattern

#### IProductCatalogServiceFactory

- `IProductCatalogService CreateService()`: Creates appropriate service instance based on configuration

#### StorageConfigurationHelper

- `StorageConfiguration CreateInMemoryConfiguration()`: Creates in-memory config
- `StorageConfiguration CreateCosmosDbConfiguration()`: Creates Cosmos DB config (local emulator)
- `StorageConfiguration CreateRedisConfiguration()`: Creates Redis config (localhost)
- `StorageConfiguration CreateFromEnvironment()`: Creates config from environment variables

### Product Class

#### Properties

- `string Name`: Product name (trimmed, case-insensitive for comparisons)
- `int Quantity`: Current quantity in stock
- `string Category`: Product category (defaults to "General")
- `ISet<string> Tags`: Set of product tags

#### Methods

- `ToString()`: Returns formatted string representation
- `Equals(object obj)`: Case-insensitive name comparison
- `GetHashCode()`: Case-insensitive hash code

## Testing

The project includes comprehensive unit tests covering:

- **Model Tests**: Product class validation and behavior
- **Service Tests**: All CRUD operations and edge cases
- **Exception Tests**: Custom exception behavior
- **Integration Tests**: Complex scenarios with multiple operations

### Test Categories

1. **Positive Tests**: Valid operations that should succeed
2. **Negative Tests**: Invalid operations that should throw exceptions
3. **Edge Cases**: Boundary conditions and special scenarios
4. **Integration Tests**: Real-world usage patterns

## Technical Details

- **Target Framework**: .NET 8.0
- **Testing Framework**: xUnit
- **Architecture**: Factory Pattern with pluggable storage backends
- **Storage Options**: In-Memory, Cosmos DB, Redis
- **Azure SDK**: Latest Cosmos DB SDK with managed identity support
- **Error Handling**: Custom exception hierarchy with proper fallback
- **Thread Safety**: Service implementations are designed for single-threaded usage
- **Dependencies**: 
  - Microsoft.Azure.Cosmos (3.39.0)
  - StackExchange.Redis (2.7.33)
  - System.Text.Json (8.0.4)

## Storage Performance Comparison

| Storage Type | Read Speed | Write Speed | Scalability | Persistence | Setup Complexity |
|-------------|------------|-------------|-------------|-------------|------------------|
| In-Memory   | Excellent  | Excellent   | Limited     | None        | None             |
| Cosmos DB   | Good       | Good        | Excellent   | Excellent   | Medium           |
| Redis       | Excellent  | Excellent   | Good        | Good        | Low              |

## Future Enhancements

Potential improvements that could be added:

- **Additional Storage Backends**: SQL Server, MongoDB, Azure Table Storage
- **Async Operations**: Full async/await pattern support
- **Thread Safety**: Concurrent access support with proper locking
- **Caching Layer**: Hybrid storage with caching for better performance
- **Event Sourcing**: Track all changes for audit and replay capabilities
- **REST API**: Web API endpoints with Swagger documentation
- **Dependency Injection**: Full DI container integration
- **Configuration**: appsettings.json support with options pattern
- **Metrics and Monitoring**: Application Insights integration
- **Health Checks**: Storage backend health monitoring 