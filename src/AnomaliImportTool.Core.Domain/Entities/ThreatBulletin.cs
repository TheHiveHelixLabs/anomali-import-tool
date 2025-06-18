using AnomaliImportTool.Core.Domain.Common;
using AnomaliImportTool.Core.Domain.ValueObjects;
using AnomaliImportTool.Core.Domain.Enums;

namespace AnomaliImportTool.Core.Domain.Entities;

/// <summary>
/// ThreatBulletin entity representing a threat intelligence bulletin
/// </summary>
public class ThreatBulletin : BaseEntity
{
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public ThreatLevel ThreatLevel { get; private set; } = ThreatLevel.Medium;
    public TlpDesignation TlpDesignation { get; private set; } = TlpDesignation.White;
    public BulletinStatus Status { get; private set; } = BulletinStatus.Draft;
    public string? ExternalId { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    public string Tags { get; private set; } = string.Empty;
    public string? SourceDocumentId { get; private set; }
    public List<string> AttachmentIds { get; private set; } = new();
    public List<Observable> Observables { get; private set; } = new();

    private ThreatBulletin() { } // For EF Core

    public ThreatBulletin(string title, string description, string content, ThreatLevel threatLevel = ThreatLevel.Medium)
    {
        Title = title ?? throw new ArgumentNullException(nameof(title));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Content = content ?? throw new ArgumentNullException(nameof(content));
        ThreatLevel = threatLevel;
        Status = BulletinStatus.Draft;
    }

    public void UpdateContent(string title, string description, string content)
    {
        Title = title ?? throw new ArgumentNullException(nameof(title));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Content = content ?? throw new ArgumentNullException(nameof(content));
        MarkAsUpdated(CreatedBy);
    }

    public void SetTlpDesignation(TlpDesignation designation)
    {
        TlpDesignation = designation;
        MarkAsUpdated(CreatedBy);
    }

    public void SetThreatLevel(ThreatLevel level)
    {
        ThreatLevel = level;
        MarkAsUpdated(CreatedBy);
    }

    public void Publish()
    {
        Status = BulletinStatus.Published;
        PublishedAt = DateTime.UtcNow;
        MarkAsUpdated(CreatedBy);
    }

    public void AddObservable(Observable observable)
    {
        if (observable == null) throw new ArgumentNullException(nameof(observable));
        Observables.Add(observable);
        MarkAsUpdated(CreatedBy);
    }

    public void AddAttachment(string attachmentId)
    {
        if (string.IsNullOrWhiteSpace(attachmentId)) throw new ArgumentNullException(nameof(attachmentId));
        AttachmentIds.Add(attachmentId);
        MarkAsUpdated(CreatedBy);
    }

    public void SetExternalId(string externalId)
    {
        ExternalId = externalId;
        MarkAsUpdated(CreatedBy);
    }

    public void SetTags(string tags)
    {
        Tags = tags ?? string.Empty;
        MarkAsUpdated(CreatedBy);
    }
} 