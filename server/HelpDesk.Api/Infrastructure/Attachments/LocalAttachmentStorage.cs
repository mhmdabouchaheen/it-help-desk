using System.Security.Cryptography;
using HelpDesk.Api.Application.Attachments;
using HelpDesk.Api.Configuration;
using Microsoft.Extensions.Options;

namespace HelpDesk.Api.Infrastructure.Attachments;

/// <summary>Stores attachment bytes outside the public web root using opaque names.</summary>
public sealed class LocalAttachmentStorage : IAttachmentStorage
{
    private const string Provider = "Local";
    private readonly string root;
    public LocalAttachmentStorage(IOptions<AttachmentOptions> options)
    {
        root = Path.GetFullPath(options.Value.StorageRoot);
        Directory.CreateDirectory(root);
    }

    public async Task<StoredAttachmentResult> SaveAsync(Stream content, string extension, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        cancellationToken.ThrowIfCancellationRequested();
        extension = extension.ToLowerInvariant();
        if (!extension.StartsWith('.') || extension.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || extension.Contains('/') || extension.Contains('\\')) throw new ArgumentException("Invalid extension.", nameof(extension));
        var key = $"{Guid.NewGuid():N}{extension}";
        var path = Resolve(key);
        try
        {
            await using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            var buffer = new byte[81920]; long size = 0;
            while (true) { var read = await content.ReadAsync(buffer, cancellationToken); if (read == 0) break; await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken); hash.AppendData(buffer, 0, read); size += read; }
            await output.FlushAsync(cancellationToken);
            return new(Provider, key, size, Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant());
        }
        catch { try { File.Delete(path); } catch { } throw; }
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Stream stream = new FileStream(Resolve(storageKey), FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    { cancellationToken.ThrowIfCancellationRequested(); File.Delete(Resolve(storageKey)); return Task.CompletedTask; }

    private string Resolve(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || Path.IsPathRooted(key) || key != Path.GetFileName(key)) throw new ArgumentException("Invalid storage key.", nameof(key));
        var path = Path.GetFullPath(Path.Combine(root, key));
        if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Invalid storage key.", nameof(key));
        return path;
    }
}
