namespace Models;

/// <summary>
/// Holiday entry shown in the report.
/// </summary>
public sealed class HolidayReportItem
{
    /// <summary>
    /// Creates holiday report item.
    /// </summary>
    public HolidayReportItem(
        string name,
        DateOnly start,
        DateOnly end,
        IReadOnlyList<string> associatedCountries)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(associatedCountries);

        Name = name;
        Start = start;
        End = end;
        AssociatedCountries = associatedCountries;
    }

    /// <summary>
    /// Gets holiday name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets holiday start date.
    /// </summary>
    public DateOnly Start { get; }

    /// <summary>
    /// Gets holiday end date.
    /// </summary>
    public DateOnly End { get; }

    /// <summary>
    /// Gets countries explicitly associated with the holiday.
    /// </summary>
    public IReadOnlyList<string> AssociatedCountries { get; }
}
