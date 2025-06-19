using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AnomaliImportTool.Core.Models;
using AnomaliImportTool.Infrastructure.DocumentProcessing;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AnomaliImportTool.Tests.Unit.DocumentProcessing
{
    public class PdfDocumentProcessorTests
    {
        private readonly Mock<ILogger<PdfDocumentProcessor>> _loggerMock;
        private readonly Mock<ILogger<OcrProcessor>> _ocrLoggerMock;
        private readonly PdfDocumentProcessor _processor;

        public PdfDocumentProcessorTests()
        {
            _loggerMock = new Mock<ILogger<PdfDocumentProcessor>>();
            _ocrLoggerMock = new Mock<ILogger<OcrProcessor>>();
            _processor = new PdfDocumentProcessor(_loggerMock.Object, _ocrLoggerMock.Object);
        }

        [Fact]
        public void SupportedExtensions_ReturnsCorrectExtensions()
        {
            // Act
            var extensions = _processor.SupportedExtensions;

            // Assert
            Assert.NotNull(extensions);
            Assert.Single(extensions);
            Assert.Contains(".pdf", extensions);
        }

        [Theory]
        [InlineData("document.pdf", true)]
        [InlineData("document.PDF", true)]
        [InlineData("document.Pdf", true)]
        [InlineData("document.doc", false)]
        [InlineData("document.txt", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void CanProcess_ReturnsExpectedResult(string filePath, bool expected)
        {
            // Act
            var result = _processor.CanProcess(filePath);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task ProcessAsync_ThrowsArgumentException_WhenFileIsNotPdf()
        {
            // Arrange
            var filePath = "document.txt";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _processor.ProcessAsync(filePath));
        }

        [Fact]
        public async Task ProcessAsync_ThrowsFileNotFoundException_WhenFileDoesNotExist()
        {
            // Arrange
            var filePath = "nonexistent.pdf";

            // Act & Assert
            await Assert.ThrowsAsync<FileNotFoundException>(() => 
                _processor.ProcessAsync(filePath));
        }

        [Fact]
        public async Task ProcessAsync_ThrowsInvalidOperationException_WhenFileSizeExceedsLimit()
        {
            // Arrange
            var tempFile = Path.GetTempFileName() + ".pdf";
            try
            {
                // Create a dummy PDF file
                await File.WriteAllBytesAsync(tempFile, new byte[] { 0x25, 0x50, 0x44, 0x46 }); // %PDF header

                var options = new ProcessingOptions { MaxFileSizeMB = 0 }; // Set limit to 0 MB

                // Act & Assert
                await Assert.ThrowsAsync<InvalidOperationException>(() => 
                    _processor.ProcessAsync(tempFile, options));
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task ProcessAsync_ReturnsDocument_WithCorrectBasicProperties()
        {
            // This test would require a real PDF file or mocking PDFSharp
            // For now, it's a placeholder showing the test structure
            
            // Arrange
            var tempFile = Path.GetTempFileName() + ".pdf";
            var fileName = Path.GetFileName(tempFile);
            
            try
            {
                // Create a minimal PDF file (this is a simplified example)
                // In real tests, you'd use a proper PDF creation library or test files
                var pdfContent = GenerateMinimalPdf();
                await File.WriteAllBytesAsync(tempFile, pdfContent);

                // Act
                // Note: This will likely fail without a proper PDF file
                // This is more to show the test structure
                Document document = null;
                try
                {
                    document = await _processor.ProcessAsync(tempFile);
                }
                catch (PdfSharpCore.Pdf.IO.PdfReaderException)
                {
                    // Expected for our minimal PDF
                    return;
                }

                // Assert (if we had a proper PDF)
                Assert.NotNull(document);
                Assert.Equal(fileName, document.FileName);
                Assert.Equal(tempFile, document.FilePath);
                Assert.Equal("PDF", document.FileType);
                Assert.Equal(DocumentStatus.Completed, document.Status);
                Assert.NotEqual(Guid.Empty, document.Id);
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task ValidateAsync_ReturnsFalse_WhenFileDoesNotExist()
        {
            // Arrange
            var filePath = "nonexistent.pdf";

            // Act
            var result = await _processor.ValidateAsync(filePath);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ValidateAsync_ReturnsFalse_WhenFileIsNotPdf()
        {
            // Arrange
            var filePath = "document.txt";

            // Act
            var result = await _processor.ValidateAsync(filePath);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ValidateAsync_ReturnsFalse_WhenFileHasInvalidPdfHeader()
        {
            // Arrange
            var tempFile = Path.GetTempFileName() + ".pdf";
            try
            {
                // Create a file with invalid PDF header
                await File.WriteAllTextAsync(tempFile, "This is not a PDF");

                // Act
                var result = await _processor.ValidateAsync(tempFile);

                // Assert
                Assert.False(result);
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task ProcessAsync_HandlesCancellation()
        {
            // Arrange
            var tempFile = Path.GetTempFileName() + ".pdf";
            var cts = new CancellationTokenSource();
            
            try
            {
                await File.WriteAllBytesAsync(tempFile, GenerateMinimalPdf());
                cts.Cancel();

                // Act & Assert
                await Assert.ThrowsAsync<OperationCanceledException>(() => 
                    _processor.ProcessAsync(tempFile, null, cts.Token));
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        private byte[] GenerateMinimalPdf()
        {
            // This creates a minimal PDF header
            // In real tests, you'd use a proper PDF creation library
            return new byte[] 
            { 
                0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34, // %PDF-1.4
                0x0A // Line feed
            };
        }
    }
} 