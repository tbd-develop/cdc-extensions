namespace TbdDevelop.CDC.Extensions.Infrastructure;

public class CdcChangeRecord
{
    public CdcOperation Operation { get; set; }
    public required byte[] LogSequenceNumber { get; set; }
    public byte[]? UpdateMask { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}