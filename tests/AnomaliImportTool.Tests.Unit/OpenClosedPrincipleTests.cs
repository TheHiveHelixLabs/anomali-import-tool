using AnomaliImportTool.Core.Application.Interfaces.Services;
using AnomaliImportTool.Core.Domain.SharedKernel.Primitives;
using AnomaliImportTool.Infrastructure.DocumentProcessing;
using AnomaliImportTool.Infrastructure.DocumentProcessing.Strategies;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace AnomaliImportTool.Tests.Unit;

/// <summary>
/// Tests validating the Open/Closed Principle implementation.
/// Verifies that new functionality can be added without modifying existing code.
/// </summary>
public class OpenClosedPrincipleTests
{
    private readonly Mock<ILogger<DocumentProcessingStrategyFactory>> _factoryLoggerMock;
    private readonly Mock<ILogger<PdfDocumentProcessingStrategy>> _pdfLoggerMock;
    private readonly Mock<ILogger<WordDocumentProcessingStrategy>> _wordLoggerMock;

    public OpenClosedPrincipleTests()
    {
        _factoryLoggerMock = new Mock<ILogger<DocumentProcessingStrategyFactory>>();
        _pdfLoggerMock = new Mock<ILogger<PdfDocumentProcessingStrategy>>();
        _wordLoggerMock = new Mock<ILogger<WordDocumentProcessingStrategy>>();
    }

    [Fact]
    public void DocumentProcessingStrategyFactory_Should_ImplementOpenClosedPrinciple()
    {
        // Arrange
        var strategies = new List<IDocumentProcessingStrategy>
        {
            new PdfDocumentProcessingStrategy(_pdfLoggerMock.Object),
            new WordDocumentProcessingStrategy(_wordLoggerMock.Object)
        };

        // Act
        var factory = new DocumentProcessingStrategyFactory(strategies, _factoryLoggerMock.Object);

        // Assert
        var allStrategies = factory.GetAllStrategies();
        allStrategies.Should().HaveCount(2);
        allStrategies.Should().Contain(s => s is PdfDocumentProcessingStrategy);
        allStrategies.Should().Contain(s => s is WordDocumentProcessingStrategy);
    }

    [Fact]
    public void DocumentProcessingStrategyFactory_Should_AllowNewStrategyRegistration_WithoutModifyingExistingCode()
    {
        // Arrange
        var initialStrategies = new List<IDocumentProcessingStrategy>
        {
            new PdfDocumentProcessingStrategy(_pdfLoggerMock.Object)
        };
        var factory = new DocumentProcessingStrategyFactory(initialStrategies, _factoryLoggerMock.Object);

        // Act - Add new strategy without modifying existing code
        var newStrategy = new WordDocumentProcessingStrategy(_wordLoggerMock.Object);
        factory.RegisterStrategy(newStrategy);

        // Assert
        var allStrategies = factory.GetAllStrategies();
        allStrategies.Should().HaveCount(2);
        allStrategies.Should().Contain(s => s is WordDocumentProcessingStrategy);
    }

    [Fact]
    public void DocumentProcessingStrategyFactory_Should_SelectCorrectStrategy_BasedOnFilePath()
    {
        // Arrange
        var strategies = new List<IDocumentProcessingStrategy>
        {
            new PdfDocumentProcessingStrategy(_pdfLoggerMock.Object),
            new WordDocumentProcessingStrategy(_wordLoggerMock.Object)
        };
        var factory = new DocumentProcessingStrategyFactory(strategies, _factoryLoggerMock.Object);

        // Act & Assert
        var pdfFile = new FilePath("test.pdf");
        var pdfStrategy = factory.GetStrategy(pdfFile);
        pdfStrategy.Should().NotBeNull();
        pdfStrategy.Should().BeOfType<PdfDocumentProcessingStrategy>();

        var docxFile = new FilePath("test.docx");
        var wordStrategy = factory.GetStrategy(docxFile);
        wordStrategy.Should().NotBeNull();
        wordStrategy.Should().BeOfType<WordDocumentProcessingStrategy>();

        var unsupportedFile = new FilePath("test.xyz");
        var noStrategy = factory.GetStrategy(unsupportedFile);
        noStrategy.Should().BeNull();
    }

