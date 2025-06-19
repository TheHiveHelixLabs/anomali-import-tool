using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using System.Linq;

namespace AnomaliImportTool.Core.Models;

/// <summary>
/// Root container for exported template data in JSON format
/// Supports versioning, metadata, and complete template preservation
/// </summary>
public class TemplateExportFormat
{
    /// <summary>
    /// Export format version for compatibility tracking
    /// </summary>
    [JsonPropertyName("export_format_version")]
    public string ExportFormatVersion { get; set; } = "1.0.0";

    /// <summary>
    /// When this export was created
    /// </summary>
    [JsonPropertyName("exported_at")]
    public DateTime ExportedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Who exported this template
    /// </summary>
    [JsonPropertyName("exported_by")]
    public string? ExportedBy { get; set; }

    /// <summary>
    /// Export metadata and settings
    /// </summary>
    [JsonPropertyName("export_metadata")]
    public TemplateExportMetadata ExportMetadata { get; set; } = new();

    /// <summary>
    /// The main template data
    /// </summary>
    [JsonPropertyName("template")]
    public SerializableImportTemplate Template { get; set; } = new();

    /// <summary>
    /// Template fields with extraction definitions
    /// </summary>
    [JsonPropertyName("fields")]
    public List<SerializableTemplateField> Fields { get; set; } = new();

    /// <summary>
    /// Extraction zones for coordinate-based field extraction
    /// </summary>
    [JsonPropertyName("extraction_zones")]
    public List<SerializableExtractionZone> ExtractionZones { get; set; } = new();

    /// <summary>
    /// Template version history (optional)
    /// </summary>
    [JsonPropertyName("version_history")]
    public List<TemplateVersionInfo>? VersionHistory { get; set; }

    /// <summary>
    /// Usage statistics (optional, for analytics)
    /// </summary>
    [JsonPropertyName("usage_statistics")]
    public TemplateUsageStatistics? UsageStatistics { get; set; }

    /// <summary>
    /// Validates the export format structure
    /// </summary>
    public TemplateValidationResult Validate()
    {
        var result = new TemplateValidationResult();

        // Validate export format version
        if (string.IsNullOrWhiteSpace(ExportFormatVersion))
        {
            result.Errors.Add("Export format version is required");
        }

        // Validate template
        if (Template == null)
        {
            result.Errors.Add("Template data is required");
        }
        else
        {
            var templateValidation = Template.Validate();
            result.Errors.AddRange(templateValidation.Errors);
            result.Warnings.AddRange(templateValidation.Warnings);
        }

        // Validate fields
        if (Fields == null || Fields.Count == 0)
        {
            result.Warnings.Add("Template has no fields defined");
        }
        else
        {
            for (int i = 0; i < Fields.Count; i++)
            {
                var fieldValidation = Fields[i].Validate();
                result.Errors.AddRange(fieldValidation.Errors.Select(e => $"Field {i + 1}: {e}"));
                result.Warnings.AddRange(fieldValidation.Warnings.Select(w => $"Field {i + 1}: {w}"));
            }
        }

        // Validate extraction zones
        if (ExtractionZones != null)
        {
            for (int i = 0; i < ExtractionZones.Count; i++)
            {
                var zoneValidation = ExtractionZones[i].Validate();
                result.Errors.AddRange(zoneValidation.Errors.Select(e => $"Zone {i + 1}: {e}"));
                result.Warnings.AddRange(zoneValidation.Warnings.Select(w => $"Zone {i + 1}: {w}"));
            }
        }

        result.IsValid = result.Errors.Count == 0;
        return result;
    }

    /// <summary>
    /// Creates a minimal export with only essential data
    /// </summary>
    public static TemplateExportFormat CreateMinimal(ImportTemplate template, List<TemplateField> fields)
    {
        return new TemplateExportFormat
        {
            ExportMetadata = new TemplateExportMetadata
            {
                ExportType = TemplateExportType.Minimal,
                IncludeUsageStatistics = false,
                IncludeVersionHistory = false
            },
            Template = SerializableImportTemplate.FromDomainModel(template),
            Fields = fields.Select(SerializableTemplateField.FromDomainModel).ToList()
        };
    }

