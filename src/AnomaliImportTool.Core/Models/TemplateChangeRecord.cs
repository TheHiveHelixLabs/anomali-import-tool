using System;
using System.Collections.Generic;

namespace AnomaliImportTool.Core.Models;

/// <summary>
/// Represents a record of changes made to a template
/// </summary>
public class TemplateChangeRecord
{
    /// <summary>
    /// Unique identifier for the change record
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// ID of the template that was changed
    /// </summary>
    public Guid TemplateId { get; set; }

    /// <summary>
    /// Version before the change
    /// </summary>
    public string PreviousVersion { get; set; } = string.Empty;

    /// <summary>
    /// Version after the change
    /// </summary>
    public string NewVersion { get; set; } = string.Empty;

    /// <summary>
    /// Type of change that was made
    /// </summary>
    public TemplateChangeType ChangeType { get; set; }

    /// <summary>
    /// Detailed description of the change
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// User who made the change
    /// </summary>
    public string ChangedBy { get; set; } = string.Empty;

    /// <summary>
    /// When the change was made
    /// </summary>
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Specific fields that were changed
    /// </summary>
    public List<string> ChangedFields { get; set; } = new();

    /// <summary>
    /// Additional metadata about the change
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Types of changes that can be made to a template
/// </summary>
public enum TemplateChangeType
{
    Created,
    Updated,
    Deleted,
    Activated,
    Deactivated,
    VersionCreated,
    RolledBack,
    Duplicated,
    Merged,
    InheritanceAdded,
    InheritanceRemoved
} 