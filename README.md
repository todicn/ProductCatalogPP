# Product Catalog Application

A C# console application that manages a product catalog with comprehensive error handling and unit tests.

## Features

- **Add Product**: Add new products with name and initial quantity
- **Remove Product**: Remove products from the catalog
- **Purchase Product**: Decrease product quantity when purchased
- **List Products**: Display all products sorted by quantity (descending order)
- **Error Handling**: Robust error handling with custom exceptions
- **Unit Tests**: Comprehensive test coverage using xUnit

## Project Structure

```
ProductCatalogPP1/
├── ProductCatalog/                    # Main application
│   ├── Models/
│   │   └── Product.cs                 # Product model class
│   ├── Services/
│   │   ├── IProductCatalogService.cs  # Service interface
│   │   └── ProductCatalogService.cs   # Service implementation
│   ├── Exceptions/
│   │   └── ProductCatalogException.cs # Custom exceptions
│   ├── Program.cs                     # Main entry point
│   └── ProductCatalog.csproj         # Project file
├── ProductCatalog.Tests/              # Unit tests
│   ├── Models/
│   │   └── ProductTests.cs           # Product model tests
│   ├── Services/
│   │   └── ProductCatalogServiceTests.cs # Service tests
│   ├── Exceptions/
│   │   └── ExceptionTests.cs         # Exception tests
│   └── ProductCatalog.Tests.csproj   # Test project file
├── ProductCatalog.sln                 # Solution file
└── README.md                          # This file
```

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- Visual Studio 2022, Visual Studio Code, or any C# compatible IDE

### Building the Solution

1. Clone or download the project
2. Open a terminal/command prompt in the project root directory
3. Build the solution:

```bash
dotnet build
```

### Running the Application

To run the console application:

```bash
dotnet run --project ProductCatalog
```

### Running Unit Tests

To execute all unit tests:

```bash
dotnet test
```

To run tests with detailed output:

```bash
dotnet test --verbosity normal
```

To generate code coverage report (if you have coverage tools installed):

```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Usage Examples

### Basic Operations

```csharp
var catalog = new ProductCatalogService();

// Add products
catalog.AddProduct("Laptop", 10);
catalog.AddProduct("Mouse", 25);
catalog.AddProduct("Keyboard", 15);

// Purchase products
catalog.PurchaseProduct("Mouse", 5);  // Decreases quantity by 5

// List products by quantity (descending)
var products = catalog.ListProductsByQuantity();
foreach (var product in products)
{
    Console.WriteLine(product); // Output: "Mouse: 20 units"
}

// Remove products
catalog.RemoveProduct("Keyboard");

// Get specific product
var laptop = catalog.GetProduct("Laptop");
Console.WriteLine($"Laptop quantity: {laptop.Quantity}");
```

### Error Handling

The application handles various error scenarios:

- **ProductNotFoundException**: Thrown when trying to access a non-existent product
- **ProductAlreadyExistsException**: Thrown when adding a product that already exists
- **InsufficientQuantityException**: Thrown when trying to purchase more than available
- **ArgumentException**: Thrown for invalid inputs (null names, negative quantities, etc.)

## API Reference

### IProductCatalogService Interface

#### Methods

- `void AddProduct(string name, int quantity)`: Adds a new product
- `void RemoveProduct(string name)`: Removes an existing product
- `void PurchaseProduct(string name, int quantity)`: Purchases product (decreases quantity)
- `IReadOnlyList<Product> ListProductsByQuantity()`: Returns products sorted by quantity (desc)
- `Product GetProduct(string name)`: Gets a specific product by name
- `int GetProductCount()`: Returns total number of products

### Product Class

#### Properties

- `string Name`: Product name (trimmed, case-insensitive for comparisons)
- `int Quantity`: Current quantity in stock

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
- **Architecture**: Service-oriented with dependency injection support
- **Error Handling**: Custom exception hierarchy
- **Thread Safety**: Not thread-safe (single-threaded usage assumed)
- **Storage**: In-memory dictionary (no persistence)

## Future Enhancements

Potential improvements that could be added:

- Persistence layer (database or file storage)
- Thread-safety for concurrent operations
- Product categories and advanced filtering
- Price tracking and inventory valuation
- REST API endpoints
- Audit logging for all operations
- Bulk operations (add/remove multiple products)
- Low stock alerts and inventory management 