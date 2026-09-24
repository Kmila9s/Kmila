namespace Kmila.Shared.Services.VisualRun;

// Bounded ring buffer for visual-mode frames. Infinite-run sims cannot
// retain every tick — older entries evict; scrubbing is limited to the
// ring window. Phase 0 uses this only as a thin record store; scrub UI
// arrives in Phase 2.
public readonly record struct VisualFrame(
    ulong Tick,
    IReadOnlyDictionary<string, string> PortValues);

public sealed class FrameRing
{
    private readonly VisualFrame[] _buf;
    private long _head;
    private long _count;
    private readonly object _gate = new();

    public int Capacity { get; }

    public FrameRing(int capacity = 2000)
    {
        if (capacity < 1) capacity = 1;
        Capacity = capacity;
        _buf = new VisualFrame[capacity];
    }

    public long Count { get { lock (_gate) return _count; } }

    public ulong NewestTick
    {
        get
        {
            lock (_gate)
            {
                if (_count == 0) return 0;
                return _buf[(_head - 1 + Capacity) % Capacity].Tick;
            }
        }
    }

    public ulong OldestTick
    {
        get
        {
            lock (_gate)
            {
                if (_count == 0) return 0;
                long start = _count < Capacity ? 0 : _head;
                return _buf[start].Tick;
            }
        }
    }

    public void Push(VisualFrame f)
    {
        lock (_gate)
        {
            _buf[_head] = f;
            _head = (_head + 1) % Capacity;
            if (_count < Capacity) _count++;
        }
    }

    public VisualFrame? Latest()
    {
        lock (_gate)
        {
            if (_count == 0) return null;
            return _buf[(_head - 1 + Capacity) % Capacity];
        }
    }

    // Phase 2 scrub lookup. Linear scan over the ring window (≤Capacity
    // entries — 2000 default) costs ~tens of µs and runs at user-input rates
    // only (not per-tick), so plenty of headroom. Ticks are written in
    // monotonic order so an early break is safe.
    /// <summary>Returns the frame whose tick exactly matches the argument, or null if the tick is outside the ring window. O(N) in worst case but stops early once past the target tick.</summary>
    public VisualFrame? TryGetByTick(ulong tick)
    {
        lock (_gate)
        {
            if (_count == 0) return null;
            long start = _count < Capacity ? 0 : _head;
            for (long i = 0; i < _count; i++)
            {
                long idx = (start + i) % Capacity;
                var f = _buf[idx];
                if (f.Tick == tick) return f;
                if (f.Tick > tick) break; // past the requested tick
            }
            return null;
        }
    }

    // Phase 2 scrub helper: return the frame closest to `tick` within the
    // ring window (nearest neighbour, no interpolation). Returns null only
    // when the ring is empty.
    /// <summary>Returns the frame closest to <paramref name="tick"/> within the ring window (nearest neighbor, no interpolation). Used by the scrub slider so any requested position snaps to the nearest captured frame. Returns null only when the ring is empty.</summary>
    public VisualFrame? Nearest(ulong tick)
    {
        lock (_gate)
        {
            if (_count == 0) return null;
            long start = _count < Capacity ? 0 : _head;
            VisualFrame best = _buf[start];
            ulong bestDist = AbsDiff(best.Tick, tick);
            for (long i = 1; i < _count; i++)
            {
                long idx = (start + i) % Capacity;
                var f = _buf[idx];
                ulong d = AbsDiff(f.Tick, tick);
                if (d < bestDist) { best = f; bestDist = d; }
                if (f.Tick > tick) break;
            }
            return best;
        }
    }

    private static ulong AbsDiff(ulong a, ulong b) => a > b ? a - b : b - a;

    public void Clear()
    {
        lock (_gate) { _head = 0; _count = 0; }
    }
}
