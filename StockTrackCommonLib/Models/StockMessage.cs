namespace StockTrackCommonLib.Models;

public class StockMessage
{
    public string Stock { get; set; } = string.Empty;
    public long Index { get; set; }
    public double Price { get; set; }
    public DateTime Timestamp { get; set; }
}
