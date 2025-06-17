namespace TokenRateLimiter.Integrations.Configuration;

/// <summary>
/// Configuration helper for setting up rate limiting with Azure OpenAI.
/// </summary>
public class AzureOpenAISetupOptions
{
    /// <summary>Tokens per minute rate limit for your Azure OpenAI deployment</summary>
    public int TokensPerMinute { get; set; } = 1_000_000;

    /// <summary>Base safety buffer in tokens</summary>
    public int SafetyBuffer { get; set; } = 50_000;

    /// <summary>Minimum wait time when rate limited</summary>
    public int MinWaitTimeMs { get; set; } = 3_000;

    /// <summary>Maximum wait time when rate limited</summary>
    public int MaxWaitTimeMs { get; set; } = 30_000;
}
