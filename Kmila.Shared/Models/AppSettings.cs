using LibraryCompiler.Services;
using SQLite;

namespace Kmila.Shared.Models;

/// <summary>
/// Represents application settings stored in the SQLite database.
/// Uses a key-value pair approach for flexibility.
/// </summary>
[Table("AppSettings")]
public class AppSettings
{
    /// <summary>
    /// Gets or sets the primary key.
    /// </summary>
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the setting key.
    /// </summary>
    [Unique]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the setting value.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the last modified timestamp.
    /// </summary>
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    #region Setting Keys Constants

    /// <summary>
    /// Key for storing the selected language.
    /// </summary>
    public const string KEY_LANGUAGE = "KMILA_LANGUAGE";

    /// <summary>
    /// Key for storing the selected theme.
    /// </summary>
    public const string KEY_THEME = "KMILA_THEME";

    /// <summary>
    /// Key for the user-chosen text-size step (xs/sm/md/lg/xl). Drives the
    /// --user-text-scale CSS variable on &lt;html&gt; so all rem-based type
    /// tokens scale uniformly. Persisted in SQLite for native parity; the
    /// browser-side localStorage copy ('kmila-text-scale') is read by the
    /// inline &lt;head&gt; boot script to apply the size pre-FOUC.
    /// </summary>
    public const string KEY_TEXT_SCALE = "KMILA_TEXT_SCALE";

    /// <summary>
    /// Key for the simulation tick-resolution mode. See <see cref="TickResolutionMode"/>.
    /// Controls how fine-grained the discrete-time simulator's tick is, which
    /// caps the maximum clock frequency the waveform can faithfully represent
    /// (Nyquist: at least two ticks per period).
    /// </summary>
    public const string KEY_TICK_RESOLUTION_MODE = "KMILA_TICK_RESOLUTION_MODE";

    /// <summary>
    /// Key for the LibraryCompiler build-policy mode (OnDemand / ProcessCached
    /// / Persistent). Controls how aggressively compiled IEEE + user library
    /// artifacts are retained between builds and across sessions.
    /// </summary>
    public const string KEY_LIBRARY_BUILD_MODE = "KMILA_LIBRARY_BUILD_MODE";

    #endregion
}

/// <summary>
/// How the simulator's ns-per-tick is chosen for a given run.
/// Persisted as a string through <see cref="AppSettings.KEY_TICK_RESOLUTION_MODE"/>
/// so future additions don't break older databases.
/// </summary>
public enum TickResolutionMode
{
    /// <summary>10 ns/tick. Caps faithful clock representation at 50 MHz. Default for backwards compatibility with existing snapshots and lessons.</summary>
    Default,
    /// <summary>1 ns/tick. Accurate up to 500 MHz. ~10× tick count per run: ~10× slower and ~10× more SignalHistory memory.</summary>
    HighPrecision,
    /// <summary>Tick computed per-run from the effective clock frequency so there are always ≥2 ticks per period. Only pays the precision cost when the design actually needs it.</summary>
    Adaptive
}

/// <summary>
/// Helpers for <see cref="TickResolutionMode"/> persistence and resolution.
/// Kept as a static class (not extension methods on the enum) so the
/// serialization format — stable string tokens, never the underlying int —
/// is visible at the call sites that write to SQLite.
/// </summary>
public static class TickResolutionModeExtensions
{
    /// <summary>Stable string token written to AppSettings. Parsed case-insensitively on read.</summary>
    public static string ToToken(this TickResolutionMode mode) => mode switch
    {
        TickResolutionMode.HighPrecision => "high",
        TickResolutionMode.Adaptive      => "adaptive",
        _                                 => "default"
    };

    /// <summary>Parse the token back. Falls back to <see cref="TickResolutionMode.Default"/> for null / unknown — never throws so a corrupted setting can't break Run.</summary>
    public static TickResolutionMode ParseToken(string? token) => (token ?? string.Empty).Trim().ToLowerInvariant() switch
    {
        "high"     => TickResolutionMode.HighPrecision,
        "adaptive" => TickResolutionMode.Adaptive,
        _          => TickResolutionMode.Default
    };

    /// <summary>
    /// Resolve the effective ns-per-tick for a run given the mode and the
    /// clock frequency. <paramref name="systemClockMhz"/> is only consulted
    /// in <see cref="TickResolutionMode.Adaptive"/>. Result is clamped to a
    /// minimum of 1 ns so arithmetic stays well-defined.
    /// </summary>
    public static int ResolveTickResNs(this TickResolutionMode mode, int systemClockMhz)
    {
        switch (mode)
        {
            case TickResolutionMode.HighPrecision:
                return 1;
            case TickResolutionMode.Adaptive:
                if (systemClockMhz <= 0) return 10;
                // Two ticks per period ⇒ tick ≤ half-period. Floor so the
                // resulting frequency is ≥ requested, never aliased below.
                double halfPeriodNs = 1_000.0 / systemClockMhz / 2.0;
                int tick = (int)Math.Floor(halfPeriodNs);
                return Math.Max(1, Math.Min(10, tick));
            default:
                return 10;
        }
    }
}

/// <summary>
/// Persistence helpers for the LibraryCompiler's <see cref="LibraryBuildMode"/>
/// — stable string tokens (never the underlying int) so future enum
/// additions don't break older databases. Mirrors the tick-resolution
/// pattern above.
/// </summary>
public static class LibraryBuildModeExtensions
{
    /// <summary>Stable string token written to AppSettings.</summary>
    public static string ToToken(this LibraryBuildMode mode) => mode switch
    {
        LibraryBuildMode.OnDemand      => "ondemand",
        LibraryBuildMode.ProcessCached => "process",
        LibraryBuildMode.Persistent    => "persistent",
        _                              => "persistent"
    };

    /// <summary>Parse the token back. Falls back to <see cref="LibraryBuildMode.Persistent"/>.</summary>
    public static LibraryBuildMode ParseToken(string? token) => (token ?? string.Empty).Trim().ToLowerInvariant() switch
    {
        "ondemand" => LibraryBuildMode.OnDemand,
        "process"  => LibraryBuildMode.ProcessCached,
        _          => LibraryBuildMode.Persistent
    };

    /// <summary>Wraps a single mode in a <see cref="LibraryBuildPolicy"/> with that default.</summary>
    public static LibraryBuildPolicy ToPolicy(LibraryBuildMode mode) => mode switch
    {
        LibraryBuildMode.OnDemand      => LibraryBuildPolicy.AlwaysOnDemand,
        LibraryBuildMode.ProcessCached => LibraryBuildPolicy.AlwaysProcessCached,
        _                              => LibraryBuildPolicy.AlwaysPersistent
    };
}
