using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text;
using System.Net.Http.Headers;
using AnomaliImportTool.Core.Application.Interfaces.Infrastructure;

namespace AnomaliImportTool.Git.Services;

/// <summary>
/// Task 6.3: GitHub integration for release management and source distribution
/// Implements GitHub API integration, automated releases, and CI/CD workflows
/// </summary>
public class GitHubIntegrationService : IGitHubService
{
    private readonly ILogger<GitHubIntegrationService> _logger;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly string _owner;
    private readonly string _repository;
    private readonly string _baseUrl = "https://api.github.com";

    public GitHubIntegrationService(
        ILogger<GitHubIntegrationService> logger,
        IConfiguration configuration,
        HttpClient httpClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        
        _owner = _configuration["GitHub:Owner"] ?? throw new InvalidOperationException("GitHub owner not configured");
        _repository = _configuration["GitHub:Repository"] ?? throw new InvalidOperationException("GitHub repository not configured");
        
        ConfigureHttpClient();
    }

    /// <summary>
    /// Task 6.3.1: Implement GitHub API integration with authentication
    /// </summary>
    public async Task<bool> AuthenticateAsync(string token)
    {
        try
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogInformation("Authenticating with GitHub API with correlation ID: {CorrelationId}", correlationId);

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            
            // Test authentication by getting user info
            var response = await _httpClient.GetAsync($"{_baseUrl}/user");
            
            if (response.IsSuccessStatusCode)
            {
                var userJson = await response.Content.ReadAsStringAsync();
                var user = JsonSerializer.Deserialize<JsonElement>(userJson);
                var username = user.GetProperty("login").GetString();
                
                _logger.LogInformation("GitHub authentication successful for user {Username} with correlation ID: {CorrelationId}", 
                    username, correlationId);
                return true;
            }
            
            _logger.LogError("GitHub authentication failed with status {StatusCode} and correlation ID: {CorrelationId}", 
                response.StatusCode, correlationId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GitHub authentication failed");
            return false;
        }
    }

