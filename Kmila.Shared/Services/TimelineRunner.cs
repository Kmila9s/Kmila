using Kmila.Shared.Models;

namespace Kmila.Shared.Services;

/// <summary>
/// Pure-C# state machine that drives an <c>ExecutionPlayer</c> through a
/// pre-recorded <see cref="ExecutionTimelineDoc"/>. Not registered in DI —
/// instantiated per player instance so each lesson page can drive its own
/// playback without singleton coupling.
/// </summary>
/// <remarks>
/// Playback is implemented with a cancellation-token Task.Delay loop rather
/// than a Timer so cancellation is precise and Blazor's render thread doesn't
/// get poked from a thread-pool callback. Speed is expressed as a multiplier
/// against <see cref="BaseStepMs"/> (e.g. 2.0 = each step lasts half as long).
/// </remarks>
public class TimelineRunner
{
    /// <summary>Wall-clock duration of one step at 1× speed.</summary>
    public const int BaseStepMs = 900;

    private ExecutionTimelineDoc _doc = new();
    private CancellationTokenSource? _playCts;

    /// <summary>0-based index into <see cref="ExecutionTimelineDoc.Steps"/>.</summary>
    public int CurrentIndex { get; private set; }

    /// <summary>Playback speed multiplier. 0.5 / 1.0 / 2.0 are the UI presets.</summary>
    public double Speed { get; set; } = 1.0;

    /// <summary>True while an autoplay loop is running.</summary>
    public bool IsPlaying => _playCts is { IsCancellationRequested: false };

    /// <summary>The active step (always safe to read after <see cref="Load"/>).</summary>
    public TimelineStep CurrentStep =>
        _doc.Steps.Count == 0
            ? new TimelineStep()
            : _doc.Steps[Math.Clamp(CurrentIndex, 0, _doc.Steps.Count - 1)];

    /// <summary>
    /// Full signal table at the active step — resolves delta steps (live tape)
    /// against their keyframe. Use this, not <c>CurrentStep.Signals</c>, for display.
    /// </summary>
    public Dictionary<string, string> CurrentSignals =>
        _doc.Steps.Count == 0
            ? new Dictionary<string, string>()
            : _doc.ResolveSignals(Math.Clamp(CurrentIndex, 0, _doc.Steps.Count - 1));

    /// <summary>True once the runner has stepped past the last step.</summary>
    public bool IsAtEnd => _doc.Steps.Count == 0 || CurrentIndex >= _doc.Steps.Count - 1;

    /// <summary>The loaded document — exposed for the player's render code.</summary>
    public ExecutionTimelineDoc Document => _doc;

    /// <summary>Fired on every step advance (manual or autoplay) and on restart.</summary>
    public event Action? OnTick;

    /// <summary>Fired when autoplay reaches the last step.</summary>
    public event Action? OnComplete;

    /// <summary>
    /// Swaps in a new timeline and resets to step 0. Cancels any in-flight
    /// playback so the player UI never advances against the wrong document.
    /// </summary>
    public void Load(ExecutionTimelineDoc doc)
    {
        Pause();
        _doc = doc ?? new ExecutionTimelineDoc();
        CurrentIndex = 0;
        OnTick?.Invoke();
    }

    /// <summary>Manual step forward. No-op once at the last step.</summary>
    public void StepForward()
    {
        if (_doc.Steps.Count == 0) return;
        if (CurrentIndex < _doc.Steps.Count - 1)
        {
            CurrentIndex++;
            OnTick?.Invoke();
        }
    }

    /// <summary>Manual step back. No-op at step 0.</summary>
    public void StepBack()
    {
        if (_doc.Steps.Count == 0) return;
        if (CurrentIndex > 0)
        {
            CurrentIndex--;
            OnTick?.Invoke();
        }
    }

    /// <summary>Jump back to step 0 and pause.</summary>
    public void Restart()
    {
        Pause();
        CurrentIndex = 0;
        OnTick?.Invoke();
    }

    /// <summary>Seek to an arbitrary step index (clamped to valid range).</summary>
    public void SeekTo(int index)
    {
        if (_doc.Steps.Count == 0) return;
        CurrentIndex = Math.Clamp(index, 0, _doc.Steps.Count - 1);
        OnTick?.Invoke();
    }

    /// <summary>
    /// Start auto-advancing. If already playing, no-op. If at the end,
    /// rewinds to step 0 first so the play button doubles as a replay button.
    /// </summary>
    public void Play()
    {
        if (IsPlaying) return;
        if (_doc.Steps.Count == 0) return;
        if (IsAtEnd) CurrentIndex = 0;

        _playCts = new CancellationTokenSource();
        var token = _playCts.Token;
        _ = PlayLoopAsync(token);
        OnTick?.Invoke();
    }

    /// <summary>Stop auto-advancing. Safe to call when already paused.</summary>
    public void Pause()
    {
        var cts = _playCts;
        _playCts = null;
        if (cts is null) return;
        try { cts.Cancel(); } catch { }
        cts.Dispose();
        OnTick?.Invoke();
    }

    private async Task PlayLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested && CurrentIndex < _doc.Steps.Count - 1)
        {
            var delay = Math.Max(80, (int)(BaseStepMs / Math.Max(0.1, Speed)));
            try
            {
                await Task.Delay(delay, token);
            }
            catch (TaskCanceledException)
            {
                return;
            }
            if (token.IsCancellationRequested) return;

            CurrentIndex++;
            OnTick?.Invoke();
        }

        if (!token.IsCancellationRequested)
        {
            _playCts = null;
            OnComplete?.Invoke();
            OnTick?.Invoke();
        }
    }
}
