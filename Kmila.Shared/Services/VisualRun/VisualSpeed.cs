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
