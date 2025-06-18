using AnomaliImportTool.Core.Application.Interfaces.Services;
using AnomaliImportTool.Core.Domain.SharedKernel.Primitives;
using AnomaliImportTool.Core.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace AnomaliImportTool.Infrastructure.DocumentProcessing.Strategies;

/// <summary>
/// PDF document processing strategy implementation.
/// Demonstrates the Open/Closed Principle - new document types can be added without modifying this class.
/// </summary>
public sealed class PdfDocumentProcessingStrategy : IDocumentProcessingStrategy
{
    private readonly ILogger<PdfDocumentProcessingStrategy> _logger;
    
    public PdfDocumentProcessingStrategy(ILogger<PdfDocumentProcessingStrategy> logger)
    {
        _logger = logger;
    }
    
    /// <inheritdoc />
    public IReadOnlyList<string> SupportedExtensions { get; } = new[] { ".pdf" }.AsReadOnly();
    
    /// <inheritdoc />
    public int Priority => 100; // High priority for PDF files
    
    /// <inheritdoc />
    public bool CanProcess(FilePath filePath)
    {
        var extension = Path.GetExtension(filePath.Value).ToLowerInvariant();
        var canProcess = SupportedExtensions.Contains(extension);
        
        _logger.LogDebug("PDF strategy can process {FilePath}: {CanProcess}", filePath.Value, canProcess);
        return canProcess;
    }
    
