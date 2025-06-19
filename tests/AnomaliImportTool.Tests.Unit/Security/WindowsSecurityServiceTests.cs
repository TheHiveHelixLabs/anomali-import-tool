using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using AnomaliImportTool.Core.Models;
using AnomaliImportTool.Infrastructure.Security;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AnomaliImportTool.Tests.Unit.Security
{
    public class WindowsSecurityServiceTests : IDisposable
    {
        private readonly Mock<ILogger<WindowsSecurityService>> _loggerMock;
        private readonly string _testCredentialPath;
        private readonly WindowsSecurityService _service;

        public WindowsSecurityServiceTests()
        {
            _loggerMock = new Mock<ILogger<WindowsSecurityService>>();
            _testCredentialPath = Path.Combine(Path.GetTempPath(), $"test_credentials_{Guid.NewGuid()}.dat");
            _service = new WindowsSecurityService(_loggerMock.Object, _testCredentialPath);
        }

        [Fact]
        public async Task EncryptAsync_WithValidInput_ReturnsEncryptedString()
        {
            // Arrange
            var plainText = "test password";

            // Act
            var encrypted = await _service.EncryptAsync(plainText);

            // Assert
            Assert.NotNull(encrypted);
            Assert.NotEmpty(encrypted);
            Assert.NotEqual(plainText, encrypted);
            Assert.True(Convert.TryFromBase64String(encrypted, new Span<byte>(new byte[encrypted.Length]), out _));
        }

        [Fact]
        public async Task EncryptAsync_WithEmptyString_ReturnsEmptyString()
        {
            // Act
            var result = await _service.EncryptAsync(string.Empty);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public async Task EncryptAsync_WithNull_ReturnsEmptyString()
        {
            // Act
            var result = await _service.EncryptAsync(null);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public async Task DecryptAsync_WithValidEncryptedData_ReturnsOriginalText()
        {
            // Arrange
            var originalText = "secret api key";
            var encrypted = await _service.EncryptAsync(originalText);

            // Act
            var decrypted = await _service.DecryptAsync(encrypted);

            // Assert
            Assert.Equal(originalText, decrypted);
        }

        [Fact]
        public async Task DecryptAsync_WithEmptyString_ReturnsEmptyString()
        {
            // Act
            var result = await _service.DecryptAsync(string.Empty);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public async Task SaveApiConfigurationAsync_WithValidConfiguration_SavesSuccessfully()
        {
            // Arrange
            var config = new ApiConfiguration
            {
                BaseUrl = "https://api.test.com",
                Username = "testuser",
                ApiKey = "testapikey",
                TimeoutSeconds = 30
            };

            // Act
            await _service.SaveApiConfigurationAsync(config);

            // Assert
            Assert.True(File.Exists(_testCredentialPath));
        }

        [Fact]
        public async Task SaveApiConfigurationAsync_WithNullConfiguration_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _service.SaveApiConfigurationAsync(null));
        }

        [Fact]
        public async Task LoadApiConfigurationAsync_WithSavedConfiguration_ReturnsDecryptedConfiguration()
        {
            // Arrange
            var originalConfig = new ApiConfiguration
            {
                BaseUrl = "https://api.test.com",
                Username = "testuser",
                ApiKey = "testapikey",
                TimeoutSeconds = 60,
                MaxRetryAttempts = 5
            };

            await _service.SaveApiConfigurationAsync(originalConfig);

            // Act
            var loadedConfig = await _service.LoadApiConfigurationAsync();

            // Assert
            Assert.NotNull(loadedConfig);
            Assert.Equal(originalConfig.BaseUrl, loadedConfig.BaseUrl);
            Assert.Equal(originalConfig.Username, loadedConfig.Username);
            Assert.Equal(originalConfig.ApiKey, loadedConfig.ApiKey);
            Assert.Equal(originalConfig.TimeoutSeconds, loadedConfig.TimeoutSeconds);
            Assert.Equal(originalConfig.MaxRetryAttempts, loadedConfig.MaxRetryAttempts);
        }

        [Fact]
        public async Task LoadApiConfigurationAsync_WithNoSavedConfiguration_ReturnsNull()
        {
            // Act
            var result = await _service.LoadApiConfigurationAsync();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task ValidateStoredCredentialsAsync_WithValidCredentials_ReturnsTrue()
        {
            // Arrange
            var config = new ApiConfiguration
            {
                BaseUrl = "https://api.test.com",
                Username = "testuser",
                ApiKey = "testapikey"
            };
            await _service.SaveApiConfigurationAsync(config);

            // Act
            var isValid = await _service.ValidateStoredCredentialsAsync();

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public async Task ValidateStoredCredentialsAsync_WithNoCredentials_ReturnsFalse()
        {
            // Act
            var isValid = await _service.ValidateStoredCredentialsAsync();

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public async Task DeleteStoredCredentialsAsync_WithExistingCredentials_DeletesFile()
        {
            // Arrange
            var config = new ApiConfiguration
            {
                BaseUrl = "https://api.test.com",
                Username = "testuser",
                ApiKey = "testapikey"
            };
            await _service.SaveApiConfigurationAsync(config);
            Assert.True(File.Exists(_testCredentialPath));

            // Act
            await _service.DeleteStoredCredentialsAsync();

            // Assert
            Assert.False(File.Exists(_testCredentialPath));
        }

        [Fact]
        public async Task DeleteStoredCredentialsAsync_WithNoCredentials_DoesNotThrow()
        {
            // Act & Assert - Should not throw
            await _service.DeleteStoredCredentialsAsync();
        }

        [Fact]
        public async Task HashAsync_WithValidInput_ReturnsConsistentHash()
        {
            // Arrange
            var input = "test string";

            // Act
            var hash1 = await _service.HashAsync(input);
            var hash2 = await _service.HashAsync(input);

            // Assert
            Assert.NotNull(hash1);
            Assert.NotEmpty(hash1);
            Assert.Equal(hash1, hash2);
            Assert.NotEqual(input, hash1);
        }

        [Fact]
        public async Task HashAsync_WithDifferentInputs_ReturnsDifferentHashes()
        {
            // Arrange
            var input1 = "test string 1";
            var input2 = "test string 2";

            // Act
            var hash1 = await _service.HashAsync(input1);
            var hash2 = await _service.HashAsync(input2);

            // Assert
            Assert.NotEqual(hash1, hash2);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task HashAsync_WithInvalidInput_ThrowsArgumentException(string input)
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.HashAsync(input));
        }

        [Fact]
        public async Task ConcurrentAccess_MultipleOperations_HandledSafely()
        {
            // Arrange
            var config = new ApiConfiguration
            {
                BaseUrl = "https://api.test.com",
                Username = "testuser",
                ApiKey = "testapikey"
            };

            // Act - Perform multiple operations concurrently
            var tasks = new[]
            {
                _service.SaveApiConfigurationAsync(config),
                _service.LoadApiConfigurationAsync(),
                _service.ValidateStoredCredentialsAsync(),
                _service.HashAsync("test")
            };

            // Assert - Should not throw
            await Task.WhenAll(tasks);
        }

        public void Dispose()
        {
            try
            {
                if (File.Exists(_testCredentialPath))
                {
                    File.Delete(_testCredentialPath);
                }
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }
} 