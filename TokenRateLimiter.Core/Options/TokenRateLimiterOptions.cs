namespace TokenRateLimiter.Core.Options;

public class TokenRateLimiterOptions
{
    // Core rate limiting
    public int TokenLimit { get; set; } = 1_000_000;
    public int WindowSeconds { get; set; } = 60;
    public int SafetyBuffer { get; set; } = 50_000;

    // Concurrency and request limiting
    public int MaxConcurrentReservations { get; set; } = 10;
    public int MaxRequestsPerMinute { get; set; } = 1_000;
    
    // Wait time control
    public int MinWaitTimeMs { get; set; } = 1_000;
    public int MaxWaitTimeMs { get; set; } = 30_000;
    public double WaitTimeMultiplier { get; set; } = 1.2;
    public int JitterRangeMs { get; set; } = 500;
    
    // Token estimation for output
    public OutputEstimationStrategy OutputEstimationStrategy { get; set; } = OutputEstimationStrategy.FixedMultiplier;
    
    /// <summary>
    /// Multiplier for input tokens to estimate output tokens (default: 0.5)
    /// Used when OutputEstimationStrategy is FixedMultiplier
    /// Examples: 0.3 = 30% of input, 1.0 = 100% of input, 2.0 = 200% of input
    /// </summary>
    public double OutputMultiplier { get; set; } = 0.5;
    
    /// <summary>
    /// Fixed number of tokens to add for output estimation (default: 1000)
    /// Used when OutputEstimationStrategy is FixedAmount
    /// Good starting points: 500 for short responses, 1000 for medium, 2000+ for long
    /// </summary>
    public int DefaultOutputTokens { get; set; } = 1000;
}

public record TokenUsageRecord(DateTime Timestamp, int TokenCount);

internal record PendingReservation(Guid Id, int EstimatedTokens, DateTime CreatedAt);

public record TokenUsageStats(
    int CurrentUsage,
    int ReservedTokens, 
    int AvailableTokens,
    int ActiveReservations,
    int RequestsInLastMinute
);