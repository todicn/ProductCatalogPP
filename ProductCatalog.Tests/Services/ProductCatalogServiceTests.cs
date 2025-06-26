using ProductCatalog.Exceptions;
using ProductCatalog.Services;
using Xunit;

namespace ProductCatalog.Tests.Services;

public class ProductCatalogServiceTests
{
    private readonly ProductCatalogService _catalog;

    public ProductCatalogServiceTests()
    {
        _catalog = new ProductCatalogService();
    }

    #region AddProduct Tests

    [Fact]
    public void AddProduct_ValidProduct_AddsSuccessfully()
    {
        // Act
        _catalog.AddProduct("Test Product", 10);

        // Assert
        Assert.Equal(1, _catalog.GetProductCount());
        var product = _catalog.GetProduct("Test Product");
        Assert.Equal("Test Product", product.Name);
        Assert.Equal(10, product.Quantity);
        Assert.Equal("General", product.Category);
        Assert.Empty(product.Tags);
    }

    [Fact]
    public void AddProduct_WithCategoryAndTags_AddsSuccessfully()
    {
        // Act
        _catalog.AddProduct("Gaming Mouse", 15, "Electronics", new[] { "gaming", "wireless" });

        // Assert
        Assert.Equal(1, _catalog.GetProductCount());
        var product = _catalog.GetProduct("Gaming Mouse");
        Assert.Equal("Gaming Mouse", product.Name);
        Assert.Equal(15, product.Quantity);
        Assert.Equal("Electronics", product.Category);
        Assert.Equal(2, product.Tags.Count);
        Assert.Contains("gaming", product.Tags);
        Assert.Contains("wireless", product.Tags);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddProduct_InvalidName_ThrowsArgumentException(string invalidName)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _catalog.AddProduct(invalidName, 10));
    }

    [Fact]
    public void AddProduct_NegativeQuantity_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _catalog.AddProduct("Product", -1));
    }

    [Fact]
    public void AddProduct_ZeroQuantity_AddsSuccessfully()
    {
        // Act
        _catalog.AddProduct("Product", 0);

        // Assert
        Assert.Equal(1, _catalog.GetProductCount());
        var product = _catalog.GetProduct("Product");
        Assert.Equal(0, product.Quantity);
    }

    [Fact]
    public void AddProduct_DuplicateName_ThrowsProductAlreadyExistsException()
    {
        // Arrange
        _catalog.AddProduct("Product", 10);

        // Act & Assert
        Assert.Throws<ProductAlreadyExistsException>(() => _catalog.AddProduct("Product", 5));
    }

    [Fact]
    public void AddProduct_DuplicateNameDifferentCase_ThrowsProductAlreadyExistsException()
    {
        // Arrange
        _catalog.AddProduct("Product", 10);

        // Act & Assert
        Assert.Throws<ProductAlreadyExistsException>(() => _catalog.AddProduct("PRODUCT", 5));
    }

    [Fact]
    public void AddProduct_NameWithWhitespace_TrimsAndAdds()
    {
        // Act
        _catalog.AddProduct("  Product Name  ", 10);

        // Assert
        var product = _catalog.GetProduct("Product Name");
        Assert.Equal("Product Name", product.Name);
    }

    #endregion

    #region RemoveProduct Tests

    [Fact]
    public void RemoveProduct_ExistingProduct_RemovesSuccessfully()
    {
        // Arrange
        _catalog.AddProduct("Product", 10);

        // Act
        _catalog.RemoveProduct("Product");

        // Assert
        Assert.Equal(0, _catalog.GetProductCount());
        Assert.Throws<ProductNotFoundException>(() => _catalog.GetProduct("Product"));
    }

    [Fact]
    public void RemoveProduct_NonExistentProduct_ThrowsProductNotFoundException()
    {
        // Act & Assert
        Assert.Throws<ProductNotFoundException>(() => _catalog.RemoveProduct("NonExistent"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RemoveProduct_InvalidName_ThrowsArgumentException(string invalidName)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _catalog.RemoveProduct(invalidName));
    }

    [Fact]
    public void RemoveProduct_CaseInsensitive_RemovesSuccessfully()
    {
        // Arrange
        _catalog.AddProduct("Product", 10);

        // Act
        _catalog.RemoveProduct("PRODUCT");

        // Assert
        Assert.Equal(0, _catalog.GetProductCount());
    }

    #endregion

    #region PurchaseProduct Tests

    [Fact]
    public void PurchaseProduct_ValidPurchase_DecreasesQuantity()
    {
        // Arrange
        _catalog.AddProduct("Product", 10);

        // Act
        _catalog.PurchaseProduct("Product", 3);

        // Assert
        var product = _catalog.GetProduct("Product");
        Assert.Equal(7, product.Quantity);
    }

    [Fact]
    public void PurchaseProduct_PurchaseAllQuantity_LeavesZero()
    {
        // Arrange
        _catalog.AddProduct("Product", 5);

        // Act
        _catalog.PurchaseProduct("Product", 5);

        // Assert
        var product = _catalog.GetProduct("Product");
        Assert.Equal(0, product.Quantity);
    }

    [Fact]
    public void PurchaseProduct_NonExistentProduct_ThrowsProductNotFoundException()
    {
        // Act & Assert
        Assert.Throws<ProductNotFoundException>(() => _catalog.PurchaseProduct("NonExistent", 1));
    }

    [Fact]
    public void PurchaseProduct_InsufficientQuantity_ThrowsInsufficientQuantityException()
    {
        // Arrange
        _catalog.AddProduct("Product", 5);

        // Act & Assert
        Assert.Throws<InsufficientQuantityException>(() => _catalog.PurchaseProduct("Product", 10));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void PurchaseProduct_InvalidQuantity_ThrowsArgumentException(int invalidQuantity)
    {
        // Arrange
        _catalog.AddProduct("Product", 10);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _catalog.PurchaseProduct("Product", invalidQuantity));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void PurchaseProduct_InvalidName_ThrowsArgumentException(string invalidName)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _catalog.PurchaseProduct(invalidName, 1));
    }

    [Fact]
    public void PurchaseProduct_CaseInsensitive_PurchasesSuccessfully()
    {
        // Arrange
        _catalog.AddProduct("Product", 10);

        // Act
        _catalog.PurchaseProduct("PRODUCT", 3);

        // Assert
        var product = _catalog.GetProduct("Product");
        Assert.Equal(7, product.Quantity);
    }

    #endregion

    #region ListProductsByQuantity Tests

    [Fact]
    public void ListProductsByQuantity_EmptyCatalog_ReturnsEmptyList()
    {
        // Act
        var products = _catalog.ListProductsByQuantity();

        // Assert
        Assert.Empty(products);
    }

    [Fact]
    public void ListProductsByQuantity_MultipleProducts_SortsByQuantityDescending()
    {
        // Arrange
        _catalog.AddProduct("Low", 5);
        _catalog.AddProduct("High", 20);
        _catalog.AddProduct("Medium", 10);

        // Act
        var products = _catalog.ListProductsByQuantity();

        // Assert
        Assert.Equal(3, products.Count);
        Assert.Equal("High", products[0].Name);
        Assert.Equal(20, products[0].Quantity);
        Assert.Equal("Medium", products[1].Name);
        Assert.Equal(10, products[1].Quantity);
        Assert.Equal("Low", products[2].Name);
        Assert.Equal(5, products[2].Quantity);
    }

    [Fact]
    public void ListProductsByQuantity_SameQuantity_SortsByNameAlphabetically()
    {
        // Arrange
        _catalog.AddProduct("Charlie", 10);
        _catalog.AddProduct("Alpha", 10);
        _catalog.AddProduct("Beta", 10);

        // Act
        var products = _catalog.ListProductsByQuantity();

        // Assert
        Assert.Equal(3, products.Count);
        Assert.Equal("Alpha", products[0].Name);
        Assert.Equal("Beta", products[1].Name);
        Assert.Equal("Charlie", products[2].Name);
    }

    [Fact]
    public void ListProductsByQuantity_ReturnsReadOnlyList()
    {
        // Arrange
        _catalog.AddProduct("Product", 10);

        // Act
        var products = _catalog.ListProductsByQuantity();

        // Assert
        Assert.IsAssignableFrom<IReadOnlyList<ProductCatalog.Models.Product>>(products);
    }

    #endregion

    #region GetProduct Tests

    [Fact]
    public void GetProduct_ExistingProduct_ReturnsProduct()
    {
        // Arrange
        _catalog.AddProduct("Product", 10);

        // Act
        var product = _catalog.GetProduct("Product");

        // Assert
        Assert.Equal("Product", product.Name);
        Assert.Equal(10, product.Quantity);
    }

    [Fact]
    public void GetProduct_NonExistentProduct_ThrowsProductNotFoundException()
    {
        // Act & Assert
        Assert.Throws<ProductNotFoundException>(() => _catalog.GetProduct("NonExistent"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetProduct_InvalidName_ThrowsArgumentException(string invalidName)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _catalog.GetProduct(invalidName));
    }

    [Fact]
    public void GetProduct_CaseInsensitive_ReturnsProduct()
    {
        // Arrange
        _catalog.AddProduct("Product", 10);

        // Act
        var product = _catalog.GetProduct("PRODUCT");

        // Assert
        Assert.Equal("Product", product.Name);
    }

    #endregion

    #region GetProductCount Tests

    [Fact]
    public void GetProductCount_EmptyCatalog_ReturnsZero()
    {
        // Act
        var count = _catalog.GetProductCount();

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public void GetProductCount_WithProducts_ReturnsCorrectCount()
    {
        // Arrange
        _catalog.AddProduct("Product1", 10);
        _catalog.AddProduct("Product2", 5);

        // Act
        var count = _catalog.GetProductCount();

        // Assert
        Assert.Equal(2, count);
    }

    [Fact]
    public void GetProductCount_AfterRemoval_ReturnsUpdatedCount()
    {
        // Arrange
        _catalog.AddProduct("Product1", 10);
        _catalog.AddProduct("Product2", 5);
        _catalog.RemoveProduct("Product1");

        // Act
        var count = _catalog.GetProductCount();

        // Assert
        Assert.Equal(1, count);
    }

    #endregion

    #region SearchByCategory Tests

    [Fact]
    public void SearchByCategory_ValidCategory_ReturnsMatchingProducts()
    {
        // Arrange
        _catalog.AddProduct("Laptop", 10, "Electronics");
        _catalog.AddProduct("Mouse", 25, "Electronics");
        _catalog.AddProduct("Desk", 5, "Furniture");

        // Act
        var result = _catalog.SearchByCategory("Electronics");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.Name == "Mouse" && p.Quantity == 25);
        Assert.Contains(result, p => p.Name == "Laptop" && p.Quantity == 10);
        // Check ordering (by quantity descending)
        Assert.Equal("Mouse", result[0].Name);
        Assert.Equal("Laptop", result[1].Name);
    }

    [Fact]
    public void SearchByCategory_CaseInsensitive_ReturnsMatchingProducts()
    {
        // Arrange
        _catalog.AddProduct("Laptop", 10, "Electronics");

        // Act
        var result = _catalog.SearchByCategory("ELECTRONICS");

        // Assert
        Assert.Single(result);
        Assert.Equal("Laptop", result[0].Name);
    }

    [Fact]
    public void SearchByCategory_NoMatches_ReturnsEmptyList()
    {
        // Arrange
        _catalog.AddProduct("Laptop", 10, "Electronics");

        // Act
        var result = _catalog.SearchByCategory("Furniture");

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SearchByCategory_InvalidCategory_ThrowsArgumentException(string invalidCategory)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _catalog.SearchByCategory(invalidCategory));
    }

    #endregion

    #region SearchByTag Tests

    [Fact]
    public void SearchByTag_ValidTag_ReturnsMatchingProducts()
    {
        // Arrange
        _catalog.AddProduct("Gaming Mouse", 20, "Electronics", new[] { "gaming", "wireless" });
        _catalog.AddProduct("Gaming Keyboard", 15, "Electronics", new[] { "gaming", "mechanical" });
        _catalog.AddProduct("Office Mouse", 10, "Electronics", new[] { "wireless", "office" });

        // Act
        var result = _catalog.SearchByTag("gaming");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.Name == "Gaming Mouse");
        Assert.Contains(result, p => p.Name == "Gaming Keyboard");
        // Check ordering (by quantity descending)
        Assert.Equal("Gaming Mouse", result[0].Name);
        Assert.Equal("Gaming Keyboard", result[1].Name);
    }

    [Fact]
    public void SearchByTag_CaseInsensitive_ReturnsMatchingProducts()
    {
        // Arrange
        _catalog.AddProduct("Gaming Mouse", 20, "Electronics", new[] { "gaming" });

        // Act
        var result = _catalog.SearchByTag("GAMING");

        // Assert
        Assert.Single(result);
        Assert.Equal("Gaming Mouse", result[0].Name);
    }

    [Fact]
    public void SearchByTag_NoMatches_ReturnsEmptyList()
    {
        // Arrange
        _catalog.AddProduct("Laptop", 10, "Electronics", new[] { "work" });

        // Act
        var result = _catalog.SearchByTag("gaming");

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SearchByTag_InvalidTag_ThrowsArgumentException(string invalidTag)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _catalog.SearchByTag(invalidTag));
    }

    #endregion

    #region SearchByTags Tests

    [Fact]
    public void SearchByTags_ValidTags_ReturnsMatchingProducts()
    {
        // Arrange
        _catalog.AddProduct("Gaming Mouse", 20, "Electronics", new[] { "gaming", "wireless" });
        _catalog.AddProduct("Office Mouse", 15, "Electronics", new[] { "office", "wireless" });
        _catalog.AddProduct("Mechanical Keyboard", 10, "Electronics", new[] { "gaming", "mechanical" });
        _catalog.AddProduct("Laptop", 5, "Electronics", new[] { "work", "portable" });

        // Act
        var result = _catalog.SearchByTags(new[] { "gaming", "office" });

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains(result, p => p.Name == "Gaming Mouse");
        Assert.Contains(result, p => p.Name == "Office Mouse");
        Assert.Contains(result, p => p.Name == "Mechanical Keyboard");
        Assert.DoesNotContain(result, p => p.Name == "Laptop");
    }

    [Fact]
    public void SearchByTags_EmptyTagList_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _catalog.SearchByTags(new string[0]));
    }

    [Fact]
    public void SearchByTags_NullTags_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _catalog.SearchByTags(null));
    }

    [Fact]
    public void SearchByTags_OnlyWhitespaceTags_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _catalog.SearchByTags(new[] { "", "   ", null }));
    }

    #endregion

    #region GetAllCategories Tests

    [Fact]
    public void GetAllCategories_WithProducts_ReturnsUniqueCategories()
    {
        // Arrange
        _catalog.AddProduct("Laptop", 10, "Electronics");
        _catalog.AddProduct("Mouse", 15, "Electronics");
        _catalog.AddProduct("Desk", 5, "Furniture");
        _catalog.AddProduct("Pen", 20, "Office");

        // Act
        var result = _catalog.GetAllCategories();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains("Electronics", result);
        Assert.Contains("Furniture", result);
        Assert.Contains("Office", result);
        // Check alphabetical ordering
        Assert.Equal("Electronics", result[0]);
        Assert.Equal("Furniture", result[1]);
        Assert.Equal("Office", result[2]);
    }

    [Fact]
    public void GetAllCategories_EmptyCatalog_ReturnsEmptyList()
    {
        // Act
        var result = _catalog.GetAllCategories();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetAllCategories_CaseInsensitive_NoDuplicates()
    {
        // Arrange
        _catalog.AddProduct("Product1", 10, "Electronics");
        _catalog.AddProduct("Product2", 15, "ELECTRONICS");
        _catalog.AddProduct("Product3", 5, "electronics");

        // Act
        var result = _catalog.GetAllCategories();

        // Assert
        Assert.Single(result);
        Assert.Equal("Electronics", result[0]);
    }

    #endregion

    #region GetAllTags Tests

    [Fact]
    public void GetAllTags_WithProducts_ReturnsUniqueTags()
    {
        // Arrange
        _catalog.AddProduct("Mouse", 10, "Electronics", new[] { "gaming", "wireless" });
        _catalog.AddProduct("Keyboard", 15, "Electronics", new[] { "gaming", "mechanical" });
        _catalog.AddProduct("Laptop", 5, "Electronics", new[] { "work", "portable" });

        // Act
        var result = _catalog.GetAllTags();

        // Assert
        Assert.Equal(5, result.Count);
        Assert.Contains("gaming", result);
        Assert.Contains("wireless", result);
        Assert.Contains("mechanical", result);
        Assert.Contains("work", result);
        Assert.Contains("portable", result);
    }

    [Fact]
    public void GetAllTags_EmptyCatalog_ReturnsEmptyList()
    {
        // Act
        var result = _catalog.GetAllTags();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetAllTags_CaseInsensitive_NoDuplicates()
    {
        // Arrange
        _catalog.AddProduct("Product1", 10, "Electronics", new[] { "Gaming" });
        _catalog.AddProduct("Product2", 15, "Electronics", new[] { "GAMING" });
        _catalog.AddProduct("Product3", 5, "Electronics", new[] { "gaming" });

        // Act
        var result = _catalog.GetAllTags();

        // Assert
        Assert.Single(result);
        Assert.Equal("Gaming", result[0]);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void ComplexScenario_AddPurchaseRemoveList_WorksCorrectly()
    {
        // Add products
        _catalog.AddProduct("Laptop", 10, "Electronics", new[] { "computer", "work" });
        _catalog.AddProduct("Mouse", 25, "Electronics", new[] { "computer", "gaming" });
        _catalog.AddProduct("Keyboard", 15, "Electronics", new[] { "computer", "gaming" });

        // Purchase some items
        _catalog.PurchaseProduct("Mouse", 5);
        _catalog.PurchaseProduct("Laptop", 3);

        // Remove a product
        _catalog.RemoveProduct("Keyboard");

        // List products
        var products = _catalog.ListProductsByQuantity();

        // Assert
        Assert.Equal(2, products.Count);
        Assert.Equal("Mouse", products[0].Name);
        Assert.Equal(20, products[0].Quantity);
        Assert.Equal("Laptop", products[1].Name);
        Assert.Equal(7, products[1].Quantity);
    }

    [Fact]
    public void ComplexScenario_CategoryAndTagSearch_WorksCorrectly()
    {
        // Add products with categories and tags
        _catalog.AddProduct("Gaming Mouse", 20, "Electronics", new[] { "gaming", "wireless" });
        _catalog.AddProduct("Office Mouse", 15, "Electronics", new[] { "office", "wireless" });
        _catalog.AddProduct("Gaming Keyboard", 10, "Electronics", new[] { "gaming", "mechanical" });
        _catalog.AddProduct("Office Chair", 5, "Furniture", new[] { "office", "ergonomic" });

        // Test category search
        var electronicsProducts = _catalog.SearchByCategory("Electronics");
        Assert.Equal(3, electronicsProducts.Count);

        // Test tag search
        var gamingProducts = _catalog.SearchByTag("gaming");
        Assert.Equal(2, gamingProducts.Count);

        // Test multiple tag search
        var officeProducts = _catalog.SearchByTags(new[] { "office" });
        Assert.Equal(2, officeProducts.Count);

        // Test categories and tags lists
        var categories = _catalog.GetAllCategories();
        Assert.Equal(2, categories.Count);
        Assert.Contains("Electronics", categories);
        Assert.Contains("Furniture", categories);

        var tags = _catalog.GetAllTags();
        Assert.Equal(5, tags.Count);
        Assert.Contains("gaming", tags);
        Assert.Contains("wireless", tags);
        Assert.Contains("office", tags);
        Assert.Contains("mechanical", tags);
        Assert.Contains("ergonomic", tags);
    }

    #endregion
} 