    [Fact]
    public void DocumentProcessingStrategyFactory_Should_OrderStrategies_ByPriority()
    {
        // Arrange
        var strategies = new List<IDocumentProcessingStrategy>
        {
            new WordDocumentProcessingStrategy(_wordLoggerMock.Object), // Priority 90
            new PdfDocumentProcessingStrategy(_pdfLoggerMock.Object)    // Priority 100
        };
        var factory = new DocumentProcessingStrategyFactory(strategies, _factoryLoggerMock.Object);

        // Act
        var orderedStrategies = factory.GetAllStrategies();

        // Assert
        orderedStrategies.Should().HaveCount(2);
        orderedStrategies[0].Should().BeOfType<PdfDocumentProcessingStrategy>(); // Higher priority first
        orderedStrategies[1].Should().BeOfType<WordDocumentProcessingStrategy>();
    }

    [Fact]
    public void DocumentProcessingStrategyFactory_Should_GetSupportedExtensions_FromAllStrategies()
    {
        // Arrange
        var strategies = new List<IDocumentProcessingStrategy>
        {
            new PdfDocumentProcessingStrategy(_pdfLoggerMock.Object),
            new WordDocumentProcessingStrategy(_wordLoggerMock.Object)
        };
        var factory = new DocumentProcessingStrategyFactory(strategies, _factoryLoggerMock.Object);

        // Act
        var supportedExtensions = factory.GetSupportedExtensions();

        // Assert
        supportedExtensions.Should().Contain(".pdf");
        supportedExtensions.Should().Contain(".docx");
        supportedExtensions.Should().Contain(".doc");
        supportedExtensions.Should().HaveCount(3);
    }

    [Fact]
    public void DocumentProcessingStrategyFactory_Should_UnregisterStrategy_Successfully()
    {
        // Arrange
        var strategies = new List<IDocumentProcessingStrategy>
        {
            new PdfDocumentProcessingStrategy(_pdfLoggerMock.Object),
            new WordDocumentProcessingStrategy(_wordLoggerMock.Object)
        };
        var factory = new DocumentProcessingStrategyFactory(strategies, _factoryLoggerMock.Object);

        // Act
        var unregistered = factory.UnregisterStrategy(typeof(WordDocumentProcessingStrategy));

        // Assert
        unregistered.Should().BeTrue();
        var remainingStrategies = factory.GetAllStrategies();
        remainingStrategies.Should().HaveCount(1);
        remainingStrategies.Should().Contain(s => s is PdfDocumentProcessingStrategy);
        remainingStrategies.Should().NotContain(s => s is WordDocumentProcessingStrategy);
    }

    [Fact]
    public void PdfDocumentProcessingStrategy_Should_OnlyProcessPdfFiles()
    {
        // Arrange
        var strategy = new PdfDocumentProcessingStrategy(_pdfLoggerMock.Object);

        // Act & Assert
        strategy.CanProcess(new FilePath("test.pdf")).Should().BeTrue();
        strategy.CanProcess(new FilePath("test.PDF")).Should().BeTrue(); // Case insensitive
        strategy.CanProcess(new FilePath("test.docx")).Should().BeFalse();
        strategy.CanProcess(new FilePath("test.txt")).Should().BeFalse();
    }

    [Fact]
    public void WordDocumentProcessingStrategy_Should_ProcessWordFiles()
    {
        // Arrange
        var strategy = new WordDocumentProcessingStrategy(_wordLoggerMock.Object);

        // Act & Assert
        strategy.CanProcess(new FilePath("test.docx")).Should().BeTrue();
        strategy.CanProcess(new FilePath("test.doc")).Should().BeTrue();
        strategy.CanProcess(new FilePath("test.DOCX")).Should().BeTrue(); // Case insensitive
        strategy.CanProcess(new FilePath("test.pdf")).Should().BeFalse();
        strategy.CanProcess(new FilePath("test.txt")).Should().BeFalse();
    }

    [Fact]
    public void PdfDocumentProcessingStrategy_Should_HaveCorrectPriority()
    {
        // Arrange & Act
        var strategy = new PdfDocumentProcessingStrategy(_pdfLoggerMock.Object);

        // Assert
        strategy.Priority.Should().Be(100);
    }

