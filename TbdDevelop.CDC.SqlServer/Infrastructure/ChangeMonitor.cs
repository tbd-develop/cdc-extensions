using Dapper;
using Microsoft.Data.SqlClient;

namespace TbdDevelop.CDC.Extensions.Infrastructure;

public class ChangeMonitor(
    string tableName,
    string connectionString,
    Func<string, byte[], CancellationToken, Task<byte[]>> onChangesDetected)
{
    public async Task StartMonitoringAsync(CancellationToken cancellationToken = default)
    {
        await WatchForChangesAsync(cancellationToken);
    }

    private async Task WatchForChangesAsync(CancellationToken cancellationToken)
    {
        byte[]? lastKnownMaxLsn = null;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync(cancellationToken);

                var currentMaxLsn = await connection.QuerySingleOrDefaultAsync<byte[]?>(
                    $"SELECT MAX(__$seqval) as MaxLsn FROM cdc.{tableName}_CT");

                if (currentMaxLsn != null && !lastKnownMaxLsn.AreLsnsEqual(currentMaxLsn))
                {  
                    lastKnownMaxLsn = await onChangesDetected.Invoke(tableName, currentMaxLsn, cancellationToken);
                }

                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception)
            {
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            }
        }
    }
}