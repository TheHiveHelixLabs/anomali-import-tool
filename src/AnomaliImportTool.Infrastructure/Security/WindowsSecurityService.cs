using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AnomaliImportTool.Core.Interfaces;
using AnomaliImportTool.Core.Models;
using Microsoft.Extensions.Logging;

namespace AnomaliImportTool.Infrastructure.Security
{
    /// <summary>
    /// Security service implementation using Windows Data Protection API (DPAPI) for credential encryption.
    /// </summary>
    public class WindowsSecurityService : ISecurityService
    {
        private readonly ILogger<WindowsSecurityService> _logger;
        private readonly string _credentialStorePath;
        private readonly object _fileLock = new object();

        public WindowsSecurityService(ILogger<WindowsSecurityService> logger, string credentialStorePath = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Use provided path or default to a relative path in app directory
            _credentialStorePath = credentialStorePath ?? 
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "credentials.dat");
            
            // Ensure directory exists
            var directory = Path.GetDirectoryName(_credentialStorePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.LogInformation("Created credential store directory: {Directory}", directory);
            }
        }

        /// <summary>
        /// Encrypts sensitive data using Windows DPAPI.
        /// </summary>
        /// <param name="plainText">The plain text to encrypt.</param>
        /// <returns>The encrypted data as base64 string.</returns>
        public async Task<string> EncryptAsync(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                return string.Empty;
            }

