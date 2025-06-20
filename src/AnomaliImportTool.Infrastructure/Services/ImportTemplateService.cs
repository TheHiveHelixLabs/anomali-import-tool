using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AnomaliImportTool.Core.Interfaces;
using AnomaliImportTool.Core.Models;
using AnomaliImportTool.Core.Services;
using AnomaliImportTool.Infrastructure.Database;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Xml.Linq;
using System.IO;
using Microsoft.Extensions.Logging.Abstractions;

namespace AnomaliImportTool.Infrastructure.Services;

/// <summary>
/// Implementation of IImportTemplateService providing CRUD operations, search, and categorization
/// Uses SQLite database for persistence with JSON serialization for complex objects
/// </summary>
public class ImportTemplateService : IImportTemplateService
{
    private readonly ILogger<ImportTemplateService> _logger;
    private readonly TemplateDatabaseService _databaseService;
    private readonly TemplateSerializationService _serializationService;

    public ImportTemplateService(
        ILogger<ImportTemplateService> logger,
        TemplateDatabaseService databaseService,
        TemplateSerializationService serializationService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        _serializationService = serializationService ?? throw new ArgumentNullException(nameof(serializationService));
    }

    #region Template CRUD Operations

    public async Task<ImportTemplate> CreateTemplateAsync(ImportTemplate template, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating new template: {TemplateName}", template.Name);

            // Validate template before creation
            var validationResult = template.ValidateTemplate();
            if (!validationResult.IsValid)
            {
                throw new InvalidOperationException($"Template validation failed: {string.Join(", ", validationResult.Errors)}");
            }

            // Ensure unique ID and set metadata
            template.Id = Guid.NewGuid();
            template.CreatedAt = DateTime.UtcNow;
            template.LastModifiedAt = DateTime.UtcNow;

            using var connection = await _databaseService.CreateAndOpenConnectionAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Insert main template record
                const string insertTemplateSql = @"
                    INSERT INTO import_templates (
                        id, name, description, version, category, created_by, last_modified_by,
                        created_at, last_modified_at, is_active, tags, supported_formats,
                        confidence_threshold, auto_apply, allow_partial_matches, template_priority,
                        document_matching_criteria, ocr_settings, template_validation, usage_stats
                    ) VALUES (
                        @id, @name, @description, @version, @category, @created_by, @last_modified_by,
                        @created_at, @last_modified_at, @is_active, @tags, @supported_formats,
                        @confidence_threshold, @auto_apply, @allow_partial_matches, @template_priority,
                        @document_matching_criteria, @ocr_settings, @template_validation, @usage_stats
                    )";

                using var command = new SqliteCommand(insertTemplateSql, connection, transaction);
                AddTemplateParameters(command, template);
                await command.ExecuteNonQueryAsync(cancellationToken);

                // Insert template fields
                await InsertTemplateFieldsAsync(connection, transaction, template.Id, template.Fields, cancellationToken);

                // Create initial version record
                await CreateTemplateVersionRecordAsync(connection, transaction, template, cancellationToken);

                transaction.Commit();
                
                _logger.LogInformation("Template created successfully: {TemplateId}", template.Id);
                return template;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create template: {TemplateName}", template.Name);
            throw;
        }
    }

    public async Task<ImportTemplate?> GetTemplateAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await _databaseService.CreateAndOpenConnectionAsync();
            
            const string sql = @"
                SELECT id, name, description, version, category, created_by, last_modified_by,
                       created_at, last_modified_at, is_active, tags, supported_formats,
                       confidence_threshold, auto_apply, allow_partial_matches, template_priority,
                       document_matching_criteria, ocr_settings, template_validation, usage_stats
                FROM import_templates 
                WHERE id = @id";

            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@id", templateId.ToString());

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                var template = await BuildTemplateFromReaderAsync(reader, cancellationToken);
                template.Fields = await GetTemplateFieldsAsync(connection, templateId, cancellationToken);
                return template;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get template: {TemplateId}", templateId);
            throw;
        }
    }

    public async Task<ImportTemplate?> GetTemplateByNameAsync(string templateName, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await _databaseService.CreateAndOpenConnectionAsync();
            
            const string sql = @"
                SELECT id, name, description, version, category, created_by, last_modified_by,
                       created_at, last_modified_at, is_active, tags, supported_formats,
                       confidence_threshold, auto_apply, allow_partial_matches, template_priority,
                       document_matching_criteria, ocr_settings, template_validation, usage_stats
                FROM import_templates 
                WHERE name = @name AND is_active = 1
                ORDER BY created_at DESC
                LIMIT 1";

            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@name", templateName);

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                var template = await BuildTemplateFromReaderAsync(reader, cancellationToken);
                template.Fields = await GetTemplateFieldsAsync(connection, template.Id, cancellationToken);
                return template;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get template by name: {TemplateName}", templateName);
            throw;
        }
    }

    public async Task<ImportTemplate> UpdateTemplateAsync(ImportTemplate template, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating template: {TemplateId}", template.Id);

            // Validate template before update
            var validationResult = template.ValidateTemplate();
            if (!validationResult.IsValid)
            {
                throw new InvalidOperationException($"Template validation failed: {string.Join(", ", validationResult.Errors)}");
            }

            template.LastModifiedAt = DateTime.UtcNow;

            using var connection = await _databaseService.CreateAndOpenConnectionAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Update main template record
                const string updateTemplateSql = @"
                    UPDATE import_templates SET
                        name = @name, description = @description, version = @version, 
                        category = @category, last_modified_by = @last_modified_by,
                        last_modified_at = @last_modified_at, is_active = @is_active,
                        tags = @tags, supported_formats = @supported_formats,
                        confidence_threshold = @confidence_threshold, auto_apply = @auto_apply,
                        allow_partial_matches = @allow_partial_matches, template_priority = @template_priority,
                        document_matching_criteria = @document_matching_criteria, 
                        ocr_settings = @ocr_settings, template_validation = @template_validation,
                        usage_stats = @usage_stats
                    WHERE id = @id";

                using var command = new SqliteCommand(updateTemplateSql, connection, transaction);
                AddTemplateParameters(command, template);
                var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);

                if (rowsAffected == 0)
                {
                    throw new InvalidOperationException($"Template with ID {template.Id} not found");
                }

                // Delete existing fields and insert updated ones
                await DeleteTemplateFieldsAsync(connection, transaction, template.Id, cancellationToken);
                await InsertTemplateFieldsAsync(connection, transaction, template.Id, template.Fields, cancellationToken);

                // Create version record if this is a version update
                await CreateTemplateVersionRecordAsync(connection, transaction, template, cancellationToken);

                transaction.Commit();
                
                _logger.LogInformation("Template updated successfully: {TemplateId}", template.Id);
                return template;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update template: {TemplateId}", template.Id);
            throw;
        }
    }

    public async Task<bool> DeleteTemplateAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting template: {TemplateId}", templateId);

            using var connection = await _databaseService.CreateAndOpenConnectionAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Soft delete - mark as inactive instead of physical deletion
                const string sql = @"
                    UPDATE import_templates 
                    SET is_active = 0, last_modified_at = @last_modified_at
                    WHERE id = @id";

                using var command = new SqliteCommand(sql, connection, transaction);
                command.Parameters.AddWithValue("@id", templateId.ToString());
                command.Parameters.AddWithValue("@last_modified_at", DateTime.UtcNow);

                var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
                transaction.Commit();

                if (rowsAffected > 0)
                {
                    _logger.LogInformation("Template deleted successfully: {TemplateId}", templateId);
                    return true;
                }

                return false;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete template: {TemplateId}", templateId);
            throw;
        }
    }

    public async Task<IEnumerable<ImportTemplate>> GetAllTemplatesAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await _databaseService.CreateAndOpenConnectionAsync();
            
            var whereClause = includeInactive ? "" : "WHERE is_active = 1";
            var sql = $@"
                SELECT id, name, description, version, category, created_by, last_modified_by,
                       created_at, last_modified_at, is_active, tags, supported_formats,
                       confidence_threshold, auto_apply, allow_partial_matches, template_priority,
                       document_matching_criteria, ocr_settings, template_validation, usage_stats
                FROM import_templates 
                {whereClause}
                ORDER BY name";

            using var command = new SqliteCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var templates = new List<ImportTemplate>();
            while (await reader.ReadAsync(cancellationToken))
            {
                var template = await BuildTemplateFromReaderAsync(reader, cancellationToken);
                templates.Add(template);
            }

            // Load fields for all templates
            foreach (var template in templates)
            {
                template.Fields = await GetTemplateFieldsAsync(connection, template.Id, cancellationToken);
            }

            return templates;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all templates");
            throw;
        }
    }

    #endregion

    #region Template Search and Filtering

    public async Task<IEnumerable<ImportTemplate>> SearchTemplatesAsync(TemplateSearchCriteria searchCriteria, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await _databaseService.CreateAndOpenConnectionAsync();
            
            var sqlBuilder = new List<string>
            {
                @"SELECT id, name, description, version, category, created_by, last_modified_by,
                         created_at, last_modified_at, is_active, tags, supported_formats,
                         confidence_threshold, auto_apply, allow_partial_matches, template_priority,
                         document_matching_criteria, ocr_settings, template_validation, usage_stats
                  FROM import_templates WHERE 1=1"
            };
            
            var parameters = new List<SqliteParameter>();

            // Add search filters
            AddSearchFilters(sqlBuilder, parameters, searchCriteria);

            // Add sorting
            var sortField = GetSortField(searchCriteria.SortBy);
            var sortDirection = searchCriteria.SortDirection == SortDirection.Descending ? "DESC" : "ASC";
            sqlBuilder.Add($"ORDER BY {sortField} {sortDirection}");

            // Add limit
            if (searchCriteria.MaxResults.HasValue)
            {
                sqlBuilder.Add($"LIMIT {searchCriteria.MaxResults.Value}");
            }

            var sql = string.Join(" ", sqlBuilder);
            using var command = new SqliteCommand(sql, connection);
            foreach (var parameter in parameters)
            {
                command.Parameters.Add(parameter);
            }

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var templates = new List<ImportTemplate>();
            
            while (await reader.ReadAsync(cancellationToken))
            {
                var template = await BuildTemplateFromReaderAsync(reader, cancellationToken);
                templates.Add(template);
            }

            // Load fields for matching templates
            foreach (var template in templates)
            {
                template.Fields = await GetTemplateFieldsAsync(connection, template.Id, cancellationToken);
            }

            return templates;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search templates");
            throw;
        }
    }

    public async Task<IEnumerable<ImportTemplate>> GetTemplatesByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        var searchCriteria = new TemplateSearchCriteria
        {
            Category = category,
            IncludeInactive = false
        };
        return await SearchTemplatesAsync(searchCriteria, cancellationToken);
    }

    public async Task<IEnumerable<ImportTemplate>> GetTemplatesByFormatAsync(string format, CancellationToken cancellationToken = default)
    {
        var searchCriteria = new TemplateSearchCriteria
        {
            SupportedFormats = new List<string> { format },
            IncludeInactive = false
        };
        return await SearchTemplatesAsync(searchCriteria, cancellationToken);
    }

    public async Task<IEnumerable<ImportTemplate>> GetTemplatesByTagsAsync(IEnumerable<string> tags, bool matchAll = false, CancellationToken cancellationToken = default)
    {
        var searchCriteria = new TemplateSearchCriteria
        {
            Tags = tags.ToList(),
            IncludeInactive = false
        };
        return await SearchTemplatesAsync(searchCriteria, cancellationToken);
    }

    #endregion

    #region Template Categorization and Organization

    public async Task<IEnumerable<string>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await _databaseService.CreateAndOpenConnectionAsync();
            
            const string sql = @"
                SELECT DISTINCT category 
                FROM import_templates 
                WHERE is_active = 1 AND category IS NOT NULL
                ORDER BY category";

            using var command = new SqliteCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var categories = new List<string>();
            while (await reader.ReadAsync(cancellationToken))
            {
                categories.Add(reader.GetString("category"));
            }

            return categories;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get categories");
            throw;
        }
    }

    public async Task<IEnumerable<string>> GetTagsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await _databaseService.CreateAndOpenConnectionAsync();
            
            const string sql = @"
                SELECT tags 
                FROM import_templates 
                WHERE is_active = 1 AND tags IS NOT NULL AND tags != ''";

            using var command = new SqliteCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var allTags = new HashSet<string>();
            while (await reader.ReadAsync(cancellationToken))
            {
                var tagsJson = reader.GetString("tags");
                if (!string.IsNullOrEmpty(tagsJson))
                {
                    try
                    {
                        var tags = JsonSerializer.Deserialize<List<string>>(tagsJson);
                        if (tags != null)
                        {
                            foreach (var tag in tags)
                            {
                                allTags.Add(tag);
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Failed to deserialize tags: {TagsJson}", tagsJson);
                    }
                }
            }

            return allTags.OrderBy(t => t);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get tags");
            throw;
        }
    }

    public async Task<IDictionary<string, int>> GetCategoryStatisticsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await _databaseService.CreateAndOpenConnectionAsync();
            
            const string sql = @"
                SELECT category, COUNT(*) as count
                FROM import_templates 
                WHERE is_active = 1
                GROUP BY category
                ORDER BY category";

            using var command = new SqliteCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var statistics = new Dictionary<string, int>();
            while (await reader.ReadAsync(cancellationToken))
            {
                var category = reader.GetString("category");
                var count = reader.GetInt32("count");
                statistics[category] = count;
            }

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get category statistics");
            throw;
        }
    }

    #endregion

    #region Private Helper Methods

    private void AddTemplateParameters(SqliteCommand command, ImportTemplate template)
    {
        command.Parameters.AddWithValue("@id", template.Id.ToString());
        command.Parameters.AddWithValue("@name", template.Name);
        command.Parameters.AddWithValue("@description", template.Description ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@version", template.Version);
        command.Parameters.AddWithValue("@category", template.Category);
        command.Parameters.AddWithValue("@created_by", template.CreatedBy ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@last_modified_by", template.LastModifiedBy ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@created_at", template.CreatedAt);
        command.Parameters.AddWithValue("@last_modified_at", template.LastModifiedAt);
        command.Parameters.AddWithValue("@is_active", template.IsActive);
        command.Parameters.AddWithValue("@tags", JsonSerializer.Serialize(template.Tags));
        command.Parameters.AddWithValue("@supported_formats", JsonSerializer.Serialize(template.SupportedFormats));
        command.Parameters.AddWithValue("@confidence_threshold", template.MatchingCriteria.MinimumConfidence);
        command.Parameters.AddWithValue("@auto_apply", template.MatchingCriteria.AutoApply);
        command.Parameters.AddWithValue("@allow_partial_matches", true);
        command.Parameters.AddWithValue("@template_priority", 0);
        command.Parameters.AddWithValue("@document_matching_criteria", JsonSerializer.Serialize(template.MatchingCriteria));
        command.Parameters.AddWithValue("@ocr_settings", JsonSerializer.Serialize(template.OcrSettings));
        command.Parameters.AddWithValue("@template_validation", JsonSerializer.Serialize(template.Validation));
        command.Parameters.AddWithValue("@usage_stats", JsonSerializer.Serialize(template.UsageStats));
    }

    private async Task<ImportTemplate> BuildTemplateFromReaderAsync(SqliteDataReader reader, CancellationToken cancellationToken)
    {
        var template = new ImportTemplate
        {
            Id = Guid.Parse(reader.GetString("id")),
            Name = reader.GetString("name"),
            Description = reader.IsDBNull("description") ? null : reader.GetString("description"),
            Version = reader.GetString("version"),
            Category = reader.GetString("category"),
            CreatedBy = reader.IsDBNull("created_by") ? null : reader.GetString("created_by"),
            LastModifiedBy = reader.IsDBNull("last_modified_by") ? null : reader.GetString("last_modified_by"),
            CreatedAt = reader.GetDateTime("created_at"),
            LastModifiedAt = reader.GetDateTime("last_modified_at"),
            IsActive = reader.GetBoolean("is_active")
        };

        // Read additional fields
        template.ConfidenceThreshold = reader.GetDouble("confidence_threshold");
        template.AutoApply = reader.GetBoolean("auto_apply");
        template.AllowPartialMatches = reader.GetBoolean("allow_partial_matches");
        template.Priority = reader.GetInt32("template_priority");

        // Deserialize JSON fields
        var tagsJson = reader.IsDBNull("tags") ? null : reader.GetString("tags");
        template.Tags = !string.IsNullOrEmpty(tagsJson) ? JsonSerializer.Deserialize<List<string>>(tagsJson) ?? new List<string>() : new List<string>();
        
        var formatsJson = reader.IsDBNull("supported_formats") ? null : reader.GetString("supported_formats");
        template.SupportedFormats = !string.IsNullOrEmpty(formatsJson) ? JsonSerializer.Deserialize<List<string>>(formatsJson) ?? new List<string>() : new List<string>();
        
        var criteriaJson = reader.IsDBNull("document_matching_criteria") ? null : reader.GetString("document_matching_criteria");
        template.MatchingCriteria = !string.IsNullOrEmpty(criteriaJson) ? JsonSerializer.Deserialize<DocumentMatchingCriteria>(criteriaJson) ?? new DocumentMatchingCriteria() : new DocumentMatchingCriteria();
        
        var ocrJson = reader.IsDBNull("ocr_settings") ? null : reader.GetString("ocr_settings");
        template.OcrSettings = !string.IsNullOrEmpty(ocrJson) ? JsonSerializer.Deserialize<OcrSettings>(ocrJson) ?? new OcrSettings() : new OcrSettings();
        
        var validationJson = reader.IsDBNull("template_validation") ? null : reader.GetString("template_validation");
        template.Validation = !string.IsNullOrEmpty(validationJson) ? JsonSerializer.Deserialize<TemplateValidation>(validationJson) ?? new TemplateValidation() : new TemplateValidation();
        
        var statsJson = reader.IsDBNull("usage_stats") ? null : reader.GetString("usage_stats");
        template.UsageStats = !string.IsNullOrEmpty(statsJson) ? JsonSerializer.Deserialize<TemplateUsageStats>(statsJson) ?? new TemplateUsageStats() : new TemplateUsageStats();

        return template;
    }

    private async Task<List<TemplateField>> GetTemplateFieldsAsync(SqliteConnection connection, Guid templateId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT id, template_id, name, display_name, description, field_type, extraction_method,
                   is_required, processing_order, text_patterns, keywords, default_value, output_format,
                   validation_rules, data_transformation, fallback_options, supports_multiple_values,
                   value_separator, confidence_threshold, created_at, updated_at
            FROM template_fields 
            WHERE template_id = @template_id
            ORDER BY processing_order, name";

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@template_id", templateId.ToString());

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var fields = new List<TemplateField>();

        while (await reader.ReadAsync(cancellationToken))
        {
            var field = BuildFieldFromReader(reader);
            
            // Load extraction zones for this field
            field.ExtractionZones = await GetFieldExtractionZonesAsync(connection, field.Id, cancellationToken);
            
            fields.Add(field);
        }

        return fields;
    }

    private TemplateField BuildFieldFromReader(SqliteDataReader reader)
    {
        var field = new TemplateField
        {
            Id = Guid.Parse(reader.GetString("id")),
            Name = reader.GetString("name"),
            DisplayName = reader.GetString("display_name"),
            Description = reader.IsDBNull("description") ? null : reader.GetString("description"),
            FieldType = Enum.Parse<TemplateFieldType>(reader.GetString("field_type")),
            ExtractionMethod = Enum.Parse<ExtractionMethod>(reader.GetString("extraction_method")),
            IsRequired = reader.GetBoolean("is_required"),
            ProcessingOrder = reader.GetInt32("processing_order"),
            DefaultValue = reader.IsDBNull("default_value") ? null : reader.GetString("default_value"),
            OutputFormat = reader.IsDBNull("output_format") ? null : reader.GetString("output_format"),
            SupportsMultipleValues = reader.GetBoolean("supports_multiple_values"),
            ValueSeparator = reader.IsDBNull("value_separator") ? null : reader.GetString("value_separator"),
            ConfidenceThreshold = reader.GetDouble("confidence_threshold")
        };

        // Deserialize JSON fields
        var textPatternsJson = reader.IsDBNull("text_patterns") ? null : reader.GetString("text_patterns");
        if (!string.IsNullOrEmpty(textPatternsJson))
        {
            field.TextPatterns = JsonSerializer.Deserialize<List<string>>(textPatternsJson) ?? new List<string>();
        }

        var keywordsJson = reader.IsDBNull("keywords") ? null : reader.GetString("keywords");
        if (!string.IsNullOrEmpty(keywordsJson))
        {
            field.Keywords = JsonSerializer.Deserialize<List<string>>(keywordsJson) ?? new List<string>();
        }

        var validationRulesJson = reader.IsDBNull("validation_rules") ? null : reader.GetString("validation_rules");
        if (!string.IsNullOrEmpty(validationRulesJson))
        {
            field.ValidationRules = JsonSerializer.Deserialize<FieldValidationRules>(validationRulesJson) ?? new FieldValidationRules();
        }

        var dataTransformationJson = reader.IsDBNull("data_transformation") ? null : reader.GetString("data_transformation");
        if (!string.IsNullOrEmpty(dataTransformationJson))
        {
            field.Transformation = JsonSerializer.Deserialize<DataTransformation>(dataTransformationJson) ?? new DataTransformation();
        }

        var fallbackOptionsJson = reader.IsDBNull("fallback_options") ? null : reader.GetString("fallback_options");
        if (!string.IsNullOrEmpty(fallbackOptionsJson))
        {
            field.Fallback = JsonSerializer.Deserialize<FallbackOptions>(fallbackOptionsJson) ?? new FallbackOptions();
        }

        return field;
    }

    private async Task<List<ExtractionZone>> GetFieldExtractionZonesAsync(SqliteConnection connection, Guid fieldId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT id, field_id, name, description, x, y, width, height, page_number,
                   coordinate_system, zone_type, priority, is_active, position_tolerance,
                   size_tolerance, visual_selection_settings, zone_extraction_settings,
                   created_at, updated_at
            FROM extraction_zones 
            WHERE field_id = @field_id AND is_active = 1
            ORDER BY priority, name";

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@field_id", fieldId.ToString());

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var zones = new List<ExtractionZone>();

        while (await reader.ReadAsync(cancellationToken))
        {
            var zone = new ExtractionZone
            {
                Id = Guid.Parse(reader.GetString("id")),
                Name = reader.GetString("name"),
                Description = reader.IsDBNull("description") ? null : reader.GetString("description"),
                X = reader.GetDouble("x"),
                Y = reader.GetDouble("y"),
                Width = reader.GetDouble("width"),
                Height = reader.GetDouble("height"),
                PageNumber = reader.GetInt32("page_number"),
                CoordinateSystem = Enum.Parse<CoordinateSystem>(reader.GetString("coordinate_system")),
                ZoneType = Enum.Parse<ExtractionZoneType>(reader.GetString("zone_type")),
                Priority = reader.GetInt32("priority"),
                IsActive = reader.GetBoolean("is_active"),
                PositionTolerance = reader.GetDouble("position_tolerance"),
                SizeTolerance = reader.GetDouble("size_tolerance")
            };

            zones.Add(zone);
        }

        return zones;
    }

    private async Task InsertTemplateFieldsAsync(SqliteConnection connection, SqliteTransaction transaction, Guid templateId, List<TemplateField> fields, CancellationToken cancellationToken)
    {
        const string sql = @"
            INSERT INTO template_fields (
                id, template_id, name, display_name, description, field_type, extraction_method,
                is_required, processing_order, text_patterns, keywords, default_value, output_format,
                validation_rules, data_transformation, fallback_options, supports_multiple_values,
                value_separator, confidence_threshold, created_at, updated_at
            ) VALUES (
                @id, @template_id, @name, @display_name, @description, @field_type, @extraction_method,
                @is_required, @processing_order, @text_patterns, @keywords, @default_value, @output_format,
                @validation_rules, @data_transformation, @fallback_options, @supports_multiple_values,
                @value_separator, @confidence_threshold, @created_at, @updated_at
            )";

        foreach (var field in fields)
        {
            using var command = new SqliteCommand(sql, connection, transaction);
            AddFieldParameters(command, templateId, field);
            await command.ExecuteNonQueryAsync(cancellationToken);

            // Insert extraction zones for this field
            await InsertExtractionZonesAsync(connection, transaction, field.Id, field.ExtractionZones, cancellationToken);
        }
    }

    private void AddFieldParameters(SqliteCommand command, Guid templateId, TemplateField field)
    {
        command.Parameters.AddWithValue("@id", field.Id.ToString());
        command.Parameters.AddWithValue("@template_id", templateId.ToString());
        command.Parameters.AddWithValue("@name", field.Name);
        command.Parameters.AddWithValue("@display_name", field.DisplayName);
        command.Parameters.AddWithValue("@description", field.Description ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@field_type", field.FieldType.ToString());
        command.Parameters.AddWithValue("@extraction_method", field.ExtractionMethod.ToString());
        command.Parameters.AddWithValue("@is_required", field.IsRequired);
        command.Parameters.AddWithValue("@processing_order", field.ProcessingOrder);
        command.Parameters.AddWithValue("@text_patterns", JsonSerializer.Serialize(field.TextPatterns));
        command.Parameters.AddWithValue("@keywords", JsonSerializer.Serialize(field.Keywords));
        command.Parameters.AddWithValue("@default_value", field.DefaultValue ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@output_format", field.OutputFormat ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@validation_rules", JsonSerializer.Serialize(field.ValidationRules));
        command.Parameters.AddWithValue("@data_transformation", JsonSerializer.Serialize(field.Transformation));
        command.Parameters.AddWithValue("@fallback_options", JsonSerializer.Serialize(field.Fallback));
        command.Parameters.AddWithValue("@supports_multiple_values", field.SupportsMultipleValues);
        command.Parameters.AddWithValue("@value_separator", field.ValueSeparator ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@confidence_threshold", field.ConfidenceThreshold);
        command.Parameters.AddWithValue("@created_at", DateTime.UtcNow);
        command.Parameters.AddWithValue("@updated_at", DateTime.UtcNow);
    }

    private async Task InsertExtractionZonesAsync(SqliteConnection connection, SqliteTransaction transaction, Guid fieldId, List<ExtractionZone> zones, CancellationToken cancellationToken)
    {
        const string sql = @"
            INSERT INTO extraction_zones (
                id, field_id, name, description, x, y, width, height, page_number,
                coordinate_system, zone_type, priority, is_active, position_tolerance,
                size_tolerance, visual_selection_settings, zone_extraction_settings,
                created_at, updated_at
            ) VALUES (
                @id, @field_id, @name, @description, @x, @y, @width, @height, @page_number,
                @coordinate_system, @zone_type, @priority, @is_active, @position_tolerance,
                @size_tolerance, @visual_selection_settings, @zone_extraction_settings,
                @created_at, @updated_at
            )";

        foreach (var zone in zones)
        {
            using var command = new SqliteCommand(sql, connection, transaction);
            AddZoneParameters(command, fieldId, zone);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private void AddZoneParameters(SqliteCommand command, Guid fieldId, ExtractionZone zone)
    {
        command.Parameters.AddWithValue("@id", zone.Id.ToString());
        command.Parameters.AddWithValue("@field_id", fieldId.ToString());
        command.Parameters.AddWithValue("@name", zone.Name);
        command.Parameters.AddWithValue("@description", zone.Description ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@x", zone.X);
        command.Parameters.AddWithValue("@y", zone.Y);
        command.Parameters.AddWithValue("@width", zone.Width);
        command.Parameters.AddWithValue("@height", zone.Height);
        command.Parameters.AddWithValue("@page_number", zone.PageNumber);
        command.Parameters.AddWithValue("@coordinate_system", zone.CoordinateSystem.ToString());
        command.Parameters.AddWithValue("@zone_type", zone.ZoneType.ToString());
        command.Parameters.AddWithValue("@priority", zone.Priority);
        command.Parameters.AddWithValue("@is_active", zone.IsActive);
        command.Parameters.AddWithValue("@position_tolerance", zone.PositionTolerance);
        command.Parameters.AddWithValue("@size_tolerance", zone.SizeTolerance);
        command.Parameters.AddWithValue("@visual_selection_settings", "{}");
        command.Parameters.AddWithValue("@zone_extraction_settings", "{}");
        command.Parameters.AddWithValue("@created_at", DateTime.UtcNow);
        command.Parameters.AddWithValue("@updated_at", DateTime.UtcNow);
    }

    private async Task DeleteTemplateFieldsAsync(SqliteConnection connection, SqliteTransaction transaction, Guid templateId, CancellationToken cancellationToken)
    {
        // Delete extraction zones first (foreign key constraint)
        const string deleteZonesSql = @"
            DELETE FROM extraction_zones 
            WHERE field_id IN (SELECT id FROM template_fields WHERE template_id = @template_id)";

        using var deleteZonesCommand = new SqliteCommand(deleteZonesSql, connection, transaction);
        deleteZonesCommand.Parameters.AddWithValue("@template_id", templateId.ToString());
        await deleteZonesCommand.ExecuteNonQueryAsync(cancellationToken);

        // Delete template fields
        const string deleteFieldsSql = "DELETE FROM template_fields WHERE template_id = @template_id";
        using var deleteFieldsCommand = new SqliteCommand(deleteFieldsSql, connection, transaction);
        deleteFieldsCommand.Parameters.AddWithValue("@template_id", templateId.ToString());
        await deleteFieldsCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task CreateTemplateVersionRecordAsync(SqliteConnection connection, SqliteTransaction transaction, ImportTemplate template, CancellationToken cancellationToken)
    {
        const string sql = @"
            INSERT OR REPLACE INTO template_versions (
                id, template_id, version_number, version_description, template_data,
                created_by, created_at, is_current
            ) VALUES (
                @id, @template_id, @version_number, @version_description, @template_data,
                @created_by, @created_at, @is_current
            )";

        using var command = new SqliteCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@id", Guid.NewGuid().ToString());
        command.Parameters.AddWithValue("@template_id", template.Id.ToString());
        command.Parameters.AddWithValue("@version_number", template.Version);
        command.Parameters.AddWithValue("@version_description", $"Version {template.Version}");
        command.Parameters.AddWithValue("@template_data", JsonSerializer.Serialize(template));
        command.Parameters.AddWithValue("@created_by", template.LastModifiedBy ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@created_at", DateTime.UtcNow);
        command.Parameters.AddWithValue("@is_current", true);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private void AddSearchFilters(List<string> sqlBuilder, List<SqliteParameter> parameters, TemplateSearchCriteria criteria)
    {
        if (!string.IsNullOrEmpty(criteria.SearchTerm))
        {
            sqlBuilder.Add("AND (name LIKE @search_term OR description LIKE @search_term)");
            parameters.Add(new SqliteParameter("@search_term", $"%{criteria.SearchTerm}%"));
        }

        if (!string.IsNullOrEmpty(criteria.Category))
        {
            sqlBuilder.Add("AND category = @category");
            parameters.Add(new SqliteParameter("@category", criteria.Category));
        }

        if (!string.IsNullOrEmpty(criteria.CreatedBy))
        {
            sqlBuilder.Add("AND created_by = @created_by");
            parameters.Add(new SqliteParameter("@created_by", criteria.CreatedBy));
        }

        if (criteria.CreatedAfter.HasValue)
        {
            sqlBuilder.Add("AND created_at >= @created_after");
            parameters.Add(new SqliteParameter("@created_after", criteria.CreatedAfter.Value));
        }

        if (criteria.CreatedBefore.HasValue)
        {
            sqlBuilder.Add("AND created_at <= @created_before");
            parameters.Add(new SqliteParameter("@created_before", criteria.CreatedBefore.Value));
        }

        if (!criteria.IncludeInactive)
        {
            sqlBuilder.Add("AND is_active = 1");
        }

        // Tag filtering would require more complex JSON queries in SQLite
        // This is a simplified version - could be enhanced with JSON1 extension
        if (criteria.Tags.Any())
        {
            sqlBuilder.Add("AND tags IS NOT NULL");
        }

        // Format filtering - similar complexity as tags
        if (criteria.SupportedFormats.Any())
        {
            sqlBuilder.Add("AND supported_formats IS NOT NULL");
        }
    }

    private string GetSortField(TemplateSortField sortField)
    {
        return sortField switch
        {
            TemplateSortField.Name => "name",
            TemplateSortField.Category => "category",
            TemplateSortField.CreatedAt => "created_at",
            TemplateSortField.LastModifiedAt => "last_modified_at",
            TemplateSortField.Version => "version",
            TemplateSortField.UsageCount => "created_at", // Simplified - would need usage stats
            TemplateSortField.SuccessRate => "created_at", // Simplified - would need usage stats
            _ => "name"
        };
    }

    #endregion

    #region Template Versioning Operations

    public async Task<ImportTemplate> CreateTemplateVersionAsync(Guid templateId, string newVersion, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating new version {Version} for template {TemplateId}", newVersion, templateId);

            var existingTemplate = await GetTemplateAsync(templateId, cancellationToken);
            if (existingTemplate == null)
            {
                throw new InvalidOperationException($"Template with ID {templateId} not found");
            }

            // Check if version already exists
            var existingVersion = await GetTemplateVersionAsync(templateId, newVersion, cancellationToken);
            if (existingVersion != null)
            {
                throw new InvalidOperationException($"Version {newVersion} already exists for template {templateId}");
            }

            // Create new version of the template
            var newVersionTemplate = existingTemplate.CreateVersion(newVersion);
            newVersionTemplate.LastModifiedAt = DateTime.UtcNow;

            using var connection = await _databaseService.CreateAndOpenConnectionAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Create version history record with previous state
                await CreateVersionHistoryRecordAsync(connection, transaction, templateId, existingTemplate, 
                    $"Backup before creating version {newVersion}", cancellationToken);

                // Update the template with new version
                existingTemplate.Version = newVersion;
                existingTemplate.LastModifiedAt = DateTime.UtcNow;
                await UpdateTemplateInternalAsync(connection, transaction, existingTemplate, cancellationToken);

                // Create version history record for new version
                await CreateVersionHistoryRecordAsync(connection, transaction, templateId, existingTemplate, 
                    $"Created version {newVersion}", cancellationToken);

                transaction.Commit();
                
                _logger.LogInformation("Template version {Version} created successfully for template {TemplateId}", newVersion, templateId);
                return existingTemplate;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create template version {Version} for template {TemplateId}", newVersion, templateId);
            throw;
        }
    }

    public async Task<IEnumerable<ImportTemplate>> GetTemplateVersionsAsync(string templateName, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await _databaseService.CreateAndOpenConnectionAsync();
            
            const string sql = @"
                SELECT tv.version_number, tv.created_at, tv.created_by, tv.version_description,
                       tv.template_data
                FROM template_versions tv
                INNER JOIN import_templates it ON tv.template_id = it.id
                WHERE it.name = @templateName
                ORDER BY tv.created_at DESC";

            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@templateName", templateName);

            var versions = new List<ImportTemplate>();
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            
            while (await reader.ReadAsync(cancellationToken))
            {
                var templateData = reader.GetString("template_data");
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var template = JsonSerializer.Deserialize<ImportTemplate>(templateData, options);
                if (template != null)
                {
                    versions.Add(template);
                }
            }

            return versions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get template versions for: {TemplateName}", templateName);
            throw;
        }
    }

    public async Task<ImportTemplate?> GetLatestTemplateVersionAsync(string templateName, CancellationToken cancellationToken = default)
    {
        return await GetTemplateByNameAsync(templateName, cancellationToken);
    }

    public async Task<ImportTemplate?> GetTemplateVersionAsync(Guid templateId, string version, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await _databaseService.CreateAndOpenConnectionAsync();
            
            const string sql = @"
                SELECT tv.template_data
                FROM template_versions tv
                WHERE tv.template_id = @templateId AND tv.version_number = @version";

            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@templateId", templateId.ToString());
            command.Parameters.AddWithValue("@version", version);

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                var templateData = reader.GetString("template_data");
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<ImportTemplate>(templateData, options);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get template version {Version} for template {TemplateId}", version, templateId);
            throw;
        }
    }

    public async Task<ImportTemplate> RollbackToVersionAsync(Guid templateId, string targetVersion, string rollbackReason = "", CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Rolling back template {TemplateId} to version {Version}", templateId, targetVersion);

            var targetVersionTemplate = await GetTemplateVersionAsync(templateId, targetVersion, cancellationToken);
            if (targetVersionTemplate == null)
            {
                throw new InvalidOperationException($"Target version {targetVersion} not found for template {templateId}");
            }

            var currentTemplate = await GetTemplateAsync(templateId, cancellationToken);
            if (currentTemplate == null)
            {
                throw new InvalidOperationException($"Template {templateId} not found");
            }

            using var connection = await _databaseService.CreateAndOpenConnectionAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Create backup of current version before rollback
                await CreateVersionHistoryRecordAsync(connection, transaction, templateId, currentTemplate, 
                    $"Backup before rollback to {targetVersion}", cancellationToken);

                // Update current template with target version data
                targetVersionTemplate.Id = templateId;
                targetVersionTemplate.LastModifiedAt = DateTime.UtcNow;
                targetVersionTemplate.Version = IncrementVersion(currentTemplate.Version);

                await UpdateTemplateInternalAsync(connection, transaction, targetVersionTemplate, cancellationToken);

                // Record the rollback operation
                await CreateVersionHistoryRecordAsync(connection, transaction, templateId, targetVersionTemplate, 
                    $"Rollback to version {targetVersion}: {rollbackReason}", cancellationToken);

                transaction.Commit();
                
                _logger.LogInformation("Template {TemplateId} successfully rolled back to version {Version}", templateId, targetVersion);
                return targetVersionTemplate;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rollback template {TemplateId} to version {Version}", templateId, targetVersion);
            throw;
        }
    }

    public async Task<IEnumerable<TemplateChangeRecord>> GetTemplateChangeHistoryAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await _databaseService.CreateAndOpenConnectionAsync();
            
            const string sql = @"
                SELECT id, template_id, version_number, version_description, created_by, created_at, is_current
                FROM template_versions
                WHERE template_id = @templateId
                ORDER BY created_at DESC";

            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@templateId", templateId.ToString());

            var changeHistory = new List<TemplateChangeRecord>();
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            
            while (await reader.ReadAsync(cancellationToken))
            {
                var changeRecord = new TemplateChangeRecord
                {
                    Id = Guid.Parse(reader.GetString("id")),
                    TemplateId = templateId,
                    Version = reader.GetString("version_number"),
                    ChangeDescription = reader.IsDBNull("version_description") ? "" : reader.GetString("version_description"),
                    ChangedBy = reader.IsDBNull("created_by") ? "" : reader.GetString("created_by"),
                    ChangeDate = reader.GetDateTime("created_at"),
                    IsCurrent = reader.GetBoolean("is_current")
                };
                changeHistory.Add(changeRecord);
            }

            return changeHistory;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get change history for template {TemplateId}", templateId);
            throw;
        }
    }

    public async Task<TemplateComparisonResult> CompareTemplateVersionsAsync(Guid templateId, string version1, string version2, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Comparing template versions {Version1} and {Version2} for template {TemplateId}", version1, version2, templateId);

            var template1 = await GetTemplateVersionAsync(templateId, version1, cancellationToken);
            var template2 = await GetTemplateVersionAsync(templateId, version2, cancellationToken);

            if (template1 == null || template2 == null)
            {
                throw new InvalidOperationException($"One or both template versions not found");
            }

            var comparison = new TemplateComparisonResult
            {
                TemplateId = templateId,
                Version1 = version1,
                Version2 = version2,
                ComparisonDate = DateTime.UtcNow
            };

            // Compare basic properties
            CompareBasicProperties(template1, template2, comparison);

            // Compare fields
            CompareTemplateFields(template1.Fields, template2.Fields, comparison);

            // Compare extraction zones
            CompareExtractionZones(template1, template2, comparison);

            _logger.LogDebug("Template version comparison completed for {TemplateId}", templateId);
            return comparison;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compare template versions for template {TemplateId}", templateId);
            throw;
        }
    }

    #endregion

    #region Template Import/Export - Placeholder

    public async Task<string> ExportTemplateAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Exporting template: {TemplateId}", templateId);

            var template = await GetTemplateAsync(templateId, cancellationToken);
            if (template == null)
            {
                throw new ArgumentException($"Template with ID {templateId} not found");
            }

            // Create export wrapper with metadata
            var exportData = new TemplateExportData
            {
                ExportVersion = "1.0",
                ExportedAt = DateTime.UtcNow,
                ExportedBy = Environment.UserName,
                Templates = new List<ImportTemplate> { template }
            };

            var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            _logger.LogInformation("Template exported successfully: {TemplateId}", templateId);
            return json;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export template: {TemplateId}", templateId);
            throw;
        }
    }

    public async Task<string> ExportTemplatesAsync(IEnumerable<Guid> templateIds, CancellationToken cancellationToken = default)
    {
        try
        {
            var templateIdsList = templateIds.ToList();
            _logger.LogInformation("Exporting {Count} templates", templateIdsList.Count);

            var templates = new List<ImportTemplate>();
            var notFoundIds = new List<Guid>();

            foreach (var templateId in templateIdsList)
            {
                var template = await GetTemplateAsync(templateId, cancellationToken);
                if (template != null)
                {
                    templates.Add(template);
                }
                else
                {
                    notFoundIds.Add(templateId);
                    _logger.LogWarning("Template not found: {TemplateId}", templateId);
                }
            }

            if (templates.Count == 0)
            {
                throw new ArgumentException("No templates found to export");
            }

            // Create export wrapper with metadata
            var exportData = new TemplateExportData
            {
                ExportVersion = "1.0",
                ExportedAt = DateTime.UtcNow,
                ExportedBy = Environment.UserName,
                Templates = templates,
                ExportMetadata = new Dictionary<string, object>
                {
                    { "RequestedCount", templateIdsList.Count },
                    { "ExportedCount", templates.Count },
                    { "NotFoundIds", notFoundIds }
                }
            };

            var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            _logger.LogInformation("Templates exported successfully: {ExportedCount}/{RequestedCount}", 
                templates.Count, templateIdsList.Count);
            return json;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export templates");
            throw;
        }
    }

    public async Task<ImportTemplate> ImportTemplateAsync(string templateJson, Core.Interfaces.TemplateImportOptions? importOptions = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Importing single template");

            importOptions ??= new Core.Interfaces.TemplateImportOptions();

            // Try to parse as export data first, then as single template
            ImportTemplate template;
            try
            {
                var exportData = JsonSerializer.Deserialize<TemplateExportData>(templateJson, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (exportData?.Templates != null && exportData.Templates.Count > 0)
                {
                    template = exportData.Templates.First();
                    _logger.LogInformation("Parsed template from export data format");
                }
                else
                {
                    throw new InvalidOperationException("Export data contains no templates");
                }
            }
            catch (JsonException)
            {
                // Try parsing as single template
                try
                {
                    template = JsonSerializer.Deserialize<ImportTemplate>(templateJson, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                    
                    if (template == null)
                    {
                        throw new InvalidOperationException("Failed to deserialize template");
                    }
                    
                    _logger.LogInformation("Parsed template from single template format");
                }
                catch (JsonException ex)
                {
                    throw new ArgumentException("Invalid JSON format for template import", ex);
                }
            }

            // Process import options
            await ProcessImportOptionsAsync(template, importOptions, cancellationToken);

            // Validate template if required
            if (importOptions.ValidateOnImport)
            {
                var validationResult = template.ValidateTemplate();
                if (!validationResult.IsValid)
                {
                    throw new InvalidOperationException($"Template validation failed: {string.Join(", ", validationResult.Errors)}");
                }
            }

            // Check for existing template with same name
            var existingTemplate = await GetTemplateByNameAsync(template.Name, cancellationToken);
            if (existingTemplate != null)
            {
                // Use the new conflict resolution system
                return await HandleTemplateConflictAsync(template, existingTemplate, importOptions, cancellationToken);
            }

            // Create new template
            var importedTemplate = await CreateTemplateAsync(template, cancellationToken);
            
            _logger.LogInformation("Template imported successfully: {TemplateId} ({TemplateName})", 
                importedTemplate.Id, importedTemplate.Name);
            
            return importedTemplate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import template");
            throw;
        }
    }

    public async Task<IEnumerable<ImportTemplate>> ImportTemplatesAsync(string templatesJson, Core.Interfaces.TemplateImportOptions? importOptions = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Importing multiple templates");

            importOptions ??= new Core.Interfaces.TemplateImportOptions();

            // Parse export data
            TemplateExportData exportData;
            try
            {
                exportData = JsonSerializer.Deserialize<TemplateExportData>(templatesJson, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (exportData?.Templates == null || exportData.Templates.Count == 0)
                {
                    throw new InvalidOperationException("Export data contains no templates");
                }
            }
            catch (JsonException)
            {
                // Try parsing as array of templates
                try
                {
                    var templatesArray = JsonSerializer.Deserialize<List<ImportTemplate>>(templatesJson, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    if (templatesArray == null || templatesArray.Count == 0)
                    {
                        throw new InvalidOperationException("No templates found in array");
                    }

                    exportData = new TemplateExportData
                    {
                        ExportVersion = "1.0",
                        ExportedAt = DateTime.UtcNow,
                        Templates = templatesArray
                    };

                    _logger.LogInformation("Parsed templates from array format");
                }
                catch (JsonException ex)
                {
                    throw new ArgumentException("Invalid JSON format for templates import", ex);
                }
            }

            var importResults = new List<TemplateImportResult>();
            var importedTemplates = new List<ImportTemplate>();

            _logger.LogInformation("Processing {Count} templates for import", exportData.Templates.Count);

            foreach (var template in exportData.Templates)
            {
                var result = new TemplateImportResult
                {
                    OriginalName = template.Name,
                    OriginalId = template.Id
                };

                try
                {
                    // Process import options for each template
                    await ProcessImportOptionsAsync(template, importOptions, cancellationToken);

                    // Validate template if required
                    if (importOptions.ValidateOnImport)
                    {
                        var validationResult = template.ValidateTemplate();
                        if (!validationResult.IsValid)
                        {
                            result.IsSuccessful = false;
                            result.Errors.AddRange(validationResult.Errors.Select(e => $"Validation: {e}"));
                            importResults.Add(result);
                            continue;
                        }
                    }

                    // Check for existing template with same name
                    var existingTemplate = await GetTemplateByNameAsync(template.Name, cancellationToken);
                    if (existingTemplate != null)
                    {
                        try
                        {
                            var resolvedTemplate = await HandleTemplateConflictAsync(template, existingTemplate, importOptions, cancellationToken);
                            result.IsSuccessful = true;
                            result.ImportedTemplate = resolvedTemplate;
                            
                            // Determine action based on conflict resolution
                            if (importOptions.ConflictResolution == Core.Interfaces.TemplateConflictResolution.Overwrite)
                            {
                                result.Action = TemplateImportAction.Updated;
                            }
                            else if (importOptions.ConflictResolution == Core.Interfaces.TemplateConflictResolution.Merge)
                            {
                                result.Action = TemplateImportAction.Updated;
                            }
                            else if (importOptions.ConflictResolution == Core.Interfaces.TemplateConflictResolution.Rename)
                            {
                                result.Action = TemplateImportAction.Created;
                            }
                            
                            importedTemplates.Add(resolvedTemplate);
                        }
                        catch (InvalidOperationException ex) when (ex.Message.Contains("skipped"))
                        {
                            result.IsSuccessful = false;
                            result.Errors.Add(ex.Message);
                            result.Action = TemplateImportAction.Skipped;
                        }
                    }
                    else
                    {
                        // Create new template
                        var importedTemplate = await CreateTemplateAsync(template, cancellationToken);
                        result.IsSuccessful = true;
                        result.ImportedTemplate = importedTemplate;
                        result.Action = TemplateImportAction.Created;
                        importedTemplates.Add(importedTemplate);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to import template: {TemplateName}", template.Name);
                    result.IsSuccessful = false;
                    result.Errors.Add($"Import failed: {ex.Message}");
                    result.Action = TemplateImportAction.Failed;
                }

                importResults.Add(result);
            }

            var successCount = importResults.Count(r => r.IsSuccessful);
            var failureCount = importResults.Count(r => !r.IsSuccessful);

            _logger.LogInformation("Template import completed: {SuccessCount} successful, {FailureCount} failed", 
                successCount, failureCount);

            // If there were failures, log details
            if (failureCount > 0)
            {
                var failedResults = importResults.Where(r => !r.IsSuccessful);
                foreach (var failedResult in failedResults)
                {
                    _logger.LogWarning("Failed to import template '{TemplateName}': {Errors}", 
                        failedResult.OriginalName, string.Join(", ", failedResult.Errors));
                }
            }

            return importedTemplates;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import templates");
            throw;
        }
    }

    #endregion

    #region Template Validation and Testing - Placeholder

    public async Task<TemplateValidationResult> ValidateTemplateAsync(ImportTemplate template, CancellationToken cancellationToken = default)
    {
        return template.ValidateTemplate();
    }

    public async Task<TemplateTestResult> TestTemplateAsync(Guid templateId, string documentPath, CancellationToken cancellationToken = default)
    {
        var result = new TemplateTestResult();
        try
        {
            _logger.LogInformation("Testing template {TemplateId} against document {DocumentPath}", templateId, documentPath);

            if (!File.Exists(documentPath))
            {
                throw new FileNotFoundException($"Document file not found: {documentPath}");
            }

            // Load template (resolve inheritance if necessary)
            var template = await GetTemplateAsync(templateId, cancellationToken);
            if (template == null)
            {
                throw new ArgumentException($"Template with ID {templateId} not found");
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Very lightweight document processing: read file text content
            var document = new Document
            {
                FilePath = documentPath,
                FileName = Path.GetFileName(documentPath),
                FileType = Path.GetExtension(documentPath).TrimStart('.'),
                FileSizeBytes = new FileInfo(documentPath).Length,
                ExtractedText = await File.ReadAllTextAsync(documentPath, cancellationToken),
                ProcessingStartTime = DateTime.UtcNow
            };

            // Run extraction
            var extractionEngine = new TemplateExtractionEngine(NullLogger<TemplateExtractionEngine>.Instance);
            var extractionResult = await extractionEngine.ExtractFieldsAsync(document, template);

            stopwatch.Stop();

            // Build test result
            result.IsSuccessful = extractionResult.IsSuccessful;
            result.OverallConfidence = extractionResult.OverallConfidence;
            result.ExtractionTime = stopwatch.Elapsed;
            result.ExtractedFields = extractionResult.FieldResults
                .Where(kvp => kvp.Value.IsSuccessful && kvp.Value.ExtractedValue != null)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ExtractedValue ?? string.Empty);
            result.FieldConfidenceScores = extractionResult.FieldResults
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Confidence);
            result.Warnings.AddRange(extractionResult.FieldResults
                .Where(kvp => !kvp.Value.IsSuccessful && kvp.Value.IsRequired)
                .Select(kvp => $"Required field '{kvp.Key}' not extracted"));

            // Update usage statistics
            await UpdateUsageStatisticsAsync(templateId, extractionResult.IsSuccessful, stopwatch.Elapsed, cancellationToken);

            _logger.LogInformation("Template test completed. Success: {IsSuccessful}, Confidence: {Confidence:F2}",
                result.IsSuccessful, result.OverallConfidence);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test template {TemplateId}", templateId);
            result.IsSuccessful = false;
            result.Errors.Add(ex.Message);
            return result;
        }
    }

    #endregion

    #region Template Usage Statistics - Placeholder

    public async Task UpdateUsageStatisticsAsync(Guid templateId, bool successful, TimeSpan extractionTime, CancellationToken cancellationToken = default)
    {
        try
        {
            var template = await GetTemplateAsync(templateId, cancellationToken);
            if (template == null)
            {
                _logger.LogWarning("Template not found when updating usage stats: {TemplateId}", templateId);
                return;
            }

            var stats = template.UsageStats ?? new TemplateUsageStats();
            stats.TotalUses++;
            if (successful) stats.SuccessfulUses++; else stats.FailedUses++;

            // Update average extraction time
            if (stats.SuccessfulUses > 0)
            {
                var totalTime = stats.AverageExtractionTime * (stats.SuccessfulUses - 1) + extractionTime.TotalMilliseconds;
                stats.AverageExtractionTime = totalTime / stats.SuccessfulUses;
            }

            stats.SuccessRate = stats.TotalUses > 0 ? (double)stats.SuccessfulUses / stats.TotalUses : 0;
            stats.LastUsed = DateTime.Now; // Use local time per cursor rules

            template.UsageStats = stats;

            // Persist only the usage_stats column to avoid side-effects on other data
            using var connection = await _databaseService.CreateAndOpenConnectionAsync();
            const string sql = "UPDATE import_templates SET usage_stats = @usage_stats WHERE id = @id";
            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@usage_stats", JsonSerializer.Serialize(stats));
            command.Parameters.AddWithValue("@id", templateId.ToString());
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update usage statistics for template {TemplateId}", templateId);
        }
    }

    public async Task<TemplateUsageStats> GetUsageStatisticsAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        var template = await GetTemplateAsync(templateId, cancellationToken);
        return template?.UsageStats ?? new TemplateUsageStats();
    }

    public async Task<IEnumerable<ImportTemplate>> GetMostUsedTemplatesAsync(int count = 10, CancellationToken cancellationToken = default)
    {
        var templates = await GetAllTemplatesAsync(includeInactive: false, cancellationToken);
        return templates.OrderByDescending(t => t.UsageStats.TotalUses).Take(count).ToList();
    }

    #endregion

    #region Template Activation and Management - Placeholder

    public async Task<ImportTemplate> ActivateTemplateAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        var template = await GetTemplateAsync(templateId, cancellationToken);
        if (template == null)
        {
            throw new ArgumentException($"Template with ID {templateId} not found");
        }

        template.IsActive = true;
        return await UpdateTemplateAsync(template, cancellationToken);
    }

    public async Task<ImportTemplate> DeactivateTemplateAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        var template = await GetTemplateAsync(templateId, cancellationToken);
        if (template == null)
        {
            throw new ArgumentException($"Template with ID {templateId} not found");
        }

        template.IsActive = false;
        return await UpdateTemplateAsync(template, cancellationToken);
    }

    public async Task<ImportTemplate> DuplicateTemplateAsync(Guid templateId, string newName, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Template duplication will be implemented in task 2.5");
    }

    #endregion

    #region Versioning Helper Methods

    private async Task CreateVersionHistoryRecordAsync(SqliteConnection connection, SqliteTransaction transaction, 
        Guid templateId, ImportTemplate template, string description, CancellationToken cancellationToken)
    {
        const string sql = @"
            INSERT INTO template_versions (
                id, template_id, version_number, version_description, template_data,
                created_by, created_at, is_current
            ) VALUES (
                @id, @template_id, @version_number, @version_description, @template_data,
                @created_by, @created_at, @is_current
            )";

        using var command = new SqliteCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@id", Guid.NewGuid().ToString());
        command.Parameters.AddWithValue("@template_id", templateId.ToString());
        command.Parameters.AddWithValue("@version_number", template.Version);
        command.Parameters.AddWithValue("@version_description", description);
        command.Parameters.AddWithValue("@template_data", JsonSerializer.Serialize(template));
        command.Parameters.AddWithValue("@created_by", template.LastModifiedBy ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@created_at", DateTime.UtcNow);
        command.Parameters.AddWithValue("@is_current", false); // Historical versions are not current

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task UpdateTemplateInternalAsync(SqliteConnection connection, SqliteTransaction transaction, 
        ImportTemplate template, CancellationToken cancellationToken)
    {
        // Update main template record
        const string updateTemplateSql = @"
            UPDATE import_templates SET
                name = @name, description = @description, version = @version, 
                category = @category, last_modified_by = @last_modified_by,
                last_modified_at = @last_modified_at, is_active = @is_active,
                tags = @tags, supported_formats = @supported_formats,
                confidence_threshold = @confidence_threshold, auto_apply = @auto_apply,
                allow_partial_matches = @allow_partial_matches, template_priority = @template_priority,
                document_matching_criteria = @document_matching_criteria,
                ocr_settings = @ocr_settings, template_validation = @template_validation,
                usage_stats = @usage_stats
            WHERE id = @id";

        using var command = new SqliteCommand(updateTemplateSql, connection, transaction);
        AddTemplateParameters(command, template);
        await command.ExecuteNonQueryAsync(cancellationToken);

        // Delete existing fields and zones
        await DeleteTemplateFieldsAsync(connection, transaction, template.Id, cancellationToken);

        // Insert updated fields
        await InsertTemplateFieldsAsync(connection, transaction, template.Id, template.Fields, cancellationToken);
    }

    private string IncrementVersion(string currentVersion)
    {
        try
        {
            var parts = currentVersion.Split('.');
            if (parts.Length >= 3 && int.TryParse(parts[2], out var patch))
            {
                parts[2] = (patch + 1).ToString();
                return string.Join(".", parts);
            }
            
            // Fallback: append .1 if version format is unexpected
            return $"{currentVersion}.1";
        }
        catch
        {
            // Fallback for any parsing errors
            return $"{currentVersion}.1";
        }
    }

    private void CompareBasicProperties(ImportTemplate template1, ImportTemplate template2, TemplateComparisonResult comparison)
    {
        if (template1.Name != template2.Name)
            comparison.Differences.Add(new TemplateDifference { Property = "Name", Value1 = template1.Name, Value2 = template2.Name });

        if (template1.Description != template2.Description)
            comparison.Differences.Add(new TemplateDifference { Property = "Description", Value1 = template1.Description, Value2 = template2.Description });

        if (template1.Category != template2.Category)
            comparison.Differences.Add(new TemplateDifference { Property = "Category", Value1 = template1.Category, Value2 = template2.Category });

        if (template1.ConfidenceThreshold != template2.ConfidenceThreshold)
            comparison.Differences.Add(new TemplateDifference { Property = "ConfidenceThreshold", Value1 = template1.ConfidenceThreshold.ToString(), Value2 = template2.ConfidenceThreshold.ToString() });

        if (template1.AutoApply != template2.AutoApply)
            comparison.Differences.Add(new TemplateDifference { Property = "AutoApply", Value1 = template1.AutoApply.ToString(), Value2 = template2.AutoApply.ToString() });
    }

    private void CompareTemplateFields(List<TemplateField> fields1, List<TemplateField> fields2, TemplateComparisonResult comparison)
    {
        var fields1Dict = fields1.ToDictionary(f => f.Name, f => f);
        var fields2Dict = fields2.ToDictionary(f => f.Name, f => f);

        // Check for added fields
        foreach (var field in fields2Dict.Values.Where(f => !fields1Dict.ContainsKey(f.Name)))
        {
            comparison.Differences.Add(new TemplateDifference 
            { 
                Property = $"Field.{field.Name}", 
                Value1 = null, 
                Value2 = $"Added: {field.FieldType}" 
            });
        }

        // Check for removed fields
        foreach (var field in fields1Dict.Values.Where(f => !fields2Dict.ContainsKey(f.Name)))
        {
            comparison.Differences.Add(new TemplateDifference 
            { 
                Property = $"Field.{field.Name}", 
                Value1 = $"Removed: {field.FieldType}", 
                Value2 = null 
            });
        }

        // Check for modified fields
        foreach (var fieldName in fields1Dict.Keys.Intersect(fields2Dict.Keys))
        {
            var field1 = fields1Dict[fieldName];
            var field2 = fields2Dict[fieldName];

            if (field1.FieldType != field2.FieldType)
                comparison.Differences.Add(new TemplateDifference 
                { 
                    Property = $"Field.{fieldName}.Type", 
                    Value1 = field1.FieldType.ToString(), 
                    Value2 = field2.FieldType.ToString() 
                });

            if (field1.ExtractionMethod != field2.ExtractionMethod)
                comparison.Differences.Add(new TemplateDifference 
                { 
                    Property = $"Field.{fieldName}.ExtractionMethod", 
                    Value1 = field1.ExtractionMethod.ToString(), 
                    Value2 = field2.ExtractionMethod.ToString() 
                });

            if (field1.IsRequired != field2.IsRequired)
                comparison.Differences.Add(new TemplateDifference 
                { 
                    Property = $"Field.{fieldName}.IsRequired", 
                    Value1 = field1.IsRequired.ToString(), 
                    Value2 = field2.IsRequired.ToString() 
                });
        }
    }

    private void CompareExtractionZones(ImportTemplate template1, ImportTemplate template2, TemplateComparisonResult comparison)
    {
        var zones1 = template1.Fields.SelectMany(f => f.ExtractionZones).ToList();
        var zones2 = template2.Fields.SelectMany(f => f.ExtractionZones).ToList();

        if (zones1.Count != zones2.Count)
        {
            comparison.Differences.Add(new TemplateDifference 
            { 
                Property = "ExtractionZones.Count", 
                Value1 = zones1.Count.ToString(), 
                Value2 = zones2.Count.ToString() 
            });
        }

        // More detailed zone comparison could be added here
        var zoneChanges = Math.Abs(zones1.Count - zones2.Count);
        if (zoneChanges > 0)
        {
            comparison.Differences.Add(new TemplateDifference 
            { 
                Property = "ExtractionZones.Changes", 
                Value1 = zones1.Count.ToString(), 
                Value2 = zones2.Count.ToString() 
            });
        }
    }

    #endregion

    #region Template Inheritance Methods

    public async Task<TemplateInheritanceRelationship> CreateInheritanceAsync(Guid childTemplateId, Guid parentTemplateId, TemplateInheritanceConfig inheritanceConfig)
    {
        try
        {
            _logger.LogInformation("Creating inheritance relationship: Child {ChildId} -> Parent {ParentId}", childTemplateId, parentTemplateId);

            // Validate that the inheritance would not create a cycle
            if (!await ValidateInheritanceAsync(childTemplateId, parentTemplateId))
            {
                throw new InvalidOperationException("Cannot create inheritance relationship: would create a cycle");
            }

            var relationship = new TemplateInheritanceRelationship
            {
                Id = Guid.NewGuid(),
                ChildTemplateId = childTemplateId,
                ParentTemplateId = parentTemplateId,
                InheritanceConfig = inheritanceConfig,
                IsActive = true,
                ValidationStatus = InheritanceValidationStatus.Valid,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            using var connection = await _databaseService.CreateAndOpenConnectionAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                const string sql = @"
                    INSERT INTO template_inheritance (
                        id, child_template_id, parent_template_id, inheritance_type,
                        field_overrides, settings_overrides, inheritance_priority,
                        allow_field_addition, allow_field_removal, allow_field_modification,
                        allow_settings_override, is_active, validation_status, validation_message,
                        created_at, updated_at
                    ) VALUES (
                        @id, @child_template_id, @parent_template_id, @inheritance_type,
                        @field_overrides, @settings_overrides, @inheritance_priority,
                        @allow_field_addition, @allow_field_removal, @allow_field_modification,
                        @allow_settings_override, @is_active, @validation_status, @validation_message,
                        @created_at, @updated_at
                    )";

                using var command = new SqliteCommand(sql, connection, transaction);
                AddInheritanceParameters(command, relationship);
                await command.ExecuteNonQueryAsync();

                transaction.Commit();
                
                _logger.LogInformation("Inheritance relationship created successfully: {RelationshipId}", relationship.Id);
                return relationship;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create inheritance relationship: Child {ChildId} -> Parent {ParentId}", childTemplateId, parentTemplateId);
            throw;
        }
    }

    public async Task<bool> RemoveInheritanceAsync(Guid childTemplateId, Guid parentTemplateId)
    {
        try
        {
            _logger.LogInformation("Removing inheritance relationship: Child {ChildId} -> Parent {ParentId}", childTemplateId, parentTemplateId);

            using var connection = await _databaseService.CreateAndOpenConnectionAsync();
            
            const string sql = @"
                DELETE FROM template_inheritance 
                WHERE child_template_id = @child_template_id AND parent_template_id = @parent_template_id";

            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@child_template_id", childTemplateId.ToString());
            command.Parameters.AddWithValue("@parent_template_id", parentTemplateId.ToString());

            var rowsAffected = await command.ExecuteNonQueryAsync();
            var removed = rowsAffected > 0;

            if (removed)
            {
                _logger.LogInformation("Inheritance relationship removed successfully");
            }
            else
            {
                _logger.LogWarning("No inheritance relationship found to remove");
            }

            return removed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove inheritance relationship: Child {ChildId} -> Parent {ParentId}", childTemplateId, parentTemplateId);
            throw;
        }
    }

    public async Task<List<TemplateInheritanceRelationship>> GetTemplateInheritanceAsync(Guid templateId)
    {
        try
        {
            using var connection = await _databaseService.CreateAndOpenConnectionAsync();
            
            const string sql = @"
                SELECT id, child_template_id, parent_template_id, inheritance_type,
                       field_overrides, settings_overrides, inheritance_priority,
                       allow_field_addition, allow_field_removal, allow_field_modification,
                       allow_settings_override, is_active, validation_status, validation_message,
                       created_at, updated_at
                FROM template_inheritance 
                WHERE child_template_id = @template_id AND is_active = 1
                ORDER BY inheritance_priority DESC";

            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@template_id", templateId.ToString());

            var relationships = new List<TemplateInheritanceRelationship>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                relationships.Add(BuildInheritanceRelationshipFromReader(reader));
            }

            return relationships;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get template inheritance: {TemplateId}", templateId);
            throw;
        }
    }

    public async Task<List<TemplateInheritanceRelationship>> GetChildTemplatesAsync(Guid parentTemplateId)
    {
        try
        {
            using var connection = await _databaseService.CreateAndOpenConnectionAsync();
            
            const string sql = @"
                SELECT id, child_template_id, parent_template_id, inheritance_type,
                       field_overrides, settings_overrides, inheritance_priority,
                       allow_field_addition, allow_field_removal, allow_field_modification,
                       allow_settings_override, is_active, validation_status, validation_message,
                       created_at, updated_at
                FROM template_inheritance 
                WHERE parent_template_id = @parent_template_id AND is_active = 1
                ORDER BY inheritance_priority DESC";

            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@parent_template_id", parentTemplateId.ToString());

            var relationships = new List<TemplateInheritanceRelationship>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                relationships.Add(BuildInheritanceRelationshipFromReader(reader));
            }

            return relationships;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get child templates: {ParentTemplateId}", parentTemplateId);
            throw;
        }
    }

    public async Task<TemplateInheritanceResult> ResolveTemplateInheritanceAsync(Guid templateId)
    {
        try
        {
            _logger.LogInformation("Resolving template inheritance: {TemplateId}", templateId);

            var result = new TemplateInheritanceResult();
            var visitedTemplates = new HashSet<Guid>();
            var inheritanceChain = new List<Guid>();

            // Get the complete inheritance chain
            var chain = await GetInheritanceChainAsync(templateId);
            if (chain.Count == 0)
            {
                // No inheritance, return the template as-is
                var template = await GetTemplateAsync(templateId);
                if (template != null)
                {
                    template.IsInheritanceResolved = true;
                    result.IsSuccessful = true;
                    result.ResolvedTemplate = template;
                    result.InheritanceChain = new List<Guid> { templateId };
                }
                return result;
            }

            // Start with the root template and apply inheritance down the chain
            ImportTemplate? resolvedTemplate = null;
            for (int i = 0; i < chain.Count; i++)
            {
                var currentTemplateId = chain[i];
                var currentTemplate = await GetTemplateAsync(currentTemplateId);
                
                if (currentTemplate == null)
                {
                    result.Errors.Add($"Template not found: {currentTemplateId}");
                    continue;
                }

                if (resolvedTemplate == null)
                {
                    // This is the root template
                    resolvedTemplate = currentTemplate;
                    result.PropertySources = GetInitialPropertySources(resolvedTemplate);
                    result.FieldSources = GetInitialFieldSources(resolvedTemplate);
                }
                else
                {
                    // Apply inheritance from parent to child
                    var parentTemplateId = chain[i - 1];
                    var inheritanceRelationships = await GetTemplateInheritanceAsync(currentTemplateId);
                    
                    var parentRelationship = inheritanceRelationships
                        .FirstOrDefault(r => r.ParentTemplateId == parentTemplateId);
                    
                    if (parentRelationship != null)
                    {
                        resolvedTemplate = ApplyInheritance(resolvedTemplate, currentTemplate, parentRelationship.InheritanceConfig, result);
                    }
                }
            }

            if (resolvedTemplate != null)
            {
                resolvedTemplate.IsInheritanceResolved = true;
                resolvedTemplate.InheritanceTracker = result.PropertySources;
                result.IsSuccessful = true;
                result.ResolvedTemplate = resolvedTemplate;
                result.InheritanceChain = chain;
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve template inheritance: {TemplateId}", templateId);
            return new TemplateInheritanceResult
            {
                IsSuccessful = false,
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<bool> ValidateInheritanceAsync(Guid childTemplateId, Guid parentTemplateId)
    {
        try
        {
            // Check if templates exist
            var childTemplate = await GetTemplateAsync(childTemplateId);
            var parentTemplate = await GetTemplateAsync(parentTemplateId);
            
            if (childTemplate == null || parentTemplate == null)
            {
                return false;
            }

            // Check for self-inheritance
            if (childTemplateId == parentTemplateId)
            {
                return false;
            }

            // Check if this would create a cycle
            var parentChain = await GetInheritanceChainAsync(parentTemplateId);
            if (parentChain.Contains(childTemplateId))
            {
                return false; // Would create a cycle
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate inheritance: Child {ChildId} -> Parent {ParentId}", childTemplateId, parentTemplateId);
            return false;
        }
    }

    public async Task<List<Guid>> GetInheritanceChainAsync(Guid templateId)
    {
        try
        {
            var chain = new List<Guid>();
            var visitedTemplates = new HashSet<Guid>();
            var currentTemplateId = templateId;

            // Build the chain from child to root
            var reverseChain = new List<Guid>();
            while (currentTemplateId != Guid.Empty)
            {
                // Prevent infinite loops
                if (visitedTemplates.Contains(currentTemplateId))
                {
                    _logger.LogWarning("Circular inheritance detected for template: {TemplateId}", templateId);
                    break;
                }

                visitedTemplates.Add(currentTemplateId);
                reverseChain.Add(currentTemplateId);

                // Get parent template ID
                var inheritanceRelationships = await GetTemplateInheritanceAsync(currentTemplateId);
                if (inheritanceRelationships.Count == 0)
                {
                    break; // No more parents
                }

                // For simplicity, take the highest priority parent
                var highestPriorityRelationship = inheritanceRelationships
                    .OrderByDescending(r => r.InheritanceConfig.InheritancePriority)
                    .First();
                    
                currentTemplateId = highestPriorityRelationship.ParentTemplateId;
            }

            // Reverse the chain so it goes from root to current
            reverseChain.Reverse();
            return reverseChain;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get inheritance chain: {TemplateId}", templateId);
            return new List<Guid>();
        }
    }

    public async Task<TemplateInheritanceRelationship> UpdateInheritanceConfigAsync(Guid childTemplateId, Guid parentTemplateId, TemplateInheritanceConfig inheritanceConfig)
    {
        try
        {
            _logger.LogInformation("Updating inheritance configuration: Child {ChildId} -> Parent {ParentId}", childTemplateId, parentTemplateId);

            using var connection = await _databaseService.CreateAndOpenConnectionAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                const string sql = @"
                    UPDATE template_inheritance SET
                        inheritance_type = @inheritance_type,
                        field_overrides = @field_overrides,
                        settings_overrides = @settings_overrides,
                        inheritance_priority = @inheritance_priority,
                        allow_field_addition = @allow_field_addition,
                        allow_field_removal = @allow_field_removal,
                        allow_field_modification = @allow_field_modification,
                        allow_settings_override = @allow_settings_override,
                        updated_at = @updated_at
                    WHERE child_template_id = @child_template_id AND parent_template_id = @parent_template_id";

                using var command = new SqliteCommand(sql, connection, transaction);
                command.Parameters.AddWithValue("@child_template_id", childTemplateId.ToString());
                command.Parameters.AddWithValue("@parent_template_id", parentTemplateId.ToString());
                command.Parameters.AddWithValue("@inheritance_type", inheritanceConfig.InheritanceType.ToString());
                command.Parameters.AddWithValue("@field_overrides", JsonSerializer.Serialize(inheritanceConfig.FieldOverrides));
                command.Parameters.AddWithValue("@settings_overrides", JsonSerializer.Serialize(inheritanceConfig.SettingsOverrides));
                command.Parameters.AddWithValue("@inheritance_priority", inheritanceConfig.InheritancePriority);
                command.Parameters.AddWithValue("@allow_field_addition", inheritanceConfig.AllowFieldAddition);
                command.Parameters.AddWithValue("@allow_field_removal", inheritanceConfig.AllowFieldRemoval);
                command.Parameters.AddWithValue("@allow_field_modification", inheritanceConfig.AllowFieldModification);
                command.Parameters.AddWithValue("@allow_settings_override", inheritanceConfig.AllowSettingsOverride);
                command.Parameters.AddWithValue("@updated_at", DateTime.UtcNow);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                if (rowsAffected == 0)
                {
                    throw new InvalidOperationException("Inheritance relationship not found");
                }

                transaction.Commit();

                // Return the updated relationship
                var relationships = await GetTemplateInheritanceAsync(childTemplateId);
                var updatedRelationship = relationships.FirstOrDefault(r => r.ParentTemplateId == parentTemplateId);
                
                if (updatedRelationship == null)
                {
                    throw new InvalidOperationException("Failed to retrieve updated inheritance relationship");
                }

                _logger.LogInformation("Inheritance configuration updated successfully");
                return updatedRelationship;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update inheritance configuration: Child {ChildId} -> Parent {ParentId}", childTemplateId, parentTemplateId);
            throw;
        }
    }

    public async Task<List<ImportTemplate>> GetAvailableParentTemplatesAsync(Guid forTemplateId)
    {
        try
        {
            var allTemplates = await GetAllTemplatesAsync(includeInactive: false);
            var availableParents = new List<ImportTemplate>();

            foreach (var template in allTemplates)
            {
                if (template.Id == forTemplateId)
                    continue; // Can't be parent of itself

                // Check if this template would create a cycle
                if (await ValidateInheritanceAsync(forTemplateId, template.Id))
                {
                    availableParents.Add(template);
                }
            }

            return availableParents;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available parent templates for: {TemplateId}", forTemplateId);
            throw;
        }
    }

    #endregion

    #region Inheritance Helper Methods

    private void AddInheritanceParameters(SqliteCommand command, TemplateInheritanceRelationship relationship)
    {
        command.Parameters.AddWithValue("@id", relationship.Id.ToString());
        command.Parameters.AddWithValue("@child_template_id", relationship.ChildTemplateId.ToString());
        command.Parameters.AddWithValue("@parent_template_id", relationship.ParentTemplateId.ToString());
        command.Parameters.AddWithValue("@inheritance_type", relationship.InheritanceConfig.InheritanceType.ToString());
        command.Parameters.AddWithValue("@field_overrides", JsonSerializer.Serialize(relationship.InheritanceConfig.FieldOverrides));
        command.Parameters.AddWithValue("@settings_overrides", JsonSerializer.Serialize(relationship.InheritanceConfig.SettingsOverrides));
        command.Parameters.AddWithValue("@inheritance_priority", relationship.InheritanceConfig.InheritancePriority);
        command.Parameters.AddWithValue("@allow_field_addition", relationship.InheritanceConfig.AllowFieldAddition);
        command.Parameters.AddWithValue("@allow_field_removal", relationship.InheritanceConfig.AllowFieldRemoval);
        command.Parameters.AddWithValue("@allow_field_modification", relationship.InheritanceConfig.AllowFieldModification);
        command.Parameters.AddWithValue("@allow_settings_override", relationship.InheritanceConfig.AllowSettingsOverride);
        command.Parameters.AddWithValue("@is_active", relationship.IsActive);
        command.Parameters.AddWithValue("@validation_status", relationship.ValidationStatus.ToString());
        command.Parameters.AddWithValue("@validation_message", relationship.ValidationMessage ?? string.Empty);
        command.Parameters.AddWithValue("@created_at", relationship.CreatedAt);
        command.Parameters.AddWithValue("@updated_at", relationship.UpdatedAt);
    }

    private TemplateInheritanceRelationship BuildInheritanceRelationshipFromReader(SqliteDataReader reader)
    {
        var relationship = new TemplateInheritanceRelationship
        {
            Id = Guid.Parse(reader.GetString("id")),
            ChildTemplateId = Guid.Parse(reader.GetString("child_template_id")),
            ParentTemplateId = Guid.Parse(reader.GetString("parent_template_id")),
            IsActive = reader.GetBoolean("is_active"),
            ValidationStatus = Enum.Parse<InheritanceValidationStatus>(reader.GetString("validation_status")),
            ValidationMessage = reader.IsDBNull("validation_message") ? null : reader.GetString("validation_message"),
            CreatedAt = reader.GetDateTime("created_at"),
            UpdatedAt = reader.GetDateTime("updated_at")
        };

        // Deserialize inheritance configuration
        var inheritanceType = Enum.Parse<InheritanceType>(reader.GetString("inheritance_type"));
        var fieldOverridesJson = reader.GetString("field_overrides");
        var settingsOverridesJson = reader.GetString("settings_overrides");

        relationship.InheritanceConfig = new TemplateInheritanceConfig
        {
            InheritanceType = inheritanceType,
            FieldOverrides = string.IsNullOrEmpty(fieldOverridesJson) 
                ? new Dictionary<string, FieldOverrideConfig>()
                : JsonSerializer.Deserialize<Dictionary<string, FieldOverrideConfig>>(fieldOverridesJson) ?? new(),
            SettingsOverrides = string.IsNullOrEmpty(settingsOverridesJson)
                ? new Dictionary<string, object>()
                : JsonSerializer.Deserialize<Dictionary<string, object>>(settingsOverridesJson) ?? new(),
            InheritancePriority = reader.GetInt32("inheritance_priority"),
            AllowFieldAddition = reader.GetBoolean("allow_field_addition"),
            AllowFieldRemoval = reader.GetBoolean("allow_field_removal"),
            AllowFieldModification = reader.GetBoolean("allow_field_modification"),
            AllowSettingsOverride = reader.GetBoolean("allow_settings_override")
        };

        return relationship;
    }

    private Dictionary<string, InheritanceSource> GetInitialPropertySources(ImportTemplate template)
    {
        var sources = new Dictionary<string, InheritanceSource>();
        
        // Mark all current properties as current source
        sources["Name"] = InheritanceSource.Current;
        sources["Description"] = InheritanceSource.Current;
        sources["Category"] = InheritanceSource.Current;
        sources["ConfidenceThreshold"] = InheritanceSource.Current;
        sources["AutoApply"] = InheritanceSource.Current;
        sources["AllowPartialMatches"] = InheritanceSource.Current;
        sources["TemplatePriority"] = InheritanceSource.Current;
        
        return sources;
    }

    private Dictionary<string, InheritanceSource> GetInitialFieldSources(ImportTemplate template)
    {
        var sources = new Dictionary<string, InheritanceSource>();
        
        foreach (var field in template.Fields)
        {
            sources[field.Name] = InheritanceSource.Current;
        }
        
        return sources;
    }

    private ImportTemplate ApplyInheritance(ImportTemplate parentTemplate, ImportTemplate childTemplate, TemplateInheritanceConfig config, TemplateInheritanceResult result)
    {
        var resolvedTemplate = childTemplate.CreateVersion(childTemplate.Version);
        
        switch (config.InheritanceType)
        {
            case InheritanceType.Full:
                ApplyFullInheritance(parentTemplate, resolvedTemplate, config, result);
                break;
            case InheritanceType.FieldsOnly:
                ApplyFieldsOnlyInheritance(parentTemplate, resolvedTemplate, config, result);
                break;
            case InheritanceType.SettingsOnly:
                ApplySettingsOnlyInheritance(parentTemplate, resolvedTemplate, config, result);
                break;
            case InheritanceType.Custom:
                ApplyCustomInheritance(parentTemplate, resolvedTemplate, config, result);
                break;
        }
        
        return resolvedTemplate;
    }

    private void ApplyFullInheritance(ImportTemplate parentTemplate, ImportTemplate childTemplate, TemplateInheritanceConfig config, TemplateInheritanceResult result)
    {
        // Apply settings inheritance
        ApplySettingsInheritance(parentTemplate, childTemplate, config, result);
        
        // Apply fields inheritance
        ApplyFieldsInheritance(parentTemplate, childTemplate, config, result);
    }

    private void ApplyFieldsOnlyInheritance(ImportTemplate parentTemplate, ImportTemplate childTemplate, TemplateInheritanceConfig config, TemplateInheritanceResult result)
    {
        ApplyFieldsInheritance(parentTemplate, childTemplate, config, result);
    }

    private void ApplySettingsOnlyInheritance(ImportTemplate parentTemplate, ImportTemplate childTemplate, TemplateInheritanceConfig config, TemplateInheritanceResult result)
    {
        ApplySettingsInheritance(parentTemplate, childTemplate, config, result);
    }

    private void ApplyCustomInheritance(ImportTemplate parentTemplate, ImportTemplate childTemplate, TemplateInheritanceConfig config, TemplateInheritanceResult result)
    {
        // Apply custom inheritance based on configuration
        if (config.AllowSettingsOverride)
        {
            ApplySettingsInheritance(parentTemplate, childTemplate, config, result);
        }
        
        ApplyFieldsInheritance(parentTemplate, childTemplate, config, result);
    }

    private void ApplySettingsInheritance(ImportTemplate parentTemplate, ImportTemplate childTemplate, TemplateInheritanceConfig config, TemplateInheritanceResult result)
    {
        if (!config.AllowSettingsOverride) return;

        // Apply settings from parent unless explicitly overridden
        if (!config.SettingsOverrides.ContainsKey("Category") && string.IsNullOrEmpty(childTemplate.Category))
        {
            childTemplate.Category = parentTemplate.Category;
            result.PropertySources["Category"] = InheritanceSource.Inherited;
        }

        if (!config.SettingsOverrides.ContainsKey("ConfidenceThreshold") && childTemplate.ConfidenceThreshold == 0.75) // Default value
        {
            childTemplate.ConfidenceThreshold = parentTemplate.ConfidenceThreshold;
            result.PropertySources["ConfidenceThreshold"] = InheritanceSource.Inherited;
        }

        if (!config.SettingsOverrides.ContainsKey("AutoApply"))
        {
            childTemplate.AutoApply = parentTemplate.AutoApply;
            result.PropertySources["AutoApply"] = InheritanceSource.Inherited;
        }

        if (!config.SettingsOverrides.ContainsKey("AllowPartialMatches"))
        {
            childTemplate.AllowPartialMatches = parentTemplate.AllowPartialMatches;
            result.PropertySources["AllowPartialMatches"] = InheritanceSource.Inherited;
        }

        // Merge tags and supported formats
        foreach (var tag in parentTemplate.Tags)
        {
            if (!childTemplate.Tags.Contains(tag))
            {
                childTemplate.Tags.Add(tag);
            }
        }

        foreach (var format in parentTemplate.SupportedFormats)
        {
            if (!childTemplate.SupportedFormats.Contains(format))
            {
                childTemplate.SupportedFormats.Add(format);
            }
        }
    }

    private void ApplyFieldsInheritance(ImportTemplate parentTemplate, ImportTemplate childTemplate, TemplateInheritanceConfig config, TemplateInheritanceResult result)
    {
        var childFieldNames = childTemplate.Fields.Select(f => f.Name).ToHashSet();
        
        // Process parent fields
        foreach (var parentField in parentTemplate.Fields)
        {
            if (config.FieldOverrides.TryGetValue(parentField.Name, out var overrideConfig))
            {
                switch (overrideConfig.Action)
                {
                    case FieldOverrideAction.Inherit:
                        if (!childFieldNames.Contains(parentField.Name) && config.AllowFieldAddition)
                        {
                            childTemplate.Fields.Add(parentField.CreateCopy());
                            result.FieldSources[parentField.Name] = InheritanceSource.Inherited;
                        }
                        break;
                        
                    case FieldOverrideAction.Override:
                        // Field is already in child template and will be used as-is
                        if (childFieldNames.Contains(parentField.Name))
                        {
                            result.FieldSources[parentField.Name] = InheritanceSource.Current;
                        }
                        break;
                        
                    case FieldOverrideAction.Merge:
                        MergeFields(parentField, childTemplate, overrideConfig, result);
                        break;
                        
                    case FieldOverrideAction.Remove:
                        if (config.AllowFieldRemoval)
                        {
                            childTemplate.Fields.RemoveAll(f => f.Name == parentField.Name);
                        }
                        break;
                }
            }
            else
            {
                // Default behavior: inherit if not present in child
                if (!childFieldNames.Contains(parentField.Name) && config.AllowFieldAddition)
                {
                    childTemplate.Fields.Add(parentField.CreateCopy());
                    result.FieldSources[parentField.Name] = InheritanceSource.Inherited;
                }
            }
        }
    }

    private void MergeFields(TemplateField parentField, ImportTemplate childTemplate, FieldOverrideConfig overrideConfig, TemplateInheritanceResult result)
    {
        var childField = childTemplate.Fields.FirstOrDefault(f => f.Name == parentField.Name);
        if (childField == null)
        {
            // Field doesn't exist in child, add the parent field
            childTemplate.Fields.Add(parentField.CreateCopy());
            result.FieldSources[parentField.Name] = InheritanceSource.Inherited;
            return;
        }

        // Merge specified properties
        foreach (var property in overrideConfig.MergeProperties)
        {
            switch (property.ToLower())
            {
                case "textpatterns":
                case "text_patterns":
                    if (parentField.TextPatterns != null)
                    {
                        childField.TextPatterns ??= new List<string>();
                        foreach (var pattern in parentField.TextPatterns)
                        {
                            if (!childField.TextPatterns.Contains(pattern))
                            {
                                childField.TextPatterns.Add(pattern);
                            }
                        }
                    }
                    break;
                    
                case "keywords":
                    if (parentField.Keywords != null)
                    {
                        childField.Keywords ??= new List<string>();
                        foreach (var keyword in parentField.Keywords)
                        {
                            if (!childField.Keywords.Contains(keyword))
                            {
                                childField.Keywords.Add(keyword);
                            }
                        }
                    }
                    break;
                    
                case "extractionzones":
                case "extraction_zones":
                    if (parentField.ExtractionZones != null)
                    {
                        childField.ExtractionZones ??= new List<ExtractionZone>();
                        foreach (var zone in parentField.ExtractionZones)
                        {
                            if (!childField.ExtractionZones.Any(z => z.Name == zone.Name))
                            {
                                childField.ExtractionZones.Add(zone.CreateCopy());
                            }
                        }
                    }
                    break;
            }
        }

        // Preserve parent validation rules if specified
        if (overrideConfig.PreserveValidation && parentField.ValidationRules != null)
        {
            childField.ValidationRules ??= new Dictionary<string, object>();
            foreach (var rule in parentField.ValidationRules)
            {
                if (!childField.ValidationRules.ContainsKey(rule.Key))
                {
                    childField.ValidationRules[rule.Key] = rule.Value;
                }
            }
        }

        result.FieldSources[parentField.Name] = InheritanceSource.Merged;
    }

    #endregion

    #region Import/Export Helper Methods

    private async Task ProcessImportOptionsAsync(ImportTemplate template, Core.Interfaces.TemplateImportOptions importOptions, CancellationToken cancellationToken)
    {
        // Assign new ID if requested
        if (importOptions.AssignNewIds)
        {
            template.Id = Guid.NewGuid();
            
            // Also assign new IDs to fields and zones
            foreach (var field in template.Fields)
            {
                field.Id = Guid.NewGuid();
                foreach (var zone in field.ExtractionZones)
                {
                    zone.Id = Guid.NewGuid();
                }
            }
        }

        // Apply name prefix/suffix
        if (!string.IsNullOrEmpty(importOptions.NamePrefix))
        {
            template.Name = $"{importOptions.NamePrefix}{template.Name}";
        }
        if (!string.IsNullOrEmpty(importOptions.NameSuffix))
        {
            template.Name = $"{template.Name}{importOptions.NameSuffix}";
        }

        // Override category if specified
        if (!string.IsNullOrEmpty(importOptions.OverrideCategory))
        {
            template.Category = importOptions.OverrideCategory;
        }

        // Add additional tags
        if (importOptions.AdditionalTags.Count > 0)
        {
            foreach (var tag in importOptions.AdditionalTags)
            {
                if (!template.Tags.Contains(tag))
                {
                    template.Tags.Add(tag);
                }
            }
        }

        // Update import metadata
        if (!string.IsNullOrEmpty(importOptions.ImportedBy))
        {
            template.LastModifiedBy = importOptions.ImportedBy;
            if (string.IsNullOrEmpty(template.CreatedBy))
            {
                template.CreatedBy = importOptions.ImportedBy;
            }
        }

        // Handle creation dates
        if (!importOptions.PreserveCreationDates)
        {
            template.CreatedAt = DateTime.UtcNow;
            template.LastModifiedAt = DateTime.UtcNow;
        }
        else
        {
            template.LastModifiedAt = DateTime.UtcNow;
        }

        // Handle version preservation
        if (!importOptions.PreserveVersions)
        {
            template.Version = "1.0.0";
        }

        // Set template activation state
        template.IsActive = importOptions.ActivateTemplates;

        // Add import metadata to template metadata
        foreach (var metadata in importOptions.ImportMetadata)
        {
            template.Metadata[metadata.Key] = metadata.Value;
        }

        // Add import timestamp
        template.Metadata["ImportedAt"] = DateTime.UtcNow;
        template.Metadata["ImportedBy"] = importOptions.ImportedBy ?? Environment.UserName;
    }

    private async Task<string> GenerateUniqueTemplateNameAsync(string baseName, CancellationToken cancellationToken)
    {
        var uniqueName = baseName;
        var counter = 1;

        while (await GetTemplateByNameAsync(uniqueName, cancellationToken) != null)
        {
            uniqueName = $"{baseName} ({counter})";
            counter++;
        }

        return uniqueName;
    }

    private async Task<ImportTemplate> HandleTemplateConflictAsync(ImportTemplate template, ImportTemplate existingTemplate, Core.Interfaces.TemplateImportOptions importOptions, CancellationToken cancellationToken)
    {
        switch (importOptions.ConflictResolution)
        {
            case Core.Interfaces.TemplateConflictResolution.Fail:
                throw new InvalidOperationException($"Template with name '{template.Name}' already exists");

            case Core.Interfaces.TemplateConflictResolution.Skip:
                throw new InvalidOperationException($"Template '{template.Name}' skipped due to conflict");

            case Core.Interfaces.TemplateConflictResolution.Overwrite:
                template.Id = existingTemplate.Id;
                template.CreatedAt = existingTemplate.CreatedAt;
                template.CreatedBy = existingTemplate.CreatedBy;
                return await UpdateTemplateAsync(template, cancellationToken);

            case Core.Interfaces.TemplateConflictResolution.Merge:
                return await MergeTemplatesAsync(existingTemplate, template, importOptions, cancellationToken);

            case Core.Interfaces.TemplateConflictResolution.Rename:
                template.Name = await GenerateUniqueTemplateNameAsync(template.Name, cancellationToken);
                return await CreateTemplateAsync(template, cancellationToken);

            default:
                throw new ArgumentException($"Unsupported conflict resolution: {importOptions.ConflictResolution}");
        }
    }

    private async Task<ImportTemplate> MergeTemplatesAsync(ImportTemplate existingTemplate, ImportTemplate newTemplate, Core.Interfaces.TemplateImportOptions importOptions, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Merging template: {TemplateName}", existingTemplate.Name);

        // Start with existing template
        var mergedTemplate = existingTemplate;

        // Merge description if new one is provided
        if (!string.IsNullOrEmpty(newTemplate.Description))
        {
            mergedTemplate.Description = newTemplate.Description;
        }

        // Merge tags
        foreach (var tag in newTemplate.Tags)
        {
            if (!mergedTemplate.Tags.Contains(tag))
            {
                mergedTemplate.Tags.Add(tag);
            }
        }

        // Merge supported formats
        foreach (var format in newTemplate.SupportedFormats)
        {
            if (!mergedTemplate.SupportedFormats.Contains(format))
            {
                mergedTemplate.SupportedFormats.Add(format);
            }
        }

        // Merge fields (add new fields, don't overwrite existing ones)
        var existingFieldNames = mergedTemplate.Fields.Select(f => f.Name).ToHashSet();
        foreach (var newField in newTemplate.Fields)
        {
            if (!existingFieldNames.Contains(newField.Name))
            {
                newField.Id = Guid.NewGuid(); // Assign new ID
                mergedTemplate.Fields.Add(newField);
            }
        }

        // Update metadata with merge information
        mergedTemplate.LastModifiedAt = DateTime.UtcNow;
        mergedTemplate.LastModifiedBy = importOptions.ImportedBy ?? Environment.UserName;
        mergedTemplate.Metadata["MergedAt"] = DateTime.UtcNow;
        mergedTemplate.Metadata["MergedFrom"] = newTemplate.Id.ToString();

        // Update the template
        return await UpdateTemplateAsync(mergedTemplate, cancellationToken);
    }

    #endregion

    #region Bulk Import/Export Operations

    public async Task<Core.Interfaces.TemplateExportResult> ExportTemplatesToFileAsync(IEnumerable<Guid> templateIds, string filePath, Core.Interfaces.TemplateExportFormat exportFormat = Core.Interfaces.TemplateExportFormat.Json, CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = new Core.Interfaces.TemplateExportResult
        {
            FilePath = filePath,
            Format = exportFormat,
            RequestedCount = templateIds.Count()
        };

        try
        {
            _logger.LogInformation("Exporting {Count} templates to file: {FilePath}", result.RequestedCount, filePath);

            // Get JSON data
            var jsonData = await ExportTemplatesAsync(templateIds, cancellationToken);
            
            // Convert to desired format
            string exportData;
            switch (exportFormat)
            {
                case Core.Interfaces.TemplateExportFormat.Json:
                    exportData = jsonData;
                    break;
                case Core.Interfaces.TemplateExportFormat.Xml:
                    exportData = ConvertJsonToXml(jsonData);
                    break;
                case Core.Interfaces.TemplateExportFormat.Yaml:
                    exportData = ConvertJsonToYaml(jsonData);
                    break;
                default:
                    throw new ArgumentException($"Unsupported export format: {exportFormat}");
            }

            // Ensure directory exists
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Write to file
            await File.WriteAllTextAsync(filePath, exportData, cancellationToken);

            // Get file info
            var fileInfo = new FileInfo(filePath);
            result.FileSizeBytes = fileInfo.Length;
            result.ExportedCount = result.RequestedCount; // Assuming all succeeded since ExportTemplatesAsync didn't throw
            result.IsSuccessful = true;
            
            result.ExportMetadata["ExportFormat"] = exportFormat.ToString();
            result.ExportMetadata["FileSize"] = fileInfo.Length;
            result.ExportMetadata["CreatedAt"] = DateTime.UtcNow;

            stopwatch.Stop();
            result.ExportDuration = stopwatch.Elapsed;

            _logger.LogInformation("Successfully exported {Count} templates to {FilePath} in {Duration}ms", 
                result.ExportedCount, filePath, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.ExportDuration = stopwatch.Elapsed;
            result.IsSuccessful = false;
            result.Errors.Add($"Export failed: {ex.Message}");
            
            _logger.LogError(ex, "Failed to export templates to file: {FilePath}", filePath);
            throw;
        }
    }

    public async Task<Core.Interfaces.TemplateImportBulkResult> ImportTemplatesFromFileAsync(string filePath, Core.Interfaces.TemplateImportOptions? importOptions = null, CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = new Core.Interfaces.TemplateImportBulkResult();

        try
        {
            _logger.LogInformation("Importing templates from file: {FilePath}", filePath);

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Import file not found: {filePath}");
            }

            // Read file content
            var fileContent = await File.ReadAllTextAsync(filePath, cancellationToken);
            var fileExtension = Path.GetExtension(filePath).ToLowerInvariant();

            // Convert to JSON if needed
            string jsonContent;
            switch (fileExtension)
            {
                case ".json":
                    jsonContent = fileContent;
                    break;
                case ".xml":
                    jsonContent = ConvertXmlToJson(fileContent);
                    break;
                case ".yaml":
                case ".yml":
                    jsonContent = ConvertYamlToJson(fileContent);
                    break;
                default:
                    throw new ArgumentException($"Unsupported file format: {fileExtension}");
            }

            // Import templates
            var importedTemplates = await ImportTemplatesAsync(jsonContent, importOptions, cancellationToken);
            
            result.ImportedTemplates = importedTemplates.ToList();
            result.SuccessfulCount = result.ImportedTemplates.Count;
            result.TotalCount = result.SuccessfulCount; // We'll need to parse the file to get actual count
            result.FailedCount = 0;
            result.SkippedCount = 0;
            result.IsSuccessful = true;

            stopwatch.Stop();
            result.ImportDuration = stopwatch.Elapsed;

            result.ImportStatistics["FileSize"] = new FileInfo(filePath).Length;
            result.ImportStatistics["FileFormat"] = fileExtension;
            result.ImportStatistics["ImportedAt"] = DateTime.UtcNow;

            _logger.LogInformation("Successfully imported {Count} templates from {FilePath} in {Duration}ms", 
                result.SuccessfulCount, filePath, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.ImportDuration = stopwatch.Elapsed;
            result.IsSuccessful = false;
            result.GeneralErrors.Add($"Import failed: {ex.Message}");
            
            _logger.LogError(ex, "Failed to import templates from file: {FilePath}", filePath);
            throw;
        }
    }

    public async Task<string> ExportTemplatesByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Exporting templates by category: {Category}", category);

            var templates = await GetTemplatesByCategoryAsync(category, cancellationToken);
            var templateIds = templates.Select(t => t.Id);

            if (!templateIds.Any())
            {
                _logger.LogWarning("No templates found in category: {Category}", category);
                
                // Return empty export data
                var emptyExportData = new TemplateExportData
                {
                    ExportVersion = "1.0",
                    ExportedAt = DateTime.UtcNow,
                    ExportedBy = Environment.UserName,
                    Templates = new List<ImportTemplate>(),
                    ExportMetadata = new Dictionary<string, object>
                    {
                        { "Category", category },
                        { "TemplateCount", 0 }
                    }
                };

                return JsonSerializer.Serialize(emptyExportData, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }

            return await ExportTemplatesAsync(templateIds, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export templates by category: {Category}", category);
            throw;
        }
    }

    public async Task<Core.Interfaces.TemplateImportValidationResult> ValidateImportAsync(string templatesJson, CancellationToken cancellationToken = default)
    {
        var result = new Core.Interfaces.TemplateImportValidationResult();

        try
        {
            _logger.LogInformation("Validating template import data");

            // Parse export data
            TemplateExportData exportData;
            try
            {
                exportData = JsonSerializer.Deserialize<TemplateExportData>(templatesJson, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (exportData?.Templates == null || exportData.Templates.Count == 0)
                {
                    throw new InvalidOperationException("Export data contains no templates");
                }
            }
            catch (JsonException)
            {
                // Try parsing as array of templates
                try
                {
                    var templatesArray = JsonSerializer.Deserialize<List<ImportTemplate>>(templatesJson, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    if (templatesArray == null || templatesArray.Count == 0)
                    {
                        throw new InvalidOperationException("No templates found in array");
                    }

                    exportData = new TemplateExportData
                    {
                        ExportVersion = "1.0",
                        ExportedAt = DateTime.UtcNow,
                        Templates = templatesArray
                    };
                }
                catch (JsonException ex)
                {
                    result.GeneralErrors.Add($"Invalid JSON format: {ex.Message}");
                    return result;
                }
            }

            result.TotalCount = exportData.Templates.Count;

            foreach (var template in exportData.Templates)
            {
                var validationSummary = new Core.Interfaces.TemplateValidationSummary
                {
                    TemplateName = template.Name,
                    TemplateId = template.Id
                };

                try
                {
                    // Validate template structure
                    var validationResult = template.ValidateTemplate();
                    validationSummary.IsValid = validationResult.IsValid;
                    validationSummary.Errors.AddRange(validationResult.Errors);
                    validationSummary.Warnings.AddRange(validationResult.Warnings);

                    // Check for conflicts with existing templates
                    var existingTemplate = await GetTemplateByNameAsync(template.Name, cancellationToken);
                    if (existingTemplate != null)
                    {
                        validationSummary.HasConflicts = true;
                        validationSummary.ConflictingTemplateNames.Add(existingTemplate.Name);
                    }

                    if (validationSummary.IsValid)
                    {
                        result.ValidCount++;
                    }
                    else
                    {
                        result.InvalidCount++;
                    }
                }
                catch (Exception ex)
                {
                    validationSummary.IsValid = false;
                    validationSummary.Errors.Add($"Validation failed: {ex.Message}");
                    result.InvalidCount++;
                }

                result.ValidationResults.Add(validationSummary);
            }

            result.IsValid = result.InvalidCount == 0;

            _logger.LogInformation("Template validation completed: {ValidCount} valid, {InvalidCount} invalid", 
                result.ValidCount, result.InvalidCount);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate template import");
            result.GeneralErrors.Add($"Validation failed: {ex.Message}");
            return result;
        }
    }

    #endregion

    #region Format Conversion Helpers

    private string ConvertJsonToXml(string jsonData)
    {
        // Simple XML conversion (for basic implementation)
        // In a real implementation, you might use a library like Newtonsoft.Json
        try
        {
            var doc = JsonDocument.Parse(jsonData);
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<TemplateExport>");
            sb.AppendLine($"  <ExportVersion>{doc.RootElement.GetProperty("exportVersion").GetString()}</ExportVersion>");
            sb.AppendLine($"  <ExportedAt>{doc.RootElement.GetProperty("exportedAt").GetString()}</ExportedAt>");
            sb.AppendLine($"  <ExportedBy>{doc.RootElement.GetProperty("exportedBy").GetString()}</ExportedBy>");
            sb.AppendLine("  <Templates>");
            
            foreach (var template in doc.RootElement.GetProperty("templates").EnumerateArray())
            {
                sb.AppendLine("    <Template>");
                sb.AppendLine($"      <Id>{template.GetProperty("id").GetString()}</Id>");
                sb.AppendLine($"      <Name>{template.GetProperty("name").GetString()}</Name>");
                sb.AppendLine($"      <Description>{template.GetProperty("description").GetString()}</Description>");
                // Add more template properties as needed
                sb.AppendLine("    </Template>");
            }
            
            sb.AppendLine("  </Templates>");
            sb.AppendLine("</TemplateExport>");
            return sb.ToString();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to convert JSON to XML: {ex.Message}", ex);
        }
    }

    private string ConvertJsonToYaml(string jsonData)
    {
        // Simple YAML conversion (for basic implementation)
        // In a real implementation, you might use a library like YamlDotNet
        try
        {
            var doc = JsonDocument.Parse(jsonData);
            var sb = new StringBuilder();
            sb.AppendLine($"exportVersion: \"{doc.RootElement.GetProperty("exportVersion").GetString()}\"");
            sb.AppendLine($"exportedAt: \"{doc.RootElement.GetProperty("exportedAt").GetString()}\"");
            sb.AppendLine($"exportedBy: \"{doc.RootElement.GetProperty("exportedBy").GetString()}\"");
            sb.AppendLine("templates:");
            
            foreach (var template in doc.RootElement.GetProperty("templates").EnumerateArray())
            {
                sb.AppendLine($"  - id: \"{template.GetProperty("id").GetString()}\"");
                sb.AppendLine($"    name: \"{template.GetProperty("name").GetString()}\"");
                sb.AppendLine($"    description: \"{template.GetProperty("description").GetString()}\"");
                // Add more template properties as needed
            }
            
            return sb.ToString();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to convert JSON to YAML: {ex.Message}", ex);
        }
    }

    private string ConvertXmlToJson(string xmlData)
    {
        // Simple placeholder - in real implementation, use proper XML to JSON conversion
        throw new NotImplementedException("XML to JSON conversion not implemented - use a proper library like Newtonsoft.Json");
    }

    private string ConvertYamlToJson(string yamlData)
    {
        // Simple placeholder - in real implementation, use proper YAML to JSON conversion
        throw new NotImplementedException("YAML to JSON conversion not implemented - use a proper library like YamlDotNet");
    }

    #endregion

    #region Import/Export Helper Methods

    private async Task<string> GenerateUniqueTemplateNameAsync(string baseName, CancellationToken cancellationToken)
    {
        var uniqueName = baseName;
        var counter = 1;

        while (await GetTemplateByNameAsync(uniqueName, cancellationToken) != null)
        {
            uniqueName = $"{baseName} ({counter})";
            counter++;
        }

        return uniqueName;
    }

    private async Task<ImportTemplate> HandleTemplateConflictAsync(ImportTemplate template, ImportTemplate existingTemplate, Core.Interfaces.TemplateImportOptions importOptions, CancellationToken cancellationToken)
    {
        switch (importOptions.ConflictResolution)
        {
            case Core.Interfaces.TemplateConflictResolution.Fail:
                throw new InvalidOperationException($"Template with name '{template.Name}' already exists");

            case Core.Interfaces.TemplateConflictResolution.Skip:
                throw new InvalidOperationException($"Template '{template.Name}' skipped due to conflict");

            case Core.Interfaces.TemplateConflictResolution.Overwrite:
                template.Id = existingTemplate.Id;
                template.CreatedAt = existingTemplate.CreatedAt;
                template.CreatedBy = existingTemplate.CreatedBy;
                return await UpdateTemplateAsync(template, cancellationToken);

            case Core.Interfaces.TemplateConflictResolution.Merge:
                return await MergeTemplatesAsync(existingTemplate, template, importOptions, cancellationToken);

            case Core.Interfaces.TemplateConflictResolution.Rename:
                template.Name = await GenerateUniqueTemplateNameAsync(template.Name, cancellationToken);
                return await CreateTemplateAsync(template, cancellationToken);

            default:
                throw new ArgumentException($"Unsupported conflict resolution: {importOptions.ConflictResolution}");
        }
    }

    private async Task<ImportTemplate> MergeTemplatesAsync(ImportTemplate existingTemplate, ImportTemplate newTemplate, Core.Interfaces.TemplateImportOptions importOptions, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Merging template: {TemplateName}", existingTemplate.Name);

        // Start with existing template
        var mergedTemplate = existingTemplate;

        // Merge description if new one is provided
        if (!string.IsNullOrEmpty(newTemplate.Description))
        {
            mergedTemplate.Description = newTemplate.Description;
        }

        // Merge tags
        foreach (var tag in newTemplate.Tags)
        {
            if (!mergedTemplate.Tags.Contains(tag))
            {
                mergedTemplate.Tags.Add(tag);
            }
        }

        // Merge supported formats
        foreach (var format in newTemplate.SupportedFormats)
        {
            if (!mergedTemplate.SupportedFormats.Contains(format))
            {
                mergedTemplate.SupportedFormats.Add(format);
            }
        }

        // Merge fields (add new fields, don't overwrite existing ones)
        var existingFieldNames = mergedTemplate.Fields.Select(f => f.Name).ToHashSet();
        foreach (var newField in newTemplate.Fields)
        {
            if (!existingFieldNames.Contains(newField.Name))
            {
                newField.Id = Guid.NewGuid(); // Assign new ID
                mergedTemplate.Fields.Add(newField);
            }
        }

        // Update metadata with merge information
        mergedTemplate.LastModifiedAt = DateTime.UtcNow;
        mergedTemplate.LastModifiedBy = importOptions.ImportedBy ?? Environment.UserName;
        mergedTemplate.Metadata["MergedAt"] = DateTime.UtcNow;
        mergedTemplate.Metadata["MergedFrom"] = newTemplate.Id.ToString();

        // Update the template
        return await UpdateTemplateAsync(mergedTemplate, cancellationToken);
    }

    #endregion

}

/// <summary>
/// Export data wrapper for templates
/// </summary>
public class TemplateExportData
{
    /// <summary>
    /// Version of the export format
    /// </summary>
    public string ExportVersion { get; set; } = "1.0";

    /// <summary>
    /// When the export was created
    /// </summary>
    public DateTime ExportedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Who created the export
    /// </summary>
    public string ExportedBy { get; set; } = string.Empty;

    /// <summary>
    /// List of templates in the export
    /// </summary>
    public List<ImportTemplate> Templates { get; set; } = new();

    /// <summary>
    /// Additional export metadata
    /// </summary>
    public Dictionary<string, object> ExportMetadata { get; set; } = new();
}

/// <summary>
/// Result of template import operation
/// </summary>
public class TemplateImportResult
{
    /// <summary>
    /// Whether the import was successful
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// Original template name from the import
    /// </summary>
    public string OriginalName { get; set; } = string.Empty;

    /// <summary>
    /// Original template ID from the import
    /// </summary>
    public Guid OriginalId { get; set; }

    /// <summary>
    /// The imported template (if successful)
    /// </summary>
    public ImportTemplate? ImportedTemplate { get; set; }

    /// <summary>
    /// Action taken during import
    /// </summary>
    public TemplateImportAction Action { get; set; }

    /// <summary>
    /// Any errors encountered during import
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Any warnings generated during import
    /// </summary>
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// Action taken during template import
/// </summary>
public enum TemplateImportAction
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
    /// Template was skipped due to conflicts
    /// </summary>
    Skipped,

    /// <summary>
    /// Import failed for this template
    /// </summary>
    Failed
}

/// <summary>
/// Represents a change record in template history
/// </summary>
public class TemplateChangeRecord
{
    public Guid Id { get; set; }
    public Guid TemplateId { get; set; }
    public string Version { get; set; } = string.Empty;
    public string ChangeDescription { get; set; } = string.Empty;
    public string ChangedBy { get; set; } = string.Empty;
    public DateTime ChangeDate { get; set; }
    public bool IsCurrent { get; set; }
}

/// <summary>
/// Results of comparing two template versions
/// </summary>
public class TemplateComparisonResult
{
    public Guid TemplateId { get; set; }
    public string Version1 { get; set; } = string.Empty;
    public string Version2 { get; set; } = string.Empty;
    public DateTime ComparisonDate { get; set; }
    public List<TemplateDifference> Differences { get; set; } = new();
    public bool HasDifferences => Differences.Count > 0;
    public int DifferenceCount => Differences.Count;
}

/// <summary>
/// Represents a specific difference between template versions
/// </summary>
public class TemplateDifference
{
    public string Property { get; set; } = string.Empty;
    public string? Value1 { get; set; }
    public string? Value2 { get; set; }
    public TemplateDifferenceType DifferenceType 
    { 
        get
        {
            if (Value1 == null) return TemplateDifferenceType.Added;
            if (Value2 == null) return TemplateDifferenceType.Removed;
            return TemplateDifferenceType.Modified;
        }
    }
}

/// <summary>
/// Types of differences between template versions
/// </summary>
public enum TemplateDifferenceType
{
    Added,
    Removed,
    Modified
} 