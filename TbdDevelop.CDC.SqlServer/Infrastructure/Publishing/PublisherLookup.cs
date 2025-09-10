using Microsoft.Extensions.DependencyInjection;

namespace TbdDevelop.CDC.Extensions.Infrastructure.Publishing;

public interface IPublisherLookup
{
    bool TryGetHandlerFor(string collectionName, out IChangeHandler? handler);
}

public class PublisherLookup(
    IServiceProvider provider,
    IDictionary<string, Type> handlers) : IPublisherLookup
{
    public bool TryGetHandlerFor(string collectionName, out IChangeHandler? handler)
    {
        handler = null;

        if (handlers.TryGetValue(collectionName, out var type))
        {
            handler = provider.GetRequiredService(type) as IChangeHandler;
        }

        return handler is not null;
    }
}