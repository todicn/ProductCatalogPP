using ProductCatalog.Logging;
using ProductCatalog.Services;
using Xunit;

namespace ProductCatalog.Tests.Logging;

public class ObserverPatternTests
{
    [Fact]
    public void AddObserver_ShouldRegisterObserver()
    {
        // Arrange
        var service = new ProductCatalogService();
        var observer = new TestObserver();

        // Act
        service.AddObserver(observer);
        service.AddProduct("Test Product", 10);

        // Assert
        Assert.True(observer.ProductAddedCalled);
        Assert.Equal("Test Product", observer.LastProductName);
    }

    [Fact]
    public void RemoveObserver_ShouldUnregisterObserver()
    {
        // Arrange
        var service = new ProductCatalogService();
        var observer = new TestObserver();
        service.AddObserver(observer);

        // Act
        service.RemoveObserver(observer);
        service.AddProduct("Test Product", 10);

        // Assert
        Assert.False(observer.ProductAddedCalled);
    }

    [Fact]
    public void AddObserver_NullObserver_ShouldThrowArgumentNullException()
    {
        // Arrange
        var service = new ProductCatalogService();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service.AddObserver(null!));
    }

    [Fact]
    public void ObserverNotifications_ShouldNotAffectMainOperation()
    {
        // Arrange
        var service = new ProductCatalogService();
        var faultyObserver = new FaultyObserver();
        service.AddObserver(faultyObserver);

        // Act & Assert - Operation should succeed despite observer failure
        service.AddProduct("Test Product", 10);
        Assert.Equal(1, service.GetProductCount());
    }

    [Fact]
    public void MultipleObservers_ShouldAllReceiveNotifications()
    {
        // Arrange
        var service = new ProductCatalogService();
        var observer1 = new TestObserver();
        var observer2 = new TestObserver();
        
        service.AddObserver(observer1);
        service.AddObserver(observer2);

        // Act
        service.AddProduct("Test Product", 10);

        // Assert
        Assert.True(observer1.ProductAddedCalled);
        Assert.True(observer2.ProductAddedCalled);
    }

    [Fact]
    public void AllOperations_ShouldTriggerAppropriateEvents()
    {
        // Arrange
        var service = new ProductCatalogService();
        var observer = new TestObserver();
        service.AddObserver(observer);

        // Act - Test all operations
        service.AddProduct("Test Product", 10);
        service.PurchaseProduct("Test Product", 3);
        service.GetProduct("Test Product");
        service.ListProductsByQuantity();
        service.RemoveProduct("Test Product");

        // Assert
        Assert.True(observer.ProductAddedCalled);
        Assert.True(observer.ProductPurchasedCalled);
        Assert.True(observer.ProductsQueriedCalled);
        Assert.True(observer.ProductRemovedCalled);
        Assert.Equal(2, observer.QueryCount); // GetProduct + ListProductsByQuantity
    }

    [Fact]
    public void ErrorOperations_ShouldTriggerErrorEvents()
    {
        // Arrange
        var service = new ProductCatalogService();
        var observer = new TestObserver();
        service.AddObserver(observer);

        // Act - Test error scenarios
        try { service.AddProduct("Test", 10); } catch { }
        try { service.AddProduct("Test", 5); } catch { } // Duplicate
        try { service.PurchaseProduct("NonExistent", 1); } catch { }
        try { service.RemoveProduct("NonExistent"); } catch { }

        // Assert
        Assert.Equal(3, observer.ErrorCount); // 3 operations should fail
    }
}

// Test observer implementation
public class TestObserver : IProductCatalogObserver
{
    public bool ProductAddedCalled { get; private set; }
    public bool ProductRemovedCalled { get; private set; }
    public bool ProductPurchasedCalled { get; private set; }
    public bool ProductsQueriedCalled { get; private set; }
    public string? LastProductName { get; private set; }
    public int QueryCount { get; private set; }
    public int ErrorCount { get; private set; }

    public void OnProductAdded(ProductEventData eventData)
    {
        ProductAddedCalled = true;
        LastProductName = eventData.ProductName;
    }

    public void OnProductRemoved(ProductEventData eventData)
    {
        ProductRemovedCalled = true;
        LastProductName = eventData.ProductName;
    }

    public void OnProductPurchased(PurchaseEventData eventData)
    {
        ProductPurchasedCalled = true;
        LastProductName = eventData.ProductName;
    }

    public void OnOperationFailed(ErrorEventData eventData)
    {
        ErrorCount++;
    }

    public void OnProductsQueried(QueryEventData eventData)
    {
        ProductsQueriedCalled = true;
        QueryCount++;
    }
}

// Observer that throws exceptions to test error handling
public class FaultyObserver : IProductCatalogObserver
{
    public void OnProductAdded(ProductEventData eventData)
    {
        throw new InvalidOperationException("Test exception from observer");
    }

    public void OnProductRemoved(ProductEventData eventData)
    {
        throw new InvalidOperationException("Test exception from observer");
    }

    public void OnProductPurchased(PurchaseEventData eventData)
    {
        throw new InvalidOperationException("Test exception from observer");
    }

    public void OnOperationFailed(ErrorEventData eventData)
    {
        throw new InvalidOperationException("Test exception from observer");
    }

    public void OnProductsQueried(QueryEventData eventData)
    {
        throw new InvalidOperationException("Test exception from observer");
    }
} 