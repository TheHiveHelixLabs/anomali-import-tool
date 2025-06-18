using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using AnomaliImportTool.Core.Domain.SharedKernel.Exceptions;

namespace AnomaliImportTool.Core.Domain.SharedKernel.Guards;

/// <summary>
/// Guard clauses for input validation and business rule enforcement
/// </summary>
public static class Guard
{
    public static class Against
    {
        public static void Null<T>([NotNull] T? input, [CallerArgumentExpression(nameof(input))] string? parameterName = null)
            where T : class
        {
            if (input is null)
                throw new DomainException($"Parameter '{parameterName}' cannot be null", "NULL_PARAMETER");
        }

        public static void NullOrEmpty([NotNull] string? input, [CallerArgumentExpression(nameof(input))] string? parameterName = null)
        {
            if (string.IsNullOrEmpty(input))
                throw new DomainException($"Parameter '{parameterName}' cannot be null or empty", "NULL_OR_EMPTY_STRING");
        }

        public static void NullOrWhiteSpace([NotNull] string? input, [CallerArgumentExpression(nameof(input))] string? parameterName = null)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new DomainException($"Parameter '{parameterName}' cannot be null, empty, or whitespace", "NULL_OR_WHITESPACE_STRING");
        }

        public static void NegativeOrZero(int input, [CallerArgumentExpression(nameof(input))] string? parameterName = null)
        {
            if (input <= 0)
                throw new DomainException($"Parameter '{parameterName}' must be positive", "NEGATIVE_OR_ZERO_VALUE");
        }

        public static void NegativeOrZero(long input, [CallerArgumentExpression(nameof(input))] string? parameterName = null)
        {
            if (input <= 0)
                throw new DomainException($"Parameter '{parameterName}' must be positive", "NEGATIVE_OR_ZERO_VALUE");
        }

        public static void NegativeOrZero(decimal input, [CallerArgumentExpression(nameof(input))] string? parameterName = null)
        {
            if (input <= 0)
                throw new DomainException($"Parameter '{parameterName}' must be positive", "NEGATIVE_OR_ZERO_VALUE");
        }

        public static void Negative(int input, [CallerArgumentExpression(nameof(input))] string? parameterName = null)
        {
            if (input < 0)
                throw new DomainException($"Parameter '{parameterName}' cannot be negative", "NEGATIVE_VALUE");
        }

        public static void OutOfRange(int input, int min, int max, [CallerArgumentExpression(nameof(input))] string? parameterName = null)
        {
            if (input < min || input > max)
                throw new DomainException($"Parameter '{parameterName}' must be between {min} and {max}", "OUT_OF_RANGE");
        }

        public static void OutOfRange(DateTime input, DateTime min, DateTime max, [CallerArgumentExpression(nameof(input))] string? parameterName = null)
        {
            if (input < min || input > max)
                throw new DomainException($"Parameter '{parameterName}' must be between {min:yyyy-MM-dd} and {max:yyyy-MM-dd}", "OUT_OF_RANGE");
        }

        public static void StringTooLong(string input, int maxLength, [CallerArgumentExpression(nameof(input))] string? parameterName = null)
        {
            if (input.Length > maxLength)
                throw new DomainException($"Parameter '{parameterName}' cannot exceed {maxLength} characters", "STRING_TOO_LONG");
        }

        public static void StringTooShort(string input, int minLength, [CallerArgumentExpression(nameof(input))] string? parameterName = null)
        {
            if (input.Length < minLength)
                throw new DomainException($"Parameter '{parameterName}' must be at least {minLength} characters", "STRING_TOO_SHORT");
        }

        public static void EmptyCollection<T>(IEnumerable<T> input, [CallerArgumentExpression(nameof(input))] string? parameterName = null)
        {
            if (!input.Any())
                throw new DomainException($"Parameter '{parameterName}' cannot be empty", "EMPTY_COLLECTION");
        }

        public static void InvalidFormat(string input, string pattern, [CallerArgumentExpression(nameof(input))] string? parameterName = null)
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(input, pattern))
                throw new DomainException($"Parameter '{parameterName}' has invalid format", "INVALID_FORMAT");
        }

        public static void Expression(bool expression, string message, string errorCode = "BUSINESS_RULE_VIOLATION")
        {
            if (expression)
                throw new DomainException(message, errorCode);
        }
    }
} 