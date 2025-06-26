using System;
using System.Collections.Generic;

namespace AnomaliImportTool.Core.Models;

/// <summary>
/// Represents the result of comparing two template versions
/// </summary>
public class TemplateComparisonResult
{
    /// <summary>
    /// ID of the template being compared
    /// </summary>
    public Guid TemplateId { get; set; }

    /// <summary>
    /// First version being compared
    /// </summary>
    public string Version1 { get; set; } = string.Empty;

    /// <summary>
    /// Second version being compared
    /// </summary>
    public string Version2 { get; set; } = string.Empty;

    /// <summary>
    /// Whether the two versions are identical
    /// </summary>
    public bool AreIdentical { get; set; }

    /// <summary>
    /// Summary of differences between versions
    /// </summary>
    public string DifferencesSummary { get; set; } = string.Empty;

    /// <summary>
    /// Fields that were added in version 2
    /// </summary>
    public List<string> AddedFields { get; set; } = new();

    /// <summary>
    /// Fields that were removed in version 2
    /// </summary>
    public List<string> RemovedFields { get; set; } = new();

    /// <summary>
    /// Fields that were modified between versions
    /// </summary>
    public List<FieldComparison> ModifiedFields { get; set; } = new();

    /// <summary>
    /// When the comparison was performed
    /// </summary>
    public DateTime ComparedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Additional metadata about the comparison
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Represents a comparison of a specific field between template versions
/// </summary>
public class FieldComparison
{
    /// <summary>
    /// Name of the field
    /// </summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// Value in the first version
    /// </summary>
    public string? OldValue { get; set; }

    /// <summary>
    /// Value in the second version
    /// </summary>
    public string? NewValue { get; set; }

    /// <summary>
    /// Type of change made to the field
    /// </summary>
    public FieldChangeType ChangeType { get; set; }

    /// <summary>
    /// Description of the change
    /// </summary>
    public string ChangeDescription { get; set; } = string.Empty;
}

/// <summary>
/// Types of changes that can occur to a field
/// </summary>
public enum FieldChangeType
{
    Modified,
    Added,
    Removed,
    TypeChanged,
    ConfigurationChanged
} 