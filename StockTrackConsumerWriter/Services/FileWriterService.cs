using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockTrackCommonLib.Helpers;
using StockTrackCommonLib.Models;
using StockTrackConsumerWriter.Abstractions;
using StockTrackConsumerWriter.Options;
using System.Text;

namespace StockTrackConsumerWriter.Services;

public class FileWriterService : IFileWriterService, IDisposable
{
    private readonly ILogger<FileWriterService> _logger;
    private readonly StreamWriter _writer;
    private readonly List<string> _batch = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private long _messageCount = 0;

    public FileWriterService(
        ILogger<FileWriterService> logger)
    {
        _logger = logger;

        var fileName = LoggerHelper.GetFullFilePath("stock-data");
        _writer = new StreamWriter(fileName, append: true);
    }

    public async Task WriteMessagesAsync(IEnumerable<StockMessage> messages, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            // Build entire batch as a single string
            var sb = new StringBuilder();
            var count = 0;

            foreach (var message in messages)
            {
                if (count > 0)
                {
                    sb.AppendLine(); // Add newline between messages
                }

                sb.Append($"{message.Timestamp:yyyy-MM-dd HH:mm:ss.fffffff} | {message.Stock} | ${message.Price:F2}");
                count++;
            }

            await _writer.WriteAsync(sb.ToString());
            await _writer.WriteAsync(Environment.NewLine);
            await _writer.FlushAsync();

            _messageCount += count;

            _logger.LogInformation("Written batch of {BatchCount} messages. Total: {Total}", count, _messageCount);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task FlushBatchAsync()
    {
        if (_batch.Count == 0)
            return;

        foreach (var line in _batch)
        {
            await _writer.WriteLineAsync(line);
        }
        await _writer.FlushAsync();

        _logger.LogInformation("Flushed batch of {Count} messages. Total: {Total}",
            _batch.Count, _messageCount);

        _batch.Clear();
    }

    public async Task FlushAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            await FlushBatchAsync();
            _logger.LogInformation("Final flush. Total messages written: {Count}", _messageCount);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Dispose()
    {
        FlushBatchAsync().Wait();
        _writer?.Dispose();
        _semaphore?.Dispose();
        _logger.LogInformation("FileWriterBatchService disposed");
    }
}
