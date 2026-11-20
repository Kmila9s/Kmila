namespace Kmila.CLI.Cli;

/// <summary>Usage/help text for the CLI. Kept in one place so `help`, bad
/// usage, and per-command <c>--help</c> stay consistent.</summary>
public static class HelpText
{
    /// <summary>Top-level usage shown by `kmila help` and on no/unknown command.</summary>
    public static string Root() => $"""
        kmila {Program.Version} — Kmila VHDL simulator, terminal head

        USAGE
          kmila <command> [options]

        COMMANDS
          sim        Run a simulation non-interactively (VCD/TXT/CSV/JSON/SVG output)
          ui         Open the full-screen terminal UI (waveform viewer + files + editor)
          check      Parse + lint a VHDL file (no simulation); CI-friendly
          diff       Compare two VCD waveform dumps
          practice   List / show / auto-grade the bundled practice exercises
          synth      Synthesize a design to an SVG schematic
          flow       Emit a PlantUML control-flow diagram
          fit        Estimate resource usage (optionally vs an --fpga device)
          blocks     Convert VHDL <-> block-editor workspace JSON
          serve      Host the local /api/v1 headless simulation API
          completion Print a shell completion script (bash|zsh|fish)
          version    Print the version
          help       Show this help

        Run `kmila <command> --help` for per-command options.
        """;

    /// <summary>Help for the <c>sim</c> sub-command.</summary>
    public static string Sim() => """
        kmila sim — simulate a VHDL design from the command line

        USAGE
          kmila sim --top-file <file.vhdl> [options]

        REQUIRED
          -t, --top-file, --main-file <path>   Primary VHDL source to simulate.

        SOURCES
          --file <path>            Additional source file (repeatable). Extra bare
                                   positional paths are treated the same way.
          --entity, --top <name>   Top-level entity to simulate
                                   (default: the top file's name without extension).
          --testbench, --tb <path> Run a VHDL testbench against the design instead
                                   of auto/manual stimulus; `report` output is logged.

        TIMING
          --duration <N[unit]>     Simulated duration. Unit: fs|ps|ns|us|ms|s
                                   (default ns). E.g. 50ns, 1us, 2000.  Default: 1000ns.
          --clockMHZ <int>         System clock in MHz. Default: from --fpga, else 50.
          --tick-res-ns <int>      Tick resolution in ns. Default: 10.

        TARGET
          --fpga <model>           FPGA device (e.g. EP4CE6, iCE40HX8K). Sets the
                                   default clock when --clockMHZ is omitted. Use
                                   `--fpga list` to list known devices.

        STIMULUS
          --stimulus <port@time=value>   Drive an input port at a time (repeatable).
                                         time may carry a unit, e.g. a@0=0 a@20ns=1.
          --clock <port[:period]>        Drive a clock on an input port (repeatable);
                                         period as a time (20ns) or a frequency (50MHz),
                                         default --clockMHZ. Without --clock no clock is
                                         driven (with --didactic: the clk/clock port).
          --initial <port=value>         Value of an input port before t=0 (no event),
                                         like a testbench signal's initial value.

        INITIAL VALUES
          (default)                      LRM, like GHDL: objects without := start at
                                         their type's left value ('U' for std_logic).
          --didactic                     Start objects without := and undriven inputs
                                         at 0 (en/enable at 1) and drive the clk port.

        ASSERTIONS (exit non-zero on any miss)
          --assert <sig@time=value>      Check a signal at a time (repeatable).
                                         value: 0/1/x/z, hex 0x1f, bin 0b101, or decimal.
          --assert-file <path>           One `sig@time=value` per line (# comments ok).

        OUTPUT
          --output-format <fmt>    vcd | txt | csv | json | svg    (default: vcd)
          -o, --output-file <path> Write here. Default: stdout for text formats;
                                   waveform.svg for svg.

        MISC
          --config <path>          Load defaults from a kmila.json (flags override it;
                                   default: ./kmila.json if present).
          --watch                  Re-run on source change until Ctrl+C.
          --no-breakpoints         Accepted for parity; the headless run never breaks.
          --lang <es|en>           UI language for messages. Default: es.
          --wall-timeout-ms <n>    Abort a run that exceeds this wall-clock budget.
          -q, --quiet              Only errors on stderr.
          -v, --verbose            Echo the engine log on stderr.
          -h, --help               Show this help.

        EXAMPLES
          kmila sim --top-file top.vhdl --duration 50ns --clockMHZ 50 -o out.vcd
          kmila sim -t alu.vhdl --file pkg.vhdl --entity alu --output-format json
          kmila sim -t counter.vhdl --fpga EP4CE6 --stimulus rst@0=1 --stimulus rst@30ns=0
        """;

    /// <summary>Help for the <c>ui</c> sub-command.</summary>
    public static string Ui() => """
        kmila ui — full-screen terminal UI (zero-dependency ANSI)

        USAGE
          kmila ui [options]

        OPTIONS
          --top-file, -t <path>    Pre-select this VHDL file as the top.
          --open <file.vcd>        Open an existing VCD waveform for viewing.
          --dir <path>            Start the file browser in this directory (default: cwd).
          --lang <es|en>           UI language. Default: es.
          --ascii                  Use ASCII-only glyphs (no box-drawing / Unicode).
          -h, --help               Show this help.

        KEYS (inside the UI)
          Tab  switch panel   ↑↓  navigate      Enter  open/select
          F5   simulate       +/- zoom waveform  < >    scroll time
          g    move cursor    e    edit file     s      save file
          o    open file      ?    help          q      quit
        """;

