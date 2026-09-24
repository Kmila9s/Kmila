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
    /// Key for storing the last-picked FPGA's JSON path so it can be restored
    /// across app restarts. SelectedFPGA is a runtime singleton with no
    /// persistence of its own; without this key every cold-start would force
    /// the user to re-pick a board before being able to Build / Run.
    /// </summary>
    public const string KEY_FPGA_PATH = "KMILA_FPGA_PATH";

    /// <summary>
    /// Key for the default Editor dock-tab to surface when a project opens.
    /// Useful for share-link "open this project on the schematic view" flows
    /// and for automation harnesses that can't drive the dock-tab buttons via
    /// scripted touch events. Falls back to EditorTab.Entities when unset.
    /// </summary>
    public const string KEY_DEFAULT_DOCK_TAB = "KMILA_DEFAULT_DOCK_TAB";

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

    /// <summary>
    /// Key for the opt-in live-replay accumulation toggle. When "true",
    /// <c>ReplayState</c> records every delta-cycle frame into the live
    /// timeline while a simulation runs, so the user can scrub frame-by-frame
    /// while it's still going. When "false" (default), only the post-hoc
    /// replay built at end-of-sim is available — the heavy per-frame snapshot
    /// allocation that can grow into GB-territory on long runs is skipped.
    /// Stored as "true" / "false" strings; any other value falls back to
    /// false on read.
    /// </summary>
    public const string KEY_LIVE_REPLAY_DURING_RUN = "KMILA_LIVE_REPLAY_DURING_RUN";

    /// <summary>
    /// Tier 5.2 Phase B — opt-in switch that boots the embedded Kestrel host
    /// serving <c>/api/v1</c> inside the MAUI app process. When "true", the
    /// MAUI startup hydration (see MauiProgram.cs) resolves EmbeddedApiHost
    /// and calls StartAsync with <see cref="KEY_LOCAL_API_PORT"/> and
    /// <see cref="KEY_LOCAL_API_BIND_LAN"/>. Default OFF — the surface is
    /// unauthenticated dev-parity with Kmila.Web, so the user opts in.
    /// </summary>
    public const string KEY_LOCAL_API_ENABLED = "KMILA_LOCAL_API_ENABLED";

    /// <summary>
    /// Preferred TCP port for the embedded local API. Stored as an int
    /// string (default "5298" — matches SimulatorBench's fallback for
    /// zero-config bench parity). "0" tells Kestrel to auto-pick an
    /// ephemeral port; the effective URL is then read from
    /// <c>IServerAddressesFeature</c> and surfaced in Settings.
    /// </summary>
    public const string KEY_LOCAL_API_PORT = "KMILA_LOCAL_API_PORT";

    /// <summary>
    /// When "true", the embedded local API binds <c>IPAddress.Any</c>
    /// instead of loopback so other devices on the LAN can hit it (useful
    /// for physical-device bench runs). Default OFF; Settings shows a red
    /// warning banner when enabled — the surface is unauthenticated when
    /// <see cref="KEY_LOCAL_API_TOKEN"/> is empty.
    /// </summary>
    public const string KEY_LOCAL_API_BIND_LAN = "KMILA_LOCAL_API_BIND_LAN";

    /// <summary>
    /// Optional bearer token gating every <c>/api/v1</c> request. When
    /// non-empty, callers must present <c>Authorization: Bearer &lt;token&gt;</c>
    /// (or <c>X-Api-Key: &lt;token&gt;</c>) or receive 401; <c>/health</c> and
    /// the static Swagger UI assets stay exempt. Empty string / missing =
    /// wide-open (dev-parity default). Read by both hosts via
    /// <c>LocalApiTokenProvider</c> so Settings can regenerate without a
    /// host restart.
    /// </summary>
    public const string KEY_LOCAL_API_TOKEN = "KMILA_LOCAL_API_TOKEN";

    /// <summary>
    /// Visual-mode ring buffer size in ticks. The infinite-run coordinator
    /// keeps the last N frames so the user can scrub backward; older entries
    /// evict. Stored as an int string ("2000"); parse failures fall back to
    /// the per-doc default in the .kviz settings block, then to 2000. Range
    /// clamp 256..16384.
    /// </summary>
    public const string KEY_VISUAL_RING_BUFFER = "KMILA_VISUAL_RING_BUFFER";

    /// <summary>
    /// Editor sync mode (2026-07-06). Controls how Code↔Blocks cross-editor
    /// sync fires. "auto" (default) syncs on every view-tab switch — a Code
    /// edit is instantly visible in the Blocks canvas and vice-versa. "manual"
    /// requires the user to click Save before propagating; tab-switch shows
    /// the last-synced state. Run/Build always sync regardless of mode.
    /// Stored as "auto" / "manual"; any other value falls back to auto.
    /// </summary>
    public const string KEY_EDITOR_SYNC_MODE = "KMILA_EDITOR_SYNC_MODE";

    /// <summary>
    /// Satisfaction-form prompt: "never show again" flag. "true" once the user
    /// dismisses the occasional survey invite permanently. No survey data is
    /// ever sent by the app — the prompt only opens an external link.
    /// </summary>
    public const string KEY_SATISFACTION_DISMISSED = "KMILA_SATISFACTION_DISMISSED";

    /// <summary>
    /// Satisfaction-form prompt: "already answered" flag. "true" once the user
    /// marks the survey as answered; the prompt then offers an "answer again"
    /// action instead of the first-time copy.
    /// </summary>
    public const string KEY_SATISFACTION_ANSWERED = "KMILA_SATISFACTION_ANSWERED";

    /// <summary>
    /// Satisfaction-form prompt: last-shown timestamp (UTC ISO-8601). Used to
    /// rate-limit the occasional invite so it never nags on consecutive visits.
    /// </summary>
    public const string KEY_SATISFACTION_LAST_SHOWN = "KMILA_SATISFACTION_LAST_SHOWN";

    #endregion
}

/// <summary>
/// Cross-editor (Code↔Blocks) sync policy. Persisted via
/// <see cref="AppSettings.KEY_EDITOR_SYNC_MODE"/>.
/// </summary>
public enum EditorSyncMode
{
    /// <summary>Sync fires on every Code↔Blocks tab switch (default; pre-2026-07-06 behavior).</summary>
    Auto,
    /// <summary>Sync only fires on explicit Save. Tab-switch shows the last-synced state.</summary>
    Manual
}

/// <summary>
/// Persistence helpers for <see cref="EditorSyncMode"/>.
/// </summary>
public static class EditorSyncModeExtensions
{
    public static string ToToken(this EditorSyncMode mode) => mode switch
    {
        EditorSyncMode.Manual => "manual",
        _                     => "auto"
    };

    public static EditorSyncMode ParseToken(string? token) => (token ?? string.Empty).Trim().ToLowerInvariant() switch
    {
        "manual" => EditorSyncMode.Manual,
        _        => EditorSyncMode.Auto
    };
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
