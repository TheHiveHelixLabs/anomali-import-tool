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

    #region Single Template Export/Import Tests

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
        var imported = await _templateService.CreateTemplateAsync(importedTemplate);

        // Assert - Verify all data is preserved
        imported.Should().NotBeNull();
        imported.Name.Should().Be(originalTemplate.Name);
        imported.Description.Should().Be(originalTemplate.Description);
        imported.Category.Should().Be(originalTemplate.Category);
        imported.SupportedFormats.Should().BeEquivalentTo(originalTemplate.SupportedFormats);
        imported.Fields.Should().HaveCount(originalTemplate.Fields.Count);

        // Verify field details
        for (int i = 0; i < originalTemplate.Fields.Count; i++)
        {
            var originalField = originalTemplate.Fields[i];
            var importedField = imported.Fields[i];

            importedField.Name.Should().Be(originalField.Name);
            importedField.DisplayName.Should().Be(originalField.DisplayName);
            importedField.FieldType.Should().Be(originalField.FieldType);
            importedField.IsRequired.Should().Be(originalField.IsRequired);
            importedField.DefaultValue.Should().Be(originalField.DefaultValue);
        }
    }

    [TestMethod]
    public async Task TemplateExportImport_TemplateWithExtractionRules_ShouldPreserveRules()
    {
        // Arrange
        var template = CreateTemplateWithExtractionRules();
        var created = await _templateService!.CreateTemplateAsync(template);

        // Act - Export and import
        var exportedJson = await _serializationService!.ExportTemplateAsync(created.Id);
        var importedTemplate = await _serializationService.ImportTemplateAsync(exportedJson);
        var imported = await _templateService.CreateTemplateAsync(importedTemplate);

        // Assert - Verify extraction rules are preserved
        imported.Fields.Should().HaveCount(template.Fields.Count);
        
        var fieldWithRules = imported.Fields.First(f => f.Name == "FieldWithRules");
        fieldWithRules.ExtractionRules.Should().HaveCount(2);
        
        var regexRule = fieldWithRules.ExtractionRules.First(r => r.RuleType == ExtractionRuleType.RegexPattern);
        regexRule.Pattern.Should().Be(@"Amount:\s*\$?(\d+(?:\.\d{2})?)");
        regexRule.Priority.Should().Be(1);
        
        var keywordRule = fieldWithRules.ExtractionRules.First(r => r.RuleType == ExtractionRuleType.KeywordSearch);
        keywordRule.Pattern.Should().Be("Total Amount");
        keywordRule.Priority.Should().Be(2);
    }

    [TestMethod]
    public async Task TemplateExportImport_TemplateWithExtractionZones_ShouldPreserveZones()
    {
        // Arrange
        var template = CreateTemplateWithExtractionZones();
        var created = await _templateService!.CreateTemplateAsync(template);

        // Act - Export and import
        var exportedJson = await _serializationService!.ExportTemplateAsync(created.Id);
        var importedTemplate = await _serializationService.ImportTemplateAsync(exportedJson);
        var imported = await _templateService.CreateTemplateAsync(importedTemplate);

        // Assert - Verify extraction zones are preserved
        var fieldWithZones = imported.Fields.First(f => f.Name == "HeaderField");
        fieldWithZones.ExtractionZones.Should().HaveCount(2);
        
        var zone1 = fieldWithZones.ExtractionZones[0];
        zone1.X.Should().Be(50);
        zone1.Y.Should().Be(100);
        zone1.Width.Should().Be(200);
        zone1.Height.Should().Be(30);
        zone1.PageNumber.Should().Be(1);
        
        var zone2 = fieldWithZones.ExtractionZones[1];
        zone2.X.Should().Be(50);
        zone2.Y.Should().Be(150);
        zone2.Width.Should().Be(200);
        zone2.Height.Should().Be(30);
        zone2.PageNumber.Should().Be(1);
    }

    [TestMethod]
    public async Task TemplateExportImport_TemplateWithConditionalRules_ShouldPreserveConditionalLogic()
    {
        // Arrange
        var template = CreateTemplateWithConditionalRules();
        var created = await _templateService!.CreateTemplateAsync(template);

        // Act - Export and import
        var exportedJson = await _serializationService!.ExportTemplateAsync(created.Id);
        var importedTemplate = await _serializationService.ImportTemplateAsync(exportedJson);
        var imported = await _templateService.CreateTemplateAsync(importedTemplate);

        // Assert - Verify conditional rules are preserved
        var fieldWithConditions = imported.Fields.First(f => f.Name == "ConditionalField");
        fieldWithConditions.ConditionalRules.Should().HaveCount(2);
        
        var rule1 = fieldWithConditions.ConditionalRules[0];
        rule1.Condition.Should().Be("document.contains('Invoice')");
        rule1.Action.Should().Be(ConditionalAction.ExtractField);
        rule1.TargetFieldName.Should().Be("InvoiceNumber");
        
        var rule2 = fieldWithConditions.ConditionalRules[1];
        rule2.Condition.Should().Be("document.contains('Receipt')");
        rule2.Action.Should().Be(ConditionalAction.SetDefaultValue);
        rule2.DefaultValue.Should().Be("N/A");
    }

    #endregion

    #region Batch Export/Import Tests

    [TestMethod]
    public async Task TemplateExportImport_MultipleTem plates_ShouldExportImportBatch()
    {
        // Arrange
        var templates = new List<ImportTemplate>
        {
            CreateSecurityTemplate(),
            CreateFinancialTemplate(),
            CreateLegalTemplate()
        };

        var createdTemplates = new List<ImportTemplate>();
        foreach (var template in templates)
        {
            var created = await _templateService!.CreateTemplateAsync(template);
            createdTemplates.Add(created);
        }

        // Act - Export multiple templates
        var exportedJson = await _serializationService!.ExportMultipleTemplatesAsync(
            createdTemplates.Select(t => t.Id).ToList());
        
        ValidateJsonFormat(exportedJson);

        // Act - Import multiple templates
        var importedTemplates = await _serializationService.ImportMultipleTemplatesAsync(exportedJson);
        
        // Create the imported templates in database
        var importedResults = new List<ImportTemplate>();
        foreach (var template in importedTemplates)
        {
            template.Name += " (Imported)"; // Avoid name conflicts
            var imported = await _templateService.CreateTemplateAsync(template);
            importedResults.Add(imported);
        }

        // Assert
        importedResults.Should().HaveCount(3);
        importedResults[0].Category.Should().Be("Security");
        importedResults[1].Category.Should().Be("Financial");
        importedResults[2].Category.Should().Be("Legal");
    }

    [TestMethod]
    public async Task TemplateExportImport_TemplateWithInheritance_ShouldPreserveHierarchy()
    {
        // Arrange - Create parent template
        var parentTemplate = CreateParentTemplate();
        var parent = await _templateService!.CreateTemplateAsync(parentTemplate);

        // Create child template
        var childTemplate = CreateChildTemplate();
        childTemplate.ParentTemplateId = parent.Id;
        var child = await _templateService.CreateTemplateAsync(childTemplate);

        // Act - Export both templates
        var exportedJson = await _serializationService!.ExportMultipleTemplatesAsync(
            new List<Guid> { parent.Id, child.Id });

        // Import templates
        var importedTemplates = await _serializationService.ImportMultipleTemplatesAsync(exportedJson);
        
        // Recreate hierarchy
        var importedParent = importedTemplates.First(t => t.Name.Contains("Parent"));
        var importedChild = importedTemplates.First(t => t.Name.Contains("Child"));
        
        importedParent.Name += " (Imported)";
        importedChild.Name += " (Imported)";
        
        var newParent = await _templateService.CreateTemplateAsync(importedParent);
        importedChild.ParentTemplateId = newParent.Id;
        var newChild = await _templateService.CreateTemplateAsync(importedChild);

        // Assert
        newChild.ParentTemplateId.Should().Be(newParent.Id);
        newChild.Fields.Should().HaveCountGreaterThan(newParent.Fields.Count, 
            "Child should have inherited fields plus its own");
    }

    #endregion

    #region JSON Format Validation Tests

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
    public async Task TemplateExportImport_InvalidJSON_ShouldThrowDescriptiveError()
    {
        // Arrange
        var invalidJson = "{ invalid json structure [";

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _serializationService!.ImportTemplateAsync(invalidJson));
        
        exception.Message.Should().Contain("Invalid JSON format");
    }

    [TestMethod]
    public async Task TemplateExportImport_MissingRequiredFields_ShouldThrowError()
    {
        // Arrange
        var incompleteJson = @"{
            ""version"": ""1.0"",
            ""templates"": [{
                ""name"": ""Incomplete Template""
                // Missing required fields like 'fields', 'supportedFormats'
            }]
        }";

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _serializationService!.ImportTemplateAsync(incompleteJson));
        
        exception.Message.Should().Contain("Missing required field");
    }

    [TestMethod]
    public async Task TemplateExportImport_UnsupportedVersion_ShouldThrowError()
    {
        // Arrange
        var futureVersionJson = @"{
            ""version"": ""2.0"",
            ""templates"": []
        }";

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<NotSupportedException>(
            () => _serializationService!.ImportTemplateAsync(futureVersionJson));
        
        exception.Message.Should().Contain("Unsupported template format version");
    }

    #endregion

    #region Error Handling Tests

    [TestMethod]
    public async Task TemplateExportImport_NonExistentTemplate_ShouldThrowError()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _serializationService!.ExportTemplateAsync(nonExistentId));
        
        exception.Message.Should().Contain("Template not found");
    }

    [TestMethod]
    public async Task TemplateExportImport_LargeTemplate_ShouldHandleGracefully()
    {
        // Arrange - Create template with many fields and complex rules
        var largeTemplate = CreateLargeTemplate();
        var created = await _templateService!.CreateTemplateAsync(largeTemplate);

        // Act
        var exportedJson = await _serializationService!.ExportTemplateAsync(created.Id);
        var importedTemplate = await _serializationService.ImportTemplateAsync(exportedJson);

        // Assert
        importedTemplate.Should().NotBeNull();
        importedTemplate.Fields.Should().HaveCount(largeTemplate.Fields.Count);
        
        // Verify JSON size is reasonable (not excessively large)
        var jsonSizeKB = System.Text.Encoding.UTF8.GetByteCount(exportedJson) / 1024;
        jsonSizeKB.Should().BeLessThan(1024, "Large template JSON should be under 1MB");
    }

    [TestMethod]
    public async Task TemplateExportImport_UnicodeContent_ShouldPreserveEncoding()
    {
        // Arrange
        var template = CreateUnicodeTemplate();
        var created = await _templateService!.CreateTemplateAsync(template);

        // Act
        var exportedJson = await _serializationService!.ExportTemplateAsync(created.Id);
        var importedTemplate = await _serializationService.ImportTemplateAsync(exportedJson);

        // Assert
        importedTemplate.Name.Should().Be("Шаблон с Unicode символами");
        importedTemplate.Description.Should().Be("描述包含中文字符 và tiếng Việt");
        importedTemplate.Fields[0].DisplayName.Should().Be("Поле с émojis 🚀🌟");
    }

    #endregion

    #region Performance Tests

    [TestMethod]
    public async Task TemplateExportImport_PerformanceBenchmark_ShouldMeetTimeRequirements()
    {
        // Arrange
        var templates = new List<ImportTemplate>();
        for (int i = 0; i < 10; i++)
        {
            var template = CreateComplexTemplate();
            template.Name = $"Performance Test Template {i}";
            var created = await _templateService!.CreateTemplateAsync(template);
            templates.Add(created);
        }

        // Act - Benchmark export
        var exportStopwatch = System.Diagnostics.Stopwatch.StartNew();
        var exportedJson = await _serializationService!.ExportMultipleTemplatesAsync(
            templates.Select(t => t.Id).ToList());
        exportStopwatch.Stop();

        // Act - Benchmark import
        var importStopwatch = System.Diagnostics.Stopwatch.StartNew();
        var importedTemplates = await _serializationService.ImportMultipleTemplatesAsync(exportedJson);
        importStopwatch.Stop();

        // Assert - Performance requirements
        exportStopwatch.ElapsedMilliseconds.Should().BeLessThan(5000, 
            "Exporting 10 templates should complete in under 5 seconds");
        importStopwatch.ElapsedMilliseconds.Should().BeLessThan(3000, 
            "Importing 10 templates should complete in under 3 seconds");
        
        importedTemplates.Should().HaveCount(10);
    }

    #endregion

    #region File-based Export/Import Tests

    [TestMethod]
    public async Task TemplateExportImport_FileExport_ShouldCreateValidFile()
    {
        // Arrange
        var template = CreateComplexTemplate();
        var created = await _templateService!.CreateTemplateAsync(template);
        var exportPath = Path.Combine(Path.GetTempPath(), "exported_template.json");
        _tempFiles.Add(exportPath);

        // Act
        await _serializationService!.ExportTemplateToFileAsync(created.Id, exportPath);

        // Assert
        File.Exists(exportPath).Should().BeTrue();
        var fileContent = await File.ReadAllTextAsync(exportPath);
        fileContent.Should().NotBeNullOrEmpty();
        ValidateJsonFormat(fileContent);
    }

    [TestMethod]
    public async Task TemplateExportImport_FileImport_ShouldReadValidFile()
    {
        // Arrange
        var template = CreateComplexTemplate();
        var created = await _templateService!.CreateTemplateAsync(template);
        var exportPath = Path.Combine(Path.GetTempPath(), "export_for_import.json");
        var importPath = Path.Combine(Path.GetTempPath(), "import_test.json");
        _tempFiles.Add(exportPath);
        _tempFiles.Add(importPath);

        // Export to file
        await _serializationService!.ExportTemplateToFileAsync(created.Id, exportPath);
        
        // Copy file to simulate external source
        File.Copy(exportPath, importPath);

        // Act
        var importedTemplate = await _serializationService.ImportTemplateFromFileAsync(importPath);

        // Assert
        importedTemplate.Should().NotBeNull();
        importedTemplate.Name.Should().Be(template.Name);
        importedTemplate.Fields.Should().HaveCount(template.Fields.Count);
    }

    [TestMethod]
    public async Task TemplateExportImport_BulkFileOperations_ShouldHandleDirectoryOperations()
    {
        // Arrange
        var templates = new List<ImportTemplate>
        {
            CreateSecurityTemplate(),
            CreateFinancialTemplate(),
            CreateLegalTemplate()
        };

        var createdTemplates = new List<ImportTemplate>();
        foreach (var template in templates)
        {
            var created = await _templateService!.CreateTemplateAsync(template);
            createdTemplates.Add(created);
        }

        var exportDirectory = Path.Combine(Path.GetTempPath(), "bulk_export_test");
        Directory.CreateDirectory(exportDirectory);
        _tempFiles.Add(exportDirectory);

        // Act - Bulk export to directory
        await _serializationService!.ExportMultipleTemplatesToDirectoryAsync(
            createdTemplates.Select(t => t.Id).ToList(), exportDirectory);

        // Assert
        var exportedFiles = Directory.GetFiles(exportDirectory, "*.json");
        exportedFiles.Should().HaveCount(3);

        // Verify each file contains valid template data
        foreach (var file in exportedFiles)
        {
            var content = await File.ReadAllTextAsync(file);
            ValidateJsonFormat(content);
        }
    }

    #endregion

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
                    MinValue = 0,
                    MaxValue = 999999.99m,
                    DefaultValue = "0.00"
                },
                new TemplateField 
                { 
                    Name = "Date", 
                    DisplayName = "Document Date", 
                    FieldType = FieldType.Date,
                    IsRequired = false,
                    ValidationPattern = @"\d{4}-\d{2}-\d{2}"
                },
                new TemplateField 
                { 
                    Name = "Category", 
                    DisplayName = "Document Category", 
                    FieldType = FieldType.DropdownList,
                    IsRequired = true,
                    Options = new List<string> { "Invoice", "Receipt", "Contract", "Report" },
                    DefaultValue = "Invoice"
                }
            },
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test-user",
            Version = 1
        };
    }

    private ImportTemplate CreateTemplateWithExtractionRules()
    {
        return new ImportTemplate
        {
            Id = Guid.NewGuid(),
            Name = "Template with Extraction Rules",
            Description = "Template containing various extraction rules",
            Category = "Testing",
            SupportedFormats = new List<string> { "pdf" },
            Fields = new List<TemplateField>
            {
                new TemplateField 
                { 
                    Name = "FieldWithRules", 
                    DisplayName = "Field with Rules", 
                    FieldType = FieldType.Text,
                    ExtractionRules = new List<ExtractionRule>
                    {
                        new ExtractionRule
                        {
                            RuleType = ExtractionRuleType.RegexPattern,
                            Pattern = @"Amount:\s*\$?(\d+(?:\.\d{2})?)",
                            Priority = 1
                        },
                        new ExtractionRule
                        {
                            RuleType = ExtractionRuleType.KeywordSearch,
                            Pattern = "Total Amount",
                            Priority = 2
                        }
                    }
                }
            },
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test-user"
        };
    }

    private ImportTemplate CreateTemplateWithExtractionZones()
    {
        return new ImportTemplate
        {
            Id = Guid.NewGuid(),
            Name = "Template with Extraction Zones",
            Description = "Template containing coordinate-based extraction zones",
            Category = "Testing",
            SupportedFormats = new List<string> { "pdf" },
            Fields = new List<TemplateField>
            {
                new TemplateField 
                { 
                    Name = "HeaderField", 
                    DisplayName = "Header Field", 
                    FieldType = FieldType.Text,
                    ExtractionZones = new List<ExtractionZone>
                    {
                        new ExtractionZone { X = 50, Y = 100, Width = 200, Height = 30, PageNumber = 1 },
                        new ExtractionZone { X = 50, Y = 150, Width = 200, Height = 30, PageNumber = 1 }
                    }
                }
            },
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test-user"
        };
    }

    private ImportTemplate CreateTemplateWithConditionalRules()
    {
        return new ImportTemplate
        {
            Id = Guid.NewGuid(),
            Name = "Template with Conditional Rules",
            Description = "Template containing conditional extraction logic",
            Category = "Testing",
            SupportedFormats = new List<string> { "pdf" },
            Fields = new List<TemplateField>
            {
                new TemplateField 
                { 
                    Name = "ConditionalField", 
                    DisplayName = "Conditional Field", 
                    FieldType = FieldType.Text,
                    ConditionalRules = new List<ConditionalExtractionRule>
                    {
                        new ConditionalExtractionRule
                        {
                            Condition = "document.contains('Invoice')",
                            Action = ConditionalAction.ExtractField,
                            TargetFieldName = "InvoiceNumber"
                        },
                        new ConditionalExtractionRule
                        {
                            Condition = "document.contains('Receipt')",
                            Action = ConditionalAction.SetDefaultValue,
                            DefaultValue = "N/A"
                        }
                    }
                }
            },
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test-user"
        };
    }

    private ImportTemplate CreateSecurityTemplate()
    {
        return new ImportTemplate
        {
            Id = Guid.NewGuid(),
            Name = "Security Report Template",
            Description = "Template for security reports",
            Category = "Security",
            SupportedFormats = new List<string> { "pdf", "docx" },
            Fields = new List<TemplateField>
            {
                new TemplateField { Name = "ThreatLevel", DisplayName = "Threat Level", FieldType = FieldType.Text },
                new TemplateField { Name = "Analyst", DisplayName = "Security Analyst", FieldType = FieldType.Text }
            },
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test-user"
        };
    }

    private ImportTemplate CreateFinancialTemplate()
    {
        return new ImportTemplate
        {
            Id = Guid.NewGuid(),
            Name = "Financial Report Template",
            Description = "Template for financial reports",
            Category = "Financial",
            SupportedFormats = new List<string> { "pdf", "xlsx" },
            Fields = new List<TemplateField>
            {
                new TemplateField { Name = "TotalAmount", DisplayName = "Total Amount", FieldType = FieldType.Number },
                new TemplateField { Name = "Currency", DisplayName = "Currency", FieldType = FieldType.Text }
            },
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test-user"
        };
    }

    private ImportTemplate CreateLegalTemplate()
    {
        return new ImportTemplate
        {
            Id = Guid.NewGuid(),
            Name = "Legal Document Template",
            Description = "Template for legal documents",
            Category = "Legal",
            SupportedFormats = new List<string> { "pdf", "docx" },
            Fields = new List<TemplateField>
            {
                new TemplateField { Name = "CaseNumber", DisplayName = "Case Number", FieldType = FieldType.Text },
                new TemplateField { Name = "Court", DisplayName = "Court Name", FieldType = FieldType.Text }
            },
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test-user"
        };
    }

    private ImportTemplate CreateParentTemplate()
    {
        return new ImportTemplate
        {
            Id = Guid.NewGuid(),
            Name = "Parent Template",
            Description = "Base template for inheritance",
            Category = "Base",
            SupportedFormats = new List<string> { "pdf" },
            Fields = new List<TemplateField>
            {
                new TemplateField { Name = "BaseField1", DisplayName = "Base Field 1", FieldType = FieldType.Text },
                new TemplateField { Name = "BaseField2", DisplayName = "Base Field 2", FieldType = FieldType.Text }
            },
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test-user"
        };
    }

    private ImportTemplate CreateChildTemplate()
    {
        return new ImportTemplate
        {
            Id = Guid.NewGuid(),
            Name = "Child Template",
            Description = "Template that inherits from parent",
            Category = "Inherited",
            SupportedFormats = new List<string> { "pdf" },
            Fields = new List<TemplateField>
            {
                new TemplateField { Name = "ChildField1", DisplayName = "Child Field 1", FieldType = FieldType.Text },
                new TemplateField { Name = "ChildField2", DisplayName = "Child Field 2", FieldType = FieldType.Number }
            },
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test-user"
        };
    }

    private ImportTemplate CreateLargeTemplate()
    {
        var fields = new List<TemplateField>();
        for (int i = 0; i < 50; i++)
        {
            fields.Add(new TemplateField
            {
                Name = $"Field{i}",
                DisplayName = $"Field {i}",
                FieldType = FieldType.Text,
                Description = new string('A', 100), // Large description
                ExtractionRules = new List<ExtractionRule>
                {
                    new ExtractionRule
                    {
                        RuleType = ExtractionRuleType.RegexPattern,
                        Pattern = $@"Field{i}:\s*(.+)",
                        Priority = 1
                    }
                }
            });
        }

        return new ImportTemplate
        {
            Id = Guid.NewGuid(),
            Name = "Large Template",
            Description = new string('B', 1000), // Large description
            Category = "Testing",
            SupportedFormats = new List<string> { "pdf", "docx", "xlsx" },
            Fields = fields,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test-user"
        };
    }

    private ImportTemplate CreateUnicodeTemplate()
    {
        return new ImportTemplate
        {
            Id = Guid.NewGuid(),
            Name = "Шаблон с Unicode символами",
            Description = "描述包含中文字符 và tiếng Việt",
            Category = "Unicode",
            SupportedFormats = new List<string> { "pdf" },
            Fields = new List<TemplateField>
            {
                new TemplateField 
                { 
                    Name = "UnicodeField", 
                    DisplayName = "Поле с émojis 🚀🌟", 
                    FieldType = FieldType.Text,
                    Description = "Beschreibung auf Deutsch mit Umlauten: äöü"
                }
            },
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test-user"
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
        
        // Validate field structure if fields exist
        if (fieldsElement.GetArrayLength() > 0)
        {
            var firstField = fieldsElement.EnumerateArray().First();
            firstField.TryGetProperty("name", out _).Should().BeTrue();
            firstField.TryGetProperty("displayName", out _).Should().BeTrue();
            firstField.TryGetProperty("fieldType", out _).Should().BeTrue();
        }
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
                IsActive INTEGER DEFAULT 1,
                ParentTemplateId TEXT
            );

            CREATE TABLE IF NOT EXISTS TemplateVersions (
                Id TEXT PRIMARY KEY,
                TemplateId TEXT,
                Version INTEGER,
                Data TEXT,
                CreatedAt TEXT,
                CreatedBy TEXT,
                ChangeLog TEXT,
                FOREIGN KEY (TemplateId) REFERENCES ImportTemplates(Id)
            );
        ";

        using var command = new SqliteCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}

