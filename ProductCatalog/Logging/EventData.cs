using ProductCatalog.Models;

namespace ProductCatalog.Logging;

/// <summary>
/// Base class for all event data
/// </summary>
public abstract class EventData
{
    public DateTime Timestamp { get; }
    public string OperationId { get; }

    protected EventData()
    {
        Timestamp = DateTime.UtcNow;
        OperationId = Guid.NewGuid().ToString("N")[..8];
    }
}

/// <summary>
/// Event data for product-related operations
/// </summary>
public class ProductEventData : EventData
{
    public string ProductName { get; }
    public int Quantity { get; }
    public string Operation { get; }

    public ProductEventData(string productName, int quantity, string operation)
    {
        ProductName = productName;
        Quantity = quantity;
        Operation = operation;
    }
}

/// <summary>
/// Event data for purchase operations
/// </summary>
public class PurchaseEventData : EventData
{
    public string ProductName { get; }
    public int PurchaseQuantity { get; }
    public int RemainingQuantity { get; }
    public int OriginalQuantity { get; }

    public PurchaseEventData(string productName, int purchaseQuantity, int remainingQuantity, int originalQuantity)
    {
        ProductName = productName;
        PurchaseQuantity = purchaseQuantity;
        RemainingQuantity = remainingQuantity;
        OriginalQuantity = originalQuantity;
    }
}

/// <summary>
/// Event data for error conditions
/// </summary>
public class ErrorEventData : EventData
{
    public string Operation { get; }
    public string ErrorMessage { get; }
    public string ExceptionType { get; }
    public string? ProductName { get; }

    public ErrorEventData(string operation, Exception exception, string? productName = null)
    {
        Operation = operation;
        ErrorMessage = exception.Message;
        ExceptionType = exception.GetType().Name;
        ProductName = productName;
    }
}

/// <summary>
/// Event data for query operations
/// </summary>
public class QueryEventData : EventData
{
    public string QueryType { get; }
    public int ResultCount { get; }
    public string? ProductName { get; }
    public TimeSpan ExecutionTime { get; }

    public QueryEventData(string queryType, int resultCount, TimeSpan executionTime, string? productName = null)
    {
        QueryType = queryType;
        ResultCount = resultCount;
        ExecutionTime = executionTime;
        ProductName = productName;
    }
} 