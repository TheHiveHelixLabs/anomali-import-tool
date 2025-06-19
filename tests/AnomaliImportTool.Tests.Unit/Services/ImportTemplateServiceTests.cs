using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AnomaliImportTool.Core.Interfaces;
using AnomaliImportTool.Core.Models;
using AnomaliImportTool.Core.Services;
using AnomaliImportTool.Infrastructure.Database;
using AnomaliImportTool.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AnomaliImportTool.Tests.Unit.Services;

public class ImportTemplateServiceTests : IDisposable
{
    private readonly Mock<ILogger<ImportTemplateService>> _mockLogger;
    private readonly Mock<ILogger<TemplateDatabaseService>> _mockDbLogger;
    private readonly Mock<ILogger<TemplateSerializationService>> _mockSerializationLogger;
    private readonly TemplateDatabaseService _databaseService;
    private readonly TemplateSerializationService _serializationService;
    private readonly ImportTemplateService _service;
    private readonly string _testDatabasePath;
    private bool _disposed = false;

    public ImportTemplateServiceTests()
    {
        _mockLogger = new Mock<ILogger<ImportTemplateService>>();
        _mockDbLogger = new Mock<ILogger<TemplateDatabaseService>>();
        _mockSerializationLogger = new Mock<ILogger<TemplateSerializationService>>();
        
        // Create temporary database for testing
        _testDatabasePath = Path.Combine(Path.GetTempPath(), $"test_templates_{Guid.NewGuid()}.db");
        
        _databaseService = new TemplateDatabaseService(_mockDbLogger.Object, _testDatabasePath);
        _serializationService = new TemplateSerializationService(_mockSerializationLogger.Object);
        _service = new ImportTemplateService(_mockLogger.Object, _databaseService, _serializationService);
    }

    [Fact]
    public async Task CreateTemplateAsync_WithValidTemplate_ShouldCreateSuccessfully()
    {
        // Arrange
        await _databaseService.InitializeDatabaseAsync();
        
        var template = CreateTestTemplate("Test Template", "Security");
        
        // Act
        var result = await _service.CreateTemplateAsync(template);
        
        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBe(Guid.Empty);
        result.Name.Should().Be("Test Template");
        result.Category.Should().Be("Security");
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        result.LastModifiedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task CreateTemplateAsync_WithInvalidTemplate_ShouldThrowException()
    {
        // Arrange
        await _databaseService.InitializeDatabaseAsync();
        
        var template = new ImportTemplate
        {
            Name = "", // Invalid - empty name
            SupportedFormats = new List<string>() // Invalid - no supported formats
        };
        
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateTemplateAsync(template));
    }

    [Fact]
    public async Task GetTemplateAsync_WithExistingId_ShouldReturnTemplate()
    {
        // Arrange
        await _databaseService.InitializeDatabaseAsync();
        
        var template = CreateTestTemplate("Existing Template", "Reports");
        var created = await _service.CreateTemplateAsync(template);
        
        // Act
        var result = await _service.GetTemplateAsync(created.Id);
        
        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(created.Id);
        result.Name.Should().Be("Existing Template");
        result.Category.Should().Be("Reports");
        result.Fields.Should().HaveCount(2); // From CreateTestTemplate
    }

