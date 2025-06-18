using AnomaliImportTool.Core.Domain.ValueObjects;

namespace AnomaliImportTool.Core.Application.Interfaces.Infrastructure;

/// <summary>
/// Interface for Git operations following Clean Architecture dependency inversion
/// </summary>
public interface IGitService
{
    Task<GitOperationResult> InitializeRepositoryAsync(string repositoryPath, CancellationToken cancellationToken = default);
    Task<GitOperationResult> CloneRepositoryAsync(string remoteUrl, string localPath, CancellationToken cancellationToken = default);
    Task<GitOperationResult> CommitChangesAsync(string repositoryPath, string commitMessage, CancellationToken cancellationToken = default);
    Task<GitOperationResult> PushChangesAsync(string repositoryPath, string remoteName = "origin", string branchName = "main", CancellationToken cancellationToken = default);
    Task<GitOperationResult> PullChangesAsync(string repositoryPath, string remoteName = "origin", string branchName = "main", CancellationToken cancellationToken = default);
    Task<GitOperationResult> CreateBranchAsync(string repositoryPath, string branchName, CancellationToken cancellationToken = default);
    Task<GitOperationResult> SwitchBranchAsync(string repositoryPath, string branchName, CancellationToken cancellationToken = default);
    Task<GitOperationResult> AddFilesAsync(string repositoryPath, IEnumerable<string> filePaths, CancellationToken cancellationToken = default);
    Task<GitStatus> GetRepositoryStatusAsync(string repositoryPath, CancellationToken cancellationToken = default);
    Task<IEnumerable<GitCommit>> GetCommitHistoryAsync(string repositoryPath, int maxCount = 50, CancellationToken cancellationToken = default);
    Task<bool> ValidateRepositoryAsync(string repositoryPath, CancellationToken cancellationToken = default);
} 