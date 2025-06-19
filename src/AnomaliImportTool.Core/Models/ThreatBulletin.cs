using System;
using System.Collections.Generic;

namespace AnomaliImportTool.Core.Models;

/// <summary>
/// Represents a threat bulletin to be created in Anomali ThreatStream
/// </summary>
public class ThreatBulletin
{
    public string? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? Source { get; set; }
    public BulletinStatus Status { get; set; } = BulletinStatus.Published;
    public TlpDesignation Tlp { get; set; } = TlpDesignation.Amber;
    public List<Document> Attachments { get; set; } = new();
    public Dictionary<string, string> Tags { get; set; } = new();
    public List<string> Indicators { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public int Confidence { get; set; } = 50;
    public string? Severity { get; set; } = "Medium";
    public List<string> References { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    // Validation
    public bool IsValid()
    {
        if (string.IsNullOrWhiteSpace(Name))
            return false;
            
        if (string.IsNullOrWhiteSpace(Body))
            return false;
            
        if (Confidence < 0 || Confidence > 100)
            return false;
            
        return true;
    }
}

public enum BulletinStatus
{
    Published,
    Reviewed,
    ReviewRequest,
    PendingReview,
    Draft
}

public enum TlpDesignation
{
    White,
    Green,
    Amber,
    Red,
    Clear
} 