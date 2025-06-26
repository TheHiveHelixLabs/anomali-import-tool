using AnomaliImportTool.Core.Models;

namespace AnomaliImportTool.Core.Interfaces;

/// <summary>
/// Interface for security operations including credential management and data protection
/// </summary>
public interface ISecurityService
{
    /// <summary>
    /// Encrypts sensitive data
    /// </summary>
    /// <param name="plainText">The plain text to encrypt</param>
    /// <returns>Encrypted data as base64 string</returns>
    Task<string> EncryptAsync(string plainText);
    
    /// <summary>
    /// Decrypts encrypted data
    /// </summary>
    /// <param name="encryptedText">The encrypted data as base64 string</param>
    /// <returns>Decrypted plain text</returns>
    Task<string> DecryptAsync(string encryptedText);
    
    /// <summary>
    /// Saves API configuration with encrypted credentials
    /// </summary>
    /// <param name="configuration">The API configuration to save</param>
    Task SaveApiConfigurationAsync(ApiConfiguration configuration);
    
    /// <summary>
    /// Loads API configuration and decrypts credentials
    /// </summary>
    /// <returns>The API configuration with decrypted credentials</returns>
    Task<ApiConfiguration> LoadApiConfigurationAsync();
    
    /// <summary>
    /// Validates stored credentials by attempting to decrypt them
    /// </summary>
    /// <returns>True if credentials are valid and can be decrypted</returns>
    Task<bool> ValidateStoredCredentialsAsync();
    
    /// <summary>
    /// Deletes stored credentials
    /// </summary>
    Task DeleteStoredCredentialsAsync();
    
    /// <summary>
    /// Creates a hash of the input string
    /// </summary>
    /// <param name="input">The input string to hash</param>
    /// <returns>Hash of the input</returns>
    Task<string> HashAsync(string input);
    
    /// <summary>
    /// Saves the API key securely
    /// </summary>
    /// <param name="apiKey">The API key to save</param>
    Task SaveApiKeyAsync(string apiKey);
    
    /// <summary>
    /// Gets the stored API key
    /// </summary>
    /// <returns>The decrypted API key or null if not found</returns>
    string GetApiKey();
    
    /// <summary>
    /// Clears the stored API key
    /// </summary>
    void ClearApiKey();
} 