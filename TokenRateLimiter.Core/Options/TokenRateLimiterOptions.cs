namespace TokenRateLimiter.Core.Options;

public class TokenRateLimiterOptions
{
    public int TokenLimit { get; set; } = 1_000_000;
    public int WindowSeconds { get; set; } = 60;
    public int SafetyBuffer { get; set; } = 50_000;
    public int MinWaitTimeMs { get; set; } = 3_000;
    public int MaxWaitTimeMs { get; set; } = 30_000;
    public double WaitTimeMultiplier { get; set; } = 1.2;
    public int JitterRangeMs { get; set; } = 1_000;
}

public record TokenUsageRecord(DateTime Timestamp, int TokenCount);