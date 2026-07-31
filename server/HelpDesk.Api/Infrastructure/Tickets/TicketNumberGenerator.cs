using System.Globalization;
using System.Security.Cryptography;
using HelpDesk.Api.Application.Tickets;

namespace HelpDesk.Api.Infrastructure.Tickets;

/// <summary>Generates bounded-length ticket numbers safe for concurrent callers.</summary>
public sealed class TicketNumberGenerator(TimeProvider timeProvider) : ITicketNumberGenerator
{
    /// <inheritdoc />
    public string Generate()
    {
        Span<byte> random = stackalloc byte[4];
        RandomNumberGenerator.Fill(random);
        return string.Create(
            CultureInfo.InvariantCulture,
            $"TKT-{timeProvider.GetUtcNow():yyyyMMdd-HHmmss}-{Convert.ToHexString(random)}");
    }
}
