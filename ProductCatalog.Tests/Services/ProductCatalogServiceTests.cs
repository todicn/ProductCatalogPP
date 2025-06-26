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

    #region Integration Tests

    [Fact]
    public void ComplexScenario_AddPurchaseRemoveList_WorksCorrectly()
    {
        // Add products
        _catalog.AddProduct("Laptop", 10);
        _catalog.AddProduct("Mouse", 25);
        _catalog.AddProduct("Keyboard", 15);

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

    #endregion
} 