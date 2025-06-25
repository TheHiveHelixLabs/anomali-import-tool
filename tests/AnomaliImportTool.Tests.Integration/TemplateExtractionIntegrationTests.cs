using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AnomaliImportTool.Core.Interfaces;
using AnomaliImportTool.Core.Models;
using AnomaliImportTool.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AnomaliImportTool.Tests.Integration;

[TestClass]
public class TemplateExportImportIntegrationTests
{
    private ServiceProvider? _serviceProvider;
    private IImportTemplateService? _templateService;
    private TemplateSerializationService? _serializationService;
    private string _tempDatabasePath = string.Empty;
    private readonly List<string> _tempFiles = new();

    [TestInitialize]
    public async Task TestInitialize()
    {
        // Create temporary database
        _tempDatabasePath = Path.GetTempFileName();
        File.Delete(_tempDatabasePath);
        _tempDatabasePath += ".db";

        // Set up service provider
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging(builder => builder.AddConsole());
        
        // Add services
        services.AddSingleton<IImportTemplateService, ImportTemplateService>();
        services.AddSingleton<TemplateSerializationService>();

        // Configure database connection
        services.AddSingleton<SqliteConnection>(provider =>
        {
            var connection = new SqliteConnection($"Data Source={_tempDatabasePath}");
            connection.Open();
            return connection;
        });

        _serviceProvider = services.BuildServiceProvider();
        _templateService = _serviceProvider.GetRequiredService<IImportTemplateService>();
        _serializationService = _serviceProvider.GetRequiredService<TemplateSerializationService>();

        // Initialize database schema
        await InitializeDatabaseSchemaAsync();
    }

    [TestCleanup]
    public async Task TestCleanup()
    {
        // Clean up temp files
        foreach (var tempFile in _tempFiles)
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }

        _serviceProvider?.Dispose();
        
        if (File.Exists(_tempDatabasePath))
            File.Delete(_tempDatabasePath);

        await Task.CompletedTask;
    }

    [TestMethod]
    public async Task TemplateExportImport_SingleTemplate_ShouldPreserveAllData()
    {
        // Arrange
        var originalTemplate = CreateComplexTemplate();
        var created = await _templateService!.CreateTemplateAsync(originalTemplate);

        // Act - Export template
        var exportedJson = await _serializationService!.ExportTemplateAsync(created.Id);
        exportedJson.Should().NotBeNullOrEmpty("Export should produce valid JSON");

        // Validate JSON format
        ValidateJsonFormat(exportedJson);

        // Act - Import template
        var importedTemplate = await _serializationService.ImportTemplateAsync(exportedJson);
        importedTemplate.Name += " (Imported)"; // Avoid name conflicts
        var imported = await _templateService.CreateTemplateAsync(importedTemplate);

        // Assert - Verify all data is preserved
        imported.Should().NotBeNull();
        imported.Name.Should().Be(originalTemplate.Name + " (Imported)");
        imported.Description.Should().Be(originalTemplate.Description);
        imported.Category.Should().Be(originalTemplate.Category);
        imported.SupportedFormats.Should().BeEquivalentTo(originalTemplate.SupportedFormats);
        imported.Fields.Should().HaveCount(originalTemplate.Fields.Count);
    }

    [TestMethod]
    public async Task TemplateExportImport_JSONSchema_ShouldFollowExpectedStructure()
    {
        // Arrange
        var template = CreateComplexTemplate();
        var created = await _templateService!.CreateTemplateAsync(template);

        // Act
        var exportedJson = await _serializationService!.ExportTemplateAsync(created.Id);

        // Assert - Parse and validate JSON structure
        var jsonDocument = JsonDocument.Parse(exportedJson);
        var root = jsonDocument.RootElement;

        // Verify required properties
        root.TryGetProperty("version", out var versionElement).Should().BeTrue();
        versionElement.GetString().Should().Be("1.0");

        root.TryGetProperty("exportedAt", out var exportedAtElement).Should().BeTrue();
        DateTime.TryParse(exportedAtElement.GetString(), out _).Should().BeTrue();

        root.TryGetProperty("templates", out var templatesElement).Should().BeTrue();
        templatesElement.ValueKind.Should().Be(JsonValueKind.Array);

        var templateElement = templatesElement.EnumerateArray().First();
        ValidateTemplateJsonStructure(templateElement);
    }

    [TestMethod]
    public async Task TemplateExportImport_FileOperations_ShouldWorkWithFiles()
    {
        // Arrange
        var template = CreateComplexTemplate();
        var created = await _templateService!.CreateTemplateAsync(template);
        var exportPath = Path.Combine(Path.GetTempPath(), "exported_template.json");
        _tempFiles.Add(exportPath);

        // Act - Export to file
        await _serializationService!.ExportTemplateToFileAsync(created.Id, exportPath);

        // Assert - File exists and contains valid JSON
        File.Exists(exportPath).Should().BeTrue();
        var fileContent = await File.ReadAllTextAsync(exportPath);
        ValidateJsonFormat(fileContent);

        // Act - Import from file
        var importedTemplate = await _serializationService.ImportTemplateFromFileAsync(exportPath);

        // Assert
        importedTemplate.Should().NotBeNull();
        importedTemplate.Name.Should().Be(template.Name);
        importedTemplate.Fields.Should().HaveCount(template.Fields.Count);
    }

    // Helper methods
    private ImportTemplate CreateComplexTemplate()
    {
        return new ImportTemplate
        {
            Id = Guid.NewGuid(),
            Name = "Complex Export Test Template",
            Description = "A complex template for testing export/import functionality",
            Category = "Testing",
            SupportedFormats = new List<string> { "pdf", "docx", "xlsx" },
            Fields = new List<TemplateField>
            {
                new TemplateField 
                { 
                    Name = "Title", 
                    DisplayName = "Document Title", 
                    FieldType = FieldType.Text,
                    IsRequired = true,
                    Description = "The main title of the document"
                },
                new TemplateField 
                { 
                    Name = "Amount", 
                    DisplayName = "Amount", 
                    FieldType = FieldType.Number,
                    IsRequired = true,
                    DefaultValue = "0.00"
                }
            },
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test-user",
            Version = 1
        };
    }

    private void ValidateJsonFormat(string json)
    {
        json.Should().NotBeNullOrEmpty();
        
        // Parse JSON to ensure it's valid
        var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        
        // Verify basic structure
        root.ValueKind.Should().Be(JsonValueKind.Object);
        root.TryGetProperty("version", out _).Should().BeTrue("JSON should have version property");
        root.TryGetProperty("exportedAt", out _).Should().BeTrue("JSON should have exportedAt property");
        root.TryGetProperty("templates", out var templatesElement).Should().BeTrue("JSON should have templates array");
        templatesElement.ValueKind.Should().Be(JsonValueKind.Array);
    }

    private void ValidateTemplateJsonStructure(JsonElement templateElement)
    {
        templateElement.ValueKind.Should().Be(JsonValueKind.Object);
        
        // Required properties
        templateElement.TryGetProperty("id", out _).Should().BeTrue();
        templateElement.TryGetProperty("name", out _).Should().BeTrue();
        templateElement.TryGetProperty("supportedFormats", out var formatsElement).Should().BeTrue();
        formatsElement.ValueKind.Should().Be(JsonValueKind.Array);
        
        templateElement.TryGetProperty("fields", out var fieldsElement).Should().BeTrue();
        fieldsElement.ValueKind.Should().Be(JsonValueKind.Array);
    }

    private async Task InitializeDatabaseSchemaAsync()
    {
        var connection = _serviceProvider!.GetRequiredService<SqliteConnection>();
        var sql = @"
            CREATE TABLE IF NOT EXISTS ImportTemplates (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                Description TEXT,
                Category TEXT,
                SupportedFormats TEXT,
                Fields TEXT,
                CreatedAt TEXT,
                CreatedBy TEXT,
                ModifiedAt TEXT,
                ModifiedBy TEXT,
                Version INTEGER DEFAULT 1,
                IsActive INTEGER DEFAULT 1
            );
        ";

        using var command = new SqliteCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}

