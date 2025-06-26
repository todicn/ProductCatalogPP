using ProductCatalog.Models;
using Xunit;

namespace ProductCatalog.Tests.Models;

public class ProductTests
{
    [Fact]
    public void Constructor_ValidNameAndQuantity_CreatesProduct()
    {
        // Arrange
        var name = "Test Product";
        var quantity = 10;

        // Act
        var product = new Product(name, quantity);

        // Assert
        Assert.Equal(name, product.Name);
        Assert.Equal(quantity, product.Quantity);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidName_ThrowsArgumentException(string invalidName)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Product(invalidName, 10));
    }

    [Fact]
    public void Constructor_NegativeQuantity_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Product("Test", -1));
    }

    [Fact]
    public void Constructor_TrimsWhitespace()
    {
        // Arrange
        var nameWithWhitespace = "  Product Name  ";

        // Act
        var product = new Product(nameWithWhitespace, 5);

        // Assert
        Assert.Equal("Product Name", product.Name);
    }

    [Fact]
    public void ToString_ReturnsCorrectFormat()
    {
        // Arrange
        var product = new Product("Test Product", 15);

        // Act
        var result = product.ToString();

        // Assert
        Assert.Equal("Test Product: 15 units", result);
    }

    [Fact]
    public void Equals_SameNameDifferentCase_ReturnsTrue()
    {
        // Arrange
        var product1 = new Product("Product", 10);
        var product2 = new Product("PRODUCT", 20);

        // Act & Assert
        Assert.True(product1.Equals(product2));
    }

    [Fact]
    public void Equals_DifferentNames_ReturnsFalse()
    {
        // Arrange
        var product1 = new Product("Product1", 10);
        var product2 = new Product("Product2", 10);

        // Act & Assert
        Assert.False(product1.Equals(product2));
    }

    [Fact]
    public void Equals_NonProductObject_ReturnsFalse()
    {
        // Arrange
        var product = new Product("Product", 10);

        // Act & Assert
        Assert.False(product.Equals("Not a product"));
    }

    [Fact]
    public void GetHashCode_SameNameDifferentCase_ReturnsSameHash()
    {
        // Arrange
        var product1 = new Product("Product", 10);
        var product2 = new Product("PRODUCT", 20);

        // Act & Assert
        Assert.Equal(product1.GetHashCode(), product2.GetHashCode());
    }
} 