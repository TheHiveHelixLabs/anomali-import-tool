using AnomaliImportTool.Core.Models;
using AnomaliImportTool.Infrastructure.Services;

namespace AnomaliImportTool.Core.Interfaces;

/// <summary>
/// Interface for managing import templates with CRUD operations, search, categorization, and versioning
/// </summary>
public interface IImportTemplateService
{
    #region Template CRUD Operations

    /// <summary>
    /// Creates a new import template
    /// </summary>
    /// <param name="template">Template to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created template with assigned ID</returns>
    Task<ImportTemplate> CreateTemplateAsync(ImportTemplate template, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a template by its ID
    /// </summary>
    /// <param name="templateId">Template ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Template if found, null otherwise</returns>
    Task<ImportTemplate?> GetTemplateAsync(Guid templateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a template by name
    /// </summary>
    /// <param name="templateName">Template name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Template if found, null otherwise</returns>
    Task<ImportTemplate?> GetTemplateByNameAsync(string templateName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing template
    /// </summary>
    /// <param name="template">Template to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated template</returns>
    Task<ImportTemplate> UpdateTemplateAsync(ImportTemplate template, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a template by ID
    /// </summary>
    /// <param name="templateId">Template ID to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteTemplateAsync(Guid templateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all templates
    /// </summary>
    /// <param name="includeInactive">Whether to include inactive templates</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all templates</returns>
    Task<IEnumerable<ImportTemplate>> GetAllTemplatesAsync(bool includeInactive = false, CancellationToken cancellationToken = default);

    #endregion

    #region Template Search and Filtering

    /// <summary>
    /// Searches templates by various criteria
    /// </summary>
    /// <param name="searchCriteria">Search criteria</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Templates matching the criteria</returns>
    Task<IEnumerable<ImportTemplate>> SearchTemplatesAsync(TemplateSearchCriteria searchCriteria, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets templates by category
    /// </summary>
    /// <param name="category">Category name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Templates in the specified category</returns>
    Task<IEnumerable<ImportTemplate>> GetTemplatesByCategoryAsync(string category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets templates that support a specific document format
    /// </summary>
    /// <param name="format">Document format (e.g., "pdf", "docx")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Templates supporting the format</returns>
    Task<IEnumerable<ImportTemplate>> GetTemplatesByFormatAsync(string format, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets templates by tags
    /// </summary>
    /// <param name="tags">Tags to search for</param>
    /// <param name="matchAll">Whether all tags must match (AND) or any tag (OR)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Templates matching the tags</returns>
    Task<IEnumerable<ImportTemplate>> GetTemplatesByTagsAsync(IEnumerable<string> tags, bool matchAll = false, CancellationToken cancellationToken = default);

    #endregion

    #region Template Categorization and Organization

    /// <summary>
    /// Gets all available template categories
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of unique categories</returns>
    Task<IEnumerable<string>> GetCategoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available template tags
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of unique tags</returns>
    Task<IEnumerable<string>> GetTagsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets template statistics by category
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Category statistics</returns>
    Task<IDictionary<string, int>> GetCategoryStatisticsAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Template Versioning

    /// <summary>
    /// Creates a new version of an existing template
    /// </summary>
    /// <param name="templateId">Original template ID</param>
    /// <param name="newVersion">New version number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New template version</returns>
    Task<ImportTemplate> CreateTemplateVersionAsync(Guid templateId, string newVersion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all versions of a template
    /// </summary>
    /// <param name="templateName">Template name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>All versions of the template</returns>
    Task<IEnumerable<ImportTemplate>> GetTemplateVersionsAsync(string templateName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest version of a template
    /// </summary>
    /// <param name="templateName">Template name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Latest version of the template</returns>
    Task<ImportTemplate?> GetLatestTemplateVersionAsync(string templateName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific version of a template
    /// </summary>
    /// <param name="templateId">Template ID</param>
    /// <param name="version">Version number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Template version if found, null otherwise</returns>
    Task<ImportTemplate?> GetTemplateVersionAsync(Guid templateId, string version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back a template to a previous version
    /// </summary>
    /// <param name="templateId">Template ID</param>
    /// <param name="targetVersion">Version to rollback to</param>
    /// <param name="rollbackReason">Reason for rollback</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Template after rollback</returns>
    Task<ImportTemplate> RollbackToVersionAsync(Guid templateId, string targetVersion, string rollbackReason = "", CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the change history for a template
    /// </summary>
    /// <param name="templateId">Template ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Change history records</returns>
    Task<IEnumerable<TemplateChangeRecord>> GetTemplateChangeHistoryAsync(Guid templateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Compares two versions of a template
    /// </summary>
    /// <param name="templateId">Template ID</param>
    /// <param name="version1">First version to compare</param>
    /// <param name="version2">Second version to compare</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Comparison result with differences</returns>
    Task<TemplateComparisonResult> CompareTemplateVersionsAsync(Guid templateId, string version1, string version2, CancellationToken cancellationToken = default);

    #endregion

    #region Template Import/Export

    /// <summary>
    /// Exports a template to JSON format
    /// </summary>
    /// <param name="templateId">Template ID to export</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>JSON representation of the template</returns>
    Task<string> ExportTemplateAsync(Guid templateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports multiple templates to JSON format
    /// </summary>
    /// <param name="templateIds">Template IDs to export</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>JSON representation of the templates</returns>
    Task<string> ExportTemplatesAsync(IEnumerable<Guid> templateIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports a template from JSON format
    /// </summary>
    /// <param name="templateJson">JSON representation of the template</param>
    /// <param name="importOptions">Import options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Imported template</returns>
    Task<ImportTemplate> ImportTemplateAsync(string templateJson, TemplateImportOptions? importOptions = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports multiple templates from JSON format
    /// </summary>
    /// <param name="templatesJson">JSON representation of multiple templates</param>
    /// <param name="importOptions">Import options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Imported templates</returns>
    Task<IEnumerable<ImportTemplate>> ImportTemplatesAsync(string templatesJson, TemplateImportOptions? importOptions = null, CancellationToken cancellationToken = default);

    #endregion

    #region Template Validation and Testing

    /// <summary>
    /// Validates a template structure and configuration
    /// </summary>
    /// <param name="template">Template to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    Task<TemplateValidationResult> ValidateTemplateAsync(ImportTemplate template, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests a template against a sample document
    /// </summary>
    /// <param name="templateId">Template ID</param>
    /// <param name="documentPath">Path to test document</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Test results with extracted data</returns>
    Task<TemplateTestResult> TestTemplateAsync(Guid templateId, string documentPath, CancellationToken cancellationToken = default);

    #endregion

    #region Template Usage Statistics

    /// <summary>
    /// Updates template usage statistics
    /// </summary>
    /// <param name="templateId">Template ID</param>
    /// <param name="successful">Whether the template was applied successfully</param>
    /// <param name="extractionTime">Time taken for extraction</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task UpdateUsageStatisticsAsync(Guid templateId, bool successful, TimeSpan extractionTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets usage statistics for a template
    /// </summary>
    /// <param name="templateId">Template ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Usage statistics</returns>
    Task<TemplateUsageStats> GetUsageStatisticsAsync(Guid templateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets most frequently used templates
    /// </summary>
    /// <param name="count">Number of templates to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Most used templates</returns>
    Task<IEnumerable<ImportTemplate>> GetMostUsedTemplatesAsync(int count = 10, CancellationToken cancellationToken = default);

    #endregion

    #region Template Activation and Management

    /// <summary>
    /// Activates a template
    /// </summary>
    /// <param name="templateId">Template ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated template</returns>
    Task<ImportTemplate> ActivateTemplateAsync(Guid templateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates a template
    /// </summary>
    /// <param name="templateId">Template ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated template</returns>
    Task<ImportTemplate> DeactivateTemplateAsync(Guid templateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Duplicates an existing template
    /// </summary>
    /// <param name="templateId">Template ID to duplicate</param>
    /// <param name="newName">Name for the duplicated template</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Duplicated template</returns>
    Task<ImportTemplate> DuplicateTemplateAsync(Guid templateId, string newName, CancellationToken cancellationToken = default);

    #endregion

    /// <summary>
    /// Compares two template versions and returns the differences
    /// </summary>
    /// <param name="templateId">Template ID</param>
    /// <param name="fromVersion">Source version to compare from</param>
    /// <param name="toVersion">Target version to compare to</param>
    /// <returns>Comparison result with differences</returns>
    Task<TemplateComparisonResult> CompareTemplateVersionsAsync(Guid templateId, string fromVersion, string toVersion);

    // Template Inheritance Methods

    /// <summary>
    /// Creates an inheritance relationship between a child and parent template
    /// </summary>
    /// <param name="childTemplateId">ID of the child template</param>
    /// <param name="parentTemplateId">ID of the parent template</param>
    /// <param name="inheritanceConfig">Configuration for the inheritance</param>
    /// <returns>The created inheritance relationship</returns>
    Task<TemplateInheritanceRelationship> CreateInheritanceAsync(Guid childTemplateId, Guid parentTemplateId, TemplateInheritanceConfig inheritanceConfig);

    /// <summary>
    /// Removes an inheritance relationship
    /// </summary>
    /// <param name="childTemplateId">ID of the child template</param>
    /// <param name="parentTemplateId">ID of the parent template</param>
    /// <returns>True if relationship was removed</returns>
    Task<bool> RemoveInheritanceAsync(Guid childTemplateId, Guid parentTemplateId);

    /// <summary>
    /// Gets all inheritance relationships for a template (as child)
    /// </summary>
    /// <param name="templateId">Template ID</param>
    /// <returns>List of inheritance relationships</returns>
    Task<List<TemplateInheritanceRelationship>> GetTemplateInheritanceAsync(Guid templateId);

    /// <summary>
    /// Gets all child templates that inherit from a parent template
    /// </summary>
    /// <param name="parentTemplateId">Parent template ID</param>
    /// <returns>List of child template inheritance relationships</returns>
    Task<List<TemplateInheritanceRelationship>> GetChildTemplatesAsync(Guid parentTemplateId);

    /// <summary>
    /// Resolves a template with all inheritance applied
    /// </summary>
    /// <param name="templateId">Template ID to resolve</param>
    /// <returns>Template inheritance result with resolved template</returns>
    Task<TemplateInheritanceResult> ResolveTemplateInheritanceAsync(Guid templateId);

    /// <summary>
    /// Validates that an inheritance relationship would not create a cycle
    /// </summary>
    /// <param name="childTemplateId">Proposed child template ID</param>
    /// <param name="parentTemplateId">Proposed parent template ID</param>
    /// <returns>True if inheritance is valid (no cycles)</returns>
    Task<bool> ValidateInheritanceAsync(Guid childTemplateId, Guid parentTemplateId);

    /// <summary>
    /// Gets the complete inheritance chain for a template
    /// </summary>
    /// <param name="templateId">Template ID</param>
    /// <returns>List of template IDs in inheritance chain from root to current</returns>
    Task<List<Guid>> GetInheritanceChainAsync(Guid templateId);

    /// <summary>
    /// Updates inheritance configuration for an existing relationship
    /// </summary>
    /// <param name="childTemplateId">Child template ID</param>
    /// <param name="parentTemplateId">Parent template ID</param>
    /// <param name="inheritanceConfig">New inheritance configuration</param>
    /// <returns>Updated inheritance relationship</returns>
    Task<TemplateInheritanceRelationship> UpdateInheritanceConfigAsync(Guid childTemplateId, Guid parentTemplateId, TemplateInheritanceConfig inheritanceConfig);

    /// <summary>
    /// Gets all templates that can be used as parent templates (no cycles)
    /// </summary>
    /// <param name="forTemplateId">Template ID that would be the child</param>
    /// <returns>List of available parent templates</returns>
    Task<List<ImportTemplate>> GetAvailableParentTemplatesAsync(Guid forTemplateId);
}

/// <summary>
/// Search criteria for template queries
/// </summary>
public class TemplateSearchCriteria
{
    /// <summary>
    /// Search term for name and description
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Category filter
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Tags filter
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Supported formats filter
    /// </summary>
    public List<string> SupportedFormats { get; set; } = new();

    /// <summary>
    /// Created by filter
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Date range filter - start
    /// </summary>
    public DateTime? CreatedAfter { get; set; }

    /// <summary>
    /// Date range filter - end
    /// </summary>
    public DateTime? CreatedBefore { get; set; }

    /// <summary>
    /// Include inactive templates
    /// </summary>
    public bool IncludeInactive { get; set; } = false;

    /// <summary>
    /// Sort by field
    /// </summary>
    public TemplateSortField SortBy { get; set; } = TemplateSortField.Name;

    /// <summary>
    /// Sort direction
    /// </summary>
    public SortDirection SortDirection { get; set; } = SortDirection.Ascending;

    /// <summary>
    /// Maximum number of results
    /// </summary>
    public int? MaxResults { get; set; }
}

/// <summary>
/// Template import options
/// </summary>
public class TemplateImportOptions
{
    /// <summary>
    /// Whether to overwrite existing templates with the same name
    /// </summary>
    public bool OverwriteExisting { get; set; } = false;

    /// <summary>
    /// Whether to validate templates during import
    /// </summary>
    public bool ValidateOnImport { get; set; } = true;

    /// <summary>
    /// Whether to assign new IDs to imported templates
    /// </summary>
    public bool AssignNewIds { get; set; } = true;

    /// <summary>
    /// User performing the import
    /// </summary>
    public string? ImportedBy { get; set; }

    /// <summary>
    /// Whether to preserve creation dates
    /// </summary>
    public bool PreserveCreationDates { get; set; } = false;
}

/// <summary>
/// Template test result
/// </summary>
public class TemplateTestResult
{
    /// <summary>
    /// Whether the test was successful
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// Extracted field data
    /// </summary>
    public Dictionary<string, string> ExtractedFields { get; set; } = new();

    /// <summary>
    /// Confidence scores for each field (0.0 to 1.0)
    /// </summary>
    public Dictionary<string, double> FieldConfidenceScores { get; set; } = new();

    /// <summary>
    /// Overall confidence score
    /// </summary>
    public double OverallConfidence { get; set; }

    /// <summary>
    /// Time taken for extraction
    /// </summary>
    public TimeSpan ExtractionTime { get; set; }

    /// <summary>
    /// Any errors encountered during testing
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Any warnings generated during testing
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Additional test metadata
    /// </summary>
    public Dictionary<string, object> TestMetadata { get; set; } = new();
}

/// <summary>
/// Template sort fields
/// </summary>
public enum TemplateSortField
{
    Name,
    Category,
    CreatedAt,
    LastModifiedAt,
    Version,
    UsageCount,
    SuccessRate
}

/// <summary>
/// Sort direction
/// </summary>
public enum SortDirection
{
    Ascending,
    Descending
} 