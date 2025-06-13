using TokenRateLimiter.Core.Abstractions;
using TokenRateLimiter.Core.Options;
using TokenRateLimiter.Core.Utils;

namespace TokenRateLimiter.Tiktoken.Estimators;

/// <summary>
/// Simple character-based token estimator for cases where exact tokenization isn't available.
/// This provides a rough approximation based on character count and typical token-to-character ratios.
/// </summary>
public class CharacterBasedEstimator : ITokenEstimator
{
    private readonly double _charactersPerToken;
    private readonly Dictionary<ResponseType, double> _responseTypeMultipliers;

    /// <summary>
    /// Creates a character-based estimator with the default ratio.
    /// </summary>
    public CharacterBasedEstimator() : this(3.5) { }

    /// <summary>
    /// Creates a character-based estimator with a custom character-to-token ratio.
    /// </summary>
    /// <param name="charactersPerToken">Average number of characters per token</param>
    public CharacterBasedEstimator(double charactersPerToken)
    {
        if (charactersPerToken <= 0)
            throw new ArgumentException("Characters per token must be positive", nameof(charactersPerToken));

        _charactersPerToken = charactersPerToken;
        _responseTypeMultipliers = new Dictionary<ResponseType, double>
        {
            [ResponseType.Conversational] = 0.3,
            [ResponseType.Analytical] = 1.2,
            [ResponseType.Code] = 0.8,
            [ResponseType.Structured] = 0.4,
            [ResponseType.Creative] = 1.5
        };
    }

    public int EstimateInputTokens(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        return (int)Math.Ceiling(text.Length / _charactersPerToken);
    }

    public int EstimateInputTokens(IEnumerable<string> texts)
    {
        if (texts == null)
            return 0;

        int totalCharacters = texts.Sum(text => text?.Length ?? 0);
        return (int)Math.Ceiling(totalCharacters / _charactersPerToken);
    }

    public int EstimateOutputTokens(string inputText, OutputTokenEstimationOptions? options = null)
    {
        options ??= new OutputTokenEstimationOptions();

        int inputTokens = EstimateInputTokens(inputText);

        // Base estimation using configured ratio
        double baseOutputTokens = inputTokens * options.OutputToInputRatio;

        // Apply response type multiplier
        double responseMultiplier = _responseTypeMultipliers.GetValueOrDefault(options.ResponseType, 0.5);
        double adjustedOutputTokens = baseOutputTokens * responseMultiplier;

        // Apply bounds
        int estimatedOutput = (int)Math.Ceiling(adjustedOutputTokens);
        return Math.Max(options.MinOutputTokens, Math.Min(estimatedOutput, options.MaxOutputTokens));
    }

    public TokenEstimation EstimateTotalTokens(string inputText, OutputTokenEstimationOptions? options = null)
    {
        int inputTokens = EstimateInputTokens(inputText);
        int outputTokens = EstimateOutputTokens(inputText, options);
        return new TokenEstimation(inputTokens, outputTokens, inputTokens + outputTokens);
    }
}
