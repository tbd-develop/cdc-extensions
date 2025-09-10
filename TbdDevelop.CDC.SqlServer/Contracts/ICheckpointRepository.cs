using TbdDevelop.CDC.Extensions.Infrastructure;

namespace TbdDevelop.CDC.Extensions.Contracts;

public interface ICheckpointRepository
{
    Task SaveCheckpointAsync(Checkpoint checkpoint);
    Task<Checkpoint> GetLastCheckpointAsync(string tableName);
}