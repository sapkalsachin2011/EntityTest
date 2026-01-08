// used for kafka testing
using Microsoft.AspNetCore.Mvc;
using EntityTestApi.Models;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;namespace EntityTestApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SampleDataController : ControllerBase
    {
        private readonly Data.ApplicationDbContext _context;

        public SampleDataController(Data.ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("products")]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductDetail)
                .ToListAsync();
            return Ok(products);
        }

        [HttpGet("suppliers")]
        public async Task<ActionResult<IEnumerable<Supplier>>> GetSuppliers()
        {
            var suppliers = await _context.Set<Supplier>().ToListAsync();
            return Ok(suppliers);
        }

        [HttpGet("categories")]
        public async Task<ActionResult<IEnumerable<Category>>> GetCategories()
        {
            var categories = await _context.Set<Category>().ToListAsync();
            return Ok(categories);
        }

        [HttpGet("productdetails")]
        public async Task<ActionResult<IEnumerable<ProductDetail>>> GetProductDetails()
        {
            var details = await _context.Set<ProductDetail>().ToListAsync();
            return Ok(details);
        }
    }
}
