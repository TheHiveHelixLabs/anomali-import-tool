using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
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
using System.Globalization;
using AnomaliImportTool.Infrastructure.DocumentProcessing;

namespace AnomaliImportTool.Tests.Integration;

[TestClass]
public class TemplatePerformanceBenchmarkTests
{
    private ServiceProvider? _serviceProvider;
    private IImportTemplateService? _templateService;
    private ITemplateMatchingService? _matchingService;
    private TemplateExtractionEngine? _extractionEngine;
    private string _tempDatabasePath = string.Empty;
    private readonly List<string> _tempFiles = new();

    [TestInitialize]
    public async Task TestInitialize()
    {
        // Create temporary database
        _tempDatabasePath = Path.GetTempFileName();
        File.Delete(_tempDatabasePath); // Delete the file so SQLite can create it properly
        _tempDatabasePath += ".db";

        // Set up service provider with real implementations
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging(builder => builder.AddConsole());
        
        // Add services
        services.AddSingleton<IImportTemplateService, ImportTemplateService>();
        services.AddSingleton<ITemplateMatchingService, TemplateMatchingService>();
        services.AddSingleton<TemplateExtractionEngine>();
        services.AddSingleton<DocumentProcessingService>();
        services.AddSingleton<PdfDocumentProcessor>();
        services.AddSingleton<WordDocumentProcessor>();
        services.AddSingleton<ExcelDocumentProcessor>();

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
        _extractionEngine = _serviceProvider.GetRequiredService<TemplateExtractionEngine>();

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

        // Dispose services
        _serviceProvider?.Dispose();

        // Clean up database
        if (File.Exists(_tempDatabasePath))
            File.Delete(_tempDatabasePath);

        await Task.CompletedTask;
    }

    [TestMethod]
    public async Task TemplateBenchmark_SingleDocumentExtraction_ShouldMeetPerformanceThresholds()
    {
        // Arrange
        var template = CreateSecurityReportTemplate();
        await _templateService!.CreateTemplateAsync(template);

        var testDocument = CreateTestDocumentWithKnownContent("security_report.pdf");
        var expectedResults = new Dictionary<string, string>
        {
            { "ThreatLevel", "HIGH" },
            { "Analyst", "John Doe" },
            { "ReportDate", "2024-01-15" }
        };

        // Act - Benchmark single document extraction
        var stopwatch = Stopwatch.StartNew();
        var results = await _extractionEngine!.ExtractFieldsAsync(template, testDocument);
        stopwatch.Stop();

        // Assert - Performance thresholds
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000, "Single document extraction should complete in under 2 seconds");
        
        // Accuracy verification
        results.Should().NotBeNull();
        results.ExtractedFields.Should().HaveCount(expectedResults.Count);
        
        foreach (var expected in expectedResults)
        {
            results.ExtractedFields.Should().ContainKey(expected.Key);
            results.ExtractedFields[expected.Key].Should().Be(expected.Value);
        }

