namespace TbdDevelop.CDC.Extensions.Infrastructure;

public class MonitoringOptions
{
    public required IEnumerable<string> Tables { get; set; }
    public required string ConnectionStringName { get; set; }
}
