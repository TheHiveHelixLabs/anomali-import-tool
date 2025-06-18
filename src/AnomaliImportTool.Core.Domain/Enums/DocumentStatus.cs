namespace AnomaliImportTool.Core.Domain.Enums;

/// <summary>
/// Document processing status enumeration
/// </summary>
public enum DocumentStatus
{
    Pending = 0,
    Processing = 1,
    Processed = 2,
    Error = 3,
    Skipped = 4
} 