using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AnomaliImportTool.Core.Models;

/// <summary>
/// Represents an import template for extracting metadata from documents
/// Supports coordinate-based extraction zones, field validation, and document matching
/// </summary>
public class ImportTemplate
{
    /// <summary>
    /// Unique identifier for the template
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Display name for the template
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the template's purpose
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Template version for change tracking
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Category for template organization (e.g., "Security/Exceptions", "Reports/APT")
    /// </summary>
    [StringLength(100)]
    public string Category { get; set; } = "General";

    /// <summary>
    /// User who created the template
    /// </summary>
    [StringLength(100)]
    public string? CreatedBy { get; set; }

    /// <summary>
    /// User who last modified the template
    /// </summary>
    [StringLength(100)]
    public string? LastModifiedBy { get; set; }

    /// <summary>
    /// When the template was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the template was last modified
    /// </summary>
    public DateTime LastModifiedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether the template is active and available for use
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Tags for template categorization and search
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Supported document formats (pdf, docx, xlsx, etc.)
    /// </summary>
    public List<string> SupportedFormats { get; set; } = new();

    /// <summary>
    /// Template fields defining what to extract and how
    /// </summary>
    public List<TemplateField> Fields { get; set; } = new();

    /// <summary>
    /// Document matching criteria for automatic template selection
    /// </summary>
    public DocumentMatchingCriteria MatchingCriteria { get; set; } = new();

    /// <summary>
    /// OCR settings specific to this template
    /// </summary>
    public OcrSettings OcrSettings { get; set; } = new();

    /// <summary>
    /// Validation rules for the entire template
    /// </summary>
    public TemplateValidation Validation { get; set; } = new();

    /// <summary>
    /// Additional metadata for the template
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Usage statistics for the template
    /// </summary>
    public TemplateUsageStats UsageStats { get; set; } = new();

    /// <summary>
    /// Confidence threshold for template matching (0.0 to 1.0)
    /// </summary>
    public double ConfidenceThreshold { get; set; } = 0.75;

    /// <summary>
    /// Whether to automatically apply this template when it matches
    /// </summary>
    public bool AutoApply { get; set; } = false;

    /// <summary>
    /// Whether to allow partial matches when applying template
    /// </summary>
    public bool AllowPartialMatches { get; set; } = false;

    /// <summary>
    /// Priority of this template when multiple templates match (higher numbers win)
    /// </summary>
    public int TemplatePriority { get; set; } = 0;

    /// <summary>
    /// Validates the template structure and rules
    /// </summary>
    /// <returns>Validation result with any errors</returns>
    public TemplateValidationResult ValidateTemplate()
    {
        var result = new TemplateValidationResult { IsValid = true };

        // Validate required fields
        if (string.IsNullOrWhiteSpace(Name))
        {
            result.IsValid = false;
            result.Errors.Add("Template name is required");
        }

        if (Fields.Count == 0)
        {
            result.IsValid = false;
            result.Errors.Add("Template must have at least one field");
        }

        // Validate field configurations
        for (int i = 0; i < Fields.Count; i++)
        {
            var field = Fields[i];
            var fieldResult = field.ValidateField();
            if (!fieldResult.IsValid)
            {
                result.IsValid = false;
                foreach (var error in fieldResult.Errors)
                {
                    result.Errors.Add($"Field {i + 1} ({field.Name}): {error}");
                }
            }
        }

        // Validate supported formats
        if (SupportedFormats.Count == 0)
        {
            result.IsValid = false;
            result.Errors.Add("Template must support at least one document format");
        }

        // Check for duplicate field names
        var fieldNames = Fields.Select(f => f.Name).ToList();
        var duplicates = fieldNames.GroupBy(n => n).Where(g => g.Count() > 1).Select(g => g.Key);
        foreach (var duplicate in duplicates)
        {
            result.IsValid = false;
            result.Errors.Add($"Duplicate field name: {duplicate}");
        }

        return result;
    }

