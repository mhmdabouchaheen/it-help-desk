using HelpDesk.Api.Application.Attachments;
using HelpDesk.Api.Application.Authorization;
using HelpDesk.Api.Application.Common.Exceptions;
using HelpDesk.Api.Application.Tickets;
using HelpDesk.Api.Configuration;
using HelpDesk.Api.Contracts.Tickets;
using HelpDesk.Api.Data;
using HelpDesk.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HelpDesk.Api.Infrastructure.Attachments;

/// <summary>Applies ticket access, content policy, and metadata persistence for attachments.</summary>
public sealed class TicketAttachmentService(ApplicationDbContext dbContext, IAttachmentStorage storage,
    IOptions<AttachmentOptions> configuredOptions, TimeProvider timeProvider, ILogger<TicketAttachmentService> logger)
    : ITicketAttachmentService
{
    private readonly AttachmentOptions options = configuredOptions.Value;
    private static readonly IReadOnlyDictionary<string, string[]> Pairs = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        [".png"] = ["image/png"], [".jpg"] = ["image/jpeg"], [".jpeg"] = ["image/jpeg"], [".webp"] = ["image/webp"],
        [".pdf"] = ["application/pdf"], [".txt"] = ["text/plain"],
        [".docx"] = ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"],
        [".xlsx"] = ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"]
    };

    public async Task<TicketAttachmentResponse> UploadAsync(Guid ticketId, Stream content, string originalFileName,
        string contentType, long sizeBytes, TicketAccessContext accessContext, CancellationToken cancellationToken = default)
    {
        ValidateIdsAndAccess(ticketId, accessContext); ArgumentNullException.ThrowIfNull(content);
        if (string.IsNullOrWhiteSpace(originalFileName) || sizeBytes <= 0) throw new AttachmentValidationException();
        if (sizeBytes > options.MaxFileSizeBytes) throw new AttachmentTooLargeException();
        var safeName = Path.GetFileName(originalFileName.Trim());
        if (safeName.Length is 0 or > 255) throw new AttachmentValidationException();
        var extension = Path.GetExtension(safeName).ToLowerInvariant();
        var normalizedType = contentType?.Trim().ToLowerInvariant() ?? string.Empty;
        var allowedExtensions = options.AllowedExtensions.Select(NormalizeExtension).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var allowedTypes = options.AllowedContentTypes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!allowedExtensions.Contains(extension) || !allowedTypes.Contains(normalizedType) ||
            !Pairs.TryGetValue(extension, out var paired) || !paired.Contains(normalizedType, StringComparer.OrdinalIgnoreCase)) throw new AttachmentValidationException();

        var ticket = await dbContext.Tickets.SingleOrDefaultAsync(x => x.Id == ticketId, cancellationToken) ?? throw new TicketNotFoundException();
        if (ticket.CancelledAtUtc is not null) throw new TicketStateConflictException();
        var support = IsSupport(accessContext); var ownsTicket = ticket.CreatedByUserId == accessContext.UserId;
        if (!support && !ownsTicket) throw new TicketNotFoundException();
        if (!support && await dbContext.Statuses.AsNoTracking().Where(x => x.Id == ticket.StatusId).Select(x => x.IsTerminal).SingleAsync(cancellationToken)) throw new TicketStateConflictException();
        if (!await dbContext.Users.AsNoTracking().AnyAsync(x => x.Id == accessContext.UserId && x.IsActive, cancellationToken)) throw new AttachmentAccessDeniedException();

        var prepared = await ValidateSignatureAsync(content, extension, cancellationToken);
        StoredAttachmentResult stored = await storage.SaveAsync(prepared, extension, cancellationToken);
        if (stored.SizeBytes <= 0 || stored.SizeBytes > options.MaxFileSizeBytes) { await storage.DeleteAsync(stored.StorageKey, cancellationToken); if (stored.SizeBytes > options.MaxFileSizeBytes) throw new AttachmentTooLargeException(); throw new AttachmentValidationException(); }
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var entity = new TicketAttachment { Id = Guid.NewGuid(), TicketId = ticketId, CommentId = null, UploadedByUserId = accessContext.UserId,
            OriginalFileName = safeName, ContentType = normalizedType, SizeBytes = stored.SizeBytes, StorageProvider = stored.StorageProvider,
            StorageKey = stored.StorageKey, ContentHash = stored.ContentHash, CreatedAtUtc = now, DeletedAtUtc = null };
        dbContext.TicketAttachments.Add(entity); ticket.UpdatedAtUtc = now;
        try { await dbContext.SaveChangesAsync(cancellationToken); }
        catch { try { await storage.DeleteAsync(stored.StorageKey, CancellationToken.None); } catch (Exception cleanup) { logger.LogError(cleanup, "Failed to clean up attachment {AttachmentId} after metadata failure.", entity.Id); } throw; }
        var displayName = await dbContext.Users.AsNoTracking().Where(x => x.Id == accessContext.UserId).Select(x => x.DisplayName).SingleAsync(cancellationToken);
        return Response(entity, displayName);
    }

    public async Task<AttachmentDownloadResult> DownloadAsync(Guid ticketId, Guid attachmentId, TicketAccessContext accessContext, CancellationToken cancellationToken = default)
    {
        ValidateIdsAndAccess(ticketId, accessContext); if (attachmentId == Guid.Empty) throw new AttachmentNotFoundException();
        var item = await (from a in dbContext.TicketAttachments.AsNoTracking() join t in dbContext.Tickets.AsNoTracking() on a.TicketId equals t.Id
            where a.Id == attachmentId && a.TicketId == ticketId && a.DeletedAtUtc == null select new { Attachment = a, t.CreatedByUserId }).SingleOrDefaultAsync(cancellationToken);
        if (item is null || (!IsSupport(accessContext) && item.CreatedByUserId != accessContext.UserId)) throw new AttachmentNotFoundException();
        try { return new(await storage.OpenReadAsync(item.Attachment.StorageKey, cancellationToken), item.Attachment.ContentType, Path.GetFileName(item.Attachment.OriginalFileName), item.Attachment.SizeBytes); }
        catch (FileNotFoundException exception) { logger.LogError(exception, "Stored content unavailable for attachment {AttachmentId} using {StorageProvider}.", attachmentId, item.Attachment.StorageProvider); throw new AttachmentUnavailableException(); }
        catch (DirectoryNotFoundException exception) { logger.LogError(exception, "Stored content unavailable for attachment {AttachmentId} using {StorageProvider}.", attachmentId, item.Attachment.StorageProvider); throw new AttachmentUnavailableException(); }
    }

    public async Task DeleteAsync(Guid ticketId, Guid attachmentId, TicketAccessContext accessContext, CancellationToken cancellationToken = default)
    {
        ValidateIdsAndAccess(ticketId, accessContext); if (attachmentId == Guid.Empty) throw new AttachmentNotFoundException();
        var attachment = await dbContext.TicketAttachments.SingleOrDefaultAsync(x => x.Id == attachmentId && x.TicketId == ticketId, cancellationToken);
        if (attachment is null) throw new AttachmentNotFoundException();
        var ticket = await dbContext.Tickets.SingleAsync(x => x.Id == ticketId, cancellationToken);
        var admin = accessContext.Roles.Contains(AppRoles.Admin); var agent = accessContext.Roles.Contains(AppRoles.ItSupportAgent);
        if (!admin && !agent && !(ticket.CreatedByUserId == accessContext.UserId && attachment.UploadedByUserId == accessContext.UserId)) throw new AttachmentAccessDeniedException();
        if (attachment.DeletedAtUtc is not null) return;
        var now = timeProvider.GetUtcNow().UtcDateTime; attachment.DeletedAtUtc = now; ticket.UpdatedAtUtc = now; await dbContext.SaveChangesAsync(cancellationToken);
        try { await storage.DeleteAsync(attachment.StorageKey, cancellationToken); }
        catch (Exception exception) { logger.LogError(exception, "Physical cleanup failed for soft-deleted attachment {AttachmentId} using {StorageProvider}.", attachment.Id, attachment.StorageProvider); }
    }

    private static bool IsSupport(TicketAccessContext access) => access.Roles.Contains(AppRoles.Admin) || access.Roles.Contains(AppRoles.ItSupportAgent);
    private static void ValidateIdsAndAccess(Guid ticketId, TicketAccessContext access) { if (ticketId == Guid.Empty) throw new TicketNotFoundException(); if (access is null || access.UserId == Guid.Empty) throw new AttachmentAccessDeniedException(); }
    private static string NormalizeExtension(string value) { var x = value.Trim().ToLowerInvariant(); return x.StartsWith('.') ? x : "." + x; }
    private static TicketAttachmentResponse Response(TicketAttachment x, string uploader) => new() { Id=x.Id, TicketId=x.TicketId, CommentId=x.CommentId, OriginalFileName=x.OriginalFileName, ContentType=x.ContentType, SizeBytes=x.SizeBytes, UploadedByUserId=x.UploadedByUserId, UploadedByDisplayName=uploader, CreatedAtUtc=x.CreatedAtUtc };

    private static async Task<Stream> ValidateSignatureAsync(Stream content, string extension, CancellationToken token)
    {
        var prefix = new byte[512]; var count = 0;
        while (count < prefix.Length) { var read = await content.ReadAsync(prefix.AsMemory(count, prefix.Length-count), token); if (read == 0) break; count += read; }
        var p = prefix.AsSpan(0, count);
        var valid = extension switch { ".png" => p.StartsWith(new byte[]{0x89,0x50,0x4e,0x47,0x0d,0x0a,0x1a,0x0a}), ".jpg" or ".jpeg" => p.StartsWith(new byte[]{0xff,0xd8,0xff}), ".pdf" => p.StartsWith("%PDF-"u8), ".webp" => p.Length >= 12 && p[..4].SequenceEqual("RIFF"u8) && p.Slice(8,4).SequenceEqual("WEBP"u8), ".txt" => !p.Contains((byte)0), ".docx" or ".xlsx" => p.StartsWith(new byte[]{0x50,0x4b,0x03,0x04}), _ => false };
        if (!valid) throw new AttachmentValidationException();
        if (content.CanSeek) { content.Position -= count; return content; }
        return new PrefixStream(prefix[..count], content);
    }

    private sealed class PrefixStream(byte[] prefix, Stream remainder) : Stream
    {
        private readonly MemoryStream head = new(prefix, writable:false);
        public override bool CanRead=>true; public override bool CanSeek=>false; public override bool CanWrite=>false; public override long Length=>throw new NotSupportedException(); public override long Position { get=>throw new NotSupportedException(); set=>throw new NotSupportedException(); }
        public override int Read(byte[] buffer,int offset,int count) { var n=head.Read(buffer,offset,count); return n!=0?n:remainder.Read(buffer,offset,count); }
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer,CancellationToken token=default) { var n=await head.ReadAsync(buffer,token); return n!=0?n:await remainder.ReadAsync(buffer,token); }
        public override void Flush(){} public override long Seek(long o,SeekOrigin s)=>throw new NotSupportedException(); public override void SetLength(long v)=>throw new NotSupportedException(); public override void Write(byte[] b,int o,int c)=>throw new NotSupportedException();
    }
}
