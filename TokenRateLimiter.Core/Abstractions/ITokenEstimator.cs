namespace TokenRateLimiter.Core.Abstractions;

public interface ITokenEstimator
{
    /// <summary>
    /// Estimates total tokens using default 50% output ratio
    /// </summary>
    int EstimateTokens(string text);

    /// <summary>
    /// Estimates total tokens with custom output ratio
    /// </summary>
    int EstimateTokens(string text, double outputToInputRatio);

    /// <summary>
    /// Estimates total tokens for multiple texts
    /// </summary>
    int EstimateTokens(IEnumerable<string> texts);

    /// <summary>
    /// Estimates total tokens for multiple texts with custom ratio
    /// </summary>
    int EstimateTokens(IEnumerable<string> texts, double outputToInputRatio);
}
