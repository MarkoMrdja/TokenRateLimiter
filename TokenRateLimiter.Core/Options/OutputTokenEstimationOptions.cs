using TokenRateLimiter.Core.Utils;

namespace TokenRateLimiter.Core.Options;

public class OutputTokenEstimationOptions
{
    public double OutputToInputRatio { get; set; } = 0.5;
    public int MaxOutputTokens { get; set; } = 4096;
    public int MinOutputTokens { get; set; } = 10;
    public ResponseType ResponseType { get; set; } = ResponseType.Conversational;
}
