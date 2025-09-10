using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using TbdDevelop.CDC.Extensions.Contracts;

namespace TbdDevelop.CDC.Extensions.Infrastructure;

public class CdcRepository(
    IConfiguration configuration,
    IOptions<MonitoringOptions> options) : ICdcRepository
{
    public async Task<IEnumerable<CdcChangeRecord>> GetChangesAsync(
        string tableName,
        byte[] fromLsn,
        byte[] toLsn)
    {
        await using var connection =
            new SqlConnection(configuration.GetConnectionString(options.Value.ConnectionStringName));

        var sql = $@"
            SELECT 
                __$operation AS __Operation,
                __$seqval AS __SequenceValue,
                __$update_mask AS __UpdateMask,
                *
            FROM cdc.fn_cdc_get_all_changes_{tableName}(@from_lsn, @to_lsn, 'all')
            ORDER BY __$seqval";

        var changes = await connection.QueryAsync<dynamic>(sql, new
        {
            from_lsn = fromLsn,
            to_lsn = toLsn
        });

        return changes.Select(MapToCdcRecord);
    }

    public async Task<byte[]?> GetMinLsnAsync(string tableName)
    {
        await using var connection =
            new SqlConnection(configuration.GetConnectionString(options.Value.ConnectionStringName));

        var result = await connection.QuerySingleOrDefaultAsync<byte[]?>(
            $"SELECT sys.fn_cdc_get_min_lsn('{tableName}')");

        return result;
    }

    private CdcChangeRecord MapToCdcRecord(dynamic row)
    {
        var record = new CdcChangeRecord
        {
            Operation = (CdcOperation)row.__Operation,
            LogSequenceNumber = row.__SequenceValue,
            UpdateMask = row.__UpdateMask
        };

        var properties = (IDictionary<string, object>)row;

        foreach (var prop in properties)
        {
            if (!IsCdcSystemColumn(prop.Key))
            {
                record.Data[prop.Key] = prop.Value;
            }
        }

        return record;
    }

    private static bool IsCdcSystemColumn(string columnName)
    {
        return columnName.StartsWith("__");
    }
}