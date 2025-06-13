using FluentAssertions;
using Microsoft.Extensions.Logging;
using TokenRateLimiter.Core.Options;

namespace TokenRateLimiter.Core.Tests;

public class BasicSetupTest
{
    [Fact]
    public void CanCreateTokenRateLimiter()
    {
        // Arrange
        var options = new TokenRateLimiterOptions();
        var logger = new LoggerFactory().CreateLogger<TokenRateLimiter>();

        // Act
        var rateLimiter = new TokenRateLimiter(options, logger);

        // Assert
        rateLimiter.Should().NotBeNull();
        rateLimiter.GetCurrentUsage().Should().Be(0);
        rateLimiter.GetReservedTokens().Should().Be(0);
    }
}
