using AnomaliImportTool.Core.Domain.SharedKernel.Primitives;
using AnomaliImportTool.Core.Domain.ValueObjects;

namespace AnomaliImportTool.Core.Application.Interfaces.Services;

/// <summary>
/// Strategy interface for document processing following the Open/Closed Principle.
/// New document types can be added by implementing this interface without modifying existing code.
/// </summary>
public interface IDocumentProcessingStrategy
{
    /// <summary>
    /// Gets the supported file extensions for this strategy
    /// </summary>
    IReadOnlyList<string> SupportedExtensions { get; }
    
    /// <summary>
    /// Gets the processing priority (higher values processed first)
    /// </summary>
    int Priority { get; }
    
    /// <summary>
    /// Determines if this strategy can process the specified file
    /// </summary>
    /// <param name="filePath">The file path to check</param>
    /// <returns>True if this strategy can process the file</returns>
    bool CanProcess(FilePath filePath);
    
    /// <summary>
    /// Processes the document and extracts content
    /// </summary>
    /// <param name="filePath">The file to process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The processed document result</returns>
    Task<DocumentProcessingResult> ProcessAsync(FilePath filePath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validates the document before processing
    /// </summary>
    /// <param name="filePath">The file to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    Task<DocumentValidationResult> ValidateAsync(FilePath filePath, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of document processing operation
/// </summary>
public record DocumentProcessingResult(
    bool IsSuccess,
    string Content,
    DocumentMetadata Metadata,
    IReadOnlyList<ProcessingError> Errors,
    IReadOnlyList<ProcessingWarning> Warnings,
    TimeSpan ProcessingTime
);

/// <summary>
/// Result of document validation operation
/// </summary>
public record DocumentValidationResult(
    bool IsValid,
    IReadOnlyList<ValidationError> Errors,
    IReadOnlyList<ValidationWarning> Warnings
);

/// <summary>
/// Processing warning information
/// </summary>
public record ProcessingWarning(
    string Code,
    string Message,
    string? Details = null
);

/// <summary>
/// Document metadata extracted during processing
/// </summary>
public record DocumentMetadata(
    string FileName,
    long FileSize,
    DateTime CreatedDate,
    DateTime ModifiedDate,
    string? Author,
    string? Title,
    string? Subject,
    int PageCount,
    string MimeType,
    IDictionary<string, object> CustomProperties
); 