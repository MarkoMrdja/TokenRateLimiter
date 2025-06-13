using Microsoft.Extensions.DependencyInjection;
using TokenRateLimiter.Core.Abstractions;
using TokenRateLimiter.Core.Extensions;
using TokenRateLimiter.Core.Options;
using TokenRateLimiter.Tiktoken.Estimators;

namespace TokenRateLimiter.Tiktoken.Extensions;

public static class TiktokenServiceCollectionExtensions
{
    /// <summary>
    /// Adds token rate limiting with Tiktoken estimator for OpenAI/Azure OpenAI compatibility.
    /// </summary>
    public static IServiceCollection AddTokenRateLimiterWithTiktoken(this IServiceCollection services,
        Action<TokenRateLimiterOptions> configureOptions)
    {
        services.AddTokenRateLimiter(configureOptions);
        services.AddSingleton<ITokenEstimator, TiktokenEstimator>();

        return services;
    }

    /// <summary>
    /// Adds token rate limiting with character-based estimator as fallback.
    /// </summary>
    public static IServiceCollection AddTokenRateLimiterWithCharacterEstimator(this IServiceCollection services,
        Action<TokenRateLimiterOptions> configureOptions,
        double charactersPerToken = 3.5)
    {
        services.AddTokenRateLimiter(configureOptions);
        services.AddSingleton<ITokenEstimator>(provider => new CharacterBasedEstimator(charactersPerToken));

        return services;
    }
}