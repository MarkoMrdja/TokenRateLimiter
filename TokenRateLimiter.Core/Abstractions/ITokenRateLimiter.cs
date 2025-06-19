﻿using TokenRateLimiter.Core.Models;
using TokenRateLimiter.Core.Options;

namespace TokenRateLimiter.Core.Abstractions;

public interface ITokenRateLimiter
{
    /// <summary>
    /// Reserves tokens for an LLM request
    /// </summary>
    /// <param name="inputTokens">Actual input tokens (prompt + system message)</param>
    /// <param name="estimatedOutputTokens">Expected output tokens (0 = use default estimation)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Token reservation that must be disposed after use</returns>
    Task<TokenReservation> ReserveTokensAsync(int inputTokens, int estimatedOutputTokens = 0, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets current token usage within the time window
    /// </summary>
    int GetCurrentUsage();
    
    /// <summary>
    /// Gets currently reserved tokens (not yet used)
    /// </summary>
    int GetReservedTokens();
    
    /// <summary>
    /// Gets comprehensive usage statistics
    /// </summary>
    TokenUsageStats GetUsageStats();
}
