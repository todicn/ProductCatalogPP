namespace ProductCatalog.Exceptions;

/// <summary>
/// Base exception for product catalog operations
/// </summary>
public class ProductCatalogException : Exception
{
    public ProductCatalogException(string message) : base(message) { }
    public ProductCatalogException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when a product is not found
/// </summary>
public class ProductNotFoundException : ProductCatalogException
{
    public ProductNotFoundException(string productName) 
        : base($"Product '{productName}' not found in catalog.") { }
}

/// <summary>
/// Exception thrown when trying to add a product that already exists
/// </summary>
public class ProductAlreadyExistsException : ProductCatalogException
{
    public ProductAlreadyExistsException(string productName) 
        : base($"Product '{productName}' already exists in catalog.") { }
}

/// <summary>
/// Exception thrown when trying to purchase more items than available
/// </summary>
public class InsufficientQuantityException : ProductCatalogException
{
    public InsufficientQuantityException(string productName, int availableQuantity, int requestedQuantity) 
        : base($"Cannot purchase {requestedQuantity} units of '{productName}'. Only {availableQuantity} units available.") { }
} 