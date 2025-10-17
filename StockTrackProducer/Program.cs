using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using StockTrackCommonLib.Helpers;
using StockTrackProducer.Abstractions;
using StockTrackProducer.Options;
using StockTrackProducer.Services;
using StockTrackProducer.Workers;

namespace StockProducer;

class Program
{
    static async Task Main(string[] args)
    {
        var fileNameIdentifier = $"stock-producer-logs";
        Log.Logger = LoggerHelper.GetLogger(fileNameIdentifier);

        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Register configuration
                services.Configure<StockProducerOptions>(options =>
                {
                    options.StockSign = Environment.GetEnvironmentVariable("STOCK_SIGN")!;
                    options.StartingPrice = double.Parse(Environment.GetEnvironmentVariable("STARTING_PRICE")!);
                    options.HeartbeatMs = int.Parse(Environment.GetEnvironmentVariable("HEARTBEAT_MS")!);
                    options.ProbabilityUp = double.Parse(Environment.GetEnvironmentVariable("PROBABILITY_UP")!);
                    options.MaxChangePercent = int.Parse(Environment.GetEnvironmentVariable("MAX_CHANGE_PERCENT")!);
                });

                services.AddSerilog();

                // Register services
                services.AddSingleton<IStockPriceGenerator, StockPriceGenerator>();
                services.AddSingleton<IKafkaProducerService, KafkaProducerService>();

                // Register hosted service
                services.AddHostedService<StockProducerWorker>();
            })
            .Build();

        await host.RunAsync();
    }
}