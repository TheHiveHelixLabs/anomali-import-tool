using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AnomaliImportTool.Core.Services
{
    /// <summary>
    /// Service for parsing and applying naming templates to threat bulletins
    /// </summary>
    public class NamingTemplateService
    {
        private readonly Dictionary<string, Func<TemplateContext, string>> _placeholders;
        private readonly List<TemplateDefinition> _predefinedTemplates;

        public NamingTemplateService()
        {
            _placeholders = InitializePlaceholders();
            _predefinedTemplates = InitializePredefinedTemplates();
        }

        /// <summary>
        /// Applies a naming template to generate a bulletin name
        /// </summary>
        public string ApplyTemplate(string template, TemplateContext context)
        {
            if (string.IsNullOrWhiteSpace(template))
                throw new ArgumentException("Template cannot be empty", nameof(template));

            var result = template;
            var placeholderPattern = @"\{([^}]+)\}";
            
            result = Regex.Replace(result, placeholderPattern, match =>
            {
                var placeholder = match.Groups[1].Value;
                var value = ResolvePlaceholder(placeholder, context);
                return value ?? match.Value; // Keep original if not resolved
            });

            // Clean up the result
            result = CleanupGeneratedName(result);
            
            return result;
        }

        /// <summary>
        /// Validates a template for syntax errors
        /// </summary>
        public TemplateValidationResult ValidateTemplate(string template)
        {
            var result = new TemplateValidationResult { IsValid = true };

            if (string.IsNullOrWhiteSpace(template))
            {
                result.IsValid = false;
                result.Errors.Add("Template cannot be empty");
                return result;
            }

            // Check for balanced braces
            var openBraces = template.Count(c => c == '{');
            var closeBraces = template.Count(c => c == '}');
            if (openBraces != closeBraces)
            {
                result.IsValid = false;
                result.Errors.Add("Unbalanced braces in template");
            }

            // Check for empty placeholders
            if (Regex.IsMatch(template, @"\{\s*\}"))
            {
                result.IsValid = false;
                result.Errors.Add("Empty placeholders are not allowed");
            }

            // Extract and validate placeholders
            var placeholderPattern = @"\{([^}]+)\}";
            var matches = Regex.Matches(template, placeholderPattern);
            
            foreach (Match match in matches)
            {
                var placeholder = match.Groups[1].Value;
                var parts = placeholder.Split(':');
                var placeholderName = parts[0].ToLowerInvariant();

                if (!_placeholders.ContainsKey(placeholderName) && 
                    !placeholderName.StartsWith("custom.") &&
                    !placeholderName.StartsWith("meta."))
                {
                    result.IsValid = false;
                    result.Errors.Add($"Unknown placeholder: {placeholderName}");
                }

                // Validate date format if specified
                if (placeholderName.Contains("date") && parts.Length > 1)
                {
                    try
                    {
                        DateTime.Now.ToString(parts[1]);
                    }
                    catch
                    {
                        result.IsValid = false;
                        result.Errors.Add($"Invalid date format: {parts[1]}");
                    }
                }
            }

            result.UsedPlaceholders = matches.Cast<Match>()
                .Select(m => m.Groups[1].Value.Split(':')[0])
                .Distinct()
                .ToList();

            return result;
        }

        /// <summary>
        /// Generates a preview of what the template will produce
        /// </summary>
        public string GeneratePreview(string template, TemplateContext context = null)
        {
            context ??= CreateSampleContext();
            return ApplyTemplate(template, context);
        }

        /// <summary>
        /// Gets all predefined templates
        /// </summary>
        public IReadOnlyList<TemplateDefinition> GetPredefinedTemplates()
        {
            return _predefinedTemplates.AsReadOnly();
        }

        /// <summary>
        /// Gets all available placeholders with descriptions
        /// </summary>
        public Dictionary<string, string> GetAvailablePlaceholders()
        {
            return new Dictionary<string, string>
            {
                { "{date}", "Current date (default format: yyyy-MM-dd)" },
                { "{date:format}", "Current date with custom format" },
                { "{time}", "Current time (default format: HH-mm-ss)" },
                { "{time:format}", "Current time with custom format" },
                { "{datetime}", "Current date and time" },
                { "{datetime:format}", "Current date and time with custom format" },
                { "{filename}", "Original filename without extension" },
                { "{extension}", "Original file extension" },
                { "{groupname}", "Name of the file group" },
                { "{groupindex}", "Index within the group (padded)" },
                { "{username}", "Current Windows username" },
                { "{machine}", "Machine name" },
                { "{guid}", "Unique identifier" },
                { "{shortguid}", "Short unique identifier (8 chars)" },
                { "{counter}", "Sequential counter" },
                { "{meta.author}", "Document author metadata" },
                { "{meta.subject}", "Document subject metadata" },
                { "{meta.title}", "Document title metadata" },
                { "{custom.field}", "Custom field value" }
            };
        }

        /// <summary>
        /// Initializes placeholder resolvers
        /// </summary>
        private Dictionary<string, Func<TemplateContext, string>> InitializePlaceholders()
        {
            return new Dictionary<string, Func<TemplateContext, string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["date"] = ctx => DateTime.Now.ToString("yyyy-MM-dd"),
                ["time"] = ctx => DateTime.Now.ToString("HH-mm-ss"),
                ["datetime"] = ctx => DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"),
                ["filename"] = ctx => Path.GetFileNameWithoutExtension(ctx.OriginalFileName ?? "Unknown"),
                ["extension"] = ctx => Path.GetExtension(ctx.OriginalFileName ?? "")?.TrimStart('.') ?? "",
                ["groupname"] = ctx => ctx.GroupName ?? "Ungrouped",
                ["groupindex"] = ctx => (ctx.GroupIndex ?? 0).ToString("D3"),
                ["username"] = ctx => Environment.UserName,
                ["machine"] = ctx => Environment.MachineName,
                ["guid"] = ctx => Guid.NewGuid().ToString(),
                ["shortguid"] = ctx => Guid.NewGuid().ToString("N").Substring(0, 8),
                ["counter"] = ctx => (ctx.Counter ?? 0).ToString("D4")
            };
        }

        /// <summary>
        /// Resolves a placeholder to its value
        /// </summary>
        private string ResolvePlaceholder(string placeholder, TemplateContext context)
        {
            var parts = placeholder.Split(':');
            var placeholderName = parts[0].ToLowerInvariant();
            var format = parts.Length > 1 ? string.Join(":", parts.Skip(1)) : null;

            // Handle date/time with custom format
            if (placeholderName.Contains("date") || placeholderName.Contains("time"))
            {
                try
                {
                    if (!string.IsNullOrEmpty(format))
                    {
                        return DateTime.Now.ToString(format);
                    }
                }
                catch
                {
                    // Fall back to default format
                }
            }

            // Handle metadata placeholders
            if (placeholderName.StartsWith("meta."))
            {
                var metaKey = placeholderName.Substring(5);
                return context.Metadata?.GetValueOrDefault(metaKey) ?? "";
            }

            // Handle custom fields
            if (placeholderName.StartsWith("custom."))
            {
                var customKey = placeholderName.Substring(7);
                return context.CustomFields?.GetValueOrDefault(customKey) ?? "";
            }

            // Standard placeholders
            if (_placeholders.TryGetValue(placeholderName, out var resolver))
            {
                return resolver(context);
            }

            return null;
        }

        /// <summary>
        /// Cleans up the generated name
        /// </summary>
        private string CleanupGeneratedName(string name)
        {
            // Remove multiple spaces/underscores
            name = Regex.Replace(name, @"[\s_]+", "_");
            
            // Remove invalid filename characters
            var invalidChars = Path.GetInvalidFileNameChars();
            var cleanName = new StringBuilder();
            
            foreach (var c in name)
            {
                if (!invalidChars.Contains(c))
                    cleanName.Append(c);
            }

            // Trim underscores from start/end
            name = cleanName.ToString().Trim('_', ' ', '-');

            // Ensure name is not empty
            if (string.IsNullOrWhiteSpace(name))
                name = "ThreatBulletin_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");

            // Limit length
            if (name.Length > 200)
                name = name.Substring(0, 200);

            return name;
        }

        /// <summary>
        /// Creates a sample context for preview generation
        /// </summary>
        private TemplateContext CreateSampleContext()
        {
            return new TemplateContext
            {
                OriginalFileName = "APT29_Report_2024.pdf",
                GroupName = "APT Reports",
                GroupIndex = 1,
                Counter = 42,
                Metadata = new Dictionary<string, string>
                {
                    ["author"] = "Security Team",
                    ["subject"] = "Threat Intelligence",
                    ["title"] = "APT29 Campaign Analysis"
                },
                CustomFields = new Dictionary<string, string>
                {
                    ["campaign"] = "CozyBear",
                    ["severity"] = "High"
                }
            };
        }

        /// <summary>
        /// Initializes predefined templates
        /// </summary>
        private List<TemplateDefinition> InitializePredefinedTemplates()
        {
            return new List<TemplateDefinition>
            {
                new TemplateDefinition
                {
                    Name = "Date and Filename",
                    Template = "{date}_{filename}",
                    Description = "Prefixes filename with current date",
                    Example = "2024-03-15_APT29_Report"
                },
                new TemplateDefinition
                {
                    Name = "Group and Index",
                    Template = "{groupname}_{groupindex}_{date}",
                    Description = "Groups files with sequential numbering",
                    Example = "APT_Reports_001_2024-03-15"
                },
                new TemplateDefinition
                {
                    Name = "Metadata Based",
                    Template = "{meta.author}_{meta.subject}_{date:yyyyMMdd}",
                    Description = "Uses document metadata for naming",
                    Example = "Security_Team_Threat_Intelligence_20240315"
                },
                new TemplateDefinition
                {
                    Name = "Timestamp Detailed",
                    Template = "ThreatBulletin_{datetime:yyyyMMdd_HHmmss}_{shortguid}",
                    Description = "Unique name with timestamp and ID",
                    Example = "ThreatBulletin_20240315_143052_a1b2c3d4"
                },
                new TemplateDefinition
                {
                    Name = "Campaign Focused",
                    Template = "{custom.campaign}_{custom.severity}_{date}_{counter}",
                    Description = "Uses custom fields for campaign tracking",
                    Example = "CozyBear_High_2024-03-15_0042"
                }
            };
        }
    }

    /// <summary>
    /// Context for template processing
    /// </summary>
    public class TemplateContext
    {
        public string OriginalFileName { get; set; }
        public string GroupName { get; set; }
        public int? GroupIndex { get; set; }
        public int? Counter { get; set; }
        public Dictionary<string, string> Metadata { get; set; }
        public Dictionary<string, string> CustomFields { get; set; }
    }

    /// <summary>
    /// Template validation result
    /// </summary>
    public class TemplateValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> UsedPlaceholders { get; set; } = new List<string>();
    }

    /// <summary>
    /// Predefined template definition
    /// </summary>
    public class TemplateDefinition
    {
        public string Name { get; set; }
        public string Template { get; set; }
        public string Description { get; set; }
        public string Example { get; set; }
    }
} 