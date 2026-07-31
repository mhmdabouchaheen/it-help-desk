namespace HelpDesk.Api.Contracts.Common;

/// <summary>Represents one page of application results.</summary>
public sealed class PagedResponse<T>
{
    /// <summary>Gets the items in this page.</summary>
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    /// <summary>Gets the one-based page number.</summary>
    public int PageNumber { get; init; }
    /// <summary>Gets the requested page size.</summary>
    public int PageSize { get; init; }
    /// <summary>Gets the total number of matching items.</summary>
    public int TotalCount { get; init; }
    /// <summary>Gets the total number of pages.</summary>
    public int TotalPages { get; init; }
    /// <summary>Gets whether an earlier page exists.</summary>
    public bool HasPreviousPage { get; init; }
    /// <summary>Gets whether a later page exists.</summary>
    public bool HasNextPage { get; init; }
}
