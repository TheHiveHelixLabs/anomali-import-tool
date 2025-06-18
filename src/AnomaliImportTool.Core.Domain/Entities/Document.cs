using AnomaliImportTool.Core.Domain.Common;
using AnomaliImportTool.Core.Domain.ValueObjects;
using AnomaliImportTool.Core.Domain.Enums;

namespace AnomaliImportTool.Core.Domain.Entities;

/// <summary>
/// Document entity representing a processed file
/// </summary>
public class Document : BaseEntity
{
    public string FileName { get; private set; } = string.Empty;
    public string FilePath { get; private set; } = string.Empty;
    public string FileType { get; private set; } = string.Empty;
    public long FileSize { get; private set; }
    public string ContentHash { get; private set; } = string.Empty;
    public DocumentStatus Status { get; private set; } = DocumentStatus.Pending;
    public string ExtractedContent { get; private set; } = string.Empty;
    public FileMetadata Metadata { get; private set; } = new();
    public Guid? BatchId { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime? ProcessedAt { get; private set; }

    private Document() { } // For EF Core

    public Document(string fileName, string filePath, string fileType, long fileSize, string contentHash)
    {
        FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
        FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        FileType = fileType ?? throw new ArgumentNullException(nameof(fileType));
        FileSize = fileSize;
        ContentHash = contentHash ?? throw new ArgumentNullException(nameof(contentHash));
        Status = DocumentStatus.Pending;
    }

    public void UpdateContent(string extractedContent, FileMetadata metadata)
    {
        ExtractedContent = extractedContent ?? string.Empty;
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        MarkAsUpdated(CreatedBy);
    }

    public void MarkAsProcessed()
    {
        Status = DocumentStatus.Processed;
        ProcessedAt = DateTime.UtcNow;
        MarkAsUpdated(CreatedBy);
    }

    public void MarkAsError(string errorMessage)
    {
        Status = DocumentStatus.Error;
        ErrorMessage = errorMessage;
        MarkAsUpdated(CreatedBy);
    }

    public void AssignToBatch(Guid batchId)
    {
        BatchId = batchId;
        MarkAsUpdated(CreatedBy);
    }
} 