    /// <summary>
    /// Creates a complete export with all available data
    /// </summary>
    public static TemplateExportFormat CreateComplete(ImportTemplate template, List<TemplateField> fields, 
        List<ExtractionZone>? zones = null, List<TemplateVersionInfo>? versionHistory = null, 
        TemplateUsageStatistics? usageStats = null)
    {
        return new TemplateExportFormat
        {
            ExportMetadata = new TemplateExportMetadata
            {
                ExportType = TemplateExportType.Complete,
                IncludeUsageStatistics = usageStats != null,
                IncludeVersionHistory = versionHistory != null
            },
            Template = SerializableImportTemplate.FromDomainModel(template),
            Fields = fields.Select(SerializableTemplateField.FromDomainModel).ToList(),
            ExtractionZones = zones?.Select(SerializableExtractionZone.FromDomainModel).ToList() ?? new(),
            VersionHistory = versionHistory,
            UsageStatistics = usageStats
        };
    }
}

/// <summary>
/// Metadata about the template export
/// </summary>
public class TemplateExportMetadata
{
    /// <summary>
    /// Type of export (minimal, complete, etc.)
    /// </summary>
    [JsonPropertyName("export_type")]
    public TemplateExportType ExportType { get; set; } = TemplateExportType.Standard;

    /// <summary>
    /// Purpose of the export
    /// </summary>
    [JsonPropertyName("export_purpose")]
    public string? ExportPurpose { get; set; }

    /// <summary>
    /// Target application or system
    /// </summary>
    [JsonPropertyName("target_system")]
    public string? TargetSystem { get; set; }

    /// <summary>
    /// Whether usage statistics are included
    /// </summary>
    [JsonPropertyName("include_usage_statistics")]
    public bool IncludeUsageStatistics { get; set; } = false;

    /// <summary>
    /// Whether version history is included
    /// </summary>
    [JsonPropertyName("include_version_history")]
    public bool IncludeVersionHistory { get; set; } = false;

    /// <summary>
    /// Whether extraction zones are included
    /// </summary>
    [JsonPropertyName("include_extraction_zones")]
    public bool IncludeExtractionZones { get; set; } = true;

    /// <summary>
    /// Compression used (if any)
    /// </summary>
    [JsonPropertyName("compression")]
    public string? Compression { get; set; }

    /// <summary>
    /// Export file size in bytes
    /// </summary>
    [JsonPropertyName("file_size_bytes")]
    public long? FileSizeBytes { get; set; }

    /// <summary>
    /// Checksum for integrity verification
    /// </summary>
    [JsonPropertyName("checksum")]
    public string? Checksum { get; set; }

    /// <summary>
    /// Custom metadata properties
    /// </summary>
    [JsonPropertyName("custom_metadata")]
    public Dictionary<string, object> CustomMetadata { get; set; } = new();
}

