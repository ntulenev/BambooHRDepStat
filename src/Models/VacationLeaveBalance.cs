using System.Globalization;

namespace Models;

/// <summary>
/// Available vacation leave balance in days.
/// </summary>
public readonly record struct VacationLeaveBalance
{
    /// <summary>
    /// Creates a vacation leave balance.
    /// </summary>
    public VacationLeaveBalance(decimal days)
    {
        Days = days;
    }

    /// <summary>
    /// Gets the available balance in days.
    /// </summary>
    public decimal Days { get; }

    /// <summary>
    /// Parses a BambooHR vacation balance value.
    /// </summary>
    public static bool TryParse(string? value, out VacationLeaveBalance balance)
    {
        balance = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        var separatorIndex = trimmed.IndexOf(' ', StringComparison.Ordinal);
        var numericPart = separatorIndex >= 0
            ? trimmed[..separatorIndex]
            : trimmed;
        var normalizedNumericPart = numericPart.Replace(",", ".", StringComparison.Ordinal);

        if (!decimal.TryParse(
                normalizedNumericPart,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var days))
        {
            return false;
        }

        balance = new VacationLeaveBalance(days);
        return true;
    }
}
