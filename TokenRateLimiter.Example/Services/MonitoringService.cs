using Microsoft.Extensions.Logging;
using TokenRateLimiter.Core.Abstractions;

namespace TokenRateLimiter.Example.Services;

/// <summary>
/// Demonstrates monitoring and statistics capabilities of the TokenRateLimiter.
/// Shows how to track usage, identify bottlenecks, and optimize token consumption.
/// </summary>
public class MonitoringService
{
    private readonly ITokenRateLimiter _rateLimiter;
    private readonly ITokenEstimator _estimator;
    private readonly ILogger<MonitoringService> _logger;

    public MonitoringService(
        ITokenRateLimiter rateLimiter,
        ITokenEstimator estimator,
        ILogger<MonitoringService> logger)
    {
        _rateLimiter = rateLimiter;
        _estimator = estimator;
        _logger = logger;
    }

    public async Task RunExample()
    {
        Console.WriteLine("📊 MONITORING AND STATISTICS EXAMPLE");
        Console.WriteLine("Shows how to monitor token usage with built-in methods.");
        Console.WriteLine();

        // Show initial state
        ShowCurrentStatus("Initial State");

        // Simulate some token usage
        Console.WriteLine("🔄 Simulating token usage...");
        await SimulateTokenUsage();

        // Show usage after simulation
        ShowCurrentStatus("After Token Usage");

        // Show token estimation capabilities
        DemonstrateTokenEstimation();
    }

    private async Task SimulateTokenUsage()
    {
        var tasks = new[]
        {
            ReserveAndUseTokens(5000, 4800),
            ReserveAndUseTokens(3000, 3200),
            ReserveAndUseTokens(7000, 6500),
            ReserveAndUseTokens(2000, 1900)
        };

        await Task.WhenAll(tasks);
        Console.WriteLine("   ✅ Token usage simulation completed");
        Console.WriteLine();
    }

    private async Task ReserveAndUseTokens(int inputTokens, int actualTotal)
    {
        var estimatedOutputTokens = (int)(inputTokens * 0.5); // 50% output estimation
        
        await using var reservation = await _rateLimiter.ReserveTokensAsync(inputTokens, estimatedOutputTokens);

        // Simulate processing time
        await Task.Delay(100);

        reservation.RecordActualUsage(actualTotal);
    }

    private void ShowCurrentStatus(string title)
    {
        Console.WriteLine($"📈 {title}:");

        var stats = _rateLimiter.GetUsageStats();

        Console.WriteLine($"   Current Usage: {stats.CurrentUsage:N0} tokens");
        Console.WriteLine($"   Reserved Tokens: {stats.ReservedTokens:N0} tokens");
        Console.WriteLine($"   Available Tokens: {stats.AvailableTokens:N0} tokens");
        Console.WriteLine($"   Active Reservations: {stats.ActiveReservations}");
        Console.WriteLine($"   Requests/Min: {stats.RequestsInLastMinute}");

        // Calculate usage percentage
        const int assumedLimit = 1_000_000;
        const int safetyBuffer = 50_000;
        int effectiveLimit = assumedLimit - safetyBuffer;
        double usagePercentage = (double)stats.CurrentUsage / effectiveLimit * 100;

        Console.WriteLine($"   Usage Percentage: {usagePercentage:F1}%");

        if (usagePercentage > 80)
        {
            Console.WriteLine("   ⚠️ WARNING: Near rate limit!");
        }

        if (stats.AvailableTokens <= 0)
        {
            Console.WriteLine("   🚨 ALERT: At capacity!");
        }

        Console.WriteLine();
    }

    private void DemonstrateTokenEstimation()
    {
        Console.WriteLine("🧮 TOKEN ESTIMATION EXAMPLE:");

        var testInputs = new[]
        {
            "Short message",
            "This is a medium-length message with some additional context and details that would consume more tokens than a simple short message.",
            CreateLongText()
        };

        foreach (var input in testInputs)
        {
            var inputTokens = _estimator.EstimateTokens(input);
            var preview = input.Length > 50 ? input[..50] + "..." : input;

            Console.WriteLine($"   Text: \"{preview}\"");
            Console.WriteLine($"   Character count: {input.Length}");
            Console.WriteLine($"   Estimated input tokens: {inputTokens:N0}");
            Console.WriteLine($"   Total with 50% output: {inputTokens + (int)(inputTokens * 0.5):N0} tokens");
            Console.WriteLine($"   Total with 80% output: {inputTokens + (int)(inputTokens * 0.8):N0} tokens");
            Console.WriteLine();
        }

        // Demonstrate batch estimation
        Console.WriteLine("📦 BATCH ESTIMATION EXAMPLE:");
        var totalTokens = testInputs.Sum(text => _estimator.EstimateTokens(text));
        Console.WriteLine($"   All texts combined: {totalTokens:N0} input tokens");
        Console.WriteLine();
    }

    private string CreateLongText()
    {
        return @"This is a comprehensive analysis document that contains detailed information about various aspects of business operations, strategic planning, market analysis, competitive landscape, financial projections, risk assessments, operational procedures, technology requirements, human resources considerations, regulatory compliance matters, customer relationship management strategies, product development roadmaps, quality assurance protocols, supply chain optimization, performance metrics and key performance indicators, stakeholder engagement processes, change management initiatives, and long-term sustainability planning across multiple business units and geographical regions.";
    }
}
