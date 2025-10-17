using System.Collections.Concurrent;

namespace StockTrackConsumerMetrics.Models;

public class StockTotalMetrics
{
    public long TotalMessages { get; set; } = 0;
    public ConcurrentDictionary<string, StockSpecificMetrics> StockSpecificMetrics { get; set; } = new();
}
