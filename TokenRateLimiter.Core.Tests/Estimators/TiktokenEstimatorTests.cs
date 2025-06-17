using FluentAssertions;
using TokenRateLimiter.Tiktoken.Estimators;

namespace TokenRateLimiter.Core.Tests.Estimators;

public class TiktokenEstimatorTests
{
    [Fact]
    public void EstimateTokens_WithValidText_ShouldReturnPositiveCount()
    {
        // Arrange
        var estimator = new TiktokenEstimator();
        var text = "Hello, world! This is a test message.";

        // Act
        var tokenCount = estimator.EstimateTokens(text);

        // Assert
        tokenCount.Should().BeGreaterThan(0);
        // Total tokens should be more than just input (includes estimated output)
        tokenCount.Should().BeGreaterThan(text.Length / 4); // Rough minimum expectation
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void EstimateTokens_WithEmptyOrNullText_ShouldReturnZero(string text)
    {
        // Arrange
        var estimator = new TiktokenEstimator();

        // Act
        var tokenCount = estimator.EstimateTokens(text);

        // Assert
        tokenCount.Should().Be(0);
    }

    [Fact]
    public void EstimateTokens_WithCustomOutputRatio_ShouldReturnDifferentCounts()
    {
        // Arrange
        var estimator = new TiktokenEstimator();
        var text = "This is a test message for custom output ratio testing.";

        // Act
        var defaultEstimation = estimator.EstimateTokens(text); // 50% default
        var lowOutputEstimation = estimator.EstimateTokens(text, 0.2); // 20% output
        var highOutputEstimation = estimator.EstimateTokens(text, 1.0); // 100% output

        // Assert
        lowOutputEstimation.Should().BeLessThan(defaultEstimation);
        defaultEstimation.Should().BeLessThan(highOutputEstimation);

        // All should be positive
        lowOutputEstimation.Should().BeGreaterThan(0);
        defaultEstimation.Should().BeGreaterThan(0);
        highOutputEstimation.Should().BeGreaterThan(0);
    }

    [Fact]
    public void EstimateTokens_WithMultipleTexts_ShouldReturnCombinedEstimation()
    {
        // Arrange
        var estimator = new TiktokenEstimator();
        var texts = new[]
        {
            "First message",
            "Second message",
            "Third message"
        };

        // Act
        var combinedEstimation = estimator.EstimateTokens(texts);
        var individualSum = texts.Sum(text => estimator.EstimateTokens(text));

        // Assert
        combinedEstimation.Should().BeGreaterThan(0);
        // Combined estimation should be close to sum of individual estimations
        // (allowing some variance due to tokenization differences)
        combinedEstimation.Should().BeInRange((int)(individualSum * 0.8), (int)(individualSum * 1.2));
    }

    [Fact]
    public void EstimateTokens_WithLongText_ShouldRespectMaxOutputLimit()
    {
        // Arrange
        var estimator = new TiktokenEstimator(); // Default max output: 16,384
        var longText = string.Join(" ", Enumerable.Repeat("word", 10000)); // Very long text

        // Act
        var estimation = estimator.EstimateTokens(longText, 2.0); // Request 200% output (very high)

        // Assert
        estimation.Should().BeGreaterThan(0);
        // Should not exceed reasonable bounds (input + max output)
        estimation.Should().BeLessThan(30000); // Reasonable upper bound for this test
    }
}