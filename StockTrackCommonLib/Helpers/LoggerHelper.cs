using Serilog;
using Serilog.Core;

namespace StockTrackCommonLib.Helpers;

public static class LoggerHelper
{
    private const string _output = "/app/output";
    private static string _instanceId = Guid.NewGuid().ToString("N")[..8];

    public static Logger GetLogger(string filename)
    {
        Directory.CreateDirectory(_output);
        var logger = new LoggerConfiguration()
                        .MinimumLevel.Debug()
                        .WriteTo.Console()
                        .WriteTo.File(
                            path: GetFullFilePath(filename),
                            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fffffff} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                            shared: true)
                        .CreateLogger();
        return logger;
    }

    public static string GetFullFilePath(string filename, string extension = "txt")
    {
        var filePath = Path.Combine(_output, $"{filename}-{_instanceId}.{extension}");
        return filePath;
    }
}
