using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AnomaliImportTool.Core.Interfaces;
using AnomaliImportTool.Core.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Extensions.Logging;

namespace AnomaliImportTool.Infrastructure.DocumentProcessing
{
    /// <summary>
    /// Processes Excel documents (.xls, .xlsx) to extract text content and metadata.
    /// </summary>
    public class ExcelDocumentProcessor : IDocumentProcessor
    {
        private readonly ILogger<ExcelDocumentProcessor> _logger;
        private readonly ProcessingOptions _defaultOptions;

        public ExcelDocumentProcessor(ILogger<ExcelDocumentProcessor> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _defaultOptions = new ProcessingOptions();
        }

        /// <summary>
        /// Gets the supported file extensions for this processor.
        /// </summary>
        public string[] SupportedExtensions => new[] { ".xlsx", ".xls" };

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
            return extension == ".xlsx" || extension == ".xls";
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
        /// Processes an Excel document asynchronously.
        /// </summary>
        /// <param name="filePath">The path to the Excel file.</param>
        /// <param name="options">Processing options. If null, default options will be used.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The processed document.</returns>
        public async Task<Document> ProcessAsync(string filePath, ProcessingOptions options = null, CancellationToken cancellationToken = default)
        {
            if (!CanProcess(filePath))
            {
                throw new ArgumentException($"File '{filePath}' is not a supported Excel file.", nameof(filePath));
            }

            options ??= _defaultOptions;
            _logger.LogInformation("Starting Excel processing for file: {FilePath}", filePath);

            var document = new Document
            {
                Id = Guid.NewGuid(),
                FileName = Path.GetFileName(filePath),
                FilePath = filePath,
                FileType = "Excel",
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
                
                if (extension == ".xlsx")
                {
                    await ProcessXlsxAsync(document, filePath, options, cancellationToken);
                }
                else if (extension == ".xls")
                {
                    await ProcessLegacyXlsAsync(document, filePath, options, cancellationToken);
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
                
                _logger.LogInformation("Excel processing completed for file: {FilePath}. Text length: {TextLength}", 
                    filePath, document.ExtractedTextLength);

                return document;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Excel processing cancelled for file: {FilePath}", filePath);
                document.Status = DocumentStatus.Failed;
                document.ErrorMessage = "Processing was cancelled.";
                document.ProcessingEndTime = DateTime.UtcNow;
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Excel file: {FilePath}", filePath);
                document.Status = DocumentStatus.Failed;
                document.ErrorMessage = ex.Message;
                document.ProcessingEndTime = DateTime.UtcNow;
                throw;
            }
        }

        /// <summary>
        /// Validates an Excel file without fully processing it.
        /// </summary>
        /// <param name="filePath">The path to the Excel file.</param>
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
                
                if (extension == ".xlsx")
                {
                    // Try to open as OpenXML spreadsheet
                    using (var spreadsheet = SpreadsheetDocument.Open(filePath, false))
                    {
                        // If we can open it and it has a workbook part, it's valid
                        return spreadsheet.WorkbookPart != null;
                    }
                }
                else if (extension == ".xls")
                {
                    // For legacy .xls files, just check if it's not empty
                    var fileInfo = new FileInfo(filePath);
                    return fileInfo.Length > 0;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating Excel file: {FilePath}", filePath);
                return false;
            }
        }

