using ProductCatalog.Exceptions;
using Xunit;

namespace ProductCatalog.Tests.Exceptions;

public class ExceptionTests
{
    [Fact]
    public void ProductNotFoundException_WithProductName_SetsCorrectMessage()
    {
        // Arrange
        var productName = "TestProduct";

        // Act
        var exception = new ProductNotFoundException(productName);

        // Assert
        Assert.Equal($"Product '{productName}' not found in catalog.", exception.Message);
    }

    [Fact]
    public void ProductAlreadyExistsException_WithProductName_SetsCorrectMessage()
    {
        // Arrange
        var productName = "TestProduct";

        // Act
        var exception = new ProductAlreadyExistsException(productName);

        // Assert
        Assert.Equal($"Product '{productName}' already exists in catalog.", exception.Message);
    }

    [Fact]
    public void InsufficientQuantityException_WithDetails_SetsCorrectMessage()
    {
        // Arrange
        var productName = "TestProduct";
        var availableQuantity = 5;
        var requestedQuantity = 10;

        // Act
        var exception = new InsufficientQuantityException(productName, availableQuantity, requestedQuantity);

        // Assert
        var expectedMessage = $"Cannot purchase {requestedQuantity} units of '{productName}'. Only {availableQuantity} units available.";
        Assert.Equal(expectedMessage, exception.Message);
    }

    [Fact]
    public void ProductCatalogException_WithMessage_SetsMessage()
    {
        // Arrange
        var message = "Test error message";

        // Act
        var exception = new ProductCatalogException(message);

        // Assert
        Assert.Equal(message, exception.Message);
    }

    [Fact]
    public void ProductCatalogException_WithMessageAndInnerException_SetsProperties()
    {
        // Arrange
        var message = "Test error message";
        var innerException = new InvalidOperationException("Inner exception");

        // Act
        var exception = new ProductCatalogException(message, innerException);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
    }

    [Fact]
    public void CustomExceptions_InheritFromProductCatalogException()
    {
        // Act & Assert
        Assert.IsAssignableFrom<ProductCatalogException>(new ProductNotFoundException("test"));
        Assert.IsAssignableFrom<ProductCatalogException>(new ProductAlreadyExistsException("test"));
        Assert.IsAssignableFrom<ProductCatalogException>(new InsufficientQuantityException("test", 1, 2));
    }

    [Fact]
    public void ProductCatalogException_InheritsFromException()
    {
        // Act & Assert
        Assert.IsAssignableFrom<Exception>(new ProductCatalogException("test"));
    }
} 