using AnomaliImportTool.Core.Domain.ValueObjects;
using AnomaliImportTool.Core.Domain.Enums;

namespace AnomaliImportTool.Core.Application.Interfaces.Services;

/// <summary>
/// Interface for logging operations following Clean Architecture dependency inversion
/// </summary>
public interface ILoggingService
{
    Task LogInformationAsync(string message, object? context = null, CancellationToken cancellationToken = default);
    Task LogWarningAsync(string message, object? context = null, CancellationToken cancellationToken = default);
    Task LogErrorAsync(string message, Exception? exception = null, object? context = null, CancellationToken cancellationToken = default);
    Task LogDebugAsync(string message, object? context = null, CancellationToken cancellationToken = default);
    Task LogCriticalAsync(string message, Exception? exception = null, object? context = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<LogEntry>> GetLogsAsync(LogLevel level = LogLevel.Information, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
    Task<LogEntry> StartOperationAsync(string operationName, object? context = null, CancellationToken cancellationToken = default);
    Task EndOperationAsync(LogEntry operation, bool success = true, object? result = null, CancellationToken cancellationToken = default);
    Task SetCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default);
    Task ClearCorrelationIdAsync(CancellationToken cancellationToken = default);
} 