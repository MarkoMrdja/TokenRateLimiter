using TokenRateLimiter.Core.Abstractions;

namespace TokenRateLimiter.Core.Extensions;

public static class TokenRateLimiterExtensions
{
    public static async Task<T> ExecuteAsync<T>(this ITokenRateLimiter rateLimiter,
        int estimatedTokens,
        Func<Task<(T Result, int ActualTokens)>> operation,
        CancellationToken cancellationToken = default)
    {
        using var reservation = await rateLimiter.ReserveTokensAsync(estimatedTokens, cancellationToken);

        var (result, actualTokens) = await operation();
        await reservation.CompleteAsync(actualTokens);

        return result;
    }

    public static async Task<T> ExecuteAsync<T>(this ITokenRateLimiter rateLimiter,
        ITokenEstimator estimator,
        string inputText,
        Func<Task<(T Result, int ActualTokens)>> operation,
        CancellationToken cancellationToken = default)
    {
        var estimation = estimator.EstimateTotalTokens(inputText);
        return await rateLimiter.ExecuteAsync(estimation.EstimatedTotal, operation, cancellationToken);
    }
}
