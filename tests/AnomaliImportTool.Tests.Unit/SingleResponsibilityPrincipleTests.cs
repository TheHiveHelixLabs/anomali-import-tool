using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using AnomaliImportTool.Core.Application.Interfaces.Infrastructure;
using AnomaliImportTool.Core.Domain.ValueObjects;
using AnomaliImportTool.Core.Domain.SharedKernel.Primitives;
using AnomaliImportTool.Infrastructure.FileProcessing;

namespace AnomaliImportTool.Tests.Unit;

/// <summary>
/// Unit tests for Single Responsibility Principle implementation
/// Validates that classes have focused, cohesive responsibilities
/// </summary>
public class SingleResponsibilityPrincipleTests
{
    #region PDF Content Extractor Tests

    [Fact]
    public async Task PdfContentExtractor_Should_OnlyHandlePdfFiles()
    {
        // Arrange
        var extractor = new PdfContentExtractor();
        var pdfFile = new FilePath("test.pdf");
        var wordFile = new FilePath("test.docx");

        // Act & Assert
        extractor.SupportsFileFormat(pdfFile).Should().BeTrue();
        extractor.SupportsFileFormat(wordFile).Should().BeFalse();
        extractor.SupportedExtensions.Should().Contain(".pdf");
        extractor.SupportedExtensions.Should().HaveCount(1);
    }

