namespace AnomaliImportTool.Core.Domain.ValueObjects;

/// <summary>
/// File metadata value object
/// </summary>
public sealed record FileMetadata
{
    public string Author { get; init; } = string.Empty;
    public DateTime CreationDate { get; init; }
    public DateTime LastModified { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public int PageCount { get; init; }
    public string MimeType { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, object> CustomProperties { get; init; } = new Dictionary<string, object>();

    public FileMetadata() { }

    public FileMetadata(string author, DateTime creationDate, DateTime lastModified, string title, string subject, int pageCount, string mimeType)
    {
        Author = author ?? string.Empty;
        CreationDate = creationDate;
        LastModified = lastModified;
        Title = title ?? string.Empty;
        Subject = subject ?? string.Empty;
        PageCount = pageCount;
        MimeType = mimeType ?? string.Empty;
    }
} 