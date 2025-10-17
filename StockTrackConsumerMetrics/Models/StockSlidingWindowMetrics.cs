namespace StockTrackConsumerMetrics.Models;

public class StockSlidingWindowMetrics
{
    public double OpenPrice { get; set; }
    public double ClosePrice { get; set; }
    public double HighPrice { get; set; } = double.MinValue;
    public double LowPrice { get; set; } = double.MaxValue;
    public DateTime WindowStart { get; set; }
    public DateTime WindowEnd { get; set; }
}
