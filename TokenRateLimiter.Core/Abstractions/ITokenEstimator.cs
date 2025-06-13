using TokenRateLimiter.Core.Options;

namespace TokenRateLimiter.Core.Abstractions;

public interface ITokenEstimator
{
    int EstimateInputTokens(string text);
    int EstimateInputTokens(IEnumerable<string> texts);
    int EstimateOutputTokens(string inputText, OutputTokenEstimationOptions? options = null);
    TokenEstimation EstimateTotalTokens(string inputText, OutputTokenEstimationOptions? options = null);
}

public record TokenEstimation(int InputTokens, int EstimatedOutputTokens, int EstimatedTotal);
