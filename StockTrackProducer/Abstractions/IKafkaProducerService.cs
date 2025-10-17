using StockTrackCommonLib.Models;

namespace StockTrackProducer.Abstractions;

public interface IKafkaProducerService
{
    Task<int> PublishMessageAsync(StockMessage message, CancellationToken cancellationToken = default);
    void Dispose();
}
