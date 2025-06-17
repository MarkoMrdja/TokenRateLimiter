using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using TokenRateLimiter.Core.Abstractions;
using TokenRateLimiter.Core.Models;
using TokenRateLimiter.Core.Options;

namespace TokenRateLimiter.Core;

public class TokenRateLimiter : ITokenRateLimiter, IDisposable
{
    private readonly TokenRateLimiterOptions _options;
    private readonly ILogger<TokenRateLimiter> _logger;

    private readonly ConcurrentQueue<TokenUsageRecord> _tokenUsageHistory = new();
    private readonly SemaphoreSlim _reservationLock = new(1, 1);
    private readonly Random _random = new();

    private int _reservedTokens = 0;
    private int _cachedHistoricalUsage = 0;
    private DateTime _lastCleanup = DateTime.MinValue;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromSeconds(5);
    private volatile bool _disposed = false;

    public TokenRateLimiter(IOptions<TokenRateLimiterOptions> options, ILogger<TokenRateLimiter> logger)
    {
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ValidateOptions();
    }

    public TokenRateLimiter(TokenRateLimiterOptions options, ILogger<TokenRateLimiter> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ValidateOptions();
    }

    public async Task<TokenReservation> ReserveTokensAsync(int estimatedTokens, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(TokenRateLimiter));

        if (estimatedTokens <= 0)
            throw new ArgumentException("Estimated tokens must be positive", nameof(estimatedTokens));

        await WaitForAvailableCapacityAsync(estimatedTokens, cancellationToken);

        await _reservationLock.WaitAsync(cancellationToken);
        try
        {
            _reservedTokens += estimatedTokens;
            _logger.LogDebug("Reserved {EstimatedTokens} tokens, total reserved: {ReservedTokens}",
                estimatedTokens, _reservedTokens);

            return new TokenReservation(estimatedTokens, ReleaseReservationAsync);
        }
        finally
        {
            _reservationLock.Release();
        }
    }

    public int GetCurrentUsage()
    {
        var now = DateTime.UtcNow;

        // Only cleanup and recalculate if enough time has passed
        if (now - _lastCleanup > _cleanupInterval)
        {
            CleanupOldUsageRecordsAndUpdateCache(now);
        }

        return _cachedHistoricalUsage + _reservedTokens;
    }

    public int GetReservedTokens() => _reservedTokens;

    private async Task WaitForAvailableCapacityAsync(int estimatedTokens, CancellationToken cancellationToken)
    {
        int effectiveLimit = _options.TokenLimit - _options.SafetyBuffer;

        while (!cancellationToken.IsCancellationRequested)
        {
            await _reservationLock.WaitAsync(cancellationToken);
            try
            {
                int currentUsage = GetCurrentUsageInternal();
                if (currentUsage + estimatedTokens <= effectiveLimit)
                {
                    return;
                }

                int tokenDeficit = (currentUsage + estimatedTokens) - effectiveLimit;
                int waitTimeMs = CalculateAdaptiveWaitTime(currentUsage, tokenDeficit);

                _logger.LogWarning(
                    "Token rate limit approached. Current usage: {CurrentUsage}, Requested: {RequestedTokens}, " +
                    "Limit: {EffectiveLimit}. Waiting {WaitTime}ms before retry.",
                    currentUsage, estimatedTokens, effectiveLimit, waitTimeMs);

                _reservationLock.Release();

                await Task.Delay(waitTimeMs, cancellationToken);

                continue;
            }
            catch
            {
                _reservationLock.Release();
                throw;
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
    }

    private int GetCurrentUsageInternal()
    {
        return _cachedHistoricalUsage + _reservedTokens;
    }

    private void CleanupOldUsageRecordsAndUpdateCache(DateTime now)
    {
        DateTime cutoffTime = now.AddSeconds(-_options.WindowSeconds);

        while (_tokenUsageHistory.TryPeek(out var oldestRecord) && oldestRecord.Timestamp < cutoffTime)
        {
            _tokenUsageHistory.TryDequeue(out _);
        }

        _cachedHistoricalUsage = _tokenUsageHistory.Sum(record => record.TokenCount);
        _lastCleanup = now;

        _logger.LogDebug("Cleaned up old usage records. Historical usage: {HistoricalUsage}, Records: {RecordCount}",
            _cachedHistoricalUsage, _tokenUsageHistory.Count);
    }

    private int CalculateAdaptiveWaitTime(int currentUsage, int tokenDeficit)
    {
        double tokensExpiringPerSecond = currentUsage / (double)_options.WindowSeconds;

        int baseWaitMs;
        if (tokensExpiringPerSecond > 0)
        {
            baseWaitMs = (int)Math.Ceiling((tokenDeficit / tokensExpiringPerSecond) * 1000 * _options.WaitTimeMultiplier);
        }
        else
        {
            baseWaitMs = _options.MinWaitTimeMs * 2;
        }

        int jitter = _random.Next(0, _options.JitterRangeMs);
        int totalWaitMs = baseWaitMs + jitter;

        return Math.Max(_options.MinWaitTimeMs, Math.Min(totalWaitMs, _options.MaxWaitTimeMs));
    }

    private async Task ReleaseReservationAsync(TokenReservation reservation)
    {
        if (_disposed) return;

        await _reservationLock.WaitAsync();
        try
        {
            _reservedTokens -= reservation.ReservedTokens;
            if (_reservedTokens < 0) _reservedTokens = 0;

            if (reservation.ActualTokensUsed.HasValue)
            {
                var usageRecord = new TokenUsageRecord(DateTime.UtcNow, reservation.ActualTokensUsed.Value);
                _tokenUsageHistory.Enqueue(usageRecord);

                _lastCleanup = DateTime.MinValue;

                _logger.LogDebug(
                    "Released reservation of {ReservedTokens} tokens, recorded actual usage of {ActualTokens} tokens",
                    reservation.ReservedTokens, reservation.ActualTokensUsed.Value);
            }
            else
            {
                _logger.LogDebug("Released reservation of {ReservedTokens} tokens without recording usage",
                    reservation.ReservedTokens);
            }
        }
        finally
        {
            _reservationLock.Release();
        }
    }

    private void ValidateOptions()
    {
        if (_options.TokenLimit <= 0)
            throw new ArgumentException("TokenLimit must be positive");
        if (_options.WindowSeconds <= 0)
            throw new ArgumentException("WindowSeconds must be positive");
        if (_options.SafetyBuffer < 0)
            throw new ArgumentException("SafetyBuffer cannot be negative");
        if (_options.SafetyBuffer >= _options.TokenLimit)
            throw new ArgumentException("SafetyBuffer must be less than TokenLimit");
        if (_options.MinWaitTimeMs <= 0)
            throw new ArgumentException("MinWaitTimeMs must be positive");
        if (_options.MaxWaitTimeMs <= _options.MinWaitTimeMs)
            throw new ArgumentException("MaxWaitTimeMs must be greater than MinWaitTimeMs");
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _reservationLock.Dispose();
        }
    }
}
