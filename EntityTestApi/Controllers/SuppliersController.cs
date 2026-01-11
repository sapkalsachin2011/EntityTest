using Microsoft.AspNetCore.Mvc;
using EntityTestApi.Data;
using EntityTestApi.Models;
// using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace EntityTestApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SuppliersController : ControllerBase
    {
        private readonly ISupplierRepository _supplierRepository;
        private readonly ILogger<SuppliersController> _logger;
       // private readonly Kafka.KafkaProducerService _kafkaProducer;

        public SuppliersController(ISupplierRepository supplierRepository, ILogger<SuppliersController> logger) //, Kafka.KafkaProducerService kafkaProducer
        {
            _supplierRepository = supplierRepository;
            _logger = logger;
           // _kafkaProducer = kafkaProducer;
        }

        // GET: api/suppliers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Supplier>>> GetSuppliers()
        {
            var suppliers = await _supplierRepository.GetAllSuppliersAsync();
            return Ok(suppliers);
        }

        // GET: api/suppliers/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Supplier>> GetSupplier(int id)
        {
            var supplier = await _supplierRepository.GetSupplierByIdAsync(id);
            if (supplier is null)
                return NotFound();
            return Ok(supplier!);
        }

        // POST: api/suppliers
        [HttpPost]
        public async Task<ActionResult<Supplier>> CreateSupplier([FromBody] Supplier supplier)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            await _supplierRepository.AddAsync(supplier);
            await _supplierRepository.SaveChangesAsync();
            _logger.LogInformation($"Supplier created with ID: {supplier.Id}");

            // Send Kafka message
            var message = $"Supplier created: {{ Id: {supplier.Id}, Name: '{supplier.Name}', Email: '{supplier.ContactEmail}' }}";
           // await _kafkaProducer.ProduceAsync(message);

            return CreatedAtAction(nameof(GetSupplier), new { id = supplier.Id }, supplier);
        }

        // PUT: api/suppliers/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSupplier(int id, [FromBody] Supplier request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var supplier = await _supplierRepository.GetByIdAsync(id);
            if (supplier is null)
                return NotFound();
            supplier.Name = request.Name;
            supplier.Description = request.Description;
            supplier.ContactEmail = request.ContactEmail;
            _supplierRepository.Update(supplier);
            await _supplierRepository.SaveChangesAsync();
            _logger.LogInformation($"Supplier with ID {id} updated");
            return Ok(supplier);
        }

        // DELETE: api/suppliers/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSupplier(int id)
        {
            var supplier = await _supplierRepository.GetByIdAsync(id);
            if (supplier is null)
                return NotFound();
            _supplierRepository.Remove(supplier);
            await _supplierRepository.SaveChangesAsync();
            _logger.LogInformation($"Supplier with ID {id} deleted");
            return NoContent();
        }
    }
}
