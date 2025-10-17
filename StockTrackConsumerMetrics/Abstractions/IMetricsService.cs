using StockTrackCommonLib.Models;
using StockTrackConsumerMetrics.Models;

namespace StockTrackConsumerMetrics.Abstractions;

public interface IMetricsService
{
    void UpdateMetrics(StockMessage message);
    void CreateNewWindowIfExpired();
    string GetAllMetricsAsJson();
}
