using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AnomaliImportTool.Core.Interfaces;
using AnomaliImportTool.Core.Models;
using AnomaliImportTool.Infrastructure.DocumentProcessing;
using AnomaliImportTool.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AnomaliImportTool.Tests.Unit.Services;

public class TemplateMatchingServiceTests : IDisposable
{
    private readonly Mock<ILogger<TemplateMatchingService>> _mockLogger;
    private readonly Mock<IImportTemplateService> _mockTemplateService;
    private readonly Mock<DocumentProcessingService> _mockDocumentProcessor;
    private readonly TemplateMatchingService _service;
    private readonly List<string> _tempFiles;
    private bool _disposed = false;

    public TemplateMatchingServiceTests()
    {
        _mockLogger = new Mock<ILogger<TemplateMatchingService>>();
        _mockTemplateService = new Mock<IImportTemplateService>();
        _mockDocumentProcessor = new Mock<DocumentProcessingService>();
        
        _service = new TemplateMatchingService(
            _mockLogger.Object,
            _mockTemplateService.Object,
            _mockDocumentProcessor.Object);
        
        _tempFiles = new List<string>();
    }

    [Fact]
    public async Task CreateDocumentFingerprintAsync_WithValidPdfDocument_ShouldReturnFingerprint()
    {
        var testDoc = "test.pdf";
        var mockDocument = new Document
        {
            FilePath = testDoc,
            Content = "Test PDF content",
            Metadata = new Dictionary<string, object> { { "PageCount", 1 } }
        };
        
        _mockDocumentProcessor.Setup(x => x.ProcessDocumentAsync(testDoc, default))
            .ReturnsAsync(mockDocument);

        var fingerprint = await _service.CreateDocumentFingerprintAsync(testDoc);

        fingerprint.Should().NotBeNull();
    }

    public void Dispose()
    {
        _disposed = true;
    }
}
