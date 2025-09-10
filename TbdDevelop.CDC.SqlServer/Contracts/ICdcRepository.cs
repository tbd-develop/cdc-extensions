using TbdDevelop.CDC.Extensions.Infrastructure;

namespace TbdDevelop.CDC.Extensions.Contracts;

public interface ICdcRepository
{
    Task<IEnumerable<CdcChangeRecord>> GetChangesAsync(
        string tableName,
        byte[] fromLsn,
        byte[] toLsn);

    Task<byte[]?> GetMinLsnAsync(string tableName);
}