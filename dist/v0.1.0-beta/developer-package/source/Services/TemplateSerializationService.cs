using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using AnomaliImportTool.Core.Models;
using Microsoft.Extensions.Logging;

namespace AnomaliImportTool.Core.Services;

/// <summary>
/// Service for serializing and deserializing import templates to/from JSON format
/// Supports compression, validation, and integrity checking
/// </summary>
public class TemplateSerializationService
{
    private readonly ILogger<TemplateSerializationService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the TemplateSerializationService
    /// </summary>
    /// <param name="logger">Logger instance</param>
    public TemplateSerializationService(ILogger<TemplateSerializationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Configure JSON serialization options
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase, // Use CamelCase instead of SnakeCaseLower
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };
    }

    /// <summary>
    /// Exports a template to JSON format with specified options
    /// </summary>
    /// <param name="template">Template to export</param>
    /// <param name="fields">Template fields</param>
    /// <param name="options">Export options</param>
    /// <returns>Template export result</returns>
    public async Task<TemplateExportResult> ExportTemplateAsync(
        ImportTemplate template, 
        List<TemplateField> fields, 
        TemplateExportOptions options)
    {
        try
        {
            _logger.LogInformation("Starting template export for template: {TemplateName}", template.Name);

            // Create export format based on options
            var exportFormat = await CreateExportFormatAsync(template, fields, options);
            
            // Validate export before serialization
            var validation = exportFormat.Validate();
            if (!validation.IsValid)
            {
                return new TemplateExportResult
                {
                    Success = false,
                    ErrorMessage = $"Export validation failed: {string.Join(", ", validation.Errors)}",
                    ValidationResult = validation
                };
            }

            // Serialize to JSON
            var jsonString = JsonSerializer.Serialize(exportFormat, _jsonOptions);
            var jsonBytes = Encoding.UTF8.GetBytes(jsonString);

            // Apply compression if requested
            var finalBytes = jsonBytes;
            var compressionUsed = "none";
            
            if (options.UseCompression)
            {
                finalBytes = await CompressDataAsync(jsonBytes);
                compressionUsed = "gzip";
                _logger.LogDebug("Template compressed from {OriginalSize} to {CompressedSize} bytes", 
                    jsonBytes.Length, finalBytes.Length);
            }

            // Generate checksum
            var checksum = GenerateChecksum(finalBytes);

            // Update export metadata
            exportFormat.ExportMetadata.FileSizeBytes = finalBytes.Length;
            exportFormat.ExportMetadata.Checksum = checksum;
            exportFormat.ExportMetadata.Compression = compressionUsed;

            var result = new TemplateExportResult
            {
                Success = true,
                ExportFormat = exportFormat,
                JsonContent = jsonString,
                BinaryContent = finalBytes,
                Checksum = checksum,
                CompressionUsed = compressionUsed,
                OriginalSizeBytes = jsonBytes.Length,
                FinalSizeBytes = finalBytes.Length,
                ValidationResult = validation
            };

            _logger.LogInformation("Template export completed successfully. Size: {Size} bytes, Compression: {Compression}", 
                finalBytes.Length, compressionUsed);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export template: {TemplateName}", template.Name);
            return new TemplateExportResult
            {
                Success = false,
                ErrorMessage = $"Export failed: {ex.Message}",
                Exception = ex
            };
        }
    }

    /// <summary>
    /// Imports a template from JSON format
    /// </summary>
    /// <param name="jsonContent">JSON content to import</param>
    /// <param name="options">Import options</param>
    /// <returns>Template import result</returns>
    public async Task<TemplateImportResult> ImportTemplateAsync(string jsonContent, SerializationImportOptions options)
    {
        try
        {
            _logger.LogInformation("Starting template import from JSON content");

            // Deserialize JSON
            var exportFormat = JsonSerializer.Deserialize<TemplateExportFormat>(jsonContent, _jsonOptions);
            if (exportFormat == null)
            {
                return new TemplateImportResult
                {
                    Success = false,
                    ErrorMessage = "Failed to deserialize JSON content"
                };
            }

            return await ImportTemplateAsync(exportFormat, options);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization failed during template import");
            return new TemplateImportResult
            {
                Success = false,
                ErrorMessage = $"JSON parsing failed: {ex.Message}",
                Exception = ex
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Template import failed");
            return new TemplateImportResult
            {
                Success = false,
                ErrorMessage = $"Import failed: {ex.Message}",
                Exception = ex
            };
        }
    }

    /// <summary>
    /// Imports a template from binary data (potentially compressed)
    /// </summary>
    /// <param name="binaryData">Binary data to import</param>
    /// <param name="options">Import options</param>
    /// <returns>Template import result</returns>
    public async Task<TemplateImportResult> ImportTemplateAsync(byte[] binaryData, SerializationImportOptions options)
    {
        try
        {
            _logger.LogInformation("Starting template import from binary data. Size: {Size} bytes", binaryData.Length);

            // Try to decompress if it looks like compressed data
            var jsonBytes = binaryData;
            var isCompressed = false;

            if (IsGzipCompressed(binaryData))
            {
                jsonBytes = await DecompressDataAsync(binaryData);
                isCompressed = true;
                _logger.LogDebug("Decompressed data from {CompressedSize} to {DecompressedSize} bytes", 
                    binaryData.Length, jsonBytes.Length);
            }

            // Convert to string and import
            var jsonContent = Encoding.UTF8.GetString(jsonBytes);
            var result = await ImportTemplateAsync(jsonContent, options);
            
            if (result.Success && isCompressed)
            {
                result.WasCompressed = true;
                result.CompressedSizeBytes = binaryData.Length;
                result.DecompressedSizeBytes = jsonBytes.Length;
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Binary template import failed");
            return new TemplateImportResult
            {
                Success = false,
                ErrorMessage = $"Binary import failed: {ex.Message}",
                Exception = ex
            };
        }
    }

    /// <summary>
    /// Imports a template from export format
    /// </summary>
    /// <param name="exportFormat">Export format to import</param>
    /// <param name="options">Import options</param>
    /// <returns>Template import result</returns>
    public async Task<TemplateImportResult> ImportTemplateAsync(TemplateExportFormat exportFormat, SerializationImportOptions options)
    {
        try
        {
            _logger.LogInformation("Starting template import from export format: {TemplateName}", exportFormat.Template.Name);

            // Validate import format
            var validation = exportFormat.Validate();
            if (!validation.IsValid && !options.IgnoreValidationErrors)
            {
                return new TemplateImportResult
                {
                    Success = false,
                    ErrorMessage = $"Import validation failed: {string.Join(", ", validation.Errors)}",
                    ValidationResult = validation
                };
            }

            // Check format version compatibility
            if (!IsFormatVersionCompatible(exportFormat.ExportFormatVersion))
            {
                if (!options.IgnoreVersionMismatch)
                {
                    return new TemplateImportResult
                    {
                        Success = false,
                        ErrorMessage = $"Incompatible export format version: {exportFormat.ExportFormatVersion}"
                    };
                }
                
                validation.Warnings.Add($"Import format version {exportFormat.ExportFormatVersion} may not be fully compatible");
            }

            // Convert to domain models
            var template = exportFormat.Template.ToDomainModel();
            var fields = exportFormat.Fields.Select(f => f.ToDomainModel()).ToList();
            var zones = exportFormat.ExtractionZones?.Select(z => z.ToDomainModel()).ToList() ?? new List<ExtractionZone>();

            // Handle ID conflicts if needed
            if (options.GenerateNewIds)
            {
                template.Id = Guid.NewGuid();
                foreach (var field in fields)
                {
                    field.Id = Guid.NewGuid();
                }
                foreach (var zone in zones)
                {
                    zone.Id = Guid.NewGuid();
                }
            }

            // Handle name conflicts
            if (!string.IsNullOrEmpty(options.NameSuffix))
            {
                template.Name += options.NameSuffix;
            }

            // Update metadata for import
            template.LastModifiedAt = DateTime.UtcNow;
            template.LastModifiedBy = options.ImportedBy;

            var result = new TemplateImportResult
            {
                Success = true,
                ImportedTemplate = template,
                ImportedFields = fields,
                ImportedZones = zones,
                ValidationResult = validation,
                ExportMetadata = exportFormat.ExportMetadata,
                VersionHistory = exportFormat.VersionHistory,
                UsageStatistics = exportFormat.UsageStatistics
            };

            _logger.LogInformation("Template import completed successfully: {TemplateName}", template.Name);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Template import from export format failed");
            return new TemplateImportResult
            {
                Success = false,
                ErrorMessage = $"Import failed: {ex.Message}",
                Exception = ex
            };
        }
    }

    /// <summary>
    /// Validates a template export without importing it
    /// </summary>
    /// <param name="jsonContent">JSON content to validate</param>
    /// <returns>Validation result</returns>
    public async Task<TemplateValidationResult> ValidateTemplateAsync(string jsonContent)
    {
        try
        {
            var exportFormat = JsonSerializer.Deserialize<TemplateExportFormat>(jsonContent, _jsonOptions);
            if (exportFormat == null)
            {
                return new TemplateValidationResult
                {
                    IsValid = false,
                    Errors = { "Failed to deserialize JSON content" }
                };
            }

            var result = exportFormat.Validate();
            
            // Add format version check
            if (!IsFormatVersionCompatible(exportFormat.ExportFormatVersion))
            {
                result.Warnings.Add($"Export format version {exportFormat.ExportFormatVersion} may not be fully compatible");
            }

            return result;
        }
        catch (JsonException ex)
        {
            return new TemplateValidationResult
            {
                IsValid = false,
                Errors = { $"JSON parsing failed: {ex.Message}" }
            };
        }
        catch (Exception ex)
        {
            return new TemplateValidationResult
            {
                IsValid = false,
                Errors = { $"Validation failed: {ex.Message}" }
            };
        }
    }

    /// <summary>
    /// Exports a template to a file
    /// </summary>
    /// <param name="template">Template to export</param>
    /// <param name="fields">Template fields</param>
    /// <param name="filePath">Output file path</param>
    /// <param name="options">Export options</param>
    /// <returns>Export result</returns>
    public async Task<TemplateExportResult> ExportTemplateToFileAsync(
        ImportTemplate template, 
        List<TemplateField> fields, 
        string filePath, 
        TemplateExportOptions options)
    {
        var exportResult = await ExportTemplateAsync(template, fields, options);
        
        if (exportResult.Success)
        {
            try
            {
                await File.WriteAllBytesAsync(filePath, exportResult.BinaryContent);
                exportResult.ExportedFilePath = filePath;
                _logger.LogInformation("Template exported to file: {FilePath}", filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write export file: {FilePath}", filePath);
                exportResult.Success = false;
                exportResult.ErrorMessage = $"Failed to write file: {ex.Message}";
            }
        }

        return exportResult;
    }

    /// <summary>
    /// Imports a template from a file
    /// </summary>
    /// <param name="filePath">File path to import from</param>
    /// <param name="options">Import options</param>
    /// <returns>Import result</returns>
    public async Task<TemplateImportResult> ImportTemplateFromFileAsync(string filePath, SerializationImportOptions options)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return new TemplateImportResult
                {
                    Success = false,
                    ErrorMessage = $"File not found: {filePath}"
                };
            }

            var fileData = await File.ReadAllBytesAsync(filePath);
            var result = await ImportTemplateAsync(fileData, options);
            
            if (result.Success)
            {
                result.ImportedFilePath = filePath;
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import template from file: {FilePath}", filePath);
            return new TemplateImportResult
            {
                Success = false,
                ErrorMessage = $"File import failed: {ex.Message}",
                Exception = ex
            };
        }
    }

    #region Private Methods

    /// <summary>
    /// Creates export format based on options
    /// </summary>
    private async Task<TemplateExportFormat> CreateExportFormatAsync(
        ImportTemplate template, 
        List<TemplateField> fields, 
        TemplateExportOptions options)
    {
        var exportFormat = new TemplateExportFormat
        {
            ExportedBy = options.ExportedBy,
            ExportMetadata = new TemplateExportMetadata
            {
                ExportType = options.ExportType,
                ExportPurpose = options.ExportPurpose,
                TargetSystem = options.TargetSystem,
                IncludeUsageStatistics = options.IncludeUsageStatistics,
                IncludeVersionHistory = options.IncludeVersionHistory,
                IncludeExtractionZones = options.IncludeExtractionZones,
                CustomMetadata = options.CustomMetadata
            },
            Template = SerializableImportTemplate.FromDomainModel(template),
            Fields = fields.Select(SerializableTemplateField.FromDomainModel).ToList()
        };

        // Add extraction zones if requested
        if (options.IncludeExtractionZones && options.ExtractionZones != null)
        {
            exportFormat.ExtractionZones = options.ExtractionZones
                .Select(SerializableExtractionZone.FromDomainModel).ToList();
        }

        // Add version history if requested
        if (options.IncludeVersionHistory && options.VersionHistory != null)
        {
            exportFormat.VersionHistory = options.VersionHistory;
        }

        // Add usage statistics if requested
        if (options.IncludeUsageStatistics && options.UsageStatistics != null)
        {
            exportFormat.UsageStatistics = options.UsageStatistics;
        }

        return exportFormat;
    }

    /// <summary>
    /// Compresses data using GZip
    /// </summary>
    private async Task<byte[]> CompressDataAsync(byte[] data)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionMode.Compress))
        {
            await gzip.WriteAsync(data, 0, data.Length);
        }
        return output.ToArray();
    }

    /// <summary>
    /// Decompresses GZip data
    /// </summary>
    private async Task<byte[]> DecompressDataAsync(byte[] compressedData)
    {
        using var input = new MemoryStream(compressedData);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        await gzip.CopyToAsync(output);
        return output.ToArray();
    }

    /// <summary>
    /// Checks if data is GZip compressed
    /// </summary>
    private bool IsGzipCompressed(byte[] data)
    {
        return data.Length >= 2 && data[0] == 0x1f && data[1] == 0x8b;
    }

    /// <summary>
    /// Generates SHA256 checksum for data integrity
    /// </summary>
    private string GenerateChecksum(byte[] data)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(data);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Checks if export format version is compatible
    /// </summary>
    private bool IsFormatVersionCompatible(string version)
    {
        // For now, we support version 1.x.x
        return version.StartsWith("1.");
    }

    #endregion
}

