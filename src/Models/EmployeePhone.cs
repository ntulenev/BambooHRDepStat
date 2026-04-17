namespace Models;

/// <summary>
/// One employee phone number.
/// </summary>
public readonly record struct EmployeePhone
{
    /// <summary>
    /// Creates an employee phone number.
    /// </summary>
    public EmployeePhone(string label, string number)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ArgumentException.ThrowIfNullOrWhiteSpace(number);

        Label = label.Trim();
        Number = number.Trim();
    }

    /// <summary>
    /// Gets the phone label.
    /// </summary>
    public string Label { get; }

    /// <summary>
    /// Gets the phone number.
    /// </summary>
    public string Number { get; }
}
