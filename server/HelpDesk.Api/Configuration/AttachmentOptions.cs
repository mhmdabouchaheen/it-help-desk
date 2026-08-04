namespace HelpDesk.Api.Configuration;

/// <summary>Configures protected attachment storage and validation policy.</summary>
public sealed class AttachmentOptions
{
    public const string SectionName = "Attachments";
    /// <summary>Gets or sets the protected local storage root.</summary>
    public string StorageRoot { get; set; } = "Data/Uploads";
    /// <summary>Gets or sets the maximum accepted file size.</summary>
    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;
    /// <summary>Gets or sets allowed declared media types.</summary>
    public string[] AllowedContentTypes { get; set; } = [];
    /// <summary>Gets or sets allowed filename extensions.</summary>
    public string[] AllowedExtensions { get; set; } = [];
}
