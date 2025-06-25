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

    [Fact]
    public async Task ExportTemplateAsync_WithValidTemplate_ShouldReturnJsonString()
    {
        // Arrange
        await _databaseService.InitializeDatabaseAsync();
        var template = CreateTestTemplate("Export Template", "Security");
        var created = await _service.CreateTemplateAsync(template);
        
        // Act
        var result = await _service.ExportTemplateAsync(created.Id);
        
        // Assert
        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain("Export Template");
        result.Should().Contain("Security");
    }

    [Fact]
    public async Task ImportTemplateAsync_WithValidJson_ShouldImportSuccessfully()
    {
        // Arrange
        await _databaseService.InitializeDatabaseAsync();
        var originalTemplate = CreateTestTemplate("Original Template", "Security");
        var created = await _service.CreateTemplateAsync(originalTemplate);
        var exportedJson = await _service.ExportTemplateAsync(created.Id);
        
        // Act
        var importOptions = new TemplateImportOptions { AssignNewIds = true };
        var imported = await _service.ImportTemplateAsync(exportedJson, importOptions);
        
        // Assert
        imported.Should().NotBeNull();
        imported.Name.Should().Be("Original Template");
        imported.Category.Should().Be("Security");
        imported.Id.Should().NotBe(created.Id); // New ID assigned
    }

    [Fact]
    public async Task CreateTemplateVersionAsync_WithExistingTemplate_ShouldCreateNewVersion()
    {
        // Arrange
        await _databaseService.InitializeDatabaseAsync();
        var template = CreateTestTemplate("Versioned Template", "Security");
        var created = await _service.CreateTemplateAsync(template);
        
        // Act
        var newVersion = await _service.CreateTemplateVersionAsync(created.Id, "2.0");
        
        // Assert
        newVersion.Should().NotBeNull();
        newVersion.Version.Should().Be("2.0");
        newVersion.Name.Should().Be("Versioned Template");
        newVersion.Id.Should().NotBe(created.Id); // Different ID for new version
    }

    [Fact]
    public async Task GetTemplateVersionsAsync_WithMultipleVersions_ShouldReturnAllVersions()
    {
        // Arrange
        await _databaseService.InitializeDatabaseAsync();
        var template = CreateTestTemplate("Multi Version Template", "Security");
        var v1 = await _service.CreateTemplateAsync(template);
        var v2 = await _service.CreateTemplateVersionAsync(v1.Id, "2.0");
        var v3 = await _service.CreateTemplateVersionAsync(v1.Id, "3.0");
        
        // Act
        var versions = await _service.GetTemplateVersionsAsync("Multi Version Template");
        
        // Assert
        versions.Should().HaveCount(3);
        versions.Should().Contain(t => t.Version == "1.0");
        versions.Should().Contain(t => t.Version == "2.0");
        versions.Should().Contain(t => t.Version == "3.0");
    }

    [Fact]
    public async Task DuplicateTemplateAsync_WithExistingTemplate_ShouldCreateDuplicate()
    {
        // Arrange
        await _databaseService.InitializeDatabaseAsync();
        var template = CreateTestTemplate("Original Template", "Security");
        var created = await _service.CreateTemplateAsync(template);
        
        // Act
        var duplicated = await _service.DuplicateTemplateAsync(created.Id, "Duplicated Template");
        
        // Assert
        duplicated.Should().NotBeNull();
        duplicated.Name.Should().Be("Duplicated Template");
        duplicated.Category.Should().Be("Security");
        duplicated.Id.Should().NotBe(created.Id);
        duplicated.Fields.Should().HaveCount(created.Fields.Count);
    }

    [Fact]
    public async Task UpdateUsageStatisticsAsync_ShouldUpdateStatistics()
    {
        // Arrange
        await _databaseService.InitializeDatabaseAsync();
        var template = CreateTestTemplate("Usage Template", "Security");
        var created = await _service.CreateTemplateAsync(template);
        
        // Act
        await _service.UpdateUsageStatisticsAsync(created.Id, true, TimeSpan.FromSeconds(2.5));
        await _service.UpdateUsageStatisticsAsync(created.Id, true, TimeSpan.FromSeconds(1.8));
        await _service.UpdateUsageStatisticsAsync(created.Id, false, TimeSpan.FromSeconds(5.0));
        
        // Get updated statistics
        var stats = await _service.GetUsageStatisticsAsync(created.Id);
        
        // Assert
        stats.Should().NotBeNull();
        stats.TotalUsageCount.Should().Be(3);
        stats.SuccessfulUsageCount.Should().Be(2);
        stats.FailureCount.Should().Be(1);
        stats.AverageExtractionTime.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task TestTemplateAsync_WithValidDocument_ShouldReturnTestResult()
    {
        // Arrange
        await _databaseService.InitializeDatabaseAsync();
        var template = CreateTestTemplate("Test Template", "Security");
        var created = await _service.CreateTemplateAsync(template);
        
        // Create a temporary test file
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, "Test document content with sample data");
        
        try
        {
            // Act
            var result = await _service.TestTemplateAsync(created.Id, tempFile);
            
            // Assert
            result.Should().NotBeNull();
            result.ExtractedFields.Should().NotBeNull();
            result.FieldConfidenceScores.Should().NotBeNull();
            result.ExtractionTime.Should().BeGreaterThan(TimeSpan.Zero);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ValidateTemplateAsync_WithValidTemplate_ShouldReturnValidResult()
    {
        // Arrange
        await _databaseService.InitializeDatabaseAsync();
        var template = CreateTestTemplate("Valid Template", "Security");
        
        // Act
        var result = await _service.ValidateTemplateAsync(template);
        
        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateTemplateAsync_WithInvalidTemplate_ShouldReturnInvalidResult()
    {
        // Arrange
        await _databaseService.InitializeDatabaseAsync();
        var invalidTemplate = new ImportTemplate
        {
            Name = "", // Invalid - empty name
            SupportedFormats = new List<string>() // Invalid - no formats
        };
        
        // Act
        var result = await _service.ValidateTemplateAsync(invalidTemplate);
        
        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetMostUsedTemplatesAsync_ShouldReturnTemplatesByUsage()
    {
        // Arrange
        await _databaseService.InitializeDatabaseAsync();
        
        var template1 = CreateTestTemplate("Template 1", "Security");
        var template2 = CreateTestTemplate("Template 2", "Reports");
        var created1 = await _service.CreateTemplateAsync(template1);
        var created2 = await _service.CreateTemplateAsync(template2);
        
        // Simulate usage - template1 used more
        await _service.UpdateUsageStatisticsAsync(created1.Id, true, TimeSpan.FromSeconds(1));
        await _service.UpdateUsageStatisticsAsync(created1.Id, true, TimeSpan.FromSeconds(1));
        await _service.UpdateUsageStatisticsAsync(created2.Id, true, TimeSpan.FromSeconds(1));
        
        // Act
        var mostUsed = await _service.GetMostUsedTemplatesAsync(2);
        
        // Assert
        mostUsed.Should().NotBeEmpty();
        var mostUsedList = mostUsed.ToList();
        mostUsedList.First().Id.Should().Be(created1.Id); // Most used should be first
    }

    [Fact]
    public async Task ExportTemplatesAsync_WithMultipleTemplates_ShouldExportAll()
    {
        // Arrange
        await _databaseService.InitializeDatabaseAsync();
        
        var template1 = CreateTestTemplate("Export Template 1", "Security");
        var template2 = CreateTestTemplate("Export Template 2", "Reports");
        var created1 = await _service.CreateTemplateAsync(template1);
        var created2 = await _service.CreateTemplateAsync(template2);
        
        var templateIds = new[] { created1.Id, created2.Id };
        
        // Act
        var result = await _service.ExportTemplatesAsync(templateIds);
        
        // Assert
        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain("Export Template 1");
        result.Should().Contain("Export Template 2");
    }

    [Fact]
    public async Task RollbackToVersionAsync_WithValidVersion_ShouldRollback()
    {
        // Arrange
        await _databaseService.InitializeDatabaseAsync();
        
        var template = CreateTestTemplate("Rollback Template", "Security");
        var v1 = await _service.CreateTemplateAsync(template);
        
        // Create version 2 with changes
        var v2Template = await _service.GetTemplateAsync(v1.Id);
        v2Template!.Description = "Modified description";
        var v2 = await _service.CreateTemplateVersionAsync(v1.Id, "2.0");
        await _service.UpdateTemplateAsync(v2);
        
        // Act - rollback to version 1.0
        var rolledBack = await _service.RollbackToVersionAsync(v2.Id, "1.0", "Test rollback");
        
        // Assert
        rolledBack.Should().NotBeNull();
        rolledBack.Description.Should().NotBe("Modified description");
    }

    [Fact]
    public async Task GetTemplateChangeHistoryAsync_ShouldReturnChangeHistory()
    {
        // Arrange
        await _databaseService.InitializeDatabaseAsync();
        
        var template = CreateTestTemplate("History Template", "Security");
        var created = await _service.CreateTemplateAsync(template);
        
        // Make some changes
        created.Description = "Updated description";
        await _service.UpdateTemplateAsync(created);
        
        await _service.CreateTemplateVersionAsync(created.Id, "2.0");
        
        // Act
        var history = await _service.GetTemplateChangeHistoryAsync(created.Id);
        
        // Assert
        history.Should().NotBeEmpty();
        history.Should().HaveCountGreaterOrEqualTo(1);
    }

    [Fact]
    public async Task CreateInheritanceAsync_WithValidTemplates_ShouldCreateInheritanceRelationship()
    {
        // Arrange
        await _databaseService.InitializeDatabaseAsync();
        
        var parentTemplate = CreateTestTemplate("Parent Template", "Security");
        var childTemplate = CreateTestTemplate("Child Template", "Security");
        var parent = await _service.CreateTemplateAsync(parentTemplate);
        var child = await _service.CreateTemplateAsync(childTemplate);
        
        var config = new TemplateInheritanceConfig
        {
            InheritanceType = InheritanceType.Full,
            AllowOverrides = true
        };
        
        // Act
        var relationship = await _service.CreateInheritanceAsync(child.Id, parent.Id, config);
        
        // Assert
        relationship.Should().NotBeNull();
        relationship.ChildTemplateId.Should().Be(child.Id);
        relationship.ParentTemplateId.Should().Be(parent.Id);
        relationship.InheritanceConfig.InheritanceType.Should().Be(InheritanceType.Full);
    }

    [Fact]
    public async Task GetTemplateInheritanceAsync_WithInheritedTemplate_ShouldReturnRelationships()
    {
        // Arrange
        await _databaseService.InitializeDatabaseAsync();
        
        var parentTemplate = CreateTestTemplate("Parent Template", "Security");
        var childTemplate = CreateTestTemplate("Child Template", "Security");
        var parent = await _service.CreateTemplateAsync(parentTemplate);
        var child = await _service.CreateTemplateAsync(childTemplate);
        
        var config = new TemplateInheritanceConfig { InheritanceType = InheritanceType.Full };
        await _service.CreateInheritanceAsync(child.Id, parent.Id, config);
        
        // Act
        var relationships = await _service.GetTemplateInheritanceAsync(child.Id);
        
        // Assert
        relationships.Should().HaveCount(1);
        relationships[0].ParentTemplateId.Should().Be(parent.Id);
    }

    [Fact]
    public async Task ValidateInheritanceAsync_WithCircularReference_ShouldReturnFalse()
    {
        // Arrange
        await _databaseService.InitializeDatabaseAsync();
        
        var template1 = CreateTestTemplate("Template 1", "Security");
        var template2 = CreateTestTemplate("Template 2", "Security");
        var created1 = await _service.CreateTemplateAsync(template1);
        var created2 = await _service.CreateTemplateAsync(template2);
        
        var config = new TemplateInheritanceConfig { InheritanceType = InheritanceType.Full };
        await _service.CreateInheritanceAsync(created2.Id, created1.Id, config);
        
        // Act - try to create circular reference
        var isValid = await _service.ValidateInheritanceAsync(created1.Id, created2.Id);
        
        // Assert
        isValid.Should().BeFalse(); // Should detect circular reference
    }

    [Fact]
    public async Task ResolveTemplateInheritanceAsync_WithInheritedTemplate_ShouldReturnResolvedTemplate()
    {
        // Arrange
        await _databaseService.InitializeDatabaseAsync();
        
        var parentTemplate = CreateTestTemplate("Parent Template", "Security");
        parentTemplate.Description = "Parent description";
        var childTemplate = CreateTestTemplate("Child Template", "Security");
        childTemplate.Description = "Child description";
        
        var parent = await _service.CreateTemplateAsync(parentTemplate);
        var child = await _service.CreateTemplateAsync(childTemplate);
        
        var config = new TemplateInheritanceConfig 
        { 
            InheritanceType = InheritanceType.FieldsOnly,
            AllowOverrides = true 
        };
        await _service.CreateInheritanceAsync(child.Id, parent.Id, config);
        
        // Act
        var resolved = await _service.ResolveTemplateInheritanceAsync(child.Id);
        
        // Assert
        resolved.Should().NotBeNull();
        resolved.ResolvedTemplate.Should().NotBeNull();
        resolved.InheritanceChain.Should().NotBeEmpty();
        resolved.PropertySources.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetAvailableParentTemplatesAsync_ShouldReturnValidParents()
    {
        // Arrange
        await _databaseService.InitializeDatabaseAsync();
        
        var template1 = CreateTestTemplate("Template 1", "Security");
        var template2 = CreateTestTemplate("Template 2", "Security");
        var template3 = CreateTestTemplate("Template 3", "Security");
        
        var created1 = await _service.CreateTemplateAsync(template1);
        var created2 = await _service.CreateTemplateAsync(template2);
        var created3 = await _service.CreateTemplateAsync(template3);
        
        // Create inheritance: template2 inherits from template1
        var config = new TemplateInheritanceConfig { InheritanceType = InheritanceType.Full };
        await _service.CreateInheritanceAsync(created2.Id, created1.Id, config);
        
        // Act - get available parents for template3
        var availableParents = await _service.GetAvailableParentTemplatesAsync(created3.Id);
        
        // Assert
        availableParents.Should().NotBeEmpty();
        availableParents.Should().Contain(t => t.Id == created1.Id);
        availableParents.Should().Contain(t => t.Id == created2.Id);
        availableParents.Should().NotContain(t => t.Id == created3.Id); // Can't inherit from itself
    }

    [Fact]
    public async Task CompareTemplateVersionsAsync_WithDifferentVersions_ShouldReturnComparison()
    {
        // Arrange
        await _databaseService.InitializeDatabaseAsync();
        
        var template = CreateTestTemplate("Compare Template", "Security");
        var v1 = await _service.CreateTemplateAsync(template);
        
        // Create version 2 with changes
        var v2Template = await _service.GetTemplateAsync(v1.Id);
        v2Template!.Description = "Modified description for v2";
        var v2 = await _service.CreateTemplateVersionAsync(v1.Id, "2.0");
        
        // Act
        var comparison = await _service.CompareTemplateVersionsAsync(v1.Id, "1.0", "2.0");
        
        // Assert
        comparison.Should().NotBeNull();
        comparison.TemplateId.Should().Be(v1.Id);
        comparison.Version1.Should().Be("1.0");
        comparison.Version2.Should().Be("2.0");
        comparison.HasDifferences.Should().BeTrue();
    }

    [Fact]
    public async Task ExportTemplatesToFileAsync_WithValidTemplates_ShouldCreateFile()
    {
        // Arrange
        await _databaseService.InitializeDatabaseAsync();
        
        var template1 = CreateTestTemplate("File Export Template 1", "Security");
        var template2 = CreateTestTemplate("File Export Template 2", "Reports");
        var created1 = await _service.CreateTemplateAsync(template1);
        var created2 = await _service.CreateTemplateAsync(template2);
        
        var templateIds = new[] { created1.Id, created2.Id };
        var tempFilePath = Path.GetTempFileName();
        
        try
        {
            // Act
            var result = await _service.ExportTemplatesToFileAsync(templateIds, tempFilePath);
            
            // Assert
            result.Should().NotBeNull();
            result.IsSuccessful.Should().BeTrue();
            result.ExportedCount.Should().Be(2);
            result.FilePath.Should().Be(tempFilePath);
            File.Exists(tempFilePath).Should().BeTrue();
            
            var fileContent = await File.ReadAllTextAsync(tempFilePath);
            fileContent.Should().Contain("File Export Template 1");
            fileContent.Should().Contain("File Export Template 2");
        }
        finally
        {
            if (File.Exists(tempFilePath))
                File.Delete(tempFilePath);
        }
    }

    [Fact]
    public async Task ImportTemplatesFromFileAsync_WithValidFile_ShouldImportTemplates()
    {
        // Arrange
        await _databaseService.InitializeDatabaseAsync();
        
        // First export some templates
        var template1 = CreateTestTemplate("Import Template 1", "Security");
        var template2 = CreateTestTemplate("Import Template 2", "Reports");
        var created1 = await _service.CreateTemplateAsync(template1);
        var created2 = await _service.CreateTemplateAsync(template2);
        
        var templateIds = new[] { created1.Id, created2.Id };
        var tempFilePath = Path.GetTempFileName();
        
        try
        {
            await _service.ExportTemplatesToFileAsync(templateIds, tempFilePath);
            
            // Delete the templates from DB to test import
            await _service.DeleteTemplateAsync(created1.Id);
            await _service.DeleteTemplateAsync(created2.Id);
            
            var importOptions = new TemplateImportOptions { AssignNewIds = true };
            
            // Act
            var result = await _service.ImportTemplatesFromFileAsync(tempFilePath, importOptions);
            
            // Assert
            result.Should().NotBeNull();
            result.IsSuccessful.Should().BeTrue();
            result.SuccessfulCount.Should().Be(2);
            result.ImportedTemplates.Should().HaveCount(2);
        }
        finally
        {
            if (File.Exists(tempFilePath))
                File.Delete(tempFilePath);
        }
    }

    [Fact]
    public async Task ValidateImportAsync_WithValidJson_ShouldReturnValidResult()
    {
        // Arrange
        await _databaseService.InitializeDatabaseAsync();
        
        var template = CreateTestTemplate("Validation Template", "Security");
        var created = await _service.CreateTemplateAsync(template);
        var exportedJson = await _service.ExportTemplateAsync(created.Id);
        
        // Act
        var result = await _service.ValidateImportAsync(exportedJson);
        
        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.ValidCount.Should().Be(1);
        result.InvalidCount.Should().Be(0);
        result.GeneralErrors.Should().BeEmpty();
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