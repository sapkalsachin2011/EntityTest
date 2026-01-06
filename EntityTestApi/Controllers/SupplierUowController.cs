using Microsoft.AspNetCore.Mvc;
using EntityTestApi.Data;
using EntityTestApi.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace EntityTestApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SupplierUowController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SupplierUowController> _logger;

        public SupplierUowController(IUnitOfWork unitOfWork, ILogger<SupplierUowController> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        // GET: api/supplieruow
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Supplier>>> GetSuppliers()
        {
            var suppliers = await _unitOfWork.Suppliers.GetAllSuppliersAsync();
            return Ok(suppliers);
        }

        // GET: api/supplieruow/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Supplier>> GetSupplier(int id)
        {
            var supplier = await _unitOfWork.Suppliers.GetSupplierByIdAsync(id);
            if (supplier is null)
                return NotFound();
            return Ok(supplier);
        }

        // POST: api/supplieruow
        [HttpPost]
        public async Task<ActionResult<Supplier>> CreateSupplier([FromBody] Supplier supplier)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            await _unitOfWork.Suppliers.AddAsync(supplier);
            await _unitOfWork.CompleteAsync();
            _logger.LogInformation($"Supplier created with ID: {supplier.Id}");
            return CreatedAtAction(nameof(GetSupplier), new { id = supplier.Id }, supplier);
        }

        // PUT: api/supplieruow/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSupplier(int id, [FromBody] Supplier request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var supplier = await _unitOfWork.Suppliers.GetByIdAsync(id);
            if (supplier is null)
                return NotFound();
            supplier.Name = request.Name;
            supplier.Description = request.Description;
            supplier.ContactEmail = request.ContactEmail;
            _unitOfWork.Suppliers.Update(supplier);
            await _unitOfWork.CompleteAsync();
            _logger.LogInformation($"Supplier with ID {id} updated");
            return Ok(supplier);
        }

        // DELETE: api/supplieruow/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSupplier(int id)
        {
            var supplier = await _unitOfWork.Suppliers.GetByIdAsync(id);
            if (supplier is null)
                return NotFound();
            _unitOfWork.Suppliers.Remove(supplier);
            await _unitOfWork.CompleteAsync();
            _logger.LogInformation($"Supplier with ID {id} deleted");
            return NoContent();
        }

        // POST: api/supplieruow/atomic
        /// <summary>
        /// Demonstrates atomic commit: adds a Supplier and a Product in one transaction
        /// </summary>

        [HttpPost("atomic")]
        public async Task<IActionResult> CreateSupplierAndProduct([FromBody] AtomicSupplierProductRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Add Supplier
            var supplier = new Supplier
            {
                Name = request.SupplierName,
                Description = request.SupplierDescription,
                ContactEmail = request.SupplierEmail
            };
            await _unitOfWork.Suppliers.AddAsync(supplier);

            // Add Product via UnitOfWork
            var product = new Product
            {
                Name = request.ProductName,
                Description = request.ProductDescription,
                Price = request.ProductPrice,
                // Optionally, set CategoryId if needed
            };
            await _unitOfWork.Products.AddAsync(product);

            // Commit both in a single transaction
            await _unitOfWork.CompleteAsync();

            return Ok(new { supplier, product, message = "Both Supplier and Product created atomically." });
        }

        public class AtomicSupplierProductRequest
        {
            public string SupplierName { get; set; }
            public string SupplierDescription { get; set; }
            public string SupplierEmail { get; set; }
            public string ProductName { get; set; }
            public string ProductDescription { get; set; }
            public decimal ProductPrice { get; set; }
        }
    }
}