    [Fact]
    public void WordDocumentProcessingStrategy_Should_HaveCorrectPriority()
    {
        // Arrange & Act
        var strategy = new WordDocumentProcessingStrategy(_wordLoggerMock.Object);

        // Assert
        strategy.Priority.Should().Be(90);
    }

    [Fact]
    public void PdfDocumentProcessingStrategy_Should_HaveCorrectSupportedExtensions()
    {
        // Arrange & Act
        var strategy = new PdfDocumentProcessingStrategy(_pdfLoggerMock.Object);

        // Assert
        strategy.SupportedExtensions.Should().HaveCount(1);
        strategy.SupportedExtensions.Should().Contain(".pdf");
    }

    [Fact]
    public void WordDocumentProcessingStrategy_Should_HaveCorrectSupportedExtensions()
    {
        // Arrange & Act
        var strategy = new WordDocumentProcessingStrategy(_wordLoggerMock.Object);

        // Assert
        strategy.SupportedExtensions.Should().HaveCount(2);
        strategy.SupportedExtensions.Should().Contain(".docx");
        strategy.SupportedExtensions.Should().Contain(".doc");
    }

    [Fact]
    public async Task PdfDocumentProcessingStrategy_Should_ValidateNonExistentFile_WithErrors()
    {
        // Arrange
        var strategy = new PdfDocumentProcessingStrategy(_pdfLoggerMock.Object);
        var nonExistentFile = new FilePath("nonexistent.pdf");

        // Act
        var result = await strategy.ValidateAsync(nonExistentFile);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.Errors.Should().Contain(e => e.Code == "PDF_VAL002");
    }

    [Fact]
    public async Task WordDocumentProcessingStrategy_Should_ValidateNonExistentFile_WithErrors()
    {
        // Arrange
        var strategy = new WordDocumentProcessingStrategy(_wordLoggerMock.Object);
        var nonExistentFile = new FilePath("nonexistent.docx");

        // Act
        var result = await strategy.ValidateAsync(nonExistentFile);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.Errors.Should().Contain(e => e.Code == "WORD_VAL002");
    }

    [Fact]
    public async Task PdfDocumentProcessingStrategy_Should_ProcessNonExistentFile_WithErrors()
    {
        // Arrange
        var strategy = new PdfDocumentProcessingStrategy(_pdfLoggerMock.Object);
        var nonExistentFile = new FilePath("nonexistent.pdf");

        // Act
        var result = await strategy.ProcessAsync(nonExistentFile);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.Errors.Should().Contain(e => e.Code == "PDF001");
        result.Content.Should().BeEmpty();
    }

    [Fact]
    public async Task WordDocumentProcessingStrategy_Should_ProcessNonExistentFile_WithErrors()
    {
        // Arrange
        var strategy = new WordDocumentProcessingStrategy(_wordLoggerMock.Object);
        var nonExistentFile = new FilePath("nonexistent.docx");

        // Act
        var result = await strategy.ProcessAsync(nonExistentFile);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.Errors.Should().Contain(e => e.Code == "WORD001");
        result.Content.Should().BeEmpty();
    }

    [Theory]
    [InlineData(".pdf", typeof(PdfDocumentProcessingStrategy))]
    [InlineData(".docx", typeof(WordDocumentProcessingStrategy))]
    [InlineData(".doc", typeof(WordDocumentProcessingStrategy))]
    public void DocumentProcessingStrategies_Should_FollowOpenClosedPrinciple_ForExtensibility(
        string extension, Type expectedStrategyType)
    {
        // Arrange
        var strategies = new List<IDocumentProcessingStrategy>
        {
            new PdfDocumentProcessingStrategy(_pdfLoggerMock.Object),
            new WordDocumentProcessingStrategy(_wordLoggerMock.Object)
        };
        var factory = new DocumentProcessingStrategyFactory(strategies, _factoryLoggerMock.Object);

        // Act
        var strategy = factory.GetStrategy(new FilePath($"test{extension}"));

        // Assert
        strategy.Should().NotBeNull();
        strategy.Should().BeOfType(expectedStrategyType);
    }

