using ProductCatalog.Logging;
using ProductCatalog.Services;
using Xunit;

namespace ProductCatalog.Tests.Logging;

public class DiagnosticsCollectorTests
{
    [Fact]
    public void InitialState_ShouldHaveZeroMetrics()
    {
        // Arrange
        var collector = new DiagnosticsCollector();

        // Act
        var report = collector.GetReport();

        // Assert
        Assert.Equal(0, report.TotalOperations);
        Assert.Equal(0, report.TotalErrors);
        Assert.Empty(report.OperationCounts);
        Assert.Empty(report.ErrorCounts);
        Assert.Empty(report.ProductAccessCounts);
        Assert.Equal(0, report.QueryStats.TotalQueries);
    }

    [Fact]
    public void ProductOperations_ShouldBeTrackedCorrectly()
    {
        // Arrange
        var service = new ProductCatalogService();
        var collector = new DiagnosticsCollector();
        service.AddObserver(collector);

        // Act
        service.AddProduct("Product1", 10);
        service.AddProduct("Product2", 20);
        service.PurchaseProduct("Product1", 5);
        service.RemoveProduct("Product2");

        var report = collector.GetReport();

        // Assert
        Assert.Equal(4, report.TotalOperations);
        Assert.Equal(0, report.TotalErrors);
        Assert.Equal(2, report.OperationCounts["ProductAdded"]);
        Assert.Equal(1, report.OperationCounts["ProductPurchased"]);
        Assert.Equal(1, report.OperationCounts["ProductRemoved"]);
        
        // Check product access counts
        Assert.Equal(2, report.ProductAccessCounts["Product1"]); // Added + Purchased
        Assert.Equal(2, report.ProductAccessCounts["Product2"]); // Added + Removed
    }

    [Fact]
    public void QueryOperations_ShouldTrackPerformanceMetrics()
    {
        // Arrange
        var service = new ProductCatalogService();
        var collector = new DiagnosticsCollector();
        service.AddObserver(collector);

        // Add some products first
        service.AddProduct("Product1", 10);
        service.AddProduct("Product2", 20);

        // Act - Perform queries
        service.ListProductsByQuantity();
        service.GetProduct("Product1");
        service.ListProductsByQuantity();

        var report = collector.GetReport();

        // Assert
        Assert.Equal(3, report.QueryStats.TotalQueries);
        Assert.True(report.QueryStats.AverageExecutionTime >= 0);
        Assert.True(report.QueryStats.MinExecutionTime >= 0);
        Assert.True(report.QueryStats.MaxExecutionTime >= 0);
        Assert.Equal(3, report.OperationCounts["ProductsQueried"]); // 2 ListProductsByQuantity + 1 GetProduct
    }

    [Fact]
    public void GetSummary_ShouldReturnFormattedString()
    {
        // Arrange
        var service = new ProductCatalogService();
        var collector = new DiagnosticsCollector();
        service.AddObserver(collector);

        // Add some operations
        service.AddProduct("Product1", 10);
        service.ListProductsByQuantity();

        // Act
        var summary = collector.GetSummary();

        // Assert
        Assert.Contains("Product Catalog Diagnostics Summary", summary);
        Assert.Contains("Total Operations:", summary);
        Assert.Contains("Success Rate:", summary);
        Assert.Contains("Operation Counts:", summary);
    }

    [Fact]
    public void Reset_ShouldClearAllMetrics()
    {
        // Arrange
        var service = new ProductCatalogService();
        var collector = new DiagnosticsCollector();
        service.AddObserver(collector);

        // Add some operations
        service.AddProduct("Product1", 10);
        service.PurchaseProduct("Product1", 5);

        // Act
        collector.Reset();
        var report = collector.GetReport();

        // Assert
        Assert.Equal(0, report.TotalOperations);
        Assert.Equal(0, report.TotalErrors);
        Assert.Empty(report.OperationCounts);
        Assert.Empty(report.ErrorCounts);
        Assert.Empty(report.ProductAccessCounts);
        Assert.Equal(0, report.QueryStats.TotalQueries);
    }
}
