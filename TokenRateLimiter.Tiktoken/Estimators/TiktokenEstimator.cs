using Tiktoken;
using Tiktoken.Encodings;
using TokenRateLimiter.Core.Abstractions;
using TokenRateLimiter.Core.Options;
using TokenRateLimiter.Core.Utils;

namespace TokenRateLimiter.Tiktoken.Estimators;

/// <summary>
/// Token estimator using Tiktoken for OpenAI-compatible models.
/// This is the same tokenization method used by OpenAI and Azure OpenAI.
/// </summary>
public class TiktokenEstimator : ITokenEstimator
{
    private readonly Encoder _encoder;
    private readonly Dictionary<ResponseType, double> _responseTypeMultipliers;

    /// <summary>
    /// Creates a new Tiktoken estimator using the O200K_BASE encoding,
    /// which is used by GPT-4, GPT-4 Turbo, and GPT-4o models.
    /// </summary>
    public TiktokenEstimator()
    {
        _encoder = new Encoder(new O200KBase());
        _responseTypeMultipliers = new Dictionary<ResponseType, double>
        {
            [ResponseType.Conversational] = 0.3,
            [ResponseType.Analytical] = 1.2,
            [ResponseType.Code] = 0.8,
            [ResponseType.Structured] = 0.4,
            [ResponseType.Creative] = 1.5
        };
    }

    /// <summary>
    /// Creates a new Tiktoken estimator with a custom encoder.
    /// </summary>
    /// <param name="encoder">The Tiktoken encoder to use</param>
    public TiktokenEstimator(Encoder encoder)
    {
        _encoder = encoder ?? throw new ArgumentNullException(nameof(encoder));
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

        return _encoder.CountTokens(text);
    }

    public int EstimateInputTokens(IEnumerable<string> texts)
    {
        if (texts == null)
            return 0;

        // Combine all texts and count tokens once for efficiency
        string combinedText = string.Join("", texts);
        return EstimateInputTokens(combinedText);
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