            return await Task.Run(() =>
            {
                try
                {
                    var plainBytes = Encoding.UTF8.GetBytes(plainText);
                    var entropy = GenerateEntropy();
                    
                    var encryptedBytes = ProtectedData.Protect(
                        plainBytes,
                        entropy,
                        DataProtectionScope.CurrentUser);

                    // Combine entropy and encrypted data
                    var combined = new byte[entropy.Length + encryptedBytes.Length];
                    Array.Copy(entropy, 0, combined, 0, entropy.Length);
                    Array.Copy(encryptedBytes, 0, combined, entropy.Length, encryptedBytes.Length);

                    return Convert.ToBase64String(combined);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error encrypting data");
                    throw new InvalidOperationException("Failed to encrypt data", ex);
                }
            });
        }

        /// <summary>
        /// Decrypts data that was encrypted using Windows DPAPI.
        /// </summary>
        /// <param name="encryptedText">The encrypted data as base64 string.</param>
        /// <returns>The decrypted plain text.</returns>
        public async Task<string> DecryptAsync(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
            {
                return string.Empty;
            }

            return await Task.Run(() =>
            {
                try
                {
                    var combined = Convert.FromBase64String(encryptedText);
                    
                    // Extract entropy (first 16 bytes)
                    var entropy = new byte[16];
                    Array.Copy(combined, 0, entropy, 0, 16);
                    
                    // Extract encrypted data (remaining bytes)
                    var encryptedBytes = new byte[combined.Length - 16];
                    Array.Copy(combined, 16, encryptedBytes, 0, encryptedBytes.Length);

                    var decryptedBytes = ProtectedData.Unprotect(
                        encryptedBytes,
                        entropy,
                        DataProtectionScope.CurrentUser);

                    return Encoding.UTF8.GetString(decryptedBytes);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error decrypting data");
                    throw new InvalidOperationException("Failed to decrypt data", ex);
                }
            });
        }

        /// <summary>
        /// Saves API configuration with encrypted credentials.
        /// </summary>
        /// <param name="configuration">The API configuration to save.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task SaveApiConfigurationAsync(ApiConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            try
            {
                _logger.LogInformation("Saving API configuration");

                // Create a copy with encrypted API key
                var configToSave = new ApiConfiguration
                {
                    BaseUrl = configuration.BaseUrl,
                    Username = configuration.Username,
                    ApiKey = await EncryptAsync(configuration.ApiKey),
                    TimeoutSeconds = configuration.TimeoutSeconds,
                    MaxRetryAttempts = configuration.MaxRetryAttempts,
                    UseProxy = configuration.UseProxy,
                    ProxyUrl = configuration.ProxyUrl,
                    IgnoreSslErrors = configuration.IgnoreSslErrors
                };

                var json = JsonSerializer.Serialize(configToSave, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                lock (_fileLock)
                {
                    File.WriteAllText(_credentialStorePath, json);
                }

                _logger.LogInformation("API configuration saved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving API configuration");
                throw new InvalidOperationException("Failed to save API configuration", ex);
            }
        }

        /// <summary>
        /// Loads API configuration and decrypts credentials.
        /// </summary>
        /// <returns>The API configuration with decrypted credentials.</returns>
        public async Task<ApiConfiguration> LoadApiConfigurationAsync()
        {
            try
            {
                if (!File.Exists(_credentialStorePath))
                {
                    _logger.LogWarning("Credential file not found at {Path}", _credentialStorePath);
                    return null;
                }

                string json;
                lock (_fileLock)
                {
                    json = File.ReadAllText(_credentialStorePath);
                }

                var configuration = JsonSerializer.Deserialize<ApiConfiguration>(json);
                if (configuration == null)
                {
                    return null;
                }

                // Decrypt the API key
                configuration.ApiKey = await DecryptAsync(configuration.ApiKey);

                _logger.LogInformation("API configuration loaded successfully");
                return configuration;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading API configuration");
                throw new InvalidOperationException("Failed to load API configuration", ex);
            }
        }

        /// <summary>
        /// Validates that stored credentials are valid and can be decrypted.
        /// </summary>
        /// <returns>True if credentials are valid; otherwise, false.</returns>
        public async Task<bool> ValidateStoredCredentialsAsync()
        {
            try
            {
                var configuration = await LoadApiConfigurationAsync();
                return configuration != null && configuration.IsValid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating stored credentials");
                return false;
            }
        }

        /// <summary>
        /// Securely deletes stored credentials.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task DeleteStoredCredentialsAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    if (File.Exists(_credentialStorePath))
                    {
                        lock (_fileLock)
                        {
                            // Overwrite file with random data before deleting
                            var fileInfo = new FileInfo(_credentialStorePath);
                            var randomData = new byte[fileInfo.Length];
                            using (var rng = RandomNumberGenerator.Create())
                            {
                                rng.GetBytes(randomData);
                            }
                            File.WriteAllBytes(_credentialStorePath, randomData);
                            File.Delete(_credentialStorePath);
                        }

                        _logger.LogInformation("Stored credentials deleted successfully");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting stored credentials");
                    throw new InvalidOperationException("Failed to delete stored credentials", ex);
                }
            });
        }

        /// <summary>
        /// Generates a hash of the provided input for verification purposes.
        /// </summary>
        /// <param name="input">The input to hash.</param>
        /// <returns>The hash as a base64 string.</returns>
        public Task<string> HashAsync(string input)
        {
            return Task.Run(() =>
            {
                if (string.IsNullOrEmpty(input))
                {
                    return string.Empty;
                }

                using (var sha256 = SHA256.Create())
                {
                    var inputBytes = Encoding.UTF8.GetBytes(input);
                    var hashBytes = sha256.ComputeHash(inputBytes);
                    return Convert.ToBase64String(hashBytes);
                }
            });
        }

        /// <summary>
        /// Saves the API key securely using DPAPI encryption.
        /// </summary>
        /// <param name="apiKey">The API key to save.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task SaveApiKeyAsync(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new ArgumentNullException(nameof(apiKey));
            }

            try
            {
                _logger.LogInformation("Saving API key securely");

                var encryptedApiKey = await EncryptAsync(apiKey);
                var apiKeyPath = Path.Combine(Path.GetDirectoryName(_credentialStorePath), "apikey.dat");

                lock (_fileLock)
                {
                    File.WriteAllText(apiKeyPath, encryptedApiKey);
                }

                _logger.LogInformation("API key saved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving API key");
                throw new InvalidOperationException("Failed to save API key", ex);
            }
        }

        /// <summary>
        /// Gets the stored API key by decrypting it.
        /// </summary>
        /// <returns>The decrypted API key or null if not found.</returns>
        public string GetApiKey()
        {
            try
            {
                var apiKeyPath = Path.Combine(Path.GetDirectoryName(_credentialStorePath), "apikey.dat");
                
                if (!File.Exists(apiKeyPath))
                {
                    _logger.LogDebug("API key file not found");
                    return null;
                }

                string encryptedApiKey;
                lock (_fileLock)
                {
                    encryptedApiKey = File.ReadAllText(apiKeyPath);
                }

                if (string.IsNullOrEmpty(encryptedApiKey))
                {
                    return null;
                }

                // Use synchronous decryption for this method
                var task = DecryptAsync(encryptedApiKey);
                task.Wait();
                return task.Result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving API key");
                return null;
            }
        }

        /// <summary>
        /// Clears the stored API key by securely deleting the file.
        /// </summary>
        public void ClearApiKey()
        {
            try
            {
                var apiKeyPath = Path.Combine(Path.GetDirectoryName(_credentialStorePath), "apikey.dat");
                
                if (File.Exists(apiKeyPath))
                {
                    lock (_fileLock)
                    {
                        // Overwrite file with random data before deleting
                        var fileInfo = new FileInfo(apiKeyPath);
                        if (fileInfo.Length > 0)
                        {
                            var randomData = new byte[fileInfo.Length];
                            using (var rng = RandomNumberGenerator.Create())
                            {
                                rng.GetBytes(randomData);
                            }
                            File.WriteAllBytes(apiKeyPath, randomData);
                        }
                        File.Delete(apiKeyPath);
                    }

                    _logger.LogInformation("API key cleared successfully");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing API key");
                throw new InvalidOperationException("Failed to clear API key", ex);
            }
        }

        /// <summary>
        /// Generates entropy for DPAPI encryption.
        /// </summary>
        /// <returns>Random entropy bytes.</returns>
        private byte[] GenerateEntropy()
        {
            var entropy = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(entropy);
            }
            return entropy;
        }
    }
} 