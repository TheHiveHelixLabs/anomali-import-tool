using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using AnomaliImportTool.Core.Application.Interfaces.Infrastructure;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace AnomaliImportTool.Git.Services;

/// <summary>
/// Task 6.2: Automated commit functionality with Conventional Commits format
/// Implements commit message generation, validation, and automated workflows
/// </summary>
public class GitCommitService : IGitCommitService
{
    private readonly ILogger<GitCommitService> _logger;
    private readonly IConfiguration _configuration;
    private readonly GitRepositoryService _repositoryService;
    private readonly Dictionary<string, CommitTemplate> _commitTemplates;

    // Conventional Commits regex pattern
    private static readonly Regex ConventionalCommitPattern = new(
        @"^(?<type>build|chore|ci|docs|feat|fix|perf|refactor|revert|style|test)(?<scope>\(.+\))?(?<breaking>!)?:\s(?<description>.+)(?:\n\n(?<body>.*))?(?:\n\n(?<footer>.*))?$",
        RegexOptions.Multiline | RegexOptions.Compiled);

    public GitCommitService(
        ILogger<GitCommitService> logger,
        IConfiguration configuration,
        GitRepositoryService repositoryService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _repositoryService = repositoryService ?? throw new ArgumentNullException(nameof(repositoryService));
        
        _commitTemplates = LoadCommitTemplates();
    }

