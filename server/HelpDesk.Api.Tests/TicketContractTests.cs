using System.ComponentModel.DataAnnotations;
using System.Reflection;
using HelpDesk.Api.Application.Authorization;
using HelpDesk.Api.Application.Tickets;
using HelpDesk.Api.Contracts.Common;
using HelpDesk.Api.Contracts.Tickets;
using TicketAllowedValuesAttribute = HelpDesk.Api.Contracts.Common.Validation.AllowedValuesAttribute;

namespace HelpDesk.Api.Tests;

public class TicketContractTests
{
    [Fact]
    public void Create_RequiresTitle() => AssertInvalid(new CreateTicketRequest { Description = "D", CategoryId = 1, PriorityId = 1 }, nameof(CreateTicketRequest.Title));

    [Fact]
    public void Create_RequiresDescription() => AssertInvalid(new CreateTicketRequest { Title = "T", CategoryId = 1, PriorityId = 1 }, nameof(CreateTicketRequest.Description));

    [Fact]
    public void Create_RejectsOversizedTitle() => AssertInvalid(ValidCreate(title: new string('x', 251)), nameof(CreateTicketRequest.Title));

    [Fact]
    public void Create_RejectsInvalidCategory() => AssertInvalid(ValidCreate(categoryId: 0), nameof(CreateTicketRequest.CategoryId));

    [Fact]
    public void Create_RejectsInvalidPriority() => AssertInvalid(ValidCreate(priorityId: 0), nameof(CreateTicketRequest.PriorityId));

    [Fact]
    public void Update_UsesSameTitleLimit() =>
        AssertInvalid(new UpdateTicketRequest { Title = new string('x', 251), Description = "D", CategoryId = 1, PriorityId = 1 }, nameof(UpdateTicketRequest.Title));

    [Fact]
    public void List_DefaultsPageNumber() => Assert.Equal(1, new TicketListRequest().PageNumber);

    [Fact]
    public void List_DefaultsPageSize() => Assert.Equal(20, new TicketListRequest().PageSize);

    [Fact]
    public void List_RejectsPageSizeOver100() => AssertInvalid(new TicketListRequest { PageSize = 101 }, nameof(PagedRequest.PageSize));

    [Fact]
    public void List_RejectsPageNumberBelowOne() => AssertInvalid(new TicketListRequest { PageNumber = 0 }, nameof(PagedRequest.PageNumber));

    [Fact]
    public void List_RejectsUnsupportedSortField() => AssertInvalid(new TicketListRequest { SortBy = "Secret" }, nameof(TicketListRequest.SortBy));

    [Fact]
    public void List_RejectsUnsupportedDirection() => AssertInvalid(new TicketListRequest { SortDirection = "sideways" }, nameof(TicketListRequest.SortDirection));

    [Theory]
    [InlineData(TicketSortFields.CreatedAtUtc)]
    [InlineData(TicketSortFields.UpdatedAtUtc)]
    [InlineData(TicketSortFields.TicketNumber)]
    [InlineData(TicketSortFields.Priority)]
    [InlineData(TicketSortFields.Status)]
    [InlineData(TicketSortFields.Title)]
    public void List_AcceptsSortFields(string field) => AssertValid(new TicketListRequest { SortBy = field });

    [Theory]
    [InlineData(SortDirections.Ascending)]
    [InlineData(SortDirections.Descending)]
    public void List_AcceptsDirections(string direction) => AssertValid(new TicketListRequest { SortDirection = direction });

    [Fact]
    public void Assign_RejectsEmptyUserId() => AssertInvalid(new AssignTicketRequest(), nameof(AssignTicketRequest.AssignedToUserId));

    [Fact]
    public void ChangeStatus_RejectsInvalidStatus() => AssertInvalid(new ChangeTicketStatusRequest(), nameof(ChangeTicketStatusRequest.StatusId));

    [Fact]
    public void AddComment_RequiresContent() => AssertInvalid(new AddTicketCommentRequest(), nameof(AddTicketCommentRequest.Content));

    [Fact]
    public void AddComment_DoesNotInventUnconfiguredMaximum() =>
        AssertValid(new AddTicketCommentRequest { Content = new string('x', 10_000) });

    [Fact]
    public void PagedResponse_InitializesItems() => Assert.Empty(new PagedResponse<object>().Items);

    [Fact]
    public void Detail_InitializesCollections()
    {
        var response = new TicketDetailResponse();
        Assert.Empty(response.Comments);
        Assert.Empty(response.Attachments);
        Assert.Empty(response.AssignmentHistory);
        Assert.Empty(response.StatusHistory);
    }

    [Fact]
    public void AccessContext_InitializesRoles() => Assert.Empty(new TicketAccessContext().Roles);

    [Theory]
    [InlineData("PasswordHash")]
    [InlineData("SecurityStamp")]
    [InlineData("ConcurrencyStamp")]
    [InlineData("RefreshToken")]
    public void PublicResponses_ExcludeSecurityProperties(string propertyName) =>
        Assert.All(ResponseTypes(), type => Assert.Null(type.GetProperty(propertyName)));

