using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EntityTestApi.CQRS.Queries
{
    // Example query
    public class GetProductsQuery
    {
        // Add filter properties if needed
    }

    // Query handler interface
    public interface IQueryHandler<TQuery, TResult>
    {
        Task<TResult> Handle(TQuery query, CancellationToken cancellationToken = default);
    }

    // Example query handler
    public class GetProductsQueryHandler : IQueryHandler<GetProductsQuery, IEnumerable<string>>
    {
        public async Task<IEnumerable<string>> Handle(GetProductsQuery query, CancellationToken cancellationToken = default)
        {
            // Add your read logic here (e.g., fetch from DB)
            // This is just a placeholder
            return await Task.FromResult(new List<string> { "Product1", "Product2" });
        }
    }
}