/// <summary>
/// Options for template export operations
/// </summary>
public class TemplateExportOptions
{
    /// <summary>
    /// Type of export to perform
    /// </summary>
    public TemplateExportType ExportType { get; set; } = TemplateExportType.Standard;

    /// <summary>
    /// Who is performing the export
    /// </summary>
    public string? ExportedBy { get; set; }

    /// <summary>
    /// Purpose of the export
    /// </summary>
    public string? ExportPurpose { get; set; }

    /// <summary>
    /// Target system for the export
    /// </summary>
    public string? TargetSystem { get; set; }

    /// <summary>
    /// Whether to include extraction zones
    /// </summary>
    public bool IncludeExtractionZones { get; set; } = true;

    /// <summary>
    /// Whether to include usage statistics
    /// </summary>
    public bool IncludeUsageStatistics { get; set; } = false;

    /// <summary>
    /// Whether to include version history
    /// </summary>
    public bool IncludeVersionHistory { get; set; } = false;

    /// <summary>
    /// Whether to use compression
    /// </summary>
    public bool UseCompression { get; set; } = false;

    /// <summary>
    /// Extraction zones to include (if IncludeExtractionZones is true)
    /// </summary>
    public List<ExtractionZone>? ExtractionZones { get; set; }

    /// <summary>
    /// Version history to include (if IncludeVersionHistory is true)
    /// </summary>
    public List<TemplateVersionInfo>? VersionHistory { get; set; }

