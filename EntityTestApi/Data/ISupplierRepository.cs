using EntityTestApi.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EntityTestApi.Data
{
    public interface ISupplierRepository : IRepository<Supplier>
    {
        Task<IEnumerable<Supplier>> GetAllSuppliersAsync();
        Task<Supplier?> GetSupplierByIdAsync(int id);
        // IRepository<Supplier> already exposes AddAsync, Update, Remove, SaveChangesAsync, GetByIdAsync
    }
}
