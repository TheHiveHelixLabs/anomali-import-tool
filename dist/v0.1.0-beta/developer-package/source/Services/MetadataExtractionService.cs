using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AnomaliImportTool.Core.Models;

namespace AnomaliImportTool.Core.Services
{
    /// <summary>
    /// Service for extracting metadata from document content and filenames
    /// </summary>
    public class MetadataExtractionService
    {
        private readonly List<ExtractionPattern> _patterns;
        private readonly Dictionary<string, Func<string, string>> _normalizers;

        public MetadataExtractionService()
        {
            _patterns = InitializePatterns();
            _normalizers = InitializeNormalizers();
        }

        /// <summary>
        /// Extracts metadata from document content and filename
        /// </summary>
        public ExtractedMetadata ExtractMetadata(string content, string fileName, Dictionary<string, string> documentProperties = null, ImportTemplate template = null)
        {
            var metadata = new ExtractedMetadata
            {
                FileName = fileName,
                ExtractionTime = DateTime.UtcNow
            };

            // Extract from filename
            ExtractFromFileName(fileName, metadata);

            // Extract from content
            if (!string.IsNullOrWhiteSpace(content))
            {
                var patterns = template != null ? MergeTemplatePatterns(template) : _patterns;
                ApplyPatternsToContent(content, patterns, metadata);
            }

            // Merge document properties
            if (documentProperties != null)
            {
                MergeDocumentProperties(documentProperties, metadata);
            }

            // Post-process and validate
            PostProcessMetadata(metadata);

            return metadata;
        }

