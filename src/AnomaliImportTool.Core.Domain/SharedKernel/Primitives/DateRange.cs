using AnomaliImportTool.Core.Domain.SharedKernel.Exceptions;

namespace AnomaliImportTool.Core.Domain.SharedKernel.Primitives;

/// <summary>
/// Date range domain primitive for temporal operations
/// </summary>
public readonly record struct DateRange
{
    public DateTime Start { get; }
    public DateTime End { get; }

    public TimeSpan Duration => End - Start;
    public bool IsValid => Start <= End;

    public DateRange(DateTime start, DateTime end)
    {
        if (start > end)
            throw new DomainException($"Start date ({start:yyyy-MM-dd}) cannot be after end date ({end:yyyy-MM-dd})", "INVALID_DATE_RANGE");

        Start = start.Date; // Normalize to date only
        End = end.Date;
    }

    public static DateRange FromDays(DateTime start, int days)
    {
        if (days < 0)
            throw new DomainException("Number of days cannot be negative", "NEGATIVE_DAYS");

        return new DateRange(start, start.AddDays(days));
    }

    public static DateRange Today() => new(DateTime.Today, DateTime.Today);

    public static DateRange ThisWeek()
    {
        var today = DateTime.Today;
        var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
        var endOfWeek = startOfWeek.AddDays(6);
        return new DateRange(startOfWeek, endOfWeek);
    }

    public static DateRange ThisMonth()
    {
        var today = DateTime.Today;
        var startOfMonth = new DateTime(today.Year, today.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
        return new DateRange(startOfMonth, endOfMonth);
    }

    public static DateRange ThisYear()
    {
        var today = DateTime.Today;
        var startOfYear = new DateTime(today.Year, 1, 1);
        var endOfYear = new DateTime(today.Year, 12, 31);
        return new DateRange(startOfYear, endOfYear);
    }

    public bool Contains(DateTime date)
    {
        var dateOnly = date.Date;
        return dateOnly >= Start && dateOnly <= End;
    }

    public bool Overlaps(DateRange other)
    {
        return Start <= other.End && End >= other.Start;
    }

    public DateRange? Intersection(DateRange other)
    {
        if (!Overlaps(other))
            return null;

        var intersectionStart = Start > other.Start ? Start : other.Start;
        var intersectionEnd = End < other.End ? End : other.End;

        return new DateRange(intersectionStart, intersectionEnd);
    }

    public DateRange Union(DateRange other)
    {
        var unionStart = Start < other.Start ? Start : other.Start;
        var unionEnd = End > other.End ? End : other.End;

        return new DateRange(unionStart, unionEnd);
    }

    public IEnumerable<DateTime> GetDatesInRange()
    {
        var current = Start;
        while (current <= End)
        {
            yield return current;
            current = current.AddDays(1);
        }
    }

    public override string ToString() => $"{Start:yyyy-MM-dd} to {End:yyyy-MM-dd}";

    public string ToString(string format) => $"{Start.ToString(format)} to {End.ToString(format)}";
} 