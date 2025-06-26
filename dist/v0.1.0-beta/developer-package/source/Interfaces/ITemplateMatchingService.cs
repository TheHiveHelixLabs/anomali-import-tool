using AnomaliImportTool.Core.Models;

namespace AnomaliImportTool.Core.Interfaces;

/// <summary>
/// Interface for intelligent document-template matching with confidence scoring and fingerprinting
/// </summary>
public interface ITemplateMatchingService
{
    #region Document-Template Matching

    /// <summary>
    /// Finds the best matching template for a document
    /// </summary>
    /// <param name="documentPath">Path to the document</param>
    /// <param name="availableTemplates">Available templates to match against</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Best matching template with confidence score</returns>
    Task<TemplateMatchResult?> FindBestMatchAsync(string documentPath, IEnumerable<ImportTemplate> availableTemplates, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the best matching template for a document by ID
    /// </summary>
    /// <param name="documentPath">Path to the document</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Best matching template with confidence score</returns>
    Task<TemplateMatchResult?> FindBestMatchAsync(string documentPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all template matches for a document, ranked by confidence
    /// </summary>
    /// <param name="documentPath">Path to the document</param>
    /// <param name="availableTemplates">Available templates to match against</param>
    /// <param name="minimumConfidence">Minimum confidence threshold (0.0 to 1.0)</param>
    /// <param name="maxResults">Maximum number of results to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>All matching templates ranked by confidence</returns>
    Task<IEnumerable<TemplateMatchResult>> GetAllMatchesAsync(string documentPath, IEnumerable<ImportTemplate> availableTemplates, double minimumConfidence = 0.1, int maxResults = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Matches a document against a specific template
    /// </summary>
    /// <param name="documentPath">Path to the document</param>
    /// <param name="template">Template to match against</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Match result with confidence score</returns>
    Task<TemplateMatchResult> MatchDocumentToTemplateAsync(string documentPath, ImportTemplate template, CancellationToken cancellationToken = default);

    #endregion

    #region Document Fingerprinting

    /// <summary>
    /// Creates a document fingerprint for template matching
    /// </summary>
    /// <param name="documentPath">Path to the document</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Document fingerprint</returns>
    Task<DocumentFingerprint> CreateDocumentFingerprintAsync(string documentPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a template fingerprint for document matching
    /// </summary>
    /// <param name="template">Template to fingerprint</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Template fingerprint</returns>
    Task<TemplateFingerprint> CreateTemplateFingerprintAsync(ImportTemplate template, CancellationToken cancellationToken = default);

    /// <summary>
    /// Compares two fingerprints and calculates similarity
    /// </summary>
    /// <param name="documentFingerprint">Document fingerprint</param>
    /// <param name="templateFingerprint">Template fingerprint</param>
    /// <returns>Similarity score (0.0 to 1.0)</returns>
    double CalculateFingerprintSimilarity(DocumentFingerprint documentFingerprint, TemplateFingerprint templateFingerprint);

    #endregion

    #region Confidence Scoring

    /// <summary>
    /// Calculates confidence score for a document-template match
    /// </summary>
    /// <param name="documentPath">Path to the document</param>
    /// <param name="template">Template to match</param>
    /// <param name="matchingCriteria">Criteria for matching</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Confidence score breakdown</returns>
    Task<ConfidenceScore> CalculateConfidenceScoreAsync(string documentPath, ImportTemplate template, MatchingCriteria? matchingCriteria = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates confidence score using pre-computed fingerprints
    /// </summary>
    /// <param name="documentFingerprint">Document fingerprint</param>
    /// <param name="templateFingerprint">Template fingerprint</param>
    /// <param name="matchingCriteria">Criteria for matching</param>
    /// <returns>Confidence score breakdown</returns>
    ConfidenceScore CalculateConfidenceScore(DocumentFingerprint documentFingerprint, TemplateFingerprint templateFingerprint, MatchingCriteria? matchingCriteria = null);

    #endregion

    #region Template Learning and Optimization

    /// <summary>
    /// Updates template matching criteria based on successful matches
    /// </summary>
    /// <param name="templateId">Template ID</param>
    /// <param name="documentPath">Successfully matched document</param>
    /// <param name="userConfirmed">Whether the match was confirmed by user</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated template</returns>
    Task<ImportTemplate> LearnFromSuccessfulMatchAsync(Guid templateId, string documentPath, bool userConfirmed, CancellationToken cancellationToken = default);

    /// <summary>
    /// Optimizes template matching criteria based on usage patterns
    /// </summary>
    /// <param name="templateId">Template ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Optimized template</returns>
    Task<ImportTemplate> OptimizeTemplateMatchingAsync(Guid templateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes template performance and suggests improvements
    /// </summary>
    /// <param name="templateId">Template ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Performance analysis and suggestions</returns>
    Task<TemplatePerformanceAnalysis> AnalyzeTemplatePerformanceAsync(Guid templateId, CancellationToken cancellationToken = default);

    #endregion

    #region Batch Operations

    /// <summary>
    /// Matches multiple documents to templates in batch
    /// </summary>
    /// <param name="documentPaths">Paths to documents</param>
    /// <param name="availableTemplates">Available templates</param>
    /// <param name="minimumConfidence">Minimum confidence threshold</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Batch matching results</returns>
    Task<BatchMatchResult> MatchDocumentsBatchAsync(IEnumerable<string> documentPaths, IEnumerable<ImportTemplate> availableTemplates, double minimumConfidence = 0.5, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pre-computes fingerprints for multiple documents
    /// </summary>
    /// <param name="documentPaths">Paths to documents</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Document fingerprints</returns>
    Task<IDictionary<string, DocumentFingerprint>> CreateDocumentFingerprintsBatchAsync(IEnumerable<string> documentPaths, CancellationToken cancellationToken = default);

    #endregion

    #region Configuration and Settings

    /// <summary>
    /// Updates matching algorithm settings
    /// </summary>
    /// <param name="settings">New matching settings</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task UpdateMatchingSettingsAsync(MatchingSettings settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current matching algorithm settings
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current matching settings</returns>
    Task<MatchingSettings> GetMatchingSettingsAsync(CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// Result of template matching operation
/// </summary>
public class TemplateMatchResult
{
    /// <summary>
    /// Matched template
    /// </summary>
    public ImportTemplate Template { get; set; } = null!;

    /// <summary>
    /// Overall confidence score (0.0 to 1.0)
    /// </summary>
    public double ConfidenceScore { get; set; }

    /// <summary>
    /// Detailed confidence breakdown
    /// </summary>
    public ConfidenceScore ConfidenceBreakdown { get; set; } = new();

    /// <summary>
    /// Reasons for the match
    /// </summary>
    public List<string> MatchReasons { get; set; } = new();

    /// <summary>
    /// Any warnings about the match
    /// </summary>
    public List<string> MatchWarnings { get; set; } = new();

    /// <summary>
    /// Time taken for matching
    /// </summary>
    public TimeSpan MatchingTime { get; set; }

    /// <summary>
    /// Additional matching metadata
    /// </summary>
    public Dictionary<string, object> MatchMetadata { get; set; } = new();
}

/// <summary>
/// Document fingerprint for template matching
/// </summary>
public class DocumentFingerprint
{
    /// <summary>
    /// Document path
    /// </summary>
    public string DocumentPath { get; set; } = string.Empty;

    /// <summary>
    /// Document format (pdf, docx, etc.)
    /// </summary>
    public string DocumentFormat { get; set; } = string.Empty;

    /// <summary>
    /// Document metadata fingerprint
    /// </summary>
    public Dictionary<string, string> MetadataFingerprint { get; set; } = new();

    /// <summary>
    /// Content-based keywords
    /// </summary>
    public List<string> ContentKeywords { get; set; } = new();

    /// <summary>
    /// Document structure fingerprint
    /// </summary>
    public DocumentStructure Structure { get; set; } = new();

    /// <summary>
    /// Text patterns found in document
    /// </summary>
    public List<string> TextPatterns { get; set; } = new();

    /// <summary>
    /// Document language
    /// </summary>
    public string Language { get; set; } = "en";

    /// <summary>
    /// When the fingerprint was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Hash of document content for quick comparison
    /// </summary>
    public string ContentHash { get; set; } = string.Empty;
}

/// <summary>
/// Template fingerprint for document matching
/// </summary>
public class TemplateFingerprint
{
    /// <summary>
    /// Template ID
    /// </summary>
    public Guid TemplateId { get; set; }

    /// <summary>
    /// Supported document formats
    /// </summary>
    public List<string> SupportedFormats { get; set; } = new();

    /// <summary>
    /// Expected keywords from matching criteria
    /// </summary>
    public List<string> ExpectedKeywords { get; set; } = new();

    /// <summary>
    /// Required keywords for matching
    /// </summary>
    public List<string> RequiredKeywords { get; set; } = new();

    /// <summary>
    /// Expected text patterns
    /// </summary>
    public List<string> ExpectedPatterns { get; set; } = new();

    /// <summary>
    /// Expected document structure
    /// </summary>
    public DocumentStructure ExpectedStructure { get; set; } = new();

    /// <summary>
    /// Template complexity score
    /// </summary>
    public double ComplexityScore { get; set; }

    /// <summary>
    /// When the fingerprint was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Document structure information
/// </summary>
public class DocumentStructure
{
    /// <summary>
    /// Number of pages
    /// </summary>
    public int PageCount { get; set; }

    /// <summary>
    /// Approximate word count
    /// </summary>
    public int WordCount { get; set; }

    /// <summary>
    /// Whether document has tables
    /// </summary>
    public bool HasTables { get; set; }

    /// <summary>
    /// Whether document has images
    /// </summary>
    public bool HasImages { get; set; }

    /// <summary>
    /// Whether document is primarily scanned/OCR
    /// </summary>
    public bool IsScanned { get; set; }

    /// <summary>
    /// Document layout type
    /// </summary>
    public DocumentLayoutType LayoutType { get; set; } = DocumentLayoutType.Standard;
}

/// <summary>
/// Confidence score breakdown
/// </summary>
public class ConfidenceScore
{
    /// <summary>
    /// Overall confidence (0.0 to 1.0)
    /// </summary>
    public double Overall { get; set; }

    /// <summary>
    /// Format match confidence
    /// </summary>
    public double FormatMatch { get; set; }

    /// <summary>
    /// Keyword match confidence
    /// </summary>
    public double KeywordMatch { get; set; }

    /// <summary>
    /// Pattern match confidence
    /// </summary>
    public double PatternMatch { get; set; }

    /// <summary>
    /// Structure match confidence
    /// </summary>
    public double StructureMatch { get; set; }

    /// <summary>
    /// Metadata match confidence
    /// </summary>
    public double MetadataMatch { get; set; }

    /// <summary>
    /// File name match confidence
    /// </summary>
    public double FileNameMatch { get; set; }

    /// <summary>
    /// Detailed scoring breakdown
    /// </summary>
    public Dictionary<string, double> DetailedScores { get; set; } = new();
}

/// <summary>
/// Criteria for template matching
/// </summary>
public class MatchingCriteria
{
    /// <summary>
    /// Weight for format matching (0.0 to 1.0)
    /// </summary>
    public double FormatWeight { get; set; } = 0.2;

    /// <summary>
    /// Weight for keyword matching (0.0 to 1.0)
    /// </summary>
    public double KeywordWeight { get; set; } = 0.3;

    /// <summary>
    /// Weight for pattern matching (0.0 to 1.0)
    /// </summary>
    public double PatternWeight { get; set; } = 0.2;

    /// <summary>
    /// Weight for structure matching (0.0 to 1.0)
    /// </summary>
    public double StructureWeight { get; set; } = 0.15;

    /// <summary>
    /// Weight for metadata matching (0.0 to 1.0)
    /// </summary>
    public double MetadataWeight { get; set; } = 0.1;

    /// <summary>
    /// Weight for filename matching (0.0 to 1.0)
    /// </summary>
    public double FileNameWeight { get; set; } = 0.05;

    /// <summary>
    /// Minimum overall confidence for auto-application
    /// </summary>
    public double AutoApplicationThreshold { get; set; } = 0.8;

    /// <summary>
    /// Whether to use machine learning enhancements
    /// </summary>
    public bool UseMachineLearning { get; set; } = false;
}

/// <summary>
/// Template performance analysis
/// </summary>
public class TemplatePerformanceAnalysis
{
    /// <summary>
    /// Template ID
    /// </summary>
    public Guid TemplateId { get; set; }

    /// <summary>
    /// Overall performance score (0.0 to 1.0)
    /// </summary>
    public double PerformanceScore { get; set; }

    /// <summary>
    /// Match accuracy rate
    /// </summary>
    public double MatchAccuracy { get; set; }

    /// <summary>
    /// Average confidence scores
    /// </summary>
    public double AverageConfidence { get; set; }

    /// <summary>
    /// Common false positive patterns
    /// </summary>
    public List<string> FalsePositivePatterns { get; set; } = new();

    /// <summary>
    /// Suggested improvements
    /// </summary>
    public List<string> Suggestions { get; set; } = new();

    /// <summary>
    /// Performance metrics by category
    /// </summary>
    public Dictionary<string, double> CategoryMetrics { get; set; } = new();

    /// <summary>
    /// Analysis timestamp
    /// </summary>
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Batch matching result
/// </summary>
public class BatchMatchResult
{
    /// <summary>
    /// Individual document match results
    /// </summary>
    public Dictionary<string, TemplateMatchResult?> DocumentMatches { get; set; } = new();

    /// <summary>
    /// Documents that couldn't be matched
    /// </summary>
    public List<string> UnmatchedDocuments { get; set; } = new();

    /// <summary>
    /// Overall batch success rate
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// Total processing time
    /// </summary>
    public TimeSpan TotalProcessingTime { get; set; }

    /// <summary>
    /// Average confidence score across all matches
    /// </summary>
    public double AverageConfidence { get; set; }
}

/// <summary>
/// Matching algorithm settings
/// </summary>
public class MatchingSettings
{
    /// <summary>
    /// Default matching criteria
    /// </summary>
    public MatchingCriteria DefaultCriteria { get; set; } = new();

    /// <summary>
    /// Enable fuzzy keyword matching
    /// </summary>
    public bool EnableFuzzyMatching { get; set; } = true;

    /// <summary>
    /// Fuzzy matching threshold (0.0 to 1.0)
    /// </summary>
    public double FuzzyMatchingThreshold { get; set; } = 0.8;

    /// <summary>
    /// Enable caching of fingerprints
    /// </summary>
    public bool EnableFingerprintCaching { get; set; } = true;

    /// <summary>
    /// Cache expiration time in hours
    /// </summary>
    public int CacheExpirationHours { get; set; } = 24;

    /// <summary>
    /// Maximum number of concurrent matching operations
    /// </summary>
    public int MaxConcurrentOperations { get; set; } = 4;

    /// <summary>
    /// Enable performance analytics collection
    /// </summary>
    public bool EnableAnalytics { get; set; } = true;
}

/// <summary>
/// Document layout types
/// </summary>
public enum DocumentLayoutType
{
    Standard,
    Form,
    Table,
    Report,
    Letter,
    Technical,
    Scanned
} 