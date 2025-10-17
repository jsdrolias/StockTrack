using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using StockTrackCommonLib.Constants;
using StockTrackCommonLib.Helpers;
using StockTrackConsumerMetrics.Abstractions;
using StockTrackConsumerMetrics.Options;
using StockTrackConsumerMetrics.Services;
using StockTrackConsumerMetrics.Workers;

namespace StockTrackConsumerMetrics;

class Program
{
    static async Task Main(string[] args)
    {
        var fileNameIdentifier = $"stock-consumer-metrics-logs";
        Log.Logger = LoggerHelper.GetLogger(fileNameIdentifier);

        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Register configuration
                services.Configure<ConsumerOptions>(options =>
                {
                    options.ConsumerGroupId = Environment.GetEnvironmentVariable("CONSUMER_GROUP_ID") ?? "stock-metrics-group";
                    options.SlidingWindowSeconds = int.Parse(Environment.GetEnvironmentVariable("SLIDING_WINDOW_SECONDS") ?? "60");
                    options.OutputIntervalSeconds = int.TryParse(Environment.GetEnvironmentVariable("OUTPUT_INTERVAL_SECONDS"), out var interval) ? interval : 6;
                });

                services.AddSerilog();

                // Register services
                services.AddSingleton<IMetricsService, MetricsService>();
                services.AddSingleton<IMetricsReportingService, MetricsReportingService>();
                services.AddSingleton<IKafkaConsumerService, KafkaConsumerService>();

                // Register hosted service
                services.AddHostedService<ConsumerMetricsWorker>();
            })
            .Build();

        await host.RunAsync();
    }
}