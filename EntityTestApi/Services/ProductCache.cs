using System.Collections.Concurrent;
using EntityTestApi.Models.DTOs;

namespace EntityTestApi.Services
{
    public interface IProductCache
    {
        void SetProducts(IEnumerable<ProductDto> products);
        IEnumerable<ProductDto>? GetProducts();
    }

    public class ProductCache : IProductCache
    {
        private List<ProductDto>? _products;
        private readonly object _lock = new object();

        public void SetProducts(IEnumerable<ProductDto> products)
        {
            lock (_lock)
            {
                _products = products.ToList();
            }
        }

        public IEnumerable<ProductDto>? GetProducts()
        {
            lock (_lock)
            {
                return _products?.ToList();
            }
        }
    }
}
