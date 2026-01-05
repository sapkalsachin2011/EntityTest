using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EntityTestApi.Data;
using EntityTestApi.Models;
using EntityTestApi.Models.DTOs;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Localization;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.Extensions.Caching.Memory;
using System.ComponentModel.DataAnnotations;
using EntityTestApi.Exceptions;
using ValidationException = EntityTestApi.Exceptions.ValidationException; // Resolve ambiguity


namespace EntityTestApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductsController> _logger;
        private readonly IMemoryCache _memoryCache;
        private const string ALL_PRODUCTS_CACHE_KEY = "all_products";

    // CQRS handlers
    private readonly CQRS.Commands.ICommandHandler<CQRS.Commands.CreateProductCommand> _createProductCommandHandler;
    private readonly CQRS.Queries.IQueryHandler<CQRS.Queries.GetProductsQuery, IEnumerable<string>> _getProductsQueryHandler;


        public ProductsController(ApplicationDbContext context, ILogger<ProductsController> logger, IMemoryCache memoryCache)
        : this(context, logger, memoryCache, null, null) { }

        public ProductsController(
            ApplicationDbContext context,
            ILogger<ProductsController> logger,
            IMemoryCache memoryCache,
            CQRS.Commands.ICommandHandler<CQRS.Commands.CreateProductCommand> createProductCommandHandler,
            CQRS.Queries.IQueryHandler<CQRS.Queries.GetProductsQuery, IEnumerable<string>> getProductsQueryHandler)
        {
            _context = context;
            _logger = logger;
            _memoryCache = memoryCache;
            _createProductCommandHandler = createProductCommandHandler;
            _getProductsQueryHandler = getProductsQueryHandler;
        }
        /// <summary>
        /// CQRS: Create a new product using command handler
        /// </summary>
        [HttpPost("cqrs")]
        public async Task<IActionResult> CreateProductCqrs([FromBody] CQRS.Commands.CreateProductCommand command, CancellationToken cancellationToken)
        {
            await _createProductCommandHandler.Handle(command, cancellationToken);
            return Ok(new { message = "Product creation command handled (CQRS pattern)." });
        }

        /// <summary>
        /// CQRS: Get all products using query handler
        /// </summary>
        [HttpGet("cqrs")]
        public async Task<IActionResult> GetProductsCqrs(CancellationToken cancellationToken)
        {
            var result = await _getProductsQueryHandler.Handle(new CQRS.Queries.GetProductsQuery(), cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Get all products - Supports content negotiation (JSON/XML)
        /// </summary>
        /// <remarks>
        /// Request with Accept header:
        /// - application/json for JSON response
        /// - application/xml for XML response
        /// Or use query parameter: ?format=json or ?format=xml
        /// </remarks>
        [HttpGet]
        [Produces("application/json", "application/xml", "text/xml")]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts([FromQuery] string? format = null)
        {
            _logger.LogInformation($"Fetching all products, Accept: {Request.Headers.Accept}, Format: {format}");
            
            List<ProductDto> productDtos;
            
            // Try to get from cache
            if (_memoryCache.TryGetValue(ALL_PRODUCTS_CACHE_KEY, out List<ProductDto>? cachedProducts))
            {
                _logger.LogInformation("✓ Returning all products from IMemoryCache");
                Console.WriteLine("✓ Returning all products from IMemoryCache");
                productDtos = cachedProducts;
            }
            else
            {
                // Fetch products and map to DTOs - No circular references
                productDtos = await _context.Products
                    .Include(p => p.Category)
                    .Select(p => new ProductDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        Price = p.Price,
                        CategoryId = p.CategoryId,
                        CategoryName = p.Category.Name
                    })
                    .ToListAsync();

                // Store in cache with absolute expiration
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
                    SlidingExpiration = TimeSpan.FromMinutes(2)
                };
                _memoryCache.Set(ALL_PRODUCTS_CACHE_KEY, productDtos, cacheOptions);

                _logger.LogInformation("Fetching all products from database and caching");
                Console.WriteLine("Fetching all products from database and caching");
            }
            
            // Apply format if specified (single location for format handling)
            if (!string.IsNullOrEmpty(format))
            {
                switch (format.ToLower())
                {
                    case "xml":
                        return new ObjectResult(productDtos) 
                        { 
                            ContentTypes = { "application/xml", "text/xml" } 
                        };
                    case "json":
                        return new ObjectResult(productDtos) 
                        { 
                            ContentTypes = { "application/json" } 
                        };
                    default:
                        return StatusCode(406, new { message = $"Format '{format}' not supported. Use 'json' or 'xml'." });
                }
            }
            
            // Use Accept header for content negotiation (default behavior)
            return Ok(productDtos);
        }


        // <summary>
        /// Show all registered routes
        /// </summary>
        [HttpGet("debug/routes")]
        public ActionResult ShowRoutes([FromServices] IActionDescriptorCollectionProvider provider)
        {
            var routes = provider.ActionDescriptors.Items.Select(x => new
            {
                Action = x.RouteValues["Action"],
                Controller = x.RouteValues["Controller"],
                Template = x.AttributeRouteInfo?.Template,
                HttpMethods = string.Join(", ", x.ActionConstraints?.OfType<HttpMethodActionConstraint>().FirstOrDefault()?.HttpMethods ?? new[] { "ANY" })
            }).ToList();

            return Ok(routes);
        }

        /// <summary>
        /// Get a product by ID - Supports content negotiation (JSON/XML)
        /// </summary>
        /// <remarks>
        /// Request with Accept header:
        /// - application/json for JSON response
        /// - application/xml for XML response
        /// - text/xml for XML response
        /// Or use query parameter: ?format=json or ?format=xml
        /// </remarks>
        [HttpGet("{id}")]
        [Produces("application/json", "application/xml", "text/xml")]
        public async Task<ActionResult<ProductDto>> GetProduct(int id, [FromQuery] string? format = null)
        {
            _logger.LogInformation($"Fetching product with ID: {id}, Accept: {Request.Headers.Accept}, Format: {format}");
            
            // Fetch product and map to DTO - No circular references
            var product = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.Id == id)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category.Name
                })
                .FirstOrDefaultAsync();

            if (product == null)
            {
                _logger.LogWarning($"Product with ID {id} not found");
                return NotFound(new { message = $"Product with ID {id} not found" });
            }

            // Allow format override via query parameter
            if (!string.IsNullOrEmpty(format))
            {
                switch (format.ToLower())
                {
                    case "xml":
                        return new ObjectResult(product) 
                        { 
                            ContentTypes = { "application/xml", "text/xml" } 
                        };
                    case "json":
                        return new ObjectResult(product) 
                        { 
                            ContentTypes = { "application/json" } 
                        };
                    default:
                        return StatusCode(406, new { message = $"Format '{format}' not supported. Use 'json' or 'xml'." });
                }
            }

            // Use Accept header for content negotiation (default behavior)
            return Ok(product);
        }

        /// <summary>
        /// Create a new product
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Product>> CreateProduct([FromBody] CreateProductRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var product = new Product
                    {
                        Name = request.Name,
                        Description = request.Description,
                        Price = request.Price
                    };

                    _context.Products.Add(product);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    _logger.LogInformation($"Product created with ID: {product.Id}");
                    return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error occurred while creating product, transaction rolled back.");
                    return StatusCode(500, "An error occurred while creating the product.");
                }
            }
        }

        /// <summary>
        /// Update an existing product
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                _logger.LogWarning($"Product with ID {id} not found for update");
                return NotFound(new { message = $"Product with ID {id} not found" });
            }

            // Set updated values
            product.Name = request.Name ?? product.Name;
            product.Description = request.Description ?? product.Description;
            if (request.Price.HasValue)
            {
                product.Price = request.Price.Value;
            }

            // Set RowVersion for concurrency check
            if (request.RowVersion != null)
            {
                product.RowVersion = Convert.FromBase64String(request.RowVersion);
            }

            _context.Entry(product).OriginalValues["RowVersion"] = product.RowVersion;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                _logger.LogWarning($"Concurrency conflict when updating product with ID {id}");
                return Conflict(new { message = "The record you attempted to edit was modified by another user after you got the original value. The edit operation was canceled." });
            }

            _logger.LogInformation($"Product with ID {id} updated");
            return Ok(product);
        }

        /// <summary>
        /// Delete a product
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            // if (product == null)
            // {
            //     _logger.LogWarning($"Product with ID {id} not found for deletion");
            //     return NotFound(new { message = $"Product with ID {id} not found" });
            // }

        if (product == null)
            throw new NotFoundException("Product", id);


            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Product with ID {id} deleted");
            return NoContent();
        }

        /// <summary>
        /// Test endpoint - Throws NotFoundException
        /// </summary>
        [HttpGet("test/notfound")]
        public IActionResult TestNotFoundException()
        {
            throw new NotFoundException("Product", 999);
        }

        /// <summary>
        /// Test endpoint - Throws ValidationException
        /// </summary>
        [HttpGet("test/validation")]
        public IActionResult TestValidationException()
        {
            var errors = new Dictionary<string, string[]>
            {
                { "Name", new[] { "Name is required", "Name must be unique" } },
                { "Price", new[] { "Price must be greater than 0" } }
            };
            throw new ValidationException(errors);
        }

        /// <summary>
        /// Test endpoint - Throws generic exception
        /// </summary>
        [HttpGet("test/error")]
        public IActionResult TestGenericException()
        {
            throw new InvalidOperationException("This is a test exception to demonstrate global error handling");
        }

        /// <summary>
        /// Test endpoint - Simulates database error
        /// </summary>
        [HttpGet("test/dberror")]
        public async Task<IActionResult> TestDatabaseError()
        {
            // Try to query a non-existent table to trigger DbException
            throw new DbUpdateException("Database update failed", 
                new Exception("A database error occurred while saving changes"));
        }
    }

    /// <summary>
    /// DTO for creating a product
    /// </summary>
    public class CreateProductRequest
    {
        [Required(ErrorMessage = "Name is required")]
        [MaxLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
        public string Name { get; set; } = null!;
        
        [MaxLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }
        
        [Range(0.01, 999999.99, ErrorMessage = "Price must be between 0.01 and 999999.99")]
        public decimal Price { get; set; }
    }

    /// <summary>
    /// DTO for updating a product
    /// </summary>
    public class UpdateProductRequest
    {
        [MaxLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
        public string? Name { get; set; }
        
        [MaxLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public string? RowVersion { get; set; } // Base64 encoded
    }
}
