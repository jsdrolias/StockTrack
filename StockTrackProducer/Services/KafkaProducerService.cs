using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockTrackCommonLib.Constants;
using StockTrackCommonLib.Models;
using StockTrackProducer.Abstractions;
using StockTrackProducer.Options;
using System.Text.Json;

namespace StockTrackProducer.Services;

public class KafkaProducerService : IKafkaProducerService, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducerService> _logger;
    private readonly string _topic = KafkaConstants.Topic;

    public KafkaProducerService(
        IOptions<StockProducerOptions> options,
        ILogger<KafkaProducerService> logger)
    {
        _logger = logger;

        var config = new ProducerConfig
        {
            BootstrapServers = KafkaConstants.BootstrapServers,
            ClientId = $"producer-{options.Value.StockSign}",
            Partitioner = Partitioner.ConsistentRandom,
            Acks = Acks.All,
            LingerMs = 10,
            CompressionType = CompressionType.Snappy,
            EnableIdempotence = true
        };

        _producer = new ProducerBuilder<string, string>(config).Build();

        _logger.LogInformation("Kafka producer initialized for bootstrap servers: {Servers}",
            KafkaConstants.BootstrapServers);
    }

    public async Task<int> PublishMessageAsync(StockMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            var messageJson = JsonSerializer.Serialize(message);

            var kafkaMessage = new Message<string, string>
            {
                Key = message.Stock,
                Value = messageJson
            };

            var result = await _producer.ProduceAsync(_topic, kafkaMessage, cancellationToken);

            return result.Partition.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing message for stock {Stock}", message.Stock);
            throw;
        }
    }

    public void Dispose()
    {
        _producer?.Flush(TimeSpan.FromSeconds(10));
        _producer?.Dispose();
        _logger.LogInformation("Kafka producer disposed");
    }
}