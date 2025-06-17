using Tiktoken;
using Tiktoken.Encodings;
using TokenRateLimiter.Core.Abstractions;

namespace TokenRateLimiter.Tiktoken.Estimators;

/// <summary>
/// Token estimator using Tiktoken for OpenAI-compatible models.
/// This is the same tokenization method used by OpenAI and Azure OpenAI.
/// </summary>
public class TiktokenEstimator : ITokenEstimator
{
    private readonly Encoder _encoder;
    private readonly int _maxOutputTokens;

    /// <summary>
    /// Creates a new Tiktoken estimator using O200K_BASE encoding (GPT-4 family).
    /// Uses 32K max output tokens (suitable for GPT-4.1 models).
    /// </summary>
    public TiktokenEstimator() : this(new Encoder(new O200KBase()), 32_768) { }

    /// <summary>
    /// Creates a new Tiktoken estimator with custom settings.
    /// </summary>
    /// <param name="encoder">The Tiktoken encoder to use</param>
    /// <param name="maxOutputTokens">Maximum output tokens the model can generate</param>
    public TiktokenEstimator(Encoder encoder, int maxOutputTokens = 32_768)
    {
        _encoder = encoder ?? throw new ArgumentNullException(nameof(encoder));
        if (maxOutputTokens <= 0)
            throw new ArgumentException("Max output tokens must be positive", nameof(maxOutputTokens));

        _maxOutputTokens = maxOutputTokens;
    }

    public int EstimateTokens(string text) => EstimateTokens(text, 0.5);

    public int EstimateTokens(string text, double outputToInputRatio)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        int inputTokens = _encoder.CountTokens(text);
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
