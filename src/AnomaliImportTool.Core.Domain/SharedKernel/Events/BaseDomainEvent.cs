namespace AnomaliImportTool.Core.Domain.SharedKernel.Events;

/// <summary>
/// Base implementation for domain events
/// </summary>
public abstract record BaseDomainEvent : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public abstract string EventType { get; }
} 