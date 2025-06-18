using AnomaliImportTool.Core.Domain.SharedKernel.Exceptions;

namespace AnomaliImportTool.Core.Domain.SharedKernel.Primitives;

/// <summary>
/// Correlation ID domain primitive for operation tracing
/// </summary>
public readonly record struct CorrelationId
{
    public string Value { get; }

    public CorrelationId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Correlation ID cannot be null or empty");

        var trimmedValue = value.Trim();
        if (trimmedValue.Length > 50)
            throw new DomainException("Correlation ID cannot exceed 50 characters");

        // Allow alphanumeric, hyphens, and underscores only
        if (!trimmedValue.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_'))
            throw new DomainException("Correlation ID can only contain alphanumeric characters, hyphens, and underscores");

        Value = trimmedValue;
    }

    public CorrelationId() : this(GenerateNew()) { }

    public static CorrelationId New() => new();

    private static string GenerateNew()
    {
        return $"{Environment.MachineName}_{DateTime.UtcNow:yyyyMMdd}_{Guid.NewGuid():N}"[..36];
    }

    public static implicit operator string(CorrelationId correlationId) => correlationId.Value;
    public static explicit operator CorrelationId(string value) => new(value);

    public override string ToString() => Value;

    public static bool TryCreate(string value, out CorrelationId correlationId)
    {
        try
        {
            correlationId = new CorrelationId(value);
            return true;
        }
        catch
        {
            correlationId = default;
            return false;
        }
    }
} 