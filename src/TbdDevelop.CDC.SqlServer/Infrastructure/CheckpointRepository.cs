using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using TbdDevelop.CDC.Extensions.Contracts;

namespace TbdDevelop.CDC.Extensions.Infrastructure;

public class CheckpointRepository(
    IConfiguration configuration,
    IOptions<MonitoringOptions> options) : ICheckpointRepository
{
    public async Task SaveCheckpointAsync(Checkpoint checkpoint)
    {
        if (checkpoint.LastProcessedLsn == null)
        {
            return;
        }

        await using var connection =
            new SqlConnection(configuration.GetConnectionString(options.Value.ConnectionStringName));

        await connection.ExecuteAsync(@$"
            MERGE cdc.CdcCheckpoints AS target
            USING (SELECT @TableName as TableName, @LastProcessedLsn as LastProcessedLsn) AS source
            ON target.TableName = source.TableName
            WHEN MATCHED THEN UPDATE SET 
                LastProcessedLsn = source.LastProcessedLsn,
                UpdatedAt = GETUTCDATE()
            WHEN NOT MATCHED THEN INSERT (TableName, LastProcessedLsn, UpdatedAt) 
                VALUES (source.TableName, source.LastProcessedLsn, GETUTCDATE());",
            new { checkpoint.TableName, checkpoint.LastProcessedLsn });
    }

    public async Task<Checkpoint> GetLastCheckpointAsync(string tableName)
    {
        await using var connection =
            new SqlConnection(configuration.GetConnectionString(options.Value.ConnectionStringName));

        return await connection.QuerySingleOrDefaultAsync<Checkpoint>(
            $"SELECT Tablename, LastProcessedLsn FROM cdc.CdcCheckpoints WHERE TableName = '{tableName}'");
    }
}

public record Checkpoint(string TableName, byte[]? LastProcessedLsn);