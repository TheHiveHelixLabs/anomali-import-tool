using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AnomaliImportTool.Core.Services;
using Xunit;

namespace AnomaliImportTool.Tests.Unit.Services
{
    public class FileGroupingServiceTests : IDisposable
    {
        private readonly FileGroupingService _service;
        private readonly string _testDirectory;
        private readonly List<string> _testFiles;

        public FileGroupingServiceTests()
        {
            _service = new FileGroupingService();
            _testDirectory = Path.Combine(Path.GetTempPath(), $"FileGroupingTests_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDirectory);
            
            _testFiles = new List<string>();
            CreateTestFiles();
        }

        [Fact]
        public void GroupFilesBySimilarity_WithSimilarFiles_GroupsCorrectly()
        {
            // Arrange
            var filePaths = new[]
            {
                Path.Combine(_testDirectory, "APT29_Report_2023.pdf"),
                Path.Combine(_testDirectory, "APT29_Analysis_2023.pdf"),
                Path.Combine(_testDirectory, "Lazarus_Group_Report.pdf"),
                Path.Combine(_testDirectory, "RandomFile.pdf")
            };

            CreateFiles(filePaths);

            // Act
            var groups = _service.GroupFilesBySimilarity(filePaths, 0.6);

            // Assert
            Assert.NotEmpty(groups);
            
            // APT29 files should be grouped together
            var apt29Group = groups.FirstOrDefault(g => g.Files.Any(f => f.Name.Contains("APT29")));
            Assert.NotNull(apt29Group);
            Assert.Equal(2, apt29Group.Files.Count);
            Assert.All(apt29Group.Files, f => Assert.Contains("APT29", f.Name));
        }

        [Fact]
        public void GroupFilesByPattern_WithAPTFiles_GroupsCorrectly()
        {
            // Arrange
            var filePaths = new[]
            {
                Path.Combine(_testDirectory, "APT1_Campaign_Analysis.pdf"),
                Path.Combine(_testDirectory, "APT28_Indicators.pdf"),
                Path.Combine(_testDirectory, "Lazarus_TTPs.pdf"),
                Path.Combine(_testDirectory, "FIN7_Report.pdf"),
                Path.Combine(_testDirectory, "RandomDocument.pdf")
            };

            CreateFiles(filePaths);

            // Act
            var groups = _service.GroupFilesByPattern(filePaths);

            // Assert
            Assert.NotEmpty(groups);
            
            // Should have APT group
            var aptGroup = groups.FirstOrDefault(g => g.Name.Contains("APT Group Reports"));
            Assert.NotNull(aptGroup);
            Assert.Equal(3, aptGroup.Files.Count); // APT1, APT28, FIN7
            
            // Should have Campaign group
            var campaignGroup = groups.FirstOrDefault(g => g.Name.Contains("Campaign Names"));
            Assert.NotNull(campaignGroup);
            Assert.Single(campaignGroup.Files); // Lazarus
        }

        [Fact]
        public void GroupFilesByDate_WithDatePatterns_GroupsCorrectly()
        {
            // Arrange
            var filePaths = new[]
            {
                Path.Combine(_testDirectory, "Report_2023-01-15.pdf"),
                Path.Combine(_testDirectory, "Analysis_2023-01-15.pdf"),
                Path.Combine(_testDirectory, "Summary_2023-02-20.pdf"),
                Path.Combine(_testDirectory, "ThreatIntel_20230315.pdf"),
                Path.Combine(_testDirectory, "Weekly_Jan_2023.pdf"),
                Path.Combine(_testDirectory, "NoDateFile.pdf")
            };

            CreateFiles(filePaths);

            // Act
            var groups = _service.GroupFilesByDate(filePaths);

            // Assert
            Assert.NotEmpty(groups);
            
            // Files with same date should be grouped
            var jan15Group = groups.FirstOrDefault(g => g.Name.Contains("2023-01-15"));
            Assert.NotNull(jan15Group);
            Assert.Equal(2, jan15Group.Files.Count);
            
            // Undated files should be in their own group
            var undatedGroup = groups.FirstOrDefault(g => g.Name.Contains("Undated"));
            Assert.NotNull(undatedGroup);
            Assert.Single(undatedGroup.Files);
        }

        [Theory]
        [InlineData("APT29_Report.pdf", "APT29_Analysis.pdf", 0.8)] // High similarity
        [InlineData("ThreatReport.pdf", "ThreatAnalysis.pdf", 0.7)] // Medium similarity
        [InlineData("APT29.pdf", "Lazarus.pdf", 0.2)] // Low similarity
        [InlineData("identical_file.pdf", "identical_file.pdf", 1.0)] // Identical
        public void CalculateSimilarity_WithVariousFileNames_ReturnsExpectedResults(
            string fileName1, string fileName2, double expectedMinSimilarity)
        {
            // This tests the private method indirectly through grouping
            var filePaths = new[]
            {
                Path.Combine(_testDirectory, fileName1),
                Path.Combine(_testDirectory, fileName2)
            };

            CreateFiles(filePaths);

            // Act
            var groups = _service.GroupFilesBySimilarity(filePaths, 0.1); // Low threshold to capture all

            // Assert
            if (expectedMinSimilarity > 0.7)
            {
                // High similarity should result in single group
                Assert.Single(groups);
                Assert.Equal(2, groups.First().Files.Count);
            }
            else if (expectedMinSimilarity < 0.3)
            {
                // Low similarity should result in separate groups
                Assert.Equal(2, groups.Count);
                Assert.All(groups, g => Assert.Single(g.Files));
            }
        }

        [Fact]
        public void GroupFilesByPattern_WithQuarterlyReports_GroupsCorrectly()
        {
            // Arrange
            var filePaths = new[]
            {
                Path.Combine(_testDirectory, "Q1_2023_ThreatReport.pdf"),
                Path.Combine(_testDirectory, "2023_Q2_Analysis.pdf"),
                Path.Combine(_testDirectory, "Q3-2023-Summary.pdf"),
                Path.Combine(_testDirectory, "RegularReport.pdf")
            };

            CreateFiles(filePaths);

            // Act
            var groups = _service.GroupFilesByPattern(filePaths);

            // Assert
            var quarterlyGroup = groups.FirstOrDefault(g => g.Name.Contains("Quarterly Reports"));
            Assert.NotNull(quarterlyGroup);
            Assert.Equal(3, quarterlyGroup.Files.Count);
        }

        [Fact]
        public void GroupFilesByPattern_WithIncidentReports_GroupsCorrectly()
        {
            // Arrange
            var filePaths = new[]
            {
                Path.Combine(_testDirectory, "INC-001_Breach_Report.pdf"),
                Path.Combine(_testDirectory, "INCIDENT_123_Analysis.pdf"),
                Path.Combine(_testDirectory, "IR-456_Summary.pdf"),
                Path.Combine(_testDirectory, "StandardReport.pdf")
            };

            CreateFiles(filePaths);

            // Act
            var groups = _service.GroupFilesByPattern(filePaths);

            // Assert
            var incidentGroup = groups.FirstOrDefault(g => g.Name.Contains("Incident Reports"));
            Assert.NotNull(incidentGroup);
            Assert.Equal(3, incidentGroup.Files.Count);
        }

        [Fact]
        public void GroupFilesByPattern_WithMalwareFamilies_GroupsCorrectly()
        {
            // Arrange
            var filePaths = new[]
            {
                Path.Combine(_testDirectory, "Emotet_Campaign_Analysis.pdf"),
                Path.Combine(_testDirectory, "TrickBot_IOCs.pdf"),
                Path.Combine(_testDirectory, "Ryuk_Ransomware_Report.pdf"),
                Path.Combine(_testDirectory, "WannaCry_Aftermath.pdf"),
                Path.Combine(_testDirectory, "GenericMalware.pdf")
            };

            CreateFiles(filePaths);

            // Act
            var groups = _service.GroupFilesByPattern(filePaths);

            // Assert
            var malwareGroup = groups.FirstOrDefault(g => g.Name.Contains("Malware Families"));
            Assert.NotNull(malwareGroup);
            Assert.Equal(4, malwareGroup.Files.Count); // Emotet, TrickBot, Ryuk, WannaCry
            Assert.Contains(malwareGroup.Files, f => f.Name.Contains("Emotet"));
            Assert.Contains(malwareGroup.Files, f => f.Name.Contains("TrickBot"));
            Assert.Contains(malwareGroup.Files, f => f.Name.Contains("Ryuk"));
            Assert.Contains(malwareGroup.Files, f => f.Name.Contains("WannaCry"));
        }

        [Fact]
        public void AddCustomPattern_WithNewPattern_AddsSuccessfully()
        {
            // Arrange
            var customPattern = new GroupingPattern
            {
                Name = "Custom CVE Reports",
                Pattern = @"CVE-\d{4}-\d{4,}",
                Description = "Groups files by CVE identifiers",
                IgnoreCase = true
            };

            var filePaths = new[]
            {
                Path.Combine(_testDirectory, "CVE-2023-1234_Analysis.pdf"),
                Path.Combine(_testDirectory, "CVE-2023-5678_Report.pdf"),
                Path.Combine(_testDirectory, "StandardReport.pdf")
            };

            CreateFiles(filePaths);

            // Act
            _service.AddCustomPattern(customPattern);
            var groups = _service.GroupFilesByPattern(filePaths);

            // Assert
            var cveGroup = groups.FirstOrDefault(g => g.Name.Contains("Custom CVE Reports"));
            Assert.NotNull(cveGroup);
            Assert.Equal(2, cveGroup.Files.Count);
        }

        [Fact]
        public void GetPatterns_ReturnsAllPatterns()
        {
            // Act
            var patterns = _service.GetPatterns();

            // Assert
            Assert.NotEmpty(patterns);
            Assert.Contains(patterns, p => p.Name == "APT Group Reports");
            Assert.Contains(patterns, p => p.Name == "Date-based Reports");
            Assert.Contains(patterns, p => p.Name == "Campaign Names");
            Assert.Contains(patterns, p => p.Name == "Quarterly Reports");
            Assert.Contains(patterns, p => p.Name == "Incident Reports");
            Assert.Contains(patterns, p => p.Name == "Malware Families");
        }

        [Fact]
        public void GroupFilesBySimilarity_WithEmptyInput_ReturnsEmptyList()
        {
            // Act
            var groups = _service.GroupFilesBySimilarity(new string[0]);

            // Assert
            Assert.Empty(groups);
        }

        [Fact]
        public void GroupFilesByPattern_WithEmptyInput_ReturnsEmptyList()
        {
            // Act
            var groups = _service.GroupFilesByPattern(new string[0]);

            // Assert
            Assert.Empty(groups);
        }

        [Fact]
        public void GroupFilesByDate_WithEmptyInput_ReturnsEmptyList()
        {
            // Act
            var groups = _service.GroupFilesByDate(new string[0]);

            // Assert
            Assert.Empty(groups);
        }

        [Theory]
        [InlineData(0.1)] // Very low threshold
        [InlineData(0.5)] // Medium threshold
        [InlineData(0.9)] // High threshold
        public void GroupFilesBySimilarity_WithDifferentThresholds_GroupsAppropriately(double threshold)
        {
            // Arrange
            var filePaths = new[]
            {
                Path.Combine(_testDirectory, "ThreatReport_2023.pdf"),
                Path.Combine(_testDirectory, "ThreatAnalysis_2023.pdf"),
                Path.Combine(_testDirectory, "CompleteDifferentName.pdf")
            };

            CreateFiles(filePaths);

            // Act
            var groups = _service.GroupFilesBySimilarity(filePaths, threshold);

            // Assert
            Assert.NotEmpty(groups);
            
            if (threshold <= 0.5)
            {
                // Low threshold should group similar files together
                Assert.True(groups.Count <= 2);
            }
            else if (threshold >= 0.9)
            {
                // High threshold should separate most files
                Assert.True(groups.Count >= 2);
            }
        }

        [Fact]
        public void FileGroup_Properties_SetCorrectly()
        {
            // Arrange
            var filePaths = new[]
            {
                Path.Combine(_testDirectory, "APT29_Report.pdf")
            };

            CreateFiles(filePaths);

            // Act
            var groups = _service.GroupFilesByPattern(filePaths);

            // Assert
            var group = groups.First();
            Assert.NotNull(group.Name);
            Assert.NotNull(group.GroupId);
            Assert.NotEmpty(group.Files);
            Assert.NotNull(group.Pattern);
        }

        private void CreateTestFiles()
        {
            // Create some basic test files that will be used across multiple tests
            var basicFiles = new[]
            {
                "TestFile1.pdf",
                "TestFile2.pdf",
                "DifferentFile.docx"
            };

            foreach (var fileName in basicFiles)
            {
                var filePath = Path.Combine(_testDirectory, fileName);
                File.WriteAllText(filePath, "Test content");
                _testFiles.Add(filePath);
            }
        }

        private void CreateFiles(string[] filePaths)
        {
            foreach (var filePath in filePaths)
            {
                var directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(filePath, "Test content");
                _testFiles.Add(filePath);
            }
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_testDirectory))
                {
                    Directory.Delete(_testDirectory, true);
                }
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }
} 