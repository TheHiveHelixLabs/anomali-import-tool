namespace AnomaliImportTool.Core.Domain.SharedKernel.Events;

/// <summary>
/// Marker interface for domain events
/// </summary>
public interface IDomainEvent
{
    Guid Id { get; }
    DateTime OccurredOn { get; }
    string EventType { get; }
} 