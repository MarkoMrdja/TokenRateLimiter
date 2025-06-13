namespace TokenRateLimiter.Integrations.Configuration;

internal class AzureOpenAIRateLimitedConfiguration
{
    /// <summary>Azure OpenAI endpoint URL</summary>
    public string Endpoint { get; set; }

    /// <summary>Azure OpenAI API key</summary>
    public string ApiKey { get; set; }

    /// <summary>Default model to use for completions</summary>
    public string DefaultModel { get; set; }

    /// <summary>Tokens per minute rate limit for your Azure OpenAI deployment</summary>
    public int TokensPerMinute { get; set; } = 1_000_000;

    /// <summary>Default temperature for completions</summary>
    public float Temperature { get; set; } = 0.1f;

    /// <summary>Whether to enable smart adaptive buffer learning</summary>
    public bool EnableSmartBuffer { get; set; } = true;

    /// <summary>Base safety buffer in tokens</summary>
    public int SafetyBuffer { get; set; } = 50_000;
}
