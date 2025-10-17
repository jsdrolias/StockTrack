using Microsoft.Extensions.Logging;
using StockTrackProducer.Abstractions;

namespace StockTrackProducer.Services;

public class StockPriceGenerator : IStockPriceGenerator
{
    private readonly Random _random;
    private readonly ILogger<StockPriceGenerator> _logger;

    public StockPriceGenerator(ILogger<StockPriceGenerator> logger)
    {
        _logger = logger;
        _random = new Random();
    }

    public double GenerateNextPrice(double currentPrice, double probabilityUp, int maxChangePercent)
    {
        var directionRandomValue = _random.NextDouble();
        var directionIsUp = directionRandomValue < probabilityUp;
        var randomChange = _random.NextDouble() * maxChangePercent;

        double newPrice;
        if (directionIsUp)
        {
            newPrice = currentPrice + (currentPrice * randomChange / 100);
        }
        else
        {
            newPrice = currentPrice - (currentPrice * randomChange / 100);
        }

        // Ensure price never goes negative
        if (newPrice < 0)
        {
            _logger.LogWarning(
                "Price calculation resulted in negative value ({NewPrice}). Setting to 0.01",
                newPrice);
            newPrice = 0.01; // Minimum price
        }

        // Round to 2 decimal places
        return Math.Round(newPrice, 2);
    }
}
