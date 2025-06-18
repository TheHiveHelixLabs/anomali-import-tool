using AnomaliImportTool.Core.Domain.ValueObjects;
using AnomaliImportTool.Core.Domain.SharedKernel.Primitives;

namespace AnomaliImportTool.Core.Application.Interfaces.Infrastructure;

/// <summary>
/// Single responsibility: Extract content from various file formats
/// Focused on content extraction operations only
/// </summary>
public interface IFileContentExtractor
{
    /// <summary>
    /// Extract text content from a file
    /// </summary>
    /// <param name="filePath">Path to the file to extract content from</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Extracted content with metadata</returns>
    Task<DetailedExtractionResult> ExtractTextAsync(FilePath filePath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Extract structured data from a file (tables, forms, etc.)
    /// </summary>
    /// <param name="filePath">Path to the file to extract data from</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Structured data extraction result</returns>
    Task<StructuredDataResult> ExtractStructuredDataAsync(FilePath filePath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if the extractor supports the given file format
    /// </summary>
    /// <param name="filePath">Path to the file to check</param>
    /// <returns>True if the file format is supported</returns>
    bool SupportsFileFormat(FilePath filePath);
    
    /// <summary>
    /// Get supported file extensions
    /// </summary>
    /// <returns>Collection of supported file extensions</returns>
    IReadOnlyCollection<string> SupportedExtensions { get; }
} 