namespace StockTrackConsumerMetrics.Models;

public class StockMinMaxPrice
{
    public double MinPrice { get; set; } = double.MaxValue;
    public double MaxPrice { get; set; } = double.MinValue;
}
