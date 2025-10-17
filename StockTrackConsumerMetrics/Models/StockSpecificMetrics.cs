namespace StockTrackConsumerMetrics.Models;

public class StockSpecificMetrics
{
    public StockMinMaxPrice StockMinMaxPrice { get; set; } = new();
    public List<StockSlidingWindowMetrics> StockSlidingWindowMetrics { get; set; } = new();
}