    [Fact]
    public async Task GetTemplateAsync_WithNonExistentId_ShouldReturnNull()
    {
        // Arrange
        await _databaseService.InitializeDatabaseAsync();
        var nonExistentId = Guid.NewGuid();
        
        // Act
        var result = await _service.GetTemplateAsync(nonExistentId);
        
        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetTemplateByNameAsync_WithExistingName_ShouldReturnTemplate()
    {
        // Arrange
        await _databaseService.InitializeDatabaseAsync();
        
        var template = CreateTestTemplate("Unique Template Name", "Security");
        await _service.CreateTemplateAsync(template);
        
        // Act
        var result = await _service.GetTemplateByNameAsync("Unique Template Name");
        
        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Unique Template Name");
        result.Category.Should().Be("Security");
    }

    [Fact]
    public async Task UpdateTemplateAsync_WithValidChanges_ShouldUpdateSuccessfully()
    {
        // Arrange
        await _databaseService.InitializeDatabaseAsync();
        
        var template = CreateTestTemplate("Original Name", "Security");
        var created = await _service.CreateTemplateAsync(template);
        
        // Modify template
        created.Name = "Updated Name";
        created.Category = "Reports";
        created.Description = "Updated description";
        
        // Act
        var result = await _service.UpdateTemplateAsync(created);
        
        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Updated Name");
        result.Category.Should().Be("Reports");
        result.Description.Should().Be("Updated description");
        result.LastModifiedAt.Should().BeAfter(result.CreatedAt);
    }

    [Fact]
    public async Task DeleteTemplateAsync_WithExistingTemplate_ShouldMarkAsInactive()
    {
        // Arrange
        await _databaseService.InitializeDatabaseAsync();
        
        var template = CreateTestTemplate("Template to Delete", "Security");
        var created = await _service.CreateTemplateAsync(template);
        
        // Act
        var result = await _service.DeleteTemplateAsync(created.Id);
        
        // Assert
        result.Should().BeTrue();
        
        // Verify soft delete - should not appear in active templates
        var activeTemplates = await _service.GetAllTemplatesAsync(includeInactive: false);
        activeTemplates.Should().NotContain(t => t.Id == created.Id);
        
        // But should appear when including inactive
        var allTemplates = await _service.GetAllTemplatesAsync(includeInactive: true);
        allTemplates.Should().Contain(t => t.Id == created.Id && !t.IsActive);
    }

    [Fact]
    public async Task GetAllTemplatesAsync_WithMultipleTemplates_ShouldReturnAllActive()
    {
        // Arrange
        await _databaseService.InitializeDatabaseAsync();
        
        var template1 = CreateTestTemplate("Template 1", "Security");
        var template2 = CreateTestTemplate("Template 2", "Reports");
        var template3 = CreateTestTemplate("Template 3", "APT");
        
        await _service.CreateTemplateAsync(template1);
        await _service.CreateTemplateAsync(template2);
        var created3 = await _service.CreateTemplateAsync(template3);
        
        // Delete one template
        await _service.DeleteTemplateAsync(created3.Id);
        
        // Act
        var activeTemplates = await _service.GetAllTemplatesAsync(includeInactive: false);
        var allTemplates = await _service.GetAllTemplatesAsync(includeInactive: true);
        
        // Assert
        activeTemplates.Should().HaveCount(2);
        activeTemplates.Should().AllSatisfy(t => t.IsActive.Should().BeTrue());
        
        allTemplates.Should().HaveCount(3);
        allTemplates.Should().Contain(t => !t.IsActive);
    }

    [Fact]
    public async Task SearchTemplatesAsync_WithNameCriteria_ShouldReturnMatchingTemplates()
    {
        // Arrange
        await _databaseService.InitializeDatabaseAsync();
        
        await _service.CreateTemplateAsync(CreateTestTemplate("Security Report Template", "Security"));
        await _service.CreateTemplateAsync(CreateTestTemplate("APT Analysis Template", "APT"));
        await _service.CreateTemplateAsync(CreateTestTemplate("Security Alert Template", "Security"));
        
        var searchCriteria = new TemplateSearchCriteria
        {
            SearchTerm = "Security"
        };
        
        // Act
        var results = await _service.SearchTemplatesAsync(searchCriteria);
        
        // Assert
        results.Should().HaveCount(2);
        results.Should().AllSatisfy(t => t.Name.Should().Contain("Security"));
    }

    [Fact]
    public async Task SearchTemplatesAsync_WithCategoryCriteria_ShouldReturnMatchingTemplates()
    {
        // Arrange
        await _databaseService.InitializeDatabaseAsync();
        
        await _service.CreateTemplateAsync(CreateTestTemplate("Template 1", "Security"));
        await _service.CreateTemplateAsync(CreateTestTemplate("Template 2", "Reports"));
        await _service.CreateTemplateAsync(CreateTestTemplate("Template 3", "Security"));
        
        var searchCriteria = new TemplateSearchCriteria
        {
            Category = "Security"
        };
        
        // Act
        var results = await _service.SearchTemplatesAsync(searchCriteria);
        
        // Assert
        results.Should().HaveCount(2);
        results.Should().AllSatisfy(t => t.Category.Should().Be("Security"));
    }

    [Fact]
    public async Task GetTemplatesByCategoryAsync_ShouldReturnTemplatesInCategory()
    {
        // Arrange
        await _databaseService.InitializeDatabaseAsync();
        
        await _service.CreateTemplateAsync(CreateTestTemplate("Security Template 1", "Security"));
        await _service.CreateTemplateAsync(CreateTestTemplate("Reports Template 1", "Reports"));
        await _service.CreateTemplateAsync(CreateTestTemplate("Security Template 2", "Security"));
        
        // Act
        var results = await _service.GetTemplatesByCategoryAsync("Security");
        
        // Assert
        results.Should().HaveCount(2);
        results.Should().AllSatisfy(t => t.Category.Should().Be("Security"));
    }

    [Fact]
    public async Task GetTemplatesByFormatAsync_ShouldReturnTemplatesSupportingFormat()
    {
        // Arrange
        await _databaseService.InitializeDatabaseAsync();
        
        var pdfTemplate = CreateTestTemplate("PDF Template", "Security");
        pdfTemplate.SupportedFormats = new List<string> { "pdf", "docx" };
        
        var wordTemplate = CreateTestTemplate("Word Template", "Reports");
        wordTemplate.SupportedFormats = new List<string> { "docx", "txt" };
        
        var excelTemplate = CreateTestTemplate("Excel Template", "APT");
        excelTemplate.SupportedFormats = new List<string> { "xlsx", "csv" };
        
        await _service.CreateTemplateAsync(pdfTemplate);
        await _service.CreateTemplateAsync(wordTemplate);
        await _service.CreateTemplateAsync(excelTemplate);
        
        // Act
        var pdfResults = await _service.GetTemplatesByFormatAsync("pdf");
        var docxResults = await _service.GetTemplatesByFormatAsync("docx");
        
        // Assert
        pdfResults.Should().HaveCount(1);
        pdfResults.First().Name.Should().Be("PDF Template");
        
        docxResults.Should().HaveCount(2);
        docxResults.Should().Contain(t => t.Name == "PDF Template");
        docxResults.Should().Contain(t => t.Name == "Word Template");
    }

    [Fact]
    public async Task GetCategoriesAsync_ShouldReturnDistinctCategories()
    {
        // Arrange
        await _databaseService.InitializeDatabaseAsync();
        
        await _service.CreateTemplateAsync(CreateTestTemplate("Template 1", "Security"));
        await _service.CreateTemplateAsync(CreateTestTemplate("Template 2", "Reports"));
        await _service.CreateTemplateAsync(CreateTestTemplate("Template 3", "Security"));
        await _service.CreateTemplateAsync(CreateTestTemplate("Template 4", "APT"));
        
        // Act
        var categories = await _service.GetCategoriesAsync();
        
        // Assert
        categories.Should().HaveCount(3);
        categories.Should().Contain("Security");
        categories.Should().Contain("Reports");
        categories.Should().Contain("APT");
    }

    [Fact]
    public async Task GetCategoryStatisticsAsync_ShouldReturnCorrectCounts()
    {
        // Arrange
        await _databaseService.InitializeDatabaseAsync();
        
        await _service.CreateTemplateAsync(CreateTestTemplate("Template 1", "Security"));
        await _service.CreateTemplateAsync(CreateTestTemplate("Template 2", "Security"));
        await _service.CreateTemplateAsync(CreateTestTemplate("Template 3", "Reports"));
        await _service.CreateTemplateAsync(CreateTestTemplate("Template 4", "APT"));
        
        // Act
        var statistics = await _service.GetCategoryStatisticsAsync();
        
        // Assert
        statistics.Should().HaveCount(3);
        statistics["Security"].Should().Be(2);
        statistics["Reports"].Should().Be(1);
        statistics["APT"].Should().Be(1);
    }

    [Fact]
    public async Task ActivateTemplateAsync_WithInactiveTemplate_ShouldActivateSuccessfully()
    {
        // Arrange
        await _databaseService.InitializeDatabaseAsync();
        
        var template = CreateTestTemplate("Inactive Template", "Security");
        var created = await _service.CreateTemplateAsync(template);
        
        // Deactivate first
        await _service.DeactivateTemplateAsync(created.Id);
        
        // Act
        var result = await _service.ActivateTemplateAsync(created.Id);
        
        // Assert
        result.Should().NotBeNull();
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task DeactivateTemplateAsync_WithActiveTemplate_ShouldDeactivateSuccessfully()
    {
        // Arrange
        await _databaseService.InitializeDatabaseAsync();
        
        var template = CreateTestTemplate("Active Template", "Security");
        var created = await _service.CreateTemplateAsync(template);
        
        // Act
        var result = await _service.DeactivateTemplateAsync(created.Id);
        
        // Assert
        result.Should().NotBeNull();
        result.IsActive.Should().BeFalse();
    }

    private ImportTemplate CreateTestTemplate(string name, string category)
    {
        return new ImportTemplate
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = $"Test template for {category}",
            Category = category,
            Version = "1.0.0",
            CreatedBy = "Test User",
            LastModifiedBy = "Test User",
            CreatedAt = DateTime.UtcNow,
            LastModifiedAt = DateTime.UtcNow,
            IsActive = true,
            Tags = new List<string> { "test", category.ToLower() },
            SupportedFormats = new List<string> { "pdf", "docx" },
            Fields = new List<TemplateField>
            {
                new TemplateField
                {
                    Id = Guid.NewGuid(),
                    Name = "test_field_1",
                    DisplayName = "Test Field 1",
                    FieldType = TemplateFieldType.Text,
                    ExtractionMethod = ExtractionMethod.Text,
                    IsRequired = true,
                    ProcessingOrder = 1,
                    TextPatterns = new List<string> { "Pattern1", "Pattern2" },
                    Keywords = new List<string> { "keyword1", "keyword2" },
                    ExtractionZones = new List<ExtractionZone>
                    {
                        new ExtractionZone
                        {
                            Id = Guid.NewGuid(),
                            Name = "Zone 1",
                            X = 100,
                            Y = 200,
                            Width = 300,
                            Height = 50,
                            PageNumber = 1,
                            CoordinateSystem = CoordinateSystem.Pixel,
                            ZoneType = ExtractionZoneType.Text,
                            Priority = 1,
                            IsActive = true
                        }
                    }
                },
                new TemplateField
                {
                    Id = Guid.NewGuid(),
                    Name = "test_field_2",
                    DisplayName = "Test Field 2",
                    FieldType = TemplateFieldType.Date,
                    ExtractionMethod = ExtractionMethod.Text,
                    IsRequired = false,
                    ProcessingOrder = 2,
                    TextPatterns = new List<string> { "Date:", "Created:" },
                    Keywords = new List<string> { "date", "created" },
                    ExtractionZones = new List<ExtractionZone>()
                }
            },
            MatchingCriteria = new DocumentMatchingCriteria
            {
                RequiredKeywords = new List<string> { "security", "threat" },
                OptionalKeywords = new List<string> { "analysis", "report" },
                MinimumConfidence = 0.75,
                AutoApply = false
            },
            OcrSettings = new OcrSettings
            {
                Enabled = false,
                Language = "eng",
                ConfidenceThreshold = 60
            },
            Validation = new TemplateValidation
            {
                MinimumRequiredFields = 1,
                RequireAllRequiredFields = true
            },
            UsageStats = new TemplateUsageStats()
        };
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Clean up test database
                if (File.Exists(_testDatabasePath))
                {
                    try
                    {
                        File.Delete(_testDatabasePath);
                    }
                    catch
                    {
                        // Ignore cleanup errors in tests
                    }
                }
            }
            _disposed = true;
        }
    }
} 