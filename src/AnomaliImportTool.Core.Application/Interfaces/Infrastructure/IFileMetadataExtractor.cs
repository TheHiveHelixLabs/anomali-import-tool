using AnomaliImportTool.Core.Domain.ValueObjects;
using AnomaliImportTool.Core.Domain.SharedKernel.Primitives;

namespace AnomaliImportTool.Core.Application.Interfaces.Infrastructure;

/// <summary>
/// Single responsibility: Extract metadata from files
/// Focused on metadata extraction operations only
/// </summary>
public interface IFileMetadataExtractor
{
    /// <summary>
    /// Extract basic file metadata (size, dates, attributes)
    /// </summary>
    /// <param name="filePath">Path to the file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Basic file metadata</returns>
    Task<FileMetadata> ExtractBasicMetadataAsync(FilePath filePath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Extract extended metadata specific to file format (EXIF, document properties, etc.)
    /// </summary>
    /// <param name="filePath">Path to the file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Extended metadata specific to file format</returns>
    Task<ExtendedMetadata> ExtractExtendedMetadataAsync(FilePath filePath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Extract security-related metadata (digital signatures, certificates, etc.)
    /// </summary>
    /// <param name="filePath">Path to the file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Security metadata</returns>
    Task<SecurityMetadata> ExtractSecurityMetadataAsync(FilePath filePath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Calculate file hash for integrity verification
    /// </summary>
    /// <param name="filePath">Path to the file</param>
    /// <param name="hashAlgorithm">Hash algorithm to use</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File content hash</returns>
    Task<ContentHash> CalculateFileHashAsync(FilePath filePath, string hashAlgorithm = "SHA256", CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if the extractor supports metadata extraction for the given file format
    /// </summary>
    /// <param name="filePath">Path to the file</param>
    /// <returns>True if metadata extraction is supported</returns>
    bool SupportsMetadataExtraction(FilePath filePath);
} 