/// <summary>
/// Serializable version of ImportTemplate optimized for JSON export
/// </summary>
public class SerializableImportTemplate
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    [Required]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0";

    [JsonPropertyName("category")]
    public string Category { get; set; } = "General";

    [JsonPropertyName("created_by")]
    public string? CreatedBy { get; set; }

    [JsonPropertyName("last_modified_by")]
    public string? LastModifiedBy { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("last_modified_at")]
    public DateTime LastModifiedAt { get; set; }

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; } = true;

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();

    [JsonPropertyName("supported_formats")]
    public List<string> SupportedFormats { get; set; } = new();

    [JsonPropertyName("confidence_threshold")]
    public double ConfidenceThreshold { get; set; } = 0.7;

    [JsonPropertyName("auto_apply")]
    public bool AutoApply { get; set; } = false;

    [JsonPropertyName("allow_partial_matches")]
    public bool AllowPartialMatches { get; set; } = true;

    [JsonPropertyName("template_priority")]
    public int TemplatePriority { get; set; } = 0;

    /// <summary>
    /// Converts from domain model to serializable format
    /// </summary>
    public static SerializableImportTemplate FromDomainModel(ImportTemplate template)
    {
        return new SerializableImportTemplate
        {
            Id = template.Id.ToString(),
            Name = template.Name,
            Description = template.Description,
            Version = template.Version,
            Category = template.Category,
            CreatedBy = template.CreatedBy,
            LastModifiedBy = template.LastModifiedBy,
            CreatedAt = template.CreatedAt,
            LastModifiedAt = template.LastModifiedAt,
            IsActive = template.IsActive,
            Tags = template.Tags,
            SupportedFormats = template.SupportedFormats,
            ConfidenceThreshold = template.ConfidenceThreshold,
            AutoApply = template.AutoApply,
            AllowPartialMatches = template.AllowPartialMatches,
            TemplatePriority = template.TemplatePriority
        };
    }

    /// <summary>
    /// Converts to domain model
    /// </summary>
    public ImportTemplate ToDomainModel()
    {
        return new ImportTemplate
        {
            Id = Guid.TryParse(Id, out var guid) ? guid : Guid.NewGuid(),
            Name = Name,
            Description = Description,
            Version = Version,
            Category = Category,
            CreatedBy = CreatedBy,
            LastModifiedBy = LastModifiedBy,
            CreatedAt = CreatedAt,
            LastModifiedAt = LastModifiedAt,
            IsActive = IsActive,
            Tags = Tags,
            SupportedFormats = SupportedFormats,
            ConfidenceThreshold = ConfidenceThreshold,
            AutoApply = AutoApply,
            AllowPartialMatches = AllowPartialMatches,
            TemplatePriority = TemplatePriority
        };
    }

    /// <summary>
    /// Validates the serializable template
    /// </summary>
    public TemplateValidationResult Validate()
    {
        var result = new TemplateValidationResult();

        if (string.IsNullOrWhiteSpace(Name))
        {
            result.Errors.Add("Template name is required");
        }

        if (string.IsNullOrWhiteSpace(Version))
        {
            result.Errors.Add("Template version is required");
        }

        if (ConfidenceThreshold < 0.0 || ConfidenceThreshold > 1.0)
        {
            result.Errors.Add("Confidence threshold must be between 0.0 and 1.0");
        }

        if (TemplatePriority < 0)
        {
            result.Errors.Add("Template priority must be non-negative");
        }

        result.IsValid = result.Errors.Count == 0;
        return result;
    }
}

