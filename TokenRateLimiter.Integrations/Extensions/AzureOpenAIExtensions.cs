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
    /// Adds rate limiting to Azure OpenAI chat completion calls.
    /// Usage: await chatClient.CompleteChatAsync(messages).WithRateLimit(rateLimiter, estimator, messages)
    /// </summary>
    public static async Task<ClientResult<ChatCompletion>> WithRateLimit(
        this Task<ClientResult<ChatCompletion>> chatCompletionTask,
        ITokenRateLimiter rateLimiter,
        ITokenEstimator estimator,
        IEnumerable<ChatMessage> messages,
        CancellationToken cancellationToken = default)
    {
        var inputText = ExtractTextFromMessages(messages);
        
        return await rateLimiter.ExecuteAsync(
            estimator,
            inputText,
            async () =>
            {
                var clientResult = await chatCompletionTask;
                var actualTokens = clientResult.Value.Usage?.TotalTokenCount ?? 0;
                return (clientResult, actualTokens);
            },
            cancellationToken);
    }

    /// <summary>
    /// Helper to extract text from messages for token estimation
    /// </summary>
    private static string ExtractTextFromMessages(IEnumerable<ChatMessage> messages)
    {
        return string.Join(" ", messages.Select(m => m.Content?.ToString() ?? ""));
    }
}