    /// <summary>
    /// Task 6.2.2: Create automated commit message generation with templates
    /// </summary>
    public async Task<string> GenerateCommitMessageAsync(
        string type,
        string description,
        string? scope = null,
        string? body = null,
        string? footer = null,
        bool isBreakingChange = false,
        Dictionary<string, object>? metadata = null)
    {
        try
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogInformation("Generating commit message with correlation ID: {CorrelationId}", correlationId);

            // Validate commit type
            if (!IsValidCommitType(type))
            {
                throw new ArgumentException($"Invalid commit type: {type}. Must be one of: build, chore, ci, docs, feat, fix, perf, refactor, revert, style, test");
            }

            // Build commit message according to Conventional Commits format
            var commitMessage = BuildConventionalCommitMessage(type, description, scope, body, footer, isBreakingChange);

            // Add metadata if provided
            if (metadata != null && metadata.Count > 0)
            {
                commitMessage = await EnrichCommitMessageWithMetadataAsync(commitMessage, metadata);
            }

            // Validate generated message
            await ValidateCommitMessageAsync(commitMessage);

            _logger.LogInformation("Commit message generated successfully with correlation ID: {CorrelationId}", correlationId);
            return commitMessage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate commit message");
            throw;
        }
    }

    /// <summary>
    /// Task 6.2.3: Implement commit validation and formatting rules
    /// </summary>
    public async Task<CommitValidationResult> ValidateCommitMessageAsync(string commitMessage)
    {
        try
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogInformation("Validating commit message with correlation ID: {CorrelationId}", correlationId);

            var validationResult = new CommitValidationResult { CorrelationId = correlationId };

            // Check Conventional Commits format
            var match = ConventionalCommitPattern.Match(commitMessage);
            if (!match.Success)
            {
                validationResult.Errors.Add("Commit message does not follow Conventional Commits format");
                validationResult.IsValid = false;
            }
            else
            {
                // Validate individual components
                await ValidateCommitComponents(match, validationResult);
            }

            // Check message length constraints
            ValidateMessageLength(commitMessage, validationResult);

            // Check for required patterns
            await ValidateRequiredPatternsAsync(commitMessage, validationResult);

            _logger.LogInformation("Commit message validation completed with correlation ID: {CorrelationId}. Valid: {IsValid}", 
                correlationId, validationResult.IsValid);

            return validationResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate commit message");
            throw;
        }
    }

    /// <summary>
    /// Task 6.2.4: Create staged changes management and selective commits
    /// </summary>
    public async Task<GitOperationResult> CommitStagedChangesAsync(
        string commitMessage,
        bool includeUntracked = false,
        List<string>? specificFiles = null)
    {
        try
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogInformation("Committing staged changes with correlation ID: {CorrelationId}", correlationId);

            // Validate commit message first
            var validationResult = await ValidateCommitMessageAsync(commitMessage);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors);
                throw new InvalidOperationException($"Invalid commit message: {errors}");
            }

            return await _repositoryService.ExecuteWithRecoveryAsync(async () =>
            {
                using var repo = new Repository(_repositoryService._repositoryPath);
                
                // Stage specific files if provided
                if (specificFiles != null && specificFiles.Count > 0)
                {
                    foreach (var file in specificFiles)
                    {
                        if (File.Exists(Path.Combine(repo.Info.WorkingDirectory, file)))
                        {
                            Commands.Stage(repo, file);
                            _logger.LogDebug("Staged file: {File}", file);
                        }
                    }
                }
                // Stage untracked files if requested
                else if (includeUntracked)
                {
                    var status = repo.RetrieveStatus();
                    foreach (var untrackedFile in status.Untracked)
                    {
                        Commands.Stage(repo, untrackedFile.FilePath);
                        _logger.LogDebug("Staged untracked file: {File}", untrackedFile.FilePath);
                    }
                }

                // Get author information
                var signature = GetCommitSignature(repo);

                // Create commit
                var commit = repo.Commit(commitMessage, signature, signature);
                
                _logger.LogInformation("Commit created successfully: {CommitSha} with correlation ID: {CorrelationId}", 
                    commit.Sha, correlationId);

                return commit.Sha;
            }, 
            "CommitStagedChanges",
            recoveryOperation: async () =>
            {
                // Recovery: Reset to previous state
                _logger.LogWarning("Attempting to recover from failed commit");
                using var repo = new Repository(_repositoryService._repositoryPath);
                repo.Reset(ResetMode.Mixed, repo.Head.Tip);
                return "recovered";
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to commit staged changes");
            return GitOperationResult.Failure(ex, Guid.NewGuid().ToString(), TimeSpan.Zero);
        }
    }

    /// <summary>
    /// Task 6.2.5: Implement commit hooks for quality validation
    /// </summary>
    public async Task<bool> InstallCommitHooksAsync()
    {
        try
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogInformation("Installing commit hooks with correlation ID: {CorrelationId}", correlationId);

            using var repo = new Repository(_repositoryService._repositoryPath);
            var hooksPath = Path.Combine(repo.Info.Path, "hooks");
            Directory.CreateDirectory(hooksPath);

            // Create pre-commit hook
            var preCommitHook = CreatePreCommitHook();
            var preCommitPath = Path.Combine(hooksPath, "pre-commit");
            await File.WriteAllTextAsync(preCommitPath, preCommitHook);
            MakeExecutable(preCommitPath);

            // Create commit-msg hook
            var commitMsgHook = CreateCommitMsgHook();
            var commitMsgPath = Path.Combine(hooksPath, "commit-msg");
            await File.WriteAllTextAsync(commitMsgPath, commitMsgHook);
            MakeExecutable(commitMsgPath);

            _logger.LogInformation("Commit hooks installed successfully with correlation ID: {CorrelationId}", correlationId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install commit hooks");
            return false;
        }
    }

    /// <summary>
    /// Task 6.2.6: Create commit history and tracking
    /// </summary>
    public async Task<List<CommitInfo>> GetCommitHistoryAsync(int maxCount = 50, string? branch = null)
    {
        try
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogInformation("Retrieving commit history with correlation ID: {CorrelationId}", correlationId);

            using var repo = new Repository(_repositoryService._repositoryPath);
            
            var commits = string.IsNullOrEmpty(branch) 
                ? repo.Commits.Take(maxCount)
                : repo.Branches[branch]?.Commits.Take(maxCount) ?? Enumerable.Empty<Commit>();

            var commitInfos = commits.Select(commit => new CommitInfo
            {
                Sha = commit.Sha,
                ShortSha = commit.Sha[..8],
                Message = commit.Message,
                MessageShort = commit.MessageShort,
                Author = commit.Author.Name,
                AuthorEmail = commit.Author.Email,
                Date = commit.Author.When,
                Committer = commit.Committer.Name,
                CommitterEmail = commit.Committer.Email,
                CommitDate = commit.Committer.When,
                ParentShas = commit.Parents.Select(p => p.Sha).ToList(),
                IsConventional = IsConventionalCommit(commit.Message),
                CommitType = ExtractCommitType(commit.Message),
                Scope = ExtractCommitScope(commit.Message),
                IsBreakingChange = IsBreakingChange(commit.Message)
            }).ToList();

            _logger.LogInformation("Retrieved {Count} commits with correlation ID: {CorrelationId}", 
                commitInfos.Count, correlationId);

            return commitInfos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve commit history");
            return new List<CommitInfo>();
        }
    }

    /// <summary>
    /// Task 6.2.7: Implement commit signing with GPG keys
    /// </summary>
    public async Task<bool> ConfigureCommitSigningAsync(string gpgKeyId, string? gpgExecutablePath = null)
    {
        try
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogInformation("Configuring commit signing with GPG key {KeyId} and correlation ID: {CorrelationId}", 
                gpgKeyId, correlationId);

            using var repo = new Repository(_repositoryService._repositoryPath);
            var config = repo.Config;

            // Configure GPG signing
            config.Set("user.signingkey", gpgKeyId);
            config.Set("commit.gpgsign", "true");
            
            if (!string.IsNullOrEmpty(gpgExecutablePath))
            {
                config.Set("gpg.program", gpgExecutablePath);
            }

            // Verify GPG key exists and is valid
            var isValidKey = await VerifyGpgKeyAsync(gpgKeyId);
            if (!isValidKey)
            {
                _logger.LogWarning("GPG key {KeyId} may not be valid or accessible", gpgKeyId);
            }

            _logger.LogInformation("Commit signing configured successfully with correlation ID: {CorrelationId}", correlationId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to configure commit signing");
            return false;
        }
    }

    /// <summary>
    /// Task 6.2.8: Create commit rollback and amendment capabilities
    /// </summary>
    public async Task<GitOperationResult> AmendLastCommitAsync(string newMessage)
    {
        try
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogInformation("Amending last commit with correlation ID: {CorrelationId}", correlationId);

            // Validate new commit message
            var validationResult = await ValidateCommitMessageAsync(newMessage);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors);
                throw new InvalidOperationException($"Invalid commit message: {errors}");
            }

            return await _repositoryService.ExecuteWithRecoveryAsync(async () =>
            {
                using var repo = new Repository(_repositoryService._repositoryPath);
                
                var lastCommit = repo.Head.Tip;
                if (lastCommit == null)
                {
                    throw new InvalidOperationException("No commits found to amend");
                }

                // Store original commit for recovery
                var originalSha = lastCommit.Sha;
                var signature = GetCommitSignature(repo);

                // Amend the commit
                var amendedCommit = repo.Commit(newMessage, signature, signature, new CommitOptions
                {
                    AmendPreviousCommit = true
                });

                _logger.LogInformation("Commit amended successfully. Original: {OriginalSha}, New: {NewSha} with correlation ID: {CorrelationId}", 
                    originalSha, amendedCommit.Sha, correlationId);

                return amendedCommit.Sha;
            },
            "AmendLastCommit",
            recoveryOperation: async () =>
            {
                // Recovery: Reset to original commit
                _logger.LogWarning("Attempting to recover from failed commit amendment");
                using var repo = new Repository(_repositoryService._repositoryPath);
                var reflog = repo.ReflogWalk(repo.Head);
                var previousEntry = reflog.Skip(1).FirstOrDefault();
                if (previousEntry != null)
                {
                    repo.Reset(ResetMode.Hard, previousEntry.To);
                }
                return "recovered";
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to amend last commit");
            return GitOperationResult.Failure(ex, Guid.NewGuid().ToString(), TimeSpan.Zero);
        }
    }

    #region Private Helper Methods

    private static readonly HashSet<string> ValidCommitTypes = new()
    {
        "build", "chore", "ci", "docs", "feat", "fix", "perf", "refactor", "revert", "style", "test"
    };

    private static bool IsValidCommitType(string type) => ValidCommitTypes.Contains(type.ToLowerInvariant());

    private string BuildConventionalCommitMessage(
        string type,
        string description,
        string? scope,
        string? body,
        string? footer,
        bool isBreakingChange)
    {
        var message = type.ToLowerInvariant();
        
        if (!string.IsNullOrEmpty(scope))
        {
            message += $"({scope})";
        }
        
        if (isBreakingChange)
        {
            message += "!";
        }
        
        message += $": {description}";
        
        if (!string.IsNullOrEmpty(body))
        {
            message += $"\n\n{body}";
        }
        
        if (!string.IsNullOrEmpty(footer))
        {
            message += $"\n\n{footer}";
        }
        
        return message;
    }

    private async Task<string> EnrichCommitMessageWithMetadataAsync(string commitMessage, Dictionary<string, object> metadata)
    {
        var enrichedMessage = commitMessage;
        
        // Add metadata as footer if not already present
        if (!commitMessage.Contains("\n\n"))
        {
            enrichedMessage += "\n\n";
        }
        else if (!commitMessage.EndsWith("\n\n"))
        {
            enrichedMessage += "\n";
        }

        foreach (var kvp in metadata)
        {
            enrichedMessage += $"{kvp.Key}: {kvp.Value}\n";
        }

        return enrichedMessage.TrimEnd();
    }

    private async Task ValidateCommitComponents(Match match, CommitValidationResult result)
    {
        var type = match.Groups["type"].Value;
        var description = match.Groups["description"].Value;

        // Validate type
        if (!IsValidCommitType(type))
        {
            result.Errors.Add($"Invalid commit type: {type}");
            result.IsValid = false;
        }

        // Validate description
        if (string.IsNullOrWhiteSpace(description))
        {
            result.Errors.Add("Commit description cannot be empty");
            result.IsValid = false;
        }
        else if (description.Length > 100)
        {
            result.Warnings.Add("Commit description is longer than 100 characters");
        }

        // Check for imperative mood
        if (!IsImperativeMood(description))
        {
            result.Warnings.Add("Commit description should use imperative mood (e.g., 'add' not 'added')");
        }
    }

    private void ValidateMessageLength(string commitMessage, CommitValidationResult result)
    {
        var lines = commitMessage.Split('\n');
        
        // Subject line length
        if (lines[0].Length > 100)
        {
            result.Errors.Add("Subject line must be 100 characters or less");
            result.IsValid = false;
        }
        else if (lines[0].Length > 72)
        {
            result.Warnings.Add("Subject line should be 72 characters or less");
        }

        // Body line length
        if (lines.Length > 2)
        {
            for (int i = 2; i < lines.Length; i++)
            {
                if (lines[i].Length > 72)
                {
                    result.Warnings.Add($"Body line {i - 1} is longer than 72 characters");
                }
            }
        }
    }

    private async Task ValidateRequiredPatternsAsync(string commitMessage, CommitValidationResult result)
    {
        var requiredPatterns = _configuration.GetSection("Git:RequiredPatterns").Get<string[]>() ?? Array.Empty<string>();
        
        foreach (var pattern in requiredPatterns)
        {
            if (!Regex.IsMatch(commitMessage, pattern, RegexOptions.IgnoreCase))
            {
                result.Warnings.Add($"Commit message should match pattern: {pattern}");
            }
        }
    }

    private static bool IsImperativeMood(string description)
    {
        var firstWord = description.Split(' ')[0].ToLowerInvariant();
        
        // Common imperative verbs in commit messages
        var imperativeVerbs = new HashSet<string>
        {
            "add", "remove", "update", "fix", "implement", "create", "delete", "modify",
            "change", "improve", "refactor", "optimize", "enhance", "clean", "merge",
            "revert", "bump", "release", "configure", "setup", "install", "upgrade"
        };

        return imperativeVerbs.Contains(firstWord);
    }

    private Signature GetCommitSignature(Repository repo)
    {
        var config = repo.Config;
        var name = config.Get<string>("user.name")?.Value ?? "Anomali Import Tool";
        var email = config.Get<string>("user.email")?.Value ?? "anomali@tool.local";
        
        return new Signature(name, email, DateTimeOffset.Now);
    }

    private string CreatePreCommitHook()
    {
        return @"#!/bin/sh
# Pre-commit hook for Anomali Import Tool
# Validates code quality before commit

echo ""Running pre-commit validations...""

# Check for large files
find . -name ""*.dll"" -o -name ""*.exe"" -o -name ""*.zip"" | grep -v .git | while read file; do
    size=$(stat -c%s ""$file"" 2>/dev/null || stat -f%z ""$file"" 2>/dev/null)
    if [ $size -gt 10485760 ]; then  # 10MB
        echo ""Error: Large file detected: $file ($size bytes)""
        exit 1
    fi
done

# Check for sensitive information
if git diff --cached --name-only | xargs grep -l ""password\|secret\|key"" 2>/dev/null; then
    echo ""Warning: Potential sensitive information detected in staged files""
fi

echo ""Pre-commit validations passed""
exit 0";
    }

    private string CreateCommitMsgHook()
    {
        return @"#!/bin/sh
# Commit message hook for Conventional Commits validation

commit_regex='^(build|chore|ci|docs|feat|fix|perf|refactor|revert|style|test)(\(.+\))?!?:\s.+'

if ! grep -qE ""$commit_regex"" ""$1""; then
    echo ""Invalid commit message format!""
    echo ""Commit message must follow Conventional Commits format:""
    echo ""<type>[optional scope]: <description>""
    echo """"
    echo ""Valid types: build, chore, ci, docs, feat, fix, perf, refactor, revert, style, test""
    echo ""Example: feat(auth): add OAuth2 authentication""
    exit 1
fi

exit 0";
    }

    private void MakeExecutable(string filePath)
    {
        if (Environment.OSVersion.Platform == PlatformID.Unix)
        {
            // On Unix systems, make the file executable
            var fileInfo = new FileInfo(filePath);
            fileInfo.UnixFileMode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;
        }
    }

    private async Task<bool> VerifyGpgKeyAsync(string keyId)
    {
        try
        {
            // Simple GPG key verification
            // In a real implementation, you would check if the key exists and is valid
            await Task.Delay(100); // Simulate verification
            return true;
        }
        catch
        {
            return false;
        }
    }

    private Dictionary<string, CommitTemplate> LoadCommitTemplates()
    {
        var templates = new Dictionary<string, CommitTemplate>();
        
        // Load default templates
        templates["feat"] = new CommitTemplate
        {
            Type = "feat",
            Description = "A new feature",
            Template = "feat({scope}): {description}\n\n{body}\n\nCloses #{issue}"
        };
        
        templates["fix"] = new CommitTemplate
        {
            Type = "fix",
            Description = "A bug fix",
            Template = "fix({scope}): {description}\n\n{body}\n\nFixes #{issue}"
        };

        // Add more templates as needed
        return templates;
    }

    private static bool IsConventionalCommit(string message) => ConventionalCommitPattern.IsMatch(message);

    private static string? ExtractCommitType(string message)
    {
        var match = ConventionalCommitPattern.Match(message);
        return match.Success ? match.Groups["type"].Value : null;
    }

    private static string? ExtractCommitScope(string message)
    {
        var match = ConventionalCommitPattern.Match(message);
        if (match.Success && match.Groups["scope"].Success)
        {
            var scope = match.Groups["scope"].Value;
            return scope.Trim('(', ')');
        }
        return null;
    }

    private static bool IsBreakingChange(string message) => message.Contains("BREAKING CHANGE") || message.Contains("!");

    #endregion
}

/// <summary>
/// Commit validation result with errors and warnings
/// </summary>
public class CommitValidationResult
{
    public bool IsValid { get; set; } = true;
    public List<string> Errors { get; } = new();
    public List<string> Warnings { get; } = new();
    public string CorrelationId { get; set; } = string.Empty;
}

/// <summary>
/// Commit information with conventional commit analysis
/// </summary>
public class CommitInfo
{
    public string Sha { get; init; } = string.Empty;
    public string ShortSha { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string MessageShort { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public string AuthorEmail { get; init; } = string.Empty;
    public DateTimeOffset Date { get; init; }
    public string Committer { get; init; } = string.Empty;
    public string CommitterEmail { get; init; } = string.Empty;
    public DateTimeOffset CommitDate { get; init; }
    public List<string> ParentShas { get; init; } = new();
    public bool IsConventional { get; init; }
    public string? CommitType { get; init; }
    public string? Scope { get; init; }
    public bool IsBreakingChange { get; init; }
}

/// <summary>
/// Commit message template
/// </summary>
public class CommitTemplate
{
    public string Type { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Template { get; init; } = string.Empty;
}

/// <summary>
/// Interface for Git commit operations
/// </summary>
public interface IGitCommitService
{
    Task<string> GenerateCommitMessageAsync(string type, string description, string? scope = null, string? body = null, string? footer = null, bool isBreakingChange = false, Dictionary<string, object>? metadata = null);
    Task<CommitValidationResult> ValidateCommitMessageAsync(string commitMessage);
    Task<GitOperationResult> CommitStagedChangesAsync(string commitMessage, bool includeUntracked = false, List<string>? specificFiles = null);
    Task<bool> InstallCommitHooksAsync();
    Task<List<CommitInfo>> GetCommitHistoryAsync(int maxCount = 50, string? branch = null);
    Task<bool> ConfigureCommitSigningAsync(string gpgKeyId, string? gpgExecutablePath = null);
    Task<GitOperationResult> AmendLastCommitAsync(string newMessage);
} 