    /// <summary>
    /// Task 6.3.2: Create automated release creation with semantic versioning
    /// </summary>
    public async Task<GitHubRelease?> CreateReleaseAsync(
        string version,
        string name,
        string body,
        bool isPrerelease = false,
        bool isDraft = false,
        string? targetCommitish = null)
    {
        try
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogInformation("Creating GitHub release {Version} with correlation ID: {CorrelationId}", 
                version, correlationId);

            // Validate semantic version
            if (!IsValidSemanticVersion(version))
            {
                throw new ArgumentException($"Invalid semantic version: {version}");
            }

            var releaseData = new
            {
                tag_name = version,
                target_commitish = targetCommitish ?? "main",
                name = name,
                body = body,
                draft = isDraft,
                prerelease = isPrerelease,
                generate_release_notes = true
            };

            var json = JsonSerializer.Serialize(releaseData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/repos/{_owner}/{_repository}/releases", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                var release = JsonSerializer.Deserialize<GitHubRelease>(responseJson);
                
                _logger.LogInformation("GitHub release {Version} created successfully with ID {ReleaseId} and correlation ID: {CorrelationId}", 
                    version, release?.Id, correlationId);
                
                return release;
            }
            
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to create GitHub release {Version}. Status: {StatusCode}, Error: {Error}", 
                version, response.StatusCode, errorContent);
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create GitHub release {Version}", version);
            return null;
        }
    }

    /// <summary>
    /// Task 6.3.3: Implement release asset upload and management
    /// </summary>
    public async Task<bool> UploadReleaseAssetAsync(
        long releaseId,
        string assetPath,
        string? assetName = null,
        string? contentType = null)
    {
        try
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogInformation("Uploading release asset {AssetPath} to release {ReleaseId} with correlation ID: {CorrelationId}", 
                assetPath, releaseId, correlationId);

            if (!File.Exists(assetPath))
            {
                _logger.LogError("Asset file not found: {AssetPath}", assetPath);
                return false;
            }

            var fileName = assetName ?? Path.GetFileName(assetPath);
            var mimeType = contentType ?? GetContentType(assetPath);

            // Get upload URL for the release
            var releaseResponse = await _httpClient.GetAsync($"{_baseUrl}/repos/{_owner}/{_repository}/releases/{releaseId}");
            if (!releaseResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get release {ReleaseId} for asset upload", releaseId);
                return false;
            }

            var releaseJson = await releaseResponse.Content.ReadAsStringAsync();
            var release = JsonSerializer.Deserialize<JsonElement>(releaseJson);
            var uploadUrl = release.GetProperty("upload_url").GetString()?.Replace("{?name,label}", "");

            if (string.IsNullOrEmpty(uploadUrl))
            {
                _logger.LogError("Upload URL not found for release {ReleaseId}", releaseId);
                return false;
            }

            // Upload the asset
            using var fileStream = File.OpenRead(assetPath);
            using var content = new StreamContent(fileStream);
            content.Headers.ContentType = new MediaTypeHeaderValue(mimeType);

            var uploadResponse = await _httpClient.PostAsync($"{uploadUrl}?name={fileName}", content);
            
            if (uploadResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation("Release asset {FileName} uploaded successfully to release {ReleaseId} with correlation ID: {CorrelationId}", 
                    fileName, releaseId, correlationId);
                return true;
            }
            
            var errorContent = await uploadResponse.Content.ReadAsStringAsync();
            _logger.LogError("Failed to upload release asset {FileName}. Status: {StatusCode}, Error: {Error}", 
                fileName, uploadResponse.StatusCode, errorContent);
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload release asset {AssetPath} to release {ReleaseId}", assetPath, releaseId);
            return false;
        }
    }

    /// <summary>
    /// Task 6.3.4: Create release notes generation from commit history
    /// </summary>
    public async Task<string> GenerateReleaseNotesAsync(string fromTag, string toTag = "HEAD")
    {
        try
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogInformation("Generating release notes from {FromTag} to {ToTag} with correlation ID: {CorrelationId}", 
                fromTag, toTag, correlationId);

            // Get commits between tags
            var commits = await GetCommitsBetweenAsync(fromTag, toTag);
            
            // Group commits by type
            var groupedCommits = GroupCommitsByType(commits);
            
            // Generate release notes
            var releaseNotes = new StringBuilder();
            releaseNotes.AppendLine($"## What's Changed");
            releaseNotes.AppendLine();

            // Features
            if (groupedCommits.ContainsKey("feat"))
            {
                releaseNotes.AppendLine("### 🚀 Features");
                foreach (var commit in groupedCommits["feat"])
                {
                    releaseNotes.AppendLine($"- {commit.Description} ({commit.ShortSha})");
                }
                releaseNotes.AppendLine();
            }

            // Bug fixes
            if (groupedCommits.ContainsKey("fix"))
            {
                releaseNotes.AppendLine("### 🐛 Bug Fixes");
                foreach (var commit in groupedCommits["fix"])
                {
                    releaseNotes.AppendLine($"- {commit.Description} ({commit.ShortSha})");
                }
                releaseNotes.AppendLine();
            }

            // Breaking changes
            var breakingChanges = commits.Where(c => c.IsBreakingChange).ToList();
            if (breakingChanges.Any())
            {
                releaseNotes.AppendLine("### ⚠️ BREAKING CHANGES");
                foreach (var commit in breakingChanges)
                {
                    releaseNotes.AppendLine($"- {commit.Description} ({commit.ShortSha})");
                }
                releaseNotes.AppendLine();
            }

            // Other changes
            var otherTypes = groupedCommits.Keys.Except(new[] { "feat", "fix" }).ToList();
            if (otherTypes.Any())
            {
                releaseNotes.AppendLine("### 🔧 Other Changes");
                foreach (var type in otherTypes)
                {
                    foreach (var commit in groupedCommits[type])
                    {
                        releaseNotes.AppendLine($"- {commit.Description} ({commit.ShortSha})");
                    }
                }
                releaseNotes.AppendLine();
            }

            // Add contributors
            var contributors = commits.Select(c => c.Author).Distinct().ToList();
            if (contributors.Any())
            {
                releaseNotes.AppendLine("### 👥 Contributors");
                foreach (var contributor in contributors)
                {
                    releaseNotes.AppendLine($"- @{contributor}");
                }
            }

            var notes = releaseNotes.ToString();
            _logger.LogInformation("Release notes generated successfully with {CommitCount} commits and correlation ID: {CorrelationId}", 
                commits.Count, correlationId);

            return notes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate release notes from {FromTag} to {ToTag}", fromTag, toTag);
            return $"## What's Changed\n\nRelease notes could not be generated automatically.";
        }
    }

    /// <summary>
    /// Task 6.3.5: Implement GitHub Actions integration for CI/CD
    /// </summary>
    public async Task<bool> TriggerWorkflowAsync(string workflowName, string branch = "main", Dictionary<string, object>? inputs = null)
    {
        try
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogInformation("Triggering GitHub workflow {WorkflowName} on branch {Branch} with correlation ID: {CorrelationId}", 
                workflowName, branch, correlationId);

            var workflowData = new
            {
                @ref = branch,
                inputs = inputs ?? new Dictionary<string, object>()
            };

            var json = JsonSerializer.Serialize(workflowData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                $"{_baseUrl}/repos/{_owner}/{_repository}/actions/workflows/{workflowName}/dispatches", 
                content);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("GitHub workflow {WorkflowName} triggered successfully with correlation ID: {CorrelationId}", 
                    workflowName, correlationId);
                return true;
            }
            
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to trigger GitHub workflow {WorkflowName}. Status: {StatusCode}, Error: {Error}", 
                workflowName, response.StatusCode, errorContent);
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger GitHub workflow {WorkflowName}", workflowName);
            return false;
        }
    }

    /// <summary>
    /// Task 6.3.6: Create issue and pull request management
    /// </summary>
    public async Task<GitHubIssue?> CreateIssueAsync(string title, string body, List<string>? labels = null, List<string>? assignees = null)
    {
        try
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogInformation("Creating GitHub issue '{Title}' with correlation ID: {CorrelationId}", 
                title, correlationId);

            var issueData = new
            {
                title = title,
                body = body,
                labels = labels ?? new List<string>(),
                assignees = assignees ?? new List<string>()
            };

            var json = JsonSerializer.Serialize(issueData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/repos/{_owner}/{_repository}/issues", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                var issue = JsonSerializer.Deserialize<GitHubIssue>(responseJson);
                
                _logger.LogInformation("GitHub issue '{Title}' created successfully with number {IssueNumber} and correlation ID: {CorrelationId}", 
                    title, issue?.Number, correlationId);
                
                return issue;
            }
            
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to create GitHub issue '{Title}'. Status: {StatusCode}, Error: {Error}", 
                title, response.StatusCode, errorContent);
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create GitHub issue '{Title}'", title);
            return null;
        }
    }

    /// <summary>
    /// Task 6.3.7: Implement repository statistics and metrics collection
    /// </summary>
    public async Task<GitHubRepositoryStats?> GetRepositoryStatsAsync()
    {
        try
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogInformation("Collecting repository statistics with correlation ID: {CorrelationId}", correlationId);

            // Get repository info
            var repoResponse = await _httpClient.GetAsync($"{_baseUrl}/repos/{_owner}/{_repository}");
            if (!repoResponse.IsSuccessStatusCode)
            {
                return null;
            }

            var repoJson = await repoResponse.Content.ReadAsStringAsync();
            var repo = JsonSerializer.Deserialize<JsonElement>(repoJson);

            // Get additional stats
            var contributorsResponse = await _httpClient.GetAsync($"{_baseUrl}/repos/{_owner}/{_repository}/contributors");
            var releasesResponse = await _httpClient.GetAsync($"{_baseUrl}/repos/{_owner}/{_repository}/releases");
            var issuesResponse = await _httpClient.GetAsync($"{_baseUrl}/repos/{_owner}/{_repository}/issues?state=all");

            var stats = new GitHubRepositoryStats
            {
                CorrelationId = correlationId,
                Name = repo.GetProperty("name").GetString() ?? "",
                FullName = repo.GetProperty("full_name").GetString() ?? "",
                Description = repo.GetProperty("description").GetString(),
                Stars = repo.GetProperty("stargazers_count").GetInt32(),
                Forks = repo.GetProperty("forks_count").GetInt32(),
                Watchers = repo.GetProperty("watchers_count").GetInt32(),
                Size = repo.GetProperty("size").GetInt32(),
                DefaultBranch = repo.GetProperty("default_branch").GetString() ?? "",
                Language = repo.GetProperty("language").GetString(),
                CreatedAt = repo.GetProperty("created_at").GetDateTime(),
                UpdatedAt = repo.GetProperty("updated_at").GetDateTime(),
                PushedAt = repo.GetProperty("pushed_at").GetDateTime(),
                IsPrivate = repo.GetProperty("private").GetBoolean(),
                HasIssues = repo.GetProperty("has_issues").GetBoolean(),
                HasWiki = repo.GetProperty("has_wiki").GetBoolean(),
                HasPages = repo.GetProperty("has_pages").GetBoolean(),
                ContributorCount = contributorsResponse.IsSuccessStatusCode ? await CountArrayElementsAsync(contributorsResponse) : 0,
                ReleaseCount = releasesResponse.IsSuccessStatusCode ? await CountArrayElementsAsync(releasesResponse) : 0,
                IssueCount = issuesResponse.IsSuccessStatusCode ? await CountArrayElementsAsync(issuesResponse) : 0
            };

            _logger.LogInformation("Repository statistics collected successfully with correlation ID: {CorrelationId}", correlationId);
            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to collect repository statistics");
            return null;
        }
    }

    #region Private Helper Methods

    private void ConfigureHttpClient()
    {
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "AnomaliImportTool/1.0");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
        _httpClient.Timeout = TimeSpan.FromMinutes(5);
    }

    private static bool IsValidSemanticVersion(string version)
    {
        var semverPattern = @"^v?(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-((?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+([0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$";
        return System.Text.RegularExpressions.Regex.IsMatch(version, semverPattern);
    }

    private static string GetContentType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".exe" => "application/octet-stream",
            ".zip" => "application/zip",
            ".tar" => "application/x-tar",
            ".gz" => "application/gzip",
            ".json" => "application/json",
            ".xml" => "application/xml",
            ".txt" => "text/plain",
            ".md" => "text/markdown",
            _ => "application/octet-stream"
        };
    }

    private async Task<List<GitHubCommit>> GetCommitsBetweenAsync(string fromTag, string toTag)
    {
        // This is a simplified implementation
        // In a real scenario, you would use the GitHub API to get commits between tags
        var commits = new List<GitHubCommit>();
        
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/repos/{_owner}/{_repository}/commits?since={fromTag}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var commitArray = JsonSerializer.Deserialize<JsonElement[]>(json);
                
                foreach (var commitElement in commitArray ?? Array.Empty<JsonElement>())
                {
                    var commit = ParseCommitFromJson(commitElement);
                    if (commit != null)
                    {
                        commits.Add(commit);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get commits between {FromTag} and {ToTag}", fromTag, toTag);
        }

        return commits;
    }

    private static Dictionary<string, List<GitHubCommit>> GroupCommitsByType(List<GitHubCommit> commits)
    {
        return commits
            .Where(c => !string.IsNullOrEmpty(c.Type))
            .GroupBy(c => c.Type!)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    private static GitHubCommit? ParseCommitFromJson(JsonElement commitElement)
    {
        try
        {
            var commit = commitElement.GetProperty("commit");
            var message = commit.GetProperty("message").GetString() ?? "";
            var author = commit.GetProperty("author");
            
            return new GitHubCommit
            {
                Sha = commitElement.GetProperty("sha").GetString() ?? "",
                ShortSha = commitElement.GetProperty("sha").GetString()?[..8] ?? "",
                Message = message,
                Description = ExtractDescription(message),
                Type = ExtractType(message),
                IsBreakingChange = message.Contains("BREAKING CHANGE") || message.Contains("!"),
                Author = author.GetProperty("name").GetString() ?? "",
                Date = author.GetProperty("date").GetDateTime()
            };
        }
        catch
        {
            return null;
        }
    }

    private static string ExtractDescription(string message)
    {
        var lines = message.Split('\n');
        return lines.Length > 0 ? lines[0] : message;
    }

    private static string? ExtractType(string message)
    {
        var match = System.Text.RegularExpressions.Regex.Match(message, @"^(\w+)(?:\(.+\))?!?:");
        return match.Success ? match.Groups[1].Value : null;
    }

    private async Task<int> CountArrayElementsAsync(HttpResponseMessage response)
    {
        try
        {
            var json = await response.Content.ReadAsStringAsync();
            var array = JsonSerializer.Deserialize<JsonElement[]>(json);
            return array?.Length ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    #endregion
}

/// <summary>
/// GitHub release information
/// </summary>
public class GitHubRelease
{
    public long Id { get; set; }
    public string TagName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool Draft { get; set; }
    public bool Prerelease { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime PublishedAt { get; set; }
    public string HtmlUrl { get; set; } = string.Empty;
}

/// <summary>
/// GitHub issue information
/// </summary>
public class GitHubIssue
{
    public long Id { get; set; }
    public int Number { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string HtmlUrl { get; set; } = string.Empty;
}

/// <summary>
/// GitHub commit information
/// </summary>
public class GitHubCommit
{
    public string Sha { get; set; } = string.Empty;
    public string ShortSha { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Type { get; set; }
    public bool IsBreakingChange { get; set; }
    public string Author { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}

/// <summary>
/// GitHub repository statistics
/// </summary>
public class GitHubRepositoryStats
{
    public string CorrelationId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Stars { get; set; }
    public int Forks { get; set; }
    public int Watchers { get; set; }
    public int Size { get; set; }
    public string DefaultBranch { get; set; } = string.Empty;
    public string? Language { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime PushedAt { get; set; }
    public bool IsPrivate { get; set; }
    public bool HasIssues { get; set; }
    public bool HasWiki { get; set; }
    public bool HasPages { get; set; }
    public int ContributorCount { get; set; }
    public int ReleaseCount { get; set; }
    public int IssueCount { get; set; }
}

/// <summary>
/// Interface for GitHub integration operations
/// </summary>
public interface IGitHubService
{
    Task<bool> AuthenticateAsync(string token);
    Task<GitHubRelease?> CreateReleaseAsync(string version, string name, string body, bool isPrerelease = false, bool isDraft = false, string? targetCommitish = null);
    Task<bool> UploadReleaseAssetAsync(long releaseId, string assetPath, string? assetName = null, string? contentType = null);
    Task<string> GenerateReleaseNotesAsync(string fromTag, string toTag = "HEAD");
    Task<bool> TriggerWorkflowAsync(string workflowName, string branch = "main", Dictionary<string, object>? inputs = null);
    Task<GitHubIssue?> CreateIssueAsync(string title, string body, List<string>? labels = null, List<string>? assignees = null);
    Task<GitHubRepositoryStats?> GetRepositoryStatsAsync();
} 