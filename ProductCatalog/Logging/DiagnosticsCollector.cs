using System.Collections.Concurrent;
using System.Text;

namespace ProductCatalog.Logging;

/// <summary>
/// Diagnostics collector that aggregates metrics and statistics
/// </summary>
public class DiagnosticsCollector : IProductCatalogObserver
{
    private readonly ConcurrentDictionary<string, long> _operationCounts = new();
    private readonly ConcurrentDictionary<string, long> _errorCounts = new();
    private readonly ConcurrentBag<double> _queryExecutionTimes = new();
    private readonly ConcurrentDictionary<string, int> _productAccessCounts = new();
    private readonly object _lock = new();

    private long _totalOperations = 0;
    private long _totalErrors = 0;
    private DateTime _startTime = DateTime.UtcNow;

    public void OnProductAdded(ProductEventData eventData)
    {
        Interlocked.Increment(ref _totalOperations);
        _operationCounts.AddOrUpdate("ProductAdded", 1, (key, oldValue) => oldValue + 1);
        _productAccessCounts.AddOrUpdate(eventData.ProductName, 1, (key, oldValue) => oldValue + 1);
    }

    public void OnProductRemoved(ProductEventData eventData)
    {
        Interlocked.Increment(ref _totalOperations);
        _operationCounts.AddOrUpdate("ProductRemoved", 1, (key, oldValue) => oldValue + 1);
        _productAccessCounts.AddOrUpdate(eventData.ProductName, 1, (key, oldValue) => oldValue + 1);
    }

    public void OnProductPurchased(PurchaseEventData eventData)
    {
        Interlocked.Increment(ref _totalOperations);
        _operationCounts.AddOrUpdate("ProductPurchased", 1, (key, oldValue) => oldValue + 1);
        _productAccessCounts.AddOrUpdate(eventData.ProductName, 1, (key, oldValue) => oldValue + 1);
    }

    public void OnOperationFailed(ErrorEventData eventData)
    {
        Interlocked.Increment(ref _totalErrors);
        _errorCounts.AddOrUpdate(eventData.ExceptionType, 1, (key, oldValue) => oldValue + 1);
        _operationCounts.AddOrUpdate($"Failed_{eventData.Operation}", 1, (key, oldValue) => oldValue + 1);
        
        if (!string.IsNullOrEmpty(eventData.ProductName))
        {
            _productAccessCounts.AddOrUpdate(eventData.ProductName, 1, (key, oldValue) => oldValue + 1);
        }
    }

    public void OnProductsQueried(QueryEventData eventData)
    {
        Interlocked.Increment(ref _totalOperations);
        _operationCounts.AddOrUpdate("ProductsQueried", 1, (key, oldValue) => oldValue + 1);
        _queryExecutionTimes.Add(eventData.ExecutionTime.TotalMilliseconds);
        
        if (!string.IsNullOrEmpty(eventData.ProductName))
        {
            _productAccessCounts.AddOrUpdate(eventData.ProductName, 1, (key, oldValue) => oldValue + 1);
        }
    }

    /// <summary>
    /// Gets comprehensive diagnostics report
    /// </summary>
    public DiagnosticsReport GetReport()
    {
        lock (_lock)
        {
            var uptime = DateTime.UtcNow - _startTime;
            var queryTimes = _queryExecutionTimes.ToArray();
            
            return new DiagnosticsReport
            {
                StartTime = _startTime,
                Uptime = uptime,
                TotalOperations = _totalOperations,
                TotalErrors = _totalErrors,
                OperationCounts = new Dictionary<string, long>(_operationCounts),
                ErrorCounts = new Dictionary<string, long>(_errorCounts),
                ProductAccessCounts = new Dictionary<string, int>(_productAccessCounts),
                QueryStats = new QueryStatistics
                {
                    TotalQueries = queryTimes.Length,
                    AverageExecutionTime = queryTimes.Length > 0 ? queryTimes.Average() : 0,
                    MinExecutionTime = queryTimes.Length > 0 ? queryTimes.Min() : 0,
                    MaxExecutionTime = queryTimes.Length > 0 ? queryTimes.Max() : 0,
                    MedianExecutionTime = CalculateMedian(queryTimes)
                }
            };
        }
    }

