using System.Text.RegularExpressions;
using AnomaliImportTool.Core.Domain.SharedKernel.Exceptions;

namespace AnomaliImportTool.Core.Domain.SharedKernel.Primitives;

/// <summary>
/// Email address domain primitive with built-in validation
/// </summary>
public readonly record struct EmailAddress
{
    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Value { get; }

    public EmailAddress(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Email address cannot be null or empty");

        var trimmedValue = value.Trim();
        if (!EmailRegex.IsMatch(trimmedValue))
            throw new DomainException($"Invalid email address format: {trimmedValue}");

        if (trimmedValue.Length > 254) // RFC 5321 limit
            throw new DomainException("Email address exceeds maximum length of 254 characters");

        Value = trimmedValue.ToLowerInvariant();
    }

    public static implicit operator string(EmailAddress email) => email.Value;
    public static explicit operator EmailAddress(string value) => new(value);

    public override string ToString() => Value;

    public static bool TryCreate(string value, out EmailAddress emailAddress)
    {
        try
        {
            emailAddress = new EmailAddress(value);
            return true;
        }
        catch
        {
            emailAddress = default;
            return false;
        }
    }
} 