/// <summary>
/// Serializable version of TemplateField
/// </summary>
public class SerializableTemplateField
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    [Required]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("field_type")]
    public string FieldType { get; set; } = "Text";

    [JsonPropertyName("extraction_method")]
    public string ExtractionMethod { get; set; } = "Text";

    [JsonPropertyName("is_required")]
    public bool IsRequired { get; set; } = false;

    [JsonPropertyName("processing_order")]
    public int ProcessingOrder { get; set; } = 0;

    [JsonPropertyName("text_patterns")]
    public List<string> TextPatterns { get; set; } = new();

    [JsonPropertyName("keywords")]
    public List<string> Keywords { get; set; } = new();

    [JsonPropertyName("default_value")]
    public string? DefaultValue { get; set; }

    [JsonPropertyName("output_format")]
    public string? OutputFormat { get; set; }

    [JsonPropertyName("supports_multiple_values")]
    public bool SupportsMultipleValues { get; set; } = false;

    [JsonPropertyName("value_separator")]
    public string ValueSeparator { get; set; } = ",";

    [JsonPropertyName("confidence_threshold")]
    public double ConfidenceThreshold { get; set; } = 0.5;

    /// <summary>
    /// Converts from domain model
    /// </summary>
    public static SerializableTemplateField FromDomainModel(TemplateField field)
    {
        return new SerializableTemplateField
        {
            Id = field.Id.ToString(),
            Name = field.Name,
            DisplayName = field.DisplayName,
            Description = field.Description,
            FieldType = field.FieldType.ToString(),
            ExtractionMethod = field.ExtractionMethod.ToString(),
            IsRequired = field.IsRequired,
            ProcessingOrder = field.ProcessingOrder,
            TextPatterns = field.TextPatterns,
            Keywords = field.Keywords,
            DefaultValue = field.DefaultValue,
            OutputFormat = field.OutputFormat,
            SupportsMultipleValues = field.SupportsMultipleValues,
            ValueSeparator = field.ValueSeparator,
            ConfidenceThreshold = field.ConfidenceThreshold
        };
    }

    /// <summary>
    /// Converts to domain model
    /// </summary>
    public TemplateField ToDomainModel()
    {
        return new TemplateField
        {
            Id = Guid.TryParse(Id, out var guid) ? guid : Guid.NewGuid(),
            Name = Name,
            DisplayName = DisplayName,
            Description = Description,
            FieldType = Enum.TryParse<TemplateFieldType>(FieldType, out var fieldType) ? fieldType : TemplateFieldType.Text,
            ExtractionMethod = Enum.TryParse<ExtractionMethod>(ExtractionMethod, out var extractionMethod) ? extractionMethod : Models.ExtractionMethod.Text,
            IsRequired = IsRequired,
            ProcessingOrder = ProcessingOrder,
            TextPatterns = TextPatterns,
            Keywords = Keywords,
            DefaultValue = DefaultValue,
            OutputFormat = OutputFormat,
            SupportsMultipleValues = SupportsMultipleValues,
            ValueSeparator = ValueSeparator,
            ConfidenceThreshold = ConfidenceThreshold
        };
    }

    /// <summary>
    /// Validates the serializable field
    /// </summary>
    public TemplateValidationResult Validate()
    {
        var result = new TemplateValidationResult();

        if (string.IsNullOrWhiteSpace(Name))
        {
            result.Errors.Add("Field name is required");
        }

        if (ProcessingOrder < 0)
        {
            result.Errors.Add("Processing order must be non-negative");
        }

        if (ConfidenceThreshold < 0.0 || ConfidenceThreshold > 1.0)
        {
            result.Errors.Add("Confidence threshold must be between 0.0 and 1.0");
        }

        result.IsValid = result.Errors.Count == 0;
        return result;
    }
}

/// <summary>
/// Serializable version of ExtractionZone
/// </summary>
public class SerializableExtractionZone
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("field_id")]
    public string FieldId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }

    [JsonPropertyName("width")]
    public double Width { get; set; }

    [JsonPropertyName("height")]
    public double Height { get; set; }

    [JsonPropertyName("page_number")]
    public int PageNumber { get; set; } = 1;

    [JsonPropertyName("coordinate_system")]
    public string CoordinateSystem { get; set; } = "Pixel";

    [JsonPropertyName("zone_type")]
    public string ZoneType { get; set; } = "Text";

    [JsonPropertyName("priority")]
    public int Priority { get; set; } = 0;

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; } = true;

    [JsonPropertyName("position_tolerance")]
    public double PositionTolerance { get; set; } = 5.0;

    [JsonPropertyName("size_tolerance")]
    public double SizeTolerance { get; set; } = 10.0;

    /// <summary>
    /// Converts from domain model
    /// </summary>
    public static SerializableExtractionZone FromDomainModel(ExtractionZone zone)
    {
        return new SerializableExtractionZone
        {
            Id = zone.Id.ToString(),
            Name = zone.Name,
            Description = zone.Description,
            X = zone.X,
            Y = zone.Y,
            Width = zone.Width,
            Height = zone.Height,
            PageNumber = zone.PageNumber,
            CoordinateSystem = zone.CoordinateSystem.ToString(),
            ZoneType = zone.ZoneType.ToString(),
            Priority = zone.Priority,
            IsActive = zone.IsActive,
            PositionTolerance = zone.PositionTolerance,
            SizeTolerance = zone.SizeTolerance
        };
    }

    /// <summary>
    /// Converts to domain model
    /// </summary>
    public ExtractionZone ToDomainModel()
    {
        return new ExtractionZone
        {
            Id = Guid.TryParse(Id, out var guid) ? guid : Guid.NewGuid(),
            Name = Name,
            Description = Description,
            X = X,
            Y = Y,
            Width = Width,
            Height = Height,
            PageNumber = PageNumber,
            CoordinateSystem = Enum.TryParse<CoordinateSystem>(CoordinateSystem, out var coordSystem) ? coordSystem : Models.CoordinateSystem.Pixel,
            ZoneType = Enum.TryParse<ExtractionZoneType>(ZoneType, out var zoneType) ? zoneType : ExtractionZoneType.Text,
            Priority = Priority,
            IsActive = IsActive,
            PositionTolerance = PositionTolerance,
            SizeTolerance = SizeTolerance
        };
    }

    /// <summary>
    /// Validates the serializable zone
    /// </summary>
    public TemplateValidationResult Validate()
    {
        var result = new TemplateValidationResult();

        if (string.IsNullOrWhiteSpace(Name))
        {
            result.Errors.Add("Zone name is required");
        }

        if (X < 0 || Y < 0)
        {
            result.Errors.Add("Zone coordinates must be non-negative");
        }

        if (Width <= 0 || Height <= 0)
        {
            result.Errors.Add("Zone dimensions must be positive");
        }

        if (PageNumber <= 0)
        {
            result.Errors.Add("Page number must be positive");
        }

        if (Priority < 0)
        {
            result.Errors.Add("Priority must be non-negative");
        }

        result.IsValid = result.Errors.Count == 0;
        return result;
    }
}

