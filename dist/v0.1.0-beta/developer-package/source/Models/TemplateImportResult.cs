using System;
using System.Collections.Generic;

namespace AnomaliImportTool.Core.Models;

/// <summary>
/// Represents the result of importing a single template
/// </summary>
public class TemplateImportResult
{
    /// <summary>
    /// Whether the import was successful
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// The imported template (if successful)
    /// </summary>
    public ImportTemplate? ImportedTemplate { get; set; }

    /// <summary>
    /// Original template ID from the import source
    /// </summary>
    public Guid? OriginalTemplateId { get; set; }

    /// <summary>
    /// Name of the template being imported
    /// </summary>
    public string TemplateName { get; set; } = string.Empty;

    /// <summary>
    /// Action taken during import
    /// </summary>
    public ImportAction ActionTaken { get; set; }

    /// <summary>
    /// Errors that occurred during import
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Warnings generated during import
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Validation issues found
    /// </summary>
    public List<string> ValidationIssues { get; set; } = new();

    /// <summary>
    /// Conflicts that were encountered
    /// </summary>
    public List<string> Conflicts { get; set; } = new();

    /// <summary>
    /// How conflicts were resolved
    /// </summary>
    public string ConflictResolution { get; set; } = string.Empty;

    /// <summary>
    /// Time taken to import this template
    /// </summary>
    public TimeSpan ImportDuration { get; set; }

    /// <summary>
    /// Additional metadata about the import
    /// </summary>
    public Dictionary<string, object> ImportMetadata { get; set; } = new();
}

/// <summary>
/// Actions that can be taken during template import
/// </summary>
public enum ImportAction
{
    /// <summary>
    /// Template was created as new
    /// </summary>
    Created,

    /// <summary>
    /// Existing template was updated
    /// </summary>
    Updated,

    /// <summary>
    /// Template was skipped (already exists)
    /// </summary>
    Skipped,

    /// <summary>
    /// Template was merged with existing
    /// </summary>
    Merged,

    /// <summary>
    /// Template was renamed to avoid conflict
    /// </summary>
    Renamed,

    /// <summary>
    /// Import failed
    /// </summary>
    Failed,

    /// <summary>
    /// Import was cancelled
    /// </summary>
    Cancelled
} 