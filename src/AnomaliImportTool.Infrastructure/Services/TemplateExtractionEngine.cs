using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AnomaliImportTool.Core.Models;
using Microsoft.Extensions.Logging;

namespace AnomaliImportTool.Infrastructure.Services
{
    /// <summary>
    /// Extracts field values from documents using template-based extraction methods
    /// Supports regex patterns, keyword matching, coordinate zones, and OCR
    /// </summary>
    public class TemplateExtractionEngine
    {
        private readonly ILogger<TemplateExtractionEngine> _logger;
        private readonly Dictionary<string, Regex> _compiledRegexCache;

        public TemplateExtractionEngine(ILogger<TemplateExtractionEngine> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _compiledRegexCache = new Dictionary<string, Regex>();
        }

        /// <summary>
        /// Extracts all field values from a document using the specified template
        /// </summary>
        /// <param name="document">Document to extract from</param>
        /// <param name="template">Template defining extraction rules</param>
        /// <returns>Extraction results with confidence scores</returns>
        public async Task<TemplateExtractionResult> ExtractFieldsAsync(Document document, ImportTemplate template)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (template == null) throw new ArgumentNullException(nameof(template));

            _logger.LogInformation("Starting field extraction for document {DocumentId} using template {TemplateId}",
                document.Id, template.Id);

            var result = new TemplateExtractionResult
            {
                DocumentId = document.Id,
                TemplateId = template.Id,
                ExtractionStartTime = DateTime.UtcNow
            };

            try
            {
                // Process fields in priority order
                var orderedFields = template.Fields
                    .Where(f => f.IsActive)
                    .OrderBy(f => f.ProcessingOrder)
                    .ThenBy(f => f.Name);

                foreach (var field in orderedFields)
                {
                    var fieldResult = await ExtractFieldAsync(document, field, result);
                    result.FieldResults[field.Name] = fieldResult;

                    // Update overall confidence
                    result.OverallConfidence = CalculateOverallConfidence(result.FieldResults.Values);
                }

                result.IsSuccessful = result.FieldResults.Values.Any(r => r.IsSuccessful);
                result.ExtractionEndTime = DateTime.UtcNow;

                _logger.LogInformation("Field extraction completed for document {DocumentId}. Success: {IsSuccessful}, Confidence: {Confidence:F2}",
                    document.Id, result.IsSuccessful, result.OverallConfidence);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during field extraction for document {DocumentId}", document.Id);
                result.IsSuccessful = false;
                result.ErrorMessage = $"Extraction failed: {ex.Message}";
                result.ExtractionEndTime = DateTime.UtcNow;
                return result;
            }
        }

        /// <summary>
        /// Extracts a single field value from a document
        /// </summary>
        /// <param name="document">Document to extract from</param>
        /// <param name="field">Field definition</param>
        /// <param name="extractionContext">Current extraction context</param>
        /// <returns>Field extraction result</returns>
        private async Task<FieldExtractionResult> ExtractFieldAsync(Document document, TemplateField field, TemplateExtractionResult extractionContext)
        {
            var result = new FieldExtractionResult
            {
                FieldName = field.Name,
                FieldType = field.FieldType,
                ExtractionMethod = field.ExtractionMethod,
                ProcessingStartTime = DateTime.UtcNow,
                IsRequired = field.IsRequired
            };

            try
            {
                _logger.LogDebug("Extracting field {FieldName} using method {ExtractionMethod}", field.Name, field.ExtractionMethod);

                // Try primary extraction method
                var primaryResult = await ExtractUsingMethod(document, field, field.ExtractionMethod);
                
                if (primaryResult.IsSuccessful && primaryResult.Confidence >= field.ConfidenceThreshold)
                {
                    result = primaryResult;
                }
                else if (field.Fallback.EnableFallback)
                {
                    // Try fallback methods
                    result = await TryFallbackMethods(document, field, primaryResult);
                }
                else
                {
                    result = primaryResult;
                }

                // Apply data transformations
                if (result.IsSuccessful && !string.IsNullOrEmpty(result.ExtractedValue))
                {
                    result.ExtractedValue = ApplyTransformations(result.ExtractedValue, field.Transformation);
                }

                // Validate extracted value
                var validationResult = ValidateExtractedValue(result.ExtractedValue, field);
                result.ValidationResult = validationResult;
                
                if (!validationResult.IsValid)
                {
                    result.IsSuccessful = false;
                    result.Confidence = Math.Min(result.Confidence, 0.5); // Reduce confidence for invalid values
                    _logger.LogWarning("Field {FieldName} validation failed: {Errors}", field.Name, string.Join(", ", validationResult.Errors));
                }

                // Use default value if extraction failed and default is specified
                if (!result.IsSuccessful && !string.IsNullOrEmpty(field.DefaultValue))
                {
                    result.ExtractedValue = field.DefaultValue;
                    result.IsSuccessful = true;
                    result.Confidence = 0.1; // Low confidence for default values
                    result.ExtractionMethod = ExtractionMethod.Default;
                    _logger.LogDebug("Using default value for field {FieldName}: {DefaultValue}", field.Name, field.DefaultValue);
                }

                result.ProcessingEndTime = DateTime.UtcNow;
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting field {FieldName}", field.Name);
                result.IsSuccessful = false;
                result.ErrorMessage = $"Field extraction failed: {ex.Message}";
                result.ProcessingEndTime = DateTime.UtcNow;
                return result;
            }
        }