    /// <summary>
    /// Resets all collected metrics
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _operationCounts.Clear();
            _errorCounts.Clear();
            _queryExecutionTimes.Clear();
            _productAccessCounts.Clear();
            _totalOperations = 0;
            _totalErrors = 0;
            _startTime = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Gets a formatted summary of diagnostics
    /// </summary>
    public string GetSummary()
    {
        var report = GetReport();
        var sb = new StringBuilder();

        sb.AppendLine("=== Product Catalog Diagnostics Summary ===");
        sb.AppendLine($"Uptime: {report.Uptime:dd\\.hh\\:mm\\:ss}");
        sb.AppendLine($"Total Operations: {report.TotalOperations:N0}");
        sb.AppendLine($"Total Errors: {report.TotalErrors:N0}");
        sb.AppendLine($"Success Rate: {(report.TotalOperations > 0 ? (1.0 - (double)report.TotalErrors / report.TotalOperations) * 100 : 100):F2}%");
        sb.AppendLine();

        if (report.OperationCounts.Any())
        {
            sb.AppendLine("Operation Counts:");
            foreach (var kvp in report.OperationCounts.OrderByDescending(x => x.Value))
            {
                sb.AppendLine($"  {kvp.Key}: {kvp.Value:N0}");
            }
            sb.AppendLine();
        }

        if (report.ErrorCounts.Any())
        {
            sb.AppendLine("Error Types:");
            foreach (var kvp in report.ErrorCounts.OrderByDescending(x => x.Value))
            {
                sb.AppendLine($"  {kvp.Key}: {kvp.Value:N0}");
            }
            sb.AppendLine();
        }

        if (report.QueryStats.TotalQueries > 0)
        {
            sb.AppendLine("Query Performance:");
            sb.AppendLine($"  Total Queries: {report.QueryStats.TotalQueries:N0}");
            sb.AppendLine($"  Average Time: {report.QueryStats.AverageExecutionTime:F2}ms");
            sb.AppendLine($"  Min Time: {report.QueryStats.MinExecutionTime:F2}ms");
            sb.AppendLine($"  Max Time: {report.QueryStats.MaxExecutionTime:F2}ms");
            sb.AppendLine($"  Median Time: {report.QueryStats.MedianExecutionTime:F2}ms");
            sb.AppendLine();
        }

        if (report.ProductAccessCounts.Any())
        {
            sb.AppendLine("Top Accessed Products:");
            foreach (var kvp in report.ProductAccessCounts.OrderByDescending(x => x.Value).Take(10))
            {
                sb.AppendLine($"  {kvp.Key}: {kvp.Value:N0} accesses");
            }
        }

        return sb.ToString();
    }

    private static double CalculateMedian(double[] values)
    {
        if (values.Length == 0) return 0;
        
        var sorted = values.OrderBy(x => x).ToArray();
        var mid = sorted.Length / 2;
        
        return sorted.Length % 2 == 0 
            ? (sorted[mid - 1] + sorted[mid]) / 2.0 
            : sorted[mid];
    }
}

/// <summary>
/// Comprehensive diagnostics report
/// </summary>
public class DiagnosticsReport
{
    public DateTime StartTime { get; set; }
    public TimeSpan Uptime { get; set; }
    public long TotalOperations { get; set; }
    public long TotalErrors { get; set; }
    public Dictionary<string, long> OperationCounts { get; set; } = new();
    public Dictionary<string, long> ErrorCounts { get; set; } = new();
    public Dictionary<string, int> ProductAccessCounts { get; set; } = new();
    public QueryStatistics QueryStats { get; set; } = new();
}

/// <summary>
/// Query performance statistics
/// </summary>
public class QueryStatistics
{
    public int TotalQueries { get; set; }
    public double AverageExecutionTime { get; set; }
    public double MinExecutionTime { get; set; }
    public double MaxExecutionTime { get; set; }
    public double MedianExecutionTime { get; set; }
} 