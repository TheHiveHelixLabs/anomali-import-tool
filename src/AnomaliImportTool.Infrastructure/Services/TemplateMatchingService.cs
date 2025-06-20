using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AnomaliImportTool.Core.Interfaces;
using AnomaliImportTool.Core.Models;
using AnomaliImportTool.Infrastructure.DocumentProcessing;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace AnomaliImportTool.Infrastructure.Services;

/// <summary>
/// Implementation of ITemplateMatchingService providing intelligent document-template matching
/// with document fingerprinting and confidence scoring algorithms
/// </summary>
public class TemplateMatchingService : ITemplateMatchingService
{
    private readonly ILogger<TemplateMatchingService> _logger;
    private readonly IImportTemplateService _templateService;
    private readonly DocumentProcessingService _documentProcessor;
    private readonly MatchingSettings _settings;
    private readonly Dictionary<string, DocumentFingerprint> _fingerprintCache;
    private readonly Dictionary<Guid, TemplateFingerprint> _templateFingerprintCache;

    public TemplateMatchingService(
        ILogger<TemplateMatchingService> logger,
        IImportTemplateService templateService,
        DocumentProcessingService documentProcessor)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _templateService = templateService ?? throw new ArgumentNullException(nameof(templateService));
        _documentProcessor = documentProcessor ?? throw new ArgumentNullException(nameof(documentProcessor));
        
