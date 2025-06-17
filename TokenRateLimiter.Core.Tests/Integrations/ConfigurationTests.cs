using FluentAssertions;
using TokenRateLimiter.Integrations.Configuration;

namespace TokenRateLimiter.Core.Tests.Integrations;

public class ConfigurationTests
{
    [Fact]
    public void AzureOpenAISetupOptions_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var options = new AzureOpenAISetupOptions();

        // Assert
        options.TokensPerMinute.Should().Be(1_000_000);
        options.SafetyBuffer.Should().Be(50_000);
        options.MinWaitTimeMs.Should().Be(3_000);
        options.MaxWaitTimeMs.Should().Be(30_000);
    }

    [Fact]
    public void AzureOpenAISetupOptions_ShouldAllowCustomization()
    {
        // Arrange & Act
        var options = new AzureOpenAISetupOptions
        {
            TokensPerMinute = 500_000,
            SafetyBuffer = 25_000,
            MinWaitTimeMs = 1_000,
            MaxWaitTimeMs = 15_000
        };

        // Assert
        options.TokensPerMinute.Should().Be(500_000);
        options.SafetyBuffer.Should().Be(25_000);
        options.MinWaitTimeMs.Should().Be(1_000);
        options.MaxWaitTimeMs.Should().Be(15_000);
    }

    [Theory]
    [InlineData(100_000)]
    [InlineData(500_000)]
    [InlineData(1_000_000)]
    [InlineData(2_000_000)]
    public void AzureOpenAISetupOptions_TokensPerMinute_ShouldAcceptValidValues(int tokensPerMinute)
    {
        // Arrange & Act
        var options = new AzureOpenAISetupOptions
        {
            TokensPerMinute = tokensPerMinute
        };

        // Assert
        options.TokensPerMinute.Should().Be(tokensPerMinute);
    }
}
