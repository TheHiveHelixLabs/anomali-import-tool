using System;
using System.Collections.Generic;

namespace AnomaliImportTool.Core.Models;

/// <summary>
/// Represents a document to be processed and imported
/// </summary>
public class Document
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public long FileSizeBytes { get; set; }
    public string? ExtractedContent { get; set; }
    public string? ExtractedText { get; set; }
    public int ExtractedTextLength { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public DateTime ProcessingStartTime { get; set; }
    public DateTime ProcessingEndTime { get; set; }
    public string? ErrorMessage { get; set; }
    
    // Document-specific properties
    public int PageCount { get; set; }
    public bool IsScanned { get; set; }
    public bool IsPasswordProtected { get; set; }
    public TlpDesignation TlpDesignation { get; set; } = TlpDesignation.Amber;
    
    // Metadata
    public string? Author { get; set; }
    public string? Title { get; set; }
    public string? Subject { get; set; }
    public string? Creator { get; set; }
    public string? Producer { get; set; }
    public string? Keywords { get; set; }
    public DateTime? DocumentDate { get; set; }
    public DateTime? CreationDate { get; set; }
    public DateTime? ModificationDate { get; set; }
    public Dictionary<string, string> ExtractedFields { get; set; } = new();
    public Dictionary<string, object> CustomProperties { get; set; } = new();
    
    // Processing metadata
    public TimeSpan ProcessingDuration => ProcessingEndTime - ProcessingStartTime;
    public bool HasErrors => !string.IsNullOrEmpty(ErrorMessage);
    public bool IsProcessed => Status == DocumentStatus.Completed || Status == DocumentStatus.Failed;
}

public enum DocumentStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    Uploaded,
    Skipped
} 