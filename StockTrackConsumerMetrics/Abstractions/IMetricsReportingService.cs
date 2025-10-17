namespace StockTrackConsumerMetrics.Abstractions;

public interface IMetricsReportingService
{
    Task SaveMetricsToFileAsync(CancellationToken cancellationToken = default);
}