    [Fact]
    public void DocumentProcessingStrategy_Interface_Should_SupportExtensionWithoutModification()
    {
        // This test validates that the IDocumentProcessingStrategy interface
        // is designed to support extension without modification (Open/Closed Principle)

        // Arrange & Act
        var interfaceType = typeof(IDocumentProcessingStrategy);

        // Assert
        interfaceType.Should().BeInterface();
        
        // Verify interface has essential methods for extensibility
        interfaceType.GetMethod(nameof(IDocumentProcessingStrategy.CanProcess)).Should().NotBeNull();
        interfaceType.GetMethod(nameof(IDocumentProcessingStrategy.ProcessAsync)).Should().NotBeNull();
        interfaceType.GetMethod(nameof(IDocumentProcessingStrategy.ValidateAsync)).Should().NotBeNull();
        
        // Verify interface has properties for strategy identification
        interfaceType.GetProperty(nameof(IDocumentProcessingStrategy.SupportedExtensions)).Should().NotBeNull();
        interfaceType.GetProperty(nameof(IDocumentProcessingStrategy.Priority)).Should().NotBeNull();
    }

    [Fact]
    public void DocumentProcessingStrategyFactory_Should_SupportPluginArchitecture()
    {
        // This test validates that the factory supports plugin architecture
        // which is a key aspect of the Open/Closed Principle

        // Arrange
        var initialStrategies = new List<IDocumentProcessingStrategy>
        {
            new PdfDocumentProcessingStrategy(_pdfLoggerMock.Object)
        };
        var factory = new DocumentProcessingStrategyFactory(initialStrategies, _factoryLoggerMock.Object);

        // Act - Simulate plugin loading by registering new strategies
        var newStrategy1 = new WordDocumentProcessingStrategy(_wordLoggerMock.Object);
        var customStrategy = new TestCustomDocumentProcessingStrategy();
        
        factory.RegisterStrategy(newStrategy1);
        factory.RegisterStrategy(customStrategy);

        // Assert
        var allStrategies = factory.GetAllStrategies();
        allStrategies.Should().HaveCount(3);
        allStrategies.Should().Contain(s => s is TestCustomDocumentProcessingStrategy);
        
        var supportedExtensions = factory.GetSupportedExtensions();
        supportedExtensions.Should().Contain(".test");
    }

    /// <summary>
    /// Test strategy to demonstrate extensibility without modifying existing code
    /// </summary>
    private class TestCustomDocumentProcessingStrategy : IDocumentProcessingStrategy
    {
        public IReadOnlyList<string> SupportedExtensions { get; } = new[] { ".test" }.AsReadOnly();
        public int Priority => 50;

        public bool CanProcess(FilePath filePath)
        {
            var extension = Path.GetExtension(filePath.Value).ToLowerInvariant();
            return SupportedExtensions.Contains(extension);
        }

        public Task<DocumentProcessingResult> ProcessAsync(FilePath filePath, CancellationToken cancellationToken = default)
        {
            var result = new DocumentProcessingResult(
                IsSuccess: true,
                Content: "Test content",
                Metadata: new DocumentMetadata(
                    FileName: Path.GetFileName(filePath.Value),
                    FileSize: 0,
                    CreatedDate: DateTime.UtcNow,
                    ModifiedDate: DateTime.UtcNow,
                    Author: "Test",
                    Title: "Test Document",
                    Subject: "Test",
                    PageCount: 1,
                    MimeType: "application/test",
                    CustomProperties: new Dictionary<string, object>()
                ),
                Errors: Array.Empty<ProcessingError>(),
                Warnings: Array.Empty<ProcessingWarning>(),
                ProcessingTime: TimeSpan.FromMilliseconds(1)
            );

            return Task.FromResult(result);
        }

        public Task<DocumentValidationResult> ValidateAsync(FilePath filePath, CancellationToken cancellationToken = default)
        {
            var result = new DocumentValidationResult(
                IsValid: true,
                Errors: Array.Empty<ValidationError>(),
                Warnings: Array.Empty<ValidationWarning>()
            );

            return Task.FromResult(result);
        }
    }
} 