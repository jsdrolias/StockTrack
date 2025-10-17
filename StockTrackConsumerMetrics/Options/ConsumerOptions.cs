namespace StockTrackConsumerMetrics.Options;

public class ConsumerOptions
{
    public string ConsumerGroupId { get; set; } = "stock-metrics-group";
    public int SlidingWindowSeconds { get; set; } = 60;
    public int OutputIntervalSeconds { get; set; } = 6;
}
