using System.Text.Json;

namespace ProductCatalog.Logging;

/// <summary>
/// File logger implementation of the observer pattern
/// </summary>
public class FileLogger : IProductCatalogObserver, IDisposable
{
    private readonly string _logFilePath;
    private readonly object _lock = new();
    private readonly StreamWriter _writer;
    private bool _disposed = false;

    public FileLogger(string? logFilePath = null)
    {
        _logFilePath = logFilePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ProductCatalog",
            "Logs",
            $"catalog_{DateTime.Now:yyyyMMdd}.log");

        // Ensure directory exists
        var directory = Path.GetDirectoryName(_logFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _writer = new StreamWriter(_logFilePath, append: true)
        {
            AutoFlush = true
        };
    }

    public void OnProductAdded(ProductEventData eventData)
    {
        var logEntry = new
        {
            Timestamp = eventData.Timestamp,
            OperationId = eventData.OperationId,
            Level = "INFO",
            Event = "ProductAdded",
            ProductName = eventData.ProductName,
            Quantity = eventData.Quantity,
            Operation = eventData.Operation
        };
        WriteLogEntry(logEntry);
    }

    public void OnProductRemoved(ProductEventData eventData)
    {
        var logEntry = new
        {
            Timestamp = eventData.Timestamp,
            OperationId = eventData.OperationId,
            Level = "INFO",
            Event = "ProductRemoved",
            ProductName = eventData.ProductName,
            Operation = eventData.Operation
        };
        WriteLogEntry(logEntry);
    }

    public void OnProductPurchased(PurchaseEventData eventData)
    {
        var logEntry = new
        {
            Timestamp = eventData.Timestamp,
            OperationId = eventData.OperationId,
            Level = "INFO",
            Event = "ProductPurchased",
            ProductName = eventData.ProductName,
            PurchaseQuantity = eventData.PurchaseQuantity,
            RemainingQuantity = eventData.RemainingQuantity,
            OriginalQuantity = eventData.OriginalQuantity
        };
        WriteLogEntry(logEntry);
    }

    public void OnOperationFailed(ErrorEventData eventData)
    {
        var logEntry = new
        {
            Timestamp = eventData.Timestamp,
            OperationId = eventData.OperationId,
            Level = "ERROR",
            Event = "OperationFailed",
            Operation = eventData.Operation,
            ErrorMessage = eventData.ErrorMessage,
            ExceptionType = eventData.ExceptionType,
            ProductName = eventData.ProductName
        };
        WriteLogEntry(logEntry);
    }

    public void OnProductsQueried(QueryEventData eventData)
    {
        var logEntry = new
        {
            Timestamp = eventData.Timestamp,
            OperationId = eventData.OperationId,
            Level = "DEBUG",
            Event = "ProductsQueried",
            QueryType = eventData.QueryType,
            ResultCount = eventData.ResultCount,
            ExecutionTimeMs = eventData.ExecutionTime.TotalMilliseconds,
            ProductName = eventData.ProductName
        };
        WriteLogEntry(logEntry);
    }

    private void WriteLogEntry(object logEntry)
    {
        if (_disposed) return;

        lock (_lock)
        {
            try
            {
                var json = JsonSerializer.Serialize(logEntry, new JsonSerializerOptions 
                { 
                    WriteIndented = false 
                });
                _writer.WriteLine(json);
            }
            catch (Exception ex)
            {
                // Fallback to simple text logging if JSON serialization fails
                _writer.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] ERROR: Failed to serialize log entry - {ex.Message}");
            }
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _writer?.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    ~FileLogger()
    {
        Dispose();
    }
} 