    [Theory]
    [InlineData("StoragePath")]
    [InlineData("StorageKey")]
    [InlineData("StorageProvider")]
    [InlineData("ContentHash")]
    public void Attachment_ExcludesStorageProperties(string propertyName) =>
        Assert.Null(typeof(TicketAttachmentResponse).GetProperty(propertyName));

    [Fact]
    public void TicketService_DoesNotExposeEntities()
    {
        var types = ServiceSurfaceTypes();
        Assert.DoesNotContain(types, type => type.Namespace == "HelpDesk.Api.Entities");
    }

    [Fact]
    public void TicketService_DoesNotExposeQueryable() =>
        Assert.DoesNotContain(ServiceSurfaceTypes(), type =>
            type == typeof(IQueryable) || type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IQueryable<>));

    [Fact]
    public void TicketService_HasNoHttpOrClaimsDependencies() =>
        Assert.DoesNotContain(ServiceSurfaceTypes(), type =>
            type.FullName is "Microsoft.AspNetCore.Http.HttpContext" or "System.Security.Claims.ClaimsPrincipal");

    [Fact]
    public void LookupResponses_ExposeOnlyExpectedFields()
    {
        AssertProperties<TicketCategoryResponse>("Id", "Name", "Description", "SortOrder", "IsActive");
        AssertProperties<TicketPriorityResponse>("Id", "Name", "Description", "Rank", "IsActive");
        AssertProperties<TicketStatusResponse>("Id", "Name", "Description", "SortOrder", "IsTerminal", "IsActive");
    }

    [Fact]
    public void TicketContracts_DoNotDuplicateRoleStrings()
    {
        var constants = new[] { AppRoles.Admin, AppRoles.ItSupportAgent, AppRoles.Employee, AppRoles.Manager };
        var defaults = typeof(TicketListRequest).Assembly.GetTypes()
            .Where(type => type.Namespace == "HelpDesk.Api.Contracts.Tickets")
            .SelectMany(type => type.GetFields(BindingFlags.Public | BindingFlags.Static))
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (string?)field.GetRawConstantValue());
        Assert.DoesNotContain(defaults, constants.Contains);
    }

    [Fact]
    public void ContractValidation_DoesNotRequireDatabase() => AssertValid(ValidCreate());

    [Fact]
    public void AllowedValues_AcceptsValue() => Assert.True(new TicketAllowedValuesAttribute("one").IsValid("one"));

    [Fact]
    public void AllowedValues_RejectsValue() => Assert.False(new TicketAllowedValuesAttribute("one").IsValid("two"));

    [Fact]
    public void AllowedValues_AllowsNullForOptionalFields() => Assert.True(new TicketAllowedValuesAttribute("one").IsValid(null));

    [Fact]
    public void AllowedValues_IsCaseInsensitive() => Assert.True(new TicketAllowedValuesAttribute("one").IsValid("ONE"));

    [Fact]
    public void AllowedValues_HasReadableMessage() => Assert.Contains("one", new TicketAllowedValuesAttribute("one").FormatErrorMessage("Value"));

    private static CreateTicketRequest ValidCreate(
        string title = "T",
        short categoryId = 1,
        short priorityId = 1) =>
        new() { Title = title, Description = "D", CategoryId = categoryId, PriorityId = priorityId };

    private static void AssertValid(object instance) => Assert.Empty(Validate(instance));

    private static void AssertInvalid(object instance, string member) =>
        Assert.Contains(Validate(instance), result => result.MemberNames.Contains(member));

    private static List<ValidationResult> Validate(object instance)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(instance, new ValidationContext(instance), results, true);
        return results;
    }

    private static Type[] ResponseTypes() =>
    [
        typeof(TicketSummaryResponse), typeof(TicketDetailResponse), typeof(TicketCommentResponse),
        typeof(TicketAttachmentResponse), typeof(TicketAssignmentResponse),
        typeof(TicketStatusHistoryResponse), typeof(TicketCategoryResponse),
        typeof(TicketPriorityResponse), typeof(TicketStatusResponse)
    ];

    private static IEnumerable<Type> ServiceSurfaceTypes() =>
        typeof(ITicketService).GetMethods()
            .SelectMany(method => method.GetParameters().Select(parameter => parameter.ParameterType).Append(method.ReturnType))
            .SelectMany(Flatten);

    private static IEnumerable<Type> Flatten(Type type)
    {
        yield return type;
        foreach (var argument in type.IsGenericType ? type.GetGenericArguments() : Type.EmptyTypes)
            foreach (var nested in Flatten(argument))
                yield return nested;
    }

    private static void AssertProperties<T>(params string[] names) =>
        Assert.Equal(names.Order(), typeof(T).GetProperties().Select(property => property.Name).Order());
}