// Supporting classes for template serialization
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

    public async Task<string> ExportMultipleTemplatesAsync(List<Guid> templateIds)
    {
        var templates = new List<ImportTemplate>();
        foreach (var id in templateIds)
        {
            var template = await _templateService.GetTemplateByIdAsync(id);
            if (template != null)
                templates.Add(template);
        }

        var exportData = new
        {
            version = "1.0",
            exportedAt = DateTime.UtcNow.ToString("O"),
            templates = templates
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

            return templates.First();
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("Invalid JSON format", ex);
        }
        
        await Task.CompletedTask;
    }

    public async Task<List<ImportTemplate>> ImportMultipleTemplatesAsync(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("JSON cannot be empty");

        try
        {
            var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (!root.TryGetProperty("templates", out var templatesElement))
                throw new ArgumentException("Missing required field: templates");

            var templates = JsonSerializer.Deserialize<ImportTemplate[]>(templatesElement.GetRawText());
            return templates?.ToList() ?? new List<ImportTemplate>();
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

    public async Task ExportMultipleTemplatesToDirectoryAsync(List<Guid> templateIds, string directoryPath)
    {
        Directory.CreateDirectory(directoryPath);

        foreach (var templateId in templateIds)
        {
            var template = await _templateService.GetTemplateByIdAsync(templateId);
            if (template != null)
            {
                var fileName = $"{template.Name.Replace(" ", "_")}.json";
                var filePath = Path.Combine(directoryPath, fileName);
                await ExportTemplateToFileAsync(templateId, filePath);
            }
        }
    }
} 