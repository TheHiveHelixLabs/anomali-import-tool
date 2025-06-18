using AnomaliImportTool.Core.Application.Interfaces.Services;
using AnomaliImportTool.Core.Domain.SharedKernel.Primitives;
using AnomaliImportTool.Core.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace AnomaliImportTool.Infrastructure.DocumentProcessing.Strategies;

/// <summary>
/// Word document processing strategy implementation.
/// Demonstrates the Open/Closed Principle - this strategy can be added without modifying existing code.
/// </summary>
public sealed class WordDocumentProcessingStrategy : IDocumentProcessingStrategy
{
    private readonly ILogger<WordDocumentProcessingStrategy> _logger;
    
    public WordDocumentProcessingStrategy(ILogger<WordDocumentProcessingStrategy> logger)
    {
        _logger = logger;
    }
    
    /// <inheritdoc />
    public IReadOnlyList<string> SupportedExtensions { get; } = new[] { ".docx", ".doc" }.AsReadOnly();
    
    /// <inheritdoc />
    public int Priority => 90; // Slightly lower priority than PDF
    
    /// <inheritdoc />
    public bool CanProcess(FilePath filePath)
    {
        var extension = Path.GetExtension(filePath.Value).ToLowerInvariant();
        var canProcess = SupportedExtensions.Contains(extension);
        
        _logger.LogDebug("Word strategy can process {FilePath}: {CanProcess}", filePath.Value, canProcess);
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
            _logger.LogInformation("Starting Word document processing for file: {FilePath}", filePath.Value);
            
            // Validate file exists and is accessible
            if (!File.Exists(filePath.Value))
            {
                errors.Add(new ProcessingError("WORD001", "File not found", filePath.Value, null));
                return CreateFailureResult(errors, warnings, startTime);
            }
            
            var extension = Path.GetExtension(filePath.Value).ToLowerInvariant();
            
            // Check for legacy .doc format
            if (extension == ".doc")
            {
                warnings.Add(new ProcessingWarning("WORD002", "Legacy Word format detected", 
                    "Consider converting to .docx for better processing"));
            }
            
            // Check file size
            var fileInfo = new FileInfo(filePath.Value);
            if (fileInfo.Length > 25 * 1024 * 1024) // 25MB for Word docs
            {
                warnings.Add(new ProcessingWarning("WORD003", "Large Word document detected", 
                    $"File size: {fileInfo.Length / (1024 * 1024)}MB"));
            }
            
            // Simulate Word document processing
            var content = await ExtractWordContentAsync(filePath, cancellationToken);
            var metadata = await ExtractWordMetadataAsync(filePath, cancellationToken);
            
            var processingTime = DateTime.UtcNow - startTime;
            
            _logger.LogInformation("Successfully processed Word document: {FilePath} in {ProcessingTime}ms", 
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
            _logger.LogError(ex, "Error processing Word document: {FilePath}", filePath.Value);
            errors.Add(new ProcessingError("WORD999", "Unexpected error during Word processing", ex.StackTrace, ex));
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
                errors.Add(new ValidationError("WORD_VAL001", "Unsupported file extension for Word strategy", null, null));
            }
            
            // Check file exists
            if (!File.Exists(filePath.Value))
            {
                errors.Add(new ValidationError("WORD_VAL002", "File does not exist", null, null));
                return new DocumentValidationResult(false, errors.AsReadOnly(), warnings.AsReadOnly());
            }
            
            // Check file is not empty
            var fileInfo = new FileInfo(filePath.Value);
            if (fileInfo.Length == 0)
            {
                errors.Add(new ValidationError("WORD_VAL003", "File is empty", null, null));
            }
            
            var extension = Path.GetExtension(filePath.Value).ToLowerInvariant();
            
            // Basic file format validation
            if (extension == ".docx")
            {
                // Check if it's a valid ZIP file (DOCX is essentially a ZIP)
                await ValidateDocxFormatAsync(filePath, errors, warnings, cancellationToken);
            }
            else if (extension == ".doc")
            {
                // Check legacy DOC format magic number
                await ValidateDocFormatAsync(filePath, errors, warnings, cancellationToken);
            }
            
            var isValid = errors.Count == 0;
            _logger.LogDebug("Word validation for {FilePath}: {IsValid}, Errors: {ErrorCount}, Warnings: {WarningCount}", 
                filePath.Value, isValid, errors.Count, warnings.Count);
            
            return new DocumentValidationResult(isValid, errors.AsReadOnly(), warnings.AsReadOnly());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Word document: {FilePath}", filePath.Value);
            errors.Add(new ValidationError("WORD_VAL999", $"Validation error: {ex.Message}", null, null));
            return new DocumentValidationResult(false, errors.AsReadOnly(), warnings.AsReadOnly());
        }
    }
    
    private async Task ValidateDocxFormatAsync(FilePath filePath, List<ValidationError> errors, 
        List<ValidationWarning> warnings, CancellationToken cancellationToken)
    {
        try
        {
            // Check ZIP signature for DOCX
            var buffer = new byte[4];
            using var stream = File.OpenRead(filePath.Value);
            await stream.ReadAsync(buffer, 0, 4, cancellationToken);
            
            // ZIP files start with "PK" (0x504B)
            if (buffer[0] != 0x50 || buffer[1] != 0x4B)
            {
                errors.Add(new ValidationError("WORD_VAL004", "DOCX file does not appear to be a valid ZIP archive", null, null));
            }
        }
        catch (Exception ex)
        {
            warnings.Add(new ValidationWarning("WORD_VAL005", $"Could not validate DOCX format: {ex.Message}", null, null));
        }
    }
    
    private async Task ValidateDocFormatAsync(FilePath filePath, List<ValidationError> errors, 
        List<ValidationWarning> warnings, CancellationToken cancellationToken)
    {
        try
        {
            // Check DOC format signature
            var buffer = new byte[8];
            using var stream = File.OpenRead(filePath.Value);
            await stream.ReadAsync(buffer, 0, 8, cancellationToken);
            
            // DOC files typically start with specific OLE signatures
            // This is a simplified check - real implementation would be more thorough
            if (buffer[0] != 0xD0 || buffer[1] != 0xCF)
            {
                warnings.Add(new ValidationWarning("WORD_VAL006", "DOC file may not be in standard format", null, null));
            }
        }
        catch (Exception ex)
        {
            warnings.Add(new ValidationWarning("WORD_VAL007", $"Could not validate DOC format: {ex.Message}", null, null));
        }
    }
    
    private async Task<string> ExtractWordContentAsync(FilePath filePath, CancellationToken cancellationToken)
    {
        // Simulate Word document text extraction
        await Task.Delay(150, cancellationToken); // Simulate processing time
        
        var extension = Path.GetExtension(filePath.Value).ToLowerInvariant();
        var docType = extension == ".docx" ? "DOCX" : "DOC";
        
        return $"[Extracted {docType} Content from {Path.GetFileName(filePath.Value)}]\n" +
               "This is simulated Word document content that would be extracted by a real Word processing library.\n" +
               "In a real implementation, this would use libraries like DocumentFormat.OpenXml for DOCX or Aspose.Words.\n" +
               "The content would include formatted text, tables, headers, footers, and other document elements.\n" +
               $"Document type: {docType}\n" +
               $"Processed at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";
    }
    
    private async Task<DocumentMetadata> ExtractWordMetadataAsync(FilePath filePath, CancellationToken cancellationToken)
    {
        // Simulate metadata extraction
        await Task.Delay(75, cancellationToken);
        
        var fileInfo = new FileInfo(filePath.Value);
        var extension = Path.GetExtension(filePath.Value).ToLowerInvariant();
        var isDocx = extension == ".docx";
        
        var customProperties = new Dictionary<string, object>
        {
            ["DocumentFormat"] = isDocx ? "Office Open XML" : "Legacy Binary",
            ["HasMacros"] = false,
            ["IsPasswordProtected"] = false,
            ["WordCount"] = 250, // Would be extracted from document
            ["ProcessingStrategy"] = nameof(WordDocumentProcessingStrategy)
        };
        
        return new DocumentMetadata(
            FileName: fileInfo.Name,
            FileSize: fileInfo.Length,
            CreatedDate: fileInfo.CreationTimeUtc,
            ModifiedDate: fileInfo.LastWriteTimeUtc,
            Author: "Document Author", // Would be extracted from document properties
            Title: Path.GetFileNameWithoutExtension(fileInfo.Name),
            Subject: "Word Document",
            PageCount: 1, // Would be extracted from document
            MimeType: isDocx ? "application/vnd.openxmlformats-officedocument.wordprocessingml.document" : "application/msword",
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