using System.Security.Cryptography;
using HelpDesk.Api.Configuration;
using HelpDesk.Api.Infrastructure.Attachments;
using Microsoft.Extensions.Options;

namespace HelpDesk.Api.Tests;

public sealed class LocalAttachmentStorageTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "helpdesk-attachments-" + Guid.NewGuid().ToString("N"));
    private LocalAttachmentStorage Storage() => new(Options.Create(new AttachmentOptions { StorageRoot = root }));
    [Fact] public async Task SaveOpenDelete_RoundTripsOpaqueHashedContent()
    {
        var bytes="hello"u8.ToArray();var storage=Storage();var saved=await storage.SaveAsync(new MemoryStream(bytes),".txt");
        Assert.Equal("Local",saved.StorageProvider);Assert.Equal(bytes.Length,saved.SizeBytes);Assert.Equal(Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(),saved.ContentHash);Assert.DoesNotContain("hello",saved.StorageKey);
        await using (var stream=await storage.OpenReadAsync(saved.StorageKey)){using var copy=new MemoryStream();await stream.CopyToAsync(copy);Assert.Equal(bytes,copy.ToArray());}
        await storage.DeleteAsync(saved.StorageKey);await storage.DeleteAsync(saved.StorageKey);await Assert.ThrowsAsync<FileNotFoundException>(()=>storage.OpenReadAsync(saved.StorageKey));
    }
    [Theory] [InlineData("../secret.txt")] [InlineData("..\\secret.txt")] [InlineData("C:\\secret.txt")]
    public async Task RejectsEscapingOrAbsoluteKeys(string key)=>await Assert.ThrowsAsync<ArgumentException>(()=>Storage().OpenReadAsync(key));
    [Fact] public void CreatesStorageRoot(){_ = Storage();Assert.True(Directory.Exists(root));}
    [Fact] public async Task CancellationIsRespected(){var cts=new CancellationTokenSource();cts.Cancel();await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>Storage().SaveAsync(new MemoryStream([1]),".txt",cts.Token));}
    public void Dispose(){if(Directory.Exists(root))Directory.Delete(root,true);GC.SuppressFinalize(this);}
}
