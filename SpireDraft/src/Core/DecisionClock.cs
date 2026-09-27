namespace SpireDraft.Core;

/// <summary>One-shot countdown. Refreshes and state changes always invalidate elapsed time.</summary>
public sealed class DecisionClock
{
    private string? _fingerprint;
    private double _elapsed;
    public bool Cancelled { get; private set; }
    public bool Committed { get; private set; }
    public void Cancel() { Cancelled = true; _elapsed = 0; }
    public void Restart() { Cancelled = false; _elapsed = 0; _fingerprint = null; }
    public void Reset() { Restart(); Committed = false; }
    public double Remaining(double delay) => Math.Max(0, delay - _elapsed);

    public bool Tick(string fingerprint, bool active, double delta, double delay)
    {
        if (Committed || Cancelled) return false;
        if (_fingerprint != fingerprint) { _fingerprint = fingerprint; _elapsed = 0; }
        if (!active || !double.IsFinite(delta) || delta < 0 || !double.IsFinite(delay) || delay < 1)
        { _elapsed = 0; return false; }
        _elapsed += Math.Min(delta, 0.2); // A stalled frame cannot silently consume the countdown.
        if (_elapsed < delay) return false;
        Committed = true;
        return true;
    }
}
