using System.Threading.Tasks;

namespace EntityTestApi.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;

        public ISupplierRepository Suppliers { get; }
        public IProductRepository Products { get; }

        public UnitOfWork(ApplicationDbContext context, ISupplierRepository supplierRepository, IProductRepository productRepository)
        {
            _context = context;
            Suppliers = supplierRepository;
            Products = productRepository;
        }

        public async Task<int> CompleteAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