    /// <summary>
    /// Usage statistics to include (if IncludeUsageStatistics is true)
    /// </summary>
    public TemplateUsageStatistics? UsageStatistics { get; set; }

    /// <summary>
    /// Custom metadata to include
    /// </summary>
    public Dictionary<string, object> CustomMetadata { get; set; } = new();
}

/// <summary>
/// Options for template import operations
/// </summary>
public class SerializationImportOptions
{
    /// <summary>
    /// Who is performing the import
    /// </summary>
    public string? ImportedBy { get; set; }

    /// <summary>
    /// Whether to generate new IDs for imported objects
    /// </summary>
    public bool GenerateNewIds { get; set; } = true;

    /// <summary>
    /// Whether to ignore validation errors during import
    /// </summary>
    public bool IgnoreValidationErrors { get; set; } = false;

    /// <summary>
    /// Whether to ignore version mismatches
    /// </summary>
    public bool IgnoreVersionMismatch { get; set; } = false;

    /// <summary>
    /// Suffix to add to template name (for conflict resolution)
    /// </summary>
    public string? NameSuffix { get; set; }

    /// <summary>
    /// Whether to overwrite existing templates with same ID
    /// </summary>
    public bool OverwriteExisting { get; set; } = false;

    /// <summary>
    /// Custom import settings
    /// </summary>
    public Dictionary<string, object> CustomSettings { get; set; } = new();
}

