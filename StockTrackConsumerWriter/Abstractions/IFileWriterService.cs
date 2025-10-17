using StockTrackCommonLib.Models;

namespace StockTrackConsumerWriter.Abstractions;

public interface IFileWriterService
{
    Task WriteMessagesAsync(IEnumerable<StockMessage> messages, CancellationToken cancellationToken = default);
    Task FlushAsync();
}