    /// <summary>Help for the <c>check</c> sub-command.</summary>
    public static string Check() => """
        kmila check — parse + lint a VHDL file (no simulation)

        USAGE
          kmila check <file.vhdl> [more.vhdl ...] [options]
          kmila check --top-file <file.vhdl> [--file <dep.vhdl> ...]

        OPTIONS
          --warnings-as-errors   Exit non-zero when there are warnings too.
          -q, --quiet            Only the summary line; suppress per-diagnostic output.
          -h, --help             Show this help.

        Diagnostics print on stderr as `file:line:col: severity [code]: message`.
        Exit: 0 clean, 1 if any error (or any warning with --warnings-as-errors).
        """;

    /// <summary>Help for the <c>diff</c> sub-command.</summary>
    public static string Diff() => """
        kmila diff — compare two VCD waveform dumps

        USAGE
          kmila diff <a.vcd> <b.vcd> [options]

        OPTIONS
          --signals <a,b,...>    Only compare these signals (repeatable/comma-list).
          --all                  Report every divergence per signal (default: first only).
          -q, --quiet            Only the summary line.
          -h, --help             Show this help.

        Divergences print on stdout; exit 0 if identical on compared signals, else 1.
        """;

    /// <summary>Help for the <c>practice</c> sub-command.</summary>
    public static string Practice() => """
        kmila practice — list / show / auto-grade the bundled exercises

        USAGE
          kmila practice list
          kmila practice show  <exercise-id>
          kmila practice grade <exercise-id> --solution <file.vhdl>
          kmila practice grade-all <dir> --exercise <exercise-id>

        OPTIONS
          --solution, -f <path>  Candidate VHDL to grade.
          --exercise, --id <id>  Exercise id (or give it as a positional).
          --lang <es|en>         Language for titles/statements. Default: es.
          -v, --verbose          Show the engine log during grading.
          -h, --help             Show this help.

        Verdict is BuildError > RuntimeError > WrongAnswer > Solved.
        Exit: 0 only when Solved (grade) or all solved (grade-all).
        """;

    /// <summary>Help for the <c>synth</c> sub-command.</summary>
    public static string Synth() => """
        kmila synth — synthesize a design to an SVG schematic

        USAGE
          kmila synth <file.vhdl> [--entity e] [-o schematic.svg] [options]

        OPTIONS
          --entity, --top <name>   Entity to synthesize (default: top file stem).
          --file <path>            Extra source file (repeatable).
          --mode <m>               raw | grouped | rtl | nodes   (default: grouped)
          --scope <s>              full | inputs | outputs        (default: full)
          --mono                   Grayscale (print-friendly).
          --no-title               Omit the title bar.
          -o, --output-file <p>    Write SVG here (default: stdout).
          -h, --help               Show this help.
        """;

    /// <summary>Help for the <c>flow</c> sub-command.</summary>
    public static string Flow() => """
        kmila flow — emit a PlantUML control-flow diagram

        USAGE
          kmila flow <file.vhdl> [-o flow.puml] [options]

        OPTIONS
          --entity, --top <name>   Preferred entity (default: top file stem).
          --file <path>            Extra source file (repeatable).
          -o, --output-file <p>    Output path. Default: stdout (PlantUML text).
                                   If it ends in .svg/.png and a `plantuml` binary
                                   is on PATH, it is rendered; otherwise a .puml is written.
          -h, --help               Show this help.
        """;

    /// <summary>Help for the <c>fit</c> sub-command.</summary>
    public static string Fit() => """
        kmila fit — estimate resource usage (optionally against a device)

        USAGE
          kmila fit <file.vhdl> [--fpga <model>] [options]

        OPTIONS
          --entity, --top <name>   Entity to analyze (default: top file stem).
          --file <path>            Extra source file (repeatable).
          --fpga <model>           Compare usage to this device's capacity.
                                   `--fpga list` lists devices.
          -h, --help               Show this help.

        Prints a LUT/FF/BRAM/DSP/IO/clock table. With --fpga, shows utilization %
        and exits non-zero if the design exceeds the device budget.
        """;

    /// <summary>Help for the <c>blocks</c> sub-command.</summary>
    public static string Blocks() => """
        kmila blocks — convert VHDL <-> block-editor workspace JSON

        USAGE
          kmila blocks --to-json <file.vhdl> [-o out.json]
          kmila blocks --to-vhdl <file.json> [-o out.vhdl]

        OPTIONS
          --to-json <path>   Parse VHDL into block-workspace JSON.
          --to-vhdl <path>   Emit VHDL from a block-workspace JSON.
          -o, --output-file  Write here (default: stdout).
          -h, --help         Show this help.
        """;

    /// <summary>Help for the <c>serve</c> sub-command.</summary>
    public static string Serve() => """
        kmila serve — host the local /api/v1 headless simulation API

        USAGE
          kmila serve [--port 8080] [--bind-lan] [--token <secret>]

        OPTIONS
          --port <n>     TCP port (default: 8080).
          --bind-lan     Listen on all interfaces (may need elevated privileges).
          --token <s>    Require this bearer token / X-Api-Key (default: open).
          -h, --help     Show this help.

        Endpoints: GET /api/v1/health, POST /projects, POST /projects/{id}/files,
        GET /projects/{id}/files/{name}, DELETE /projects/{id}, POST /simulate.
        Runs until Ctrl+C.
        """;
}
