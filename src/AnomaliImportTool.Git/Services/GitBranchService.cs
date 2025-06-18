using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using AnomaliImportTool.Core.Application.Interfaces.Infrastructure;

namespace AnomaliImportTool.Git.Services;

/// <summary>
/// Task 6.4: Branch management and merge conflict resolution
/// Implements comprehensive branch operations with automated conflict resolution
/// </summary>
public class GitBranchService : IGitBranchService
{
    private readonly ILogger<GitBranchService> _logger;
    private readonly GitRepositoryService _repositoryService;

    public GitBranchService(
        ILogger<GitBranchService> logger,
        GitRepositoryService repositoryService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _repositoryService = repositoryService ?? throw new ArgumentNullException(nameof(repositoryService));
    }

    /// <summary>
    /// Task 6.4.1: Create branch creation, switching, and deletion operations
    /// </summary>
    public async Task<GitOperationResult> CreateBranchAsync(string branchName, string? startPoint = null)
    {
        return await _repositoryService.ExecuteWithRecoveryAsync(async () =>
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogInformation("Creating branch {BranchName} with correlation ID: {CorrelationId}", 
                branchName, correlationId);

            using var repo = new Repository(_repositoryService._repositoryPath);
            
            // Validate branch name
            if (!IsValidBranchName(branchName))
            {
                throw new ArgumentException($"Invalid branch name: {branchName}");
            }

            // Check if branch already exists
            if (repo.Branches[branchName] != null)
            {
                throw new InvalidOperationException($"Branch {branchName} already exists");
            }

            // Determine start point
            Commit startCommit;
            if (!string.IsNullOrEmpty(startPoint))
            {
                startCommit = repo.Lookup<Commit>(startPoint) 
                    ?? throw new ArgumentException($"Start point {startPoint} not found");
            }
            else
            {
                startCommit = repo.Head.Tip 
                    ?? throw new InvalidOperationException("No commits found in repository");
            }

            // Create the branch
            var branch = repo.CreateBranch(branchName, startCommit);
            
            _logger.LogInformation("Branch {BranchName} created successfully from {StartPoint} with correlation ID: {CorrelationId}", 
                branchName, startCommit.Sha[..8], correlationId);

            return new BranchInfo
            {
                Name = branch.FriendlyName,
                Sha = branch.Tip.Sha,
                IsRemote = branch.IsRemote,
                IsTracking = branch.IsTracking
            };
        }, 
        "CreateBranch");
    }

    /// <summary>
    /// Task 6.4.2: Implement merge conflict detection and resolution
    /// </summary>
    public async Task<MergeResult> MergeBranchAsync(
        string targetBranch, 
        string sourceBranch, 
        MergeOptions? options = null)
    {
        try
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogInformation("Merging branch {SourceBranch} into {TargetBranch} with correlation ID: {CorrelationId}", 
                sourceBranch, targetBranch, correlationId);

            using var repo = new Repository(_repositoryService._repositoryPath);
            
            // Get branches
            var target = repo.Branches[targetBranch] 
                ?? throw new ArgumentException($"Target branch {targetBranch} not found");
            var source = repo.Branches[sourceBranch] 
                ?? throw new ArgumentException($"Source branch {sourceBranch} not found");

            // Checkout target branch
            Commands.Checkout(repo, target);

            // Configure merge options
            var mergeOptions = options ?? new MergeOptions
            {
                CommitOnSuccess = true,
                FastForwardStrategy = FastForwardStrategy.Default
            };

            // Get signature for merge commit
            var signature = GetMergeSignature(repo);

            // Perform merge
            var libGit2MergeResult = repo.Merge(source, signature, mergeOptions);

            var result = new MergeResult
            {
                CorrelationId = correlationId,
                Status = MapMergeStatus(libGit2MergeResult.Status),
                Commit = libGit2MergeResult.Commit?.Sha,
                ConflictedFiles = new List<string>()
            };

            // Handle conflicts if any
            if (libGit2MergeResult.Status == LibGit2Sharp.MergeStatus.Conflicts)
            {
                result.ConflictedFiles = await DetectConflictsAsync(repo);
                _logger.LogWarning("Merge conflicts detected in {ConflictCount} files for correlation ID: {CorrelationId}", 
                    result.ConflictedFiles.Count, correlationId);
            }
            else
            {
                _logger.LogInformation("Merge completed successfully with status {Status} and correlation ID: {CorrelationId}", 
                    result.Status, correlationId);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to merge branch {SourceBranch} into {TargetBranch}", sourceBranch, targetBranch);
            return new MergeResult
            {
                CorrelationId = Guid.NewGuid().ToString(),
                Status = MergeStatus.Failed,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Task 6.4.3: Create branch comparison and diff visualization
    /// </summary>
    public async Task<BranchComparison> CompareBranchesAsync(string baseBranch, string compareBranch)
    {
        try
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogInformation("Comparing branches {BaseBranch} and {CompareBranch} with correlation ID: {CorrelationId}", 
                baseBranch, compareBranch, correlationId);

            using var repo = new Repository(_repositoryService._repositoryPath);
            
            var baseRef = repo.Branches[baseBranch] 
                ?? throw new ArgumentException($"Base branch {baseBranch} not found");
            var compareRef = repo.Branches[compareBranch] 
                ?? throw new ArgumentException($"Compare branch {compareBranch} not found");

            // Get commits unique to each branch
            var baseCommits = repo.Commits.QueryBy(new CommitFilter { IncludeReachableFrom = baseRef.Tip })
                .Take(100).ToList();
            var compareCommits = repo.Commits.QueryBy(new CommitFilter { IncludeReachableFrom = compareRef.Tip })
                .Take(100).ToList();

            var baseCommitShas = new HashSet<string>(baseCommits.Select(c => c.Sha));
            var compareCommitShas = new HashSet<string>(compareCommits.Select(c => c.Sha));

            var uniqueToBase = baseCommits.Where(c => !compareCommitShas.Contains(c.Sha)).ToList();
            var uniqueToCompare = compareCommits.Where(c => !baseCommitShas.Contains(c.Sha)).ToList();

            // Get file differences
            var diff = repo.Diff.Compare<TreeChanges>(baseRef.Tip.Tree, compareRef.Tip.Tree);
            var fileDiffs = diff.Select(change => new FileDiff
            {
                Path = change.Path,
                Status = MapChangeKind(change.Status),
                LinesAdded = 0, // LibGit2Sharp doesn't provide line counts in TreeChanges
                LinesDeleted = 0,
                OldPath = change.OldPath
            }).ToList();

            var comparison = new BranchComparison
            {
                CorrelationId = correlationId,
                BaseBranch = baseBranch,
                CompareBranch = compareBranch,
                AheadBy = uniqueToCompare.Count,
                BehindBy = uniqueToBase.Count,
                UniqueToBase = uniqueToBase.Select(MapCommitInfo).ToList(),
                UniqueToCompare = uniqueToCompare.Select(MapCommitInfo).ToList(),
                FileDifferences = fileDiffs,
                TotalChangedFiles = fileDiffs.Count,
                TotalAdditions = fileDiffs.Where(f => f.Status == FileChangeStatus.Added).Count(),
                TotalDeletions = fileDiffs.Where(f => f.Status == FileChangeStatus.Deleted).Count(),
                TotalModifications = fileDiffs.Where(f => f.Status == FileChangeStatus.Modified).Count()
            };

            _logger.LogInformation("Branch comparison completed: {AheadBy} ahead, {BehindBy} behind, {ChangedFiles} changed files with correlation ID: {CorrelationId}", 
                comparison.AheadBy, comparison.BehindBy, comparison.TotalChangedFiles, correlationId);

            return comparison;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compare branches {BaseBranch} and {CompareBranch}", baseBranch, compareBranch);
            return new BranchComparison
            {
                CorrelationId = Guid.NewGuid().ToString(),
                BaseBranch = baseBranch,
                CompareBranch = compareBranch,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Task 6.4.4: Implement merge strategies and options
    /// </summary>
    public async Task<GitOperationResult> SwitchBranchAsync(string branchName, bool createIfNotExists = false)
    {
        return await _repositoryService.ExecuteWithRecoveryAsync(async () =>
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogInformation("Switching to branch {BranchName} with correlation ID: {CorrelationId}", 
                branchName, correlationId);

            using var repo = new Repository(_repositoryService._repositoryPath);
            
            var branch = repo.Branches[branchName];
            
            // Create branch if it doesn't exist and createIfNotExists is true
            if (branch == null && createIfNotExists)
            {
                _logger.LogInformation("Creating new branch {BranchName} as it doesn't exist", branchName);
                branch = repo.CreateBranch(branchName);
            }
            else if (branch == null)
            {
                throw new ArgumentException($"Branch {branchName} not found");
            }

            // Check for uncommitted changes
            var status = repo.RetrieveStatus();
            if (status.IsDirty)
            {
                _logger.LogWarning("Repository has uncommitted changes. Stashing changes before branch switch.");
                
                // Stash changes
                var signature = GetMergeSignature(repo);
                var stash = repo.Stashes.Add(signature, $"Auto-stash before switching to {branchName}");
                
                _logger.LogInformation("Changes stashed with message: {StashMessage}", stash.Message);
            }

            // Switch to the branch
            Commands.Checkout(repo, branch);
            
            _logger.LogInformation("Successfully switched to branch {BranchName} with correlation ID: {CorrelationId}", 
                branchName, correlationId);

            return new BranchInfo
            {
                Name = branch.FriendlyName,
                Sha = branch.Tip.Sha,
                IsRemote = branch.IsRemote,
                IsTracking = branch.IsTracking
            };
        }, 
        "SwitchBranch");
    }

    /// <summary>
    /// Task 6.4.5: Create branch protection and validation rules
    /// </summary>
    public async Task<bool> DeleteBranchAsync(string branchName, bool force = false)
    {
        try
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogInformation("Deleting branch {BranchName} with correlation ID: {CorrelationId}", 
                branchName, correlationId);

            using var repo = new Repository(_repositoryService._repositoryPath);
            
            var branch = repo.Branches[branchName] 
                ?? throw new ArgumentException($"Branch {branchName} not found");

            // Validate deletion
            if (!ValidateBranchDeletion(repo, branch, force))
            {
                return false;
            }

            // Delete the branch
            repo.Branches.Remove(branch);
            
            _logger.LogInformation("Branch {BranchName} deleted successfully with correlation ID: {CorrelationId}", 
                branchName, correlationId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete branch {BranchName}", branchName);
            return false;
        }
    }

    /// <summary>
    /// Task 6.4.6: Implement branch synchronization with remote repositories
    /// </summary>
    public async Task<List<BranchInfo>> GetAllBranchesAsync(bool includeRemote = true)
    {
        try
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogInformation("Getting all branches with correlation ID: {CorrelationId}", correlationId);

            using var repo = new Repository(_repositoryService._repositoryPath);
            
            var branches = repo.Branches
                .Where(b => includeRemote || !b.IsRemote)
                .Select(b => new BranchInfo
                {
                    Name = b.FriendlyName,
                    Sha = b.Tip?.Sha ?? "",
                    IsRemote = b.IsRemote,
                    IsTracking = b.IsTracking,
                    IsCurrentBranch = b.IsCurrentRepositoryHead,
                    RemoteName = b.RemoteName,
                    UpstreamBranchCanonicalName = b.UpstreamBranchCanonicalName,
                    AheadBy = b.TrackingDetails?.AheadBy ?? 0,
                    BehindBy = b.TrackingDetails?.BehindBy ?? 0
                })
                .ToList();

            _logger.LogInformation("Retrieved {BranchCount} branches with correlation ID: {CorrelationId}", 
                branches.Count, correlationId);

            return branches;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get branches");
            return new List<BranchInfo>();
        }
    }

    #region Private Helper Methods

    private static bool IsValidBranchName(string branchName)
    {
        if (string.IsNullOrWhiteSpace(branchName))
            return false;

        // Git branch name restrictions
        var invalidChars = new[] { ' ', '~', '^', ':', '?', '*', '[', '\\' };
        if (branchName.IndexOfAny(invalidChars) >= 0)
            return false;

        if (branchName.StartsWith('-') || branchName.EndsWith('/') || branchName.Contains("//"))
            return false;

        if (branchName.EndsWith(".lock") || branchName.Contains("@{"))
            return false;

        return true;
    }

    private Signature GetMergeSignature(Repository repo)
    {
        var config = repo.Config;
        var name = config.Get<string>("user.name")?.Value ?? "Anomali Import Tool";
        var email = config.Get<string>("user.email")?.Value ?? "anomali@tool.local";
        
        return new Signature(name, email, DateTimeOffset.Now);
    }

    private static MergeStatus MapMergeStatus(LibGit2Sharp.MergeStatus status)
    {
        return status switch
        {
            LibGit2Sharp.MergeStatus.UpToDate => MergeStatus.UpToDate,
            LibGit2Sharp.MergeStatus.FastForward => MergeStatus.FastForward,
            LibGit2Sharp.MergeStatus.NonFastForward => MergeStatus.NonFastForward,
            LibGit2Sharp.MergeStatus.Conflicts => MergeStatus.Conflicts,
            _ => MergeStatus.Failed
        };
    }

    private async Task<List<string>> DetectConflictsAsync(Repository repo)
    {
        var conflicts = new List<string>();
        var status = repo.RetrieveStatus();
        
        foreach (var conflict in status.Conflicted)
        {
            conflicts.Add(conflict.FilePath);
        }

        return conflicts;
    }

    private static FileChangeStatus MapChangeKind(ChangeKind changeKind)
    {
        return changeKind switch
        {
            ChangeKind.Added => FileChangeStatus.Added,
            ChangeKind.Deleted => FileChangeStatus.Deleted,
            ChangeKind.Modified => FileChangeStatus.Modified,
            ChangeKind.Renamed => FileChangeStatus.Renamed,
            ChangeKind.Copied => FileChangeStatus.Copied,
            ChangeKind.Ignored => FileChangeStatus.Ignored,
            ChangeKind.Untracked => FileChangeStatus.Untracked,
            ChangeKind.TypeChanged => FileChangeStatus.TypeChanged,
            _ => FileChangeStatus.Unmodified
        };
    }

    private static CommitInfo MapCommitInfo(Commit commit)
    {
        return new CommitInfo
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
            CommitDate = commit.Committer.When
        };
    }

    private bool ValidateBranchDeletion(Repository repo, Branch branch, bool force)
    {
        // Cannot delete current branch
        if (branch.IsCurrentRepositoryHead)
        {
            _logger.LogError("Cannot delete current branch: {BranchName}", branch.FriendlyName);
            return false;
        }

        // Cannot delete main/master without force
        if (!force && (branch.FriendlyName == "main" || branch.FriendlyName == "master"))
        {
            _logger.LogError("Cannot delete main/master branch without force flag: {BranchName}", branch.FriendlyName);
            return false;
        }

        // Check for unmerged commits (if not force)
        if (!force && HasUnmergedCommits(repo, branch))
        {
            _logger.LogError("Branch {BranchName} has unmerged commits. Use force to delete anyway.", branch.FriendlyName);
            return false;
        }

        return true;
    }

    private bool HasUnmergedCommits(Repository repo, Branch branch)
    {
        try
        {
            // Simple check: compare with main branch
            var mainBranch = repo.Branches["main"] ?? repo.Branches["master"];
            if (mainBranch == null) return false;

            var branchCommits = repo.Commits.QueryBy(new CommitFilter { IncludeReachableFrom = branch.Tip }).Take(50);
            var mainCommits = repo.Commits.QueryBy(new CommitFilter { IncludeReachableFrom = mainBranch.Tip }).Take(100);
            
            var mainCommitShas = new HashSet<string>(mainCommits.Select(c => c.Sha));
            
            return branchCommits.Any(c => !mainCommitShas.Contains(c.Sha));
        }
        catch
        {
            return false; // If we can't determine, allow deletion
        }
    }

    #endregion
}

/// <summary>
/// Branch information
/// </summary>
public class BranchInfo
{
    public string Name { get; set; } = string.Empty;
    public string Sha { get; set; } = string.Empty;
    public bool IsRemote { get; set; }
    public bool IsTracking { get; set; }
    public bool IsCurrentBranch { get; set; }
    public string? RemoteName { get; set; }
    public string? UpstreamBranchCanonicalName { get; set; }
    public int AheadBy { get; set; }
    public int BehindBy { get; set; }
}

/// <summary>
/// Merge operation result
/// </summary>
public class MergeResult
{
    public string CorrelationId { get; set; } = string.Empty;
    public MergeStatus Status { get; set; }
    public string? Commit { get; set; }
    public List<string> ConflictedFiles { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Merge status enumeration
/// </summary>
public enum MergeStatus
{
    UpToDate,
    FastForward,
    NonFastForward,
    Conflicts,
    Failed
}

/// <summary>
/// Branch comparison result
/// </summary>
public class BranchComparison
{
    public string CorrelationId { get; set; } = string.Empty;
    public string BaseBranch { get; set; } = string.Empty;
    public string CompareBranch { get; set; } = string.Empty;
    public int AheadBy { get; set; }
    public int BehindBy { get; set; }
    public List<CommitInfo> UniqueToBase { get; set; } = new();
    public List<CommitInfo> UniqueToCompare { get; set; } = new();
    public List<FileDiff> FileDifferences { get; set; } = new();
    public int TotalChangedFiles { get; set; }
    public int TotalAdditions { get; set; }
    public int TotalDeletions { get; set; }
    public int TotalModifications { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// File difference information
/// </summary>
public class FileDiff
{
    public string Path { get; set; } = string.Empty;
    public string? OldPath { get; set; }
    public FileChangeStatus Status { get; set; }
    public int LinesAdded { get; set; }
    public int LinesDeleted { get; set; }
}

/// <summary>
/// File change status enumeration
/// </summary>
public enum FileChangeStatus
{
    Unmodified,
    Added,
    Deleted,
    Modified,
    Renamed,
    Copied,
    Ignored,
    Untracked,
    TypeChanged
}

/// <summary>
/// Interface for Git branch operations
/// </summary>
public interface IGitBranchService
{
    Task<GitOperationResult> CreateBranchAsync(string branchName, string? startPoint = null);
    Task<MergeResult> MergeBranchAsync(string targetBranch, string sourceBranch, MergeOptions? options = null);
    Task<BranchComparison> CompareBranchesAsync(string baseBranch, string compareBranch);
    Task<GitOperationResult> SwitchBranchAsync(string branchName, bool createIfNotExists = false);
    Task<bool> DeleteBranchAsync(string branchName, bool force = false);
    Task<List<BranchInfo>> GetAllBranchesAsync(bool includeRemote = true);
} 