    /// <summary>
    /// Creates a copy of the template with a new version
    /// </summary>
    /// <param name="newVersion">Version number for the copy</param>
    /// <returns>New template instance</returns>
    public ImportTemplate CreateVersion(string newVersion)
    {
        var copy = new ImportTemplate
        {
            Id = Guid.NewGuid(),
            Name = Name,
            Description = Description,
            Version = newVersion,
            Category = Category,
            CreatedBy = LastModifiedBy,
            LastModifiedBy = LastModifiedBy,
            CreatedAt = DateTime.UtcNow,
            LastModifiedAt = DateTime.UtcNow,
            IsActive = IsActive,
            Tags = new List<string>(Tags),
            SupportedFormats = new List<string>(SupportedFormats),
            Fields = Fields.Select(f => f.CreateCopy()).ToList(),
            MatchingCriteria = MatchingCriteria.CreateCopy(),
            OcrSettings = OcrSettings.CreateCopy(),
            Validation = Validation.CreateCopy(),
            Metadata = new Dictionary<string, object>(Metadata),
            UsageStats = new TemplateUsageStats(), // Reset stats for new version
            ConfidenceThreshold = ConfidenceThreshold,
            AutoApply = AutoApply,
            AllowPartialMatches = AllowPartialMatches,
            TemplatePriority = TemplatePriority
        };

        return copy;
    }

    /// <summary>
    /// Updates usage statistics
    /// </summary>
    /// <param name="successful">Whether the template was successfully applied</param>
    public void UpdateUsageStats(bool successful)
    {
        UsageStats.TotalUses++;
        UsageStats.LastUsed = DateTime.UtcNow;

        if (successful)
        {
            UsageStats.SuccessfulUses++;
        }
        else
        {
            UsageStats.FailedUses++;
        }

        // Update success rate
        UsageStats.SuccessRate = UsageStats.TotalUses > 0 
            ? (double)UsageStats.SuccessfulUses / UsageStats.TotalUses 
            : 0.0;
    }
}

/// <summary>
/// Document matching criteria for automatic template selection
/// </summary>
public class DocumentMatchingCriteria
{
    /// <summary>
    /// Keywords that should be present in the document
    /// </summary>
    public List<string> RequiredKeywords { get; set; } = new();

    /// <summary>
    /// Optional keywords that increase match confidence
    /// </summary>
    public List<string> OptionalKeywords { get; set; } = new();

    /// <summary>
    /// File name patterns (regex) that match this template
    /// </summary>
    public List<string> FileNamePatterns { get; set; } = new();

    /// <summary>
    /// Document title patterns (regex) that match this template
    /// </summary>
    public List<string> TitlePatterns { get; set; } = new();

    /// <summary>
    /// Author patterns that match this template
    /// </summary>
    public List<string> AuthorPatterns { get; set; } = new();

    /// <summary>
    /// Minimum confidence threshold for automatic matching (0.0 to 1.0)
    /// </summary>
    public double MinimumConfidence { get; set; } = 0.75;

    /// <summary>
    /// Whether to enable automatic template application
    /// </summary>
    public bool AutoApply { get; set; } = false;

    /// <summary>
    /// Creates a copy of the matching criteria
    /// </summary>
    public DocumentMatchingCriteria CreateCopy()
    {
        return new DocumentMatchingCriteria
        {
            RequiredKeywords = new List<string>(RequiredKeywords),
            OptionalKeywords = new List<string>(OptionalKeywords),
            FileNamePatterns = new List<string>(FileNamePatterns),
            TitlePatterns = new List<string>(TitlePatterns),
            AuthorPatterns = new List<string>(AuthorPatterns),
            MinimumConfidence = MinimumConfidence,
            AutoApply = AutoApply
        };
    }
}

/// <summary>
/// OCR settings specific to a template
/// </summary>
public class OcrSettings
{
    /// <summary>
    /// Whether OCR is enabled for this template
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// OCR language configuration
    /// </summary>
    public string Language { get; set; } = "eng";

