using System.ComponentModel.DataAnnotations;

namespace HelpDesk.Api.Contracts.Common;

/// <summary>Defines validated pagination inputs.</summary>
public class PagedRequest
{
    /// <summary>Gets the one-based page number.</summary>
    [Range(1, int.MaxValue)]
    public int PageNumber { get; init; } = 1;

    /// <summary>Gets the number of items requested per page.</summary>
    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}
