using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TbdDevelop.CDC.Extensions.Contracts;
using TbdDevelop.CDC.Extensions.Infrastructure;
using TbdDevelop.CDC.Extensions.Infrastructure.Publishing;
using TbdDevelop.CDC.Extensions.Services;

namespace TbdDevelop.CDC.Extensions;

public static class ApplicationHostBuilderExtensions
{
    public static TBuilder AddChangeMonitoring<TBuilder>(this TBuilder builder,
        Action<ChangeHandlingConfiguration>? configure = null)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services.Configure<MonitoringOptions>(builder.Configuration.GetSection("monitoring"));

        if (configure == null)
        {
            return builder;
        }

        var configuration = new ChangeHandlingConfiguration(builder.Services);

        configure(configuration);

        builder.Services.AddScoped<ICdcRepository, CdcRepository>();
        builder.Services.AddScoped<ICheckpointRepository, CheckpointRepository>();

        builder.Services.AddHostedService<ChangeMonitoringService>();

        return builder;
    }
}