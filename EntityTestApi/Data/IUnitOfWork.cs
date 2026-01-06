using System;
using System.Threading.Tasks;

namespace EntityTestApi.Data
{
    public interface IUnitOfWork : IDisposable
    {
        ISupplierRepository Suppliers { get; }
        IProductRepository Products { get; }
        Task<int> CompleteAsync();
    }
}