/// <summary>
/// Template version information for export
/// </summary>
public class TemplateVersionInfo
{
    [JsonPropertyName("version_number")]
    public string VersionNumber { get; set; } = string.Empty;

    [JsonPropertyName("version_description")]
    public string? VersionDescription { get; set; }

    [JsonPropertyName("created_by")]
    public string? CreatedBy { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("is_current")]
    public bool IsCurrent { get; set; }

    [JsonPropertyName("changes_summary")]
    public List<string> ChangesSummary { get; set; } = new();
}

/// <summary>
/// Template usage statistics for export
/// </summary>
public class TemplateUsageStatistics
{
    [JsonPropertyName("total_uses")]
    public int TotalUses { get; set; }

    [JsonPropertyName("successful_uses")]
    public int SuccessfulUses { get; set; }

    [JsonPropertyName("failed_uses")]
    public int FailedUses { get; set; }

    [JsonPropertyName("success_rate")]
    public double SuccessRate { get; set; }

    [JsonPropertyName("last_used")]
    public DateTime? LastUsed { get; set; }

    [JsonPropertyName("average_extraction_time_ms")]
    public double AverageExtractionTimeMs { get; set; }

    [JsonPropertyName("most_common_document_types")]
    public Dictionary<string, int> MostCommonDocumentTypes { get; set; } = new();

    [JsonPropertyName("performance_trend")]
    public List<PerformanceDataPoint> PerformanceTrend { get; set; } = new();
}

/// <summary>
/// Performance data point for trend analysis
/// </summary>
public class PerformanceDataPoint
{
    [JsonPropertyName("date")]
    public DateTime Date { get; set; }

    [JsonPropertyName("success_rate")]
    public double SuccessRate { get; set; }

    [JsonPropertyName("usage_count")]
    public int UsageCount { get; set; }

    [JsonPropertyName("avg_extraction_time_ms")]
    public double AverageExtractionTimeMs { get; set; }
}

/// <summary>
/// Types of template exports
/// </summary>
public enum TemplateExportType
{
    /// <summary>
    /// Minimal export with only essential template data
    /// </summary>
    Minimal,

    /// <summary>
    /// Standard export with template and fields
    /// </summary>
    Standard,

    /// <summary>
    /// Complete export with all data including statistics and history
    /// </summary>
    Complete,

    /// <summary>
    /// Backup export with full database representation
    /// </summary>
    Backup
}

 