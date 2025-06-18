using AnomaliImportTool.Core.Domain.Enums;

namespace AnomaliImportTool.Core.Domain.ValueObjects;

/// <summary>
/// Processing result value object
/// </summary>
public sealed record ProcessingResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public TimeSpan ProcessingTime { get; init; }
    public IReadOnlyDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();
}

/// <summary>
/// Extraction result value object
/// </summary>
public sealed record ExtractionResult
{
    public string ExtractedText { get; init; } = string.Empty;
    public FileMetadata Metadata { get; init; } = new();
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Processing progress value object
/// </summary>
public sealed record ProcessingProgress
{
    public int TotalFiles { get; init; }
    public int ProcessedFiles { get; init; }
    public int SuccessfulFiles { get; init; }
    public int FailedFiles { get; init; }
    public string CurrentFile { get; init; } = string.Empty;
    public double PercentageComplete => TotalFiles > 0 ? (double)ProcessedFiles / TotalFiles * 100 : 0;
}

/// <summary>
/// Observable value object for threat intelligence
/// </summary>
public sealed record Observable
{
    public string Type { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int Confidence { get; init; }
    public TlpDesignation TlpDesignation { get; init; } = TlpDesignation.White;
}

/// <summary>
/// Import session value object
/// </summary>
public sealed record ImportSession
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime StartTime { get; init; } = DateTime.UtcNow;
    public DateTime? EndTime { get; init; }
    public int TotalObservables { get; init; }
    public int ImportedObservables { get; init; }
    public string Status { get; init; } = string.Empty;
}

/// <summary>
/// Authentication result value object
/// </summary>
public sealed record AuthenticationResult
{
    public bool IsAuthenticated { get; init; }
    public string? Token { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Encryption result value object
/// </summary>
public sealed record EncryptionResult
{
    public string EncryptedData { get; init; } = string.Empty;
    public string Salt { get; init; } = string.Empty;
    public string Algorithm { get; init; } = string.Empty;
}

/// <summary>
/// Secure credential value object
/// </summary>
public sealed record SecureCredential
{
    public string Key { get; init; } = string.Empty;
    public EncryptionResult EncryptedValue { get; init; } = new();
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; init; }
}

/// <summary>
/// Validation result value object
/// </summary>
public sealed record ValidationResult
{
    public bool IsValid { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = new List<string>();
    public string? SanitizedInput { get; init; }
}

/// <summary>
/// Hash result value object
/// </summary>
public sealed record HashResult
{
    public string Hash { get; init; } = string.Empty;
    public string Salt { get; init; } = string.Empty;
    public string Algorithm { get; init; } = string.Empty;
}

/// <summary>
/// Audit entry value object
/// </summary>
public sealed record AuditEntry
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string EventType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public object? Metadata { get; init; }
    public string CorrelationId { get; init; } = string.Empty;
}

/// <summary>
/// Git operation result value object
/// </summary>
public sealed record GitOperationResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public string? Output { get; init; }
    public string Operation { get; init; } = string.Empty;
}

/// <summary>
/// Git status value object
/// </summary>
public sealed record GitStatus
{
    public bool HasChanges { get; init; }
    public IReadOnlyList<string> ModifiedFiles { get; init; } = new List<string>();
    public IReadOnlyList<string> AddedFiles { get; init; } = new List<string>();
    public IReadOnlyList<string> DeletedFiles { get; init; } = new List<string>();
    public string CurrentBranch { get; init; } = string.Empty;
}

/// <summary>
/// Git commit value object
/// </summary>
public sealed record GitCommit
{
    public string Hash { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}

/// <summary>
/// Notification message value object
/// </summary>
public sealed record NotificationMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Progress update value object
/// </summary>
public sealed record ProgressUpdate
{
    public string Operation { get; init; } = string.Empty;
    public double PercentageComplete { get; init; }
    public string CurrentStep { get; init; } = string.Empty;
    public TimeSpan? EstimatedTimeRemaining { get; init; }
}

/// <summary>
/// Error notification value object
/// </summary>
public sealed record ErrorNotification
{
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public Exception? Exception { get; init; }
    public string CorrelationId { get; init; } = string.Empty;
}

/// <summary>
/// Success notification value object
/// </summary>
public sealed record SuccessNotification
{
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public object? Data { get; init; }
}

/// <summary>
/// Notification event args
/// </summary>
public sealed class NotificationEventArgs : EventArgs
{
    public NotificationMessage Notification { get; }

    public NotificationEventArgs(NotificationMessage notification)
    {
        Notification = notification ?? throw new ArgumentNullException(nameof(notification));
    }
}

/// <summary>
/// Log entry value object
/// </summary>
public sealed record LogEntry
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public LogLevel Level { get; init; }
    public string Message { get; init; } = string.Empty;
    public Exception? Exception { get; init; }
    public object? Context { get; init; }
    public string CorrelationId { get; init; } = string.Empty;
    public string OperationName { get; init; } = string.Empty;
    public TimeSpan? Duration { get; init; }
} 