/// <summary>
/// Result of template export operation
/// </summary>
public class TemplateExportResult
{
    /// <summary>
    /// Whether the export was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if export failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Exception that occurred during export
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// The exported template format
    /// </summary>
    public TemplateExportFormat? ExportFormat { get; set; }

    /// <summary>
    /// JSON content as string
    /// </summary>
    public string? JsonContent { get; set; }

    /// <summary>
    /// Binary content (potentially compressed)
    /// </summary>
    public byte[] BinaryContent { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Checksum of the exported data
    /// </summary>
    public string? Checksum { get; set; }

    /// <summary>
    /// Compression method used
    /// </summary>
    public string CompressionUsed { get; set; } = "none";

    /// <summary>
    /// Original size before compression
    /// </summary>
    public long OriginalSizeBytes { get; set; }

    /// <summary>
    /// Final size after compression
    /// </summary>
    public long FinalSizeBytes { get; set; }

    /// <summary>
    /// Validation result
    /// </summary>
    public TemplateValidationResult? ValidationResult { get; set; }

    /// <summary>
    /// Path where the template was exported (if applicable)
    /// </summary>
    public string? ExportedFilePath { get; set; }
}

/// <summary>
/// Result of template import operation
/// </summary>
public class TemplateImportResult
{
    /// <summary>
    /// Whether the import was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if import failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Exception that occurred during import
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// The imported template
    /// </summary>
    public ImportTemplate? ImportedTemplate { get; set; }

