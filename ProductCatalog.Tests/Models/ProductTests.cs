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
        Assert.Equal("General", product.Category);
        Assert.Empty(product.Tags);
    }

    [Fact]
    public void Constructor_WithCategoryAndTags_CreatesProductCorrectly()
    {
        // Arrange
        var name = "Test Product";
        var quantity = 10;
        var category = "Electronics";
        var tags = new[] { "computer", "work", "portable" };

        // Act
        var product = new Product(name, quantity, category, tags);

        // Assert
        Assert.Equal(name, product.Name);
        Assert.Equal(quantity, product.Quantity);
        Assert.Equal(category, product.Category);
        Assert.Equal(3, product.Tags.Count);
        Assert.Contains("computer", product.Tags);
        Assert.Contains("work", product.Tags);
        Assert.Contains("portable", product.Tags);
    }

    [Fact]
    public void Constructor_EmptyCategory_DefaultsToGeneral()
    {
        // Act
        var product = new Product("Test", 10, "");

        // Assert
        Assert.Equal("General", product.Category);
    }

    [Fact]
    public void Constructor_NullCategory_DefaultsToGeneral()
    {
        // Act
        var product = new Product("Test", 10, null);

        // Assert
        Assert.Equal("General", product.Category);
    }

    [Fact]
    public void Constructor_EmptyAndNullTags_FiltersOut()
    {
        // Arrange
        var tags = new[] { "valid", "", "  ", null, "another" };

        // Act
        var product = new Product("Test", 10, "Category", tags);

        // Assert
        Assert.Equal(2, product.Tags.Count);
        Assert.Contains("valid", product.Tags);
        Assert.Contains("another", product.Tags);
    }

    [Fact]
    public void Tags_CaseInsensitive_NoDuplicates()
    {
        // Arrange
        var tags = new[] { "Gaming", "GAMING", "gaming", "Work" };

        // Act
        var product = new Product("Test", 10, "Category", tags);

        // Assert
        Assert.Equal(2, product.Tags.Count);
        Assert.Contains("Gaming", product.Tags);
        Assert.Contains("Work", product.Tags);
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
    public void ToString_WithoutTags_ReturnsCorrectFormat()
    {
        // Arrange
        var product = new Product("Test Product", 15, "Electronics");

        // Act
        var result = product.ToString();

        // Assert
        Assert.Equal("Test Product: 15 units (Category: Electronics)", result);
    }

    [Fact]
    public void ToString_WithTags_ReturnsCorrectFormat()
    {
        // Arrange
        var product = new Product("Test Product", 15, "Electronics", new[] { "computer", "work" });

        // Act
        var result = product.ToString();

        // Assert
        Assert.Contains("Test Product: 15 units (Category: Electronics) [Tags:", result);
        Assert.Contains("computer", result);
        Assert.Contains("work", result);
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