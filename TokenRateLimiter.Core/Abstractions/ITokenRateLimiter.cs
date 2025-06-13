using TokenRateLimiter.Core.Models;

namespace TokenRateLimiter.Core.Abstractions;

public interface ITokenRateLimiter
{
    Task<TokenReservation> ReserveTokensAsync(int estimatedTokens, CancellationToken cancellationToken = default);
    int GetCurrentUsage();
    int GetReservedTokens();
}
