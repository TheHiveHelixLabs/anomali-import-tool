using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AnomaliImportTool.Core.Interfaces;
using AnomaliImportTool.Core.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml;
using Microsoft.Extensions.Logging;
using CoreDocument = AnomaliImportTool.Core.Models.Document;
using OpenXmlDocument = DocumentFormat.OpenXml.Wordprocessing.Document;

namespace AnomaliImportTool.Infrastructure.DocumentProcessing
{
    /// <summary>
    /// Processes Word documents (.doc, .docx) to extract text content and metadata.
    /// </summary>
    public class WordDocumentProcessor : IDocumentProcessor
    {
        private readonly ILogger<WordDocumentProcessor> _logger;
        private readonly ProcessingOptions _defaultOptions;

        public WordDocumentProcessor(ILogger<WordDocumentProcessor> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _defaultOptions = new ProcessingOptions();
        }

        /// <summary>
        /// Gets the supported file extensions for this processor.
        /// </summary>
        public string[] SupportedExtensions => new[] { ".docx", ".doc" };

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
            return extension == ".docx" || extension == ".doc";
        }

        /// <summary>
        /// Processes a document and extracts its content (interface implementation).
        /// </summary>
        /// <param name="filePath">Path to the file to process</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Processed document with extracted content</returns>
        public async Task<AnomaliImportTool.Core.Models.Document> ProcessDocumentAsync(string filePath, CancellationToken cancellationToken = default)
        {
            return await ProcessAsync(filePath, _defaultOptions, cancellationToken);
        }

        /// <summary>
        /// Processes a Word document asynchronously.
        /// </summary>
        /// <param name="filePath">The path to the Word file.</param>
        /// <param name="options">Processing options. If null, default options will be used.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The processed document.</returns>
        public async Task<AnomaliImportTool.Core.Models.Document> ProcessAsync(string filePath, ProcessingOptions options = null, CancellationToken cancellationToken = default)
        {
            if (!CanProcess(filePath))
            {
                throw new ArgumentException($"File '{filePath}' is not a supported Word file.", nameof(filePath));
            }

            options ??= _defaultOptions;
            _logger.LogInformation("Starting Word processing for file: {FilePath}", filePath);

            var document = new CoreDocument
            {
                Id = Guid.NewGuid(),
                FileName = Path.GetFileName(filePath),
                FilePath = filePath,
                FileType = "Word",
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

                // Process based on file extension
                var extension = Path.GetExtension(filePath).ToLowerInvariant();
                
                if (extension == ".docx")
                {
                    await ProcessDocxAsync(document, filePath, options, cancellationToken);
                }
                else if (extension == ".doc")
                {
                    await ProcessLegacyDocAsync(document, filePath, options, cancellationToken);
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
                
                _logger.LogInformation("Word processing completed for file: {FilePath}. Text length: {TextLength}", 
                    filePath, document.ExtractedTextLength);

                return document;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Word processing cancelled for file: {FilePath}", filePath);
                document.Status = DocumentStatus.Failed;
                document.ErrorMessage = "Processing was cancelled.";
                document.ProcessingEndTime = DateTime.UtcNow;
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Word file: {FilePath}", filePath);
                document.Status = DocumentStatus.Failed;
                document.ErrorMessage = ex.Message;
                document.ProcessingEndTime = DateTime.UtcNow;
                throw;
            }
        }

        /// <summary>
        /// Validates a Word file without fully processing it.
        /// </summary>
        /// <param name="filePath">The path to the Word file.</param>
        /// <returns>True if the file is valid; otherwise, false.</returns>
        public async Task<bool> ValidateAsync(string filePath)
        {
            try
            {
                if (!CanProcess(filePath))
                    return false;

                if (!File.Exists(filePath))
                    return false;

                var extension = Path.GetExtension(filePath).ToLowerInvariant();
                
                if (extension == ".docx")
                {
                    // Try to open as OpenXML document
                    using (var wordDoc = WordprocessingDocument.Open(filePath, false))
                    {
                        // If we can open it and it has a main document part, it's valid
                        return wordDoc.MainDocumentPart != null;
                    }
                }
                else if (extension == ".doc")
                {
                    // For legacy .doc files, just check if it's not empty
                    var fileInfo = new FileInfo(filePath);
                    return fileInfo.Length > 0;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating Word file: {FilePath}", filePath);
                return false;
            }
        }

        private async Task ProcessDocxAsync(CoreDocument document, string filePath, ProcessingOptions options, CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                using (var wordDoc = WordprocessingDocument.Open(filePath, false))
                {
                    // Extract metadata
                    if (options.ExtractMetadata)
                    {
                        ExtractMetadata(wordDoc, document);
                    }

                    // Extract text content
                    if (options.ExtractTextContent)
                    {
                        var text = ExtractText(wordDoc, options, cancellationToken);
                        document.ExtractedText = text;
                        document.ExtractedTextLength = text?.Length ?? 0;
                    }

                    // Count pages (approximation for Word docs)
                    document.PageCount = CountPages(wordDoc);
                }
            }, cancellationToken);
        }

