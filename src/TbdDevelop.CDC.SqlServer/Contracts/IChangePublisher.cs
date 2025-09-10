namespace TbdDevelop.CDC.Extensions.Contracts;

public interface IChangePublisher
{
    Task PublishAsync(EntityChange change, CancellationToken cancellationToken = default);
}

public record EntityChange(ChangeOperation ChangeOperation, object Data, string Source);

public enum ChangeOperation
{
    Insert = 1,
    Update = 2,
    Delete = 3,
}