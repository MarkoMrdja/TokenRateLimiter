using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using TokenRateLimiter.Core.Abstractions;
using TokenRateLimiter.Integrations.Extensions;

namespace TokenRateLimiter.Example.Services;

/// <summary>
/// Demonstrates the most common usage pattern: extension methods for easy integration.
/// </summary>
public class ChatExampleService
{
    private readonly AzureOpenAIClient _azureClient;
    private readonly ITokenRateLimiter _rateLimiter;
    private readonly ITokenEstimator _estimator;
    private readonly ILogger<ChatExampleService> _logger;

    public ChatExampleService(
        AzureOpenAIClient azureClient,
        ITokenRateLimiter rateLimiter,
        ITokenEstimator estimator,
        ILogger<ChatExampleService> logger)
    {
        _azureClient = azureClient;
        _rateLimiter = rateLimiter;
        _estimator = estimator;
        _logger = logger;
    }

    public async Task RunExample()
    {
        Console.WriteLine("This example shows how to add rate limiting to existing Azure OpenAI code.");
        Console.WriteLine("Rate limiting is added with just one line: .WithRateLimit()");
        Console.WriteLine();

        var prompts = new[]
        {
            "Explain quantum computing in one sentence.",
            "What's the capital of France?",
            "Write a haiku about programming.",
            "Explain the theory of relativity briefly."
        };

        foreach (var prompt in prompts)
        {
            Console.WriteLine($"🤖 Asking: {prompt}");

            try
            {
                var response = await ChatWithRateLimit(prompt);
                Console.WriteLine($"✅ Response: {response}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
            }

            Console.WriteLine();
        }
    }

    private async Task<string> ChatWithRateLimit(string userMessage)
    {
        var messages = new ChatMessage[]
        {
            ChatMessage.CreateSystemMessage("You are a helpful assistant. Keep responses concise."),
            ChatMessage.CreateUserMessage(userMessage)
        };

        // This is your existing Azure OpenAI code:
        // var completion = await _azureClient.GetChatClient("gpt-4o").CompleteChatAsync(messages);

        // With rate limiting, just add .WithRateLimit():
        var completion = await _azureClient
            .GetChatClient("gpt-4o")
            .CompleteChatAsync(messages)
            .WithRateLimit(_rateLimiter, _estimator, messages);

        return completion.Value.Content[0].Text;
    }
}
