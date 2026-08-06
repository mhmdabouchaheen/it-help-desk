using HelpDesk.Api.Application.Authorization;
using HelpDesk.Api.Application.Common.Exceptions;
using HelpDesk.Api.Application.Tickets;
using HelpDesk.Api.Application.Notifications;
using HelpDesk.Api.Contracts.Tickets;
using HelpDesk.Api.Data;
using HelpDesk.Api.Entities;
using HelpDesk.Api.Infrastructure.Tickets;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace HelpDesk.Api.Tests;

public sealed class TicketServiceTests
{
    [Fact]
    public void NumberGenerator_ProducesBoundedPrefixedUniqueValuesFromInjectedTime()
    {
        var time = new FixedTimeProvider(new DateTimeOffset(2026, 7, 30, 15, 30, 45, TimeSpan.Zero));
        var generator = new TicketNumberGenerator(time);
        var values = Enumerable.Range(0, 100).Select(_ => generator.Generate()).ToArray();

        Assert.All(values, value =>
        {
            Assert.NotEmpty(value);
            Assert.True(value.Length <= 30);
            Assert.StartsWith("TKT-20260730-153045-", value);
            Assert.DoesNotContain("user", value, StringComparison.OrdinalIgnoreCase);
        });
        Assert.Equal(values.Length, values.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void CreateContract_DoesNotAcceptServerControlledFields()
    {
        var properties = typeof(CreateTicketRequest).GetProperties().Select(x => x.Name).ToArray();
        Assert.DoesNotContain("TicketNumber", properties);
        Assert.DoesNotContain("CreatedByUserId", properties);
        Assert.DoesNotContain("StatusId", properties);
        Assert.DoesNotContain("AssignedToUserId", properties);
    }

    [Theory]
    [InlineData(AppRoles.Admin)]
    [InlineData(AppRoles.ItSupportAgent)]
    [InlineData(AppRoles.Employee)]
    [InlineData(AppRoles.Manager)]
    public async Task RecognizedRole_CanCreate(string role)
    {
        await using var fixture = await Fixture.CreateAsync();
        var result = await fixture.Service.CreateAsync(
            new CreateTicketRequest { Title = "  Printer  ", Description = "  Jammed  ", CategoryId = 1, PriorityId = 1 },
            fixture.Access(fixture.OwnerId, role));

        Assert.Equal("Printer", result.Title);
        Assert.Equal("Jammed", result.Description);
        Assert.Equal(fixture.OwnerId, result.CreatedByUserId);
        Assert.Equal("Open", result.StatusName);
        Assert.Equal(fixture.Now.UtcDateTime, result.CreatedAtUtc);
        Assert.Null(result.AssignedToUserId);
        Assert.Empty(result.Comments);
        Assert.Empty(result.Attachments);
        Assert.Empty(result.AssignmentHistory);
        Assert.Empty(result.StatusHistory);
    }

    [Fact]
    public async Task Create_RejectsInvalidAccessContext()
    {
        await using var fixture = await Fixture.CreateAsync();
        await Assert.ThrowsAsync<TicketAccessDeniedException>(() => fixture.Service.CreateAsync(
            ValidCreate(), fixture.Access(Guid.Empty, AppRoles.Employee)));
        await Assert.ThrowsAsync<TicketAccessDeniedException>(() => fixture.Service.CreateAsync(
            ValidCreate(), fixture.Access(fixture.OwnerId, "Unknown")));
    }

    [Fact]
    public async Task Create_RejectsMissingOrInactiveCreator()
    {
        await using var fixture = await Fixture.CreateAsync();
        await Assert.ThrowsAsync<TicketAccessDeniedException>(() => fixture.Service.CreateAsync(
            ValidCreate(), fixture.Access(Guid.NewGuid(), AppRoles.Employee)));
        await Assert.ThrowsAsync<TicketAccessDeniedException>(() => fixture.Service.CreateAsync(
            ValidCreate(), fixture.Access(fixture.InactiveUserId, AppRoles.Employee)));
    }

    [Fact]
    public async Task Create_ValidatesLookupsAndNormalizedText()
    {
        await using var fixture = await Fixture.CreateAsync();
        await Assert.ThrowsAsync<CategoryNotFoundException>(() => fixture.Service.CreateAsync(
            ValidCreate(categoryId: 99), fixture.Access()));
        await Assert.ThrowsAsync<PriorityNotFoundException>(() => fixture.Service.CreateAsync(
            ValidCreate(priorityId: 99), fixture.Access()));
        await Assert.ThrowsAsync<TicketValidationException>(() => fixture.Service.CreateAsync(
            ValidCreate(title: "  "), fixture.Access()));
        await Assert.ThrowsAsync<TicketValidationException>(() => fixture.Service.CreateAsync(
            new CreateTicketRequest { Title = "T", Description = " ", CategoryId = 1, PriorityId = 1 }, fixture.Access()));
    }

    [Fact]
    public async Task Create_RejectsInactiveLookupsAndMissingOpen()
    {
        await using var fixture = await Fixture.CreateAsync();
        await Assert.ThrowsAsync<CategoryNotFoundException>(() => fixture.Service.CreateAsync(
            ValidCreate(categoryId: 2), fixture.Access()));
        await Assert.ThrowsAsync<PriorityNotFoundException>(() => fixture.Service.CreateAsync(
            ValidCreate(priorityId: 2), fixture.Access()));
        fixture.Db.Statuses.Single(x => x.Id == 1).IsActive = false;
        await fixture.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<StatusNotFoundException>(() => fixture.Service.CreateAsync(ValidCreate(), fixture.Access()));
    }

    [Theory]
    [InlineData(AppRoles.Admin, 2)]
    [InlineData(AppRoles.ItSupportAgent, 2)]
    [InlineData(AppRoles.Employee, 1)]
    [InlineData(AppRoles.Manager, 1)]
    public async Task List_AppliesRoleVisibility(string role, int expected)
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.AddTicketsAsync();
        var result = await fixture.Service.GetPagedAsync(new TicketListRequest(), fixture.Access(fixture.OwnerId, role));
        Assert.Equal(expected, result.TotalCount);
    }

    [Fact]
    public async Task List_MultiRoleSupportVisibilityIsAdditive()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.AddTicketsAsync();
        var result = await fixture.Service.GetPagedAsync(
            new TicketListRequest(), fixture.Access(fixture.OwnerId, AppRoles.Employee, AppRoles.ItSupportAgent));
        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task List_OwnershipCannotBeEscapedByCreatorFilter()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.AddTicketsAsync();
        var result = await fixture.Service.GetPagedAsync(
            new TicketListRequest { CreatedByUserId = fixture.OtherId },
            fixture.Access(fixture.OwnerId, AppRoles.Employee));
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task List_FiltersSearchesAndMapsAssignee()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.AddTicketsAsync();
        var result = await fixture.Service.GetPagedAsync(
            new TicketListRequest
            {
                Search = "PRINTER",
                CategoryId = 1,
                PriorityId = 1,
                StatusId = 1,
                AssignedToUserId = fixture.OtherId
            }, fixture.Access(fixture.OwnerId, AppRoles.Admin));
        var item = Assert.Single(result.Items);
        Assert.Equal(fixture.OtherId, item.AssignedToUserId);
        Assert.Equal("Other User", item.AssignedToDisplayName);
    }

