using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TbdDevelop.CDC.Extensions.Contracts;
using TbdDevelop.CDC.Extensions.Infrastructure;

namespace TbdDevelop.CDC.Extensions.Services;

public class ChangeMonitoringService(
    IServiceScopeFactory factory,
    IConfiguration configuration,
    ILogger<ChangeMonitoringService> logger) : BackgroundService
{
    private readonly IServiceProvider _serviceProvider = factory.CreateScope().ServiceProvider;

    private ICdcRepository _repository = null!;
    private ICheckpointRepository _checkpoints = null!;
    private IOptions<MonitoringOptions> _options = null!;
    private IChangePublisher _changePublisher = null!;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Initialize();

        await StartMonitoring(stoppingToken);
    }

    private async Task StartMonitoring(CancellationToken cancellationToken)
    {
        var toMonitor = _options.Value.Tables.Select(table =>
            new ChangeMonitor(table,
                configuration.GetConnectionString(_options.Value.ConnectionStringName)!,
                ProcessChangesAsync)).ToList();

        logger.LogInformation("{Count} monitors loaded", toMonitor.Count);

        var monitors =
            toMonitor.Select(monitor =>
                    Task.Run(() => monitor.StartMonitoringAsync(cancellationToken), cancellationToken))
                .ToList();

        logger.LogInformation("{Count} monitors started", monitors.Count);

        await Task.WhenAll(monitors);
    }

    private async Task<byte[]> ProcessChangesAsync(string tableName,
        byte[] currentMaxLsn,
        CancellationToken stoppingToken = default)
    {
        var fromLsn = await GetStartingLsn(tableName);

        var changes = (await _repository.GetChangesAsync(
            tableName,
            fromLsn,
            currentMaxLsn)).ToList();

        logger.LogDebug("Processing {TableName} for {FromLsn} to {CurrentMaxLsn}", tableName,
            fromLsn.AsReadableString(),
            currentMaxLsn.AsReadableString());

        foreach (var change in changes.Where(c => c.Operation != CdcOperation.UpdateBefore))
        {
            logger.LogDebug("Processing Change for {TableName} {Lsn}", tableName,
                change.LogSequenceNumber.AsReadableString());

            await _changePublisher.PublishAsync(new EntityChange(
                    change.Operation.AsChangeOperation(),
                    change.Data.ToObject(),
                    tableName),
                stoppingToken);
        }

        await _checkpoints.SaveCheckpointAsync(new Checkpoint(tableName, currentMaxLsn));

        logger.LogDebug("Processed {Count} Changes", changes.Count);

        return currentMaxLsn;
    }

    private async Task<byte[]> GetStartingLsn(string tableName)
    {
        var checkpoint = await _checkpoints.GetLastCheckpointAsync(tableName);

        if (checkpoint is { LastProcessedLsn: not null })
        {
            return checkpoint.LastProcessedLsn;
        }

        var minLsn = await _repository.GetMinLsnAsync(tableName);

        return minLsn ?? new byte[10];
    }

    private void Initialize()
    {
        logger.LogInformation("Initializing Monitoring Service");

        _repository = _serviceProvider.GetRequiredService<ICdcRepository>();
        _checkpoints = _serviceProvider.GetRequiredService<ICheckpointRepository>();
        _options = _serviceProvider.GetRequiredService<IOptions<MonitoringOptions>>();
        _changePublisher = _serviceProvider.GetRequiredService<IChangePublisher>();
    }
}