    /// <inheritdoc />
    public async Task<DocumentProcessingResult> ProcessAsync(FilePath filePath, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var errors = new List<ProcessingError>();
        var warnings = new List<ProcessingWarning>();
        
        try
        {
            _logger.LogInformation("Starting PDF processing for file: {FilePath}", filePath.Value);
            
            // Validate file exists and is accessible
            if (!File.Exists(filePath.Value))
            {
                errors.Add(new ProcessingError("PDF001", "File not found", filePath.Value, null));
                return CreateFailureResult(errors, warnings, startTime);
            }
            
            // Check file size (example: max 50MB)
            var fileInfo = new FileInfo(filePath.Value);
            if (fileInfo.Length > 50 * 1024 * 1024)
            {
                warnings.Add(new ProcessingWarning("PDF002", "Large file size detected", 
                    $"File size: {fileInfo.Length / (1024 * 1024)}MB"));
            }
            
            // Simulate PDF text extraction
            var content = await ExtractPdfContentAsync(filePath, cancellationToken);
            var metadata = await ExtractPdfMetadataAsync(filePath, cancellationToken);
            
            var processingTime = DateTime.UtcNow - startTime;
            
            _logger.LogInformation("Successfully processed PDF file: {FilePath} in {ProcessingTime}ms", 
                filePath.Value, processingTime.TotalMilliseconds);
            
            return new DocumentProcessingResult(
                IsSuccess: true,
                Content: content,
                Metadata: metadata,
                Errors: errors.AsReadOnly(),
                Warnings: warnings.AsReadOnly(),
                ProcessingTime: processingTime
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PDF file: {FilePath}", filePath.Value);
            errors.Add(new ProcessingError("PDF999", "Unexpected error during PDF processing", ex.StackTrace, ex));
            return CreateFailureResult(errors, warnings, startTime);
        }
    }
    
    /// <inheritdoc />
    public async Task<DocumentValidationResult> ValidateAsync(FilePath filePath, CancellationToken cancellationToken = default)
    {
        var errors = new List<ValidationError>();
        var warnings = new List<ValidationWarning>();
        
        try
        {
            // Check file extension
            if (!CanProcess(filePath))
            {
                errors.Add(new ValidationError("PDF_VAL001", "Unsupported file extension for PDF strategy", null, null));
            }
            
            // Check file exists
            if (!File.Exists(filePath.Value))
            {
                errors.Add(new ValidationError("PDF_VAL002", "File does not exist", null, null));
                return new DocumentValidationResult(false, errors.AsReadOnly(), warnings.AsReadOnly());
            }
            
            // Check file is not empty
            var fileInfo = new FileInfo(filePath.Value);
            if (fileInfo.Length == 0)
            {
                errors.Add(new ValidationError("PDF_VAL003", "File is empty", null, null));
            }
            
            // Check PDF magic number (simplified validation)
            var buffer = new byte[4];
            using var stream = File.OpenRead(filePath.Value);
            await stream.ReadAsync(buffer, 0, 4, cancellationToken);
            
            // PDF files start with "%PDF"
            var pdfHeader = System.Text.Encoding.ASCII.GetString(buffer);
            if (!pdfHeader.StartsWith("%PDF"))
            {
                errors.Add(new ValidationError("PDF_VAL004", "File does not appear to be a valid PDF", null, null));
            }
            
            // Check file size warnings
            if (fileInfo.Length > 10 * 1024 * 1024) // 10MB
            {
                warnings.Add(new ValidationWarning("PDF_VAL005", 
                    $"Large PDF file detected: {fileInfo.Length / (1024 * 1024)}MB", null, null));
            }
            
            var isValid = errors.Count == 0;
            _logger.LogDebug("PDF validation for {FilePath}: {IsValid}, Errors: {ErrorCount}, Warnings: {WarningCount}", 
                filePath.Value, isValid, errors.Count, warnings.Count);
            
            return new DocumentValidationResult(isValid, errors.AsReadOnly(), warnings.AsReadOnly());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating PDF file: {FilePath}", filePath.Value);
            errors.Add(new ValidationError("PDF_VAL999", $"Validation error: {ex.Message}", null, null));
            return new DocumentValidationResult(false, errors.AsReadOnly(), warnings.AsReadOnly());
        }
    }
    
    private async Task<string> ExtractPdfContentAsync(FilePath filePath, CancellationToken cancellationToken)
    {
        // Simulate PDF text extraction
        await Task.Delay(100, cancellationToken); // Simulate processing time
        
        return $"[Extracted PDF Content from {Path.GetFileName(filePath.Value)}]\n" +
               "This is simulated PDF text content that would be extracted by a real PDF library.\n" +
               "In a real implementation, this would use libraries like iTextSharp, PdfPig, or similar.\n" +
               $"Processed at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";
    }
    
    private async Task<DocumentMetadata> ExtractPdfMetadataAsync(FilePath filePath, CancellationToken cancellationToken)
    {
        // Simulate metadata extraction
        await Task.Delay(50, cancellationToken);
        
        var fileInfo = new FileInfo(filePath.Value);
        var customProperties = new Dictionary<string, object>
        {
            ["PdfVersion"] = "1.4",
            ["IsEncrypted"] = false,
            ["HasFormFields"] = false,
            ["ProcessingStrategy"] = nameof(PdfDocumentProcessingStrategy)
        };
        
        return new DocumentMetadata(
            FileName: fileInfo.Name,
            FileSize: fileInfo.Length,
            CreatedDate: fileInfo.CreationTimeUtc,
            ModifiedDate: fileInfo.LastWriteTimeUtc,
            Author: "Unknown", // Would be extracted from PDF metadata
            Title: Path.GetFileNameWithoutExtension(fileInfo.Name),
            Subject: null,
            PageCount: 1, // Would be extracted from PDF
            MimeType: "application/pdf",
            CustomProperties: customProperties
        );
    }
    
    private DocumentProcessingResult CreateFailureResult(
        List<ProcessingError> errors, 
        List<ProcessingWarning> warnings, 
        DateTime startTime)
    {
        return new DocumentProcessingResult(
            IsSuccess: false,
            Content: string.Empty,
            Metadata: new DocumentMetadata(
                FileName: string.Empty,
                FileSize: 0,
                CreatedDate: DateTime.MinValue,
                ModifiedDate: DateTime.MinValue,
                Author: null,
                Title: null,
                Subject: null,
                PageCount: 0,
                MimeType: string.Empty,
                CustomProperties: new Dictionary<string, object>()
            ),
            Errors: errors.AsReadOnly(),
            Warnings: warnings.AsReadOnly(),
            ProcessingTime: DateTime.UtcNow - startTime
        );
    }
} 