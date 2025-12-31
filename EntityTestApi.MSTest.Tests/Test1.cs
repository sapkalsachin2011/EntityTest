using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using EntityTestApi.Controllers;
using EntityTestApi.Data;
using EntityTestApi.Models;
using EntityTestApi.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EntityTestApi.MSTest.Tests;

[TestClass]
public class ProductsControllerTests
{
    private ApplicationDbContext _context = null!;
    private Mock<ILogger<ProductsController>> _mockLogger = null!;
    private Mock<IMemoryCache> _mockCache = null!;
    private ProductsController _controller = null!;

    [TestInitialize]
    public void Setup()
    {
        // Setup in-memory database with transaction warnings suppressed
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        
        _context = new ApplicationDbContext(options);
        
        // Create mocks
        _mockLogger = new Mock<ILogger<ProductsController>>();
        _mockCache = new Mock<IMemoryCache>();
        
        // Create controller with mocked dependencies
        _controller = new ProductsController(_context, _mockLogger.Object, _mockCache.Object);
        
        // Setup HTTP context for controller
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        
        // Seed test data
        SeedTestData();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private void SeedTestData()
    {
        var category = new Category 
        { 
            Id = 1, 
            Name = "Electronics"
        };
        
        var products = new List<Product>
        {
            new Product 
            { 
                Id = 1, 
                Name = "Wireless Mouse", 
                Description = "Ergonomic wireless mouse",
                Price = 29.99m,
                CategoryId = 1,
                Category = category,
                RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
            },
            new Product 
            { 
                Id = 2, 
                Name = "Keyboard", 
                Description = "Mechanical keyboard",
                Price = 149.99m,
                CategoryId = 1,
                Category = category,
                RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
            }
        };

        _context.Categories.Add(category);
        _context.Products.AddRange(products);
        _context.SaveChanges();
    }

    [TestMethod]
    public async Task GetProduct_ReturnsOkResult_WhenProductExists()
    {
        // Arrange
        var productId = 1;

        // Act
        var result = await _controller.GetProduct(productId);

        // Assert
        Assert.IsInstanceOfType(result.Result, typeof(OkObjectResult));
        
        var okResult = result.Result as OkObjectResult;
        var productDto = okResult!.Value as ProductDto;
        
        Assert.IsNotNull(productDto);
        Assert.AreEqual(1, productDto!.Id);
        Assert.AreEqual("Wireless Mouse", productDto.Name);
        Assert.AreEqual(29.99m, productDto.Price);
        Assert.AreEqual("Electronics", productDto.CategoryName);
    }

    [TestMethod]
    public async Task GetProduct_ReturnsNotFound_WhenProductDoesNotExist()
    {
        // Arrange
        var invalidId = 999;

        // Act
        var result = await _controller.GetProduct(invalidId);

        // Assert
        Assert.IsInstanceOfType(result.Result, typeof(NotFoundObjectResult));
        
        var notFoundResult = result.Result as NotFoundObjectResult;
        Assert.IsNotNull(notFoundResult);
        
        // Verify logger was called
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Product with ID {invalidId} not found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [TestMethod]
    public async Task GetProducts_ReturnsCachedData_WhenCacheHit()
    {
        // Arrange
        var cachedProducts = new List<ProductDto>
        {
            new ProductDto 
            { 
                Id = 1, 
                Name = "Cached Mouse", 
                Price = 19.99m,
                CategoryId = 1,
                CategoryName = "Electronics"
            }
        };

        object? cacheEntry = cachedProducts;
        _mockCache
            .Setup(x => x.TryGetValue("all_products", out cacheEntry))
            .Returns(true);

        // Act
        var result = await _controller.GetProducts();

        // Assert
        Assert.IsInstanceOfType(result.Result, typeof(OkObjectResult));
        
        var okResult = result.Result as OkObjectResult;
        var products = okResult!.Value as List<ProductDto>;
        
        Assert.AreEqual(1, products!.Count);
        Assert.AreEqual("Cached Mouse", products[0].Name);
        Assert.AreEqual(19.99m, products[0].Price);
    }

    [TestMethod]
    public async Task GetProducts_FetchesFromDatabase_WhenCacheMiss()
    {
        // Arrange
        object? cacheEntry = null;
        _mockCache
            .Setup(x => x.TryGetValue("all_products", out cacheEntry))
            .Returns(false);
        
        _mockCache
            .Setup(x => x.CreateEntry(It.IsAny<object>()))
            .Returns(Mock.Of<ICacheEntry>());

        // Act
        var result = await _controller.GetProducts();

        // Assert
        Assert.IsInstanceOfType(result.Result, typeof(OkObjectResult));
        
        var okResult = result.Result as OkObjectResult;
        var products = okResult!.Value as List<ProductDto>;
        
        Assert.AreEqual(2, products!.Count);
        Assert.AreEqual("Wireless Mouse", products[0].Name);
        Assert.AreEqual("Keyboard", products[1].Name);
    }

    [TestMethod]
    public async Task GetProduct_ReturnsXml_WhenFormatIsXml()
    {
        // Arrange
        var productId = 1;

        // Act
        var result = await _controller.GetProduct(productId, "xml");

        // Assert
        Assert.IsInstanceOfType(result.Result, typeof(ObjectResult));
        
        var objectResult = result.Result as ObjectResult;
        CollectionAssert.Contains(objectResult!.ContentTypes.ToList(), "application/xml");
        CollectionAssert.Contains(objectResult.ContentTypes.ToList(), "text/xml");
        
        var productDto = objectResult.Value as ProductDto;
        Assert.IsNotNull(productDto);
        Assert.AreEqual("Wireless Mouse", productDto!.Name);
    }

    [TestMethod]
    public async Task DeleteProduct_ReturnsNoContent_WhenProductExists()
    {
        // Arrange
        var productId = 1;

        // Act
        var result = await _controller.DeleteProduct(productId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(NoContentResult));
        
        // Verify product was deleted
        var deletedProduct = await _context.Products.FindAsync(productId);
        Assert.IsNull(deletedProduct);
    }

    [TestMethod]
    public async Task DeleteProduct_ReturnsNotFound_WhenProductDoesNotExist()
    {
        // Arrange
        var invalidId = 999;

        // Act
        var result = await _controller.DeleteProduct(invalidId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(NotFoundObjectResult));
    }
}
