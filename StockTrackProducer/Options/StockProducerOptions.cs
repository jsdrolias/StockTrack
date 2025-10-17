namespace StockTrackProducer.Options;

public class StockProducerOptions
{
    public string StockSign { get; set; } = "AAPL";
    public double StartingPrice { get; set; } = 100.0;
    public int HeartbeatMs { get; set; } = 1000;
    public double ProbabilityUp { get; set; } = 0.5;
    public int MaxChangePercent { get; set; } = 10;
}
