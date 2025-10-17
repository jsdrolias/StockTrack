using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockTrackCommonLib.Models;
using StockTrackConsumerWriter.Abstractions;
using StockTrackConsumerWriter.Options;

namespace StockTrackConsumerWriter.Workers;

public class ConsumerWriterWorker : BackgroundService
{
    private readonly ILogger<ConsumerWriterWorker> _logger;
    private readonly IKafkaConsumerService _kafkaConsumer;
    private readonly IFileWriterService _fileWriter;
    private long _messagesProcessed = 0;
    private readonly int _batchSize;

    public ConsumerWriterWorker(
        ILogger<ConsumerWriterWorker> logger,
        IKafkaConsumerService kafkaConsumer,
        IFileWriterService fileWriter,
        IOptions<ConsumerOptions> options)
    {
        _logger = logger;
        _kafkaConsumer = kafkaConsumer;
        _fileWriter = fileWriter;
        _batchSize = options.Value.BatchSize;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Batch Consumer Writer Worker started (batch size: {BatchSize})", _batchSize);

        try
        {
            var batch = new List<StockMessage>();

            while (!stoppingToken.IsCancellationRequested)
            {
                var message = await _kafkaConsumer.ConsumeAsync(stoppingToken);

                if (message != null)
                {
                    batch.Add(message);

                    // When batch is full, process all at once
                    if (batch.Count >= _batchSize)
                    {
                        await _fileWriter.WriteMessagesAsync(batch, stoppingToken);

                        _kafkaConsumer.Commit(); // Single commit for whole batch

                        _messagesProcessed += batch.Count;
                        _logger.LogInformation("Processed batch of {Count} messages. Total: {Total}",
                            batch.Count, _messagesProcessed);

                        batch.Clear();
                    }
                }

                // Small delay
                await Task.Delay(10, stoppingToken);
            }

            // Process remaining messages
            if (batch.Count > 0)
            {
                await _fileWriter.WriteMessagesAsync(batch, stoppingToken);
                _kafkaConsumer.Commit();
                _messagesProcessed += batch.Count;
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Batch Consumer Writer Worker stopping gracefully...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in Batch Consumer Writer Worker");
            throw;
        }
        finally
        {
            await _fileWriter.FlushAsync();
            _kafkaConsumer.Close();
            _logger.LogInformation("Batch Consumer Writer Worker stopped. Total: {Count}", _messagesProcessed);
        }
    }
}
