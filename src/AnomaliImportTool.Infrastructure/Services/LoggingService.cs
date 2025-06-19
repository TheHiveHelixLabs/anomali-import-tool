using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Context;
using Serilog.Core;
using Serilog.Events;

namespace AnomaliImportTool.Infrastructure.Services
{
    /// <summary>
    /// Service for configuring and managing application logging
    /// </summary>
    public class LoggingService
    {
        private readonly string _logDirectory;
        private readonly LoggingLevelSwitch _levelSwitch;
        private Logger _logger;

        public LoggingService()
        {
            // Default to relative path in app directory for portability
            _logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            _levelSwitch = new LoggingLevelSwitch();
            
            ConfigureLogging();
        }

        public LoggingService(IConfiguration configuration)
        {
            // Allow configuration override but default to portable location
            _logDirectory = configuration["Logging:LogDirectory"] 
                ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            
            _levelSwitch = new LoggingLevelSwitch();
            
            // Set log level from configuration
            if (Enum.TryParse<LogEventLevel>(configuration["Logging:MinimumLevel"], out var level))
            {
                _levelSwitch.MinimumLevel = level;
            }
            else
            {
                _levelSwitch.MinimumLevel = LogEventLevel.Information;
            }
            
            ConfigureLogging();
        }

        /// <summary>
        /// Gets the configured logger instance
        /// </summary>
        public ILogger Logger => _logger ?? Serilog.Log.Logger;

