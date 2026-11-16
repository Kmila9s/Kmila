namespace Kmila.Shared.Services.VisualRun;

/// <summary>Discrete playback speed presets for the Visual Run mode.</summary>
public enum VisualSpeed
{
    /// <summary>Half speed (0.5x).</summary>
    Half,
    /// <summary>Normal speed (1x).</summary>
    Normal,
    /// <summary>Fast speed (10x).</summary>
    Fast,
    /// <summary>Unthrottled — run as fast as the scheduler allows.</summary>
    Free,
}

/// <summary>Helpers mapping <see cref="VisualSpeed"/> presets to tick delays, labels, and slider indices.</summary>
public static class VisualSpeedExtensions
{
    /// <summary>Returns the per-tick delay in milliseconds for the given speed preset.</summary>
    /// <param name="s">The speed preset.</param>
    /// <returns>The per-tick delay in milliseconds (0 for <see cref="VisualSpeed.Free"/>).</returns>
    public static int TickDelayMs(this VisualSpeed s) => s switch
    {
        VisualSpeed.Half => 32,
        VisualSpeed.Normal => 16,
        VisualSpeed.Fast => 2,
        VisualSpeed.Free => 0,
        _ => 16,
    };

    /// <summary>Returns the short human-readable label (e.g. "1x") for the given speed preset.</summary>
    /// <param name="s">The speed preset.</param>
    /// <returns>The display label.</returns>
    public static string DisplayLabel(this VisualSpeed s) => s switch
    {
        VisualSpeed.Half => "0.5x",
        VisualSpeed.Normal => "1x",
        VisualSpeed.Fast => "10x",
        VisualSpeed.Free => "Free",
        _ => "1x",
    };

}

// Continuous full-range speed scale for the Visual Run playback slider.
//
// The slider is an integer position 0..SliderMax. Position SliderMax is
// "Free" (0 ms/tick — as fast as the scheduler allows). Every other position
// maps LOGARITHMICALLY onto a real per-tick delay between SlowestMs (far left,
// ~1 tick/s so a student can watch each transition) and FastestMs (just before
// Free). A log scale means each drag step changes the delay by a constant
// ratio — the natural feel for a value that spans three orders of magnitude.
//
// The runtime already consumes an arbitrary delay via
// VisualRunCoordinator.CustomTickDelayMs (0 = Free), so this class only owns
// the slider<->delay mapping and the human-readable label.
/// <summary>
/// Continuous full-range speed scale for the Visual Run playback slider, mapping
/// slider positions to per-tick delays (logarithmically) and back, plus a readout label.
/// </summary>
public static class VisualSpeedScale
{
    /// <summary>Maximum slider position; this position means "Free" (0 ms/tick).</summary>
    public const int SliderMax = 100;
    /// <summary>Far-left finite per-tick delay in milliseconds (~1 tick/s).</summary>
    public const int SlowestMs = 1000; // far-left finite delay (~1 tick/s)
    /// <summary>Fastest finite per-tick delay in milliseconds (just before Free).</summary>
    public const int FastestMs = 1;    // just-before-Free finite delay

    /// <summary>Converts a slider position (0..<see cref="SliderMax"/>) to a per-tick delay in ms; <see cref="SliderMax"/> yields 0 (Free).</summary>
    /// <param name="pos">The slider position.</param>
    /// <returns>The per-tick delay in milliseconds.</returns>
    public static int DelayFromSlider(int pos)
    {
        pos = Math.Clamp(pos, 0, SliderMax);
        if (pos >= SliderMax) return 0; // Free
        double frac = pos / (double)(SliderMax - 1); // 0..1 across the finite range
        double delay = SlowestMs * Math.Pow((double)FastestMs / SlowestMs, frac);
        return Math.Max(FastestMs, (int)Math.Round(delay));
    }

    /// <summary>Converts a per-tick delay in ms to the nearest slider position; 0 or negative (Free) yields <see cref="SliderMax"/>.</summary>
    /// <param name="delayMs">The per-tick delay in milliseconds.</param>
    /// <returns>The nearest slider position.</returns>
    public static int SliderFromDelay(int delayMs)
    {
        if (delayMs <= 0) return SliderMax; // Free
        delayMs = Math.Clamp(delayMs, FastestMs, SlowestMs);
        double frac = Math.Log(delayMs / (double)SlowestMs)
                    / Math.Log((double)FastestMs / SlowestMs);
        int pos = (int)Math.Round(frac * (SliderMax - 1));
        return Math.Clamp(pos, 0, SliderMax - 1);
    }

    /// <summary>
    /// Builds the readout label for a per-tick delay. Null or 0 returns the caller-supplied
    /// localized "Free" text; any positive delay renders as "{d} ms/tick · ~{n}/s".
    /// </summary>
    /// <param name="delayMs">The per-tick delay in milliseconds, or null for Free.</param>
    /// <param name="freeText">Localized text to return when the delay represents Free.</param>
    /// <returns>The readout label.</returns>
    public static string DelayLabel(int? delayMs, string freeText)
    {
        if (delayMs is null || delayMs <= 0) return freeText;
        int perSec = (int)Math.Round(1000.0 / delayMs.Value);
        return $"{delayMs.Value} ms/tick · ~{perSec}/s";
    }
}
