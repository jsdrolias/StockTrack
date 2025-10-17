using StockTrackCommonLib.Constants;

namespace StockTrackConsumerWriter.Options;

public class ConsumerOptions
{
    public string ConsumerGroupId { get; set; } = "stock-writer-group";
    public int BatchSize { get; set; } = 100;
}