// Supporting service for template serialization
public class TemplateSerializationService
{
    private readonly IImportTemplateService _templateService;
    private readonly ILogger<TemplateSerializationService> _logger;

    public TemplateSerializationService(IImportTemplateService templateService, ILogger<TemplateSerializationService> logger)
    {
        _templateService = templateService;
        _logger = logger;
    }

    public async Task<string> ExportTemplateAsync(Guid templateId)
    {
        var template = await _templateService.GetTemplateByIdAsync(templateId);
        if (template == null)
            throw new ArgumentException("Template not found");

        var exportData = new
        {
            version = "1.0",
            exportedAt = DateTime.UtcNow.ToString("O"),
            templates = new[] { template }
        };

        return JsonSerializer.Serialize(exportData, new JsonSerializerOptions { WriteIndented = true });
    }

    public async Task<ImportTemplate> ImportTemplateAsync(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("JSON cannot be empty");

        try
        {
            var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (!root.TryGetProperty("version", out var versionElement))
                throw new ArgumentException("Missing required field: version");

            var version = versionElement.GetString();
            if (version != "1.0")
                throw new NotSupportedException($"Unsupported template format version: {version}");

            if (!root.TryGetProperty("templates", out var templatesElement))
                throw new ArgumentException("Missing required field: templates");

            var templates = JsonSerializer.Deserialize<ImportTemplate[]>(templatesElement.GetRawText());
            if (templates == null || !templates.Any())
                throw new ArgumentException("No templates found in JSON");

            var importedTemplate = templates.First();
            importedTemplate.Id = Guid.NewGuid(); // Generate new ID for import
            return importedTemplate;
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("Invalid JSON format", ex);
        }
        
        await Task.CompletedTask;
    }

    public async Task ExportTemplateToFileAsync(Guid templateId, string filePath)
    {
        var json = await ExportTemplateAsync(templateId);
        await File.WriteAllTextAsync(filePath, json);
    }

    public async Task<ImportTemplate> ImportTemplateFromFileAsync(string filePath)
    {
        var json = await File.ReadAllTextAsync(filePath);
        return await ImportTemplateAsync(json);
    }
}