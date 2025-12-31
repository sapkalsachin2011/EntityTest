using NUnit.Framework;
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

namespace EntityTestApi.NUnit.Tests;

[TestFixture]
public class ProductsControllerTests
{
    private ApplicationDbContext _context = null!;
    private Mock<ILogger<ProductsController>> _mockLogger = null!;
    private Mock<IMemoryCache> _mockCache = null!;
    private ProductsController _controller = null!;

    [SetUp]
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

    [TearDown]
    public void TearDown()
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

    [Test]
    public async Task GetProduct_ReturnsOkResult_WhenProductExists()
    {
        // Arrange
        var productId = 1;

        // Act
        var result = await _controller.GetProduct(productId);

        // Assert
        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
        
        var okResult = result.Result as OkObjectResult;
        var productDto = okResult!.Value as ProductDto;
        
        Assert.That(productDto, Is.Not.Null);
        Assert.That(productDto!.Id, Is.EqualTo(1));
        Assert.That(productDto.Name, Is.EqualTo("Wireless Mouse"));
        Assert.That(productDto.Price, Is.EqualTo(29.99m));
        Assert.That(productDto.CategoryName, Is.EqualTo("Electronics"));
    }

    [Test]
    public async Task GetProduct_ReturnsNotFound_WhenProductDoesNotExist()
    {
        // Arrange
        var invalidId = 999;

        // Act
        var result = await _controller.GetProduct(invalidId);

        // Assert
        Assert.That(result.Result, Is.TypeOf<NotFoundObjectResult>());
        
        var notFoundResult = result.Result as NotFoundObjectResult;
        Assert.That(notFoundResult, Is.Not.Null);
        
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

    [Test]
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
        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
        
        var okResult = result.Result as OkObjectResult;
        var products = okResult!.Value as List<ProductDto>;
        
        Assert.That(products, Has.Count.EqualTo(1));
        Assert.That(products![0].Name, Is.EqualTo("Cached Mouse"));
        Assert.That(products[0].Price, Is.EqualTo(19.99m));
    }

    [Test]
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
        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
        
        var okResult = result.Result as OkObjectResult;
        var products = okResult!.Value as List<ProductDto>;
        
        Assert.That(products, Has.Count.EqualTo(2));
        Assert.That(products![0].Name, Is.EqualTo("Wireless Mouse"));
        Assert.That(products[1].Name, Is.EqualTo("Keyboard"));
    }

    [Test]
    public async Task GetProduct_ReturnsXml_WhenFormatIsXml()
    {
        // Arrange
        var productId = 1;

        // Act
        var result = await _controller.GetProduct(productId, "xml");

        // Assert
        Assert.That(result.Result, Is.TypeOf<ObjectResult>());
        
        var objectResult = result.Result as ObjectResult;
        Assert.That(objectResult!.ContentTypes, Does.Contain("application/xml"));
        Assert.That(objectResult.ContentTypes, Does.Contain("text/xml"));
        
        var productDto = objectResult.Value as ProductDto;
        Assert.That(productDto, Is.Not.Null);
        Assert.That(productDto!.Name, Is.EqualTo("Wireless Mouse"));
    }

    [Test]
    public async Task DeleteProduct_ReturnsNoContent_WhenProductExists()
    {
        // Arrange
        var productId = 1;

        // Act
        var result = await _controller.DeleteProduct(productId);

        // Assert
        Assert.That(result, Is.TypeOf<NoContentResult>());
        
        // Verify product was deleted
        var deletedProduct = await _context.Products.FindAsync(productId);
        Assert.That(deletedProduct, Is.Null);
    }

    [Test]
    public async Task DeleteProduct_ReturnsNotFound_WhenProductDoesNotExist()
    {
        // Arrange
        var invalidId = 999;

        // Act
        var result = await _controller.DeleteProduct(invalidId);

        // Assert
        Assert.That(result, Is.TypeOf<NotFoundObjectResult>());
    }
}