        private async Task ProcessLegacyDocAsync(CoreDocument document, string filePath, ProcessingOptions options, CancellationToken cancellationToken)
        {
            // Legacy .doc format handling
            // In a real implementation, you would use a library like NPOI or convert to .docx first
            _logger.LogWarning("Legacy .doc format detected. Limited processing available.");
            
            document.ExtractedText = "Legacy .doc format - conversion required for full text extraction.";
            document.ExtractedTextLength = document.ExtractedText.Length;
            document.PageCount = 0; // Unknown for legacy format without proper parsing
            
            await Task.CompletedTask;
        }

        private void ExtractMetadata(WordprocessingDocument wordDoc, CoreDocument document)
        {
            try
            {
                var coreProps = wordDoc.PackageProperties;
                
                if (!string.IsNullOrEmpty(coreProps.Title))
                    document.Title = coreProps.Title;
                
                if (!string.IsNullOrEmpty(coreProps.Creator))
                    document.Author = coreProps.Creator;
                
                if (!string.IsNullOrEmpty(coreProps.Subject))
                    document.Subject = coreProps.Subject;
                
                if (!string.IsNullOrEmpty(coreProps.Keywords))
                    document.Keywords = coreProps.Keywords;
                
                if (coreProps.Created.HasValue)
                    document.CreationDate = coreProps.Created.Value;
                
                if (coreProps.Modified.HasValue)
                    document.ModificationDate = coreProps.Modified.Value;
                
                // Additional properties
                document.Creator = coreProps.Creator;
                
                // Extended properties
                var extProps = wordDoc.ExtendedFilePropertiesPart;
                if (extProps != null && extProps.Properties != null)
                {
                    var props = extProps.Properties;
                    if (props.Application != null)
                        document.Producer = props.Application.Text;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error extracting Word metadata");
            }
        }

        private string ExtractText(WordprocessingDocument wordDoc, ProcessingOptions options, CancellationToken cancellationToken)
        {
            var textBuilder = new StringBuilder();
            var maxLength = options.MaxTextContentLength;

            try
            {
                var body = wordDoc.MainDocumentPart?.Document?.Body;
                if (body == null)
                    return string.Empty;

                // Extract paragraphs
                foreach (var paragraph in body.Descendants<Paragraph>())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var paragraphText = ExtractParagraphText(paragraph, options.PreserveFormatting);
                    
                    if (!string.IsNullOrWhiteSpace(paragraphText))
                    {
                        textBuilder.AppendLine(paragraphText);
                        
                        // Check if we've exceeded max length
                        if (textBuilder.Length > maxLength)
                        {
                            _logger.LogWarning("Text extraction truncated at {MaxLength} characters", maxLength);
                            return textBuilder.ToString(0, maxLength);
                        }
                    }
                }

                // Extract tables
                foreach (var table in body.Descendants<Table>())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var tableText = ExtractTableText(table, options.PreserveFormatting);
                    
                    if (!string.IsNullOrWhiteSpace(tableText))
                    {
                        textBuilder.AppendLine();
                        textBuilder.AppendLine(tableText);
                        
                        // Check if we've exceeded max length
                        if (textBuilder.Length > maxLength)
                        {
                            _logger.LogWarning("Text extraction truncated at {MaxLength} characters", maxLength);
                            return textBuilder.ToString(0, maxLength);
                        }
                    }
                }

                // Extract headers and footers
                ExtractHeadersAndFooters(wordDoc, textBuilder, maxLength);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error extracting text from Word document");
            }

