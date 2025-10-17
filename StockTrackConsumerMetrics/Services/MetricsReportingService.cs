using Microsoft.Extensions.Logging;
using StockTrackCommonLib.Helpers;
using StockTrackConsumerMetrics.Abstractions;

namespace StockTrackConsumerMetrics.Services;

public class MetricsReportingService : IMetricsReportingService
{
    private readonly ILogger<MetricsReportingService> _logger;
    private readonly IMetricsService _metricsService;

    public MetricsReportingService(
        ILogger<MetricsReportingService> logger,
        IMetricsService metricsService)
    {
        _logger = logger;
        _metricsService = metricsService;
    }

    public async Task SaveMetricsToFileAsync(CancellationToken cancellationToken = default)
    {
        var metricsJson = _metricsService.GetAllMetricsAsJson();

        var metricsFile = LoggerHelper.GetFullFilePath("metrics");

        await File.WriteAllTextAsync(metricsFile, metricsJson, cancellationToken);

        _logger.LogInformation("Metrics saved to {File}", metricsFile);
    }
}
