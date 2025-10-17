using StockTrackCommonLib.Models;

namespace StockTrackConsumerWriter.Abstractions;

public interface IKafkaConsumerService
{
    Task<StockMessage?> ConsumeAsync(CancellationToken cancellationToken);
    void Commit();
    void Close();
}
