using System.Threading;
using System.Threading.Tasks;

namespace EntityTestApi.CQRS.Commands
{
    // Example command
    public class CreateProductCommand
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
    }

    // Command handler interface
    public interface ICommandHandler<TCommand>
    {
        Task Handle(TCommand command, CancellationToken cancellationToken = default);
    }

    // Example command handler
    public class CreateProductCommandHandler : ICommandHandler<CreateProductCommand>
    {
        public async Task Handle(CreateProductCommand command, CancellationToken cancellationToken = default)
        {
            // Add your write logic here (e.g., save to DB)
            // This is just a placeholder
            await Task.CompletedTask;
        }
    }
}
