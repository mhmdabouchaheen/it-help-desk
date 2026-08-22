using System.Text.Json;
using HelpDesk.Api.Application.Audit;
using HelpDesk.Api.Application.Common.Exceptions;
using HelpDesk.Api.Contracts.Audit;
using HelpDesk.Api.Contracts.Common;
using HelpDesk.Api.Data;
using HelpDesk.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Api.Infrastructure.Audit;

/// <summary>Persists and queries bounded, allowlisted activity metadata.</summary>
public sealed class ActivityLogService(ApplicationDbContext db, TimeProvider timeProvider,
    ILogger<ActivityLogService> logger) : IActivityLogService
{
    private const int MaxMetadataEntries = 12;
    private const int MaxMetadataValueLength = 500;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly IReadOnlyDictionary<string, HashSet<string>> AllowedMetadata =
        new Dictionary<string, HashSet<string>>(StringComparer.Ordinal)
        {
            [ActivityActions.TicketCreated] = Keys("referenceNumber", "categoryId", "priorityId"),
            [ActivityActions.TicketUpdated] = Keys("changedFields"),
            [ActivityActions.TicketAssigned] = Keys("assignedToUserId", "previousAssignedToUserId"),
            [ActivityActions.TicketStatusChanged] = Keys("fromStatusId", "toStatusId"),
            [ActivityActions.TicketCommentAdded] = Keys("commentId", "visibility"),
            [ActivityActions.TicketInternalCommentAdded] = Keys("commentId", "visibility"),
            [ActivityActions.TicketAttachmentUploaded] = Keys("attachmentId", "contentType", "sizeBytes"),
            [ActivityActions.TicketAttachmentDeleted] = Keys("attachmentId"),
            [ActivityActions.UserRegistered] = Keys("userId"),
            [ActivityActions.UserLoggedIn] = Keys("userId"),
            [ActivityActions.UserLoggedOut] = Keys("userId"),
            [ActivityActions.UserRolesChanged] = Keys("previousRoles", "newRoles"),
            [ActivityActions.NotificationMarkedRead] = Keys("notificationId"),
            [ActivityActions.NotificationMarkedAllRead] = Keys("count")
        };

    public async Task WriteAsync(Guid? actorUserId, string action, string entityType, string entityIdentifier,
        IReadOnlyDictionary<string, string?>? metadata = null, CancellationToken cancellationToken = default)
    {
        action = Validate(action, 150); entityType = Validate(entityType, 100); entityIdentifier = Validate(entityIdentifier, 100);
        var safeMetadata = ValidateMetadata(action, metadata);
        db.ActivityLogs.Add(new ActivityLog { ActorUserId=actorUserId, Action=action, EntityType=entityType,
            EntityIdentifier=entityIdentifier, OccurredAtUtc=timeProvider.GetUtcNow().UtcDateTime,
            Metadata=safeMetadata.Count == 0 ? null : JsonSerializer.Serialize(safeMetadata, JsonOptions) });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResponse<ActivityLogResponse>> GetPagedAsync(ActivityLogListRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request); ValidateRequest(request);
        var query = db.ActivityLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.Action)) query=query.Where(x=>x.Action==request.Action.Trim());
        if (!string.IsNullOrWhiteSpace(request.EntityType)) query=query.Where(x=>x.EntityType==request.EntityType.Trim());
        if (request.ActorUserId.HasValue) query=query.Where(x=>x.ActorUserId==request.ActorUserId);
        if (request.FromUtc.HasValue) query=query.Where(x=>x.OccurredAtUtc>=request.FromUtc);
        if (request.ToUtc.HasValue) query=query.Where(x=>x.OccurredAtUtc<=request.ToUtc);
        var total=await query.CountAsync(cancellationToken);
        var rows=await (from activity in query orderby activity.OccurredAtUtc descending,activity.Id descending
            join user in db.Users.AsNoTracking() on activity.ActorUserId equals (Guid?)user.Id into actors
            from actor in actors.DefaultIfEmpty()
            select new { Activity=activity, ActorDisplayName=actor==null?null:actor.DisplayName })
            .Skip(checked((request.PageNumber-1)*request.PageSize)).Take(request.PageSize).ToListAsync(cancellationToken);
        var pages=total==0?0:(int)Math.Ceiling(total/(double)request.PageSize);
        return new PagedResponse<ActivityLogResponse>{Items=rows.Select(x=>Map(x.Activity,x.ActorDisplayName)).ToList(),
            PageNumber=request.PageNumber,PageSize=request.PageSize,TotalCount=total,TotalPages=pages,
            HasPreviousPage=request.PageNumber>1&&pages>0,HasNextPage=request.PageNumber<pages};
    }

    public async Task<PagedResponse<ActivityLogResponse>> GetForTicketAsync(Guid ticketId,PagedRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if(ticketId==Guid.Empty||request.PageNumber<1||request.PageSize is <1 or >100)throw new ActivityLogValidationException();
        var id=ticketId.ToString();
        var query=db.ActivityLogs.AsNoTracking().Where(activity=>
            activity.EntityType==ActivityEntityTypes.Ticket&&activity.EntityIdentifier==id);
        var total=await query.CountAsync(cancellationToken);
        var rows=await (from activity in query
            orderby activity.OccurredAtUtc descending,activity.Id descending
            join user in db.Users.AsNoTracking() on activity.ActorUserId equals (Guid?)user.Id into actors
            from actor in actors.DefaultIfEmpty()
            select new {Activity=activity,ActorDisplayName=actor==null?null:actor.DisplayName})
            .Skip(checked((request.PageNumber-1)*request.PageSize)).Take(request.PageSize).ToListAsync(cancellationToken);
        var pages=total==0?0:(int)Math.Ceiling(total/(double)request.PageSize);
        return new PagedResponse<ActivityLogResponse>{Items=rows.Select(x=>Map(x.Activity,x.ActorDisplayName)).ToList(),
            PageNumber=request.PageNumber,PageSize=request.PageSize,TotalCount=total,TotalPages=pages,
            HasPreviousPage=request.PageNumber>1&&pages>0,HasNextPage=request.PageNumber<pages};
    }

    private static string Validate(string? value,int max){var result=value?.Trim();if(string.IsNullOrWhiteSpace(result)||result.Length>max)throw new ActivityLogValidationException();return result;}
    private static void ValidateRequest(ActivityLogListRequest r){if(r.PageNumber<1||r.PageSize is <1 or >100||r.FromUtc>r.ToUtc)throw new ActivityLogValidationException();}
    private static Dictionary<string,string?> ValidateMetadata(string action,IReadOnlyDictionary<string,string?>? metadata)
    {
        if(metadata is null||metadata.Count==0)return[];
        if(metadata.Count>MaxMetadataEntries||!AllowedMetadata.TryGetValue(action,out var allowed))throw new ActivityLogValidationException();
        var safe=new Dictionary<string,string?>(StringComparer.Ordinal);
        foreach(var (key,value) in metadata){if(!allowed.Contains(key)||key.Length>64||value?.Length>MaxMetadataValueLength)throw new ActivityLogValidationException();safe[key]=value;}
        return safe;
    }
    private ActivityLogResponse Map(ActivityLog x,string? actor)=>new(){Id=x.Id,ActorUserId=x.ActorUserId,
        ActorDisplayName=actor,Action=x.Action,EntityType=x.EntityType,EntityIdentifier=x.EntityIdentifier,
        OccurredAtUtc=x.OccurredAtUtc,Metadata=Deserialize(x.Metadata)};
    private IReadOnlyDictionary<string,string?> Deserialize(string? json){if(string.IsNullOrWhiteSpace(json))return new Dictionary<string,string?>();try{return JsonSerializer.Deserialize<Dictionary<string,string?>>(json,JsonOptions)??new();}catch(JsonException){logger.LogWarning("Ignored malformed metadata for an activity-log response.");return new Dictionary<string,string?>();}}
    private static HashSet<string> Keys(params string[] values)=>new(values,StringComparer.Ordinal);
}
