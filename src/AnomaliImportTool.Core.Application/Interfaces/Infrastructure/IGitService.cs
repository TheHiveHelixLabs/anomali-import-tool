using AnomaliImportTool.Core.Domain.ValueObjects;

namespace AnomaliImportTool.Core.Application.Interfaces.Infrastructure;

/// <summary>
/// Comprehensive Git service interface for all Git operations
/// Supports authentication, repository management, and advanced Git workflows
/// </summary>
public interface IGitService
{
    // Authentication Methods
    Task<bool> AuthenticateWithSshKeyAsync(string keyPath, string passphrase = "");
    Task<bool> AuthenticateWithTokenAsync(string username, string token);
    Task<bool> StoreCredentialsAsync(string remoteName, string username, string password);
    
    // Repository Management
    Task<bool> InitializeRepositoryAsync(string path, bool bare = false);
    Task<bool> AddRemoteAsync(string name, string url);
    Task<GitStatusInfo> GetRepositoryStatusAsync();
    
    // Error Handling and Recovery
    Task<GitOperationResult> ExecuteWithRecoveryAsync<T>(
        Func<Task<T>> operation, 
        string operationName,
        Func<Task<T>>? recoveryOperation = null);
}

/// <summary>
/// Git operation result with correlation tracking and error handling
/// </summary>
public class GitOperationResult
{
    public bool IsSuccess { get; init; }
    public object? Result { get; init; }
    public string CorrelationId { get; init; } = string.Empty;
    public TimeSpan Duration { get; init; }
    public Exception? Exception { get; init; }
    public bool WasRecovered { get; init; }

    public static GitOperationResult Success(object? result, string correlationId, TimeSpan duration)
        => new() { IsSuccess = true, Result = result, CorrelationId = correlationId, Duration = duration };

    public static GitOperationResult SuccessWithRecovery(object? result, string correlationId, TimeSpan duration, Exception originalException)
        => new() { IsSuccess = true, Result = result, CorrelationId = correlationId, Duration = duration, WasRecovered = true, Exception = originalException };

    public static GitOperationResult Failure(Exception exception, string correlationId, TimeSpan duration)
        => new() { IsSuccess = false, Exception = exception, CorrelationId = correlationId, Duration = duration };
}

/// <summary>
/// Comprehensive Git repository status information
/// </summary>
public class GitStatusInfo
{
    public string CorrelationId { get; init; } = string.Empty;
    public string CurrentBranch { get; init; } = string.Empty;
    public bool IsDetachedHead { get; init; }
    public bool IsDirty { get; init; }
    public List<string> AddedFiles { get; init; } = new();
    public List<string> ModifiedFiles { get; init; } = new();
    public List<string> RemovedFiles { get; init; } = new();
    public List<string> UntrackedFiles { get; init; } = new();
    public List<string> ConflictedFiles { get; init; } = new();
    public int AheadBy { get; init; }
    public int BehindBy { get; init; }
    public string? LastCommitSha { get; init; }
    public string? LastCommitMessage { get; init; }
    public string? LastCommitAuthor { get; init; }
    public DateTimeOffset? LastCommitDate { get; init; }

    public static GitStatusInfo Empty() => new();
} 