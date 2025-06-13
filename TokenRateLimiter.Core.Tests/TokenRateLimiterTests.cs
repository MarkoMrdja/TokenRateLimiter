using FluentAssertions;
using Microsoft.Extensions.Logging;
using TokenRateLimiter.Core.Options;

namespace TokenRateLimiter.Core.Tests;

public class TokenRateLimiterTests
{
    private readonly ILogger<TokenRateLimiter> _logger;
    private readonly TokenRateLimiterOptions _options;

    public TokenRateLimiterTests()
    {
        _logger = new LoggerFactory().CreateLogger<TokenRateLimiter>();
        _options = new TokenRateLimiterOptions
        {
            TokenLimit = 1000,
            WindowSeconds = 60,
            SafetyBuffer = 100
        };
    }

    [Fact]
    public async Task ReserveTokensAsync_WithAvailableCapacity_ShouldReturnImmediately()
    {
        // Arrange
        var rateLimiter = new TokenRateLimiter(_options, _logger);

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        using var reservation = await rateLimiter.ReserveTokensAsync(100);
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100);
        reservation.ReservedTokens.Should().Be(100);
        rateLimiter.GetReservedTokens().Should().Be(100);
    }

    [Fact]
    public async Task CompleteAsync_ShouldRecordActualUsageAndReleaseReservation()
    {
        // Arrange
        var rateLimiter = new TokenRateLimiter(_options, _logger);

        // Act
        var reservation = await rateLimiter.ReserveTokensAsync(100);
        var initialReserved = rateLimiter.GetReservedTokens();

        await reservation.CompleteAsync(80);

        // Assert
        initialReserved.Should().Be(100);
        rateLimiter.GetReservedTokens().Should().Be(0);
        reservation.ActualTokensUsed.Should().Be(80);
        reservation.IsDisposed.Should().BeTrue();
    }
}