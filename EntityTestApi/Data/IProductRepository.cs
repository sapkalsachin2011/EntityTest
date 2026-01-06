using EntityTestApi.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EntityTestApi.Data
{
    public interface IProductRepository : IRepository<Product>
    {
        Task<IEnumerable<Product>> GetAllProductsAsync();
        Task<Product?> GetProductByIdAsync(int id);
    }
}
