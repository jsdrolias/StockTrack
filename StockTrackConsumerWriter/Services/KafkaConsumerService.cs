using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockTrackCommonLib.Constants;
using StockTrackCommonLib.Models;
using StockTrackConsumerWriter.Abstractions;
using StockTrackConsumerWriter.Options;
using System.Text.Json;

namespace StockTrackConsumerWriter.Services;

public class KafkaConsumerService : IKafkaConsumerService, IDisposable
{
    private readonly IConsumer<string, string> _consumer;
    private readonly ILogger<KafkaConsumerService> _logger;
    private ConsumeResult<string, string>? _lastResult;

    public KafkaConsumerService(
        ILogger<KafkaConsumerService> logger,
        IOptions<ConsumerOptions> options)
    {
        _logger = logger;

        var config = new ConsumerConfig
        {
            BootstrapServers = KafkaConstants.BootstrapServers,
            GroupId = options.Value.ConsumerGroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            MaxPollIntervalMs = 300000,
            SessionTimeoutMs = 10000
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
        _consumer.Subscribe(KafkaConstants.Topic);

        _logger.LogInformation(
            "Kafka consumer initialized. Group: {GroupId}, Topic: {Topic}",
            options.Value.ConsumerGroupId,
            KafkaConstants.Topic);
    }

    public async Task<StockMessage?> ConsumeAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = _consumer.Consume(TimeSpan.FromSeconds(1));

            if (result == null)
            {
                return null;
            }

            _lastResult = result;

            var message = JsonSerializer.Deserialize<StockMessage>(result.Message.Value);

            if (message != null)
            {
                _logger.LogInformation(
                    "Consumed message - Stock: {Stock}, Index: {Index}, Price: ${Price}, Partition: {Partition}, Offset: {Offset}",
                    message.Stock, message.Index, message.Price, result.Partition.Value, result.Offset.Value);
            }

            return message;
        }
        catch (ConsumeException ex)
        {
            _logger.LogError(ex, "Error consuming message from Kafka");
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error deserializing message");
            return null;
        }
    }

    public void Commit()
    {
        if (_lastResult != null)
        {
            _consumer.Commit(_lastResult);
            _lastResult = null;
        }
    }

    public void Close()
    {
        _consumer.Close();
        _logger.LogInformation("Kafka consumer closed");
    }

    public void Dispose()
    {
        _consumer?.Dispose();
    }
}
