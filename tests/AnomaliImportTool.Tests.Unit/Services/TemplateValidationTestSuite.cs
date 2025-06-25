using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AnomaliImportTool.Core.Interfaces;
using AnomaliImportTool.Core.Models;
using AnomaliImportTool.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace AnomaliImportTool.Tests.Unit.Services;

[TestClass]
public class TemplateValidationTestSuite
{
    private ServiceProvider? _serviceProvider;
    private IImportTemplateService? _templateService;
    private ITemplateMatchingService? _matchingService;
    private Mock<ILogger<ImportTemplateService>>? _mockLogger;
    private string _tempDatabasePath = string.Empty;

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
        _mockLogger = new Mock<ILogger<ImportTemplateService>>();
        services.AddSingleton(_mockLogger.Object);
        
        // Add services
        services.AddSingleton<IImportTemplateService, ImportTemplateService>();
        services.AddSingleton<ITemplateMatchingService, TemplateMatchingService>();

        // Configure database connection
        services.AddSingleton<SqliteConnection>(provider =>
        {
            var connection = new SqliteConnection($"Data Source={_tempDatabasePath}");
            connection.Open();
            return connection;
        });

        _serviceProvider = services.BuildServiceProvider();
        _templateService = _serviceProvider.GetRequiredService<IImportTemplateService>();
        _matchingService = _serviceProvider.GetRequiredService<ITemplateMatchingService>();