    /// <summary>
    /// The imported fields
    /// </summary>
    public List<TemplateField> ImportedFields { get; set; } = new();

    /// <summary>
    /// The imported extraction zones
    /// </summary>
    public List<ExtractionZone> ImportedZones { get; set; } = new();

    /// <summary>
    /// Validation result
    /// </summary>
    public TemplateValidationResult? ValidationResult { get; set; }

    /// <summary>
    /// Export metadata from the imported template
    /// </summary>
    public TemplateExportMetadata? ExportMetadata { get; set; }

    /// <summary>
    /// Version history from the imported template
    /// </summary>
    public List<TemplateVersionInfo>? VersionHistory { get; set; }

    /// <summary>
    /// Usage statistics from the imported template
    /// </summary>
    public TemplateUsageStatistics? UsageStatistics { get; set; }

    /// <summary>
    /// Whether the import data was compressed
    /// </summary>
    public bool WasCompressed { get; set; }

    /// <summary>
    /// Compressed size if applicable
    /// </summary>
    public long CompressedSizeBytes { get; set; }

    /// <summary>
    /// Decompressed size if applicable
    /// </summary>
    public long DecompressedSizeBytes { get; set; }

    /// <summary>
    /// Path where the template was imported from (if applicable)
    /// </summary>
    public string? ImportedFilePath { get; set; }
} 