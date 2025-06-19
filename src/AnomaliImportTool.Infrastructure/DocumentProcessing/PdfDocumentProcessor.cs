using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AnomaliImportTool.Core.Interfaces;
using AnomaliImportTool.Core.Models;
using Microsoft.Extensions.Logging;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.Content;
using PdfSharpCore.Pdf.Content.Objects;
using PdfSharpCore.Pdf.IO;
using PdfSharpCore.Drawing;

namespace AnomaliImportTool.Infrastructure.DocumentProcessing
{
    /// <summary>
    /// Processes PDF documents to extract text content and metadata.
    /// </summary>
    public class PdfDocumentProcessor : IDocumentProcessor
    {
        private readonly ILogger<PdfDocumentProcessor> _logger;
        private readonly ILogger<OcrProcessor> _ocrLogger;
        private readonly ProcessingOptions _defaultOptions;
        private OcrProcessor _ocrProcessor;

        public PdfDocumentProcessor(ILogger<PdfDocumentProcessor> logger, ILogger<OcrProcessor> ocrLogger = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _ocrLogger = ocrLogger;
            _defaultOptions = new ProcessingOptions();
        }

        /// <summary>
        /// Gets the supported file extensions for this processor.
        /// </summary>
        public string[] SupportedExtensions => new[] { ".pdf" };

        /// <summary>
        /// Determines if this processor can handle the specified file.
        /// </summary>
        /// <param name="filePath">The path to the file.</param>
        /// <returns>True if the processor can handle the file; otherwise, false.</returns>
        public bool CanProcess(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return false;

            var extension = Path.GetExtension(filePath)?.ToLowerInvariant();
            return extension == ".pdf";
        }

        /// <summary>
        /// Processes a document and extracts its content (interface implementation).
        /// </summary>
        /// <param name="filePath">Path to the file to process</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Processed document with extracted content</returns>
        public async Task<Document> ProcessDocumentAsync(string filePath, CancellationToken cancellationToken = default)
        {
            return await ProcessAsync(filePath, _defaultOptions, cancellationToken);
        }

        /// <summary>
        /// Processes a PDF document asynchronously.
        /// </summary>
        /// <param name="filePath">The path to the PDF file.</param>
        /// <param name="options">Processing options. If null, default options will be used.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The processed document.</returns>
        public async Task<Document> ProcessAsync(string filePath, ProcessingOptions options = null, CancellationToken cancellationToken = default)
        {
            if (!CanProcess(filePath))
            {
                throw new ArgumentException($"File '{filePath}' is not a supported PDF file.", nameof(filePath));
            }

            options ??= _defaultOptions;
            _logger.LogInformation("Starting PDF processing for file: {FilePath}", filePath);

            // Initialize OCR if enabled
            if (options.EnableOcr && _ocrLogger != null)
            {
                _ocrProcessor ??= new OcrProcessor(_ocrLogger);
            }

            var document = new Document
            {
                Id = Guid.NewGuid(),
                FileName = Path.GetFileName(filePath),
                FilePath = filePath,
                FileType = "PDF",
                ProcessingStartTime = DateTime.UtcNow,
                Status = DocumentStatus.Processing
            };

            try
            {
                // Validate file exists
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"File not found: {filePath}", filePath);
                }

                // Check file size
                var fileInfo = new FileInfo(filePath);
                document.FileSizeBytes = fileInfo.Length;

                if (fileInfo.Length > options.MaxFileSizeMB * 1024 * 1024)
                {
                    throw new InvalidOperationException($"File size ({fileInfo.Length / 1024 / 1024} MB) exceeds maximum allowed size ({options.MaxFileSizeMB} MB).");
                }

