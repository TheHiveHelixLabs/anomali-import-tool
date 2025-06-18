using AnomaliImportTool.Core.Domain.ValueObjects;
using AnomaliImportTool.Core.Domain.SharedKernel.Primitives;

namespace AnomaliImportTool.Core.Application.Interfaces.Infrastructure;

/// <summary>
/// Single responsibility: Orchestrate file processing operations
/// Coordinates multiple single-responsibility services for complete file processing workflows
/// </summary>
public interface IFileProcessingOrchestrator
{
    /// <summary>
    /// Process a single file through the complete workflow (validate, extract content, extract metadata)
    /// </summary>
    /// <param name="filePath">Path to the file to process</param>
    /// <param name="options">Processing options and configurations</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Complete processing result</returns>
    Task<FileProcessingResult> ProcessFileAsync(FilePath filePath, FileProcessingOptions options, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Process multiple files with progress reporting
    /// </summary>
    /// <param name="filePaths">Collection of file paths to process</param>
    /// <param name="options">Processing options and configurations</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable of processing progress updates</returns>
    IAsyncEnumerable<FileProcessingProgress> ProcessFilesWithProgressAsync(
        IEnumerable<FilePath> filePaths, 
        FileProcessingOptions options, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Process files in batches for better performance
    /// </summary>
    /// <param name="filePaths">Collection of file paths to process</param>
    /// <param name="batchSize">Number of files to process in each batch</param>
    /// <param name="options">Processing options and configurations</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Batch processing results</returns>
    Task<BatchProcessingResult> ProcessFilesBatchAsync(
        IEnumerable<FilePath> filePaths, 
        int batchSize, 
        FileProcessingOptions options, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validate processing pipeline configuration
    /// </summary>
    /// <param name="options">Processing options to validate</param>
    /// <returns>Configuration validation result</returns>
    Task<ConfigurationValidationResult> ValidateConfigurationAsync(FileProcessingOptions options);
} 