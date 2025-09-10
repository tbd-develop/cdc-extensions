using System.Reflection;

namespace TbdDevelop.CDC.Extensions.Infrastructure.Publishing;

public interface IChangeHandler
{
    Task HandleChangeAsync(object data, CancellationToken cancellationToken);
}

public abstract class ChangeHandler<TEntity> : IChangeHandler
    where TEntity : class, new()
{
    private static readonly IEnumerable<PropertyInfo> ChangeHandlerProperties =
        typeof(TEntity).GetProperties(BindingFlags.Public | BindingFlags.Instance);

    public Task HandleChangeAsync(object data, CancellationToken cancellationToken)
    {
        if (MapFrom(data) is { } entity)
        {
            return HandleChangeAsync(entity, cancellationToken);
        }

        return Task.CompletedTask;
    }

    protected abstract Task HandleChangeAsync(TEntity data, CancellationToken cancellationToken);

    private TEntity MapFrom(dynamic input)
    {
        var getter = input as IDictionary<string, object>;

        ArgumentNullException.ThrowIfNull(getter);

        var result = new TEntity();

        foreach (var property in ChangeHandlerProperties)
        {
            if (getter.TryGetValue(property.Name, out var value))
            {
                property.SetValue(result, value, null);
            }
        }

        return result;
    }
}