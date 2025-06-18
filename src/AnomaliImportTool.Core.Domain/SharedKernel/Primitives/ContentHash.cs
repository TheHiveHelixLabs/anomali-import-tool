using AnomaliImportTool.Core.Domain.SharedKernel.Exceptions;

namespace AnomaliImportTool.Core.Domain.SharedKernel.Primitives;

/// <summary>
/// Content hash domain primitive for file integrity verification
/// Note: Hash computation operations moved to infrastructure layer
/// </summary>
public readonly record struct ContentHash
{
    public string Value { get; }
    public HashAlgorithmType Algorithm { get; }

    public ContentHash(string value, HashAlgorithmType algorithm = HashAlgorithmType.SHA256)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Content hash cannot be null or empty");

        var trimmedValue = value.Trim().ToUpperInvariant();
        
        ValidateHashFormat(trimmedValue, algorithm);

        Value = trimmedValue;
        Algorithm = algorithm;
    }

    private static void ValidateHashFormat(string hash, HashAlgorithmType algorithm)
    {
        var expectedLength = algorithm switch
        {
            HashAlgorithmType.MD5 => 32,
            HashAlgorithmType.SHA1 => 40,
            HashAlgorithmType.SHA256 => 64,
            HashAlgorithmType.SHA512 => 128,
            _ => throw new DomainException($"Unsupported hash algorithm: {algorithm}")
        };

        if (hash.Length != expectedLength)
            throw new DomainException($"Invalid {algorithm} hash length. Expected {expectedLength}, got {hash.Length}");

        if (!hash.All(c => char.IsDigit(c) || (c >= 'A' && c <= 'F')))
            throw new DomainException("Hash must contain only hexadecimal characters");
    }

    // Note: Hash computation operations moved to infrastructure layer
    // Use infrastructure services to compute hashes and create ContentHash instances
    
    public bool VerifyHash(string otherHashValue)
    {
        return Value.Equals(otherHashValue, StringComparison.OrdinalIgnoreCase);
    }

    public static implicit operator string(ContentHash hash) => hash.Value;
    public static explicit operator ContentHash(string value) => new(value);

    public override string ToString() => $"{Algorithm}:{Value}";

    public static bool TryCreate(string value, out ContentHash contentHash, HashAlgorithmType algorithm = HashAlgorithmType.SHA256)
    {
        try
        {
            contentHash = new ContentHash(value, algorithm);
            return true;
        }
        catch
        {
            contentHash = default;
            return false;
        }
    }
}

public enum HashAlgorithmType
{
    MD5,
    SHA1,
    SHA256,
    SHA512
} 