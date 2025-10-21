using CCStudio.Tunneler.Core.Models;
using Serilog;
using Serilog.Events;

namespace CCStudio.Tunneler.Core.Utilities;

/// <summary>
/// Factory for creating and configuring Serilog loggers
/// </summary>
public static class LoggerFactory
{
    /// <summary>
    /// Create a logger with the specified configuration
    /// </summary>
    public static ILogger CreateLogger(LoggingConfiguration config)
    {
        var logLevel = ParseLogLevel(config.Level);

        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Is(logLevel);

        // Add console sink if enabled
        if (config.EnableConsole)
        {
            loggerConfig.WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");
        }

        // Add file sink
        var logPath = Path.Combine(config.Path, "ccstudio-tunneler-.log");
        loggerConfig.WriteTo.File(
            logPath,
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: config.RetentionDays,
            fileSizeLimitBytes: config.MaxFileSizeMB * 1024 * 1024,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}");

        return loggerConfig.CreateLogger();
    }

    /// <summary>
    /// Parse log level string to LogEventLevel
    /// </summary>
    private static LogEventLevel ParseLogLevel(string level)
    {
        return level?.ToLower() switch
        {
            "verbose" => LogEventLevel.Verbose,
            "debug" => LogEventLevel.Debug,
            "information" => LogEventLevel.Information,
            "warning" => LogEventLevel.Warning,
            "error" => LogEventLevel.Error,
            "fatal" => LogEventLevel.Fatal,
            _ => LogEventLevel.Information
        };
    }

    /// <summary>
    /// Ensure log directory exists
    /// </summary>
    public static void EnsureLogDirectoryExists(string logPath)
    {
        if (!Directory.Exists(logPath))
        {
            Directory.CreateDirectory(logPath);
        }
    }
}
