using Microsoft.Extensions.Logging;
using StockTrackCommonLib.Helpers;
using StockTrackCommonLib.Models;
using StockTrackConsumerWriter.Abstractions;
using System.Text;

namespace StockTrackConsumerWriter.Services;

public class FileWriterService : IFileWriterService, IDisposable
{
    private readonly ILogger<FileWriterService> _logger;
    private readonly StreamWriter _writer;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private long _messageCount = 0;

    public FileWriterService(ILogger<FileWriterService> logger)
    {
        _logger = logger;
        var fileName = LoggerHelper.GetFullFilePath("stock-data");
        _writer = new StreamWriter(fileName, append: true);

        _logger.LogInformation("FileWriterService initialized. File: {FileName}", fileName);
    }

    public async Task WriteMessagesAsync(IEnumerable<StockMessage> messages, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var sb = new StringBuilder();
            var count = 0;

            foreach (var message in messages)
            {
                if (count > 0)
                {
                    sb.AppendLine();
                }
                sb.Append($"{message.Timestamp:yyyy-MM-dd HH:mm:ss.fffffff} | {message.Stock} | ${message.Price:F2}");
                count++;
            }

            await _writer.WriteAsync(sb.ToString());
            await _writer.WriteAsync(Environment.NewLine);
            await _writer.FlushAsync(cancellationToken);

            _messageCount += count;

            _logger.LogInformation("Written batch of {BatchCount} messages. Total: {Total}", count, _messageCount);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task FlushAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            await _writer.FlushAsync();
            _logger.LogInformation("Final flush. Total messages written: {Count}", _messageCount);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Dispose()
    {
        try
        {
            // Final flush before disposal
            _writer?.Flush();
            _logger.LogInformation("FileWriterService disposed. Total messages: {Count}", _messageCount);
        }
        finally
        {
            _writer?.Dispose();
            _semaphore?.Dispose();
        }
    }
}
