using System.Globalization;

namespace Models;

/// <summary>
/// Strongly typed BambooHR employee identifier.
/// </summary>
public readonly record struct EmployeeId : IComparable<EmployeeId>, IFormattable
{
    /// <summary>
    /// Creates an employee identifier.
    /// </summary>
    public EmployeeId(int value)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
        Value = value;
    }

    /// <summary>
    /// Gets the underlying numeric value.
    /// </summary>
    public int Value { get; }

    /// <inheritdoc/>
    public int CompareTo(EmployeeId other)
    {
        return Value.CompareTo(other.Value);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return Value.ToString(CultureInfo.InvariantCulture);
    }

    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        return Value.ToString(format, formatProvider);
    }

    /// <summary>
    /// Formats the identifier using the supplied provider.
    /// </summary>
    public string ToString(IFormatProvider? formatProvider)
    {
        return Value.ToString(formatProvider);
    }

    /// <summary>
    /// Parses an employee identifier from a string.
    /// </summary>
    public static EmployeeId Parse(string value)
    {
        if (!TryParse(value, out var employeeId))
        {
            throw new FormatException("Employee ID must be a positive integer.");
        }

        return employeeId;
    }

    /// <summary>
    /// Tries to parse an employee identifier from a string.
    /// </summary>
    public static bool TryParse(string? value, out EmployeeId employeeId)
    {
        employeeId = default;

        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)
            || id <= 0)
        {
            return false;
        }

        employeeId = new EmployeeId(id);
        return true;
    }

    public static bool operator <(EmployeeId left, EmployeeId right)
    {
        return left.CompareTo(right) < 0;
    }

    public static bool operator <=(EmployeeId left, EmployeeId right)
    {
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >(EmployeeId left, EmployeeId right)
    {
        return left.CompareTo(right) > 0;
    }

    public static bool operator >=(EmployeeId left, EmployeeId right)
    {
        return left.CompareTo(right) >= 0;
    }

}
