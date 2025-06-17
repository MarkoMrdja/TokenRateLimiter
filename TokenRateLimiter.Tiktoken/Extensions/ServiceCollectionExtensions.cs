using Microsoft.Extensions.DependencyInjection;
using Tiktoken;
using Tiktoken.Encodings;
using TokenRateLimiter.Core.Abstractions;
using TokenRateLimiter.Core.Extensions;
using TokenRateLimiter.Core.Options;
using TokenRateLimiter.Tiktoken.Estimators;

namespace TokenRateLimiter.Tiktoken.Extensions;

public static class TiktokenServiceCollectionExtensions
{
    /// <summary>
    /// Adds token rate limiting with Tiktoken estimator using default Azure OpenAI settings.
    /// Default: 1M tokens/minute, 32K max output tokens.
    /// </summary>
    public static IServiceCollection AddTokenRateLimiterWithTiktoken(this IServiceCollection services)
    {
        return services.AddTokenRateLimiterWithTiktoken(options =>
        {
            // Use all defaults from TokenRateLimiterOptions
        });
    }

    /// <summary>
    /// Adds token rate limiting with Tiktoken estimator and custom configuration.
    /// </summary>
    public static IServiceCollection AddTokenRateLimiterWithTiktoken(this IServiceCollection services,
        Action<TokenRateLimiterOptions> configureOptions)
    {
        services.AddTokenRateLimiter(configureOptions);
        services.AddSingleton<ITokenEstimator, TiktokenEstimator>();
        return services;
    }

    /// <summary>
    /// Adds token rate limiting with Tiktoken estimator and custom max output tokens.
    /// </summary>
    public static IServiceCollection AddTokenRateLimiterWithTiktoken(this IServiceCollection services,
        int maxOutputTokens)
    {
        services.AddTokenRateLimiter(options => { }); // Use defaults
        services.AddSingleton<ITokenEstimator>(provider => new TiktokenEstimator(
            new Encoder(new O200KBase()),
            maxOutputTokens));
        return services;
    }

    /// <summary>
    /// Adds token rate limiting with Tiktoken estimator, custom configuration and max output tokens.
    /// </summary>
    public static IServiceCollection AddTokenRateLimiterWithTiktoken(this IServiceCollection services,
        Action<TokenRateLimiterOptions> configureOptions,
        int maxOutputTokens)
    {
        services.AddTokenRateLimiter(configureOptions);
        services.AddSingleton<ITokenEstimator>(provider => new TiktokenEstimator(
            new Encoder(new O200KBase()),
            maxOutputTokens));
        return services;
    }

    /// <summary>
    /// Adds token rate limiting with character-based estimator using defaults.
    /// Default: 3.5 chars/token, 32K max output tokens.
    /// </summary>
    public static IServiceCollection AddTokenRateLimiterWithCharacterEstimator(this IServiceCollection services)
    {
        return services.AddTokenRateLimiterWithCharacterEstimator(options => { });
    }

    /// <summary>
    /// Adds token rate limiting with character-based estimator and custom configuration.
    /// </summary>
    public static IServiceCollection AddTokenRateLimiterWithCharacterEstimator(this IServiceCollection services,
        Action<TokenRateLimiterOptions> configureOptions,
        double charactersPerToken = 3.5)
    {
        services.AddTokenRateLimiter(configureOptions);
        services.AddSingleton<ITokenEstimator>(provider =>
            new CharacterBasedEstimator(charactersPerToken));
        return services;
    }

    /// <summary>
    /// Adds token rate limiting with character-based estimator and custom model settings.
    /// </summary>
    public static IServiceCollection AddTokenRateLimiterWithCharacterEstimator(this IServiceCollection services,
        double charactersPerToken,
        int maxOutputTokens)
    {
        services.AddTokenRateLimiter(options => { }); // Use defaults
        services.AddSingleton<ITokenEstimator>(provider =>
            new CharacterBasedEstimator(charactersPerToken, maxOutputTokens));
        return services;
    }

    /// <summary>
    /// Adds token rate limiting with character-based estimator, custom configuration and model settings.
    /// </summary>
    public static IServiceCollection AddTokenRateLimiterWithCharacterEstimator(this IServiceCollection services,
        Action<TokenRateLimiterOptions> configureOptions,
        double charactersPerToken,
        int maxOutputTokens)
    {
        services.AddTokenRateLimiter(configureOptions);
        services.AddSingleton<ITokenEstimator>(provider =>
            new CharacterBasedEstimator(charactersPerToken, maxOutputTokens));
        return services;
    }
}