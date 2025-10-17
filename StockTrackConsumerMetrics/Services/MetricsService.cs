using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockTrackCommonLib.Models;
using StockTrackConsumerMetrics.Abstractions;
using StockTrackConsumerMetrics.Models;
using StockTrackConsumerMetrics.Options;
using System.Collections.Concurrent;
using System.Text.Json;

namespace StockTrackConsumerMetrics.Services;

public class MetricsService : IMetricsService
{
    private readonly ILogger<MetricsService> _logger;
    private readonly int _windowSeconds;
    private StockTotalMetrics _stockTotalMetrics = new();
    private DateTime _currentWindowStart;
    private ConcurrentDictionary<string, StockSlidingWindowMetrics> _currentStockSlidingWindowMetrics = new();

    public MetricsService(
        ILogger<MetricsService> logger,
        IOptions<ConsumerOptions> options)
    {
        _logger = logger;
        _windowSeconds = options.Value.SlidingWindowSeconds;
        _currentWindowStart = DateTime.UtcNow;
    }

    public void UpdateMetrics(StockMessage message)
    {
        _stockTotalMetrics.TotalMessages++;
        UpdateMinMaxPrice(message.Stock, message.Price);
        UpdateCurrentSlidingWindow(message.Stock, message.Price);
    }

    public void CreateNewWindowIfExpired()
    {
        var _currentWindowEnd = DateTime.UtcNow;
        if ((_currentWindowEnd - _currentWindowStart).TotalSeconds < _windowSeconds)
        {
            return;
        }

        if (_currentStockSlidingWindowMetrics.Count > 0)
        {
            foreach (var kvp in _currentStockSlidingWindowMetrics)
            {
                var stock = kvp.Key;
                var stockMetrics = kvp.Value;

                stockMetrics.WindowStart = _currentWindowStart;
                stockMetrics.WindowEnd = _currentWindowEnd;

                _stockTotalMetrics.StockSpecificMetrics[stock].StockSlidingWindowMetrics.Add(stockMetrics);
            }
            _currentStockSlidingWindowMetrics.Clear();
        }
        _currentWindowStart = _currentWindowEnd;
    }

    public string GetAllMetricsAsJson()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        return JsonSerializer.Serialize(_stockTotalMetrics, options);
    }

    private void UpdateMinMaxPrice(string stock, double price)
    {
        var stockExistsInMetric = _stockTotalMetrics.StockSpecificMetrics.ContainsKey(stock);
        if (stockExistsInMetric)
        {
            var stockMinMaxPrice = _stockTotalMetrics.StockSpecificMetrics[stock].StockMinMaxPrice;
            if (price < stockMinMaxPrice.MinPrice)
            {
                stockMinMaxPrice.MinPrice = price;
            }

            if (price > stockMinMaxPrice.MaxPrice)
            {
                stockMinMaxPrice.MaxPrice = price;
            }
        }
        else
        {
            _stockTotalMetrics.StockSpecificMetrics.TryAdd(
                stock,
                new StockSpecificMetrics
                {
                    StockMinMaxPrice = new StockMinMaxPrice
                    {
                        MinPrice = price,
                        MaxPrice = price
                    }
                });
        }
    }

    private void UpdateCurrentSlidingWindow(string stock, double price)
    {
        var stockExistsInWindow = _currentStockSlidingWindowMetrics.ContainsKey(stock);
        if (stockExistsInWindow)
        {
            var stockValues = _currentStockSlidingWindowMetrics[stock];
            if (price < stockValues.LowPrice)
            {
                stockValues.LowPrice = price;
            }

            if (price > stockValues.HighPrice)
            {
                stockValues.HighPrice = price;
            }

            stockValues.ClosePrice = price;
        }
        else
        {
            _currentStockSlidingWindowMetrics.TryAdd(
                stock,
                new StockSlidingWindowMetrics
                {
                    OpenPrice = price,
                    ClosePrice = price,
                    LowPrice = price,
                    HighPrice = price
                });
        }
    }
}
