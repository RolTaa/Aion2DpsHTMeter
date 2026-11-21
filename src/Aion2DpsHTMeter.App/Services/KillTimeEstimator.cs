namespace Aion2DpsHTMeter.App.Services;

/// <summary>
/// Estimates when the target dies from how fast its HP fell over the last <see cref="Window"/>, so a phase change or
/// a member dying shows in the estimate within seconds. Kept free of WPF so the tests can use it.
/// </summary>
public sealed class KillTimeEstimator
{
    public static readonly TimeSpan Window = TimeSpan.FromSeconds(20);

    /// <summary>Fewer seconds of samples than this give no estimate (the first hits say little).</summary>
    private static readonly TimeSpan MinSpan = TimeSpan.FromSeconds(3);

    private readonly Queue<(long Ms, long Hp)> _samples = new();
    private uint _target;

    /// <summary>Adds the target's HP seen at <paramref name="nowMs"/> and returns the time left, or null when unknown.</summary>
    public TimeSpan? Update(uint target, long nowMs, long hp)
    {
        if (target != _target)
        {
            _target = target;
            _samples.Clear();
        }
        if (_samples.Count > 0 && hp > _samples.Last().Hp) _samples.Clear(); // healed or reset: start over
        _samples.Enqueue((nowMs, hp));
        while (_samples.Count > 2 && nowMs - _samples.Peek().Ms > Window.TotalMilliseconds) _samples.Dequeue();

        var (firstMs, firstHp) = _samples.Peek();
        var spanMs = nowMs - firstMs;
        var lost = firstHp - hp;
        if (hp <= 0 || spanMs < MinSpan.TotalMilliseconds || lost <= 0) return null;
        return TimeSpan.FromMilliseconds(hp * (double)spanMs / lost);
    }
}
