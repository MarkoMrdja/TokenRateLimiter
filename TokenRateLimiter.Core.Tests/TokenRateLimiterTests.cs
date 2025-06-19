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
        await using var reservation = await rateLimiter.ReserveTokensAsync(100);
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100);
        reservation.ReservedTokens.Should().BeGreaterOrEqualTo(100); // Might include estimated output
        rateLimiter.GetReservedTokens().Should().BeGreaterOrEqualTo(100);
    }

    [Fact]
    public async Task RecordActualUsageAndDispose_ShouldRecordUsageAndReleaseReservation()
    {
        // Arrange
        var rateLimiter = new TokenRateLimiter(_options, _logger);

        // Act
        var reservation = await rateLimiter.ReserveTokensAsync(100);
        var initialReserved = rateLimiter.GetReservedTokens();

        reservation.RecordActualUsage(80);
        await reservation.DisposeAsync();

        // Assert
        initialReserved.Should().BeGreaterOrEqualTo(100);
        rateLimiter.GetReservedTokens().Should().Be(0);
        reservation.ActualTokensUsed.Should().Be(80);
    }

    [Fact]
    public async Task UsingStatement_ShouldAutomaticallyReleaseReservation()
    {
        // Arrange
        var rateLimiter = new TokenRateLimiter(_options, _logger);

        // Act & Assert
        await using (var reservation = await rateLimiter.ReserveTokensAsync(100))
        {
            rateLimiter.GetReservedTokens().Should().BeGreaterOrEqualTo(100);
            reservation.RecordActualUsage(90);
        }

        // After disposal
        rateLimiter.GetReservedTokens().Should().Be(0);
    }

    [Fact]
    public async Task MultipleReservations_ShouldTrackCorrectly()
    {
        // Arrange
        var rateLimiter = new TokenRateLimiter(_options, _logger);

        // Act
        await using var reservation1 = await rateLimiter.ReserveTokensAsync(100);
        await using var reservation2 = await rateLimiter.ReserveTokensAsync(200);

        // Assert
        rateLimiter.GetReservedTokens().Should().BeGreaterOrEqualTo(300);
        
        reservation1.RecordActualUsage(80);
        reservation2.RecordActualUsage(180);
    }

    [Fact]
    public async Task FailedRequest_WithoutRecordingUsage_ShouldNotAffectUsageStats()
    {
        // Arrange
        var rateLimiter = new TokenRateLimiter(_options, _logger);
        var initialUsage = rateLimiter.GetCurrentUsage();

        // Act - simulate failed request (no RecordActualUsage call)
        await using (var reservation = await rateLimiter.ReserveTokensAsync(100))
        {
            // Don't call RecordActualUsage - simulates failure
        }

        // Assert
        var finalUsage = rateLimiter.GetCurrentUsage();
        finalUsage.Should().Be(initialUsage); // Usage should be unchanged
    }

    [Fact]
    public async Task SuccessfulRequest_WithRecordedUsage_ShouldIncreaseUsageStats()
    {
        // Arrange
        var rateLimiter = new TokenRateLimiter(_options, _logger);
        var initialUsage = rateLimiter.GetCurrentUsage();

        // Act
        await using (var reservation = await rateLimiter.ReserveTokensAsync(100))
        {
            reservation.RecordActualUsage(120); // More than estimated
        }

        // Assert
        var finalUsage = rateLimiter.GetCurrentUsage();
        finalUsage.Should().BeGreaterThan(initialUsage);
    }

    [Fact]
    public async Task ExplicitOutputEstimation_ShouldReserveCorrectAmount()
    {
        // Arrange
        var rateLimiter = new TokenRateLimiter(_options, _logger);

        // Act
        await using var reservation = await rateLimiter.ReserveTokensAsync(
            inputTokens: 100, 
            estimatedOutputTokens: 50);

        // Assert
        reservation.ReservedTokens.Should().Be(150); // 100 input + 50 output
        reservation.InputTokens.Should().Be(100);
    }
}