        _settings = new MatchingSettings();
        _fingerprintCache = new Dictionary<string, DocumentFingerprint>();
        _templateFingerprintCache = new Dictionary<Guid, TemplateFingerprint>();
    }

    #region Document-Template Matching

    public async Task<TemplateMatchResult?> FindBestMatchAsync(string documentPath, IEnumerable<ImportTemplate> availableTemplates, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Finding best template match for document: {DocumentPath}", documentPath);

            var matches = await GetAllMatchesAsync(documentPath, availableTemplates, _settings.DefaultCriteria.AutoApplicationThreshold, 1, cancellationToken);
            var bestMatch = matches.FirstOrDefault();

            if (bestMatch != null)
            {
                _logger.LogInformation("Best match found: Template {TemplateId} with confidence {Confidence:F2}", 
                    bestMatch.Template.Id, bestMatch.ConfidenceScore);
            }
            else
            {
                _logger.LogInformation("No suitable template match found for document: {DocumentPath}", documentPath);
            }

            return bestMatch;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding best template match for document: {DocumentPath}", documentPath);
            throw;
        }
    }

    public async Task<TemplateMatchResult?> FindBestMatchAsync(string documentPath, CancellationToken cancellationToken = default)
    {
        var allTemplates = await _templateService.GetAllTemplatesAsync(false, cancellationToken);
        return await FindBestMatchAsync(documentPath, allTemplates, cancellationToken);
    }

    public async Task<IEnumerable<TemplateMatchResult>> GetAllMatchesAsync(string documentPath, IEnumerable<ImportTemplate> availableTemplates, double minimumConfidence = 0.1, int maxResults = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting all template matches for document: {documentPath} (min confidence: {minConfidence:F2})", 
                documentPath, minimumConfidence);

            var startTime = DateTime.UtcNow;
            var matches = new List<TemplateMatchResult>();

            foreach (var template in availableTemplates)
            {
                try
                {
                    var matchResult = await MatchDocumentToTemplateAsync(documentPath, template, cancellationToken);
                    if (matchResult.ConfidenceScore >= minimumConfidence)
                    {
                        matches.Add(matchResult);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error matching document to template {TemplateId}: {Error}", template.Id, ex.Message);
                }
            }

            // Sort by confidence score descending and take top results
            var rankedMatches = matches
                .OrderByDescending(m => m.ConfidenceScore)
                .Take(maxResults)
                .ToList();

            var processingTime = DateTime.UtcNow - startTime;
            _logger.LogInformation("Found {matchCount} matches for document in {processingTime:F2}ms", 
                rankedMatches.Count, processingTime.TotalMilliseconds);

            return rankedMatches;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting template matches for document: {DocumentPath}", documentPath);
            throw;
        }
    }

    public async Task<TemplateMatchResult> MatchDocumentToTemplateAsync(string documentPath, ImportTemplate template, CancellationToken cancellationToken = default)
    {
        try
        {
            var startTime = DateTime.UtcNow;
            var documentFingerprint = await CreateDocumentFingerprintAsync(documentPath, cancellationToken);
            var templateFingerprint = await CreateTemplateFingerprintAsync(template, cancellationToken);

            var confidenceScore = CalculateConfidenceScore(documentFingerprint, templateFingerprint, _settings.DefaultCriteria);
            var matchingTime = DateTime.UtcNow - startTime;

            var matchResult = new TemplateMatchResult
            {
                Template = template,
                ConfidenceScore = confidenceScore.Overall,
                ConfidenceBreakdown = confidenceScore,
                MatchingTime = matchingTime,
                MatchReasons = GenerateMatchReasons(confidenceScore),
                MatchWarnings = GenerateMatchWarnings(confidenceScore),
                MatchMetadata = new Dictionary<string, object>
                {
                    { "DocumentFormat", documentFingerprint.DocumentFormat },
                    { "WordCount", documentFingerprint.Structure.WordCount },
                    { "PageCount", documentFingerprint.Structure.PageCount },
                    { "TemplateComplexity", templateFingerprint.ComplexityScore }
                }
            };

            return matchResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error matching document {documentPath} to template {templateId}", documentPath, template.Id);
            throw;
        }
    }

    #endregion

    #region Document Fingerprinting

    public async Task<DocumentFingerprint> CreateDocumentFingerprintAsync(string documentPath, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check cache first if enabled
            if (_settings.EnableFingerprintCaching && _fingerprintCache.TryGetValue(documentPath, out var cachedFingerprint))
            {
                if (DateTime.UtcNow - cachedFingerprint.CreatedAt < TimeSpan.FromHours(_settings.CacheExpirationHours))
                {
                    return cachedFingerprint;
                }
                _fingerprintCache.Remove(documentPath);
            }

            _logger.LogInformation("Creating document fingerprint for: {DocumentPath}", documentPath);

            if (!File.Exists(documentPath))
            {
                throw new FileNotFoundException($"Document not found: {documentPath}");
            }

            var documentFormat = GetDocumentFormat(documentPath);
            
            // Process document to extract content
            var document = await _documentProcessor.ProcessDocumentAsync(documentPath, cancellationToken);

            var fingerprint = new DocumentFingerprint
            {
                DocumentPath = documentPath,
                DocumentFormat = documentFormat,
                CreatedAt = DateTime.UtcNow,
                ContentHash = ComputeContentHash(document.ExtractedText),
                Language = DetectLanguage(document.ExtractedText),
                Structure = new DocumentStructure
                {
                    PageCount = document.PageCount,
                    WordCount = CountWords(document.ExtractedText),
                    HasTables = document.ExtractedText.Contains('\t') || ContainsTablePatterns(document.ExtractedText),
                    HasImages = document.ProcessingMetadata.ContainsKey("HasImages") && (bool)document.ProcessingMetadata["HasImages"],
                    IsScanned = document.ProcessingMetadata.ContainsKey("IsScanned") && (bool)document.ProcessingMetadata["IsScanned"],
                    LayoutType = DetermineLayoutType(document.ExtractedText)
                },
                ContentKeywords = ExtractKeywords(document.ExtractedText),
                TextPatterns = ExtractTextPatterns(document.ExtractedText),
                MetadataFingerprint = ExtractMetadataFingerprint(document.DocumentMetadata)
            };

            // Cache the fingerprint if enabled
            if (_settings.EnableFingerprintCaching)
            {
                _fingerprintCache[documentPath] = fingerprint;
            }

            _logger.LogInformation("Document fingerprint created for {documentPath}: {wordCount} words, {pageCount} pages, {keywordCount} keywords", 
                documentPath, fingerprint.Structure.WordCount, fingerprint.Structure.PageCount, fingerprint.ContentKeywords.Count);

            return fingerprint;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating document fingerprint for: {DocumentPath}", documentPath);
            throw;
        }
    }

    public async Task<TemplateFingerprint> CreateTemplateFingerprintAsync(ImportTemplate template, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check cache first
            if (_templateFingerprintCache.TryGetValue(template.Id, out var cachedFingerprint))
            {
                return cachedFingerprint;
            }

            _logger.LogInformation("Creating template fingerprint for: {TemplateId}", template.Id);

            var fingerprint = new TemplateFingerprint
            {
                TemplateId = template.Id,
                SupportedFormats = template.SupportedFormats,
                CreatedAt = DateTime.UtcNow,
                ComplexityScore = CalculateTemplateComplexity(template),
                ExpectedKeywords = ExtractTemplateKeywords(template),
                RequiredKeywords = ExtractRequiredKeywords(template),
                ExpectedPatterns = ExtractTemplatePatterns(template),
                ExpectedStructure = InferExpectedStructure(template)
            };

            // Cache the fingerprint
            _templateFingerprintCache[template.Id] = fingerprint;

            _logger.LogInformation("Template fingerprint created for {templateId}: complexity {complexity:F2}, {keywordCount} keywords", 
                template.Id, fingerprint.ComplexityScore, fingerprint.ExpectedKeywords.Count);

            return fingerprint;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating template fingerprint for: {TemplateId}", template.Id);
            throw;
        }
    }

    public double CalculateFingerprintSimilarity(DocumentFingerprint documentFingerprint, TemplateFingerprint templateFingerprint)
    {
        try
        {
            var similarities = new List<double>();

            // Format similarity
            var formatSimilarity = CalculateFormatSimilarity(documentFingerprint.DocumentFormat, templateFingerprint.SupportedFormats);
            similarities.Add(formatSimilarity);

            // Keyword similarity
            var keywordSimilarity = CalculateKeywordSimilarity(documentFingerprint.ContentKeywords, templateFingerprint.ExpectedKeywords);
            similarities.Add(keywordSimilarity);

            // Pattern similarity
            var patternSimilarity = CalculatePatternSimilarity(documentFingerprint.TextPatterns, templateFingerprint.ExpectedPatterns);
            similarities.Add(patternSimilarity);

            // Structure similarity
            var structureSimilarity = CalculateStructureSimilarity(documentFingerprint.Structure, templateFingerprint.ExpectedStructure);
            similarities.Add(structureSimilarity);

            // Calculate weighted average
            return similarities.Average();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error calculating fingerprint similarity");
            return 0.0;
        }
    }

    #endregion

    #region Confidence Scoring

    public async Task<ConfidenceScore> CalculateConfidenceScoreAsync(string documentPath, ImportTemplate template, MatchingCriteria? matchingCriteria = null, CancellationToken cancellationToken = default)
    {
        var documentFingerprint = await CreateDocumentFingerprintAsync(documentPath, cancellationToken);
        var templateFingerprint = await CreateTemplateFingerprintAsync(template, cancellationToken);
        return CalculateConfidenceScore(documentFingerprint, templateFingerprint, matchingCriteria);
    }

    public ConfidenceScore CalculateConfidenceScore(DocumentFingerprint documentFingerprint, TemplateFingerprint templateFingerprint, MatchingCriteria? matchingCriteria = null)
    {
        try
        {
            var criteria = matchingCriteria ?? _settings.DefaultCriteria;
            
            var confidence = new ConfidenceScore();

            // Calculate individual confidence components
            confidence.FormatMatch = CalculateFormatSimilarity(documentFingerprint.DocumentFormat, templateFingerprint.SupportedFormats);
            confidence.KeywordMatch = CalculateKeywordSimilarity(documentFingerprint.ContentKeywords, templateFingerprint.ExpectedKeywords);
            confidence.PatternMatch = CalculatePatternSimilarity(documentFingerprint.TextPatterns, templateFingerprint.ExpectedPatterns);
            confidence.StructureMatch = CalculateStructureSimilarity(documentFingerprint.Structure, templateFingerprint.ExpectedStructure);
            confidence.MetadataMatch = CalculateMetadataMatch(documentFingerprint.MetadataFingerprint, templateFingerprint);
            confidence.FileNameMatch = CalculateFileNameMatch(documentFingerprint.DocumentPath, templateFingerprint);

            // Calculate weighted overall score
            confidence.Overall = 
                (confidence.FormatMatch * criteria.FormatWeight) +
                (confidence.KeywordMatch * criteria.KeywordWeight) +
                (confidence.PatternMatch * criteria.PatternWeight) +
                (confidence.StructureMatch * criteria.StructureWeight) +
                (confidence.MetadataMatch * criteria.MetadataWeight) +
                (confidence.FileNameMatch * criteria.FileNameWeight);

            // Add detailed scores for analysis
            confidence.DetailedScores = new Dictionary<string, double>
            {
                { "RequiredKeywordMatch", CalculateRequiredKeywordMatch(documentFingerprint.ContentKeywords, templateFingerprint.RequiredKeywords) },
                { "ComplexityMatch", CalculateComplexityMatch(documentFingerprint, templateFingerprint) },
                { "LanguageMatch", CalculateLanguageMatch(documentFingerprint.Language, templateFingerprint) }
            };

            return confidence;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error calculating confidence score");
            return new ConfidenceScore();
        }
    }

    #endregion

    #region Template Learning and Optimization

    public async Task<ImportTemplate> LearnFromSuccessfulMatchAsync(Guid templateId, string documentPath, bool userConfirmed, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Learning from successful match: Template {templateId}, Document {documentPath}, UserConfirmed: {userConfirmed}", 
                templateId, documentPath, userConfirmed);

            var template = await _templateService.GetTemplateAsync(templateId, cancellationToken);
            if (template == null)
            {
                throw new ArgumentException($"Template {templateId} not found");
            }

            if (userConfirmed)
            {
                // Extract patterns and keywords from successful document
                var documentFingerprint = await CreateDocumentFingerprintAsync(documentPath, cancellationToken);
                
                // Update template with learned patterns
                var updatedTemplate = EnhanceTemplateFromDocument(template, documentFingerprint);
                return await _templateService.UpdateTemplateAsync(updatedTemplate, cancellationToken);
            }

            return template;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error learning from successful match: Template {TemplateId}", templateId);
            throw;
        }
    }

    public async Task<ImportTemplate> OptimizeTemplateMatchingAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Optimizing template matching for: {TemplateId}", templateId);

            var template = await _templateService.GetTemplateAsync(templateId, cancellationToken);
            if (template == null)
            {
                throw new ArgumentException($"Template {templateId} not found");
            }

            // Analyze template performance and optimize
            var performance = await AnalyzeTemplatePerformanceAsync(templateId, cancellationToken);
            var optimizedTemplate = ApplyOptimizations(template, performance);

            return await _templateService.UpdateTemplateAsync(optimizedTemplate, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error optimizing template: {TemplateId}", templateId);
            throw;
        }
    }

    public async Task<TemplatePerformanceAnalysis> AnalyzeTemplatePerformanceAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Analyzing template performance for: {TemplateId}", templateId);

            var template = await _templateService.GetTemplateAsync(templateId, cancellationToken);
            if (template == null)
            {
                throw new ArgumentException($"Template {templateId} not found");
            }

            var analysis = new TemplatePerformanceAnalysis
            {
                TemplateId = templateId,
                AnalyzedAt = DateTime.UtcNow,
                PerformanceScore = CalculatePerformanceScore(template),
                MatchAccuracy = template.UsageStats.SuccessRate,
                AverageConfidence = template.UsageStats.AverageConfidence,
                Suggestions = GenerateOptimizationSuggestions(template),
                CategoryMetrics = CalculateCategoryMetrics(template)
            };

            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing template performance: {TemplateId}", templateId);
            throw;
        }
    }

    #endregion

    #region Batch Operations

    public async Task<BatchMatchResult> MatchDocumentsBatchAsync(IEnumerable<string> documentPaths, IEnumerable<ImportTemplate> availableTemplates, double minimumConfidence = 0.5, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting batch matching for {documentCount} documents with {templateCount} templates", 
                documentPaths.Count(), availableTemplates.Count());

            var startTime = DateTime.UtcNow;
            var documentMatches = new Dictionary<string, TemplateMatchResult?>();
            var unmatchedDocuments = new List<string>();

            var semaphore = new SemaphoreSlim(_settings.MaxConcurrentOperations);
            var tasks = documentPaths.Select(async documentPath =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    var bestMatch = await FindBestMatchAsync(documentPath, availableTemplates, cancellationToken);
                    
                    if (bestMatch != null && bestMatch.ConfidenceScore >= minimumConfidence)
                    {
                        documentMatches[documentPath] = bestMatch;
                    }
                    else
                    {
                        documentMatches[documentPath] = null;
                        unmatchedDocuments.Add(documentPath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error processing document in batch: {DocumentPath}", documentPath);
                    documentMatches[documentPath] = null;
                    unmatchedDocuments.Add(documentPath);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);

            var totalTime = DateTime.UtcNow - startTime;
            var successfulMatches = documentMatches.Values.Where(m => m != null).ToList();

            var result = new BatchMatchResult
            {
                DocumentMatches = documentMatches,
                UnmatchedDocuments = unmatchedDocuments,
                SuccessRate = successfulMatches.Count / (double)documentPaths.Count(),
                TotalProcessingTime = totalTime,
                AverageConfidence = successfulMatches.Any() ? successfulMatches.Average(m => m!.ConfidenceScore) : 0.0
            };

            _logger.LogInformation("Batch matching completed: {successRate:P1} success rate, {averageConfidence:F2} average confidence", 
                result.SuccessRate, result.AverageConfidence);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in batch document matching");
            throw;
        }
    }

    public async Task<IDictionary<string, DocumentFingerprint>> CreateDocumentFingerprintsBatchAsync(IEnumerable<string> documentPaths, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating fingerprints for {documentCount} documents", documentPaths.Count());

            var fingerprints = new Dictionary<string, DocumentFingerprint>();
            var semaphore = new SemaphoreSlim(_settings.MaxConcurrentOperations);

            var tasks = documentPaths.Select(async documentPath =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    var fingerprint = await CreateDocumentFingerprintAsync(documentPath, cancellationToken);
                    fingerprints[documentPath] = fingerprint;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error creating fingerprint for document: {DocumentPath}", documentPath);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);

            _logger.LogInformation("Created {fingerprintCount} fingerprints successfully", fingerprints.Count);
            return fingerprints;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in batch fingerprint creation");
            throw;
        }
    }

    #endregion

    #region Configuration and Settings

    public async Task UpdateMatchingSettingsAsync(MatchingSettings settings, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating matching settings");
            
            // Update internal settings
            _settings.DefaultCriteria = settings.DefaultCriteria;
            _settings.EnableFuzzyMatching = settings.EnableFuzzyMatching;
            _settings.FuzzyMatchingThreshold = settings.FuzzyMatchingThreshold;
            _settings.EnableFingerprintCaching = settings.EnableFingerprintCaching;
            _settings.CacheExpirationHours = settings.CacheExpirationHours;
            _settings.MaxConcurrentOperations = settings.MaxConcurrentOperations;
            _settings.EnableAnalytics = settings.EnableAnalytics;

            // Clear caches if caching was disabled
            if (!settings.EnableFingerprintCaching)
            {
                _fingerprintCache.Clear();
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating matching settings");
            throw;
        }
    }

    public async Task<MatchingSettings> GetMatchingSettingsAsync(CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        return _settings;
    }

    #endregion

    #region Private Helper Methods

    private string GetDocumentFormat(string documentPath)
    {
        var extension = Path.GetExtension(documentPath).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => "pdf",
            ".docx" => "docx",
            ".doc" => "doc",
            ".xlsx" => "xlsx",
            ".xls" => "xls",
            ".txt" => "txt",
            ".rtf" => "rtf",
            _ => "unknown"
        };
    }

    private string ComputeContentHash(string content)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(content ?? string.Empty));
        return Convert.ToBase64String(hash);
    }

    private string DetectLanguage(string text)
    {
        // Simple language detection - can be enhanced with proper NLP libraries
        if (string.IsNullOrWhiteSpace(text))
            return "en";

        // Basic heuristics for common languages
        var commonEnglishWords = new[] { "the", "and", "or", "but", "in", "on", "at", "to", "for", "of", "with", "by" };
        var englishMatches = commonEnglishWords.Sum(word => Regex.Matches(text.ToLowerInvariant(), @"\b" + word + @"\b").Count);
        
        return englishMatches > 10 ? "en" : "unknown";
    }

    private int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        return text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    private bool ContainsTablePatterns(string text)
    {
        // Look for table-like patterns
        var lines = text.Split('\n');
        return lines.Any(line => line.Split('\t').Length > 3) || 
               Regex.IsMatch(text, @"\|\s*[^|]+\s*\|", RegexOptions.Multiline);
    }

    private DocumentLayoutType DetermineLayoutType(string text)
    {
        if (ContainsTablePatterns(text))
            return DocumentLayoutType.Table;
        
        if (Regex.IsMatch(text, @"(name|address|phone|email).*:.*", RegexOptions.IgnoreCase))
            return DocumentLayoutType.Form;
        
        if (text.Contains("Dear ") || text.Contains("Sincerely"))
            return DocumentLayoutType.Letter;
        
        return DocumentLayoutType.Standard;
    }

    private List<string> ExtractKeywords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<string>();

        // Extract significant words (3+ characters, not common stop words)
        var stopWords = new HashSet<string> { "the", "and", "or", "but", "in", "on", "at", "to", "for", "of", "with", "by", "from", "was", "were", "been", "have", "has", "had", "will", "would", "could", "should", "may", "might", "can", "must", "shall", "this", "that", "these", "those" };
        
        var words = Regex.Matches(text.ToLowerInvariant(), @"\b[a-z]{3,}\b")
            .Cast<Match>()
            .Select(m => m.Value)
            .Where(word => !stopWords.Contains(word))
            .GroupBy(word => word)
            .OrderByDescending(g => g.Count())
            .Take(50)
            .Select(g => g.Key)
            .ToList();

        return words;
    }

    private List<string> ExtractTextPatterns(string text)
    {
        var patterns = new List<string>();

        // Common document patterns
        if (Regex.IsMatch(text, @"\b\d{4}-\d{2}-\d{2}\b"))
            patterns.Add("date_iso");
        
        if (Regex.IsMatch(text, @"\b\d{1,2}/\d{1,2}/\d{4}\b"))
            patterns.Add("date_us");
        
        if (Regex.IsMatch(text, @"\b[A-Z]{2,4}-\d{4,6}\b"))
            patterns.Add("ticket_number");
        
        if (Regex.IsMatch(text, @"\b[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}\b"))
            patterns.Add("email");
        
        if (Regex.IsMatch(text, @"\b\d{3}-\d{3}-\d{4}\b"))
            patterns.Add("phone_us");

        return patterns;
    }

    private Dictionary<string, string> ExtractMetadataFingerprint(Dictionary<string, object> metadata)
    {
        var fingerprint = new Dictionary<string, string>();

        foreach (var kvp in metadata)
        {
            fingerprint[kvp.Key] = kvp.Value?.ToString() ?? string.Empty;
        }

        return fingerprint;
    }

    private double CalculateTemplateComplexity(ImportTemplate template)
    {
        var complexity = 0.0;
        
        // Base complexity from field count
        complexity += template.Fields.Count * 0.1;
        
        // Complexity from extraction methods
        foreach (var field in template.Fields)
        {
            complexity += field.ExtractionMethod switch
            {
                ExtractionMethod.Text => 0.1,
                ExtractionMethod.Coordinates => 0.3,
                ExtractionMethod.OCR => 0.5,
                ExtractionMethod.Metadata => 0.2,
                ExtractionMethod.Hybrid => 0.7,
                _ => 0.1
            };
            
            // Add complexity for patterns and validation
            complexity += field.TextPatterns.Count * 0.05;
            complexity += field.ExtractionZones.Count * 0.1;
        }

        return Math.Min(complexity, 10.0); // Cap at 10.0
    }

    private List<string> ExtractTemplateKeywords(ImportTemplate template)
    {
        var keywords = new List<string>();
        
        foreach (var field in template.Fields)
        {
            keywords.AddRange(field.Keywords);
            
            // Extract keywords from patterns
            foreach (var pattern in field.TextPatterns)
            {
                // Simple extraction of literal text from regex patterns
                var literalParts = Regex.Replace(pattern, @"[^\w\s]", " ")
                    .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(part => part.Length > 2);
                keywords.AddRange(literalParts);
            }
        }

        return keywords.Distinct().ToList();
    }

    private List<string> ExtractRequiredKeywords(ImportTemplate template)
    {
        return template.Fields
            .Where(f => f.IsRequired)
            .SelectMany(f => f.Keywords)
            .Distinct()
            .ToList();
    }

    private List<string> ExtractTemplatePatterns(ImportTemplate template)
    {
        return template.Fields
            .SelectMany(f => f.TextPatterns)
            .Distinct()
            .ToList();
    }

    private DocumentStructure InferExpectedStructure(ImportTemplate template)
    {
        // Infer expected document structure from template configuration
        var structure = new DocumentStructure
        {
            PageCount = 1, // Default assumption
            WordCount = 0,  // Will be determined at runtime
            HasTables = template.Fields.Any(f => f.ExtractionMethod == ExtractionMethod.Coordinates),
            HasImages = false, // Cannot infer from template
            IsScanned = template.Fields.Any(f => f.ExtractionMethod == ExtractionMethod.OCR),
            LayoutType = DocumentLayoutType.Standard
        };

        // Infer layout type from field types
        if (template.Fields.Any(f => f.FieldType == TemplateFieldType.Username || f.FieldType == TemplateFieldType.Email))
            structure.LayoutType = DocumentLayoutType.Form;

        return structure;
    }

    private double CalculateFormatSimilarity(string documentFormat, List<string> supportedFormats)
    {
        if (supportedFormats.Contains(documentFormat, StringComparer.OrdinalIgnoreCase))
            return 1.0;
        
        // Partial matches for compatible formats
        if (documentFormat == "docx" && supportedFormats.Contains("doc", StringComparer.OrdinalIgnoreCase))
            return 0.8;
        if (documentFormat == "xlsx" && supportedFormats.Contains("xls", StringComparer.OrdinalIgnoreCase))
            return 0.8;
        
        return 0.0;
    }

    private double CalculateKeywordSimilarity(List<string> documentKeywords, List<string> templateKeywords)
    {
        if (!templateKeywords.Any())
            return 1.0; // No requirements = perfect match
        
        if (!documentKeywords.Any())
            return 0.0;

        var matches = templateKeywords.Count(tk => documentKeywords.Contains(tk, StringComparer.OrdinalIgnoreCase));
        return (double)matches / templateKeywords.Count;
    }

    private double CalculatePatternSimilarity(List<string> documentPatterns, List<string> templatePatterns)
    {
        if (!templatePatterns.Any())
            return 1.0;
        
        var matches = 0;
        foreach (var templatePattern in templatePatterns)
        {
            if (documentPatterns.Any(dp => string.Equals(dp, templatePattern, StringComparison.OrdinalIgnoreCase)))
                matches++;
        }
        
        return (double)matches / templatePatterns.Count;
    }

    private double CalculateStructureSimilarity(DocumentStructure documentStructure, DocumentStructure expectedStructure)
    {
        var similarities = new List<double>();
        
        // Page count similarity (less important for single page docs)
        if (expectedStructure.PageCount > 0)
        {
            var pageRatio = Math.Min(documentStructure.PageCount, expectedStructure.PageCount) / 
                           (double)Math.Max(documentStructure.PageCount, expectedStructure.PageCount);
            similarities.Add(pageRatio);
        }
        
        // Layout type match
        similarities.Add(documentStructure.LayoutType == expectedStructure.LayoutType ? 1.0 : 0.5);
        
        // Feature matches
        similarities.Add(documentStructure.HasTables == expectedStructure.HasTables ? 1.0 : 0.0);
        similarities.Add(documentStructure.IsScanned == expectedStructure.IsScanned ? 1.0 : 0.0);
        
        return similarities.Average();
    }

    private double CalculateMetadataMatch(Dictionary<string, string> documentMetadata, TemplateFingerprint templateFingerprint)
    {
        // Basic metadata matching - can be enhanced based on specific requirements
        return 0.5; // Placeholder implementation
    }

    private double CalculateFileNameMatch(string documentPath, TemplateFingerprint templateFingerprint)
    {
        var fileName = Path.GetFileNameWithoutExtension(documentPath).ToLowerInvariant();
        
        // Check if filename contains template-related keywords
        var matches = templateFingerprint.ExpectedKeywords
            .Count(keyword => fileName.Contains(keyword.ToLowerInvariant()));
        
        return templateFingerprint.ExpectedKeywords.Any() ? 
            (double)matches / templateFingerprint.ExpectedKeywords.Count : 0.0;
    }

    private double CalculateRequiredKeywordMatch(List<string> documentKeywords, List<string> requiredKeywords)
    {
        if (!requiredKeywords.Any())
            return 1.0;
        
        var matches = requiredKeywords.Count(rk => documentKeywords.Contains(rk, StringComparer.OrdinalIgnoreCase));
        return (double)matches / requiredKeywords.Count;
    }

    private double CalculateComplexityMatch(DocumentFingerprint documentFingerprint, TemplateFingerprint templateFingerprint)
    {
        // Assess if document complexity matches template expectations
        var docComplexity = documentFingerprint.Structure.WordCount / 100.0 + 
                           documentFingerprint.ContentKeywords.Count / 10.0 +
                           documentFingerprint.Structure.PageCount;
        
        var ratio = Math.Min(docComplexity, templateFingerprint.ComplexityScore) / 
                   Math.Max(docComplexity, templateFingerprint.ComplexityScore);
        
        return ratio;
    }

    private double CalculateLanguageMatch(string documentLanguage, TemplateFingerprint templateFingerprint)
    {
        // Simple language matching - can be enhanced
        return documentLanguage == "en" ? 1.0 : 0.8; // Assume most templates are English-focused
    }

    private List<string> GenerateMatchReasons(ConfidenceScore confidence)
    {
        var reasons = new List<string>();
        
        if (confidence.FormatMatch > 0.8)
            reasons.Add("Document format matches template requirements");
        
        if (confidence.KeywordMatch > 0.6)
            reasons.Add("High keyword similarity detected");
        
        if (confidence.PatternMatch > 0.7)
            reasons.Add("Text patterns match template expectations");
        
        if (confidence.StructureMatch > 0.5)
            reasons.Add("Document structure is compatible");
        
        return reasons;
    }

    private List<string> GenerateMatchWarnings(ConfidenceScore confidence)
    {
        var warnings = new List<string>();
        
        if (confidence.FormatMatch < 0.5)
            warnings.Add("Document format may not be fully supported");
        
        if (confidence.KeywordMatch < 0.3)
            warnings.Add("Low keyword match - manual verification recommended");
        
        if (confidence.Overall < 0.6)
            warnings.Add("Low overall confidence - consider manual template selection");
        
        return warnings;
    }

    private ImportTemplate EnhanceTemplateFromDocument(ImportTemplate template, DocumentFingerprint documentFingerprint)
    {
        // Add successful patterns and keywords to template
        var enhancedTemplate = template.CreateCopy();
        
        // Add new keywords from successful match
        foreach (var field in enhancedTemplate.Fields)
        {
            var relevantKeywords = documentFingerprint.ContentKeywords
                .Where(k => !field.Keywords.Contains(k, StringComparer.OrdinalIgnoreCase))
                .Take(5); // Limit to avoid keyword bloat
            
            field.Keywords.AddRange(relevantKeywords);
        }
        
        return enhancedTemplate;
    }

    private ImportTemplate ApplyOptimizations(ImportTemplate template, TemplatePerformanceAnalysis performance)
    {
        var optimizedTemplate = template.CreateCopy();
        
        // Apply suggestions from performance analysis
        foreach (var suggestion in performance.Suggestions)
        {
            // Implementation would depend on specific optimization rules
            _logger.LogInformation("Applying optimization: {Suggestion}", suggestion);
        }
        
        return optimizedTemplate;
    }

    private double CalculatePerformanceScore(ImportTemplate template)
    {
        // Calculate overall performance score based on usage statistics
        return template.UsageStats.SuccessRate * 0.6 + 
               template.UsageStats.AverageConfidence * 0.4;
    }

    private List<string> GenerateOptimizationSuggestions(ImportTemplate template)
    {
        var suggestions = new List<string>();
        
        if (template.UsageStats.SuccessRate < 0.7)
            suggestions.Add("Consider adding more specific keywords to improve matching accuracy");
        
        if (template.UsageStats.AverageConfidence < 0.6)
            suggestions.Add("Review extraction patterns for better confidence scoring");
        
        if (template.Fields.Count > 10)
            suggestions.Add("Consider simplifying template by reducing non-essential fields");
        
        return suggestions;
    }

    private Dictionary<string, double> CalculateCategoryMetrics(ImportTemplate template)
    {
        return new Dictionary<string, double>
        {
            { "Complexity", CalculateTemplateComplexity(template) },
            { "FieldCount", template.Fields.Count },
            { "RequiredFields", template.Fields.Count(f => f.IsRequired) },
            { "CoordinateFields", template.Fields.Count(f => f.ExtractionMethod == ExtractionMethod.Coordinates) }
        };
    }

    #endregion
} 