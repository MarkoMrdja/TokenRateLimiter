using OpenAI.Chat;
using System.ClientModel;
using TokenRateLimiter.Core.Abstractions;
using TokenRateLimiter.Core.Extensions;

namespace TokenRateLimiter.Integrations.Extensions;

/// <summary>
/// Extension methods that add rate limiting to Azure OpenAI operations.
/// </summary>
public static class AzureOpenAIExtensions
{
    /// <summary>
    /// Adds rate limiting to Azure OpenAI ChatClient.CompleteChatAsync calls.
    /// This handles the ClientResult<ChatCompletion> return type from Azure OpenAI SDK.
    /// </summary>
    public static async Task<ClientResult<ChatCompletion>> WithRateLimit(
        this Task<ClientResult<ChatCompletion>> chatCompletionTask,
        ITokenRateLimiter rateLimiter,
        ITokenEstimator estimator,
        IEnumerable<ChatMessage> messages,
        CancellationToken cancellationToken = default)
    {
        var inputText = string.Join("", messages.Select(m => m.Content?.ToString() ?? ""));
        var estimatedTokens = estimator.EstimateTokens(inputText);

        return await rateLimiter.ExecuteAsync(
            estimatedTokens,
            async () =>
            {
                var clientResult = await chatCompletionTask;
                return (clientResult, clientResult.Value.Usage.TotalTokenCount);
            },
            cancellationToken);
    }

    /// <summary>
    /// Adds rate limiting to Azure OpenAI ChatClient.CompleteChatAsync calls with custom output ratio.
    /// </summary>
    public static async Task<ClientResult<ChatCompletion>> WithRateLimit(
        this Task<ClientResult<ChatCompletion>> chatCompletionTask,
        ITokenRateLimiter rateLimiter,
        ITokenEstimator estimator,
        IEnumerable<ChatMessage> messages,
        double outputToInputRatio,
        CancellationToken cancellationToken = default)
    {
        var inputText = string.Join("", messages.Select(m => m.Content?.ToString() ?? ""));
        var estimatedTokens = estimator.EstimateTokens(inputText, outputToInputRatio);

        return await rateLimiter.ExecuteAsync(
            estimatedTokens,
            async () =>
            {
                var clientResult = await chatCompletionTask;
                return (clientResult, clientResult.Value.Usage.TotalTokenCount);
            },
            cancellationToken);
    }

    /// <summary>
    /// Adds rate limiting to ChatClient operations with fluent syntax.
    /// This allows: chatClient.CompleteChatWithRateLimit(messages, rateLimiter, estimator)
    /// </summary>
    public static async Task<ClientResult<ChatCompletion>> CompleteChatWithRateLimit(
        this ChatClient chatClient,
        IEnumerable<ChatMessage> messages,
        ITokenRateLimiter rateLimiter,
        ITokenEstimator estimator,
        ChatCompletionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return await chatClient
            .CompleteChatAsync(messages, options, cancellationToken)
            .WithRateLimit(rateLimiter, estimator, messages, cancellationToken);
    }

    /// <summary>
    /// Adds rate limiting to ChatClient operations with custom output ratio.
    /// </summary>
    public static async Task<ClientResult<ChatCompletion>> CompleteChatWithRateLimit(
        this ChatClient chatClient,
        IEnumerable<ChatMessage> messages,
        ITokenRateLimiter rateLimiter,
        ITokenEstimator estimator,
        double outputToInputRatio,
        ChatCompletionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return await chatClient
            .CompleteChatAsync(messages, options, cancellationToken)
            .WithRateLimit(rateLimiter, estimator, messages, outputToInputRatio, cancellationToken);
    }

    /// <summary>
    /// Convenience method that returns just the ChatCompletion (unwrapped from ClientResult).
    /// </summary>
    public static async Task<ChatCompletion> CompleteChatWithRateLimitUnwrapped(
        this ChatClient chatClient,
        IEnumerable<ChatMessage> messages,
        ITokenRateLimiter rateLimiter,
        ITokenEstimator estimator,
        ChatCompletionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var clientResult = await chatClient.CompleteChatWithRateLimit(
            messages, rateLimiter, estimator, options, cancellationToken);

        return clientResult.Value;
    }

    /// <summary>
    /// Convenience method that returns just the ChatCompletion with custom output ratio.
    /// </summary>
    public static async Task<ChatCompletion> CompleteChatWithRateLimitUnwrapped(
        this ChatClient chatClient,
        IEnumerable<ChatMessage> messages,
        ITokenRateLimiter rateLimiter,
        ITokenEstimator estimator,
        double outputToInputRatio,
        ChatCompletionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var clientResult = await chatClient.CompleteChatWithRateLimit(
            messages, rateLimiter, estimator, outputToInputRatio, options, cancellationToken);

        return clientResult.Value;
    }

    /// <summary>
    /// Generic extension for any Azure OpenAI operation that returns ClientResult<T>.
    /// </summary>
    public static async Task<ClientResult<T>> WithRateLimit<T>(
        this Task<ClientResult<T>> operation,
        ITokenRateLimiter rateLimiter,
        int estimatedTokens,
        Func<ClientResult<T>, int> actualTokensExtractor,
        CancellationToken cancellationToken = default)
    {
        return await rateLimiter.ExecuteAsync(
            estimatedTokens,
            async () =>
            {
                var result = await operation;
                var actualTokens = actualTokensExtractor(result);
                return (result, actualTokens);
            },
            cancellationToken);
    }

    /// <summary>
    /// Generic extension for any operation that returns Task<T> with token usage.
    /// </summary>
    public static async Task<T> WithRateLimit<T>(
        this Task<T> operation,
        ITokenRateLimiter rateLimiter,
        int estimatedTokens,
        Func<T, int> actualTokensExtractor,
        CancellationToken cancellationToken = default)
    {
        return await rateLimiter.ExecuteAsync(
            estimatedTokens,
            async () =>
            {
                var result = await operation;
                var actualTokens = actualTokensExtractor(result);
                return (result, actualTokens);
            },
            cancellationToken);
    }
}