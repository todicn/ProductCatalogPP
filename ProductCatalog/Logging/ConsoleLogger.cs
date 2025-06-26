using System.Text;

namespace ProductCatalog.Logging;

/// <summary>
/// Console logger implementation of the observer pattern
/// </summary>
public class ConsoleLogger : IProductCatalogObserver
{
    private readonly bool _useColors;
    private readonly object _lock = new();

    public ConsoleLogger(bool useColors = true)
    {
        _useColors = useColors;
    }

    public void OnProductAdded(ProductEventData eventData)
    {
        var message = $"[{eventData.Timestamp:HH:mm:ss.fff}] [{eventData.OperationId}] PRODUCT ADDED: '{eventData.ProductName}' with quantity {eventData.Quantity}";
        WriteColoredLine(message, ConsoleColor.Green);
    }

    public void OnProductRemoved(ProductEventData eventData)
    {
        var message = $"[{eventData.Timestamp:HH:mm:ss.fff}] [{eventData.OperationId}] PRODUCT REMOVED: '{eventData.ProductName}'";
        WriteColoredLine(message, ConsoleColor.Yellow);
    }

    public void OnProductPurchased(PurchaseEventData eventData)
    {
        var message = $"[{eventData.Timestamp:HH:mm:ss.fff}] [{eventData.OperationId}] PURCHASE: '{eventData.ProductName}' " +
                     $"quantity {eventData.PurchaseQuantity} (was {eventData.OriginalQuantity}, now {eventData.RemainingQuantity})";
        WriteColoredLine(message, ConsoleColor.Blue);
    }

    public void OnOperationFailed(ErrorEventData eventData)
    {
        var productInfo = eventData.ProductName != null ? $" for product '{eventData.ProductName}'" : "";
        var message = $"[{eventData.Timestamp:HH:mm:ss.fff}] [{eventData.OperationId}] ERROR in {eventData.Operation}{productInfo}: " +
                     $"{eventData.ExceptionType} - {eventData.ErrorMessage}";
        WriteColoredLine(message, ConsoleColor.Red);
    }

    public void OnProductsQueried(QueryEventData eventData)
    {
        var productInfo = eventData.ProductName != null ? $" for '{eventData.ProductName}'" : "";
        var message = $"[{eventData.Timestamp:HH:mm:ss.fff}] [{eventData.OperationId}] QUERY: {eventData.QueryType}{productInfo} " +
                     $"returned {eventData.ResultCount} result(s) in {eventData.ExecutionTime.TotalMilliseconds:F2}ms";
        WriteColoredLine(message, ConsoleColor.Cyan);
    }

    private void WriteColoredLine(string message, ConsoleColor color)
    {
        lock (_lock)
        {
            if (_useColors)
            {
                var originalColor = Console.ForegroundColor;
                Console.ForegroundColor = color;
                Console.WriteLine(message);
                Console.ForegroundColor = originalColor;
            }
            else
            {
                Console.WriteLine(message);
            }
        }
    }
} 