        results.AccuracyScore.Should().BeGreaterThan(0.95, "Accuracy should be above 95%");
        results.ConfidenceScore.Should().BeGreaterThan(0.9, "Confidence should be above 90%");
    }

    [TestMethod]
    public async Task TemplateBenchmark_BatchProcessing_ShouldHandleLargeVolumes()
    {
        // Arrange
        var template = CreateFinancialReportTemplate();
        await _templateService!.CreateTemplateAsync(template);

        const int documentCount = 100;
        var testDocuments = new List<string>();
        
        for (int i = 0; i < documentCount; i++)
        {
            var document = CreateTestDocumentWithVariableContent($"financial_report_{i}.pdf", i);
            testDocuments.Add(document);
        }

        // Act - Benchmark batch processing
        var stopwatch = Stopwatch.StartNew();
        var results = new List<FieldExtractionResult>();
        var successCount = 0;
        var totalAccuracy = 0.0;

        var semaphore = new SemaphoreSlim(Environment.ProcessorCount, Environment.ProcessorCount);
        var tasks = testDocuments.Select(async document =>
        {
            await semaphore.WaitAsync();
            try
            {
                var result = await _extractionEngine!.ExtractFieldsAsync(template, document);
                lock (results)
                {
                    results.Add(result);
                    if (result.IsSuccessful)
                    {
                        successCount++;
                        totalAccuracy += result.AccuracyScore;
                    }
                }
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert - Performance and accuracy metrics
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(30000, "100 documents should process in under 30 seconds");
        
        var averageTimePerDocument = (double)stopwatch.ElapsedMilliseconds / documentCount;
        averageTimePerDocument.Should().BeLessThan(300, "Average processing time per document should be under 300ms");

        var successRate = (double)successCount / documentCount;
        successRate.Should().BeGreaterThan(0.95, "Success rate should be above 95%");

        var averageAccuracy = totalAccuracy / successCount;
        averageAccuracy.Should().BeGreaterThan(0.9, "Average accuracy should be above 90%");

        // Memory usage should be reasonable
        var process = Process.GetCurrentProcess();
        var memoryUsageMB = process.WorkingSet64 / (1024 * 1024);
        memoryUsageMB.Should().BeLessThan(500, "Memory usage should be under 500MB during batch processing");
    }

    [TestMethod]
    public async Task TemplateBenchmark_TemplateMatching_ShouldProvideRapidResults()
    {
        // Arrange
        var templates = new List<ImportTemplate>
        {
            CreateSecurityReportTemplate(),
            CreateFinancialReportTemplate(),
            CreateLegalDocumentTemplate(),
            CreateTechnicalReportTemplate(),
            CreateIncidentReportTemplate()
        };

        foreach (var template in templates)
        {
            await _templateService!.CreateTemplateAsync(template);
        }

        var testDocument = CreateTestDocumentWithKnownContent("security_report.pdf");

        // Act - Benchmark template matching
        var stopwatch = Stopwatch.StartNew();
        var matchingResults = await _matchingService!.FindBestMatchingTemplatesAsync(testDocument, maxResults: 3);
        stopwatch.Stop();

        // Assert - Performance thresholds for template matching
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000, "Template matching should complete in under 1 second");
        
        matchingResults.Should().NotBeNull();
        matchingResults.Should().NotBeEmpty("Should find at least one matching template");
        matchingResults.Should().BeInDescendingOrder(r => r.ConfidenceScore, "Results should be ordered by confidence");
        
        var bestMatch = matchingResults.First();
        bestMatch.Template.Name.Should().Be("Security Report Template");
        bestMatch.ConfidenceScore.Should().BeGreaterThan(0.8, "Best match should have high confidence");
    }

    [TestMethod]
    public async Task TemplateBenchmark_ExtractionAccuracy_ShouldMaintainHighAccuracy()
    {
        // Arrange
        var template = CreateComprehensiveTestTemplate();
        await _templateService!.CreateTemplateAsync(template);

        var testCases = new[]
        {
            new { Document = CreateTestDocumentWithPerfectData(), ExpectedAccuracy = 1.0 },
            new { Document = CreateTestDocumentWithMinorErrors(), ExpectedAccuracy = 0.9 },
            new { Document = CreateTestDocumentWithMissingFields(), ExpectedAccuracy = 0.7 },
            new { Document = CreateTestDocumentWithFormatVariations(), ExpectedAccuracy = 0.85 }
        };

        var accuracyResults = new List<double>();

        // Act - Test extraction accuracy across various scenarios
        foreach (var testCase in testCases)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = await _extractionEngine!.ExtractFieldsAsync(template, testCase.Document);
            stopwatch.Stop();

            // Performance check
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(3000, "Each extraction should complete in under 3 seconds");

            // Accuracy verification
            result.AccuracyScore.Should().BeGreaterOrEqualTo(testCase.ExpectedAccuracy - 0.1, 
                $"Accuracy should be within tolerance for test case");
            
            accuracyResults.Add(result.AccuracyScore);
        }

        // Assert - Overall accuracy metrics
        var overallAccuracy = accuracyResults.Average();
        overallAccuracy.Should().BeGreaterThan(0.85, "Overall accuracy should be above 85%");

        var minAccuracy = accuracyResults.Min();
        minAccuracy.Should().BeGreaterThan(0.6, "Even worst-case scenarios should maintain reasonable accuracy");
    }

    [TestMethod]
    public async Task TemplateBenchmark_ConcurrentOperations_ShouldHandleHighConcurrency()
    {
        // Arrange
        var template = CreateSecurityReportTemplate();
        await _templateService!.CreateTemplateAsync(template);

        const int concurrentOperations = 50;
        var testDocument = CreateTestDocumentWithKnownContent("security_report.pdf");

        // Act - Benchmark concurrent extractions
        var stopwatch = Stopwatch.StartNew();
        var tasks = new List<Task<FieldExtractionResult>>();

        for (int i = 0; i < concurrentOperations; i++)
        {
            tasks.Add(_extractionEngine!.ExtractFieldsAsync(template, testDocument));
        }

        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert - Concurrency performance
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(15000, "50 concurrent operations should complete in under 15 seconds");
        
        results.Should().HaveCount(concurrentOperations);
        results.Should().OnlyContain(r => r.IsSuccessful, "All concurrent operations should succeed");
        
        var averageAccuracy = results.Average(r => r.AccuracyScore);
        averageAccuracy.Should().BeGreaterThan(0.95, "Concurrent operations should maintain high accuracy");
    }

    [TestMethod]
    public async Task TemplateBenchmark_MemoryEfficiency_ShouldMaintainReasonableMemoryUsage()
    {
        // Arrange
        var template = CreateLargeTemplateWithManyFields();
        await _templateService!.CreateTemplateAsync(template);

        var initialMemory = GC.GetTotalMemory(true);
        var processMemoryBefore = Process.GetCurrentProcess().WorkingSet64;

        // Act - Process multiple documents to test memory usage
        const int documentCount = 20;
        for (int i = 0; i < documentCount; i++)
        {
            var document = CreateLargeTestDocument($"large_document_{i}.pdf");
            var result = await _extractionEngine!.ExtractFieldsAsync(template, document);
            result.Should().NotBeNull();

            // Force garbage collection every 5 documents
            if (i % 5 == 0)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
        }

        var finalMemory = GC.GetTotalMemory(true);
        var processMemoryAfter = Process.GetCurrentProcess().WorkingSet64;

        // Assert - Memory efficiency
        var memoryIncreaseMB = (finalMemory - initialMemory) / (1024 * 1024);
        memoryIncreaseMB.Should().BeLessThan(100, "Memory increase should be under 100MB after processing 20 documents");

        var processMemoryIncreaseMB = (processMemoryAfter - processMemoryBefore) / (1024 * 1024);
        processMemoryIncreaseMB.Should().BeLessThan(200, "Process memory increase should be under 200MB");
    }

    [TestMethod]
    public async Task TemplateBenchmark_ExtractionSpeedOptimization_ShouldShowConsistentPerformance()
    {
        // Arrange
        var template = CreateOptimizedTemplate();
        await _templateService!.CreateTemplateAsync(template);

        var documents = new List<string>();
        for (int i = 0; i < 10; i++)
        {
            documents.Add(CreateTestDocumentWithKnownContent($"test_document_{i}.pdf"));
        }

        var extractionTimes = new List<long>();

        // Act - Measure extraction times for consistency
        foreach (var document in documents)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = await _extractionEngine!.ExtractFieldsAsync(template, document);
            stopwatch.Stop();

            result.Should().NotBeNull();
            result.IsSuccessful.Should().BeTrue();
            extractionTimes.Add(stopwatch.ElapsedMilliseconds);
        }

        // Assert - Performance consistency
        var averageTime = extractionTimes.Average();
        var maxTime = extractionTimes.Max();
        var minTime = extractionTimes.Min();
        var standardDeviation = Math.Sqrt(extractionTimes.Select(t => Math.Pow(t - averageTime, 2)).Average());

        averageTime.Should().BeLessThan(1500, "Average extraction time should be under 1.5 seconds");
        standardDeviation.Should().BeLessThan(averageTime * 0.3, "Standard deviation should be less than 30% of average time");
        (maxTime - minTime).Should().BeLessThan(averageTime, "Time variance should be reasonable");
    }

    // Helper methods for creating test templates and documents
    private ImportTemplate CreateSecurityReportTemplate()
    {
        return new ImportTemplate
        {
            Id = Guid.NewGuid(),
            Name = "Security Report Template",
            Description = "Template for security analysis reports",
            Category = "Security",
            SupportedFormats = new List<string> { "pdf", "docx" },
            Fields = new List<TemplateField>
            {
                new TemplateField { Name = "ThreatLevel", DisplayName = "Threat Level", FieldType = FieldType.Text, IsRequired = true },
                new TemplateField { Name = "Analyst", DisplayName = "Analyst", FieldType = FieldType.Text, IsRequired = true },
                new TemplateField { Name = "ReportDate", DisplayName = "Report Date", FieldType = FieldType.Date, IsRequired = true }
            },
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test-user"
        };
    }

    private ImportTemplate CreateFinancialReportTemplate()
    {
        return new ImportTemplate
        {
            Id = Guid.NewGuid(),
            Name = "Financial Report Template",
            Description = "Template for financial analysis reports",
            Category = "Financial",
            SupportedFormats = new List<string> { "pdf", "xlsx" },
            Fields = new List<TemplateField>
            {
                new TemplateField { Name = "ReportTitle", DisplayName = "Report Title", FieldType = FieldType.Text, IsRequired = true },
                new TemplateField { Name = "Amount", DisplayName = "Amount", FieldType = FieldType.Number, IsRequired = true },
                new TemplateField { Name = "Currency", DisplayName = "Currency", FieldType = FieldType.Text, IsRequired = true }
            },
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test-user"
        };
    }

    private ImportTemplate CreateLegalDocumentTemplate()
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
                new TemplateField { Name = "CaseNumber", DisplayName = "Case Number", FieldType = FieldType.Text, IsRequired = true },
                new TemplateField { Name = "Court", DisplayName = "Court", FieldType = FieldType.Text, IsRequired = true },
                new TemplateField { Name = "FilingDate", DisplayName = "Filing Date", FieldType = FieldType.Date, IsRequired = true }
            },
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test-user"
        };
    }

    private ImportTemplate CreateTechnicalReportTemplate()
    {
        return new ImportTemplate
        {
            Id = Guid.NewGuid(),
            Name = "Technical Report Template",
            Description = "Template for technical reports",
            Category = "Technical",
            SupportedFormats = new List<string> { "pdf", "docx" },
            Fields = new List<TemplateField>
            {
                new TemplateField { Name = "SystemName", DisplayName = "System Name", FieldType = FieldType.Text, IsRequired = true },
                new TemplateField { Name = "Version", DisplayName = "Version", FieldType = FieldType.Text, IsRequired = true },
                new TemplateField { Name = "TestDate", DisplayName = "Test Date", FieldType = FieldType.Date, IsRequired = true }
            },
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test-user"
        };
    }

    private ImportTemplate CreateIncidentReportTemplate()
    {
        return new ImportTemplate
        {
            Id = Guid.NewGuid(),
            Name = "Incident Report Template",
            Description = "Template for incident reports",
            Category = "Security",
            SupportedFormats = new List<string> { "pdf", "docx" },
            Fields = new List<TemplateField>
            {
                new TemplateField { Name = "IncidentId", DisplayName = "Incident ID", FieldType = FieldType.Text, IsRequired = true },
                new TemplateField { Name = "Severity", DisplayName = "Severity", FieldType = FieldType.DropdownList, IsRequired = true },
                new TemplateField { Name = "ReportedBy", DisplayName = "Reported By", FieldType = FieldType.Text, IsRequired = true }
            },
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test-user"
        };
    }

    private ImportTemplate CreateComprehensiveTestTemplate()
    {
        return new ImportTemplate
        {
            Id = Guid.NewGuid(),
            Name = "Comprehensive Test Template",
            Description = "Template with various field types for testing",
            Category = "Testing",
            SupportedFormats = new List<string> { "pdf", "docx", "xlsx" },
            Fields = new List<TemplateField>
            {
                new TemplateField { Name = "TextValue", DisplayName = "Text Value", FieldType = FieldType.Text, IsRequired = true },
                new TemplateField { Name = "NumberValue", DisplayName = "Number Value", FieldType = FieldType.Number, IsRequired = true },
                new TemplateField { Name = "DateValue", DisplayName = "Date Value", FieldType = FieldType.Date, IsRequired = true },
                new TemplateField { Name = "EmailValue", DisplayName = "Email Value", FieldType = FieldType.Email, IsRequired = false },
                new TemplateField { Name = "DropdownValue", DisplayName = "Dropdown Value", FieldType = FieldType.DropdownList, IsRequired = false }
            },
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test-user"
        };
    }

    private ImportTemplate CreateLargeTemplateWithManyFields()
    {
        var fields = new List<TemplateField>();
        for (int i = 1; i <= 20; i++)
        {
            fields.Add(new TemplateField 
            { 
                Name = $"Field{i}", 
                DisplayName = $"Field {i}", 
                FieldType = FieldType.Text, 
                IsRequired = i <= 10 
            });
        }

        return new ImportTemplate
        {
            Id = Guid.NewGuid(),
            Name = "Large Template with Many Fields",
            Description = "Template with many fields for memory testing",
            Category = "Testing",
            SupportedFormats = new List<string> { "pdf", "docx" },
            Fields = fields,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test-user"
        };
    }

    private ImportTemplate CreateOptimizedTemplate()
    {
        return new ImportTemplate
        {
            Id = Guid.NewGuid(),
            Name = "Optimized Template",
            Description = "Optimized template for performance testing",
            Category = "Performance",
            SupportedFormats = new List<string> { "pdf" },
            Fields = new List<TemplateField>
            {
                new TemplateField { Name = "QuickField1", DisplayName = "Quick Field 1", FieldType = FieldType.Text, IsRequired = true },
                new TemplateField { Name = "QuickField2", DisplayName = "Quick Field 2", FieldType = FieldType.Number, IsRequired = true },
                new TemplateField { Name = "QuickField3", DisplayName = "Quick Field 3", FieldType = FieldType.Date, IsRequired = true }
            },
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test-user"
        };
    }

    private string CreateTestDocumentWithKnownContent(string filename)
    {
        var content = @"
Security Analysis Report

Threat Level: HIGH
Analyst: John Doe
Report Date: 2024-01-15

This is a comprehensive security analysis report with known content for testing purposes.
The document contains predictable patterns that should be extracted successfully.
";
        return CreateTestDocumentWithContent(filename, content);
    }

    private string CreateTestDocumentWithVariableContent(string filename, int index)
    {
        var content = $@"
Financial Report #{index}

Report Title: Quarterly Analysis {index}
Amount: {1000 + index * 100}
Currency: USD

Analysis Date: 2024-01-{15 + (index % 15):D2}
Report ID: FIN-{index:D4}
";
        return CreateTestDocumentWithContent(filename, content);
    }

    private string CreateTestDocumentWithPerfectData()
    {
        var content = @"
Test Document with Perfect Data

TextValue: Perfect Text Sample
NumberValue: 12345
DateValue: 2024-01-15
EmailValue: test@example.com
DropdownValue: Option1
";
        return CreateTestDocumentWithContent("perfect_data.pdf", content);
    }

    private string CreateTestDocumentWithMinorErrors()
    {
        var content = @"
Test Document with Minor Errors

TextValue: Sample Text (slightly different format)
NumberValue: 12,345
DateValue: Jan 15, 2024
EmailValue: test@example.com
DropdownValue: Option1
";
        return CreateTestDocumentWithContent("minor_errors.pdf", content);
    }

    private string CreateTestDocumentWithMissingFields()
    {
        var content = @"
Test Document with Missing Fields

TextValue: Sample Text
NumberValue: 54321
EmailValue: test@example.com
";
        return CreateTestDocumentWithContent("missing_fields.pdf", content);
    }

    private string CreateTestDocumentWithFormatVariations()
    {
        var content = @"
Test Document with Format Variations

Text Value: Different Format Sample
Number Value: 67890
Date Value: 2024/01/15
Email Value: different@format.com
Dropdown Value: Alternative Option
";
        return CreateTestDocumentWithContent("format_variations.pdf", content);
    }

    private string CreateLargeTestDocument(string filename)
    {
        var content = new System.Text.StringBuilder();
        content.AppendLine("Large Test Document");
        content.AppendLine("===================");
        
        for (int i = 1; i <= 20; i++)
        {
            content.AppendLine($"Field{i}: Value for field {i} with extended content to make the document larger");
        }

        // Add additional content to make the document larger
        for (int i = 0; i < 100; i++)
        {
            content.AppendLine($"Additional content line {i} to increase document size for memory testing purposes.");
        }

        return CreateTestDocumentWithContent(filename, content.ToString());
    }

    private string CreateTestDocumentWithContent(string filename, string content)
    {
        var tempFilePath = Path.Combine(Path.GetTempPath(), filename);
        File.WriteAllText(tempFilePath, content);
        _tempFiles.Add(tempFilePath);
        return tempFilePath;
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

// Additional classes for benchmark results
public class FieldExtractionResult
{
    public bool IsSuccessful { get; set; }
    public Dictionary<string, string> ExtractedFields { get; set; } = new();
    public double AccuracyScore { get; set; }
    public double ConfidenceScore { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class TemplateMatchResult
{
    public ImportTemplate Template { get; set; } = null!;
    public double ConfidenceScore { get; set; }
    public string MatchReason { get; set; } = string.Empty;
} 