using System.ComponentModel.DataAnnotations;

namespace HelpDesk.Api.Contracts.Auth;

public sealed class UpdateProfileRequest
{
    [Required, MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;
}

public sealed class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required, MinLength(8)]
    public string NewPassword { get; set; } = string.Empty;

    [Required, Compare(nameof(NewPassword))]
    public string ConfirmPassword { get; set; } = string.Empty;
}