    [Fact]
    public async Task List_AppliesDateFiltersAndPaginationMetadata()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.AddTicketsAsync();
        var result = await fixture.Service.GetPagedAsync(
            new TicketListRequest
            {
                CreatedFromUtc = fixture.Now.UtcDateTime.AddMinutes(-2),
                CreatedToUtc = fixture.Now.UtcDateTime,
                PageSize = 1
            }, fixture.Access(fixture.OwnerId, AppRoles.Admin));
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
        Assert.False(result.HasPreviousPage);
        Assert.True(result.HasNextPage);
    }

    [Fact]
    public async Task List_EmptyMetadataAndDefensiveValidation()
    {
        await using var fixture = await Fixture.CreateAsync();
        var empty = await fixture.Service.GetPagedAsync(new TicketListRequest(), fixture.Access());
        Assert.Equal(0, empty.TotalPages);
        Assert.False(empty.HasNextPage);
        Assert.False(empty.HasPreviousPage);
        await Assert.ThrowsAsync<TicketValidationException>(() => fixture.Service.GetPagedAsync(
            new TicketListRequest { PageSize = 101 }, fixture.Access()));
        await Assert.ThrowsAsync<TicketValidationException>(() => fixture.Service.GetPagedAsync(
            new TicketListRequest { SortBy = "Bad" }, fixture.Access()));
        await Assert.ThrowsAsync<TicketValidationException>(() => fixture.Service.GetPagedAsync(
            new TicketListRequest { SortDirection = "Bad" }, fixture.Access()));
    }

    [Theory]
    [InlineData(TicketSortFields.CreatedAtUtc)]
    [InlineData(TicketSortFields.UpdatedAtUtc)]
    [InlineData(TicketSortFields.TicketNumber)]
    [InlineData(TicketSortFields.Priority)]
    [InlineData(TicketSortFields.Status)]
    [InlineData(TicketSortFields.Title)]
    public async Task List_SupportsEverySortField(string sortBy)
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.AddTicketsAsync();
        var result = await fixture.Service.GetPagedAsync(
            new TicketListRequest { SortBy = sortBy, SortDirection = SortDirections.Ascending },
            fixture.Access(fixture.OwnerId, AppRoles.Admin));
        Assert.Equal(2, result.Items.Count);
    }

    [Theory]
    [InlineData(AppRoles.Admin, true)]
    [InlineData(AppRoles.ItSupportAgent, true)]
    [InlineData(AppRoles.Employee, false)]
    [InlineData(AppRoles.Manager, false)]
    public async Task GetById_EnforcesVisibility(string role, bool canSeeOther)
    {
        await using var fixture = await Fixture.CreateAsync();
        var (_, other) = await fixture.AddTicketsAsync();
        if (canSeeOther)
            Assert.Equal(other.Id, (await fixture.Service.GetByIdAsync(other.Id, fixture.Access(fixture.OwnerId, role))).Id);
        else
            await Assert.ThrowsAsync<TicketNotFoundException>(() =>
                fixture.Service.GetByIdAsync(other.Id, fixture.Access(fixture.OwnerId, role)));
    }

    [Fact]
    public async Task GetById_MapsOrderedSafeHistory()
    {
        await using var fixture = await Fixture.CreateAsync();
        var (ticket, _) = await fixture.AddTicketsAsync();
        await fixture.AddHistoryAsync(ticket);
        var detail = await fixture.Service.GetByIdAsync(ticket.Id, fixture.Access());
        Assert.Single(detail.Comments);
        Assert.Single(detail.Attachments);
        Assert.Single(detail.AssignmentHistory);
        Assert.Single(detail.StatusHistory);
        Assert.Equal("file.txt", detail.Attachments[0].OriginalFileName);
    }

    [Fact]
    public async Task GetById_RejectsEmptyAndMissingIds()
    {
        await using var fixture = await Fixture.CreateAsync();
        await Assert.ThrowsAsync<TicketValidationException>(() => fixture.Service.GetByIdAsync(Guid.Empty, fixture.Access()));
        await Assert.ThrowsAsync<TicketNotFoundException>(() => fixture.Service.GetByIdAsync(Guid.NewGuid(), fixture.Access()));
    }

    [Theory]
    [InlineData(AppRoles.Admin, true)]
    [InlineData(AppRoles.ItSupportAgent, true)]
    [InlineData(AppRoles.Employee, false)]
    [InlineData(AppRoles.Manager, false)]
    public async Task Update_EnforcesOwnershipAndSupportPrivileges(string role, bool canUpdateOther)
    {
        await using var fixture = await Fixture.CreateAsync();
        var (_, other) = await fixture.AddTicketsAsync();
        var action = () => fixture.Service.UpdateAsync(other.Id, ValidUpdate(), fixture.Access(fixture.OwnerId, role));
        if (canUpdateOther)
            Assert.Equal("Updated", (await action()).Title);
        else
            await Assert.ThrowsAsync<TicketNotFoundException>(action);
    }

    [Theory]
    [InlineData(AppRoles.Admin, true)]
    [InlineData(AppRoles.ItSupportAgent, true)]
    [InlineData(AppRoles.Employee, false)]
    [InlineData(AppRoles.Manager, false)]
    public async Task Update_TerminalStatusRestrictionDependsOnSupportPrivilege(string role, bool canUpdate)
    {
        await using var fixture = await Fixture.CreateAsync();
        var (ticket, _) = await fixture.AddTicketsAsync(terminal: true);
        var action = () => fixture.Service.UpdateAsync(ticket.Id, ValidUpdate(), fixture.Access(fixture.OwnerId, role));
        if (canUpdate)
            Assert.Equal("Updated", (await action()).Title);
        else
            await Assert.ThrowsAsync<TicketStateConflictException>(action);
    }

    [Fact]
    public async Task Update_ChangesOnlyBasicFieldsAndTrimsValues()
    {
        await using var fixture = await Fixture.CreateAsync();
        var (ticket, _) = await fixture.AddTicketsAsync();
        var originalNumber = ticket.ReferenceNumber;
        var originalCreator = ticket.CreatedByUserId;
        var originalStatus = ticket.StatusId;
        var result = await fixture.Service.UpdateAsync(
            ticket.Id,
            new UpdateTicketRequest { Title = " Updated ", Description = " Fixed ", CategoryId = 1, PriorityId = 1 },
            fixture.Access());
        Assert.Equal("Updated", result.Title);
        Assert.Equal("Fixed", result.Description);
        Assert.Equal(originalNumber, result.TicketNumber);
        Assert.Equal(originalCreator, result.CreatedByUserId);
        Assert.Equal(originalStatus, result.StatusId);
        Assert.Equal(fixture.Now.UtcDateTime, result.UpdatedAtUtc);
    }

    [Fact]
    public async Task ValidOperations_NoLongerThrowNotSupportedException()
    {
        await using var fixture = await Fixture.CreateAsync();
        var (ticket, _) = await fixture.AddTicketsAsync();
        var assignment = await Record.ExceptionAsync(() => fixture.Service.AssignAsync(
            ticket.Id, new() { AssignedToUserId = fixture.OwnerId }, fixture.Access(null, AppRoles.Admin)));
        var status = await Record.ExceptionAsync(() => fixture.Service.ChangeStatusAsync(
            ticket.Id, new() { StatusId = 2 }, fixture.Access(null, AppRoles.Admin)));
        var comment = await Record.ExceptionAsync(() => fixture.Service.AddCommentAsync(
            ticket.Id, new() { Content = "Works" }, fixture.Access()));
        Assert.Null(assignment);
        Assert.Null(status);
        Assert.Null(comment);
    }

    [Theory]
    [InlineData(AppRoles.Admin)]
    [InlineData(AppRoles.ItSupportAgent)]
    public async Task Assign_SupportRolesCreateTrimmedAssignment(string role)
    {
        await using var fixture = await Fixture.CreateAsync();
        var (ticket, _) = await fixture.AddTicketsAsync();
        var result = await fixture.Service.AssignAsync(ticket.Id,
            new() { AssignedToUserId = fixture.OwnerId, Note = "  escalation  " },
            fixture.Access(null, role));
        Assert.Equal(fixture.OwnerId, result.AssignedToUserId);
        var assignment = Assert.Single(result.AssignmentHistory);
        Assert.Equal(fixture.OwnerId, assignment.AssignedByUserId);
        Assert.Equal(fixture.Now.UtcDateTime, assignment.AssignedAtUtc);
        Assert.Equal("escalation", assignment.Reason);
        Assert.Equal(1, ticket.StatusId);
        Assert.Empty(result.StatusHistory);
    }

    [Theory]
    [InlineData(AppRoles.Employee)]
    [InlineData(AppRoles.Manager)]
    public async Task Assign_NonSupportRolesAreDenied(string role)
    {
        await using var fixture = await Fixture.CreateAsync();
        var (ticket, _) = await fixture.AddTicketsAsync();
        await Assert.ThrowsAsync<TicketAccessDeniedException>(() => fixture.Service.AssignAsync(
            ticket.Id, new() { AssignedToUserId = fixture.OtherId }, fixture.Access(null, role)));
    }

    [Fact]
    public async Task Assign_MultiRoleAndSameAssigneeAreIdempotent()
    {
        await using var fixture = await Fixture.CreateAsync();
        var (ticket, _) = await fixture.AddTicketsAsync();
        var access = fixture.Access(null, AppRoles.Employee, AppRoles.ItSupportAgent);
        await fixture.Service.AssignAsync(ticket.Id, new() { AssignedToUserId = fixture.OwnerId }, access);
        var result = await fixture.Service.AssignAsync(ticket.Id, new() { AssignedToUserId = fixture.OwnerId }, access);
        Assert.Single(result.AssignmentHistory);
        fixture.Notifications.Verify(x=>x.NotifyAssignmentAsync(ticket.Id,ticket.ReferenceNumber,fixture.OwnerId,fixture.OwnerId,It.IsAny<CancellationToken>()),Times.Once);
    }

    [Fact]
    public async Task Assign_ReassignmentPreservesAndEndsPrevious()
    {
        await using var fixture = await Fixture.CreateAsync();
        var (ticket, _) = await fixture.AddTicketsAsync();
        await fixture.Service.AssignAsync(ticket.Id, new() { AssignedToUserId = fixture.OwnerId }, fixture.Access(null, AppRoles.Admin));
        var result = await fixture.Service.AssignAsync(ticket.Id, new() { AssignedToUserId = fixture.OtherId }, fixture.Access(null, AppRoles.Admin));
        Assert.Equal(2, result.AssignmentHistory.Count);
        Assert.Single(result.AssignmentHistory, item => item.EndedAtUtc is not null);
        Assert.Single(result.AssignmentHistory, item => item.EndedAtUtc is null);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Assign_RejectsMissingOrInactiveTarget(bool inactive)
    {
        await using var fixture = await Fixture.CreateAsync();
        var (ticket, _) = await fixture.AddTicketsAsync();
        var target = inactive ? fixture.InactiveUserId : Guid.NewGuid();
        await Assert.ThrowsAsync<AssignmentTargetNotFoundException>(() => fixture.Service.AssignAsync(
            ticket.Id, new() { AssignedToUserId = target }, fixture.Access(null, AppRoles.Admin)));
    }

    [Theory]
    [InlineData(AppRoles.Admin)]
    [InlineData(AppRoles.ItSupportAgent)]
    public async Task ChangeStatus_SupportAppendsTrimmedHistory(string role)
    {
        await using var fixture = await Fixture.CreateAsync();
        var (ticket, _) = await fixture.AddTicketsAsync();
        var result = await fixture.Service.ChangeStatusAsync(ticket.Id,
            new() { StatusId = 2, Note = "  investigating  " }, fixture.Access(null, role));
        Assert.Equal(2, result.StatusId);
        var history = Assert.Single(result.StatusHistory);
        Assert.Equal((short?)1, history.FromStatusId);
        Assert.Equal((short)2, history.ToStatusId);
        Assert.Equal(fixture.OwnerId, history.ChangedByUserId);
        Assert.Equal(fixture.Now.UtcDateTime, history.ChangedAtUtc);
        Assert.Equal("investigating", history.Reason);
        Assert.Equal(ticket.AssignedToUserId, result.AssignedToUserId);
        Assert.Empty(result.AssignmentHistory);
    }

    [Theory]
    [InlineData(AppRoles.Employee)]
    [InlineData(AppRoles.Manager)]
    public async Task ChangeStatus_NonSupportRolesAreDenied(string role)
    {
        await using var fixture = await Fixture.CreateAsync();
        var (ticket, _) = await fixture.AddTicketsAsync();
        await Assert.ThrowsAsync<TicketAccessDeniedException>(() => fixture.Service.ChangeStatusAsync(
            ticket.Id, new() { StatusId = 2 }, fixture.Access(null, role)));
    }

    [Fact]
    public async Task ChangeStatus_SameStatusIsIdempotent()
    {
        await using var fixture = await Fixture.CreateAsync();
        var (ticket, _) = await fixture.AddTicketsAsync();
        var result = await fixture.Service.ChangeStatusAsync(ticket.Id, new() { StatusId = 1 }, fixture.Access(null, AppRoles.Admin));
        Assert.Empty(result.StatusHistory);
        Assert.Equal(ticket.UpdatedAtUtc, result.UpdatedAtUtc);
        fixture.Notifications.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ChangeStatus_TerminalReopenRequiresAdminAndClearsTimestamp()
    {
        await using var fixture = await Fixture.CreateAsync();
        var (ticket, _) = await fixture.AddTicketsAsync(terminal: true);
        ticket.ClosedAtUtc = fixture.Now.UtcDateTime.AddDays(-1);
        await fixture.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<TicketStateConflictException>(() => fixture.Service.ChangeStatusAsync(
            ticket.Id, new() { StatusId = 1 }, fixture.Access(null, AppRoles.ItSupportAgent)));
        var result = await fixture.Service.ChangeStatusAsync(ticket.Id, new() { StatusId = 1 }, fixture.Access(null, AppRoles.Admin));
        Assert.Null(result.ClosedAtUtc);
    }

    [Fact]
    public async Task ChangeStatus_EnteringClosedSetsTimestamp()
    {
        await using var fixture = await Fixture.CreateAsync();
        var (ticket, _) = await fixture.AddTicketsAsync();
        var result = await fixture.Service.ChangeStatusAsync(ticket.Id, new() { StatusId = 5 }, fixture.Access(null, AppRoles.Admin));
        Assert.Equal(fixture.Now.UtcDateTime, result.ClosedAtUtc);
    }

    [Theory]
    [InlineData(AppRoles.Employee)]
    [InlineData(AppRoles.Manager)]
    public async Task AddComment_OwnerAddsTrimmedPublicComment(string role)
    {
        await using var fixture = await Fixture.CreateAsync();
        var (ticket, _) = await fixture.AddTicketsAsync();
        var result = await fixture.Service.AddCommentAsync(ticket.Id, new() { Content = "  update  " }, fixture.Access(null, role));
        Assert.Equal("update", result.Body);
        Assert.Equal(TicketCommentVisibilities.Public, result.Visibility);
        Assert.Equal(fixture.OwnerId, result.AuthorUserId);
        Assert.Equal("Owner User", result.AuthorDisplayName);
        fixture.Notifications.Verify(x=>x.NotifyCommentAddedAsync(ticket.Id,ticket.ReferenceNumber,ticket.CreatedByUserId,ticket.AssignedToUserId,fixture.OwnerId,false,It.IsAny<CancellationToken>()),Times.Once);
        Assert.Equal(fixture.Now.UtcDateTime, result.CreatedAtUtc);
    }

    [Theory]
    [InlineData(AppRoles.Admin)]
    [InlineData(AppRoles.ItSupportAgent)]
    public async Task AddComment_SupportAddsInternalToAnyTicket(string role)
    {
        await using var fixture = await Fixture.CreateAsync();
        var (_, other) = await fixture.AddTicketsAsync();
        var result = await fixture.Service.AddCommentAsync(other.Id, new() { Content = "internal", IsInternal = true }, fixture.Access(null, role));
        Assert.Equal(TicketCommentVisibilities.Internal, result.Visibility);
    }

    [Theory]
    [InlineData(AppRoles.Employee)]
    [InlineData(AppRoles.Manager)]
    public async Task AddComment_InaccessibleTicketIsHidden(string role)
    {
        await using var fixture = await Fixture.CreateAsync();
        var (_, other) = await fixture.AddTicketsAsync();
        await Assert.ThrowsAsync<TicketNotFoundException>(() => fixture.Service.AddCommentAsync(
            other.Id, new() { Content = "hello" }, fixture.Access(null, role)));
    }

    [Fact]
    public async Task AddComment_InternalAndTerminalRulesAreEnforced()
    {
        await using var fixture = await Fixture.CreateAsync();
        var (ticket, _) = await fixture.AddTicketsAsync(terminal: true);
        await Assert.ThrowsAsync<TicketAccessDeniedException>(() => fixture.Service.AddCommentAsync(
            ticket.Id, new() { Content = "internal", IsInternal = true }, fixture.Access()));
        await Assert.ThrowsAsync<TicketStateConflictException>(() => fixture.Service.AddCommentAsync(
            ticket.Id, new() { Content = "public" }, fixture.Access()));
        var result = await fixture.Service.AddCommentAsync(ticket.Id, new() { Content = "support" }, fixture.Access(null, AppRoles.Admin));
        Assert.Equal("support", result.Body);
        Assert.Equal(5, ticket.StatusId);
        Assert.Equal(fixture.OtherId, ticket.AssignedToUserId);
    }

    [Fact]
    public async Task AddComment_BlankAndInactiveAuthorAreRejected()
    {
        await using var fixture = await Fixture.CreateAsync();
        var (ticket, _) = await fixture.AddTicketsAsync();
        await Assert.ThrowsAsync<TicketValidationException>(() => fixture.Service.AddCommentAsync(
            ticket.Id, new() { Content = "  " }, fixture.Access()));
        await Assert.ThrowsAsync<TicketAccessDeniedException>(() => fixture.Service.AddCommentAsync(
            ticket.Id, new() { Content = "hello" }, fixture.Access(fixture.InactiveUserId, AppRoles.Admin)));
    }

    [Theory]
    [InlineData(AppRoles.Admin)] [InlineData(AppRoles.ItSupportAgent)] [InlineData(AppRoles.Employee)] [InlineData(AppRoles.Manager)]
    public async Task Cancel_EligibleCaller_SetsTimestamp_PreservesStatusAndHistory_EndsAssignment(string role)
    {
        await using var fixture=await Fixture.CreateAsync();var(ticket,_)=await fixture.AddTicketsAsync();await fixture.AddHistoryAsync(ticket);var status=ticket.StatusId;
        var result=await fixture.Service.CancelAsync(ticket.Id,new(){Reason="  not persisted  "},fixture.Access(fixture.OwnerId,role));
        Assert.Equal(fixture.Now.UtcDateTime,result.CancelledAtUtc);Assert.Equal(fixture.Now.UtcDateTime,result.UpdatedAtUtc);Assert.Equal(status,result.StatusId);
        Assert.Single(result.Comments);Assert.Single(result.Attachments);Assert.Single(result.AssignmentHistory);Assert.Single(result.StatusHistory);
        Assert.Equal(fixture.Now.UtcDateTime,result.AssignmentHistory[0].EndedAtUtc);Assert.Equal(fixture.OwnerId,result.AssignmentHistory[0].EndedByUserId);Assert.Null(result.AssignedToUserId);
    }

    [Theory] [InlineData(AppRoles.Employee)] [InlineData(AppRoles.Manager)]
    public async Task Cancel_NonSupportCannotCancelOtherOrTerminalTicket(string role)
    { await using var fixture=await Fixture.CreateAsync();var(owner,other)=await fixture.AddTicketsAsync(terminal:true);await Assert.ThrowsAsync<TicketNotFoundException>(()=>fixture.Service.CancelAsync(other.Id,new(),fixture.Access(fixture.OwnerId,role)));await Assert.ThrowsAsync<TicketStateConflictException>(()=>fixture.Service.CancelAsync(owner.Id,new(),fixture.Access(fixture.OwnerId,role))); }

    [Theory] [InlineData(AppRoles.Admin)] [InlineData(AppRoles.ItSupportAgent)]
    public async Task Cancel_SupportMayCancelTerminalTicket_AndRepeatedCallIsIdempotent(string role)
    { await using var fixture=await Fixture.CreateAsync();var(ticket,_)=await fixture.AddTicketsAsync(terminal:true);var access=fixture.Access(fixture.OwnerId,role);var first=await fixture.Service.CancelAsync(ticket.Id,new(),access);var second=await fixture.Service.CancelAsync(ticket.Id,new(),access);Assert.Equal(first.CancelledAtUtc,second.CancelledAtUtc);Assert.Equal((short)5,second.StatusId);fixture.Notifications.Verify(x=>x.NotifyTicketCancelledAsync(ticket.Id,ticket.ReferenceNumber,ticket.CreatedByUserId,fixture.OtherId,fixture.OwnerId,It.IsAny<CancellationToken>()),Times.Once); }

    [Fact]
    public async Task NotificationFailure_DoesNotRollBackPersistedTicketEvents()
    {
        await using var fixture=await Fixture.CreateAsync();var(ticket,_)=await fixture.AddTicketsAsync();
        fixture.Notifications.Setup(x=>x.NotifyStatusChangedAsync(It.IsAny<Guid>(),It.IsAny<string>(),It.IsAny<Guid>(),It.IsAny<Guid>(),It.IsAny<string>(),It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("notification unavailable"));
        var result=await fixture.Service.ChangeStatusAsync(ticket.Id,new(){StatusId=2},fixture.Access(null,AppRoles.Admin));
        Assert.Equal(2,result.StatusId);Assert.Single(result.StatusHistory);
    }

    [Fact]
    public async Task CancelledTicket_BlocksMutations_ButSupportMayComment()
    { await using var fixture=await Fixture.CreateAsync();var(ticket,_)=await fixture.AddTicketsAsync();var admin=fixture.Access(fixture.OwnerId,AppRoles.Admin);await fixture.Service.CancelAsync(ticket.Id,new(),admin);await Assert.ThrowsAsync<TicketStateConflictException>(()=>fixture.Service.UpdateAsync(ticket.Id,ValidUpdate(),admin));await Assert.ThrowsAsync<TicketStateConflictException>(()=>fixture.Service.AssignAsync(ticket.Id,new(){AssignedToUserId=fixture.OtherId},admin));await Assert.ThrowsAsync<TicketStateConflictException>(()=>fixture.Service.ChangeStatusAsync(ticket.Id,new(){StatusId=2},admin));await Assert.ThrowsAsync<TicketStateConflictException>(()=>fixture.Service.AddCommentAsync(ticket.Id,new(){Content="employee"},fixture.Access()));var comment=await fixture.Service.AddCommentAsync(ticket.Id,new(){Content="audit"},admin);Assert.Equal("audit",comment.Body); }

    private static CreateTicketRequest ValidCreate(string title = "Title", short categoryId = 1, short priorityId = 1) =>
        new() { Title = title, Description = "Description", CategoryId = categoryId, PriorityId = priorityId };
    private static UpdateTicketRequest ValidUpdate() =>
        new() { Title = "Updated", Description = "Updated description", CategoryId = 1, PriorityId = 1 };

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private Fixture(SqliteConnection connection, ApplicationDbContext db, FixedTimeProvider time)
        {
            _connection = connection;
            Db = db;
            Now = time.GetUtcNow();
            Notifications = new Mock<ITicketNotificationService>();
            Service = new TicketService(db, new TicketNumberGenerator(time), time, NullLogger<TicketService>.Instance,
                Notifications.Object);
        }

        public ApplicationDbContext Db { get; }
        public TicketService Service { get; }
        public Mock<ITicketNotificationService> Notifications { get; }
        public DateTimeOffset Now { get; }
        public Guid OwnerId { get; } = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        public Guid OtherId { get; } = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        public Guid InactiveUserId { get; } = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

        public static async Task<Fixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            await TicketSqliteDatabase.InitializeAsync(connection);
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
            var db = new ApplicationDbContext(options);
            var time = new FixedTimeProvider(new DateTimeOffset(2026, 7, 30, 15, 30, 45, TimeSpan.Zero));
            var fixture = new Fixture(connection, db, time);
            db.Users.AddRange(
                User(fixture.OwnerId, "Owner User", true),
                User(fixture.OtherId, "Other User", true),
                User(fixture.InactiveUserId, "Inactive User", false));
            await db.SaveChangesAsync();
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE \"Users\" SET \"IsActive\" = 0 WHERE \"Id\" = {fixture.InactiveUserId}");
            var adminRoleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var supportRoleId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO \"UserRoles\" VALUES ({fixture.OwnerId}, {adminRoleId}, {time.GetUtcNow().UtcDateTime}, NULL)");
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO \"UserRoles\" VALUES ({fixture.OtherId}, {supportRoleId}, {time.GetUtcNow().UtcDateTime}, NULL)");
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO \"UserRoles\" VALUES ({fixture.InactiveUserId}, {supportRoleId}, {time.GetUtcNow().UtcDateTime}, NULL)");
            db.ChangeTracker.Clear();
            db.Categories.Single(x => x.Id == 2).IsActive = false;
            db.Priorities.Single(x => x.Id == 2).IsActive = false;
            await db.SaveChangesAsync();
            return fixture;
        }

        public TicketAccessContext Access(Guid? userId = null, params string[] roles) =>
            new() { UserId = userId ?? OwnerId, Roles = roles.Length == 0 ? [AppRoles.Employee] : roles };

        public async Task<(Ticket Owner, Ticket Other)> AddTicketsAsync(bool terminal = false)
        {
            var created = Now.UtcDateTime.AddMinutes(-1);
            var owner = Ticket(OwnerId, "TKT-OWNER", "Printer issue", created, terminal ? (short)5 : (short)1);
            owner.AssignedToUserId = OtherId;
            var other = Ticket(OtherId, "TKT-OTHER", "Network issue", created.AddSeconds(1), 1);
            Db.Tickets.AddRange(owner, other);
            await Db.SaveChangesAsync();
            return (owner, other);
        }

        public async Task AddHistoryAsync(Ticket ticket)
        {
            Db.TicketComments.Add(new TicketComment { Id = Guid.NewGuid(), TicketId = ticket.Id, AuthorUserId = OwnerId, Body = "Comment", Visibility = "Public", CreatedAtUtc = Now.UtcDateTime });
            Db.TicketAttachments.Add(new TicketAttachment { Id = Guid.NewGuid(), TicketId = ticket.Id, UploadedByUserId = OwnerId, OriginalFileName = "file.txt", ContentType = "text/plain", SizeBytes = 1, StorageProvider = "test", StorageKey = "secret", CreatedAtUtc = Now.UtcDateTime });
            Db.TicketAssignments.Add(new TicketAssignment { Id = Guid.NewGuid(), TicketId = ticket.Id, AssignedToUserId = OtherId, AssignedByUserId = OwnerId, AssignedAtUtc = Now.UtcDateTime });
            Db.TicketStatusHistory.Add(new TicketStatusHistory { Id = Guid.NewGuid(), TicketId = ticket.Id, FromStatusId = null, ToStatusId = 1, ChangedByUserId = OwnerId, ChangedAtUtc = Now.UtcDateTime });
            await Db.SaveChangesAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await _connection.DisposeAsync();
        }

        private static User User(Guid id, string displayName, bool active) => new()
        {
            Id = id, UserName = $"{id}@test", NormalizedUserName = $"{id}@TEST",
            Email = $"{id}@test", NormalizedEmail = $"{id}@TEST", DisplayName = displayName,
            IsActive = active, SecurityStamp = Guid.NewGuid().ToString(), ConcurrencyStamp = Guid.NewGuid().ToString(),
            CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow
        };

        private static Ticket Ticket(Guid creator, string number, string title, DateTime created, short status) => new()
        {
            Id = Guid.NewGuid(), ReferenceNumber = number, Title = title, Description = "Description",
            CategoryId = 1, PriorityId = 1, StatusId = status, CreatedByUserId = creator,
            CreatedAtUtc = created, UpdatedAtUtc = created
        };
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }
}