                // Extract text and metadata
                using (var pdfDocument = PdfReader.Open(filePath, PdfDocumentOpenMode.ReadOnly))
                {
                    // Extract metadata
                    if (options.ExtractMetadata)
                    {
                        ExtractMetadata(pdfDocument, document);
                    }

                    // Extract text content
                    if (options.ExtractTextContent)
                    {
                        var extractedText = await ExtractTextAsync(pdfDocument, options, cancellationToken);
                        
                        // If no text extracted and OCR is enabled, try OCR
                        if (string.IsNullOrWhiteSpace(extractedText) && options.EnableOcr && _ocrProcessor != null)
                        {
                            _logger.LogInformation("No text found in PDF, attempting OCR extraction");
                            extractedText = await ExtractTextWithOcrAsync(pdfDocument, options, cancellationToken);
                            document.IsScanned = true;
                        }
                        
                        document.ExtractedText = extractedText;
                        document.ExtractedTextLength = extractedText?.Length ?? 0;
                    }

                    // Set page count
                    document.PageCount = pdfDocument.PageCount;
                }

                // Auto-detect TLP if enabled
                if (options.AutoDetectTlp && !string.IsNullOrEmpty(document.ExtractedText))
                {
                    document.TlpDesignation = DetectTlpDesignation(document.ExtractedText);
                }
                else
                {
                    document.TlpDesignation = options.DefaultTlpDesignation;
                }

                document.ProcessingEndTime = DateTime.UtcNow;
                document.Status = DocumentStatus.Completed;
                
                _logger.LogInformation("PDF processing completed for file: {FilePath}. Pages: {PageCount}, Text length: {TextLength}, Scanned: {IsScanned}", 
                    filePath, document.PageCount, document.ExtractedTextLength, document.IsScanned);

