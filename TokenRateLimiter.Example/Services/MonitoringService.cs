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

    private async Task ReserveAndUseTokens(int estimated, int actual)
    {
        using var reservation = await _rateLimiter.ReserveTokensAsync(estimated);

        // Simulate processing time
        await Task.Delay(100);

        await reservation.CompleteAsync(actual);
    }

    private void ShowCurrentStatus(string title)
    {
        Console.WriteLine($"📈 {title}:");

        int currentUsage = _rateLimiter.GetCurrentUsage();
        int reservedTokens = _rateLimiter.GetReservedTokens();
        int historicalUsage = currentUsage - reservedTokens;

        Console.WriteLine($"   Current Usage: {currentUsage:N0} tokens");
        Console.WriteLine($"   Reserved Tokens: {reservedTokens:N0} tokens");
        Console.WriteLine($"   Historical Usage: {historicalUsage:N0} tokens");

        // Simple capacity estimation
        const int assumedLimit = 1_000_000;
        const int safetyBuffer = 50_000;
        int effectiveLimit = assumedLimit - safetyBuffer;
        int availableTokens = Math.Max(0, effectiveLimit - currentUsage);
        double usagePercentage = (double)currentUsage / effectiveLimit * 100;

        Console.WriteLine($"   Available Tokens: {availableTokens:N0} tokens");
        Console.WriteLine($"   Usage Percentage: {usagePercentage:F1}%");

        if (usagePercentage > 80)
        {
            Console.WriteLine("   ⚠️ WARNING: Near rate limit!");
        }

        if (availableTokens <= 0)
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
            // Updated to use simplified interface
            var totalEstimation = _estimator.EstimateTokens(input); // Default 50% output ratio
            var customEstimation = _estimator.EstimateTokens(input, 0.8); // 80% output ratio
            var preview = input.Length > 50 ? input[..50] + "..." : input;

            Console.WriteLine($"   Text: \"{preview}\"");
            Console.WriteLine($"   Character count: {input.Length}");
            Console.WriteLine($"   Total estimated (50% output): {totalEstimation:N0} tokens");
            Console.WriteLine($"   Total estimated (80% output): {customEstimation:N0} tokens");
            Console.WriteLine();
        }

        // Demonstrate batch estimation
        Console.WriteLine("📦 BATCH ESTIMATION EXAMPLE:");
        var totalBatchEstimation = _estimator.EstimateTokens(testInputs);
        Console.WriteLine($"   All texts combined: {totalBatchEstimation:N0} tokens");
        Console.WriteLine();
    }

    private string CreateLongText()
    {
        return @"This is a comprehensive analysis document that contains detailed information about various aspects of business operations, strategic planning, market analysis, competitive landscape, financial projections, risk assessments, operational procedures, technology requirements, human resources considerations, regulatory compliance matters, customer relationship management strategies, product development roadmaps, quality assurance protocols, supply chain optimization, performance metrics and key performance indicators, stakeholder engagement processes, change management initiatives, and long-term sustainability planning across multiple business units and geographical regions.";
    }
}
