using AnomaliImportTool.Core.Domain.ValueObjects;
using AnomaliImportTool.Core.Domain.SharedKernel.Primitives;

namespace AnomaliImportTool.Core.Application.Interfaces.Infrastructure;

/// <summary>
/// Single responsibility: Validate file integrity, format, and security
/// Focused on file validation operations only
/// </summary>
public interface IFileValidator
{
    /// <summary>
    /// Validate file format and structure
    /// </summary>
    /// <param name="filePath">Path to the file to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with details</returns>
    Task<FileValidationResult> ValidateFormatAsync(FilePath filePath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validate file integrity using checksums
    /// </summary>
    /// <param name="filePath">Path to the file to validate</param>
    /// <param name="expectedHash">Expected file hash for integrity check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Integrity validation result</returns>
    Task<IntegrityValidationResult> ValidateIntegrityAsync(FilePath filePath, ContentHash expectedHash, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validate file security (malware scan, safe content)
    /// </summary>
    /// <param name="filePath">Path to the file to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Security validation result</returns>
    Task<SecurityValidationResult> ValidateSecurityAsync(FilePath filePath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validate file size constraints
    /// </summary>
    /// <param name="filePath">Path to the file to validate</param>
    /// <param name="maxSizeBytes">Maximum allowed file size in bytes</param>
    /// <returns>Size validation result</returns>
    Task<SizeValidationResult> ValidateSizeAsync(FilePath filePath, long maxSizeBytes);
    
    /// <summary>
    /// Check if file exists and is accessible
    /// </summary>
    /// <param name="filePath">Path to the file to check</param>
    /// <returns>Accessibility validation result</returns>
    Task<AccessibilityValidationResult> ValidateAccessibilityAsync(FilePath filePath);
} 