        /// <summary>
        /// Configures Serilog with file sink and rotation
        /// </summary>
        private void ConfigureLogging()
        {
            // Ensure log directory exists
            Directory.CreateDirectory(_logDirectory);

            var logPath = Path.Combine(_logDirectory, "AnomaliImportTool-.log");

            _logger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(_levelSwitch)
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithEnvironmentUserName()
                .Enrich.WithProcessId()
                .Enrich.WithThreadId()
                .WriteTo.File(
                    logPath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    fileSizeLimitBytes: 10 * 1024 * 1024, // 10MB per file
                    rollOnFileSizeLimit: true,
                    shared: true,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            // Set as global logger if not already set
            if (Serilog.Log.Logger.GetType().Name == "SilentLogger")
            {
                Serilog.Log.Logger = _logger;
            }

            _logger.Information("Logging initialized. Log directory: {LogDirectory}", _logDirectory);
        }

        /// <summary>
        /// Changes the minimum log level dynamically
        /// </summary>
        public void SetLogLevel(LogEventLevel level)
        {
            _levelSwitch.MinimumLevel = level;
            _logger.Information("Log level changed to: {LogLevel}", level);
        }

        /// <summary>
        /// Gets the current log level
        /// </summary>
        public LogEventLevel GetLogLevel()
        {
            return _levelSwitch.MinimumLevel;
        }

        /// <summary>
        /// Gets the path to the log directory
        /// </summary>
        public string GetLogDirectory()
        {
            return _logDirectory;
        }

        /// <summary>
        /// Creates a context logger for a specific type
        /// </summary>
        public ILogger ForContext<T>()
        {
            return (_logger ?? Serilog.Log.Logger).ForContext<T>();
        }

        /// <summary>
        /// Creates a context logger with property enrichment
        /// </summary>
        public ILogger ForContext(string propertyName, object value, bool destructureObjects = false)
        {
            return (_logger ?? Serilog.Log.Logger).ForContext(propertyName, value, destructureObjects);
        }

        /// <summary>
        /// Performs cleanup of old log files based on retention policy
        /// </summary>
        public void CleanupOldLogs(int daysToKeep = 30)
        {
            try
            {
                var cutoffDate = DateTime.Now.AddDays(-daysToKeep);
                var logFiles = Directory.GetFiles(_logDirectory, "AnomaliImportTool-*.log");
                
                foreach (var file in logFiles)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.LastWriteTime < cutoffDate)
                    {
                        try
                        {
                            File.Delete(file);
                            _logger.Debug("Deleted old log file: {FileName}", fileInfo.Name);
                        }
                        catch (Exception ex)
                        {
                            _logger.Warning(ex, "Failed to delete old log file: {FileName}", fileInfo.Name);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error during log cleanup");
            }
        }

        /// <summary>
        /// Flushes any buffered log entries
        /// </summary>
        public void Flush()
        {
            _logger?.Dispose();
        }

        /// <summary>
        /// Creates a scoped logging operation
        /// </summary>
        public IDisposable BeginScope(string operationName, params object[] args)
        {
            return LogContext.PushProperty("Operation", string.Format(operationName, args));
        }

        /// <summary>
        /// Logs a metric value
        /// </summary>
        public void LogMetric(string metricName, double value, string unit = null)
        {
            var logger = (_logger ?? Serilog.Log.Logger).ForContext("MetricName", metricName);
            
            if (!string.IsNullOrEmpty(unit))
            {
                logger = logger.ForContext("MetricUnit", unit);
            }
            
            logger.Information("Metric recorded: {MetricName} = {MetricValue} {MetricUnit}", 
                metricName, value, unit ?? string.Empty);
        }

        /// <summary>
        /// Logs a timing metric
        /// </summary>
        public IDisposable LogTiming(string operationName)
        {
            return new TimingLogger((_logger ?? Serilog.Log.Logger), operationName);
        }

        /// <summary>
        /// Disposes the logger and flushes any pending logs
        /// </summary>
        public void Dispose()
        {
            _logger?.Dispose();
            if (Serilog.Log.Logger == _logger)
            {
                Serilog.Log.CloseAndFlush();
            }
        }

        /// <summary>
        /// Helper class for timing operations
        /// </summary>
        private class TimingLogger : IDisposable
        {
            private readonly ILogger _logger;
            private readonly string _operationName;
            private readonly DateTime _startTime;

            public TimingLogger(ILogger logger, string operationName)
            {
                _logger = logger;
                _operationName = operationName;
                _startTime = DateTime.UtcNow;
                
                _logger.Debug("Started operation: {Operation}", operationName);
            }

            public void Dispose()
            {
                var duration = DateTime.UtcNow - _startTime;
                _logger.Information("Completed operation: {Operation} in {Duration}ms", 
                    _operationName, duration.TotalMilliseconds);
            }
        }
    }

    /// <summary>
    /// Static helper for structured logging
    /// </summary>
    public static class LogHelper
    {
        /// <summary>
        /// Logs entry into a method
        /// </summary>
        public static void LogMethodEntry(ILogger logger, string methodName, object parameters = null)
        {
            if (parameters != null)
            {
                logger.Debug("Entering {MethodName} with parameters: {@Parameters}", methodName, parameters);
            }
            else
            {
                logger.Debug("Entering {MethodName}", methodName);
            }
        }

        /// <summary>
        /// Logs exit from a method
        /// </summary>
        public static void LogMethodExit(ILogger logger, string methodName, object result = null)
        {
            if (result != null)
            {
                logger.Debug("Exiting {MethodName} with result: {@Result}", methodName, result);
            }
            else
            {
                logger.Debug("Exiting {MethodName}", methodName);
            }
        }

        /// <summary>
        /// Logs a security event
        /// </summary>
        public static void LogSecurityEvent(ILogger logger, string eventType, string description, bool success)
        {
            logger.ForContext("EventType", "Security")
                   .ForContext("SecurityEventType", eventType)
                   .ForContext("Success", success)
                   .Information("Security Event: {EventType} - {Description}", eventType, description);
        }

        /// <summary>
        /// Logs a data processing event
        /// </summary>
        public static void LogDataProcessing(ILogger logger, string dataType, int recordCount, TimeSpan duration)
        {
            logger.ForContext("EventType", "DataProcessing")
                   .ForContext("DataType", dataType)
                   .ForContext("RecordCount", recordCount)
                   .ForContext("DurationMs", duration.TotalMilliseconds)
                   .Information("Processed {RecordCount} {DataType} records in {Duration}ms", 
                       recordCount, dataType, duration.TotalMilliseconds);
        }
    }
} 