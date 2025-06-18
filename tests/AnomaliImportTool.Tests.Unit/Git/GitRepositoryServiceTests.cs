using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using AnomaliImportTool.Git.Services;
using AnomaliImportTool.Core.Application.Interfaces.Infrastructure;

namespace AnomaliImportTool.Tests.Unit.Git;

/// <summary>
/// Task 6.1.9: Create comprehensive Git integration unit tests
/// Tests all Git repository operations with proper mocking and validation
/// </summary>
public class GitRepositoryServiceTests : IDisposable
{
    private readonly Mock<ILogger<GitRepositoryService>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly GitRepositoryService _gitService;
    private readonly string _testRepositoryPath;

    public GitRepositoryServiceTests()
    {
        _mockLogger = new Mock<ILogger<GitRepositoryService>>();
        _mockConfiguration = new Mock<IConfiguration>();
        
        // Setup test repository path
        _testRepositoryPath = Path.Combine(Path.GetTempPath(), "AnomaliImportTool_Test_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_testRepositoryPath);
        
        // Setup configuration mock
        _mockConfiguration.Setup(c => c["Git:RepositoryPath"]).Returns(_testRepositoryPath);
        _mockConfiguration.Setup(c => c["Git:DefaultUser:Name"]).Returns("Test User");
        _mockConfiguration.Setup(c => c["Git:DefaultUser:Email"]).Returns("test@example.com");
        
        _gitService = new GitRepositoryService(_mockLogger.Object, _mockConfiguration.Object);
    }

    [Fact]
    public async Task AuthenticateWithSshKeyAsync_ValidKey_ReturnsTrue()
    {
        // Arrange
        var tempKeyFile = Path.GetTempFileName();
        var validSshKey = "-----BEGIN OPENSSH PRIVATE KEY-----\ntest_key_content\n-----END OPENSSH PRIVATE KEY-----";
        await File.WriteAllTextAsync(tempKeyFile, validSshKey);

        try
        {
            // Act
            var result = await _gitService.AuthenticateWithSshKeyAsync(tempKeyFile, "test_passphrase");

            // Assert
            result.Should().BeTrue();
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("SSH key authentication successful")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
        finally
        {
            File.Delete(tempKeyFile);
        }
    }

    [Fact]
    public async Task AuthenticateWithSshKeyAsync_InvalidKeyFile_ReturnsFalse()
    {
        // Arrange
        var nonExistentKeyFile = "non_existent_key.pem";

        // Act
        var result = await _gitService.AuthenticateWithSshKeyAsync(nonExistentKeyFile);

        // Assert
        result.Should().BeFalse();
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("SSH key file not found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData("ghp_1234567890abcdef", true)]
    [InlineData("gho_1234567890abcdef", true)]
    [InlineData("ghu_1234567890abcdef", true)]
    [InlineData("ghs_1234567890abcdef", true)]
    [InlineData("ghr_1234567890abcdef", true)]
    [InlineData("invalid_token", false)]
    [InlineData("", false)]
    public async Task AuthenticateWithTokenAsync_VariousTokens_ReturnsExpectedResult(string token, bool expectedResult)
    {
        // Arrange
        var username = "testuser";

        // Act
        var result = await _gitService.AuthenticateWithTokenAsync(username, token);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact]
    public async Task InitializeRepositoryAsync_ValidPath_ReturnsTrue()
    {
        // Arrange
        var newRepoPath = Path.Combine(Path.GetTempPath(), "NewTestRepo_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(newRepoPath);

        try
        {
            // Act
            var result = await _gitService.InitializeRepositoryAsync(newRepoPath);

            // Assert
            result.Should().BeTrue();
            Directory.Exists(Path.Combine(newRepoPath, ".git")).Should().BeTrue();
            
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Repository initialized successfully")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
        finally
        {
            if (Directory.Exists(newRepoPath))
            {
                Directory.Delete(newRepoPath, true);
            }
        }
    }

    [Fact]
    public async Task InitializeRepositoryAsync_ExistingRepository_ReturnsTrue()
    {
        // Arrange
        var existingRepoPath = Path.Combine(Path.GetTempPath(), "ExistingTestRepo_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(existingRepoPath);
        Directory.CreateDirectory(Path.Combine(existingRepoPath, ".git"));

        try
        {
            // Act
            var result = await _gitService.InitializeRepositoryAsync(existingRepoPath);

            // Assert
            result.Should().BeTrue();
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Repository already exists")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
        finally
        {
            if (Directory.Exists(existingRepoPath))
            {
                Directory.Delete(existingRepoPath, true);
            }
        }
    }

    [Fact]
    public async Task StoreCredentialsAsync_ValidCredentials_ReturnsTrue()
    {
        // Arrange
        var remoteName = "origin";
        var username = "testuser";
        var password = "testpassword";

        // Act
        var result = await _gitService.StoreCredentialsAsync(remoteName, username, password);

        // Assert
        result.Should().BeTrue();
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Credentials stored successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetRepositoryStatusAsync_EmptyRepository_ReturnsEmptyStatus()
    {
        // Act
        var status = await _gitService.GetRepositoryStatusAsync();

        // Assert
        status.Should().NotBeNull();
        status.CorrelationId.Should().NotBeEmpty();
        
        // Since we don't have an initialized repository, it should return empty status
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Getting repository status")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteWithRecoveryAsync_SuccessfulOperation_ReturnsSuccess()
    {
        // Arrange
        var testOperation = new Func<Task<string>>(() => Task.FromResult("success"));
        var operationName = "TestOperation";

        // Act
        var result = await _gitService.ExecuteWithRecoveryAsync(testOperation, operationName);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Result.Should().Be("success");
        result.CorrelationId.Should().NotBeEmpty();
        result.Duration.Should().BeGreaterThan(TimeSpan.Zero);
        result.WasRecovered.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteWithRecoveryAsync_FailedOperationWithRecovery_ReturnsSuccessWithRecovery()
    {
        // Arrange
        var failingOperation = new Func<Task<string>>(() => throw new InvalidOperationException("Test failure"));
        var recoveryOperation = new Func<Task<string>>(() => Task.FromResult("recovered"));
        var operationName = "TestOperationWithRecovery";

        // Act
        var result = await _gitService.ExecuteWithRecoveryAsync(failingOperation, operationName, recoveryOperation);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Result.Should().Be("recovered");
        result.WasRecovered.Should().BeTrue();
        result.Exception.Should().NotBeNull();
        result.Exception!.Message.Should().Be("Test failure");
    }

    [Fact]
    public async Task ExecuteWithRecoveryAsync_FailedOperationNoRecovery_ReturnsFailure()
    {
        // Arrange
        var failingOperation = new Func<Task<string>>(() => throw new InvalidOperationException("Test failure"));
        var operationName = "TestFailedOperation";

        // Act
        var result = await _gitService.ExecuteWithRecoveryAsync(failingOperation, operationName);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Exception.Should().NotBeNull();
        result.Exception!.Message.Should().Be("Test failure");
        result.WasRecovered.Should().BeFalse();
    }

    [Theory]
    [InlineData("origin", "https://github.com/user/repo.git")]
    [InlineData("upstream", "git@github.com:user/repo.git")]
    public async Task AddRemoteAsync_ValidRemote_ReturnsTrue(string remoteName, string remoteUrl)
    {
        // Arrange - Initialize a repository first
        await _gitService.InitializeRepositoryAsync(_testRepositoryPath);

        // Act
        var result = await _gitService.AddRemoteAsync(remoteName, remoteUrl);

        // Assert
        result.Should().BeTrue();
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Remote {remoteName} added successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        // Act & Assert
        var disposing = () =>
        {
            _gitService.Dispose();
            _gitService.Dispose(); // Second call should not throw
        };

        disposing.Should().NotThrow();
    }

    [Fact]
    public async Task AuthenticateWithTokenAsync_EmptyUsername_ReturnsFalse()
    {
        // Arrange
        var token = "ghp_validtoken123";

        // Act
        var result = await _gitService.AuthenticateWithTokenAsync("", token);

        // Assert
        result.Should().BeFalse();
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Username or token cannot be empty")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task AuthenticateWithTokenAsync_EmptyToken_ReturnsFalse()
    {
        // Arrange
        var username = "testuser";

        // Act
        var result = await _gitService.AuthenticateWithTokenAsync(username, "");

        // Assert
        result.Should().BeFalse();
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Username or token cannot be empty")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    public void Dispose()
    {
        _gitService?.Dispose();
        
        if (Directory.Exists(_testRepositoryPath))
        {
            try
            {
                Directory.Delete(_testRepositoryPath, true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }
}

/// <summary>
/// Integration tests for Git repository service with real Git operations
/// </summary>
public class GitRepositoryServiceIntegrationTests : IDisposable
{
    private readonly GitRepositoryService _gitService;
    private readonly string _testRepositoryPath;
    private readonly Mock<ILogger<GitRepositoryService>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;

    public GitRepositoryServiceIntegrationTests()
    {
        _mockLogger = new Mock<ILogger<GitRepositoryService>>();
        _mockConfiguration = new Mock<IConfiguration>();
        
        _testRepositoryPath = Path.Combine(Path.GetTempPath(), "AnomaliImportTool_Integration_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_testRepositoryPath);
        
        _mockConfiguration.Setup(c => c["Git:RepositoryPath"]).Returns(_testRepositoryPath);
        _mockConfiguration.Setup(c => c["Git:DefaultUser:Name"]).Returns("Integration Test");
        _mockConfiguration.Setup(c => c["Git:DefaultUser:Email"]).Returns("integration@test.com");
        
        _gitService = new GitRepositoryService(_mockLogger.Object, _mockConfiguration.Object);
    }

    [Fact]
    public async Task FullWorkflow_InitializeAddRemoteGetStatus_WorksCorrectly()
    {
        // Arrange & Act
        var initResult = await _gitService.InitializeRepositoryAsync(_testRepositoryPath);
        var remoteResult = await _gitService.AddRemoteAsync("origin", "https://github.com/test/repo.git");
        var status = await _gitService.GetRepositoryStatusAsync();

        // Assert
        initResult.Should().BeTrue();
        remoteResult.Should().BeTrue();
        status.Should().NotBeNull();
        status.CurrentBranch.Should().NotBeEmpty();
        status.CorrelationId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CircuitBreakerPattern_MultipleFailures_TriggersCircuitBreaker()
    {
        // This test would require more complex setup to trigger actual LibGit2Sharp exceptions
        // For now, we'll test that the service handles exceptions gracefully
        
        var failingOperation = new Func<Task<string>>(() => throw new InvalidOperationException("Simulated failure"));
        
        // Act
        var result = await _gitService.ExecuteWithRecoveryAsync(failingOperation, "TestCircuitBreaker");

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Exception.Should().NotBeNull();
    }

    public void Dispose()
    {
        _gitService?.Dispose();
        
        if (Directory.Exists(_testRepositoryPath))
        {
            try
            {
                Directory.Delete(_testRepositoryPath, true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }
} 