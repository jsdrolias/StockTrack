using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using StockTrackCommonLib.Constants;
using StockTrackCommonLib.Helpers;
using StockTrackConsumerWriter.Abstractions;
using StockTrackConsumerWriter.Options;
using StockTrackConsumerWriter.Services;
using StockTrackConsumerWriter.Workers;

namespace StockTrackConsumerWriter;

class Program
{
    static async Task Main(string[] args)
    {
        var fileNameIdentifier = $"stock-consumer-writer-logs";
        Log.Logger = LoggerHelper.GetLogger(fileNameIdentifier);

        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Register configuration
                services.Configure<ConsumerOptions>(options =>
                {
                    options.ConsumerGroupId = Environment.GetEnvironmentVariable("CONSUMER_GROUP_ID") ?? "stock-writer-group";
                    options.BatchSize = int.TryParse(Environment.GetEnvironmentVariable("BATCH_SIZE"), out var batchSize) ? batchSize : 100;
                });

                services.AddSerilog();

                // Register services
                services.AddSingleton<IFileWriterService, FileWriterService>();
                services.AddSingleton<IKafkaConsumerService, KafkaConsumerService>();

                // Register hosted service
                services.AddHostedService<ConsumerWriterWorker>();
            })
            .Build();

        await host.RunAsync();
    }
}