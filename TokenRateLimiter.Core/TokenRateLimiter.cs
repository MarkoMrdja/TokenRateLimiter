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

    // Concurrency control
    private readonly SemaphoreSlim _concurrencyLimiter;
    private readonly ReaderWriterLockSlim _usageLock = new(LockRecursionPolicy.NoRecursion);
    private readonly object _reservationLock = new();
    private readonly Random _random = new();

    // Timeline-based tracking for precision
    private readonly SortedDictionary<DateTime, int> _tokenUsageTimeline = new();
    private readonly SortedDictionary<DateTime, int> _requestTimeline = new();
    private readonly Dictionary<Guid, PendingReservation> _activeReservations = new();

    // Performance optimizations
    private int _cachedHistoricalUsage = 0;
    private DateTime _lastCleanup = DateTime.MinValue;
    private bool _cacheNeedsRefresh = false;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromSeconds(5);
    private volatile bool _disposed = false;

    public TokenRateLimiter(IOptions<TokenRateLimiterOptions> options, ILogger<TokenRateLimiter> logger)
    {
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _concurrencyLimiter = new SemaphoreSlim(_options.MaxConcurrentReservations, _options.MaxConcurrentReservations);
        
        ValidateOptions();
    }

    public TokenRateLimiter(TokenRateLimiterOptions options, ILogger<TokenRateLimiter> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _concurrencyLimiter = new SemaphoreSlim(_options.MaxConcurrentReservations, _options.MaxConcurrentReservations);
        
        ValidateOptions();
    }

    public async Task<TokenReservation> ReserveTokensAsync(int inputTokens, int estimatedOutputTokens = 0, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(TokenRateLimiter));

        if (inputTokens <= 0)
            throw new ArgumentException("Input tokens must be positive", nameof(inputTokens));

        if (estimatedOutputTokens < 0)
            throw new ArgumentException("Estimated output tokens cannot be negative", nameof(estimatedOutputTokens));

        // Calculate total estimated tokens with user-defined multiplier
        int totalEstimatedTokens = CalculateEstimatedTotalTokens(inputTokens, estimatedOutputTokens);

        // Step 1: Acquire concurrency slot
        await _concurrencyLimiter.WaitAsync(cancellationToken);
        
        try
        {
            var reservationId = Guid.NewGuid();
            var pendingReservation = new PendingReservation(reservationId, totalEstimatedTokens, DateTime.UtcNow);

            // Step 2: Wait for capacity (optimistic approach)
            await WaitForAvailableCapacityAsync(totalEstimatedTokens, cancellationToken);

            // Step 3: Atomic check-and-reserve
            bool shouldRetry = false;
            lock (_reservationLock)
            {
                // Double-check capacity after acquiring lock
                if (!HasCapacityInternal(totalEstimatedTokens))
                {
                    shouldRetry = true;
                }
                else
                {
                    // Make the reservation atomically
                    _activeReservations[reservationId] = pendingReservation;
                    RecordSuccessfulRequest();

                    _logger.LogDebug("Reserved {EstimatedTokens} tokens (input: {InputTokens}, estimated output: {EstimatedOutput}) with ID {ReservationId}, total active reservations: {ActiveCount}",
                        totalEstimatedTokens, inputTokens, estimatedOutputTokens, reservationId, _activeReservations.Count);
                }
            }

            // Handle retry outside the lock
            if (shouldRetry)
            {
                _concurrencyLimiter.Release();
                return await ReserveTokensAsync(inputTokens, estimatedOutputTokens, cancellationToken);
            }

            return new TokenReservation(reservationId, totalEstimatedTokens, inputTokens, ReleaseReservationAsync);
        }
        catch
        {
            _concurrencyLimiter.Release();
            throw;
        }
    }

    public int GetCurrentUsage()
    {
        _usageLock.EnterReadLock();
        try
        {
            var now = DateTime.UtcNow;
            
            // Only cleanup and recalculate if enough time has passed
            if (now - _lastCleanup > _cleanupInterval)
            {
                // Upgrade to write lock for cleanup
                _usageLock.ExitReadLock();
                _usageLock.EnterWriteLock();
                try
                {
                    if (now - _lastCleanup > _cleanupInterval) // Double-check pattern
                    {
                        CleanupOldUsageRecordsAndUpdateCache(now);
                    }
                }
                finally
                {
                    _usageLock.ExitWriteLock();
                    _usageLock.EnterReadLock();
                }
            }

            return GetCurrentUsageInternal();
        }
        finally
        {
            _usageLock.ExitReadLock();
        }
    }

    public int GetReservedTokens()
    {
        lock (_reservationLock)
        {
            return _activeReservations.Values.Sum(r => r.EstimatedTokens);
        }
    }

    public TokenUsageStats GetUsageStats()
    {
        _usageLock.EnterReadLock();
        try
        {
            int currentUsage = GetCurrentUsageInternal();
            int reservedTokens = GetReservedTokens();
            int availableTokens = Math.Max(0, _options.TokenLimit - _options.SafetyBuffer - currentUsage);
            
            lock (_reservationLock)
            {
                return new TokenUsageStats(
                    currentUsage,
                    reservedTokens,
                    availableTokens,
                    _activeReservations.Count,
                    GetCurrentRequestCount()
                );
            }
        }
        finally
        {
            _usageLock.ExitReadLock();
        }
    }

    private int CalculateEstimatedTotalTokens(int inputTokens, int estimatedOutputTokens)
    {
        if (estimatedOutputTokens > 0)
        {
            // User provided explicit estimate
            return inputTokens + estimatedOutputTokens;
        }

        // Apply default estimation strategy
        if (_options.OutputEstimationStrategy == OutputEstimationStrategy.FixedMultiplier)
        {
            return inputTokens + (int)Math.Ceiling(inputTokens * _options.OutputMultiplier);
        }
        else if (_options.OutputEstimationStrategy == OutputEstimationStrategy.FixedAmount)
        {
            return inputTokens + _options.DefaultOutputTokens;
        }
        else
        {
            // Conservative approach - assume output equals input
            return inputTokens * 2;
        }
    }

    private async Task WaitForAvailableCapacityAsync(int estimatedTokens, CancellationToken cancellationToken)
    {
        const int maxRetries = 10;
        int retryCount = 0;

        while (!cancellationToken.IsCancellationRequested && retryCount < maxRetries)
        {
            bool hasCapacity;
            
            _usageLock.EnterReadLock();
            try
            {
                hasCapacity = HasCapacityInternal(estimatedTokens);
            }
            finally
            {
                _usageLock.ExitReadLock();
            }

            if (hasCapacity)
            {
                return; // We have capacity
            }

            // Calculate wait time outside of locks to minimize lock contention
            int waitTimeMs;
            _usageLock.EnterReadLock();
            try
            {
                int currentUsage = GetCurrentUsageInternal();
                int tokenDeficit = (currentUsage + estimatedTokens) - (_options.TokenLimit - _options.SafetyBuffer);
                waitTimeMs = CalculateAdaptiveWaitTime(currentUsage, tokenDeficit);
            }
            finally
            {
                _usageLock.ExitReadLock();
            }

            _logger.LogInformation(
                "Token rate limit approached. Waiting {WaitTime}ms for capacity. " +
                "Attempt {Retry}/{MaxRetries}, Requested: {RequestedTokens}",
                waitTimeMs, retryCount + 1, maxRetries, estimatedTokens);

            await Task.Delay(waitTimeMs, cancellationToken);
            retryCount++;
        }

        if (retryCount >= maxRetries)
        {
            throw new TimeoutException($"Unable to acquire token capacity after {maxRetries} retries");
        }

        cancellationToken.ThrowIfCancellationRequested();
    }

    private bool HasCapacityInternal(int requiredTokens)
    {
        // This method assumes we're already in a read lock
        int currentUsage = GetCurrentUsageInternal();
        int effectiveLimit = _options.TokenLimit - _options.SafetyBuffer;
        
        // Check token capacity
        bool hasTokenCapacity = currentUsage + requiredTokens <= effectiveLimit;
        
        // Check request capacity
        bool hasRequestCapacity = GetCurrentRequestCount() < _options.MaxRequestsPerMinute;

        return hasTokenCapacity && hasRequestCapacity;
    }

    private int GetCurrentUsageInternal()
    {
        // This method assumes we're already in a read lock
        if (_cacheNeedsRefresh)
        {
            _cachedHistoricalUsage = _tokenUsageTimeline.Values.Sum();
            _cacheNeedsRefresh = false;
        }
        
        int reservedTokens;
        lock (_reservationLock)
        {
            reservedTokens = _activeReservations.Values.Sum(r => r.EstimatedTokens);
        }
        
        return _cachedHistoricalUsage + reservedTokens;
    }

    private int GetCurrentRequestCount()
    {
        var cutoff = DateTime.UtcNow.AddSeconds(-60);
        return _requestTimeline.Where(kv => kv.Key >= cutoff).Sum(kv => kv.Value);
    }

    private void RecordSuccessfulRequest()
    {
        var now = DateTime.UtcNow;
        if (_requestTimeline.TryGetValue(now, out var existing))
        {
            _requestTimeline[now] = existing + 1;
        }
        else
        {
            _requestTimeline[now] = 1;
        }
    }

    private void CleanupOldUsageRecordsAndUpdateCache(DateTime now)
    {
        DateTime tokenCutoff = now.AddSeconds(-_options.WindowSeconds);
        DateTime requestCutoff = now.AddMinutes(-2); // Keep 2 minutes of request history

        // Clean token timeline and recalculate cache
        var expiredTokenKeys = _tokenUsageTimeline.Keys.Where(k => k < tokenCutoff).ToList();
        foreach (var key in expiredTokenKeys)
        {
            _tokenUsageTimeline.Remove(key);
        }
        
        _cachedHistoricalUsage = _tokenUsageTimeline.Values.Sum();
        _cacheNeedsRefresh = false;

        // Clean request timeline
        var expiredRequestKeys = _requestTimeline.Keys.Where(k => k < requestCutoff).ToList();
        foreach (var key in expiredRequestKeys)
        {
            _requestTimeline.Remove(key);
        }

        // Clean stale reservations (safety measure)
        lock (_reservationLock)
        {
            var staleReservationCutoff = now.AddMinutes(-10);
            var staleIds = _activeReservations
                .Where(kv => kv.Value.CreatedAt < staleReservationCutoff)
                .Select(kv => kv.Key)
                .ToList();
                
            foreach (var id in staleIds)
            {
                _activeReservations.Remove(id);
                _logger.LogWarning("Removed stale reservation {ReservationId}", id);
            }
        }

        _lastCleanup = now;

        _logger.LogDebug("Cleaned up old records. Token usage: {TokenUsage}, Active reservations: {Reservations}, Request entries: {Requests}",
            _cachedHistoricalUsage, _activeReservations.Count, _requestTimeline.Count);
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

        bool reservationFound = false;
        
        try
        {
            _usageLock.EnterWriteLock();
            try
            {
                lock (_reservationLock)
                {
                    if (_activeReservations.Remove(reservation.Id))
                    {
                        reservationFound = true;
                        
                        if (reservation.ActualTokensUsed.HasValue)
                        {
                            // Record actual usage in timeline
                            var now = DateTime.UtcNow;
                            if (_tokenUsageTimeline.TryGetValue(now, out var existing))
                            {
                                _tokenUsageTimeline[now] = existing + reservation.ActualTokensUsed.Value;
                            }
                            else
                            {
                                _tokenUsageTimeline[now] = reservation.ActualTokensUsed.Value;
                            }

                            // Mark cache for refresh on next access
                            _cacheNeedsRefresh = true;

                            _logger.LogDebug(
                                "Released reservation {ReservationId}: {Reserved} → {Actual} tokens, active reservations: {ActiveCount}",
                                reservation.Id, reservation.ReservedTokens, reservation.ActualTokensUsed.Value, _activeReservations.Count);
                        }
                        else
                        {
                            _logger.LogDebug("Released reservation {ReservationId} without usage recording (likely failed request), active reservations: {ActiveCount}",
                                reservation.Id, _activeReservations.Count);
                        }
                    }
                }
            }
            finally
            {
                _usageLock.ExitWriteLock();
            }
        }
        finally
        {
            // Always release the concurrency slot if reservation was found
            if (reservationFound)
            {
                _concurrencyLimiter.Release();
            }
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
        if (_options.MaxConcurrentReservations <= 0)
            throw new ArgumentException("MaxConcurrentReservations must be positive");
        if (_options.MaxRequestsPerMinute <= 0)
            throw new ArgumentException("MaxRequestsPerMinute must be positive");
        if (_options.OutputMultiplier < 0)
            throw new ArgumentException("OutputMultiplier cannot be negative");
        if (_options.DefaultOutputTokens < 0)
            throw new ArgumentException("DefaultOutputTokens cannot be negative");
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _concurrencyLimiter.Dispose();
            _usageLock.Dispose();
        }
    }
}