    /// <summary>
    /// OCR engine mode (e.g., Tesseract engine modes)
    /// </summary>
    public int EngineMode { get; set; } = 3; // Default to best mode

    /// <summary>
    /// Page segmentation mode
    /// </summary>
    public int PageSegmentationMode { get; set; } = 6; // Assume single column of text

    /// <summary>
    /// OCR confidence threshold (0-100)
    /// </summary>
    public int ConfidenceThreshold { get; set; } = 60;

    /// <summary>
    /// Creates a copy of the OCR settings
    /// </summary>
    public OcrSettings CreateCopy()
    {
        return new OcrSettings
        {
            Enabled = Enabled,
            Language = Language,
            EngineMode = EngineMode,
            PageSegmentationMode = PageSegmentationMode,
            ConfidenceThreshold = ConfidenceThreshold
        };
    }
}

/// <summary>
/// Template-level validation rules
/// </summary>
public class TemplateValidation
{
    /// <summary>
    /// Minimum number of fields that must be successfully extracted
    /// </summary>
    public int MinimumRequiredFields { get; set; } = 1;

    /// <summary>
    /// Whether all required fields must be extracted for success
    /// </summary>
    public bool RequireAllRequiredFields { get; set; } = true;

    /// <summary>
    /// Custom validation rules (C# expressions)
    /// </summary>
    public List<string> CustomValidationRules { get; set; } = new();

    /// <summary>
    /// Error handling strategy
    /// </summary>
    public ValidationErrorHandling ErrorHandling { get; set; } = ValidationErrorHandling.LogAndContinue;

    /// <summary>
    /// Creates a copy of the validation settings
    /// </summary>
    public TemplateValidation CreateCopy()
    {
        return new TemplateValidation
        {
            MinimumRequiredFields = MinimumRequiredFields,
            RequireAllRequiredFields = RequireAllRequiredFields,
            CustomValidationRules = new List<string>(CustomValidationRules),
            ErrorHandling = ErrorHandling
        };
    }
}

/// <summary>
/// Template usage statistics
/// </summary>
public class TemplateUsageStats
{
    /// <summary>
    /// Total number of times the template has been used
    /// </summary>
    public int TotalUses { get; set; } = 0;

    /// <summary>
    /// Number of successful extractions
    /// </summary>
    public int SuccessfulUses { get; set; } = 0;

    /// <summary>
    /// Number of failed extractions
    /// </summary>
    public int FailedUses { get; set; } = 0;

    /// <summary>
    /// Success rate (0.0 to 1.0)
    /// </summary>
    public double SuccessRate { get; set; } = 0.0;

    /// <summary>
    /// When the template was last used
    /// </summary>
    public DateTime? LastUsed { get; set; }

    /// <summary>
    /// Average extraction time in milliseconds
    /// </summary>
    public double AverageExtractionTime { get; set; } = 0.0;
}

/// <summary>
/// Validation error handling strategies
/// </summary>
public enum ValidationErrorHandling
{
    /// <summary>
    /// Stop processing on first error
    /// </summary>
    StopOnError,

    /// <summary>
    /// Log error and continue processing
    /// </summary>
    LogAndContinue,

    /// <summary>
    /// Attempt to use fallback extraction methods
    /// </summary>
    UseFallback
}

/// <summary>
/// Template validation result
/// </summary>
public class TemplateValidationResult
{
    /// <summary>
    /// Whether the template is valid
    /// </summary>
    public bool IsValid { get; set; } = true;

    /// <summary>
    /// List of validation errors
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// List of validation warnings
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Additional validation metadata
    /// </summary>
    public Dictionary<string, object> ValidationMetadata { get; set; } = new();

    /// <summary>
    /// List of placeholders used during template validation
    /// </summary>
    public List<string> UsedPlaceholders { get; set; } = new();
} 