public sealed class FallbackGuard
{
    public bool InFallback { get; private set; }

    public bool TryEnter()
    {
        if (InFallback) return false;
        InFallback = true;
        return true;
    }
}