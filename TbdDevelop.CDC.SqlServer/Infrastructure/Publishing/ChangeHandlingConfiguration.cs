using Microsoft.Extensions.DependencyInjection;
using TbdDevelop.CDC.Extensions.Contracts;

namespace TbdDevelop.CDC.Extensions.Infrastructure.Publishing;

public class ChangeHandlingConfiguration
{
    private readonly IServiceCollection _services;
    private readonly Dictionary<string, Type> _handlerLookup = new(StringComparer.OrdinalIgnoreCase);

    public ChangeHandlingConfiguration(IServiceCollection services)
    {
        _services = services;

        services.AddSingleton<IPublisherLookup>(provider => new PublisherLookup(provider, _handlerLookup));
        services.AddSingleton<IChangePublisher, ChangesPublisher>();
    }

    public ChangeHandlingConfiguration RegisterHandler<THandler>(string collectionName)
        where THandler : class, IChangeHandler
    {
        if (_handlerLookup.ContainsKey(collectionName))
        {
            throw new ArgumentException($"Handler {collectionName} is already registered");
        }

        _handlerLookup.Add(collectionName, typeof(THandler));

        _services.AddScoped<THandler>();

        return this;
    }
}