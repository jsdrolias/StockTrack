using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockTrackCommonLib.Models;
using StockTrackProducer.Abstractions;
using StockTrackProducer.Options;

namespace StockTrackProducer.Workers;

public class StockProducerWorker : BackgroundService
{
    private readonly ILogger<StockProducerWorker> _logger;
    private readonly StockProducerOptions _options;
    private readonly IStockPriceGenerator _priceGenerator;
    private readonly IKafkaProducerService _kafkaProducer;
    private long _messageCount = 1;

    public StockProducerWorker(
        ILogger<StockProducerWorker> logger,
        IOptions<StockProducerOptions> options,
        IStockPriceGenerator priceGenerator,
        IKafkaProducerService kafkaProducer)
    {
        _logger = logger;
        _options = options.Value;
        _priceGenerator = priceGenerator;
        _kafkaProducer = kafkaProducer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting Stock Producer for {StockSign}", _options.StockSign);
        _logger.LogInformation(
            "Configuration - Starting Price: ${Price}, Heartbeat: {Heartbeat}ms, Probability Up: {Prob}, Max Change: {Max}%",
            _options.StartingPrice, _options.HeartbeatMs, _options.ProbabilityUp, _options.MaxChangePercent);

        double currentPrice = _options.StartingPrice;

        _logger.LogInformation("Producer connected to Kafka. Starting to publish messages...");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Generate next price
                currentPrice = _priceGenerator.GenerateNextPrice(
                    currentPrice,
                    _options.ProbabilityUp,
                    _options.MaxChangePercent);

                var timeStamp = DateTime.UtcNow;

                // Create message
                var message = new StockMessage
                {
                    Stock = _options.StockSign,
                    Index = _messageCount,
                    Price = currentPrice,
                    Timestamp = timeStamp
                };

                // Publish to Kafka
                var partition = await _kafkaProducer.PublishMessageAsync(message, stoppingToken);

                _logger.LogInformation(
                      "Published message #{Count} - {Stock}: ${Price} to partition {Partition} at {Timestamp}",
                      _messageCount, _options.StockSign, currentPrice, partition, timeStamp.ToString("yyyy-MM-dd HH:mm:ss.fffffff"));

                _messageCount++;

                // Wait for next heartbeat
                await Task.Delay(_options.HeartbeatMs, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Producer stopping gracefully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in producer worker");
            throw;
        }
        finally
        {
            _logger.LogInformation("Producer shutting down. Total messages sent: {Count}", _messageCount);
        }
    }

    public override void Dispose()
    {
        _kafkaProducer?.Dispose();
        base.Dispose();
    }
}
