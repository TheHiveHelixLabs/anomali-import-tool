using AnomaliImportTool.Core.Models;

namespace AnomaliImportTool.Core.Interfaces;

/// <summary>
/// Interface for processing documents and extracting content
/// </summary>
public interface IDocumentProcessor
{
    /// <summary>
    /// Gets the supported file extensions for this processor
    /// </summary>
    string[] SupportedExtensions { get; }
    
    /// <summary>
    /// Processes a document and extracts its content
    /// </summary>
    /// <param name="filePath">Path to the file to process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Processed document with extracted content</returns>
    Task<Document> ProcessDocumentAsync(string filePath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Processes a document with specific options
    /// </summary>
    /// <param name="filePath">Path to the file to process</param>
    /// <param name="options">Processing options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Processed document with extracted content</returns>
    Task<Document> ProcessAsync(string filePath, ProcessingOptions options = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if the processor can handle the given file type
    /// </summary>
    /// <param name="fileExtension">File extension to check</param>
    /// <returns>True if the processor can handle the file</returns>
    bool CanProcess(string fileExtension);
    
    /// <summary>
    /// Validates a file without fully processing it
    /// </summary>
    /// <param name="filePath">Path to the file to validate</param>
    /// <returns>True if the file is valid for processing</returns>
    Task<bool> ValidateAsync(string filePath);
} 