                return document;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("PDF processing cancelled for file: {FilePath}", filePath);
                document.Status = DocumentStatus.Failed;
                document.ErrorMessage = "Processing was cancelled.";
                document.ProcessingEndTime = DateTime.UtcNow;
                throw;
            }
            catch (PdfReaderException ex) when (ex.Message.Contains("password"))
            {
                _logger.LogWarning("PDF file is password protected: {FilePath}", filePath);
                document.Status = DocumentStatus.Failed;
                document.ErrorMessage = "PDF is password protected.";
                document.ProcessingEndTime = DateTime.UtcNow;
                
                if (!options.SkipPasswordProtected && !string.IsNullOrEmpty(options.DefaultPassword))
                {
                    // Try with default password
                    return await ProcessPasswordProtectedPdfAsync(filePath, options, cancellationToken);
                }
                
                throw new InvalidOperationException("PDF is password protected and no password was provided.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing PDF file: {FilePath}", filePath);
                document.Status = DocumentStatus.Failed;
                document.ErrorMessage = ex.Message;
                document.ProcessingEndTime = DateTime.UtcNow;
                throw;
            }
            finally
            {
                // Dispose OCR processor if we created it
                if (options.EnableOcr && _ocrProcessor != null && _ocrLogger != null)
                {
                    _ocrProcessor.Dispose();
                    _ocrProcessor = null;
                }
            }
        }

        /// <summary>
        /// Validates a PDF file without fully processing it.
        /// </summary>
        /// <param name="filePath">The path to the PDF file.</param>
        /// <returns>True if the file is valid; otherwise, false.</returns>
        public async Task<bool> ValidateAsync(string filePath)
        {
            try
            {
                if (!CanProcess(filePath))
                    return false;

                if (!File.Exists(filePath))
                    return false;

                // Check PDF header
                using (var fileStream = File.OpenRead(filePath))
                {
                    var buffer = new byte[5];
                    var bytesRead = await fileStream.ReadAsync(buffer, 0, 5);
                    
                    if (bytesRead < 5)
                        return false;

                    var header = Encoding.ASCII.GetString(buffer);
                    if (!header.StartsWith("%PDF-"))
                        return false;
                }

                // Try to open with PDFSharp to validate structure
                using (var pdfDocument = PdfReader.Open(filePath, PdfDocumentOpenMode.ReadOnly))
                {
                    // If we can open it and it has at least one page, it's valid
                    return pdfDocument.PageCount > 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating PDF file: {FilePath}", filePath);
                return false;
            }
        }

        private void ExtractMetadata(PdfDocument pdfDocument, Document document)
        {
            try
            {
                var info = pdfDocument.Info;
                
                if (!string.IsNullOrEmpty(info.Title))
                    document.Title = info.Title;
                
                if (!string.IsNullOrEmpty(info.Author))
                    document.Author = info.Author;
                
                if (!string.IsNullOrEmpty(info.Subject))
                    document.Subject = info.Subject;
                
                if (!string.IsNullOrEmpty(info.Keywords))
                    document.Keywords = info.Keywords;
                
                if (info.CreationDate != DateTime.MinValue)
                    document.CreationDate = info.CreationDate;
                
                if (info.ModificationDate != DateTime.MinValue)
                    document.ModificationDate = info.ModificationDate;
                
                // Additional metadata
                document.Producer = info.Producer;
                document.Creator = info.Creator;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error extracting PDF metadata");
            }
        }

        private async Task<string> ExtractTextAsync(PdfDocument pdfDocument, ProcessingOptions options, CancellationToken cancellationToken)
        {
            var textBuilder = new StringBuilder();
            var maxLength = options.MaxTextContentLength;

            try
            {
                for (int pageIndex = 0; pageIndex < pdfDocument.PageCount; pageIndex++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var page = pdfDocument.Pages[pageIndex];
                    var pageText = ExtractTextFromPage(page);

                    if (!string.IsNullOrWhiteSpace(pageText))
                    {
                        if (textBuilder.Length > 0)
                            textBuilder.AppendLine();
                        
                        textBuilder.AppendLine($"--- Page {pageIndex + 1} ---");
                        textBuilder.AppendLine(pageText);

                        // Check if we've exceeded max length
                        if (textBuilder.Length > maxLength)
                        {
                            _logger.LogWarning("Text extraction truncated at {MaxLength} characters", maxLength);
                            return textBuilder.ToString(0, maxLength);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error extracting text from PDF pages");
            }

            return textBuilder.ToString();
        }

        private string ExtractTextFromPage(PdfPage page)
        {
            var content = ContentReader.ReadContent(page);
            var text = new StringBuilder();

            ExtractTextFromContent(content, text);

            return text.ToString();
        }

        private void ExtractTextFromContent(CObject obj, StringBuilder text)
        {
            if (obj is COperator op)
            {
                if (op.OpCode.Name == OpCodeName.Tj.ToString() || op.OpCode.Name == OpCodeName.TJ.ToString())
                {
                    foreach (var operand in op.Operands)
                    {
                        ExtractTextFromOperand(operand, text);
                    }
                }
            }
            else if (obj is CSequence sequence)
            {
                foreach (var item in sequence)
                {
                    ExtractTextFromContent(item, text);
                }
            }
        }

        private void ExtractTextFromOperand(CObject operand, StringBuilder text)
        {
            if (operand is CString cString)
            {
                text.Append(cString.Value);
            }
            else if (operand is CArray array)
            {
                foreach (var item in array)
                {
                    if (item is CString str)
                    {
                        text.Append(str.Value);
                    }
                }
            }
        }

        private async Task<string> ExtractTextWithOcrAsync(PdfDocument pdfDocument, ProcessingOptions options, CancellationToken cancellationToken)
        {
            var textBuilder = new StringBuilder();
            var maxLength = options.MaxTextContentLength;

            try
            {
                if (!_ocrProcessor.IsOcrAvailable())
                {
                    _logger.LogWarning("OCR is not available. Tesseract data files may be missing.");
                    return string.Empty;
                }

                for (int pageIndex = 0; pageIndex < pdfDocument.PageCount; pageIndex++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    _logger.LogDebug("Performing OCR on page {PageNumber} of {TotalPages}", pageIndex + 1, pdfDocument.PageCount);

                    try
                    {
                        // Convert PDF page to image for OCR
                        // Note: This is a simplified implementation. In production, you'd need
                        // a PDF rendering library to convert pages to images
                        var pageText = await PerformOcrOnPageAsync(pdfDocument.Pages[pageIndex], options);

                        if (!string.IsNullOrWhiteSpace(pageText))
                        {
                            if (textBuilder.Length > 0)
                                textBuilder.AppendLine();

                            textBuilder.AppendLine($"--- Page {pageIndex + 1} (OCR) ---");
                            textBuilder.AppendLine(pageText);

                            // Check if we've exceeded max length
                            if (textBuilder.Length > maxLength)
                            {
                                _logger.LogWarning("OCR text extraction truncated at {MaxLength} characters", maxLength);
                                return textBuilder.ToString(0, maxLength);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error performing OCR on page {PageNumber}", pageIndex + 1);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing OCR on PDF");
            }

            return textBuilder.ToString();
        }

        private async Task<string> PerformOcrOnPageAsync(PdfPage page, ProcessingOptions options)
        {
            // Note: This is a placeholder implementation
            // In a real implementation, you would:
            // 1. Render the PDF page to an image using a library like PDFium or Ghostscript
            // 2. Pass the image bytes to the OCR processor
            // 3. Return the extracted text

            _logger.LogWarning("PDF to image conversion not implemented. OCR skipped for this page.");
            return string.Empty;

            // Example of what the real implementation would look like:
            /*
            // Render page to image
            var imageBytes = RenderPageToImage(page, 300); // 300 DPI
            
            // Perform OCR
            var ocrResult = await _ocrProcessor.ProcessImageAsync(
                imageBytes, 
                options.OcrLanguage, 
                options.OcrMinConfidence * 100);
            
            if (ocrResult.Success)
            {
                return ocrResult.Text;
            }
            else
            {
                _logger.LogWarning("OCR failed: {Error}", ocrResult.ErrorMessage);
                return string.Empty;
            }
            */
        }

        private TlpDesignation DetectTlpDesignation(string text)
        {
            var lowerText = text.ToLowerInvariant();

            if (lowerText.Contains("tlp:red") || lowerText.Contains("tlp red"))
                return TlpDesignation.Red;
            
            if (lowerText.Contains("tlp:amber") || lowerText.Contains("tlp amber"))
                return TlpDesignation.Amber;
            
            if (lowerText.Contains("tlp:green") || lowerText.Contains("tlp green"))
                return TlpDesignation.Green;
            
            if (lowerText.Contains("tlp:white") || lowerText.Contains("tlp white") || lowerText.Contains("tlp:clear") || lowerText.Contains("tlp clear"))
                return TlpDesignation.Clear;

            // Default to Amber if not specified
            return TlpDesignation.Amber;
        }

        private async Task<Document> ProcessPasswordProtectedPdfAsync(string filePath, ProcessingOptions options, CancellationToken cancellationToken)
        {
            try
            {
                using (var pdfDocument = PdfReader.Open(filePath, options.DefaultPassword, PdfDocumentOpenMode.ReadOnly))
                {
                    // Re-run the processing with the opened document
                    // This is simplified - in a real implementation, you'd refactor to avoid duplication
                    _logger.LogInformation("Successfully opened password-protected PDF with default password");
                    
                    var document = new Document
                    {
                        Id = Guid.NewGuid(),
                        FileName = Path.GetFileName(filePath),
                        FilePath = filePath,
                        FileType = "PDF",
                        ProcessingStartTime = DateTime.UtcNow,
                        Status = DocumentStatus.Processing,
                        IsPasswordProtected = true
                    };

                    // Process as normal...
                    // (Implementation details omitted for brevity)
                    
                    return document;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open password-protected PDF with default password");
                throw new InvalidOperationException("Failed to open password-protected PDF with the provided password.", ex);
            }
        }
    }
} 