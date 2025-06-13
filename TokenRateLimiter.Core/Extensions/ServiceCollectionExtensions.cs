using Microsoft.Extensions.DependencyInjection;
using TokenRateLimiter.Core.Abstractions;
using TokenRateLimiter.Core.Options;

namespace TokenRateLimiter.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTokenRateLimiter(this IServiceCollection services)
    {
        return services.AddTokenRateLimiter(options => { });
    }

    public static IServiceCollection AddTokenRateLimiter(this IServiceCollection services,
        Action<TokenRateLimiterOptions> configureOptions)
    {
        services.Configure(configureOptions);
        services.AddSingleton<ITokenRateLimiter, TokenRateLimiter>();

        return services;
    }

    public static IServiceCollection AddAzureOpenAIRateLimiter(this IServiceCollection services,
        int tokensPerMinute = 1_000_000)
    {
        return services.AddTokenRateLimiter(options =>
        {
            options.TokenLimit = tokensPerMinute;
            options.WindowSeconds = 60;
            options.SafetyBuffer = Math.Max(50_000, tokensPerMinute / 20);
            options.MinWaitTimeMs = 3_000;
            options.MaxWaitTimeMs = 30_000;
        });
    }
}