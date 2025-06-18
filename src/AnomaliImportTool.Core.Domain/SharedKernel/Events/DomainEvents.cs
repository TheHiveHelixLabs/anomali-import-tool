namespace AnomaliImportTool.Core.Domain.SharedKernel.Events;

/// <summary>
/// Document processing started event
/// </summary>
public record DocumentProcessingStarted(
    Guid DocumentId,
    string FileName,
    string FilePath,
    DateTime StartedAt
) : BaseDomainEvent
{
    public override string EventType => nameof(DocumentProcessingStarted);
}

/// <summary>
/// Document processing completed event
/// </summary>
public record DocumentProcessingCompleted(
    Guid DocumentId,
    string FileName,
    bool IsSuccess,
    string? ErrorMessage,
    TimeSpan ProcessingTime
) : BaseDomainEvent
{
    public override string EventType => nameof(DocumentProcessingCompleted);
}

/// <summary>
/// Threat bulletin created event
/// </summary>
public record ThreatBulletinCreated(
    Guid BulletinId,
    string Title,
    string ThreatLevel,
    string CreatedBy
) : BaseDomainEvent
{
    public override string EventType => nameof(ThreatBulletinCreated);
}

/// <summary>
/// Threat bulletin published event
/// </summary>
public record ThreatBulletinPublished(
    Guid BulletinId,
    string Title,
    string ExternalId,
    DateTime PublishedAt
) : BaseDomainEvent
{
    public override string EventType => nameof(ThreatBulletinPublished);
}

/// <summary>
/// Security event occurred
/// </summary>
public record SecurityEventOccurred(
    string SecurityEventType,
    string Description,
    string UserId,
    string? CorrelationId,
    object? Metadata
) : BaseDomainEvent
{
    public override string EventType => nameof(SecurityEventOccurred);
}

/// <summary>
/// Git operation completed event
/// </summary>
public record GitOperationCompleted(
    string Operation,
    string RepositoryPath,
    bool IsSuccess,
    string? ErrorMessage,
    string? Output
) : BaseDomainEvent
{
    public override string EventType => nameof(GitOperationCompleted);
}

/// <summary>
/// Batch processing completed event
/// </summary>
public record BatchProcessingCompleted(
    Guid BatchId,
    int TotalFiles,
    int SuccessfulFiles,
    int FailedFiles,
    TimeSpan TotalProcessingTime
) : BaseDomainEvent
{
    public override string EventType => nameof(BatchProcessingCompleted);
} 