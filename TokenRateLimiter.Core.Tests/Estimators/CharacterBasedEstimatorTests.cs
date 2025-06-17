using FluentAssertions;
using TokenRateLimiter.Tiktoken.Estimators;

namespace TokenRateLimiter.Core.Tests.Estimators;

public class CharacterBasedEstimatorTests
{
    [Fact]
    public void EstimateTokens_WithDefaultSettings_ShouldUseCorrectRatio()
    {
        // Arrange
        var estimator = new CharacterBasedEstimator(); // Default: 3.5 chars/token, 32K max output
        var text = "Hello world!"; // 12 characters

        // Act
        var estimation = estimator.EstimateTokens(text);

        // Assert
        estimation.Should().BeGreaterThan(0);
        // Should be reasonable for 12 characters + 50% output
        estimation.Should().BeInRange(5, 20);
    }

    [Fact]
    public void EstimateTokens_WithCustomCharacterRatio_ShouldUseProvidedRatio()
    {
        // Arrange
        var estimator = new CharacterBasedEstimator(charactersPerToken: 4.0); // 4 chars per token
        var text = "Test message"; // 12 characters

        // Act
        var estimation = estimator.EstimateTokens(text);

        // Assert
        estimation.Should().BeGreaterThan(0);
        // With 4 chars/token, input should be ~3 tokens, plus output estimation
        estimation.Should().BeGreaterThan(3);
    }

    [Fact]
    public void EstimateTokens_WithCustomMaxOutput_ShouldRespectLimit()
    {
        // Arrange
        var estimator = new CharacterBasedEstimator(3.5, maxOutputTokens: 100); // Low max output
        var text = string.Join(" ", Enumerable.Repeat("word", 1000)); // Long text

        // Act
        var estimation = estimator.EstimateTokens(text, 5.0); // Request very high output ratio

        // Assert
        estimation.Should().BeGreaterThan(0);
        // Should be capped by maxOutputTokens
        estimation.Should().BeLessThan(1000); // Input + 100 max output should be reasonable
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithInvalidCharacterRatio_ShouldThrowArgumentException(double invalidRatio)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new CharacterBasedEstimator(invalidRatio));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Constructor_WithInvalidMaxOutput_ShouldThrowArgumentException(int invalidMaxOutput)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new CharacterBasedEstimator(3.5, invalidMaxOutput));
    }
}
