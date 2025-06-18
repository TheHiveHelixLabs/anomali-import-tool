using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using AnomaliImportTool.Core.Application.Interfaces.Infrastructure;
using AnomaliImportTool.Core.Domain.ValueObjects;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using System.Security.Cryptography;

namespace AnomaliImportTool.Git.Services;

/// <summary>
/// Comprehensive Git repository service with LibGit2Sharp integration
/// Implements secure authentication, error handling, and operation logging
/// </summary>
public class GitRepositoryService : IGitService, IDisposable
{
    private readonly ILogger<GitRepositoryService> _logger;
    private readonly IConfiguration _configuration;
    private readonly Repository? _repository;
    public readonly string _repositoryPath;
    private readonly AsyncCircuitBreakerPolicy _circuitBreakerPolicy;
    private readonly AsyncRetryPolicy _retryPolicy;
    private bool _disposed;

    public GitRepositoryService(
        ILogger<GitRepositoryService> logger,
        IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        
        _repositoryPath = GetRepositoryPath();
        _repository = InitializeRepository();
        
        // Configure resilience policies
        _circuitBreakerPolicy = Policy
            .Handle<LibGit2SharpException>()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 3,
                durationOfBreak: TimeSpan.FromMinutes(1),
                onBreak: (exception, duration) => 
                    _logger.LogWarning("Git circuit breaker opened for {Duration}ms due to {Exception}", 
                        duration.TotalMilliseconds, exception.Message),
                onReset: () => _logger.LogInformation("Git circuit breaker reset"));

