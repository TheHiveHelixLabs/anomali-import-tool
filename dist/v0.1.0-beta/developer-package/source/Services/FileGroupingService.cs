using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace AnomaliImportTool.Core.Services
{
    /// <summary>
    /// Service for grouping files based on filename patterns and similarity
    /// </summary>
    public class FileGroupingService
    {
        private readonly List<GroupingPattern> _patterns = new List<GroupingPattern>
        {
            // Common threat report patterns
            new GroupingPattern
            {
                Name = "APT Group Reports",
                Pattern = @"^(APT\d+|Lazarus|Carbanak|FIN\d+)",
                Description = "Groups files by APT group name"
            },
            new GroupingPattern
            {
                Name = "Date-based Reports",
                Pattern = @"(\d{4}[-_]?\d{2}[-_]?\d{2})|(\d{2}[-_]?\d{2}[-_]?\d{4})",
                Description = "Groups files by date pattern"
            },
            new GroupingPattern
            {
                Name = "Campaign Names",
                Pattern = @"^([A-Za-z]+(?:Bear|Cat|Panda|Spider|Tiger|Wolf))",
                Description = "Groups files by campaign codenames"
            },
            new GroupingPattern
            {
                Name = "Quarterly Reports",
                Pattern = @"(Q[1-4][-_]?\d{4})|(\d{4}[-_]?Q[1-4])",
                Description = "Groups quarterly reports together"
            },
            new GroupingPattern
            {
                Name = "Incident Reports",
                Pattern = @"(INC|INCIDENT|IR)[-_]?\d+",
                Description = "Groups incident reports by ID"
            },
            new GroupingPattern
            {
                Name = "Malware Families",
                Pattern = @"(Emotet|Trickbot|Ryuk|WannaCry|NotPetya|Dridex)",
                Description = "Groups files by malware family",
                IgnoreCase = true
            }
        };

        /// <summary>
        /// Groups files based on filename similarity
        /// </summary>
        public List<FileGroup> GroupFilesBySimilarity(IEnumerable<string> filePaths, double similarityThreshold = 0.7)
        {
            var groups = new List<FileGroup>();
            var files = filePaths.Select(p => new FileInfo(p)).ToList();
            var processed = new HashSet<string>();

            foreach (var file in files)
            {
                if (processed.Contains(file.FullName))
                    continue;

                var group = new FileGroup
                {
                    Name = GetGroupName(file.Name),
                    Files = new List<FileInfo> { file }
                };
                processed.Add(file.FullName);

                // Find similar files
                foreach (var otherFile in files)
                {
                    if (processed.Contains(otherFile.FullName))
                        continue;

                    var similarity = CalculateSimilarity(file.Name, otherFile.Name);
                    if (similarity >= similarityThreshold)
                    {
                        group.Files.Add(otherFile);
                        processed.Add(otherFile.FullName);
                    }
                }

                groups.Add(group);
            }

            return groups;
        }

        /// <summary>
        /// Groups files based on predefined patterns
        /// </summary>
        public List<FileGroup> GroupFilesByPattern(IEnumerable<string> filePaths)
        {
            var groups = new Dictionary<string, FileGroup>();
            var ungrouped = new List<FileInfo>();

            foreach (var filePath in filePaths)
            {
                var file = new FileInfo(filePath);
                var grouped = false;

                foreach (var pattern in _patterns)
                {
                    var regex = new Regex(pattern.Pattern, 
                        pattern.IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
                    
                    var match = regex.Match(file.Name);
                    if (match.Success)
                    {
                        var key = $"{pattern.Name}_{match.Value}";
                        if (!groups.ContainsKey(key))
                        {
                            groups[key] = new FileGroup
                            {
                                Name = $"{pattern.Name}: {match.Value}",
                                Pattern = pattern.Name,
                                Files = new List<FileInfo>()
                            };
                        }
                        groups[key].Files.Add(file);
                        grouped = true;
                        break;
                    }
                }

                if (!grouped)
                {
                    ungrouped.Add(file);
                }
            }

            // Add ungrouped files
            if (ungrouped.Any())
            {
                groups["_ungrouped"] = new FileGroup
                {
                    Name = "Other Files",
                    Files = ungrouped
                };
            }

            return groups.Values.ToList();
        }

        /// <summary>
        /// Groups files by date extracted from filename
        /// </summary>
        public List<FileGroup> GroupFilesByDate(IEnumerable<string> filePaths)
        {
            var groups = new Dictionary<string, FileGroup>();
            var datePatterns = new[]
            {
                @"(\d{4}[-_]?\d{2}[-_]?\d{2})", // YYYY-MM-DD
                @"(\d{2}[-_]?\d{2}[-_]?\d{4})", // DD-MM-YYYY or MM-DD-YYYY
                @"(\d{8})", // YYYYMMDD
                @"(\d{4}[-_]\d{2})", // YYYY-MM
                @"(Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)[-_]?\d{4}" // Mon-YYYY
            };

            foreach (var filePath in filePaths)
            {
                var file = new FileInfo(filePath);
                var grouped = false;

                foreach (var pattern in datePatterns)
                {
                    var match = Regex.Match(file.Name, pattern, RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        var dateStr = match.Value;
                        if (!groups.ContainsKey(dateStr))
                        {
                            groups[dateStr] = new FileGroup
                            {
                                Name = $"Date: {dateStr}",
                                Pattern = "Date-based",
                                Files = new List<FileInfo>()
                            };
                        }
                        groups[dateStr].Files.Add(file);
                        grouped = true;
                        break;
                    }
                }

                if (!grouped)
                {
                    if (!groups.ContainsKey("_undated"))
                    {
                        groups["_undated"] = new FileGroup
                        {
                            Name = "Undated Files",
                            Files = new List<FileInfo>()
                        };
                    }
                    groups["_undated"].Files.Add(file);
                }
            }

            return groups.Values.OrderBy(g => g.Name).ToList();
        }

        /// <summary>
        /// Calculates similarity between two filenames using Levenshtein distance
        /// </summary>
        private double CalculateSimilarity(string fileName1, string fileName2)
        {
            // Remove extensions for comparison
            var name1 = Path.GetFileNameWithoutExtension(fileName1).ToLowerInvariant();
            var name2 = Path.GetFileNameWithoutExtension(fileName2).ToLowerInvariant();

            // Quick exact match check
            if (name1 == name2) return 1.0;

            // Check for common prefixes
            var commonPrefixLength = GetCommonPrefixLength(name1, name2);
            if (commonPrefixLength >= Math.Min(name1.Length, name2.Length) * 0.7)
                return 0.8;

            // Calculate Levenshtein distance
            var distance = LevenshteinDistance(name1, name2);
            var maxLength = Math.Max(name1.Length, name2.Length);
            
            return 1.0 - (double)distance / maxLength;
        }

        /// <summary>
        /// Calculates the Levenshtein distance between two strings
        /// </summary>
        private int LevenshteinDistance(string s1, string s2)
        {
            var m = s1.Length;
            var n = s2.Length;
            var d = new int[m + 1, n + 1];

            if (m == 0) return n;
            if (n == 0) return m;

            for (var i = 0; i <= m; i++)
                d[i, 0] = i;

            for (var j = 0; j <= n; j++)
                d[0, j] = j;

            for (var i = 1; i <= m; i++)
            {
                for (var j = 1; j <= n; j++)
                {
                    var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            return d[m, n];
        }

        /// <summary>
        /// Gets the length of the common prefix between two strings
        /// </summary>
        private int GetCommonPrefixLength(string s1, string s2)
        {
            var minLength = Math.Min(s1.Length, s2.Length);
            for (var i = 0; i < minLength; i++)
            {
                if (s1[i] != s2[i])
                    return i;
            }
            return minLength;
        }

        /// <summary>
        /// Generates a group name from a filename
        /// </summary>
        private string GetGroupName(string fileName)
        {
            var name = Path.GetFileNameWithoutExtension(fileName);
            
            // Try to extract meaningful prefix
            var match = Regex.Match(name, @"^([A-Za-z]+[\w-]*?)[-_\s]*\d");
            if (match.Success)
                return match.Groups[1].Value;

            // Use first significant word
            var words = name.Split(new[] { '-', '_', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return words.FirstOrDefault() ?? "Group";
        }

        /// <summary>
        /// Adds a custom grouping pattern
        /// </summary>
        public void AddCustomPattern(GroupingPattern pattern)
        {
            _patterns.Insert(0, pattern); // Insert at beginning for priority
        }

        /// <summary>
        /// Gets all available grouping patterns
        /// </summary>
        public IReadOnlyList<GroupingPattern> GetPatterns()
        {
            return _patterns.AsReadOnly();
        }
    }

    /// <summary>
    /// Represents a group of files
    /// </summary>
    public class FileGroup
    {
        public string Name { get; set; }
        public string Pattern { get; set; }
        public List<FileInfo> Files { get; set; } = new List<FileInfo>();
        public string GroupId { get; set; } = Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Represents a grouping pattern
    /// </summary>
    public class GroupingPattern
    {
        public string Name { get; set; }
        public string Pattern { get; set; }
        public string Description { get; set; }
        public bool IgnoreCase { get; set; } = true;
    }
} 