            return textBuilder.ToString();
        }

        private string ExtractParagraphText(Paragraph paragraph, bool preserveFormatting)
        {
            if (paragraph == null)
                return string.Empty;

            var textBuilder = new StringBuilder();

            foreach (var run in paragraph.Descendants<Run>())
            {
                var text = run.InnerText;
                
                if (!string.IsNullOrEmpty(text))
                {
                    if (preserveFormatting)
                    {
                        // Check for formatting like bold, italic, etc.
                        var runProps = run.RunProperties;
                        if (runProps != null)
                        {
                            if (runProps.Bold != null && runProps.Bold.Val)
                                text = $"**{text}**";
                            if (runProps.Italic != null && runProps.Italic.Val)
                                text = $"*{text}*";
                        }
                    }
                    
                    textBuilder.Append(text);
                }
            }

            return textBuilder.ToString();
        }

        private string ExtractTableText(Table table, bool preserveFormatting)
        {
            var textBuilder = new StringBuilder();

            if (preserveFormatting)
                textBuilder.AppendLine("[Table]");

            foreach (var row in table.Descendants<TableRow>())
            {
                var cellTexts = new List<string>();
                
                foreach (var cell in row.Descendants<TableCell>())
                {
                    var cellText = new StringBuilder();
                    foreach (var paragraph in cell.Descendants<Paragraph>())
                    {
                        var paragraphText = ExtractParagraphText(paragraph, false);
                        if (!string.IsNullOrWhiteSpace(paragraphText))
                            cellText.Append(paragraphText + " ");
                    }
                    cellTexts.Add(cellText.ToString().Trim());
                }

                if (cellTexts.Any(t => !string.IsNullOrWhiteSpace(t)))
                {
                    if (preserveFormatting)
                        textBuilder.AppendLine("| " + string.Join(" | ", cellTexts) + " |");
                    else
                        textBuilder.AppendLine(string.Join("\t", cellTexts));
                }
            }

            if (preserveFormatting)
                textBuilder.AppendLine("[/Table]");

            return textBuilder.ToString();
        }

        private void ExtractHeadersAndFooters(WordprocessingDocument wordDoc, StringBuilder textBuilder, int maxLength)
        {
            try
            {
                // Extract headers
                var headers = wordDoc.MainDocumentPart?.HeaderParts;
                if (headers != null)
                {
                    foreach (var headerPart in headers)
                    {
                        var headerText = ExtractTextFromPart(headerPart.Header);
                        if (!string.IsNullOrWhiteSpace(headerText))
                        {
                            textBuilder.AppendLine("--- Header ---");
                            textBuilder.AppendLine(headerText);
                        }
                    }
                }

                // Extract footers
                var footers = wordDoc.MainDocumentPart?.FooterParts;
                if (footers != null)
                {
                    foreach (var footerPart in footers)
                    {
                        var footerText = ExtractTextFromPart(footerPart.Footer);
                        if (!string.IsNullOrWhiteSpace(footerText))
                        {
                            textBuilder.AppendLine("--- Footer ---");
                            textBuilder.AppendLine(footerText);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error extracting headers/footers");
            }
        }

        private string ExtractTextFromPart(OpenXmlElement element)
        {
            if (element == null)
                return string.Empty;

            var textBuilder = new StringBuilder();
            
            foreach (var paragraph in element.Descendants<Paragraph>())
            {
                var paragraphText = ExtractParagraphText(paragraph, false);
                if (!string.IsNullOrWhiteSpace(paragraphText))
                    textBuilder.AppendLine(paragraphText);
            }

            return textBuilder.ToString();
        }

        private int CountPages(WordprocessingDocument wordDoc)
        {
            try
            {
                // Try to get page count from document properties
                var extProps = wordDoc.ExtendedFilePropertiesPart?.Properties;
                if (extProps?.Pages?.Text != null && int.TryParse(extProps.Pages.Text, out int pageCount))
                {
                    return pageCount;
                }

                // Fallback: count section properties (approximate)
                var sectionProperties = wordDoc.MainDocumentPart?.Document?.Body?.Descendants<SectionProperties>().Count() ?? 0;
                return Math.Max(1, sectionProperties);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error counting pages in Word document");
                return 0;
            }
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
    }
} 