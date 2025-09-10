using TbdDevelop.CDC.Extensions.Contracts;

namespace TbdDevelop.CDC.Extensions.Infrastructure.Publishing;

public class ChangesPublisher(IPublisherLookup lookup) : IChangePublisher
{
    public async Task PublishAsync(EntityChange change, CancellationToken cancellationToken = default)
    {
        if (lookup.TryGetHandlerFor(change.Source, out var handler))
        {
            await handler!.HandleChangeAsync(change.Data, cancellationToken);
        }
    }
}