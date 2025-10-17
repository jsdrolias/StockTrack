using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockTrackConsumerMetrics.Abstractions;
using StockTrackConsumerMetrics.Options;

namespace StockTrackConsumerMetrics.Workers;

public class ConsumerMetricsWorker : BackgroundService
{
    private readonly ILogger<ConsumerMetricsWorker> _logger;
    private readonly IKafkaConsumerService _kafkaConsumer;
    private readonly IMetricsService _metricsService;
    private readonly IMetricsReportingService _reportingService;
    private long _messagesProcessed = 0;
    private int _outputIntervalSeconds;

    public ConsumerMetricsWorker(
        ILogger<ConsumerMetricsWorker> logger,
        IKafkaConsumerService kafkaConsumer,
        IMetricsService metricsService,
        IMetricsReportingService reportingService,
        IOptions<ConsumerOptions> options)
    {
        _logger = logger;
        _kafkaConsumer = kafkaConsumer;
        _metricsService = metricsService;
        _reportingService = reportingService;
        _outputIntervalSeconds = options.Value.OutputIntervalSeconds;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Consumer Metrics Worker started");

        // Start background reporting task
        var reportingTask = Task.Run(() => PeriodicReportingAsync(stoppingToken), stoppingToken);


        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _metricsService.CreateNewWindowIfExpired();
                var message = await _kafkaConsumer.ConsumeAsync(stoppingToken);

                if (message != null)
                {
                    // Update metrics
                    _metricsService.UpdateMetrics(message);

                    // Commit offset
                    _kafkaConsumer.Commit();

                    _messagesProcessed++;
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Consumer Metrics Worker stopping gracefully...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in Consumer Metrics Worker");
            throw;
        }
        finally
        {
            // Final report and save
            await _reportingService.SaveMetricsToFileAsync(stoppingToken);

            _kafkaConsumer.Close();
            _logger.LogInformation("Consumer Metrics Worker stopped. Total messages processed: {Count}", _messagesProcessed);
        }

        await reportingTask;
    }

    private async Task PeriodicReportingAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Save metrics to file periodically
                await Task.Delay(TimeSpan.FromSeconds(_outputIntervalSeconds), cancellationToken);
                await _reportingService.SaveMetricsToFileAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in periodic reporting");
            }
        }
    }
}
