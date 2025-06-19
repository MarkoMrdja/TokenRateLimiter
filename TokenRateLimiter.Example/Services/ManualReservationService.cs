using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using TokenRateLimiter.Core.Abstractions;

namespace TokenRateLimiter.Example.Services;

/// <summary>
/// Demonstrates manual reservation for advanced scenarios where you need precise control
/// over token allocation, such as conditional processing or complex business logic.
/// </summary>
public class ManualReservationService
{
    private readonly AzureOpenAIClient _azureClient;
    private readonly ITokenRateLimiter _rateLimiter;
    private readonly ITokenEstimator _estimator;
    private readonly ILogger<ManualReservationService> _logger;

    public ManualReservationService(
        AzureOpenAIClient azureClient,
        ITokenRateLimiter rateLimiter,
        ITokenEstimator estimator,
        ILogger<ManualReservationService> logger)
    {
        _azureClient = azureClient;
        _rateLimiter = rateLimiter;
        _estimator = estimator;
        _logger = logger;
    }

    public async Task RunExample()
    {
        Console.WriteLine("⚙️ MANUAL RESERVATION EXAMPLE");
        Console.WriteLine("Shows advanced control for complex scenarios:");
        Console.WriteLine("- Conditional processing based on available capacity");
        Console.WriteLine("- Fallback strategies when tokens are limited");
        Console.WriteLine("- Precise token management for business logic");
        Console.WriteLine();

        var analysisRequests = new[]
        {
            new AnalysisRequest("Critical Production Issue", "Analyze this critical production error log and provide immediate recommendations.", Priority.Critical),
            new AnalysisRequest("Customer Feedback Review", "Review these customer feedback responses and identify key themes.", Priority.High),
            new AnalysisRequest("Market Trends Analysis", "Analyze current market trends in the technology sector.", Priority.Medium),
            new AnalysisRequest("Code Review Summary", "Provide a summary of this code review and suggest improvements.", Priority.Low),
            new AnalysisRequest("Meeting Notes Summary", "Summarize these meeting notes and extract action items.", Priority.Low)
        };

        Console.WriteLine($"📋 Processing {analysisRequests.Length} requests with priority-based token allocation...");
        Console.WriteLine();

        foreach (var request in analysisRequests)
        {
            await ProcessWithPriorityLogic(request);
            Console.WriteLine();
        }
    }

    private async Task ProcessWithPriorityLogic(AnalysisRequest request)
    {
        Console.WriteLine($"🔍 Processing: {request.Title} (Priority: {request.Priority})");

        // For critical items, always process immediately
        // For others, check current usage to decide strategy
        if (request.Priority == Priority.Critical)
        {
            await ProcessFullAnalysis(request);
        }
        else
        {
            // Check current token usage to decide on strategy
            var stats = _rateLimiter.GetUsageStats();

            Console.WriteLine($"   📊 Current usage: {stats.CurrentUsage} tokens ({stats.ReservedTokens} reserved)");

            // Simple heuristic: if we're using more than 80% of available capacity, use fallback
            if (stats.AvailableTokens < 200_000) // Less than 200k tokens available
            {
                await ProcessWithFallback(request);
            }
            else
            {
                await ProcessFullAnalysis(request);
            }
        }
    }

    private async Task ProcessFullAnalysis(AnalysisRequest request)
    {
        Console.WriteLine($"   🚀 Processing with full analysis");

        var fullPrompt = CreateDetailedPrompt(request.Description);
        var inputTokens = _estimator.EstimateTokens(fullPrompt);
        var estimatedOutputTokens = (int)(inputTokens * 1.2); // Expect 120% more for detailed analysis

        await using var reservation = await _rateLimiter.ReserveTokensAsync(inputTokens, estimatedOutputTokens);

        try
        {
            var messages = new ChatMessage[]
            {
                ChatMessage.CreateSystemMessage("You are an expert analyst. Provide comprehensive, detailed analysis with actionable insights."),
                ChatMessage.CreateUserMessage(fullPrompt)
            };

            var chatClient = _azureClient.GetChatClient("gpt-4o");
            var completion = await chatClient.CompleteChatAsync(messages);

            reservation.RecordActualUsage(completion.Value.Usage.TotalTokenCount);

            Console.WriteLine($"   ✅ Full analysis completed using {completion.Value.Usage.TotalTokenCount} tokens");
            Console.WriteLine($"   📝 Result: {completion.Value.Content[0].Text[..Math.Min(150, completion.Value.Content[0].Text.Length)]}...");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ Full analysis failed: {ex.Message}");
        }
    }

    private async Task ProcessWithFallback(AnalysisRequest request)
    {
        Console.WriteLine($"   🔄 Using fallback strategy (quick summary)");

        var simplePrompt = CreateSimplePrompt(request.Description);
        var inputTokens = _estimator.EstimateTokens(simplePrompt);
        var estimatedOutputTokens = (int)(inputTokens * 0.3); // Expect 30% more for brief summaries

        await using var reservation = await _rateLimiter.ReserveTokensAsync(inputTokens, estimatedOutputTokens);

        try
        {
            var messages = new ChatMessage[]
            {
                ChatMessage.CreateSystemMessage("You are a helpful assistant. Provide concise summaries."),
                ChatMessage.CreateUserMessage(simplePrompt)
            };

            var chatClient = _azureClient.GetChatClient("gpt-4o");
            var completion = await chatClient.CompleteChatAsync(messages);

            reservation.RecordActualUsage(completion.Value.Usage.TotalTokenCount);

            Console.WriteLine($"   ✅ Quick summary completed using {completion.Value.Usage.TotalTokenCount} tokens");
            Console.WriteLine($"   📝 Result: {completion.Value.Content[0].Text[..Math.Min(150, completion.Value.Content[0].Text.Length)]}...");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ Fallback strategy failed: {ex.Message}");
        }
    }

    private string CreateDetailedPrompt(string description)
    {
        return $@"Please provide a comprehensive analysis of the following:

{description}

Include in your analysis:
1. Key findings and insights
2. Potential risks or concerns
3. Actionable recommendations
4. Next steps and priorities
5. Any additional context or considerations

Please be thorough and detailed in your response.";
    }

    private string CreateSimplePrompt(string description)
    {
        return $"Provide a brief summary and key points for: {description}";
    }

    private record AnalysisRequest(string Title, string Description, Priority Priority);

    private enum Priority
    {
        Low,
        Medium,
        High,
        Critical
    }
}