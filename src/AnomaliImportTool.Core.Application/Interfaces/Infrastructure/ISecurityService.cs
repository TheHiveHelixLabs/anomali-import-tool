using AnomaliImportTool.Core.Domain.ValueObjects;

namespace AnomaliImportTool.Core.Application.Interfaces.Infrastructure;

/// <summary>
/// Interface for security operations following Clean Architecture dependency inversion
/// </summary>
public interface ISecurityService
{
    Task<EncryptionResult> EncryptAsync(string plainText, CancellationToken cancellationToken = default);
    Task<string> DecryptAsync(EncryptionResult encryptedData, CancellationToken cancellationToken = default);
    Task<SecureCredential> StoreCredentialAsync(string key, string value, CancellationToken cancellationToken = default);
    Task<string?> RetrieveCredentialAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> DeleteCredentialAsync(string key, CancellationToken cancellationToken = default);
    Task<ValidationResult> ValidateInputAsync(string input, string validationType, CancellationToken cancellationToken = default);
    Task<string> SanitizeInputAsync(string input, CancellationToken cancellationToken = default);
    Task<HashResult> GenerateHashAsync(string input, CancellationToken cancellationToken = default);
    Task<bool> VerifyHashAsync(string input, HashResult hash, CancellationToken cancellationToken = default);
    Task<AuditEntry> LogSecurityEventAsync(string eventType, string description, object? metadata = null, CancellationToken cancellationToken = default);
} 