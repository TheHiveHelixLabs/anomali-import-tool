using AnomaliImportTool.Core.Domain.Entities;
using AnomaliImportTool.Core.Domain.ValueObjects;

namespace AnomaliImportTool.Core.Application.Interfaces.Infrastructure;

/// <summary>
/// Interface for file processing operations following Clean Architecture dependency inversion
/// </summary>
public interface IFileProcessingService
{
    Task<ProcessingResult> ProcessFileAsync(string filePath, CancellationToken cancellationToken = default);
    Task<ProcessingResult> ProcessFilesAsync(IEnumerable<string> filePaths, CancellationToken cancellationToken = default);
    Task<ExtractionResult> ExtractContentAsync(string filePath, CancellationToken cancellationToken = default);
    Task<bool> ValidateFileAsync(string filePath, CancellationToken cancellationToken = default);
    Task<FileMetadata> GetFileMetadataAsync(string filePath, CancellationToken cancellationToken = default);
    IAsyncEnumerable<ProcessingProgress> ProcessWithProgressAsync(IEnumerable<string> filePaths, CancellationToken cancellationToken = default);
} 