        /// <summary>
        /// Validates extraction patterns against content
        /// </summary>
        public PatternValidationResult ValidatePatterns(string content, List<ExtractionPattern> customPatterns = null)
        {
            var result = new PatternValidationResult();
            var patterns = customPatterns ?? _patterns;

            foreach (var pattern in patterns)
            {
                try
                {
                    var regex = new Regex(pattern.Pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                    var matches = regex.Matches(content);
                    
                    if (matches.Count > 0)
                    {
                        result.MatchedPatterns.Add(new PatternMatch
                        {
                            PatternName = pattern.Name,
                            MatchCount = matches.Count,
                            SampleValues = matches.Cast<Match>()
                                .Take(3)
                                .Select(m => ExtractValue(m, pattern.CaptureGroup))
                                .ToList()
                        });
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Pattern '{pattern.Name}' error: {ex.Message}");
                }
            }

            result.IsValid = !result.Errors.Any();
            return result;
        }

        /// <summary>
        /// Gets all available extraction patterns
        /// </summary>
        public IReadOnlyList<ExtractionPattern> GetPatterns()
        {
            return _patterns.AsReadOnly();
        }

        /// <summary>
        /// Adds a custom extraction pattern
        /// </summary>
        public void AddCustomPattern(ExtractionPattern pattern)
        {
            if (pattern == null)
                throw new ArgumentNullException(nameof(pattern));

            // Validate regex
            try
            {
                new Regex(pattern.Pattern);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Invalid regex pattern: {ex.Message}", nameof(pattern));
            }

            _patterns.Add(pattern);
        }

        /// <summary>
        /// Extracts metadata from filename
        /// </summary>
        private void ExtractFromFileName(string fileName, ExtractedMetadata metadata)
        {
            // Extract date from filename
            var datePatterns = new[]
            {
                @"(\d{4}[-_]?\d{2}[-_]?\d{2})",
                @"(\d{2}[-_]?\d{2}[-_]?\d{4})",
                @"(\d{8})"
            };

            foreach (var pattern in datePatterns)
            {
                var match = Regex.Match(fileName, pattern);
                if (match.Success)
                {
                    metadata.FileDate = match.Value;
                    break;
                }
            }

            // Extract ticket/incident number
            var ticketMatch = Regex.Match(fileName, @"(INC|TICKET|IR)[-_]?(\d+)", RegexOptions.IgnoreCase);
            if (ticketMatch.Success)
            {
                metadata.TicketNumber = ticketMatch.Value;
            }

            // Extract campaign/group name
            var campaignPatterns = new[]
            {
                @"(APT\d+)",
                @"([A-Za-z]+(?:Bear|Cat|Panda|Spider|Tiger|Wolf))",
                @"(Lazarus|Carbanak|FIN\d+|Emotet|Trickbot|Ryuk)"
            };

            foreach (var pattern in campaignPatterns)
            {
                var match = Regex.Match(fileName, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    metadata.CampaignName = match.Value;
                    break;
                }
            }
        }

        /// <summary>
        /// Extracts metadata from content
        /// </summary>
        private void ExtractFromContent(string content, ExtractedMetadata metadata) => ApplyPatternsToContent(content, _patterns, metadata);

        /// <summary>
        /// Extracts value from regex match
        /// </summary>
        private string ExtractValue(Match match, int captureGroup)
        {
            if (captureGroup > 0 && match.Groups.Count > captureGroup)
            {
                return match.Groups[captureGroup].Value.Trim();
            }
            return match.Value.Trim();
        }

        /// <summary>
        /// Applies extracted values to metadata
        /// </summary>
        private void ApplyExtractedValues(string field, List<string> values, ExtractedMetadata metadata)
        {
            switch (field.ToLowerInvariant())
            {
                case "username":
                case "author":
                    metadata.Authors.AddRange(values.Where(v => !metadata.Authors.Contains(v)));
                    break;
                
                case "email":
                    metadata.Emails.AddRange(values.Where(v => !metadata.Emails.Contains(v)));
                    break;
                
                case "date":
                    metadata.Dates.AddRange(values.Where(v => !metadata.Dates.Contains(v)));
                    break;
                
                case "ticket":
                case "incident":
                    metadata.TicketNumbers.AddRange(values.Where(v => !metadata.TicketNumbers.Contains(v)));
                    break;
                
                case "ipv4":
                    metadata.IpAddresses.AddRange(values.Where(v => !metadata.IpAddresses.Contains(v)));
                    break;
                
                case "domain":
                    metadata.Domains.AddRange(values.Where(v => !metadata.Domains.Contains(v)));
                    break;
                
                case "hash":
                case "md5":
                case "sha256":
                    metadata.Hashes.AddRange(values.Where(v => !metadata.Hashes.Contains(v)));
                    break;
                
                case "cve":
                    metadata.CveIds.AddRange(values.Where(v => !metadata.CveIds.Contains(v)));
                    break;
                
                default:
                    // Store values in custom fields
                    if (!metadata.CustomFields.ContainsKey(field))
                        metadata.CustomFields[field] = new List<string>();
                    metadata.CustomFields[field].AddRange(values.Where(v => !metadata.CustomFields[field].Contains(v)));
                    break;
            }
        }

        /// <summary>
        /// Merges document properties into metadata
        /// </summary>
        private void MergeDocumentProperties(Dictionary<string, string> properties, ExtractedMetadata metadata)
        {
            foreach (var prop in properties)
            {
                switch (prop.Key.ToLowerInvariant())
                {
                    case "author":
                    case "creator":
                        if (!string.IsNullOrWhiteSpace(prop.Value) && !metadata.Authors.Contains(prop.Value))
                        {
                            metadata.Authors.Add(prop.Value);
                        }
                        break;
                    
                    case "subject":
                        metadata.Subject = prop.Value;
                        break;
                    
                    case "title":
                        metadata.Title = prop.Value;
                        break;
                    
                    case "creationdate":
                    case "created":
                        metadata.CreationDate = prop.Value;
                        break;
                    
                    case "modificationdate":
                    case "modified":
                        metadata.ModificationDate = prop.Value;
                        break;
                }
            }
        }

        /// <summary>
        /// Post-processes and validates metadata
        /// </summary>
        private void PostProcessMetadata(ExtractedMetadata metadata)
        {
            // Normalize and validate IPs
            metadata.IpAddresses = metadata.IpAddresses
                .Where(ip => IsValidIpAddress(ip))
                .Distinct()
                .ToList();

            // Normalize domains
            metadata.Domains = metadata.Domains
                .Select(d => d.ToLowerInvariant())
                .Where(d => IsValidDomain(d))
                .Distinct()
                .ToList();

            // Normalize hashes
            metadata.Hashes = metadata.Hashes
                .Select(h => h.ToUpperInvariant())
                .Where(h => IsValidHash(h))
                .Distinct()
                .ToList();

            // Set primary values
            metadata.PrimaryAuthor = metadata.Authors.FirstOrDefault();
            metadata.PrimaryDate = metadata.Dates.FirstOrDefault() ?? metadata.FileDate;
            
            if (string.IsNullOrEmpty(metadata.TicketNumber) && metadata.TicketNumbers.Any())
            {
                metadata.TicketNumber = metadata.TicketNumbers.First();
            }
        }

        /// <summary>
        /// Validates IP address format
        /// </summary>
        private bool IsValidIpAddress(string ip)
        {
            return Regex.IsMatch(ip, @"^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$");
        }

        /// <summary>
        /// Validates domain format
        /// </summary>
        private bool IsValidDomain(string domain)
        {
            return Regex.IsMatch(domain, @"^(?:[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?$", RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Validates hash format
        /// </summary>
        private bool IsValidHash(string hash)
        {
            // MD5, SHA1, SHA256
            return Regex.IsMatch(hash, @"^[A-Fa-f0-9]{32}$|^[A-Fa-f0-9]{40}$|^[A-Fa-f0-9]{64}$");
        }

        /// <summary>
        /// Initializes default extraction patterns
        /// </summary>
        private List<ExtractionPattern> InitializePatterns()
        {
            return new List<ExtractionPattern>
            {
                new ExtractionPattern
                {
                    Name = "Email Address",
                    Field = "email",
                    Pattern = @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b",
                    Description = "Extracts email addresses"
                },
                new ExtractionPattern
                {
                    Name = "Author/Analyst Name",
                    Field = "username",
                    Pattern = @"(?:Author|Analyst|Created by|Prepared by|Written by)[:\s]+([A-Za-z\s]+?)(?:\n|$|,)",
                    CaptureGroup = 1,
                    Description = "Extracts author names from common patterns"
                },
                new ExtractionPattern
                {
                    Name = "Ticket Number",
                    Field = "ticket",
                    Pattern = @"\b(?:INC|INCIDENT|TICKET|IR|CASE)[-\s]?(\d{4,10})\b",
                    Description = "Extracts incident/ticket numbers"
                },
                new ExtractionPattern
                {
                    Name = "Date Pattern",
                    Field = "date",
                    Pattern = @"\b(\d{4}[-/]\d{2}[-/]\d{2}|\d{2}[-/]\d{2}[-/]\d{4})\b",
                    Description = "Extracts dates in various formats"
                },
                new ExtractionPattern
                {
                    Name = "IPv4 Address",
                    Field = "ipv4",
                    Pattern = @"\b(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\b",
                    Description = "Extracts IPv4 addresses"
                },
                new ExtractionPattern
                {
                    Name = "Domain Name",
                    Field = "domain",
                    Pattern = @"\b(?:[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\b",
                    Description = "Extracts domain names"
                },
                new ExtractionPattern
                {
                    Name = "MD5 Hash",
                    Field = "hash",
                    Pattern = @"\b[A-Fa-f0-9]{32}\b",
                    Description = "Extracts MD5 hashes"
                },
                new ExtractionPattern
                {
                    Name = "SHA256 Hash",
                    Field = "hash",
                    Pattern = @"\b[A-Fa-f0-9]{64}\b",
                    Description = "Extracts SHA256 hashes"
                },
                new ExtractionPattern
                {
                    Name = "CVE ID",
                    Field = "cve",
                    Pattern = @"\bCVE-\d{4}-\d{4,7}\b",
                    Description = "Extracts CVE identifiers"
                }
            };
        }

        /// <summary>
        /// Initializes field normalizers
        /// </summary>
        private Dictionary<string, Func<string, string>> InitializeNormalizers()
        {
            return new Dictionary<string, Func<string, string>>
            {
                ["email"] = email => email.ToLowerInvariant(),
                ["domain"] = domain => domain.ToLowerInvariant().TrimEnd('.'),
                ["hash"] = hash => hash.ToUpperInvariant(),
                ["cve"] = cve => cve.ToUpperInvariant()
            };
        }

        /// <summary>
        /// Creates extraction patterns from a template and merges with default patterns
        /// </summary>
        private List<ExtractionPattern> MergeTemplatePatterns(ImportTemplate template)
        {
            var merged = new List<ExtractionPattern>(_patterns);

            if (template == null)
                return merged;

            foreach (var field in template.Fields)
            {
                if (field.ExtractionMethod != ExtractionMethod.Text || field.TextPatterns.Count == 0)
                    continue;

                string fieldKey = field.FieldType switch
                {
                    TemplateFieldType.Username => "author",
                    TemplateFieldType.TicketNumber => "ticket",
                    TemplateFieldType.Date => "date",
                    TemplateFieldType.Email => "email",
                    TemplateFieldType.Text => field.Name.ToLowerInvariant(),
                    _ => field.Name.ToLowerInvariant()
                };

                int captureGroup = 1; // assume first capture group

                foreach (var pattern in field.TextPatterns)
                {
                    merged.Add(new ExtractionPattern
                    {
                        Name = $"TPL_{field.Name}",
                        Field = fieldKey,
                        Pattern = pattern,
                        CaptureGroup = captureGroup,
                        Description = $"Pattern derived from template field {field.Name}"
                    });
                }
            }

            return merged;
        }

        /// <summary>
        /// Applies extraction patterns to content and populates metadata
        /// </summary>
        private void ApplyPatternsToContent(string content, List<ExtractionPattern> patterns, ExtractedMetadata metadata)
        {
            foreach (var pattern in patterns)
            {
                try
                {
                    var regex = new Regex(pattern.Pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                    var matches = regex.Matches(content);

                    if (matches.Count > 0)
                    {
                        var values = matches.Cast<Match>()
                            .Select(m => ExtractValue(m, pattern.CaptureGroup))
                            .Where(v => !string.IsNullOrWhiteSpace(v))
                            .Distinct()
                            .ToList();

                        if (values.Any())
                        {
                            ApplyExtractedValues(pattern.Field, values, metadata);
                        }
                    }
                }
                catch
                {
                    // Skip invalid pattern
                }
            }
        }
    }

    /// <summary>
    /// Represents extracted metadata from a document
    /// </summary>
    public class ExtractedMetadata
    {
        public string FileName { get; set; }
        public DateTime ExtractionTime { get; set; }
        
        // Document properties
        public string Title { get; set; }
        public string Subject { get; set; }
        public string CreationDate { get; set; }
        public string ModificationDate { get; set; }
        
        // Extracted from filename
        public string FileDate { get; set; }
        public string TicketNumber { get; set; }
        public string CampaignName { get; set; }
        
        // Extracted from content
        public List<string> Authors { get; set; } = new List<string>();
        public List<string> Emails { get; set; } = new List<string>();
        public List<string> Dates { get; set; } = new List<string>();
        public List<string> TicketNumbers { get; set; } = new List<string>();
        public List<string> IpAddresses { get; set; } = new List<string>();
        public List<string> Domains { get; set; } = new List<string>();
        public List<string> Hashes { get; set; } = new List<string>();
        public List<string> CveIds { get; set; } = new List<string>();
        
        // Custom fields
        public Dictionary<string, List<string>> CustomFields { get; set; } = new Dictionary<string, List<string>>();
        
        // Primary values (for convenience)
        public string PrimaryAuthor { get; set; }
        public string PrimaryDate { get; set; }
    }

    /// <summary>
    /// Represents an extraction pattern
    /// </summary>
    public class ExtractionPattern
    {
        public string Name { get; set; }
        public string Field { get; set; }
        public string Pattern { get; set; }
        public int CaptureGroup { get; set; } = 0;
        public string Description { get; set; }
    }

    /// <summary>
    /// Pattern validation result
    /// </summary>
    public class PatternValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<PatternMatch> MatchedPatterns { get; set; } = new List<PatternMatch>();
    }

    /// <summary>
    /// Represents a pattern match
    /// </summary>
    public class PatternMatch
    {
        public string PatternName { get; set; }
        public int MatchCount { get; set; }
        public List<string> SampleValues { get; set; } = new List<string>();
    }
} 