using StockTrackCommonLib.Models;

namespace StockTrackConsumerMetrics.Abstractions;

public interface IKafkaConsumerService
{
    Task<StockMessage?> ConsumeAsync(CancellationToken cancellationToken);
    void Commit();
    void Close();
}
