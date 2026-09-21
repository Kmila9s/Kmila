namespace Kmila.Shared.Services.VisualRun;

public enum VisualSpeed
{
    Half,
    Normal,
    Fast,
    Free,
}

public static class VisualSpeedExtensions
{
    public static int TickDelayMs(this VisualSpeed s) => s switch
    {
        VisualSpeed.Half => 32,
        VisualSpeed.Normal => 16,
        VisualSpeed.Fast => 2,
        VisualSpeed.Free => 0,
        _ => 16,
    };

    public static string DisplayLabel(this VisualSpeed s) => s switch
    {
        VisualSpeed.Half => "0.5x",
        VisualSpeed.Normal => "1x",
        VisualSpeed.Fast => "10x",
        VisualSpeed.Free => "Free",
        _ => "1x",
    };

    // Phase 2: slider-index mapping. 0..3 ↔ Half/Normal/Fast/Free. Out-of-range
    // collapses to Normal — never throws, the UI sets the slider via this.
    public static VisualSpeed FromSliderIndex(int i) => i switch
    {
        0 => VisualSpeed.Half,
        1 => VisualSpeed.Normal,
        2 => VisualSpeed.Fast,
        3 => VisualSpeed.Free,
        _ => VisualSpeed.Normal,
    };

    public static int ToSliderIndex(this VisualSpeed s) => s switch
    {
        VisualSpeed.Half => 0,
        VisualSpeed.Normal => 1,
        VisualSpeed.Fast => 2,
        VisualSpeed.Free => 3,
        _ => 1,
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
public static class VisualSpeedScale
{
    public const int SliderMax = 100;
    public const int SlowestMs = 1000; // far-left finite delay (~1 tick/s)
    public const int FastestMs = 1;    // just-before-Free finite delay

    // Slider position (0..SliderMax) → per-tick delay in ms. SliderMax → 0 (Free).
    public static int DelayFromSlider(int pos)
    {
        pos = Math.Clamp(pos, 0, SliderMax);
        if (pos >= SliderMax) return 0; // Free
        double frac = pos / (double)(SliderMax - 1); // 0..1 across the finite range
        double delay = SlowestMs * Math.Pow((double)FastestMs / SlowestMs, frac);
        return Math.Max(FastestMs, (int)Math.Round(delay));
    }

    // Per-tick delay in ms → nearest slider position. 0/negative (Free) → SliderMax.
    public static int SliderFromDelay(int delayMs)
    {
        if (delayMs <= 0) return SliderMax; // Free
        delayMs = Math.Clamp(delayMs, FastestMs, SlowestMs);
        double frac = Math.Log(delayMs / (double)SlowestMs)
                    / Math.Log((double)FastestMs / SlowestMs);
        int pos = (int)Math.Round(frac * (SliderMax - 1));
        return Math.Clamp(pos, 0, SliderMax - 1);
    }

    // Readout label. null / 0 → the caller supplies the localized "Free" text;
    // any positive delay renders as "{d} ms/tick · ~{n}/s".
    public static string DelayLabel(int? delayMs, string freeText)
    {
        if (delayMs is null || delayMs <= 0) return freeText;
        int perSec = (int)Math.Round(1000.0 / delayMs.Value);
        return $"{delayMs.Value} ms/tick · ~{perSec}/s";
    }
}