        /// <summary>
        /// Extracts field value using the specified extraction method
        /// </summary>
        private async Task<FieldExtractionResult> ExtractUsingMethod(Document document, TemplateField field, ExtractionMethod method)
        {
            var result = new FieldExtractionResult
            {
                FieldName = field.Name,
                FieldType = field.FieldType,
                ExtractionMethod = method,
                ProcessingStartTime = DateTime.UtcNow,
                IsRequired = field.IsRequired
            };

            switch (method)
            {
                case ExtractionMethod.Text:
                    return await ExtractFromText(document, field);

                case ExtractionMethod.Coordinates:
                    return await ExtractFromCoordinates(document, field);

                case ExtractionMethod.OCR:
                    return await ExtractFromOCR(document, field);

                case ExtractionMethod.Metadata:
                    return await ExtractFromMetadata(document, field);

                case ExtractionMethod.Hybrid:
                    return await ExtractUsingHybrid(document, field);

                default:
                    result.IsSuccessful = false;
                    result.ErrorMessage = $"Unsupported extraction method: {method}";
                    return result;
            }
        }

        /// <summary>
        /// Extracts field value from document text using patterns and keywords
        /// </summary>
        private async Task<FieldExtractionResult> ExtractFromText(Document document, TemplateField field)
        {
            var result = new FieldExtractionResult
            {
                FieldName = field.Name,
                FieldType = field.FieldType,
                ExtractionMethod = ExtractionMethod.Text,
                ProcessingStartTime = DateTime.UtcNow,
                IsRequired = field.IsRequired
            };

            if (string.IsNullOrEmpty(document.ExtractedText))
            {
                result.IsSuccessful = false;
                result.ErrorMessage = "Document has no extracted text content";
                return result;
            }

            var documentText = document.ExtractedText;
            var extractedValues = new List<string>();
            var maxConfidence = 0.0;

            // Try regex patterns first
            foreach (var pattern in field.TextPatterns)
            {
                try
                {
                    var regex = GetCompiledRegex(pattern);
                    var matches = regex.Matches(documentText);

                    foreach (Match match in matches)
                    {
                        var value = match.Groups.Count > 1 ? match.Groups[1].Value : match.Value;
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            extractedValues.Add(value.Trim());
                            maxConfidence = Math.Max(maxConfidence, 0.9); // High confidence for regex matches
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error processing regex pattern {Pattern} for field {FieldName}", pattern, field.Name);
                }
            }

            // Try keyword-based extraction if no regex matches
            if (extractedValues.Count == 0)
            {
                foreach (var keyword in field.Keywords)
                {
                    var keywordMatches = ExtractUsingKeyword(documentText, keyword, field.FieldType);
                    extractedValues.AddRange(keywordMatches);
                    if (keywordMatches.Count > 0)
                    {
                        maxConfidence = Math.Max(maxConfidence, 0.7); // Medium confidence for keyword matches
                    }
                }
            }

            if (extractedValues.Count > 0)
            {
                result.IsSuccessful = true;
                result.ExtractedValue = field.AllowMultipleValues 
                    ? string.Join(field.MultiValueSeparator, extractedValues.Distinct())
                    : extractedValues.First();
                result.Confidence = maxConfidence;
                result.AllExtractedValues = extractedValues.Distinct().ToList();
            }
            else
            {
                result.IsSuccessful = false;
                result.ErrorMessage = "No matches found for text patterns or keywords";
                result.Confidence = 0.0;
            }

            result.ProcessingEndTime = DateTime.UtcNow;
            return result;
        }

        /// <summary>
        /// Extracts field value from specific coordinate zones
        /// </summary>
        private async Task<FieldExtractionResult> ExtractFromCoordinates(Document document, TemplateField field)
        {
            var result = new FieldExtractionResult
            {
                FieldName = field.Name,
                FieldType = field.FieldType,
                ExtractionMethod = ExtractionMethod.Coordinates,
                ProcessingStartTime = DateTime.UtcNow,
                IsRequired = field.IsRequired
            };

            if (field.ExtractionZones.Count == 0)
            {
                result.IsSuccessful = false;
                result.ErrorMessage = "No extraction zones defined for coordinate-based extraction";
                return result;
            }

            var extractedValues = new List<string>();
            var maxConfidence = 0.0;

            // Process each extraction zone
            foreach (var zone in field.ExtractionZones.Where(z => z.IsActive).OrderBy(z => z.Priority))
            {
                try
                {
                    var zoneResult = await ExtractFromZone(document, zone);
                    if (zoneResult.IsSuccessful)
                    {
                        extractedValues.AddRange(zoneResult.ExtractedValues);
                        maxConfidence = Math.Max(maxConfidence, zoneResult.Confidence);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error extracting from zone {ZoneId} for field {FieldName}", zone.Id, field.Name);
                }
            }

            if (extractedValues.Count > 0)
            {
                result.IsSuccessful = true;
                result.ExtractedValue = field.AllowMultipleValues 
                    ? string.Join(field.MultiValueSeparator, extractedValues.Distinct())
                    : extractedValues.First();
                result.Confidence = maxConfidence;
                result.AllExtractedValues = extractedValues.Distinct().ToList();
            }
            else
            {
                result.IsSuccessful = false;
                result.ErrorMessage = "No text extracted from coordinate zones";
                result.Confidence = 0.0;
            }

            result.ProcessingEndTime = DateTime.UtcNow;
            return result;
        }

        /// <summary>
        /// Extracts field value using OCR on specific zones
        /// </summary>
        private async Task<FieldExtractionResult> ExtractFromOCR(Document document, TemplateField field)
        {
            var result = new FieldExtractionResult
            {
                FieldName = field.Name,
                FieldType = field.FieldType,
                ExtractionMethod = ExtractionMethod.OCR,
                ProcessingStartTime = DateTime.UtcNow,
                IsRequired = field.IsRequired
            };

            // OCR extraction would require integration with OCR service
            // For now, return a placeholder implementation
            result.IsSuccessful = false;
            result.ErrorMessage = "OCR extraction not yet implemented";
            result.ProcessingEndTime = DateTime.UtcNow;

            // TODO: Implement OCR extraction using Tesseract or similar
            // This would involve:
            // 1. Converting document pages to images
            // 2. Extracting regions defined by extraction zones
            // 3. Running OCR on those regions
            // 4. Applying text patterns and keywords to OCR results

            return result;
        }

        /// <summary>
        /// Extracts field value from document metadata
        /// </summary>
        private async Task<FieldExtractionResult> ExtractFromMetadata(Document document, TemplateField field)
        {
            var result = new FieldExtractionResult
            {
                FieldName = field.Name,
                FieldType = field.FieldType,
                ExtractionMethod = ExtractionMethod.Metadata,
                ProcessingStartTime = DateTime.UtcNow,
                IsRequired = field.IsRequired
            };

            var extractedValues = new List<string>();

            // Try to extract from known metadata fields based on field type
            switch (field.FieldType)
            {
                case TemplateFieldType.Username:
                    if (!string.IsNullOrEmpty(document.Author))
                        extractedValues.Add(document.Author);
                    if (!string.IsNullOrEmpty(document.Creator))
                        extractedValues.Add(document.Creator);
                    break;

                case TemplateFieldType.Date:
                    if (document.DocumentDate.HasValue)
                        extractedValues.Add(document.DocumentDate.Value.ToString("yyyy-MM-dd"));
                    if (document.CreationDate.HasValue)
                        extractedValues.Add(document.CreationDate.Value.ToString("yyyy-MM-dd"));
                    break;

                case TemplateFieldType.Text:
                    if (!string.IsNullOrEmpty(document.Title))
                        extractedValues.Add(document.Title);
                    if (!string.IsNullOrEmpty(document.Subject))
                        extractedValues.Add(document.Subject);
                    break;
            }

            // Check custom properties
            foreach (var property in document.CustomProperties)
            {
                if (field.Keywords.Any(k => property.Key.Contains(k, StringComparison.OrdinalIgnoreCase)))
                {
                    extractedValues.Add(property.Value?.ToString() ?? "");
                }
            }

            // Check extracted fields
            foreach (var extractedField in document.ExtractedFields)
            {
                if (field.Keywords.Any(k => extractedField.Key.Contains(k, StringComparison.OrdinalIgnoreCase)))
                {
                    extractedValues.Add(extractedField.Value);
                }
            }

            if (extractedValues.Count > 0)
            {
                result.IsSuccessful = true;
                result.ExtractedValue = field.AllowMultipleValues 
                    ? string.Join(field.MultiValueSeparator, extractedValues.Distinct())
                    : extractedValues.First();
                result.Confidence = 0.8; // Good confidence for metadata extraction
                result.AllExtractedValues = extractedValues.Distinct().ToList();
            }
            else
            {
                result.IsSuccessful = false;
                result.ErrorMessage = "No matching metadata found";
                result.Confidence = 0.0;
            }

            result.ProcessingEndTime = DateTime.UtcNow;
            return result;
        }

        /// <summary>
        /// Extracts field value using hybrid approach (combines multiple methods)
        /// </summary>
        private async Task<FieldExtractionResult> ExtractUsingHybrid(Document document, TemplateField field)
        {
            var result = new FieldExtractionResult
            {
                FieldName = field.Name,
                FieldType = field.FieldType,
                ExtractionMethod = ExtractionMethod.Hybrid,
                ProcessingStartTime = DateTime.UtcNow,
                IsRequired = field.IsRequired
            };

            var allResults = new List<FieldExtractionResult>();

            // Try all extraction methods
            var methods = new[] { ExtractionMethod.Text, ExtractionMethod.Coordinates, ExtractionMethod.Metadata };
            
            foreach (var method in methods)
            {
                try
                {
                    var methodResult = await ExtractUsingMethod(document, field, method);
                    if (methodResult.IsSuccessful)
                    {
                        allResults.Add(methodResult);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error in hybrid extraction using method {Method} for field {FieldName}", method, field.Name);
                }
            }

            if (allResults.Count > 0)
            {
                // Use the result with highest confidence
                var bestResult = allResults.OrderByDescending(r => r.Confidence).First();
                result.IsSuccessful = true;
                result.ExtractedValue = bestResult.ExtractedValue;
                result.Confidence = bestResult.Confidence;
                result.AllExtractedValues = bestResult.AllExtractedValues;
                result.ExtractionMethod = bestResult.ExtractionMethod; // Record which method was actually used
            }
            else
            {
                result.IsSuccessful = false;
                result.ErrorMessage = "No successful extraction from any hybrid method";
                result.Confidence = 0.0;
            }

            result.ProcessingEndTime = DateTime.UtcNow;
            return result;
        }

        /// <summary>
        /// Tries fallback extraction methods if primary method fails
        /// </summary>
        private async Task<FieldExtractionResult> TryFallbackMethods(Document document, TemplateField field, FieldExtractionResult primaryResult)
        {
            var bestResult = primaryResult;

            foreach (var fallbackMethod in field.Fallback.FallbackMethods)
            {
                try
                {
                    var fallbackResult = await ExtractUsingMethod(document, field, fallbackMethod);
                    if (fallbackResult.IsSuccessful && fallbackResult.Confidence > bestResult.Confidence)
                    {
                        bestResult = fallbackResult;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error in fallback extraction using method {Method} for field {FieldName}", fallbackMethod, field.Name);
                }
            }

            // Try fallback patterns if specified
            if (field.Fallback.FallbackPatterns.Count > 0 && !bestResult.IsSuccessful)
            {
                var fallbackField = new TemplateField
                {
                    Name = field.Name,
                    FieldType = field.FieldType,
                    ExtractionMethod = ExtractionMethod.Text,
                    TextPatterns = field.Fallback.FallbackPatterns,
                    AllowMultipleValues = field.AllowMultipleValues,
                    MultiValueSeparator = field.MultiValueSeparator
                };

                var fallbackResult = await ExtractFromText(document, fallbackField);
                if (fallbackResult.IsSuccessful && fallbackResult.Confidence > bestResult.Confidence)
                {
                    bestResult = fallbackResult;
                }
            }

            return bestResult;
        }

        /// <summary>
        /// Extracts text from a specific coordinate zone
        /// </summary>
        private async Task<ZoneExtractionResult> ExtractFromZone(Document document, ExtractionZone zone)
        {
            var result = new ZoneExtractionResult
            {
                ZoneId = zone.Id,
                ZoneName = zone.Name,
                Confidence = 0.0
            };

            // For now, this is a placeholder implementation
            // In a real implementation, this would:
            // 1. Convert coordinates to actual document positions
            // 2. Extract text from the specified region
            // 3. Apply zone-specific settings (OCR, formatting, etc.)

            // Simulate extraction based on zone type
            switch (zone.ZoneType)
            {
                case ExtractionZoneType.Text:
                    // Extract text from coordinate region
                    result.ExtractedValues.Add($"Text from zone {zone.Name}");
                    result.Confidence = 0.6;
                    result.IsSuccessful = true;
                    break;

                case ExtractionZoneType.OCR:
                    // Would use OCR on the zone
                    result.IsSuccessful = false;
                    result.ErrorMessage = "OCR zone extraction not implemented";
                    break;

                default:
                    result.IsSuccessful = false;
                    result.ErrorMessage = $"Zone type {zone.ZoneType} not supported";
                    break;
            }

            return result;
        }

        /// <summary>
        /// Extracts values using keyword-based search
        /// </summary>
        private List<string> ExtractUsingKeyword(string text, string keyword, TemplateFieldType fieldType)
        {
            var results = new List<string>();
            var keywordIndex = text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);

            if (keywordIndex >= 0)
            {
                // Extract text following the keyword based on field type
                var afterKeyword = text.Substring(keywordIndex + keyword.Length);
                
                switch (fieldType)
                {
                    case TemplateFieldType.Username:
                        var usernameMatch = Regex.Match(afterKeyword, @"[\s:]*([a-zA-Z0-9._-]+@[a-zA-Z0-9.-]+|[a-zA-Z0-9._-]+)");
                        if (usernameMatch.Success)
                            results.Add(usernameMatch.Groups[1].Value);
                        break;

                    case TemplateFieldType.TicketNumber:
                        var ticketMatch = Regex.Match(afterKeyword, @"[\s:#]*([A-Z0-9-]+)");
                        if (ticketMatch.Success)
                            results.Add(ticketMatch.Groups[1].Value);
                        break;

                    case TemplateFieldType.Date:
                        var dateMatch = Regex.Match(afterKeyword, @"[\s:]*(\d{1,2}[/-]\d{1,2}[/-]\d{2,4}|\d{4}-\d{2}-\d{2})");
                        if (dateMatch.Success)
                            results.Add(dateMatch.Groups[1].Value);
                        break;

                    case TemplateFieldType.Text:
                    default:
                        // Extract next word or phrase
                        var textMatch = Regex.Match(afterKeyword, @"[\s:]*([^\r\n]{1,100})");
                        if (textMatch.Success)
                            results.Add(textMatch.Groups[1].Value.Trim());
                        break;
                }
            }

            return results;
        }

        /// <summary>
        /// Applies data transformations to extracted values
        /// </summary>
        private string ApplyTransformations(string value, DataTransformation transformation)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            var result = value;

            if (transformation.TrimWhitespace)
                result = result.Trim();

            if (transformation.ToLowerCase)
                result = result.ToLower();

            if (transformation.ToUpperCase)
                result = result.ToUpper();

            if (transformation.RemoveSpecialCharacters)
                result = Regex.Replace(result, @"[^a-zA-Z0-9\s]", "");

            if (transformation.FormatAsDate && DateTime.TryParse(result, out var date))
                result = date.ToString(transformation.DateFormat);

            return result;
        }

        /// <summary>
        /// Validates extracted value against field validation rules
        /// </summary>
        private TemplateValidationResult ValidateExtractedValue(string value, TemplateField field)
        {
            var result = new TemplateValidationResult { IsValid = true };

            if (field.IsRequired && string.IsNullOrEmpty(value))
            {
                result.IsValid = false;
                result.Errors.Add("Required field is empty");
                return result;
            }

            if (!string.IsNullOrEmpty(value))
            {
                var rules = field.ValidationRules;

                if (rules.MinLength.HasValue && value.Length < rules.MinLength.Value)
                {
                    result.IsValid = false;
                    result.Errors.Add($"Value is too short (minimum {rules.MinLength.Value} characters)");
                }

                if (rules.MaxLength.HasValue && value.Length > rules.MaxLength.Value)
                {
                    result.IsValid = false;
                    result.Errors.Add($"Value is too long (maximum {rules.MaxLength.Value} characters)");
                }

                if (!string.IsNullOrEmpty(rules.RegexPattern))
                {
                    try
                    {
                        var regex = GetCompiledRegex(rules.RegexPattern);
                        if (!regex.IsMatch(value))
                        {
                            result.IsValid = false;
                            result.Errors.Add("Value does not match required pattern");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Invalid regex pattern in validation rules: {Pattern}", rules.RegexPattern);
                        result.Warnings.Add("Validation pattern is invalid");
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Gets a compiled regex from cache or creates and caches a new one
        /// </summary>
        private Regex GetCompiledRegex(string pattern)
        {
            if (!_compiledRegexCache.TryGetValue(pattern, out var regex))
            {
                regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                _compiledRegexCache[pattern] = regex;
            }
            return regex;
        }

        /// <summary>
        /// Calculates overall confidence score from individual field results
        /// </summary>
        private double CalculateOverallConfidence(IEnumerable<FieldExtractionResult> fieldResults)
        {
            var results = fieldResults.ToList();
            if (results.Count == 0) return 0.0;

            var successfulResults = results.Where(r => r.IsSuccessful).ToList();
            if (successfulResults.Count == 0) return 0.0;

            // Weighted average based on field importance (required fields have higher weight)
            var totalWeight = 0.0;
            var weightedSum = 0.0;

            foreach (var result in results)
            {
                var weight = result.IsRequired ? 2.0 : 1.0;
                totalWeight += weight;
                
                if (result.IsSuccessful)
                {
                    weightedSum += result.Confidence * weight;
                }
            }

            return totalWeight > 0 ? weightedSum / totalWeight : 0.0;
        }
    }

    /// <summary>
    /// Results of template-based field extraction
    /// </summary>
    public class TemplateExtractionResult
    {
        public Guid DocumentId { get; set; }
        public Guid TemplateId { get; set; }
        public bool IsSuccessful { get; set; }
        public double OverallConfidence { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime ExtractionStartTime { get; set; }
        public DateTime ExtractionEndTime { get; set; }
        public TimeSpan ExtractionDuration => ExtractionEndTime - ExtractionStartTime;
        public Dictionary<string, FieldExtractionResult> FieldResults { get; set; } = new();
        public Dictionary<string, object> ExtractionMetadata { get; set; } = new();
    }

    /// <summary>
    /// Results of individual field extraction
    /// </summary>
    public class FieldExtractionResult
    {
        public string FieldName { get; set; } = string.Empty;
        public TemplateFieldType FieldType { get; set; }
        public ExtractionMethod ExtractionMethod { get; set; }
        public bool IsSuccessful { get; set; }
        public bool IsRequired { get; set; }
        public double Confidence { get; set; }
        public string? ExtractedValue { get; set; }
        public List<string> AllExtractedValues { get; set; } = new();
        public string? ErrorMessage { get; set; }
        public DateTime ProcessingStartTime { get; set; }
        public DateTime ProcessingEndTime { get; set; }
        public TimeSpan ProcessingDuration => ProcessingEndTime - ProcessingStartTime;
        public TemplateValidationResult? ValidationResult { get; set; }
        public Dictionary<string, object> FieldMetadata { get; set; } = new();
    }

    /// <summary>
    /// Results of zone-based extraction
    /// </summary>
    public class ZoneExtractionResult
    {
        public Guid ZoneId { get; set; }
        public string ZoneName { get; set; } = string.Empty;
        public bool IsSuccessful { get; set; }
        public double Confidence { get; set; }
        public List<string> ExtractedValues { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Extended extraction method enum to include default values
    /// </summary>
    public enum ExtractionMethod
    {
        Text,
        Coordinates,
        OCR,
        Metadata,
        Hybrid,
        Default
    }
} 