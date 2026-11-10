using Kmila.Shared.Services.Trace;

namespace Kmila.Shared.Services;

/// <summary>Why the <see cref="RunWatchdog"/> stopped a run.</summary>
public enum RunStopReason
{
    /// <summary>No reason — the run may continue.</summary>
    None,

    /// <summary>
    /// The run's on-disk trace reached its size budget or the disk is nearly full.
    /// Stopping here (before the hard guard) keeps the archive complete.
    /// </summary>
    TraceStorageLimit,

    /// <summary>
    /// Managed memory stayed over budget after a full collection, or the OS
    /// reported critical memory pressure (Android onTrimMemory / onLowMemory).
    /// </summary>
    MemoryPressure,
}

/// <summary>
/// Long-run safety net. Polls a running simulation and stops it GRACEFULLY — through
/// the same path as the Stop button, so the partial waveform and its archive still
/// render — before the process runs out of disk or memory:
/// <list type="bullet">
///   <item>Trace store near its limit (<see cref="TraceSession.IsNearLimit"/>): stop while
///   the in-memory tail still fits, instead of letting the hard guard discard the archive.</item>
///   <item>Managed heap over <see cref="MaxManagedHeapBytes"/> even after a full GC.</item>
///   <item>Critical OS memory pressure reported through <see cref="ReportCriticalMemoryPressure"/>.</item>
/// </list>
/// Lives in the app layer: the Interpreter's existing <c>Stop()</c> is the only hook it needs.
/// One instance per host (DI singleton); <see cref="Watch"/> is called once per run.
/// </summary>
public sealed class RunWatchdog
{
    private static long _pressureStamp; // monotonic counter, bumped on each critical signal

    /// <summary>Managed-heap budget in bytes; 0 disables the heap check.</summary>
    public long MaxManagedHeapBytes { get; }

    /// <summary>How often a running simulation is checked. Default 500 ms.</summary>
    public TimeSpan PollInterval { get; init; } = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Creates the watchdog. <paramref name="maxManagedHeapBytes"/> &gt; 0 sets the heap
    /// budget explicitly (Android passes one derived from its memory class); 0 derives
    /// it from <see cref="DefaultHeapBudget"/>; a negative value disables the heap check.
    /// </summary>
    public RunWatchdog(long maxManagedHeapBytes = 0)
    {
        MaxManagedHeapBytes = maxManagedHeapBytes > 0 ? maxManagedHeapBytes
            : maxManagedHeapBytes < 0 ? 0
            : DefaultHeapBudget();
    }

    /// <summary>
    /// 60% of the memory the GC reports as available to the process (the container
    /// limit on a server, physical RAM on a desktop); 0 (= check disabled) when the
    /// runtime can't tell, e.g. Mono on iOS. The Phase-1 caps keep a healthy run far
    /// below this — it only trips on a genuine runaway.
    /// </summary>
    public static long DefaultHeapBudget()
    {
        try
        {
            long total = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
            return total > 0 ? (long)(total * 0.6) : 0;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Platform hook: the OS says memory is critically low (Android
    /// <c>onTrimMemory(RUNNING_CRITICAL / COMPLETE)</c>, <c>onLowMemory</c>). Any run
    /// being watched stops at its next poll. Safe to call from any thread.
    /// </summary>
    public static void ReportCriticalMemoryPressure() => Interlocked.Increment(ref _pressureStamp);

    /// <summary>Current critical-pressure counter (the <c>pressureSince</c> baseline for <see cref="Evaluate"/>).</summary>
    public static long PressureStamp => Interlocked.Read(ref _pressureStamp);

    /// <summary>
    /// Pure policy, for one poll. <paramref name="pressureSince"/> is the
    /// <see cref="ReportCriticalMemoryPressure"/> counter value when the run started.
    /// <paramref name="heapAfterCollect"/> is called only when the heap looks over
    /// budget; it should collect and return the heap size afterwards.
    /// </summary>
    public RunStopReason Evaluate(TraceSession? trace, long pressureSince,
        long heapBytes, Func<long> heapAfterCollect)
    {
        if (trace != null && trace.IsNearLimit)
            return RunStopReason.TraceStorageLimit;

        if (PressureStamp != pressureSince)
            return RunStopReason.MemoryPressure;

        // Over budget by the cheap estimate → confirm after a full collection, so
        // uncollected garbage between gen2 GCs never stops a healthy run.
        if (MaxManagedHeapBytes > 0 && heapBytes > MaxManagedHeapBytes
            && heapAfterCollect() > MaxManagedHeapBytes)
            return RunStopReason.MemoryPressure;

        return RunStopReason.None;
    }

    /// <summary>
    /// Starts watching one run. <paramref name="stop"/> is invoked at most once, on a
    /// timer thread, with the reason. Dispose the result when the run ends.
    /// </summary>
    public IDisposable Watch(TraceSession? trace, Action<RunStopReason> stop)
    {
        long pressureSince = PressureStamp;
        int fired = 0;
        int inflight = 0;
        Timer? timer = null;
        timer = new Timer(_ =>
        {
            if (Volatile.Read(ref fired) == 1) return;
            if (Interlocked.Exchange(ref inflight, 1) == 1) return;
            try
            {
                var reason = Evaluate(trace, pressureSince, GC.GetTotalMemory(false),
                    static () => GC.GetTotalMemory(forceFullCollection: true));
                if (reason != RunStopReason.None && Interlocked.Exchange(ref fired, 1) == 0)
                {
                    timer?.Change(Timeout.Infinite, Timeout.Infinite);
                    stop(reason);
                }
            }
            catch
            {
                // A failing check must never take the run (or the timer thread) down.
            }
            finally
            {
                Interlocked.Exchange(ref inflight, 0);
            }
        }, null, PollInterval, PollInterval);
        return timer;
    }
}