        private async Task ProcessXlsxAsync(Document document, string filePath, ProcessingOptions options, CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                using (var spreadsheet = SpreadsheetDocument.Open(filePath, false))
                {
                    // Extract metadata
                    if (options.ExtractMetadata)
                    {
                        ExtractMetadata(spreadsheet, document);
                    }

                    // Extract text content
                    if (options.ExtractTextContent)
                    {
                        var text = ExtractText(spreadsheet, options, cancellationToken);
                        document.ExtractedText = text;
                        document.ExtractedTextLength = text?.Length ?? 0;
                    }

                    // Count sheets as pages
                    var workbookPart = spreadsheet.WorkbookPart;
                    if (workbookPart?.Workbook?.Sheets != null)
                    {
                        document.PageCount = workbookPart.Workbook.Sheets.Count();
                    }
                }
            }, cancellationToken);
        }

        private async Task ProcessLegacyXlsAsync(Document document, string filePath, ProcessingOptions options, CancellationToken cancellationToken)
        {
            // Legacy .xls format handling
            // In a real implementation, you would use a library like NPOI or convert to .xlsx first
            _logger.LogWarning("Legacy .xls format detected. Limited processing available.");
            
            document.ExtractedText = "Legacy .xls format - conversion required for full data extraction.";
            document.ExtractedTextLength = document.ExtractedText.Length;
            document.PageCount = 0; // Unknown for legacy format without proper parsing
            
            await Task.CompletedTask;
        }

        private void ExtractMetadata(SpreadsheetDocument spreadsheet, Document document)
        {
            try
            {
                var coreProps = spreadsheet.PackageProperties;
                
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
                var extProps = spreadsheet.ExtendedFilePropertiesPart;
                if (extProps != null && extProps.Properties != null)
                {
                    var props = extProps.Properties;
                    if (props.Application != null)
                        document.Producer = props.Application.Text;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error extracting Excel metadata");
            }
        }

        private string ExtractText(SpreadsheetDocument spreadsheet, ProcessingOptions options, CancellationToken cancellationToken)
        {
            var textBuilder = new StringBuilder();
            var maxLength = options.MaxTextContentLength;

            try
            {
                var workbookPart = spreadsheet.WorkbookPart;
                if (workbookPart == null)
                    return string.Empty;

                var sheets = workbookPart.Workbook.Descendants<Sheet>();
                var sharedStringTable = workbookPart.SharedStringTablePart?.SharedStringTable;

                foreach (var sheet in sheets)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (string.IsNullOrEmpty(sheet.Id?.Value))
                        continue;

                    var worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id.Value);
                    if (worksheetPart?.Worksheet == null)
                        continue;

                    textBuilder.AppendLine($"--- Sheet: {sheet.Name} ---");

                    var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();
                    if (sheetData == null)
                        continue;

                    foreach (var row in sheetData.Descendants<Row>())
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var rowText = ExtractRowText(row, sharedStringTable, options.PreserveFormatting);
                        
                        if (!string.IsNullOrWhiteSpace(rowText))
                        {
                            textBuilder.AppendLine(rowText);
                            
                            // Check if we've exceeded max length
                            if (textBuilder.Length > maxLength)
                            {
                                _logger.LogWarning("Text extraction truncated at {MaxLength} characters", maxLength);
                                return textBuilder.ToString(0, maxLength);
                            }
                        }
                    }

                    textBuilder.AppendLine(); // Empty line between sheets
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error extracting text from Excel document");
            }

            return textBuilder.ToString();
        }

        private string ExtractRowText(Row row, SharedStringTable sharedStringTable, bool preserveFormatting)
        {
            var cellValues = new List<string>();

            foreach (var cell in row.Descendants<Cell>())
            {
                var cellValue = GetCellValue(cell, sharedStringTable);
                
                if (!string.IsNullOrEmpty(cellValue))
                {
                    cellValues.Add(cellValue);
                }
            }

            if (cellValues.Count == 0)
                return string.Empty;

            if (preserveFormatting)
            {
                // Format as table row
                return "| " + string.Join(" | ", cellValues) + " |";
            }
            else
            {
                // Tab-separated values
                return string.Join("\t", cellValues);
            }
        }

        private string GetCellValue(Cell cell, SharedStringTable sharedStringTable)
        {
            if (cell.CellValue == null)
                return string.Empty;

            var value = cell.CellValue.InnerText;

            // If the cell contains a shared string, look it up
            if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
            {
                if (sharedStringTable != null && int.TryParse(value, out int index))
                {
                    var sharedStringItem = sharedStringTable.Elements<SharedStringItem>().ElementAtOrDefault(index);
                    if (sharedStringItem != null)
                    {
                        return sharedStringItem.InnerText;
                    }
                }
            }
            // Handle boolean values
            else if (cell.DataType != null && cell.DataType.Value == CellValues.Boolean)
            {
                return value == "1" ? "TRUE" : "FALSE";
            }
            // Handle dates (stored as numbers in Excel)
            else if (cell.StyleIndex != null && IsDateFormat(cell))
            {
                if (double.TryParse(value, out double oaDate))
                {
                    try
                    {
                        var dateTime = DateTime.FromOADate(oaDate);
                        return dateTime.ToString("yyyy-MM-dd");
                    }
                    catch
                    {
                        // If conversion fails, return the raw value
                        return value;
                    }
                }
            }

            return value;
        }

        private bool IsDateFormat(Cell cell)
        {
            // This is a simplified check - in a real implementation,
            // you would check the cell's style and number format
            return false;
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