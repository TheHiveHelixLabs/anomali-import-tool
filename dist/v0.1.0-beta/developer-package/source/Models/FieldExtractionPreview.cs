namespace AnomaliImportTool.Core.Models;

public class FieldExtractionPreview
{
    public string FieldName { get; set; } = string.Empty;
    public string? Value { get; set; }
    public double Confidence { get; set; }
} 