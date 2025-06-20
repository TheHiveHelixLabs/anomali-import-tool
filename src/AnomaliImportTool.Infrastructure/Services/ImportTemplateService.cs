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
        throw new NotImplementedException("Template import/export will be implemented in task 2.6");
    }

    public async Task<string> ExportTemplatesAsync(IEnumerable<Guid> templateIds, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Template import/export will be implemented in task 2.6");
    }

    public async Task<ImportTemplate> ImportTemplateAsync(string templateJson, Core.Interfaces.TemplateImportOptions? importOptions = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Template import/export will be implemented in task 2.6");
    }

    public async Task<IEnumerable<ImportTemplate>> ImportTemplatesAsync(string templatesJson, Core.Interfaces.TemplateImportOptions? importOptions = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Template import/export will be implemented in task 2.6");
    }

    #endregion

    #region Template Validation and Testing - Placeholder

    public async Task<TemplateValidationResult> ValidateTemplateAsync(ImportTemplate template, CancellationToken cancellationToken = default)
    {
        return template.ValidateTemplate();
    }

    public async Task<TemplateTestResult> TestTemplateAsync(Guid templateId, string documentPath, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Template testing will be implemented in task 2.7");
    }

    #endregion

    #region Template Usage Statistics - Placeholder

    public async Task UpdateUsageStatisticsAsync(Guid templateId, bool successful, TimeSpan extractionTime, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Usage statistics will be implemented in task 2.7");
    }

    public async Task<TemplateUsageStats> GetUsageStatisticsAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Usage statistics will be implemented in task 2.7");
    }

    public async Task<IEnumerable<ImportTemplate>> GetMostUsedTemplatesAsync(int count = 10, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Usage statistics will be implemented in task 2.7");
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
}

/// <summary>
/// Represents a change record in template version history
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