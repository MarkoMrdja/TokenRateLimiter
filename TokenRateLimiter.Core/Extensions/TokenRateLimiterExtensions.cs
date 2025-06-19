using TokenRateLimiter.Core.Abstractions;

namespace TokenRateLimiter.Core.Extensions;

public static class TokenRateLimiterExtensions
{
    public static async Task<T> ExecuteAsync<T>(this ITokenRateLimiter rateLimiter,
        int inputTokens,
        Func<Task<(T Result, int ActualTokens)>> operation,
        int estimatedOutputTokens = 0,
        CancellationToken cancellationToken = default)
    {
        await using var reservation = await rateLimiter.ReserveTokensAsync(inputTokens, estimatedOutputTokens, cancellationToken);

        var (result, actualTokens) = await operation();
        reservation.RecordActualUsage(actualTokens);
        return result;
    }

    public static async Task<T> ExecuteAsync<T>(this ITokenRateLimiter rateLimiter,
        ITokenEstimator estimator,
        string inputText,
        Func<Task<(T Result, int ActualTokens)>> operation,
        CancellationToken cancellationToken = default)
    {
        var inputTokens = estimator.EstimateTokens(inputText);
        return await rateLimiter.ExecuteAsync(inputTokens, operation, cancellationToken: cancellationToken);
    }
}
