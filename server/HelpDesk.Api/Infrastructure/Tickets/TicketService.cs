using HelpDesk.Api.Application.Authorization;
using HelpDesk.Api.Application.Audit;
using HelpDesk.Api.Application.Common.Exceptions;
using HelpDesk.Api.Application.Tickets;
using HelpDesk.Api.Application.Notifications;
using HelpDesk.Api.Contracts.Common;
using HelpDesk.Api.Contracts.Tickets;
using HelpDesk.Api.Data;
using HelpDesk.Api.Entities;
using HelpDesk.Api.Infrastructure.Authorization;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HelpDesk.Api.Infrastructure.Tickets;

/// <summary>Implements core ticket creation, visibility, retrieval, and basic editing.</summary>
public sealed class TicketService(
    ApplicationDbContext dbContext,
    ITicketNumberGenerator ticketNumberGenerator,
    TimeProvider timeProvider,
    ILogger<TicketService> logger,
    ITicketNotificationService ticketNotifications,
    IActivityLogService? activityLogs = null) : ITicketService
{
    private const string InitialStatusName = "Open";
    private const string ReferenceNumberIndex = "IX_Tickets_ReferenceNumber";
    private const string ActiveAssignmentIndex = "IX_TicketAssignments_TicketId";
    private const int MaxNumberAttempts = 3;

    /// <inheritdoc />
    public async Task<TicketDetailResponse> CreateAsync(
        CreateTicketRequest request,
        TicketAccessContext accessContext,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateAccess(accessContext);
        var (title, description) = Normalize(request.Title, request.Description);

        var creatorActive = await dbContext.Users.AsNoTracking()
            .Where(user => user.Id == accessContext.UserId)
            .Select(user => (bool?)user.IsActive)
            .SingleOrDefaultAsync(cancellationToken);
        if (creatorActive is not true)
            throw new TicketAccessDeniedException();

        await ValidateCategoryAsync(request.CategoryId, cancellationToken);
        await ValidatePriorityAsync(request.PriorityId, cancellationToken);
        var statusId = await dbContext.Statuses.AsNoTracking()
            .Where(status => status.Name == InitialStatusName && status.IsActive)
            .Select(status => (short?)status.Id)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new StatusNotFoundException();

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            ReferenceNumber = ticketNumberGenerator.Generate(),
            Title = title,
            Description = description,
            CategoryId = request.CategoryId,
            PriorityId = request.PriorityId,
            StatusId = statusId,
            CreatedByUserId = accessContext.UserId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        dbContext.Tickets.Add(ticket);

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                break;
            }
            catch (DbUpdateException exception) when (
                attempt < MaxNumberAttempts && IsReferenceNumberCollision(exception))
            {
                logger.LogWarning(
                    "Generated ticket number collided with the unique index; retrying attempt {Attempt}.",
                    attempt + 1);
                ticket.ReferenceNumber = ticketNumberGenerator.Generate();
            }
        }

        await TryAuditAsync(accessContext.UserId,ActivityActions.TicketCreated,ticket.Id,
            new Dictionary<string,string?>{{"referenceNumber",ticket.ReferenceNumber},{"categoryId",ticket.CategoryId.ToString()},{"priorityId",ticket.PriorityId.ToString()}},cancellationToken);
        return await GetByIdAsync(ticket.Id, accessContext, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PagedResponse<TicketSummaryResponse>> GetPagedAsync(
        TicketListRequest request,
        TicketAccessContext accessContext,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateAccess(accessContext);
        ValidateListRequest(request);

        IQueryable<Ticket> query = TicketReadScope.Apply(
            dbContext.Tickets.AsNoTracking(), dbContext, accessContext);

        if (request.CategoryId.HasValue)
            query = query.Where(ticket => ticket.CategoryId == request.CategoryId);
        if (request.PriorityId.HasValue)
            query = query.Where(ticket => ticket.PriorityId == request.PriorityId);
        if (request.StatusId.HasValue)
            query = query.Where(ticket => ticket.StatusId == request.StatusId);
        if (request.CreatedByUserId.HasValue)
            query = query.Where(ticket => ticket.CreatedByUserId == request.CreatedByUserId);
        if (request.AssignedToUserId.HasValue)
            query = query.Where(ticket => ticket.AssignedToUserId == request.AssignedToUserId);
        if (request.CreatedFromUtc.HasValue)
            query = query.Where(ticket => ticket.CreatedAtUtc >= request.CreatedFromUtc);
        if (request.CreatedToUtc.HasValue)
            query = query.Where(ticket => ticket.CreatedAtUtc <= request.CreatedToUtc);

        var search = request.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            if (dbContext.Database.IsNpgsql())
            {
                var pattern = $"%{EscapeLike(search)}%";
                query = query.Where(ticket =>
                    EF.Functions.ILike(ticket.ReferenceNumber, pattern, "\\") ||
                    EF.Functions.ILike(ticket.Title, pattern, "\\"));
            }
            else
            {
                var normalized = search.ToUpperInvariant();
                query = query.Where(ticket =>
                    ticket.ReferenceNumber.ToUpper().Contains(normalized) ||
                    ticket.Title.ToUpper().Contains(normalized));
            }
        }

        var totalCount = await query.CountAsync(cancellationToken);
        query = ApplyOrdering(query, request.SortBy, request.SortDirection);
        var skip = checked((request.PageNumber - 1) * request.PageSize);

        var items = await ProjectSummaries(query)
            .Skip(skip).Take(request.PageSize)
            .ToListAsync(cancellationToken);
        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)request.PageSize);

        return new PagedResponse<TicketSummaryResponse>
        {
            Items = items,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasPreviousPage = request.PageNumber > 1 && totalPages > 0,
            HasNextPage = request.PageNumber < totalPages
        };
    }

    /// <inheritdoc />
    public async Task<TicketDetailResponse> GetByIdAsync(
        Guid ticketId,
        TicketAccessContext accessContext,
        CancellationToken cancellationToken = default)
    {
        if (ticketId == Guid.Empty)
            throw new TicketValidationException();
        ValidateAccess(accessContext);
        var support = TicketReadScope.IsSupportWide(accessContext);

        var ticketQuery = TicketReadScope.Apply(
            dbContext.Tickets.AsNoTracking().Where(ticket => ticket.Id == ticketId),
            dbContext,
            accessContext);

        var detail = await ProjectDetails(ticketQuery).SingleOrDefaultAsync(cancellationToken)
            ?? throw new TicketNotFoundException();

        detail = await PopulateHistoryAsync(detail, support, cancellationToken);
        return detail;
    }

    /// <inheritdoc />
    public async Task<TicketDetailResponse> UpdateAsync(
        Guid ticketId,
        UpdateTicketRequest request,
        TicketAccessContext accessContext,
        CancellationToken cancellationToken = default)
    {
        if (ticketId == Guid.Empty)
            throw new TicketValidationException();
        ArgumentNullException.ThrowIfNull(request);
        var support = ValidateAccess(accessContext);
        var (title, description) = Normalize(request.Title, request.Description);

        var ticket = await dbContext.Tickets.SingleOrDefaultAsync(
            item => item.Id == ticketId, cancellationToken);
        if (ticket is null || !support && ticket.CreatedByUserId != accessContext.UserId)
            throw new TicketNotFoundException();
        if (ticket.CancelledAtUtc is not null)
            throw new TicketStateConflictException();

        if (!support)
        {
            var terminal = await dbContext.Statuses.AsNoTracking()
                .Where(status => status.Id == ticket.StatusId)
                .Select(status => (bool?)status.IsTerminal)
                .SingleOrDefaultAsync(cancellationToken)
                ?? throw new StatusNotFoundException();
            if (terminal)
                throw new TicketStateConflictException();
        }

        await ValidateCategoryAsync(request.CategoryId, cancellationToken);
        await ValidatePriorityAsync(request.PriorityId, cancellationToken);

        var changedFields=new List<string>();if(ticket.Title!=title)changedFields.Add("title");if(ticket.Description!=description)changedFields.Add("description");if(ticket.CategoryId!=request.CategoryId)changedFields.Add("categoryId");if(ticket.PriorityId!=request.PriorityId)changedFields.Add("priorityId");
        if (changedFields.Count>0)
        {
            ticket.Title = title;
            ticket.Description = description;
            ticket.CategoryId = request.CategoryId;
            ticket.PriorityId = request.PriorityId;
            ticket.UpdatedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
            await dbContext.SaveChangesAsync(cancellationToken);
            await TryAuditAsync(accessContext.UserId,ActivityActions.TicketUpdated,ticketId,
                new Dictionary<string,string?>{{"changedFields",string.Join(',',changedFields)}},cancellationToken);
        }

        return await GetByIdAsync(ticketId, accessContext, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TicketDetailResponse> AssignAsync(
        Guid ticketId,
        AssignTicketRequest request,
        TicketAccessContext accessContext,
        CancellationToken cancellationToken = default)
    {
        if (ticketId == Guid.Empty || request?.AssignedToUserId == Guid.Empty)
            throw new TicketValidationException();
        ArgumentNullException.ThrowIfNull(request);
        RequireSupport(accessContext);

        var ticket = await dbContext.Tickets.SingleOrDefaultAsync(x => x.Id == ticketId, cancellationToken)
            ?? throw new TicketNotFoundException();
        if (ticket.CancelledAtUtc is not null)
            throw new TicketStateConflictException();
        if (await IsTerminalAsync(ticket.StatusId, cancellationToken))
            throw new TicketStateConflictException();
        if (!await IsSupportAssignmentTargetAsync(request.AssignedToUserId, cancellationToken))
            throw new AssignmentTargetNotFoundException();

        var current = await dbContext.TicketAssignments.SingleOrDefaultAsync(
            x => x.TicketId == ticketId && x.EndedAtUtc == null, cancellationToken);
        if (current?.AssignedToUserId == request.AssignedToUserId)
            return await GetByIdAsync(ticketId, accessContext, cancellationToken);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (current is not null)
        {
            current.EndedAtUtc = now;
            current.EndedByUserId = accessContext.UserId;
        }

        var assignment = new TicketAssignment
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            AssignedToUserId = request.AssignedToUserId,
            AssignedByUserId = accessContext.UserId,
            AssignedAtUtc = now,
            Reason = NormalizeNote(request.Note)
        };
        dbContext.TicketAssignments.Add(assignment);
        ticket.AssignedToUserId = request.AssignedToUserId;
        ticket.UpdatedAtUtc = now;
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsActiveAssignmentConflict(exception))
        {
            throw new TicketStateConflictException();
        }

        logger.LogInformation(
            "Assigned ticket {TicketId} to user {TargetUserId} by user {ActingUserId}; history {HistoryId}.",
            ticketId, request.AssignedToUserId, accessContext.UserId, assignment.Id);
        await TryNotifyAsync(() => ticketNotifications.NotifyAssignmentAsync(ticket.Id,ticket.ReferenceNumber,
            request.AssignedToUserId,accessContext.UserId,cancellationToken),ticket.Id);
        await TryAuditAsync(accessContext.UserId,ActivityActions.TicketAssigned,ticketId,
            new Dictionary<string,string?>{{"assignedToUserId",request.AssignedToUserId.ToString()},{"previousAssignedToUserId",current?.AssignedToUserId.ToString()}},cancellationToken);
        return await GetByIdAsync(ticketId, accessContext, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TicketDetailResponse> ChangeStatusAsync(
        Guid ticketId,
        ChangeTicketStatusRequest request,
        TicketAccessContext accessContext,
        CancellationToken cancellationToken = default)
    {
        if (ticketId == Guid.Empty || request?.StatusId <= 0)
            throw new TicketValidationException();
        ArgumentNullException.ThrowIfNull(request);
        RequireSupport(accessContext);

        var ticket = await dbContext.Tickets.SingleOrDefaultAsync(x => x.Id == ticketId, cancellationToken)
            ?? throw new TicketNotFoundException();
        if (ticket.CancelledAtUtc is not null)
            throw new TicketStateConflictException();
        var currentStatus = await dbContext.Statuses.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == ticket.StatusId, cancellationToken)
            ?? throw new StatusNotFoundException();
        var targetStatus = await dbContext.Statuses.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == request.StatusId && x.IsActive, cancellationToken)
            ?? throw new StatusNotFoundException();
        if (currentStatus.Id == targetStatus.Id)
            return await GetByIdAsync(ticketId, accessContext, cancellationToken);
        if (currentStatus.IsTerminal &&
            !accessContext.Roles.Contains(AppRoles.Admin, StringComparer.Ordinal))
            throw new TicketStateConflictException();

        var now = timeProvider.GetUtcNow().UtcDateTime;
        ApplyTerminalTimestamps(ticket, currentStatus, targetStatus, now);
        var history = new TicketStatusHistory
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            FromStatusId = currentStatus.Id,
            ToStatusId = targetStatus.Id,
            ChangedByUserId = accessContext.UserId,
            ChangedAtUtc = now,
            Reason = NormalizeNote(request.Note)
        };
        ticket.StatusId = targetStatus.Id;
        ticket.UpdatedAtUtc = now;
        dbContext.TicketStatusHistory.Add(history);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Changed ticket {TicketId} status to {StatusId} by user {ActingUserId}; history {HistoryId}.",
            ticketId, targetStatus.Id, accessContext.UserId, history.Id);
        await TryNotifyAsync(() => ticketNotifications.NotifyStatusChangedAsync(ticket.Id,ticket.ReferenceNumber,
            ticket.CreatedByUserId,accessContext.UserId,targetStatus.Name,cancellationToken),ticket.Id);
        await TryAuditAsync(accessContext.UserId,ActivityActions.TicketStatusChanged,ticketId,
            new Dictionary<string,string?>{{"fromStatusId",currentStatus.Id.ToString()},{"toStatusId",targetStatus.Id.ToString()}},cancellationToken);
        return await GetByIdAsync(ticketId, accessContext, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TicketCommentResponse> AddCommentAsync(
        Guid ticketId,
        AddTicketCommentRequest request,
        TicketAccessContext accessContext,
        CancellationToken cancellationToken = default)
    {
        if (ticketId == Guid.Empty)
            throw new TicketValidationException();
        ArgumentNullException.ThrowIfNull(request);
        var support = ValidateAccess(accessContext);
        var content = request.Content?.Trim();
        if (string.IsNullOrWhiteSpace(content))
            throw new TicketValidationException();

        var ticket = await dbContext.Tickets.SingleOrDefaultAsync(x => x.Id == ticketId, cancellationToken);
        if (ticket is null || !support && ticket.CreatedByUserId != accessContext.UserId)
            throw new TicketNotFoundException();
        if (ticket.CancelledAtUtc is not null && !support)
            throw new TicketStateConflictException();
        if (request.IsInternal && !support)
            throw new TicketAccessDeniedException();
        if (!support && await IsTerminalAsync(ticket.StatusId, cancellationToken))
            throw new TicketStateConflictException();

        var author = await dbContext.Users.AsNoTracking()
            .Where(x => x.Id == accessContext.UserId && x.IsActive)
            .Select(x => x.DisplayName)
            .SingleOrDefaultAsync(cancellationToken);
        if (author is null)
            throw new TicketAccessDeniedException();

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var comment = new TicketComment
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            AuthorUserId = accessContext.UserId,
            Body = content,
            Visibility = request.IsInternal
                ? TicketCommentVisibilities.Internal
                : TicketCommentVisibilities.Public,
            CreatedAtUtc = now
        };
        ticket.UpdatedAtUtc = now;
        dbContext.TicketComments.Add(comment);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Added comment {CommentId} to ticket {TicketId} by user {ActingUserId}.",
            comment.Id, ticketId, accessContext.UserId);
        await TryNotifyAsync(() => ticketNotifications.NotifyCommentAddedAsync(ticket.Id,ticket.ReferenceNumber,
            ticket.CreatedByUserId,ticket.AssignedToUserId,accessContext.UserId,request.IsInternal,cancellationToken),ticket.Id);
        await TryAuditAsync(accessContext.UserId,request.IsInternal?ActivityActions.TicketInternalCommentAdded:ActivityActions.TicketCommentAdded,ticketId,
            new Dictionary<string,string?>{{"commentId",comment.Id.ToString()},{"visibility",comment.Visibility}},cancellationToken);
        return new TicketCommentResponse
        {
            Id = comment.Id,
            TicketId = ticketId,
            AuthorUserId = accessContext.UserId,
            AuthorDisplayName = author,
            Body = comment.Body,
            Visibility = comment.Visibility,
            CreatedAtUtc = comment.CreatedAtUtc,
            UpdatedAtUtc = comment.UpdatedAtUtc
        };
    }

    /// <inheritdoc />
    public async Task<TicketDetailResponse> CancelAsync(Guid ticketId, CancelTicketRequest request,
        TicketAccessContext accessContext, CancellationToken cancellationToken = default)
    {
        if (ticketId == Guid.Empty) throw new TicketValidationException();
        ArgumentNullException.ThrowIfNull(request);
        var support = ValidateAccess(accessContext);
        var ticket = await dbContext.Tickets.SingleOrDefaultAsync(x => x.Id == ticketId, cancellationToken);
        if (ticket is null || !support && ticket.CreatedByUserId != accessContext.UserId) throw new TicketNotFoundException();
        if (ticket.CancelledAtUtc is not null) return await GetByIdAsync(ticketId, accessContext, cancellationToken);
        if (!support && await IsTerminalAsync(ticket.StatusId, cancellationToken)) throw new TicketStateConflictException();
        var now = timeProvider.GetUtcNow().UtcDateTime;
        ticket.CancelledAtUtc = now; ticket.UpdatedAtUtc = now;
        var previousAssigneeId = ticket.AssignedToUserId;
        var assignment = await dbContext.TicketAssignments.SingleOrDefaultAsync(x => x.TicketId == ticketId && x.EndedAtUtc == null, cancellationToken);
        if (assignment is not null) { assignment.EndedAtUtc = now; assignment.EndedByUserId = accessContext.UserId; ticket.AssignedToUserId = null; }
        await dbContext.SaveChangesAsync(cancellationToken);
        await TryNotifyAsync(() => ticketNotifications.NotifyTicketCancelledAsync(ticket.Id,ticket.ReferenceNumber,
            ticket.CreatedByUserId,previousAssigneeId,accessContext.UserId,cancellationToken),ticket.Id);
        await TryAuditAsync(accessContext.UserId,ActivityActions.TicketCancelled,ticketId,null,cancellationToken);
        return await GetByIdAsync(ticketId, accessContext, cancellationToken);
    }

    private async Task TryNotifyAsync(Func<Task> action,Guid ticketId)
    {
        try{await action();}
        catch(Exception exception){logger.LogWarning(exception,"Notification creation failed after ticket {TicketId} was persisted.",ticketId);}
    }

    private async Task TryAuditAsync(Guid actor,string action,Guid ticketId,IReadOnlyDictionary<string,string?>? metadata,CancellationToken token)
    {
        if(activityLogs is null)return;try{await activityLogs.WriteAsync(actor,action,ActivityEntityTypes.Ticket,ticketId.ToString(),metadata,token);}
        catch(Exception exception){logger.LogWarning(exception,"Activity logging failed after ticket {TicketId} was persisted.",ticketId);}
    }

    private void RequireSupport(TicketAccessContext accessContext)
    {
        if (!ValidateAccess(accessContext))
            throw new TicketAccessDeniedException();
    }

    private async Task<bool> IsTerminalAsync(short statusId, CancellationToken cancellationToken) =>
        await dbContext.Statuses.AsNoTracking()
            .Where(x => x.Id == statusId)
            .Select(x => (bool?)x.IsTerminal)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new StatusNotFoundException();

    private async Task<bool> IsSupportAssignmentTargetAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var active = await dbContext.Users.AsNoTracking()
            .AnyAsync(x => x.Id == userId && x.IsActive, cancellationToken);
        if (!active)
            return false;
        return await (
            from userRole in dbContext.UserRoles.AsNoTracking()
            join role in dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            where userRole.UserId == userId &&
                (role.Name == AppRoles.Admin || role.Name == AppRoles.ItSupportAgent)
            select userRole).AnyAsync(cancellationToken);
    }

    private static string? NormalizeNote(string? note)
    {
        var normalized = note?.Trim();
        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }

    private static void ApplyTerminalTimestamps(
        Ticket ticket,
        Status currentStatus,
        Status targetStatus,
        DateTime now)
    {
        if (currentStatus.IsTerminal)
            SetTerminalTimestamp(ticket, currentStatus.Name, null);
        if (targetStatus.IsTerminal)
            SetTerminalTimestamp(ticket, targetStatus.Name, now);
    }

    private static void SetTerminalTimestamp(Ticket ticket, string statusName, DateTime? value)
    {
        if (statusName.Equals("Resolved", StringComparison.Ordinal))
            ticket.ResolvedAtUtc = value;
        else if (statusName.Equals("Closed", StringComparison.Ordinal))
            ticket.ClosedAtUtc = value;
        else if (statusName.Equals("Cancelled", StringComparison.Ordinal))
            ticket.CancelledAtUtc = value;
    }

    private async Task ValidateCategoryAsync(short id, CancellationToken cancellationToken)
    {
        if (!await dbContext.Categories.AsNoTracking().AnyAsync(
                category => category.Id == id && category.IsActive, cancellationToken))
            throw new CategoryNotFoundException();
    }

    private async Task ValidatePriorityAsync(short id, CancellationToken cancellationToken)
    {
        if (!await dbContext.Priorities.AsNoTracking().AnyAsync(
                priority => priority.Id == id && priority.IsActive, cancellationToken))
            throw new PriorityNotFoundException();
    }

    private static bool ValidateAccess(TicketAccessContext accessContext)
    {
        if (accessContext is null || accessContext.UserId == Guid.Empty)
            throw new TicketAccessDeniedException();
        var roles = accessContext.Roles ?? Array.Empty<string>();
        if (!roles.Any(role => AppRoles.All.Contains(role, StringComparer.Ordinal)))
            throw new TicketAccessDeniedException();
        return roles.Contains(AppRoles.Admin, StringComparer.Ordinal) ||
            roles.Contains(AppRoles.ItSupportAgent, StringComparer.Ordinal);
    }

    private static (string Title, string Description) Normalize(string? title, string? description)
    {
        title = title?.Trim();
        description = description?.Trim();
        if (string.IsNullOrWhiteSpace(title) || title.Length > 250 ||
            string.IsNullOrWhiteSpace(description))
            throw new TicketValidationException();
        return (title, description);
    }

    private static void ValidateListRequest(TicketListRequest request)
    {
        if (request.PageNumber < 1 || request.PageSize is < 1 or > 100 ||
            request.CreatedFromUtc.HasValue && request.CreatedToUtc.HasValue &&
            request.CreatedFromUtc > request.CreatedToUtc)
            throw new TicketValidationException();

        string[] fields =
        [
            TicketSortFields.CreatedAtUtc, TicketSortFields.UpdatedAtUtc,
            TicketSortFields.TicketNumber, TicketSortFields.Priority,
            TicketSortFields.Status, TicketSortFields.Title
        ];
        if (!fields.Contains(request.SortBy, StringComparer.OrdinalIgnoreCase) ||
            request.SortDirection is null ||
            !(request.SortDirection.Equals(SortDirections.Ascending, StringComparison.OrdinalIgnoreCase) ||
              request.SortDirection.Equals(SortDirections.Descending, StringComparison.OrdinalIgnoreCase)))
            throw new TicketValidationException();
    }

    private IQueryable<Ticket> ApplyOrdering(
        IQueryable<Ticket> query,
        string sortBy,
        string direction)
    {
        var ascending = direction.Equals(SortDirections.Ascending, StringComparison.OrdinalIgnoreCase);
        IOrderedQueryable<Ticket> ordered = sortBy.ToUpperInvariant() switch
        {
            "CREATEDATUTC" => ascending ? query.OrderBy(x => x.CreatedAtUtc) : query.OrderByDescending(x => x.CreatedAtUtc),
            "UPDATEDATUTC" => ascending ? query.OrderBy(x => x.UpdatedAtUtc) : query.OrderByDescending(x => x.UpdatedAtUtc),
            "TICKETNUMBER" => ascending ? query.OrderBy(x => x.ReferenceNumber) : query.OrderByDescending(x => x.ReferenceNumber),
            "PRIORITY" => ascending
                ? query.OrderBy(x => dbContext.Priorities.Where(p => p.Id == x.PriorityId).Select(p => p.Rank).First())
                : query.OrderByDescending(x => dbContext.Priorities.Where(p => p.Id == x.PriorityId).Select(p => p.Rank).First()),
            "STATUS" => ascending
                ? query.OrderBy(x => dbContext.Statuses.Where(s => s.Id == x.StatusId).Select(s => s.SortOrder).First())
                : query.OrderByDescending(x => dbContext.Statuses.Where(s => s.Id == x.StatusId).Select(s => s.SortOrder).First()),
            "TITLE" => ascending ? query.OrderBy(x => x.Title) : query.OrderByDescending(x => x.Title),
            _ => throw new TicketValidationException()
        };
        return ordered.ThenBy(x => x.Id);
    }

    private IQueryable<TicketSummaryResponse> ProjectSummaries(IQueryable<Ticket> query) =>
        query.Select(ticket => new TicketSummaryResponse
        {
            Id = ticket.Id,
            TicketNumber = ticket.ReferenceNumber,
            Title = ticket.Title,
            CategoryId = ticket.CategoryId,
            CategoryName = dbContext.Categories.Where(x => x.Id == ticket.CategoryId).Select(x => x.Name).First(),
            PriorityId = ticket.PriorityId,
            PriorityName = dbContext.Priorities.Where(x => x.Id == ticket.PriorityId).Select(x => x.Name).First(),
            StatusId = ticket.StatusId,
            StatusName = dbContext.Statuses.Where(x => x.Id == ticket.StatusId).Select(x => x.Name).First(),
            CreatedByUserId = ticket.CreatedByUserId,
            CreatedByDisplayName = dbContext.Users.Where(x => x.Id == ticket.CreatedByUserId).Select(x => x.DisplayName).FirstOrDefault() ?? "Unknown User",
            AssignedToUserId = ticket.AssignedToUserId,
            AssignedToDisplayName = ticket.AssignedToUserId == null
                ? null
                : dbContext.Users.Where(x => x.Id == ticket.AssignedToUserId).Select(x => x.DisplayName).FirstOrDefault(),
            CreatedAtUtc = ticket.CreatedAtUtc,
            UpdatedAtUtc = ticket.UpdatedAtUtc
            ,CancelledAtUtc = ticket.CancelledAtUtc
        });

    private IQueryable<TicketDetailResponse> ProjectDetails(IQueryable<Ticket> query) =>
        query.Select(ticket => new TicketDetailResponse
        {
            Id = ticket.Id,
            TicketNumber = ticket.ReferenceNumber,
            Title = ticket.Title,
            Description = ticket.Description,
            CategoryId = ticket.CategoryId,
            CategoryName = dbContext.Categories.Where(x => x.Id == ticket.CategoryId).Select(x => x.Name).First(),
            PriorityId = ticket.PriorityId,
            PriorityName = dbContext.Priorities.Where(x => x.Id == ticket.PriorityId).Select(x => x.Name).First(),
            StatusId = ticket.StatusId,
            StatusName = dbContext.Statuses.Where(x => x.Id == ticket.StatusId).Select(x => x.Name).First(),
            CreatedByUserId = ticket.CreatedByUserId,
            CreatedByDisplayName = dbContext.Users.Where(x => x.Id == ticket.CreatedByUserId).Select(x => x.DisplayName).FirstOrDefault() ?? "Unknown User",
            AssignedToUserId = ticket.AssignedToUserId,
            AssignedToDisplayName = ticket.AssignedToUserId == null ? null : dbContext.Users.Where(x => x.Id == ticket.AssignedToUserId).Select(x => x.DisplayName).FirstOrDefault(),
            CreatedAtUtc = ticket.CreatedAtUtc,
            UpdatedAtUtc = ticket.UpdatedAtUtc,
            ResolvedAtUtc = ticket.ResolvedAtUtc,
            ClosedAtUtc = ticket.ClosedAtUtc,
            CancelledAtUtc = ticket.CancelledAtUtc
        });

    private async Task<TicketDetailResponse> PopulateHistoryAsync(
        TicketDetailResponse detail,
        bool includeInternalComments,
        CancellationToken cancellationToken)
    {
        var commentQuery = dbContext.TicketComments.AsNoTracking()
            .Where(x => x.TicketId == detail.Id && x.DeletedAtUtc == null);
        if (!includeInternalComments)
            commentQuery = commentQuery.Where(x => x.Visibility == TicketCommentVisibilities.Public);

        var comments = await commentQuery
            .OrderBy(x => x.CreatedAtUtc).ThenBy(x => x.Id)
            .Select(x => new TicketCommentResponse
            {
                Id = x.Id, TicketId = x.TicketId, AuthorUserId = x.AuthorUserId,
                AuthorDisplayName = dbContext.Users.Where(u => u.Id == x.AuthorUserId).Select(u => u.DisplayName).FirstOrDefault() ?? "Unknown User",
                Body = x.Body, Visibility = x.Visibility, CreatedAtUtc = x.CreatedAtUtc, UpdatedAtUtc = x.UpdatedAtUtc
            }).ToListAsync(cancellationToken);
        var attachments = await dbContext.TicketAttachments.AsNoTracking()
            .Where(x => x.TicketId == detail.Id && x.DeletedAtUtc == null)
            .OrderBy(x => x.CreatedAtUtc).ThenBy(x => x.Id)
            .Select(x => new TicketAttachmentResponse
            {
                Id = x.Id, TicketId = x.TicketId, CommentId = x.CommentId,
                OriginalFileName = x.OriginalFileName, ContentType = x.ContentType, SizeBytes = x.SizeBytes,
                UploadedByUserId = x.UploadedByUserId,
                UploadedByDisplayName = dbContext.Users.Where(u => u.Id == x.UploadedByUserId).Select(u => u.DisplayName).FirstOrDefault() ?? "Unknown User",
                CreatedAtUtc = x.CreatedAtUtc
            }).ToListAsync(cancellationToken);
        var assignments = await dbContext.TicketAssignments.AsNoTracking()
            .Where(x => x.TicketId == detail.Id).OrderBy(x => x.AssignedAtUtc).ThenBy(x => x.Id)
            .Select(x => new TicketAssignmentResponse
            {
                Id = x.Id, TicketId = x.TicketId, AssignedToUserId = x.AssignedToUserId,
                AssignedToDisplayName = dbContext.Users.Where(u => u.Id == x.AssignedToUserId).Select(u => u.DisplayName).FirstOrDefault() ?? "Unknown User",
                AssignedByUserId = x.AssignedByUserId,
                AssignedByDisplayName = dbContext.Users.Where(u => u.Id == x.AssignedByUserId).Select(u => u.DisplayName).FirstOrDefault(),
                AssignedAtUtc = x.AssignedAtUtc, EndedAtUtc = x.EndedAtUtc, EndedByUserId = x.EndedByUserId,
                EndedByDisplayName = dbContext.Users.Where(u => u.Id == x.EndedByUserId).Select(u => u.DisplayName).FirstOrDefault(),
                Reason = x.Reason
            }).ToListAsync(cancellationToken);
        var statuses = await dbContext.TicketStatusHistory.AsNoTracking()
            .Where(x => x.TicketId == detail.Id).OrderBy(x => x.ChangedAtUtc).ThenBy(x => x.Id)
            .Select(x => new TicketStatusHistoryResponse
            {
                Id = x.Id, TicketId = x.TicketId, FromStatusId = x.FromStatusId,
                FromStatusName = dbContext.Statuses.Where(s => s.Id == x.FromStatusId).Select(s => s.Name).FirstOrDefault(),
                ToStatusId = x.ToStatusId,
                ToStatusName = dbContext.Statuses.Where(s => s.Id == x.ToStatusId).Select(s => s.Name).First(),
                ChangedByUserId = x.ChangedByUserId,
                ChangedByDisplayName = dbContext.Users.Where(u => u.Id == x.ChangedByUserId).Select(u => u.DisplayName).FirstOrDefault(),
                ChangedAtUtc = x.ChangedAtUtc, Reason = x.Reason
            }).ToListAsync(cancellationToken);

        return new TicketDetailResponse
        {
            Id = detail.Id, TicketNumber = detail.TicketNumber, Title = detail.Title,
            Description = detail.Description, CategoryId = detail.CategoryId, CategoryName = detail.CategoryName,
            PriorityId = detail.PriorityId, PriorityName = detail.PriorityName, StatusId = detail.StatusId,
            StatusName = detail.StatusName, CreatedByUserId = detail.CreatedByUserId,
            CreatedByDisplayName = detail.CreatedByDisplayName, AssignedToUserId = detail.AssignedToUserId,
            AssignedToDisplayName = detail.AssignedToDisplayName, CreatedAtUtc = detail.CreatedAtUtc,
            UpdatedAtUtc = detail.UpdatedAtUtc, ResolvedAtUtc = detail.ResolvedAtUtc,
            ClosedAtUtc = detail.ClosedAtUtc, CancelledAtUtc = detail.CancelledAtUtc,
            Comments = comments, Attachments = attachments, AssignmentHistory = assignments, StatusHistory = statuses
        };
    }

    private static bool IsReferenceNumberCollision(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: ReferenceNumberIndex
        };

    private static bool IsActiveAssignmentConflict(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: ActiveAssignmentIndex
        };

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);

}
