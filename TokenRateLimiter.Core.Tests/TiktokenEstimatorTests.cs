using FluentAssertions;
using TokenRateLimiter.Tiktoken.Estimators;

namespace TokenRateLimiter.Core.Tests;

public class TiktokenEstimatorTests
{
    [Fact]
    public void EstimateInputTokens_WithValidText_ShouldReturnPositiveCount()
    {
        // Arrange
        var estimator = new TiktokenEstimator();
        var text = "Hello, world! This is a test message.";

        // Act
        var tokenCount = estimator.EstimateInputTokens(text);

        // Assert
        tokenCount.Should().BeGreaterThan(0);
        tokenCount.Should().BeLessThan(text.Length); // Should be fewer tokens than characters
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void EstimateInputTokens_WithEmptyOrNullText_ShouldReturnZero(string text)
    {
        // Arrange
        var estimator = new TiktokenEstimator();

        // Act
        var tokenCount = estimator.EstimateInputTokens(text);

        // Assert
        tokenCount.Should().Be(0);
    }
}