        // Initialize database schema
        await InitializeDatabaseSchemaAsync();
    }

    [TestCleanup]
    public async Task TestCleanup()
    {
        _serviceProvider?.Dispose();
        
        if (File.Exists(_tempDatabasePath))
            File.Delete(_tempDatabasePath);

        await Task.CompletedTask;
    }

    #region Template Name Validation Tests

    [TestMethod]
    public async Task TemplateValidation_EmptyName_ShouldRejectTemplate()
    {
        // Arrange
        var template = CreateValidTemplate();
        template.Name = string.Empty;

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _templateService!.CreateTemplateAsync(template));
        
        exception.Message.Should().Contain("Template name cannot be empty");
    }

    [TestMethod]
    public async Task TemplateValidation_NullName_ShouldRejectTemplate()
    {
        // Arrange
        var template = CreateValidTemplate();
        template.Name = null!;

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _templateService!.CreateTemplateAsync(template));
        
        exception.Message.Should().Contain("Template name cannot be empty");
    }

    [TestMethod]
    public async Task TemplateValidation_WhitespaceOnlyName_ShouldRejectTemplate()
    {
        // Arrange
        var template = CreateValidTemplate();
        template.Name = "   \t\n   ";

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _templateService!.CreateTemplateAsync(template));
        
        exception.Message.Should().Contain("Template name cannot be empty");
    }

    [TestMethod]
    public async Task TemplateValidation_ExcessivelyLongName_ShouldRejectTemplate()
    {
        // Arrange
        var template = CreateValidTemplate();
        template.Name = new string('A', 256); // Assuming 255 character limit

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _templateService!.CreateTemplateAsync(template));
        
        exception.Message.Should().Contain("Template name exceeds maximum length");
    }

    [TestMethod]
    public async Task TemplateValidation_InvalidCharactersInName_ShouldRejectTemplate()
    {
        // Arrange
        var template = CreateValidTemplate();
        template.Name = "Template<>:\"/\\|?*Name";

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _templateService!.CreateTemplateAsync(template));
        
        exception.Message.Should().Contain("Template name contains invalid characters");
    }

    [TestMethod]
    public async Task TemplateValidation_DuplicateName_ShouldRejectTemplate()
    {
        // Arrange
        var template1 = CreateValidTemplate();
        template1.Name = "Duplicate Template Name";
        await _templateService!.CreateTemplateAsync(template1);

        var template2 = CreateValidTemplate();
        template2.Name = "Duplicate Template Name";

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            () => _templateService.CreateTemplateAsync(template2));
        
        exception.Message.Should().Contain("Template with this name already exists");
    }

    #endregion

    #region Field Validation Tests

    [TestMethod]
    public async Task TemplateValidation_NoFields_ShouldRejectTemplate()
    {
        // Arrange
        var template = CreateValidTemplate();
        template.Fields.Clear();

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _templateService!.CreateTemplateAsync(template));
        
        exception.Message.Should().Contain("Template must have at least one field");
    }

    [TestMethod]
    public async Task TemplateValidation_NullFields_ShouldRejectTemplate()
    {
        // Arrange
        var template = CreateValidTemplate();
        template.Fields = null!;

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _templateService!.CreateTemplateAsync(template));
        
        exception.Message.Should().Contain("Template must have at least one field");
    }

    [TestMethod]
    public async Task TemplateValidation_DuplicateFieldNames_ShouldRejectTemplate()
    {
        // Arrange
        var template = CreateValidTemplate();
        template.Fields.Add(new TemplateField 
        { 
            Name = "Field1", 
            DisplayName = "Field 1", 
            FieldType = FieldType.Text 
        });
        template.Fields.Add(new TemplateField 
        { 
            Name = "Field1", // Duplicate name
            DisplayName = "Field 1 Duplicate", 
            FieldType = FieldType.Number 
        });

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _templateService!.CreateTemplateAsync(template));
        
        exception.Message.Should().Contain("Duplicate field names are not allowed");
    }

    [TestMethod]
    public async Task TemplateValidation_InvalidFieldName_ShouldRejectTemplate()
    {
        // Arrange
        var template = CreateValidTemplate();
        template.Fields[0].Name = "Field With Spaces";

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _templateService!.CreateTemplateAsync(template));
        
        exception.Message.Should().Contain("Field names must follow naming conventions");
    }

    [TestMethod]
    public async Task TemplateValidation_EmptyFieldName_ShouldRejectTemplate()
    {
        // Arrange
        var template = CreateValidTemplate();
        template.Fields[0].Name = string.Empty;

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _templateService!.CreateTemplateAsync(template));
        
        exception.Message.Should().Contain("Field name cannot be empty");
    }

    [TestMethod]
    public async Task TemplateValidation_ExcessiveFields_ShouldRejectTemplate()
    {
        // Arrange
        var template = CreateValidTemplate();
        template.Fields.Clear();
        
        // Add more than maximum allowed fields (assuming 100 is the limit)
        for (int i = 0; i < 101; i++)
        {
            template.Fields.Add(new TemplateField 
            { 
                Name = $"Field{i}", 
                DisplayName = $"Field {i}", 
                FieldType = FieldType.Text 
            });
        }

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _templateService!.CreateTemplateAsync(template));
        
        exception.Message.Should().Contain("Template exceeds maximum number of fields");
    }

    #endregion

    #region Field Type Validation Tests

    [TestMethod]
    public async Task TemplateValidation_DropdownFieldWithoutOptions_ShouldRejectTemplate()
    {
        // Arrange
        var template = CreateValidTemplate();
        template.Fields.Add(new TemplateField 
        { 
            Name = "DropdownField", 
            DisplayName = "Dropdown Field", 
            FieldType = FieldType.DropdownList,
            Options = new List<string>() // Empty options
        });

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _templateService!.CreateTemplateAsync(template));
        
        exception.Message.Should().Contain("Dropdown fields must have at least one option");
    }

    [TestMethod]
    public async Task TemplateValidation_NumberFieldWithInvalidRange_ShouldRejectTemplate()
    {
        // Arrange
        var template = CreateValidTemplate();
        template.Fields.Add(new TemplateField 
        { 
            Name = "NumberField", 
            DisplayName = "Number Field", 
            FieldType = FieldType.Number,
            MinValue = 100,
            MaxValue = 50 // Max less than min
        });

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _templateService!.CreateTemplateAsync(template));
        
        exception.Message.Should().Contain("Maximum value must be greater than minimum value");
    }

    [TestMethod]
    public async Task TemplateValidation_EmailFieldWithInvalidPattern_ShouldRejectTemplate()
    {
        // Arrange
        var template = CreateValidTemplate();
        template.Fields.Add(new TemplateField 
        { 
            Name = "EmailField", 
            DisplayName = "Email Field", 
            FieldType = FieldType.Email,
            ValidationPattern = "invalid-regex-[" // Invalid regex
        });

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _templateService!.CreateTemplateAsync(template));
        
        exception.Message.Should().Contain("Invalid validation pattern");
    }

    #endregion

    #region Extraction Rule Validation Tests

    [TestMethod]
    public async Task TemplateValidation_InvalidRegexPattern_ShouldRejectTemplate()
    {
        // Arrange
        var template = CreateValidTemplate();
        template.Fields[0].ExtractionRules.Add(new ExtractionRule
        {
            RuleType = ExtractionRuleType.RegexPattern,
            Pattern = "[invalid-regex" // Missing closing bracket
        });

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _templateService!.CreateTemplateAsync(template));
        
        exception.Message.Should().Contain("Invalid regex pattern");
    }

    [TestMethod]
    public async Task TemplateValidation_EmptyExtractionPattern_ShouldRejectTemplate()
    {
        // Arrange
        var template = CreateValidTemplate();
        template.Fields[0].ExtractionRules.Add(new ExtractionRule
        {
            RuleType = ExtractionRuleType.RegexPattern,
            Pattern = string.Empty
        });

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _templateService!.CreateTemplateAsync(template));
        
        exception.Message.Should().Contain("Extraction pattern cannot be empty");
    }

    [TestMethod]
    public async Task TemplateValidation_ConflictingExtractionRules_ShouldRejectTemplate()
    {
        // Arrange
        var template = CreateValidTemplate();
        template.Fields[0].ExtractionRules.Add(new ExtractionRule
        {
            RuleType = ExtractionRuleType.RegexPattern,
            Pattern = @"\d+",
            Priority = 1
        });
        template.Fields[0].ExtractionRules.Add(new ExtractionRule
        {
            RuleType = ExtractionRuleType.KeywordSearch,
            Pattern = "123",
            Priority = 1 // Same priority
        });

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _templateService!.CreateTemplateAsync(template));
        
        exception.Message.Should().Contain("Extraction rules with same priority may cause conflicts");
    }

    #endregion

    #region Extraction Zone Validation Tests

    [TestMethod]
    public async Task TemplateValidation_InvalidExtractionZoneCoordinates_ShouldRejectTemplate()
    {
        // Arrange
        var template = CreateValidTemplate();
        template.Fields[0].ExtractionZones.Add(new ExtractionZone
        {
            X = -10, // Negative coordinate
            Y = -5,
            Width = 100,
            Height = 50
        });

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _templateService!.CreateTemplateAsync(template));
        
        exception.Message.Should().Contain("Extraction zone coordinates must be non-negative");
    }

    [TestMethod]
    public async Task TemplateValidation_ZeroSizeExtractionZone_ShouldRejectTemplate()
    {
        // Arrange
        var template = CreateValidTemplate();
        template.Fields[0].ExtractionZones.Add(new ExtractionZone
        {
            X = 10,
            Y = 10,
            Width = 0, // Zero width
            Height = 0  // Zero height
        });

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _templateService!.CreateTemplateAsync(template));
        
        exception.Message.Should().Contain("Extraction zone must have positive dimensions");
    }

    [TestMethod]
    public async Task TemplateValidation_OverlappingExtractionZones_ShouldWarnButAllow()
    {
        // Arrange
        var template = CreateValidTemplate();
        template.Fields[0].ExtractionZones.Add(new ExtractionZone
        {
            X = 10, Y = 10, Width = 100, Height = 50
        });
        template.Fields[0].ExtractionZones.Add(new ExtractionZone
        {
            X = 50, Y = 30, Width = 100, Height = 50 // Overlapping
        });

        // Act
        var result = await _templateService!.CreateTemplateAsync(template);

        // Assert
        result.Should().NotBeNull("Template should be created despite overlapping zones");
        // Verify warning was logged
        _mockLogger!.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("overlapping extraction zones")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region Supported Format Validation Tests

    [TestMethod]
    public async Task TemplateValidation_NoSupportedFormats_ShouldRejectTemplate()
    {
        // Arrange
        var template = CreateValidTemplate();
        template.SupportedFormats.Clear();

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _templateService!.CreateTemplateAsync(template));
        
        exception.Message.Should().Contain("Template must support at least one format");
    }

    [TestMethod]
    public async Task TemplateValidation_InvalidSupportedFormat_ShouldRejectTemplate()
    {
        // Arrange
        var template = CreateValidTemplate();
        template.SupportedFormats.Add("invalid-format");

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _templateService!.CreateTemplateAsync(template));
        
        exception.Message.Should().Contain("Unsupported format: invalid-format");
    }

    [TestMethod]
    public async Task TemplateValidation_DuplicateSupportedFormats_ShouldNormalize()
    {
        // Arrange
        var template = CreateValidTemplate();
        template.SupportedFormats.Add("pdf");
        template.SupportedFormats.Add("PDF"); // Duplicate in different case

        // Act
        var result = await _templateService!.CreateTemplateAsync(template);
        var retrieved = await _templateService.GetTemplateByIdAsync(result.Id);

        // Assert
        retrieved!.SupportedFormats.Should().ContainSingle("pdf");
    }

    #endregion

    #region Complex Validation Scenarios

    [TestMethod]
    public async Task TemplateValidation_CircularInheritance_ShouldRejectTemplate()
    {
        // Arrange
        var parentTemplate = CreateValidTemplate();
        parentTemplate.Name = "Parent Template";
        var parent = await _templateService!.CreateTemplateAsync(parentTemplate);

        var childTemplate = CreateValidTemplate();
        childTemplate.Name = "Child Template";
        childTemplate.ParentTemplateId = parent.Id;
        var child = await _templateService.CreateTemplateAsync(childTemplate);

        // Try to create circular reference
        parent.ParentTemplateId = child.Id;

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            () => _templateService.UpdateTemplateAsync(parent));
        
        exception.Message.Should().Contain("Circular inheritance detected");
    }

    [TestMethod]
    public async Task TemplateValidation_ExcessiveInheritanceDepth_ShouldRejectTemplate()
    {
        // Arrange - Create a chain of inheritance that's too deep
        ImportTemplate? previousTemplate = null;
        
        for (int i = 0; i < 11; i++) // Assuming max depth is 10
        {
            var template = CreateValidTemplate();
            template.Name = $"Template Level {i}";
            if (previousTemplate != null)
            {
                template.ParentTemplateId = previousTemplate.Id;
            }
            
            if (i < 10)
            {
                previousTemplate = await _templateService!.CreateTemplateAsync(template);
            }
            else
            {
                // This should fail due to excessive depth
                var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                    () => _templateService!.CreateTemplateAsync(template));
                
                exception.Message.Should().Contain("Inheritance depth exceeds maximum allowed");
                return;
            }
        }
        
        Assert.Fail("Should have thrown exception for excessive inheritance depth");
    }

    [TestMethod]
    public async Task TemplateValidation_ConditionalRuleWithInvalidCondition_ShouldRejectTemplate()
    {
        // Arrange
        var template = CreateValidTemplate();
        template.Fields[0].ConditionalRules.Add(new ConditionalExtractionRule
        {
            Condition = "invalid javascript condition {", // Invalid JavaScript
            Action = ConditionalAction.ExtractField,
            TargetFieldName = "ConditionalField"
        });

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _templateService!.CreateTemplateAsync(template));
        
        exception.Message.Should().Contain("Invalid conditional rule syntax");
    }

    [TestMethod]
    public async Task TemplateValidation_FieldWithBothZoneAndRegexRules_ShouldWarnButAllow()
    {
        // Arrange
        var template = CreateValidTemplate();
        template.Fields[0].ExtractionZones.Add(new ExtractionZone { X = 10, Y = 10, Width = 100, Height = 50 });
        template.Fields[0].ExtractionRules.Add(new ExtractionRule 
        { 
            RuleType = ExtractionRuleType.RegexPattern, 
            Pattern = @"\d+" 
        });

        // Act
        var result = await _templateService!.CreateTemplateAsync(template);

        // Assert
        result.Should().NotBeNull("Template should be created with both zone and regex rules");
        // Verify warning was logged about potential conflicts
        _mockLogger!.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("both extraction zones and regex rules")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region Edge Case Tests

    [TestMethod]
    public async Task TemplateValidation_UnicodeCharactersInFields_ShouldAccept()
    {
        // Arrange
        var template = CreateValidTemplate();
        template.Name = "Шаблон с Unicode символами";
        template.Fields[0].DisplayName = "Поле с émojis 🚀";

        // Act
        var result = await _templateService!.CreateTemplateAsync(template);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Шаблон с Unicode символами");
        result.Fields[0].DisplayName.Should().Be("Поле с émojis 🚀");
    }

    [TestMethod]
    public async Task TemplateValidation_VeryLargeTemplate_ShouldHandleGracefully()
    {
        // Arrange
        var template = CreateValidTemplate();
        template.Description = new string('A', 10000); // Very long description
        
        // Add many fields
        for (int i = 0; i < 50; i++)
        {
            template.Fields.Add(new TemplateField
            {
                Name = $"Field{i}",
                DisplayName = $"Field {i}",
                FieldType = FieldType.Text,
                Description = new string('B', 1000) // Long field descriptions
            });
        }

        // Act & Assert
        var result = await _templateService!.CreateTemplateAsync(template);
        result.Should().NotBeNull("Large template should be handled gracefully");
    }

    [TestMethod]
    public async Task TemplateValidation_NullOptionalFields_ShouldAccept()
    {
        // Arrange
        var template = new ImportTemplate
        {
            Id = Guid.NewGuid(),
            Name = "Minimal Template",
            Description = null, // Null optional field
            Category = null,    // Null optional field
            SupportedFormats = new List<string> { "pdf" },
            Fields = new List<TemplateField>
            {
                new TemplateField 
                { 
                    Name = "Field1", 
                    DisplayName = "Field 1", 
                    FieldType = FieldType.Text,
                    Description = null // Null optional field
                }
            },
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test-user"
        };

        // Act
        var result = await _templateService!.CreateTemplateAsync(template);

        // Assert
        result.Should().NotBeNull("Template with null optional fields should be accepted");
    }

    [TestMethod]
    public async Task TemplateValidation_ConcurrentValidation_ShouldHandleRaceConditions()
    {
        // Arrange
        var templates = new List<ImportTemplate>();
        for (int i = 0; i < 10; i++)
        {
            var template = CreateValidTemplate();
            template.Name = $"Concurrent Template {i}";
            templates.Add(template);
        }

        // Act - Create templates concurrently
        var tasks = templates.Select(t => _templateService!.CreateTemplateAsync(t));
        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(10);
        results.Should().OnlyContain(r => r != null, "All templates should be created successfully");
        
        var names = results.Select(r => r.Name).ToList();
        names.Should().OnlyHaveUniqueItems("All template names should be unique");
    }

    #endregion

    #region Performance Validation Tests

    [TestMethod]
    public async Task TemplateValidation_LargeScaleValidation_ShouldCompleteInReasonableTime()
    {
        // Arrange
        var templates = new List<ImportTemplate>();
        for (int i = 0; i < 100; i++)
        {
            var template = CreateValidTemplate();
            template.Name = $"Performance Test Template {i}";
            templates.Add(template);
        }

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var tasks = templates.Select(t => _templateService!.CreateTemplateAsync(t));
        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(10000, "100 template validations should complete in under 10 seconds");
    }

    #endregion

    // Helper methods
    private ImportTemplate CreateValidTemplate()
    {
        return new ImportTemplate
        {
            Id = Guid.NewGuid(),
            Name = "Valid Test Template",
            Description = "A valid template for testing",
            Category = "Testing",
            SupportedFormats = new List<string> { "pdf", "docx" },
            Fields = new List<TemplateField>
            {
                new TemplateField 
                { 
                    Name = "Field1", 
                    DisplayName = "Field 1", 
                    FieldType = FieldType.Text,
                    IsRequired = true,
                    ExtractionRules = new List<ExtractionRule>(),
                    ExtractionZones = new List<ExtractionZone>(),
                    ConditionalRules = new List<ConditionalExtractionRule>()
                }
            },
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test-user",
            ExtractionRules = new List<ExtractionRule>(),
            ConditionalRules = new List<ConditionalExtractionRule>()
        };
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

// Additional classes for validation testing
public enum ExtractionRuleType
{
    RegexPattern,
    KeywordSearch,
    CoordinateZone
}

public enum ConditionalAction
{
    ExtractField,
    SkipField,
    SetDefaultValue
}

public class ExtractionRule
{
    public ExtractionRuleType RuleType { get; set; }
    public string Pattern { get; set; } = string.Empty;
    public int Priority { get; set; }
}

public class ConditionalExtractionRule
{
    public string Condition { get; set; } = string.Empty;
    public ConditionalAction Action { get; set; }
    public string TargetFieldName { get; set; } = string.Empty;
} 