    [Fact]
    public async Task PdfContentExtractor_Should_ExtractTextContent_WhenFileExists()
    {
        // Arrange
        var extractor = new PdfContentExtractor();
        var testFile = CreateTestFile("test.pdf");
        
        try
        {
            // Act
            var result = await extractor.ExtractTextAsync(testFile, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.FileFormat.Should().Be("PDF");
            result.ExtractedText.Should().NotBeNullOrWhiteSpace();
            result.ProcessingTime.Should().BeGreaterThan(TimeSpan.Zero);
            result.IsSuccessful.Should().BeTrue();
        }
        finally
        {
            CleanupTestFile(testFile);
        }
    }

    [Fact]
    public async Task PdfContentExtractor_Should_ExtractStructuredData_WhenFileContainsTables()
    {
        // Arrange
        var extractor = new PdfContentExtractor();
        var testFile = CreateTestFile("test_table.pdf");
        
        try
        {
            // Act
            var result = await extractor.ExtractStructuredDataAsync(testFile, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Schema.Should().Be("PDF-1.7");
            result.IsValid.Should().BeTrue();
            result.HasTables.Should().BeTrue();
            result.Tables.Should().HaveCountGreaterThan(0);
        }
        finally
        {
            CleanupTestFile(testFile);
        }
    }

    [Fact]
    public async Task PdfContentExtractor_Should_ThrowException_ForUnsupportedFormat()
    {
        // Arrange
        var extractor = new PdfContentExtractor();
        var unsupportedFile = new FilePath("test.txt");

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() => 
            extractor.ExtractTextAsync(unsupportedFile, CancellationToken.None));
    }

    #endregion

    #region File Security Validator Tests

    [Fact]
    public async Task FileSecurityValidator_Should_DetectDangerousExtensions()
    {
        // Arrange
        var validator = new FileSecurityValidator();
        var dangerousFile = new FilePath("malware.exe");

        // Act
        var result = await validator.ValidateFormatAsync(dangerousFile, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.HasErrors.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "DANGEROUS_EXTENSION");
    }

    [Fact]
    public async Task FileSecurityValidator_Should_DetectSuspiciousFileNames()
    {
        // Arrange
        var validator = new FileSecurityValidator();
        var suspiciousFile = CreateTestFile("urgent_invoice.pdf");
        
        try
        {
            // Act
            var result = await validator.ValidateFormatAsync(suspiciousFile, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.HasWarnings.Should().BeTrue();
            result.Warnings.Should().Contain(w => w.Code == "SUSPICIOUS_FILENAME");
        }
        finally
        {
            CleanupTestFile(suspiciousFile);
        }
    }

    [Fact]
    public async Task FileSecurityValidator_Should_DetectDoubleExtensions()
    {
        // Arrange
        var validator = new FileSecurityValidator();
        var doubleExtensionFile = new FilePath("document.pdf.exe");

        // Act
        var result = await validator.ValidateFormatAsync(doubleExtensionFile, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.HasErrors.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "DOUBLE_EXTENSION");
    }

    [Fact]
    public async Task FileSecurityValidator_Should_ValidateFileIntegrity()
    {
        // Arrange
        var validator = new FileSecurityValidator();
        var testFile = CreateTestFile("test.pdf");
        var expectedHash = new ContentHash("1234567890123456789012345678901234567890123456789012345678901234", HashAlgorithmType.SHA256);
        
        try
        {
            // Act
            var result = await validator.ValidateIntegrityAsync(testFile, expectedHash, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.ValidationMethod.Should().Be("SHA256");
            result.ActualHash.Should().NotBeNull();
            result.ExpectedHash.Should().Be(expectedHash);
        }
        finally
        {
            CleanupTestFile(testFile);
        }
    }

    [Fact]
    public async Task FileSecurityValidator_Should_PerformSecurityScan()
    {
        // Arrange
        var validator = new FileSecurityValidator();
        var testFile = CreateTestFile("test.pdf");
        
        try
        {
            // Act
            var result = await validator.ValidateSecurityAsync(testFile, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.ScanEngine.Should().NotBeNullOrWhiteSpace();
            result.ScanTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
            result.ScanVersion.Should().NotBeNullOrWhiteSpace();
        }
        finally
        {
            CleanupTestFile(testFile);
        }
    }

    [Fact]
    public async Task FileSecurityValidator_Should_ValidateFileSize()
    {
        // Arrange
        var validator = new FileSecurityValidator();
        var testFile = CreateTestFile("test.pdf");
        var maxSize = 1024L; // 1KB
        
        try
        {
            // Act
            var result = await validator.ValidateSizeAsync(testFile, maxSize);

            // Assert
            result.Should().NotBeNull();
            result.MaxAllowedSize.Should().Be(maxSize);
            result.SizeUnit.Should().Be("bytes");
            result.ActualSize.Should().BeGreaterThan(0);
        }
        finally
        {
            CleanupTestFile(testFile);
        }
    }

    [Fact]
    public async Task FileSecurityValidator_Should_ValidateFileAccessibility()
    {
        // Arrange
        var validator = new FileSecurityValidator();
        var testFile = CreateTestFile("test.pdf");
        
        try
        {
            // Act
            var result = await validator.ValidateAccessibilityAsync(testFile);

            // Assert
            result.Should().NotBeNull();
            result.FileExists.Should().BeTrue();
            result.IsAccessible.Should().BeTrue();
            result.HasReadPermission.Should().BeTrue();
        }
        finally
        {
            CleanupTestFile(testFile);
        }
    }

    #endregion

    #region Single Responsibility Validation Tests

    [Fact]
    public void PdfContentExtractor_Should_HaveSingleResponsibility()
    {
        // Arrange
        var extractorType = typeof(PdfContentExtractor);
        
        // Act
        var methods = extractorType.GetMethods()
            .Where(m => m.IsPublic && !m.IsSpecialName)
            .Select(m => m.Name)
            .ToList();

        // Assert - Should only have content extraction related methods
        methods.Should().Contain("ExtractTextAsync");
        methods.Should().Contain("ExtractStructuredDataAsync");
        methods.Should().Contain("SupportsFileFormat");
        
        // Should NOT have validation, metadata extraction, or other unrelated methods
        methods.Should().NotContain(m => m.Contains("Validate"));
        methods.Should().NotContain(m => m.Contains("Metadata"));
        methods.Should().NotContain(m => m.Contains("Security"));
    }

    [Fact]
    public void FileSecurityValidator_Should_HaveSingleResponsibility()
    {
        // Arrange
        var validatorType = typeof(FileSecurityValidator);
        
        // Act
        var methods = validatorType.GetMethods()
            .Where(m => m.IsPublic && !m.IsSpecialName)
            .Select(m => m.Name)
            .ToList();

        // Assert - Should only have validation related methods
        methods.Should().Contain("ValidateFormatAsync");
        methods.Should().Contain("ValidateIntegrityAsync");
        methods.Should().Contain("ValidateSecurityAsync");
        methods.Should().Contain("ValidateSizeAsync");
        methods.Should().Contain("ValidateAccessibilityAsync");
        
        // Should NOT have content extraction or other unrelated methods
        methods.Should().NotContain(m => m.Contains("Extract"));
        methods.Should().NotContain(m => m.Contains("Process"));
        methods.Should().NotContain(m => m.Contains("Transform"));
    }

    [Fact]
    public void Interfaces_Should_FollowSingleResponsibilityPrinciple()
    {
        // Arrange & Act
        var contentExtractorInterface = typeof(IFileContentExtractor);
        var validatorInterface = typeof(IFileValidator);
        var metadataExtractorInterface = typeof(IFileMetadataExtractor);
        var orchestratorInterface = typeof(IFileProcessingOrchestrator);

        // Assert - Each interface should have a focused responsibility
        
        // Content extractor should only extract content
        var contentMethods = contentExtractorInterface.GetMethods().Select(m => m.Name).ToList();
        contentMethods.Should().OnlyContain(name => 
            name.Contains("Extract") || name.Contains("Supports") || name.Contains("get_"));

        // Validator should only validate
        var validatorMethods = validatorInterface.GetMethods().Select(m => m.Name).ToList();
        validatorMethods.Should().OnlyContain(name => 
            name.Contains("Validate") || name.Contains("get_"));

        // Metadata extractor should only extract metadata
        var metadataMethods = metadataExtractorInterface.GetMethods().Select(m => m.Name).ToList();
        metadataMethods.Should().OnlyContain(name => 
            name.Contains("Extract") || name.Contains("Calculate") || name.Contains("Supports") || name.Contains("get_"));

        // Orchestrator should only orchestrate
        var orchestratorMethods = orchestratorInterface.GetMethods().Select(m => m.Name).ToList();
        orchestratorMethods.Should().OnlyContain(name => 
            name.Contains("Process") || name.Contains("Validate") || name.Contains("get_"));
    }

    [Fact]
    public void ValueObjects_Should_BeImmutableAndFocused()
    {
        // Arrange
        var valueObjectTypes = new[]
        {
            typeof(DetailedExtractionResult),
            typeof(FileValidationResult),
            typeof(SecurityValidationResult),
            typeof(FileProcessingOptions),
            typeof(FileProcessingResult)
        };

        // Act & Assert
        foreach (var type in valueObjectTypes)
        {
            // Should be records (immutable)
            type.IsValueType.Should().BeFalse("Value objects should be reference types (records)");
            
            // Should have focused properties related to their responsibility
            var properties = type.GetProperties().Select(p => p.Name).ToList();
            properties.Should().NotBeEmpty("Value objects should have properties");
            
            // Should not have methods that change state (immutable)
            var methods = type.GetMethods()
                .Where(m => m.IsPublic && !m.IsSpecialName && m.DeclaringType == type)
                .Where(m => !m.Name.StartsWith("get_") && !m.Name.StartsWith("set_"))
                .Where(m => !m.Name.Contains("Deconstruct")) // Records have Deconstruct methods
                .ToList();
                
            // Methods should be query methods only (no state changes)
            methods.Should().OnlyContain(method => 
                method.ReturnType != typeof(void) || method.Name.Contains("ToString"));
        }
    }

    #endregion

    #region Helper Methods

    private FilePath CreateTestFile(string fileName)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), fileName);
        File.WriteAllText(tempPath, "Test file content for SRP validation");
        return new FilePath(tempPath);
    }

    private void CleanupTestFile(FilePath filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch
        {
            // Ignore cleanup errors in tests
        }
    }

    #endregion
} 