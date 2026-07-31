using System.ComponentModel.DataAnnotations;

namespace HelpDesk.Api.Contracts.Common.Validation;

/// <summary>Validates that a value is a non-empty <see cref="Guid"/>.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class NotEmptyGuidAttribute : ValidationAttribute
{
    /// <inheritdoc />
    public override bool IsValid(object? value) => value is Guid identifier && identifier != Guid.Empty;
}