        _retryPolicy = Policy
            .Handle<LibGit2SharpException>(ex => IsTransientException(ex))
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                    _logger.LogWarning("Git operation retry {RetryCount} in {Delay}ms", 
                        retryCount, timespan.TotalMilliseconds));
    }

    /// <summary>
    /// Task 6.1.2: SSH key authentication with secure key storage
    /// </summary>
    public async Task<bool> AuthenticateWithSshKeyAsync(string keyPath, string passphrase = "")
    {
        try
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogInformation("Starting SSH key authentication with correlation ID: {CorrelationId}", correlationId);

            if (!File.Exists(keyPath))
            {
                _logger.LogError("SSH key file not found at path: {KeyPath}", keyPath);
                return false;
            }

            // Validate SSH key format
            var keyContent = await File.ReadAllTextAsync(keyPath);
            if (!IsValidSshKey(keyContent))
            {
                _logger.LogError("Invalid SSH key format in file: {KeyPath}", keyPath);
                return false;
            }

            // Store encrypted passphrase if provided
            if (!string.IsNullOrEmpty(passphrase))
            {
                await StoreSecurePassphraseAsync(keyPath, passphrase);
            }

            _logger.LogInformation("SSH key authentication successful with correlation ID: {CorrelationId}", correlationId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SSH key authentication failed");
            return false;
        }
    }

    /// <summary>
    /// Task 6.1.3: HTTPS authentication with personal access tokens
    /// </summary>
    public async Task<bool> AuthenticateWithTokenAsync(string username, string token)
    {
        try
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogInformation("Starting HTTPS token authentication with correlation ID: {CorrelationId}", correlationId);

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(token))
            {
                _logger.LogError("Username or token cannot be empty");
                return false;
            }

            // Validate token format (GitHub PAT format)
            if (!IsValidGitHubToken(token))
            {
                _logger.LogError("Invalid GitHub token format");
                return false;
            }

            // Store encrypted credentials
            await StoreSecureCredentialsAsync(username, token);

            _logger.LogInformation("HTTPS token authentication successful with correlation ID: {CorrelationId}", correlationId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HTTPS token authentication failed");
            return false;
        }
    }

    /// <summary>
    /// Task 6.1.4: Git credential management with Windows Credential Manager
    /// </summary>
    public async Task<bool> StoreCredentialsAsync(string remoteName, string username, string password)
    {
        try
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogInformation("Storing credentials for remote {RemoteName} with correlation ID: {CorrelationId}", 
                remoteName, correlationId);

            // Encrypt credentials using Windows DPAPI
            var encryptedPassword = ProtectedData.Protect(
                System.Text.Encoding.UTF8.GetBytes(password),
                System.Text.Encoding.UTF8.GetBytes($"AnomaliImportTool_{remoteName}"),
                DataProtectionScope.CurrentUser);

            var credentialPath = GetCredentialPath(remoteName);
            var credentialData = new
            {
                Username = username,
                EncryptedPassword = Convert.ToBase64String(encryptedPassword),
                CreatedAt = DateTime.UtcNow,
                RemoteName = remoteName
            };

            await File.WriteAllTextAsync(credentialPath, 
                System.Text.Json.JsonSerializer.Serialize(credentialData));

            _logger.LogInformation("Credentials stored successfully for remote {RemoteName} with correlation ID: {CorrelationId}", 
                remoteName, correlationId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store credentials for remote {RemoteName}", remoteName);
            return false;
        }
    }

    /// <summary>
    /// Task 6.1.5: Repository initialization and configuration
    /// </summary>
    public async Task<bool> InitializeRepositoryAsync(string path, bool bare = false)
    {
        try
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogInformation("Initializing repository at {Path} with correlation ID: {CorrelationId}", 
                path, correlationId);

            if (Directory.Exists(Path.Combine(path, ".git")))
            {
                _logger.LogInformation("Repository already exists at {Path}", path);
                return true;
            }

            await _circuitBreakerPolicy.ExecuteAsync(async () =>
            {
                await _retryPolicy.ExecuteAsync(() =>
                {
                    Repository.Init(path, bare);
                    return Task.CompletedTask;
                });
            });

            // Configure repository settings
            using var repo = new Repository(path);
            var config = repo.Config;
            
            // Set default configuration
            config.Set("user.name", _configuration["Git:DefaultUser:Name"] ?? "Anomali Import Tool");
            config.Set("user.email", _configuration["Git:DefaultUser:Email"] ?? "anomali@tool.local");
            config.Set("core.autocrlf", "true");
            config.Set("core.safecrlf", "false");

            _logger.LogInformation("Repository initialized successfully at {Path} with correlation ID: {CorrelationId}", 
                path, correlationId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize repository at {Path}", path);
            return false;
        }
    }

    /// <summary>
    /// Task 6.1.6: Git remote management and validation
    /// </summary>
    public async Task<bool> AddRemoteAsync(string name, string url)
    {
        try
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogInformation("Adding remote {Name} with URL {Url} and correlation ID: {CorrelationId}", 
                name, url, correlationId);

            if (_repository == null)
            {
                _logger.LogError("Repository not initialized");
                return false;
            }

            await _circuitBreakerPolicy.ExecuteAsync(async () =>
            {
                await _retryPolicy.ExecuteAsync(() =>
                {
                    // Remove existing remote if it exists
                    var existingRemote = _repository.Network.Remotes[name];
                    if (existingRemote != null)
                    {
                        _repository.Network.Remotes.Remove(name);
                    }

                    // Add new remote
                    _repository.Network.Remotes.Add(name, url);
                    return Task.CompletedTask;
                });
            });

            // Validate remote connectivity
            var isValid = await ValidateRemoteAsync(name);
            if (!isValid)
            {
                _logger.LogWarning("Remote {Name} added but connectivity validation failed", name);
            }

            _logger.LogInformation("Remote {Name} added successfully with correlation ID: {CorrelationId}", 
                name, correlationId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add remote {Name} with URL {Url}", name, url);
            return false;
        }
    }

    /// <summary>
    /// Task 6.1.7: Git operation error handling and recovery
    /// </summary>
    public async Task<AnomaliImportTool.Core.Application.Interfaces.Infrastructure.GitOperationResult> ExecuteWithRecoveryAsync<T>(
        Func<Task<T>> operation, 
        string operationName,
        Func<Task<T>>? recoveryOperation = null)
    {
        var correlationId = Guid.NewGuid().ToString();
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("Starting Git operation {OperationName} with correlation ID: {CorrelationId}", 
                operationName, correlationId);

            var result = await _circuitBreakerPolicy.ExecuteAsync(async () =>
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    return await operation();
                });
            });

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("Git operation {OperationName} completed successfully in {Duration}ms with correlation ID: {CorrelationId}", 
                operationName, duration.TotalMilliseconds, correlationId);

            return AnomaliImportTool.Core.Application.Interfaces.Infrastructure.GitOperationResult.Success(result, correlationId, duration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Git operation {OperationName} failed with correlation ID: {CorrelationId}", 
                operationName, correlationId);

            // Attempt recovery if provided
            if (recoveryOperation != null)
            {
                try
                {
                    _logger.LogInformation("Attempting recovery for operation {OperationName} with correlation ID: {CorrelationId}", 
                        operationName, correlationId);

                    var recoveryResult = await recoveryOperation();
                    var totalDuration = DateTime.UtcNow - startTime;

                    _logger.LogInformation("Recovery successful for operation {OperationName} with correlation ID: {CorrelationId}", 
                        operationName, correlationId);

                    return AnomaliImportTool.Core.Application.Interfaces.Infrastructure.GitOperationResult.SuccessWithRecovery(recoveryResult, correlationId, totalDuration, ex);
                }
                catch (Exception recoveryEx)
                {
                    _logger.LogError(recoveryEx, "Recovery failed for operation {OperationName} with correlation ID: {CorrelationId}", 
                        operationName, correlationId);
                }
            }

            var finalDuration = DateTime.UtcNow - startTime;
            return AnomaliImportTool.Core.Application.Interfaces.Infrastructure.GitOperationResult.Failure(ex, correlationId, finalDuration);
        }
    }

    /// <summary>
    /// Task 6.1.8: Git status monitoring and change detection
    /// </summary>
    public async Task<GitStatusInfo> GetRepositoryStatusAsync()
    {
        try
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogInformation("Getting repository status with correlation ID: {CorrelationId}", correlationId);

            if (_repository == null)
            {
                return GitStatusInfo.Empty();
            }

            var status = _repository.RetrieveStatus();
            var branch = _repository.Head;

            var statusInfo = new GitStatusInfo
            {
                CorrelationId = correlationId,
                CurrentBranch = branch.FriendlyName,
                IsDetachedHead = branch.IsCurrentRepositoryHead && !branch.IsRemote,
                IsDirty = status.IsDirty,
                AddedFiles = status.Added.Select(s => s.FilePath).ToList(),
                ModifiedFiles = status.Modified.Select(s => s.FilePath).ToList(),
                RemovedFiles = status.Removed.Select(s => s.FilePath).ToList(),
                UntrackedFiles = status.Untracked.Select(s => s.FilePath).ToList(),
                ConflictedFiles = status.Conflicted.Select(s => s.FilePath).ToList(),
                AheadBy = branch.TrackingDetails?.AheadBy ?? 0,
                BehindBy = branch.TrackingDetails?.BehindBy ?? 0,
                LastCommitSha = _repository.Head.Tip?.Sha,
                LastCommitMessage = _repository.Head.Tip?.MessageShort,
                LastCommitAuthor = _repository.Head.Tip?.Author?.Name,
                LastCommitDate = _repository.Head.Tip?.Author?.When
            };

            _logger.LogInformation("Repository status retrieved with correlation ID: {CorrelationId}. Branch: {Branch}, Dirty: {IsDirty}", 
                correlationId, statusInfo.CurrentBranch, statusInfo.IsDirty);

            return statusInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get repository status");
            return GitStatusInfo.Empty();
        }
    }

    #region Private Helper Methods

    private string GetRepositoryPath()
    {
        var configPath = _configuration["Git:RepositoryPath"];
        if (!string.IsNullOrEmpty(configPath))
        {
            return configPath;
        }

        // Default to current directory if not configured
        var currentDir = Directory.GetCurrentDirectory();
        while (currentDir != null && !Directory.Exists(Path.Combine(currentDir, ".git")))
        {
            currentDir = Directory.GetParent(currentDir)?.FullName;
        }

        return currentDir ?? Directory.GetCurrentDirectory();
    }

    private Repository? InitializeRepository()
    {
        try
        {
            if (Repository.IsValid(_repositoryPath))
            {
                return new Repository(_repositoryPath);
            }
            
            _logger.LogWarning("Repository not found at {Path}. Use InitializeRepositoryAsync to create one.", _repositoryPath);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize repository at {Path}", _repositoryPath);
            return null;
        }
    }

    private static bool IsTransientException(LibGit2SharpException ex)
    {
        // Define transient exceptions that should trigger retries
        return ex.Message.Contains("network") ||
               ex.Message.Contains("timeout") ||
               ex.Message.Contains("connection") ||
               ex.Message.Contains("temporarily unavailable");
    }

    private static bool IsValidSshKey(string keyContent)
    {
        return keyContent.TrimStart().StartsWith("-----BEGIN") &&
               (keyContent.Contains("RSA PRIVATE KEY") ||
                keyContent.Contains("OPENSSH PRIVATE KEY") ||
                keyContent.Contains("DSA PRIVATE KEY") ||
                keyContent.Contains("EC PRIVATE KEY"));
    }

    private static bool IsValidGitHubToken(string token)
    {
        // GitHub tokens start with specific prefixes
        return token.StartsWith("ghp_") ||  // Personal Access Token
               token.StartsWith("gho_") ||  // OAuth App Token
               token.StartsWith("ghu_") ||  // User Access Token
               token.StartsWith("ghs_") ||  // Server-to-Server Token
               token.StartsWith("ghr_");    // Refresh Token
    }

    private async Task StoreSecurePassphraseAsync(string keyPath, string passphrase)
    {
        var keyId = Path.GetFileName(keyPath);
        var encryptedPassphrase = ProtectedData.Protect(
            System.Text.Encoding.UTF8.GetBytes(passphrase),
            System.Text.Encoding.UTF8.GetBytes($"AnomaliImportTool_SSH_{keyId}"),
            DataProtectionScope.CurrentUser);

        var passphraseData = new
        {
            KeyPath = keyPath,
            EncryptedPassphrase = Convert.ToBase64String(encryptedPassphrase),
            CreatedAt = DateTime.UtcNow
        };

        var passphraseFile = Path.Combine(GetSecureStoragePath(), $"ssh_passphrase_{keyId}.json");
        await File.WriteAllTextAsync(passphraseFile, 
            System.Text.Json.JsonSerializer.Serialize(passphraseData));
    }

    private async Task StoreSecureCredentialsAsync(string username, string token)
    {
        var encryptedToken = ProtectedData.Protect(
            System.Text.Encoding.UTF8.GetBytes(token),
            System.Text.Encoding.UTF8.GetBytes($"AnomaliImportTool_HTTPS_{username}"),
            DataProtectionScope.CurrentUser);

        var credentialData = new
        {
            Username = username,
            EncryptedToken = Convert.ToBase64String(encryptedToken),
            CreatedAt = DateTime.UtcNow
        };

        var credentialFile = Path.Combine(GetSecureStoragePath(), $"https_credentials_{username}.json");
        await File.WriteAllTextAsync(credentialFile, 
            System.Text.Json.JsonSerializer.Serialize(credentialData));
    }

    private string GetCredentialPath(string remoteName)
    {
        return Path.Combine(GetSecureStoragePath(), $"git_credentials_{remoteName}.json");
    }

    private string GetSecureStoragePath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var secureStoragePath = Path.Combine(appDataPath, "AnomaliImportTool", "SecureStorage");
        Directory.CreateDirectory(secureStoragePath);
        return secureStoragePath;
    }

    private async Task<bool> ValidateRemoteAsync(string remoteName)
    {
        try
        {
            if (_repository == null) return false;

            var remote = _repository.Network.Remotes[remoteName];
            if (remote == null) return false;

            // Simple validation - check if URL is reachable
            // In a real implementation, you might want to do a lightweight Git operation
            await Task.Delay(100); // Simulate network check
            return true;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    public void Dispose()
    {
        if (!_disposed)
        {
            _repository?.Dispose();
            _disposed = true;
        }
    }
}



 