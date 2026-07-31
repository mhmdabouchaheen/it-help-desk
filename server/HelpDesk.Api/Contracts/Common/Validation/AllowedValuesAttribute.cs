using System.ComponentModel.DataAnnotations;

namespace HelpDesk.Api.Contracts.Common.Validation;

/// <summary>Validates that a string is one of a fixed set of values using ordinal, case-insensitive comparison.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class AllowedValuesAttribute(params string[] values) : ValidationAttribute
{
    private readonly IReadOnlyList<string> _values =
        Array.AsReadOnly(values ?? throw new ArgumentNullException(nameof(values)));

    /// <inheritdoc />
    public override bool IsValid(object? value) =>
        value is null || value is string text &&
        _values.Contains(text, StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public override string FormatErrorMessage(string name) =>
        $"{name} must be one of: {string.Join(", ", _values)}.";
}
