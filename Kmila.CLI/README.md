# Kmila.CLI — terminal head

> 📖 End-user guide: see the **[Manual de Usuario — Cap. 16 (CLI)](../docs/manual/16-cli-y-terminal.md)** for a friendly, Spanish walkthrough of these commands.

A **fifth, parallel head** next to `Kmila` (MAUI), `Kmila.Web` (Blazor Server)
and `Kmila.Wasm` (Blazor WebAssembly). This one has **no GUI**: it runs the
*same* VHDL engine from the command line, for low-resource machines, headless
servers, CI pipelines, and SSH sessions. Like every other head it references
only `Kmila.Shared`; the engine libraries (`Debugger` / `Interpreter` /
`LibraryCompiler` → transitively `Parser` / `TimeMachine`) flow in with it. It
is **not** a replacement for any other head — all five coexist over the one
shared library, unchanged.

No third-party packages are added. The interactive UI is **hand-rolled ANSI**,
so the tool ships with the smallest footprint and the widest terminal reach
(Linux, macOS, Windows 10+). Cross-platform by virtue of the .NET 9 runtime.

## Contents

- [Commands at a glance](#commands-at-a-glance)
- [`kmila sim` — non-interactive](#1-kmila-sim--non-interactive)
  - [Options](#sim-options) · [Output formats](#output-formats) ·
    [Exit codes](#exit-codes) · [Examples](#sim-examples)
- [`kmila ui` — full-screen terminal UI](#2-kmila-ui--full-screen-terminal-ui)
  - [Options](#ui-options) · [Panels](#panels) · [Keys](#keys)
- [Teaching & grading commands](#teaching--grading-commands) — `check` · `diff` · `practice`
- [Analysis & export commands](#analysis--export-commands) — `synth` · `flow` · `fit` · `blocks`
- [Infrastructure commands](#infrastructure-commands) — `serve` · `completion` · config · watch
- [How it reuses everything](#how-it-reuses-everything)
- [Source layout](#source-layout)
- [Build / run / install](#build--run--install)
- [Resource resolution & environment variables](#resource-resolution--environment-variables)
- [Testing](#testing)
- [Notes for other modules](#notes-for-other-modules-recorded-not-acted-on)

---

## Commands at a glance

| Command | What it does | TTY? |
|---|---|---|
| `kmila sim` | Non-interactive run → VCD/TXT/CSV/JSON/SVG (or stdout). Pipe/CI friendly. Supports `--assert`, `--testbench`, `--watch`. | No |
| `kmila ui` | Full-screen ANSI UI: file browser + VHDL editor + waveform viewer. | Yes |
| `kmila check` | Parse + lint a design (no simulation); CI gate. | No |
| `kmila diff` | Compare two VCD dumps and report divergences. | No |
| `kmila practice` | List / show / auto-grade the bundled practice exercises. | No |
| `kmila synth` | Synthesize a design → SVG schematic. | No |
| `kmila flow` | Emit a PlantUML control-flow diagram. | No |
| `kmila fit` | Estimate resource usage, optionally vs an `--fpga` device. | No |
| `kmila blocks` | Convert VHDL ⇆ block-editor workspace JSON. | No |
| `kmila serve` | Host the local `/api/v1` headless simulation API. | No |
| `kmila completion` | Print a shell completion script (bash/zsh/fish). | No |
| `kmila help` / `version` | Top-level help / version. | — |

Every command takes `--help`. The default UI/message language is **Spanish**
(`es`); pass `--lang en` for English (same dictionaries the Blazor app ships).

---

## 1. `kmila sim` — non-interactive

Runs one simulation and writes the result in the format you choose. The engine
pipeline is the same one `Kmila.Shared/Services/Api/HeadlessSimulator.cs` uses
(scan → elaborate → parse → stimulus → run), mirrored inside the CLI so it can
keep the engine's live `SignalHistory` and emit **every** output format from it.

```
kmila sim --top-file <file.vhdl> [options]
```

Data goes to **stdout** (so it pipes cleanly) unless you pass `--output-file`;
engine chatter, the progress meter and the one-line run summary go to
**stderr**. That split is deliberate — `kmila sim -t top.vhdl > out.vcd` yields a
clean VCD with no diagnostics mixed in.

### <a id="sim-options"></a>Options

**Required**

| Flag | Meaning |
|---|---|
| `-t`, `--top-file`, `--main-file <path>` | Primary VHDL source to simulate. |

**Sources**

| Flag | Meaning |
|---|---|
| `--file <path>` | Additional source file; **repeatable**. Bare positional paths are treated the same way (multi-file designs). |
| `--entity`, `--top <name>` | Top-level entity to simulate. Default: the top file's name without extension. |
| `--testbench`, `--tb <path>` | Run a VHDL testbench against the design (TB drives its own clock/reset); `report` output is logged. |

**Timing**

| Flag | Meaning | Default |
|---|---|---|
| `--duration <N[unit]>` | Simulated time. Unit ∈ `fs ps ns us ms s m h` (a bare number = ns). E.g. `50ns`, `1us`, `2000`. | `1000ns` |
| `--clockMHZ <int>` | System clock in MHz. | from `--fpga`, else `50` |
| `--tick-res-ns <int>` | Tick resolution in ns. | `10` |

**Target**

| Flag | Meaning |
|---|---|
| `--fpga <model>` | FPGA device (e.g. `EP4CE6`, `iCE40HX8K`, `XC7A35T`, `LFE5U-25`). Validated against the device catalogue; sets the default clock when `--clockMHZ` is omitted. |
| `--fpga list` | Print the known device names and exit. |

> The catalogue is the 34 JSON specs in `Kmila.Shared/wwwroot/Devices/`
> (Intel/Altera Cyclone IV E `EP4CE*`, Lattice `iCE40*` / ECP5 `LFE5U-*`,
> AMD/Xilinx Spartan-6 `XC6SLX*` and Artix-7 `XC7A*`). There is no literal
> "EC5"; the ECP5 parts are the `LFE5U-*` entries.

**Stimulus**

| Flag | Meaning |
|---|---|
| `--stimulus <port@time=value>` | Drive an input port at a time; **repeatable**. The time may carry a unit. Events for one port are grouped and time-sorted automatically. E.g. `--stimulus rst@0=1 --stimulus rst@30ns=0`. |

**Assertions** (turn a run into a pass/fail gate — exit non-zero on any miss)

| Flag | Meaning |
|---|---|
| `--assert <sig@time=value>` | Check a signal's value at a time; **repeatable**. Value accepts `0/1/x/z`, hex `0x1f`, binary `0b101`, or decimal. E.g. `--assert cnt@100ns=0x5`. |
| `--assert-file <path>` | One `sig@time=value` per line (`#` comments allowed). |

**Output**

| Flag | Meaning | Default |
|---|---|---|
| `--output-format <fmt>` | `vcd \| txt \| csv \| json \| svg` | `vcd` |
| `-o`, `--output-file <path>` | Write here instead of stdout. | stdout (text formats); `waveform.svg` for `svg` |

**Misc**

| Flag | Meaning |
|---|---|
| `--config <path>` | Load defaults from a `kmila.json` (CLI flags override it). Default: `./kmila.json` if present — see [config](#infrastructure-commands). |
| `--watch` | Re-run automatically whenever a source file changes, until Ctrl+C. |
| `--no-breakpoints` | Accepted for parity with the example CLI; the headless run never breaks (breakpoints are an interactive-debugger concept, available in `kmila ui`). |
| `--lang <es\|en>` | Language for messages. Default `es`. |
| `--wall-timeout-ms <n>` | Abort a run that exceeds this wall-clock budget. |
| `--progress` | Print a `[sim] NN%` progress meter on stderr. |
| `-q`, `--quiet` | Suppress the run summary; only errors on stderr. |
| `-v`, `--verbose` | Echo the captured engine log/console on stderr after the run. |
| `-h`, `--help` | Show the `sim` help. |

### <a id="output-formats"></a>Output formats

| `--output-format` | Source | Contents |
|---|---|---|
| `vcd`  | `SignalHistory.ExportToVcd` | Value-Change Dump — open in **Surfer** / GTKWave / any VCD viewer. |
| `txt`  | `SignalHistory.ExportToText` | Human-readable signal dump. |
| `csv`  | `SignalHistory.ExportToCsv` | One column per signal, one row per time step. |
| `json` | wrapper | `{ entity, durationMs, totalTicks, tickResNs, signalCount, log, vcd }` — shape-compatible with the shared `SimulateResponse`, with the VCD embedded. |
| `svg`  | `WaveformExporter.BuildSvg` | Static waveform image (the app's own exporter). PNG is host-rasterized elsewhere; the CLI emits SVG only. |

### <a id="exit-codes"></a>Exit codes

| Code | Meaning |
|---|---|
| `0` | Success. |
| `1` | Run / parse / IO failure (bad VHDL, missing source, engine error). |
| `64` | Usage error (missing `--top-file`, bad `--duration`, unknown `--output-format`, …). |

### <a id="sim-examples"></a>Examples

```bash
# Minimal: simulate for 1000 ns, VCD to stdout
kmila sim --top-file counter.vhdl > counter.vcd

# Your canonical example (EC5 ≈ an ECP5 LFE5U part)
kmila sim --top-file top.vhdl --duration 50ns --fpga LFE5U-25 --clockMHZ 50 \
          --no-breakpoints --output-format vcd --output-file out.vcd

# Multi-file design, choose the entity explicitly, emit JSON
kmila sim -t alu.vhdl --file alu_pkg.vhdl --entity alu --output-format json -o alu.json

# Combinational gate driven by manual stimulus
kmila sim -t nor_gate.vhdl --duration 100ns \
          --stimulus a@0=0 --stimulus b@0=0 --stimulus a@40ns=1 --stimulus b@60ns=1

# Let the device pick the clock, show progress, write CSV
kmila sim -t divider.vhdl --fpga EP4CE6 --progress --output-format csv -o div.csv

# List the known FPGA devices
kmila sim --fpga list
```

On this Mac, open the resulting `.vcd` in **Surfer** (GTKWave crashes on
macOS 26).

---

## 2. `kmila ui` — full-screen terminal UI

A zero-dependency ANSI application: no external TUI toolkit, just raw VT escape
sequences over a double-rendered cell grid. It needs a **real interactive
terminal** — it refuses to start when stdin/stdout are redirected (so it never
corrupts a pipe), which is why the automated tests drive it through a pty.

```
kmila ui [options]
```

### <a id="ui-options"></a>Options

| Flag | Meaning |
|---|---|
| `-t`, `--top-file <path>` | Pre-select this VHDL file as the top and open it in the editor. |
| `--open <file.vcd>` | Open an existing VCD for viewing (no simulation needed). Reads Kmila / GHDL / Surfer dumps. |
| `--dir <path>` | Start the file browser here. Default: the top file's folder, else the current directory. |
| `--lang <es\|en>` | UI language. Default `es`. |
| `--ascii` | Force ASCII-only glyphs (no box-drawing / Unicode block characters). |
| `-h`, `--help` | Show the `ui` help. |

`NO_COLOR` and `TERM=dumb` are honored (colors drop, emphasis stays); a
non-UTF-8 terminal auto-falls-back to ASCII glyphs even without `--ascii`.

### <a id="panels"></a>Panels

- **Archivos** (Files) — browse the `*.vhd` / `*.vhdl` files in the directory;
  the current top file is marked (`●` / `*`). `Enter` sets the selected file as
  top and opens it.
- **Editor** — view/edit VHDL with line numbers, **VHDL syntax highlighting**,
  cursor movement, insert/delete, and `Ctrl+S` to save.
- **Ondas** (Waveforms) — ASCII waveform viewer: scalar wires as two-level
  square waves, buses as hex value segments, a time ruler, a movable cursor with
  a value-at-cursor readout, a second **measurement marker** (Δt + frequency),
  per-signal **hide** and name **filter**, and **export** to VCD/SVG.

A status bar shows the focused panel, the last action, and the cursor time; the
header bar shows the key hints.

### <a id="keys"></a>Keys

| Key | Action |
|---|---|
| `Tab` | Switch panel (Archivos → Editor → Ondas) |
| `↑` `↓` (or `k` `j`) | Navigate list / lines / signals |
| `Enter` | (Archivos) set as top & open |
| `F5` or `r` | Simulate the current file |
| `Ctrl+S` | Save the editor buffer |
| `p` | Edit sim parameters (duration / clock / FPGA) |
| `+` `-` | (Ondas) zoom in / out |
| `<` `>` | (Ondas) scroll time |
| `←` `→` | (Ondas) move the cursor |
| `0` | (Ondas) fit to the window |
| `m` | (Ondas) set/clear the measurement marker — shows Δt + frequency |
| `h` / `a` | (Ondas) hide the selected signal / show all (clear hide+filter) |
| `/` | (Ondas) filter signals by name |
| `S` | (Ondas) export the waveform (VCD, or SVG after a run) |
| `Home` `End` `PgUp` `PgDn` | Jump / page (per panel) |
| `e` / `o` / `w` | Jump to Editor / Archivos (open) / Ondas |
| `?` or `F1` | Help overlay |
| `q` / `Esc` | Quit (from a navigation panel) |
| `Ctrl+Q` | Quit from **anywhere**, including the editor |

Typical flow: `Tab`/`↑↓` pick a file → `Enter` → `F5` to simulate → `Tab` to
**Ondas** → `+`/`-` and `←`/`→` to explore. Resize the window any time; it
redraws.

---

## Teaching & grading commands

Built for classrooms and CI — each exits **non-zero on a logical failure** (bad
VHDL, wrong answer, assertion miss, diff found), so they slot into pipelines.

### `kmila check <file.vhdl>`

Parse + lint, **no simulation** — a fast "does it compile?" gate. Diagnostics
print on stderr as `file:line:col: severity [code]: message`. `--warnings-as-errors`
also fails on warnings. Exit `0` clean, `1` on any error.

### `kmila diff <a.vcd> <b.vcd>`

Compare two dumps signal-by-signal; prints divergences (`~ sig @ t: va != vb`,
and `+`/`-` for signals present in only one). `--signals a,b` limits the set;
`--all` shows every divergence per signal (default: first only). Exit `1` if they
differ. Great against a golden dump in a "fix-the-bug" exercise.

### `kmila practice …` — the auto-grader

Runs the **same in-process grader the GUI uses** over the bundled exercises.

```bash
kmila practice list                                   # catalogue
kmila practice show  <id>                              # statement + metadata
kmila practice grade <id> --solution mysol.vhdl       # verdict + exit code
kmila practice grade-all <dir> --exercise <id>        # grade a whole folder
```

Verdict is `BuildError > RuntimeError > WrongAnswer > Solved`; `grade` exits `0`
only when **Solved**, `grade-all` only when **every** file is solved (a class
roster → a pass/fail table). The exercise bundle is copied into the build output
(lean subset: manifest + solution/starter/tests + es/en statements) and is also
found by walking up to `Kmila.Shared/wwwroot/Practice` in a source checkout.

### `kmila sim --assert …` / `--testbench …`

Two more grading levers on `sim`: **assertions** check exact values at times
(above), and **`--testbench dut_tb.vhdl`** drives the design from a VHDL
testbench and logs its `report` output. See the [sim options](#sim-options).

---

## Analysis & export commands

### `kmila synth <file.vhdl> [-o schematic.svg]`

Synthesize the design to a gate-level netlist and export an **SVG schematic**
(the app's own `SchematicExporter`). `--mode raw|grouped|rtl|nodes`,
`--scope full|inputs|outputs`, `--mono`, `--no-title`. Prints a `cells/nets`
summary on stderr; SVG to the file (or stdout if no `-o`).

### `kmila flow <file.vhdl> [-o flow.puml]`

Emit a **PlantUML** control-flow diagram. Output is PlantUML text; if the output
name ends in `.svg`/`.png` **and** a `plantuml` binary is on `PATH`, it's rendered,
otherwise a `.puml` is written. Good for thesis figures.

### `kmila fit <file.vhdl> [--fpga <model>]`

Estimate **resource usage** (LUTs / FFs / BRAM / DSP / IO / clocks) via the
engine's estimator. With `--fpga` it compares against the device's capacity and
prints a utilization table with `ok`/`WARN`/`OVER` status, exiting non-zero if the
design exceeds the budget. `--fpga list` lists devices.

```
Resource estimate for 'divider_buggy' on EP4CE6:
  Resource       Used    Avail    Util  Status
  LUTs              0     6272      0%  ok
  FlipFlops         3     6272      0%  ok
  ...
```

### `kmila blocks --to-json <f.vhdl>` / `--to-vhdl <f.json>`

Convert between VHDL and the block-editor's workspace JSON, in either direction
(reuses `VhdlBlockBuilder` / `VhdlBlockEmitter`).

---

## Infrastructure commands

### `kmila serve [--port 8080] [--bind-lan] [--token <secret>]`

Host Kmila's local **`/api/v1` headless-simulation API** from the console (the
same transport-neutral handlers the MAUI head self-hosts), so other tools can
drive builds/sims over HTTP. Endpoints: `GET /health`, `POST /projects`,
`POST /projects/{id}/files`, `GET /projects/{id}/files/{name}`,
`DELETE /projects/{id}`, `POST /simulate`. Runs until Ctrl+C.

```bash
kmila serve --port 8080 &
curl localhost:8080/api/v1/health
PID=$(curl -s -XPOST localhost:8080/api/v1/projects -d '{"name":"d"}' | jq -r .projectId)
curl -s -XPOST localhost:8080/api/v1/projects/$PID/files -d '{"name":"a.vhdl","content":"..."}'
curl -s -XPOST localhost:8080/api/v1/simulate -d "{\"projectId\":\"$PID\",\"entryPoint\":\"a\",\"duration\":50,\"timeScale\":\"NS\"}"
```

`--token` requires a `Authorization: Bearer <secret>` or `X-Api-Key` header on
every route except `/health`.

### Config file (`kmila.json`)

`kmila sim` reads defaults from `./kmila.json` (or `--config <path>`); explicit
flags always win. Keys: `duration`, `clockMHZ`, `fpga`, `tickResNs`, `format`,
`lang`.

```json
{ "duration": "200ns", "clockMHZ": 50, "fpga": "EP4CE6", "format": "vcd" }
```

### `kmila sim --watch`

Re-runs the simulation whenever the top file or a dependency changes on disk —
an edit-and-see loop. Ctrl+C to stop.

### `kmila completion bash|zsh|fish`

Prints a shell completion script for the command set, e.g.
`kmila completion zsh > ~/.kmila-completion.zsh && source ~/.kmila-completion.zsh`.

---

## How it reuses everything

- References `Kmila.Shared` verbatim (the sanctioned variant pattern); the engine
  libs come along transitively.
- **Headless engine**: `KmilaSimulationService` mirrors the proven pipeline in
  `HeadlessSimulator` but surfaces the live `SignalHistory`, which is what makes
  the non-VCD formats and the live TUI viewer possible.
- **Simulation building blocks** reused directly: `SandboxProjectStore`,
  `SimulationRunner`, `SignalHistory`, `StimulusBuilder.BuildConfigNs`,
  `SourceElaborator`, `EntityRegistry` / `PackageSubtypeScanner`,
  `Debugger.Repositories.Parser`.
- **Waveforms**: `WaveformData.FromSignalHistory` + `WaveformExporter.BuildSvg`
  for the SVG export.
- **FPGA**: the `Devices/*.json` specs + `FPGAClocking.GetDefaultSimClockMhz`.
- **i18n**: the flat `Dictionaries/{es,en}.json` key→string tables, loaded
  directly (English as a base, the target language overlaid).
- **Analysis**: `Synthesizer` + `LayeredLayout` + `SelectionFilter` +
  `SchematicExporter` (schematic), `PlantUmlFlowEmitter` (flow), `Resources`
  estimator + `FPGAResources` (fit), `VhdlBlockBuilder` / `VhdlBlockEmitter`
  (blocks), and the parser's own `Diagnostics` stream (check).
- **Grading**: `PracticeCatalog` + `PracticeEvaluator` over the bundled
  `Practice/` content; `ProgramBuilder.RunTestbenchAsync` for `--testbench`.
- **API host**: the transport-neutral `SimulationApiHandlers` + `HeadlessSimulator`
  + `SandboxProjectStore`, served over `System.Net.HttpListener` (same pattern as
  the MAUI head's `EmbeddedApiHost`).

The only piece the shared library doesn't provide is a **VCD reader** (it has a
writer only), so the CLI ships its own compact `VcdReader` for `ui --open`,
`diff`, and `--assert`.

## Source layout

```
Kmila.CLI/
  Program.cs                      Entry point; dispatches every sub-command
  Cli/
    ArgParser.cs                  Hand-rolled flag parser
    HelpText.cs                   Localized usage/help for root + each command
    ResourcePaths.cs              Locate Devices/ Dictionaries/ Practice/ (bin copy → walk up)
    ConfigFile.cs                 kmila.json defaults for `sim`
  Commands/
    SimCommand.cs                 `sim` (+ --assert / --testbench / --watch / --config)
    UiCommand.cs                  `ui` launcher
    CheckCommand.cs               `check`  — parse + lint
    DiffCommand.cs                `diff`   — compare two VCDs
    PracticeCommand.cs            `practice` — list / show / grade / grade-all
    SynthCommand.cs               `synth`  — netlist → SVG (+ shared TopFile arg helper)
    FlowCommand.cs                `flow`   — PlantUML control-flow
    FitCommand.cs                 `fit`    — resource estimate vs device
    BlocksCommand.cs              `blocks` — VHDL ⇆ block JSON
    ServeCommand.cs               `serve`  — HTTP API host launcher
    CompletionCommand.cs          `completion` — shell scripts
  Simulation/
    KmilaSimulationService.cs     The headless pipeline; returns SignalHistory + metadata
    DesignLoader.cs               Parse/elaborate a design to its Entity (for synth/flow/fit)
    TestbenchRunner.cs            Run a VHDL testbench via ProgramBuilder
    AssertionChecker.cs           Evaluate sig@time=value assertions
    PracticeContent.cs            File-reader delegate over the bundled Practice/
    ResourceReport.cs             Used-vs-device utilization math + table
    DurationParser.cs             "50ns"/"1us"/2000 → ns; port@time=value
    FpgaResolver.cs               Validate --fpga, default clock from the device spec
    OutputWriter.cs               vcd | txt | csv | json | svg rendering
    VcdReader.cs                  Compact VCD → WaveModel
  Serve/
    ApiHost.cs                    HttpListener host for /api/v1
  Tui/
    AnsiTerminal.cs               Platform init (Windows VT, UTF-8, alt-screen, caps)
    ScreenBuffer.cs               In-memory cell grid → one ANSI string per frame
    WaveModel.cs                  Terminal waveform model (fs times, ValueAtFs lookup)
    WaveformRenderer.cs           WaveModel → ASCII/Unicode waves + ruler + cursor
    VhdlHighlighter.cs            Per-line VHDL syntax coloring for the editor
    TuiApp.cs                     Event loop, panels, editor, measurement/filter/export
  I18n/
    Strings.cs                    Loads the es/en dictionaries; T(key[, args])
  Kmila.CLI.csproj                Exe · net9.0 · AssemblyName=kmila · PackAsTool
  README.md                       This file
```

## Build / run / install

```bash
# from App/Kmila/
dotnet build Kmila.CLI/Kmila.CLI.csproj
dotnet run   --project Kmila.CLI -- sim --top-file <file.vhdl> --duration 200ns
dotnet run   --project Kmila.CLI -- ui  --top-file <file.vhdl>
```

Everything after `--` is passed to `kmila`; everything before it is for
`dotnet`.

```bash
# self-contained single-OS binary
dotnet publish Kmila.CLI -c Release -r osx-arm64 --self-contained -o out/
dotnet publish Kmila.CLI -c Release -r linux-x64 --self-contained -o out/
dotnet publish Kmila.CLI -c Release -r win-x64   --self-contained -o out/

# or as a global command named `kmila`
dotnet pack Kmila.CLI -c Release
dotnet tool install --global --add-source ./nupkg Kmila.CLI
# later: dotnet tool update --global --add-source ./nupkg Kmila.CLI
```

The project is registered in `Kmila.sln` (so it's part of the solution and
`dotnet sln list` shows it). Registering it there is the **only** cross-module
edit this head required.

## Resource resolution & environment variables

The device specs (`Devices/`) and UI dictionaries (`Dictionaries/`) are linked
into `bin/` at build time (`CopyToOutputDirectory=PreserveNewest`) so a published
binary is self-contained. At runtime `ResourcePaths` resolves each folder in
order:

1. an environment-variable override (if set and existing);
2. a copy next to the executable (the published layout);
3. a walk up the directory tree to `…/Kmila.Shared/wwwroot/<folder>` (covers a
   bare `dotnet run` from inside a source checkout).

| Variable | Overrides |
|---|---|
| `KMILA_DEVICES_DIR` | the `Devices/` folder (FPGA specs) |
| `KMILA_DICT_DIR` | the `Dictionaries/` folder (i18n) |
| `KMILA_PRACTICE_DIR` | the `Practice/` exercise bundle |
| `NO_COLOR` | disables color in `kmila ui` |
| `TERM=dumb` | disables color in `kmila ui` |

## Testing

The suites live in the session scratchpad (they write only to temp dirs, never
into the repo):

- `test_sim.sh` — 18 checks on `kmila sim`: clean stdout, all five output
  formats, FPGA default clock, stimulus, multi-file elaboration, error/usage codes.
- `test_features.sh` — 35 checks across the new commands: `check` (good/bad),
  `--assert` (hit/miss), `diff` (equal/divergent), `practice list/show/grade/
  grade-all`, `--testbench`, `synth`→SVG, `flow`→`@startuml`, `fit` utilization,
  `blocks` round-trip, `completion`, config file, and `serve` (health + a full
  create→write→simulate flow).
- `test_ui.py` / `test_ui_extra.py` / `test_ui_phase4.py` — pty-driven: panels,
  a run, help overlay, `--open`, `--ascii`, editor save, plus the measurement
  marker (Δt), hide/show, and VCD export.

Because `kmila ui` requires a TTY, the UI tests allocate a pseudo-terminal.

## Notes for other modules (recorded, not acted on)

This head was built without modifying any other module; the only cross-module
edit was registering the project in `Kmila.sln`. Items that would want a change
elsewhere, for a maintainer to decide later:

- **`SimulateRequest.Fpga` is ignored by the shared engine.** The CLI applies
  the device's default clock itself (`FPGAClocking.GetDefaultSimClockMhz`). If
  FPGA should drive the clock for every head, that belongs in `Kmila.Shared`.
- **No shareable VCD *reader* exists in `Kmila.Shared`** (only a writer; the one
  reader lives in `SimulatorBench`). The CLI ships its own `VcdReader`; a future
  refactor could promote one into `Kmila.Shared`.
- **PNG waveform export is host-rasterized** (browser/MAUI). The CLI emits
  **SVG** only; native PNG would need a rasterizer dependency. Same for `flow`:
  `PlantUmlFlowEmitter` emits PlantUML **text** — the CLI shells out to a local
  `plantuml` binary for `.svg`/`.png`, else writes the `.puml`.
- **`Interpreter/Services/Resources.cs` writes its estimate diagnostics to
  `Console`**; the CLI captures `Console.Out` around the call. A pure return value
  would be cleaner. There is also **no "used-vs-device %" service** — the CLI
  computes that itself (`Simulation/ResourceReport.cs`).
- **Run history is DB-bound.** `SimulationSnapshotStore` requires the EF Core
  `ApplicationDbContext` (SQLite), so a run-history command is out of scope here;
  a CLI-local file store could be added later without touching `Kmila.Shared`.
- **Step-replay and breakpoints in the TUI** (`ReplayBuilder`/engine hooks) were
  scoped as stretch and are not implemented yet; `--no-breakpoints` on `sim`
  stays a documented no-op.
- Referencing `Kmila.Shared` pulls the ASP.NET Core framework ref + Blazor/sqlite
  packages transitively on `net9.0`. It builds and runs fine (as `SimulatorBench`
  / `PracticeValidator` prove) but the binary is larger than a minimal console
  app. A leaner future option is to reference the engine libs directly.
