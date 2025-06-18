using AnomaliImportTool.Core.Domain.SharedKernel.Exceptions;
using AnomaliImportTool.Core.Domain.SharedKernel.Guards;

namespace AnomaliImportTool.Core.Domain.SharedKernel.Primitives;

/// <summary>
/// Money domain primitive for financial calculations with currency support
/// </summary>
public readonly record struct Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency = "USD")
    {
        Guard.Against.NullOrWhiteSpace(currency, nameof(currency));
        Guard.Against.StringTooLong(currency, 3, nameof(currency));
        Guard.Against.StringTooShort(currency, 3, nameof(currency));

        Amount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        Currency = currency.ToUpperInvariant();
    }

    public static Money Zero(string currency = "USD") => new(0, currency);

    public Money Add(Money other)
    {
        ValidateSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        ValidateSameCurrency(other);
        return new Money(Amount - other.Amount, Currency);
    }

    public Money Multiply(decimal factor)
    {
        return new Money(Amount * factor, Currency);
    }

    public Money Divide(decimal divisor)
    {
        if (divisor == 0)
            throw new DomainException("Cannot divide money by zero", "DIVISION_BY_ZERO");
        
        return new Money(Amount / divisor, Currency);
    }

    private void ValidateSameCurrency(Money other)
    {
        if (Currency != other.Currency)
            throw new DomainException($"Cannot perform operation on different currencies: {Currency} and {other.Currency}", "CURRENCY_MISMATCH");
    }

    public static Money operator +(Money left, Money right) => left.Add(right);
    public static Money operator -(Money left, Money right) => left.Subtract(right);
    public static Money operator *(Money left, decimal right) => left.Multiply(right);
    public static Money operator /(Money left, decimal right) => left.Divide(right);

    public static bool operator >(Money left, Money right)
    {
        left.ValidateSameCurrency(right);
        return left.Amount > right.Amount;
    }

    public static bool operator <(Money left, Money right)
    {
        left.ValidateSameCurrency(right);
        return left.Amount < right.Amount;
    }

    public static bool operator >=(Money left, Money right)
    {
        left.ValidateSameCurrency(right);
        return left.Amount >= right.Amount;
    }

    public static bool operator <=(Money left, Money right)
    {
        left.ValidateSameCurrency(right);
        return left.Amount <= right.Amount;
    }

    public override string ToString() => $"{Amount:C} {Currency}";

    public string ToString(string format) => $"{Amount.ToString(format)} {Currency}";
} 