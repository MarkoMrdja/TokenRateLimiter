namespace TokenRateLimiter.Core.Models;

public class TokenReservation : IDisposable
{
    internal TokenReservation(int reservedTokens, Func<TokenReservation, Task> releaseAction)
    {
        ReservedTokens = reservedTokens;
        _releaseAction = releaseAction;
    }

    public int ReservedTokens { get; }
    public int? ActualTokensUsed { get; private set; }
    public bool IsDisposed { get; private set; }

    private readonly Func<TokenReservation, Task> _releaseAction;

    public async Task CompleteAsync(int actualTokensUsed)
    {
        if (IsDisposed) return;

        ActualTokensUsed = actualTokensUsed;
        await _releaseAction(this);
        IsDisposed = true;
    }

    public void Dispose()
    {
        if (!IsDisposed)
        {
            _ = Task.Run(() => _releaseAction(this));
            IsDisposed = true;
        }
    }
}
