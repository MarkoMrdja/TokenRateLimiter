using TokenRateLimiter.Core.Abstractions;

namespace TokenRateLimiter.Tiktoken.Estimators;

/// <summary>
/// Simple character-based token estimator for models where exact tokenization isn't available.
/// Works well with Claude, Gemini, local models, and any other LLM provider.
/// </summary>
public class CharacterBasedEstimator : ITokenEstimator
{
    private readonly double _charactersPerToken;
    private readonly int _maxOutputTokens;

    /// <summary>
    /// Creates a character-based estimator with default settings.
    /// Uses 3.5 chars per token and 32K max output tokens.
    /// </summary>
    public CharacterBasedEstimator() : this(3.5, 32_768) { }

    /// <summary>
    /// Creates a character-based estimator with custom character-to-token ratio.
    /// </summary>
    /// <param name="charactersPerToken">Average number of characters per token for your model</param>
    /// <param name="maxOutputTokens">Maximum output tokens the model can generate (default: 32,768)</param>
    public CharacterBasedEstimator(double charactersPerToken, int maxOutputTokens = 32_768)
    {
        if (charactersPerToken <= 0)
            throw new ArgumentException("Characters per token must be positive", nameof(charactersPerToken));
        if (maxOutputTokens <= 0)
            throw new ArgumentException("Max output tokens must be positive", nameof(maxOutputTokens));

        _charactersPerToken = charactersPerToken;
        _maxOutputTokens = maxOutputTokens;
    }

    public int EstimateTokens(string text) => EstimateTokens(text, 0.5);

    public int EstimateTokens(string text, double outputToInputRatio)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        int inputTokens = (int)Math.Ceiling(text.Length / _charactersPerToken);

        int estimatedOutput = (int)Math.Ceiling(inputTokens * outputToInputRatio);

        int boundedOutput = Math.Max(10, Math.Min(estimatedOutput, _maxOutputTokens));

        return inputTokens + boundedOutput;
    }

    public int EstimateTokens(IEnumerable<string> texts) =>
        EstimateTokens(texts, 0.5);

    public int EstimateTokens(IEnumerable<string> texts, double outputToInputRatio)
    {
        if (texts == null)
            return 0;

        string combinedText = string.Join("", texts);
        return EstimateTokens(combinedText, outputToInputRatio);
    }
}