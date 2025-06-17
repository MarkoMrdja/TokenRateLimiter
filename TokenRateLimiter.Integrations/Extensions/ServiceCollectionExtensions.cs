using Microsoft.Extensions.DependencyInjection;
using TokenRateLimiter.Integrations.Configuration;
using TokenRateLimiter.Tiktoken.Extensions;

namespace TokenRateLimiter.Integrations.Extensions;

/// <summary>
/// Simple extension methods for setting up rate limiting with Azure OpenAI.
/// These just configure the DI container - no wrappers or complex abstractions.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds token rate limiting optimized for Azure OpenAI usage.
    /// This configures sensible defaults for Azure OpenAI rate limits.
    /// </summary>
    public static IServiceCollection AddAzureOpenAIRateLimiting(
        this IServiceCollection services,
        Action<AzureOpenAISetupOptions>? configure = null)
    {
        var options = new AzureOpenAISetupOptions();
        configure?.Invoke(options);

        // Add rate limiting with Tiktoken estimator (perfect for Azure OpenAI)
        services.AddTokenRateLimiterWithTiktoken(rateLimiterOptions =>
        {
            rateLimiterOptions.TokenLimit = options.TokensPerMinute;
            rateLimiterOptions.WindowSeconds = 60;
            rateLimiterOptions.SafetyBuffer = options.SafetyBuffer;
            rateLimiterOptions.MinWaitTimeMs = options.MinWaitTimeMs;
            rateLimiterOptions.MaxWaitTimeMs = options.MaxWaitTimeMs;
        });

        return services;
    }
}
