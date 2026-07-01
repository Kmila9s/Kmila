# Kmila Android Full-App Exploration — 2026-06-14

End-to-end emulator walkthrough (emulator-5554, 1080x2400, Pixel-class device).
Goal: feed a future task with comprehensive UI ground-truth. Captured by driving
the live MAUI WebView via `adb input tap`, scrolling each surface, and screenshotting
at each meaningful state. 118 PNGs total, all 1080×2400 native.

## Capture environment
- App: `kmila.kmila` (MAUI)
- Sample project used: **Ejemplo · Contador de 4 bits** (sim_lab entity, 3 IN / 8 OUT, 19 signals)
- Language: Spanish (default)
- Theme: Oscuro (dark) per profile menu (note: most pages still rendered light — theme toggling not exercised)
- FPGA target chip: EP4CE10
- Pre-existing projects: `Ejemplo · Contador de 4 bits` (sample), `counter`

## Folder map

| Folder | Count | What it covers |
|---|---|---|
| `00-launch/` | 1 | Cold-boot home screen |
| `01-home/` | 7 | Hero, features bento (scrolled), closing CTA, nav drawer first-look |
| `02-projects/` | 6 | Projects list w/ Probar un ejemplo + Crear nuevo proyecto, ProjectDetailsModal (Nuevo proyecto) |
| `03-editor/` | 26 | Monaco w/ main.vhd, tab strip (Código/Bloques/Reproducción), RunBar, dock tabs Entidades · Esquemático · Flujo · Puertos · Formas-de-Onda |
| `04-learn/` | 19 | Aprender catalog (PLAN DE ESTUDIOS 0/31), category filters, lesson scroll, ROM síncrona inferida lesson w/ Pruébalo en el editor CTA |
| `04-simulation/` | 4 | Build → Run → Pausar → Ejecutar otra vez state machine |
| `05-concepts/` | 14 | Conceptos catalog (6 conceptos + 4 Blockly), lesson "¿Qué significa 'ejecutar'?" with inline ExecutionPlayer (7-step, t=0, signals panel) |
| `06-practice/` | 7 | Práctica catalog (135 ejercicios: Compuerta NOR/NOT/OR, Comparador, Decodificador, Medio sumador, Medio restador, Votador mayoritario, Generador de paridad par…), Medio-sumador detail w/ truth table + locked entity + Pista + Caso de prueba visible |
| `07-logs/` | 2 | Diagnóstico Registros 5/5 with hydrate events (LessonCatalog, LessonProgressStore, ConceptCatalog, ConceptProgressStore, PracticeCatalog) |
| `08-settings/` | 4 | Configuración: Apariencia (Tema/Idioma/Tamaño-de-letra 5-step/Tour-de-bienvenida), Editor (placeholder), Simulación, Compilación-de-bibliotecas, FPGA-personalizados, Idiomas-personalizados |
| `09-modals/` | 18 | ProjectDetailsModal, profile menu detailed, GitHubLoginModal w/ "OAuth no configurado" Tier-3 warning, drawer states, editor re-entry |
| `10-topbar/` | 2 | Profile menu (Invitado · FPGA EP4CE10 · 7 idiomas · 3 temas · Ajustes-de-cuenta · GitHub-login) |
| `12-flow-schematic/` | 3 | Esquemático tab w/ "Esquema lógico — 102 celdas" + Exportar (SVG/PNG/Reset/Zoom), Flujo tab w/ "Flujo de control" + SVG/PNG/UML/Fullscreen exporters |
| `99-misc/` | 8 | Relaunch states, drawer recheck, Privacidad page (PRIVACIDAD Y DATOS hero + 4 categorías + Comentarios a GitHub + 3 acciones) |

## Key flows verified live

### Sample-project run-through
1. Open Projects → tap "Ejemplo · Contador de 4 bits" → lands on Editor with `main.vhd`.
2. Monaco shows 20+ lines (Kmila sample comment header + library IEEE + entity sim_lab declaration).
3. Tap **Ejecutar** (Run) → app builds and runs end-to-end without user clock spec.
4. Dock auto-switches to **Formas de Onda** showing 19 signals (clk IN green-toggling, enable/reset IN, bit0–bit2 OUT orange-pulsing, plus internal q1–q6 visible after scroll).
5. Time axis advances 0 ns → ~600 µs; "Pausar" / Stop / Ejecutar-otra-vez state machine all visible.
6. Sim runs to its natural endpoint; auto-flips to "Ejecutar otra vez" idle state.

### Entity / Schematic / Flow trio
- **Entidades** shows `entity sim_lab is` with 3 IN / 8 OUT pill counts and inline port list w/ current STD_LOGIC value (clk=1, reset=0, enable=1, …).
- **Esquemático** renders the synthesized netlist as inline SVG, 102 celdas reported, Exportar + zoom controls.
- **Flujo** renders the control-flow tree (`Flujo de control`) with SVG / PNG / UML / Fullscreen export quartet.

### Inline pedagogy players
- Concept lesson "¿Qué significa 'ejecutar'?" embeds an **ExecutionPlayer**: 9-line process snippet, "1 / 7 — t = 0", playback chrome (skip-back/prev/play/next/skip-forward + Velocidad 1×), and a SEÑALES panel showing signal "a = U" (uninitialised) at step 0.

### Modals successfully prompted
- **ProjectDetailsModal** (Nuevo proyecto) — full form: Nombre del Proyecto (required) + Descripción del Proyecto (opcional), folder-path live preview ("Los archivos se guardarán en ./Projects/—"), Crear Proyecto submit.
- **GitHubLoginModal** — surfaces the "OAuth de GitHub no configurado" guard with Nivel-1/Nivel-2 fallback note (matches memory `project_github_feedback.md` Tier-3 status).
- **Profile menu (popover)** — full payload visible: guest banner, FPGA chip, 7-language grid, 3-theme grid, Ajustes-de-cuenta link, primary GitHub-login button.

### Pages walked
| Route | Captured | Notable details |
|---|---|---|
| `/` Home | ✅ | Hero, features bento, closing CTA |
| `/projects` | ✅ | 2 projects, search, Probar/Crear actions, kebab delete + open per card |
| `/editor/{sample-id}` | ✅ | Monaco + RunBar + 5 visible dock tabs + dock grabber + collapse chevron |
| `/learn` | ✅ | Hero, 31 lessons across 8 capítulos, filter chips, search |
| `/learn/{lesson}` | ✅ | "ROM síncrona inferida" w/ embedded `process(clk) begin … end process;` block + "Pruébalo en el editor" CTA |
| `/concepts` | ✅ | "MINI-CURSO Modelos de ejecución", 6 lessons (PRINCIPIANTE), 4 Blockly exercises (PIZZA receta, Cocina concurrente, Medio sumador, Contador con bloques) |
| `/concepts/{lesson}` | ✅ | Inline ExecutionPlayer + sequential/concurrent/parallel table + "Prueba esto" callout |
| `/practice` | ✅ | 135 ejercicios catalog (Compuerta NOR / NOT / OR, Comparador 4 bits, Decodificador 2→4, Medio sumador, Medio restador, Votador mayoritario, Generador paridad par…) |
| `/practice/{exercise}` | ✅ | Medio sumador w/ truth-table + locked entity + hint + visible test case + 4 hidden cases + Monaco editor |
| `/settings` | ✅ | 6 cards: Apariencia / Editor / Simulación / Compilación-de-bibliotecas / FPGA-personalizados / Idiomas-personalizados |
| `/logs` | ✅ | Diagnóstico 5/5 entries, level filter "Todos", search box, download/pause/clear actions |
| `/privacy` | ✅ | "PRIVACIDAD Y DATOS" hero + 4-item storage breakdown + GitHub feedback section + 3 action buttons |

## Surfaces NOT reached (known gaps)
Documented for the follow-up task — the live WebView became unresponsive to taps
after rapid sequential interactions (twice during the session), so these stayed
unreached after force-stop/restart cycles:

| Modal / view | Why missed |
|---|---|
| `ConfigurationModal` (Manage configs) | Reached via "Manage…" entry on Configurations pill — pill stayed "Sin config" because no config saved, and the pill popover didn't open on tap before the freeze |
| `CreateFileModal` | Reached via RunBar overflow menu → New file; RunBar overflow ☰ didn't open in this session |
| `FileDetailsModal` | Reached via Files dock tab → row kebab → details; Files tab is past the 5-tab visible slot — overflow tab strip never scrolled horizontally on this device |
| `ExportWaveform / ExportSchematic / ExportPracticeSession` | Reached via Exportar buttons on each dock tab — schematic Exportar button was visible but tap got swallowed during the freeze |
| `MarkdownModal` | Triggered by Build on a `.md` file — sample has no `.md` |
| Editor's Bloques tab (Scratch-VHDL) | Visible in the Editor tab strip (Código / Bloques / Reproducción pa…) but never selected |
| Editor's Reproducción tab (Replay) | Visible but never selected; would have shown the StepReplayPanel + breakpoint list |
| Dock tabs Runs / Files / Output | Past the 5-tab strip overflow — horizontal scroll on `.kmila-dock__tabs` didn't engage via swipe |
| Schematic debug step-through (`is-active-step` pulse) | Would require enabling Debug mode mid-live-run; the Debug toggle is inside Reproducción tab which wasn't reached |
| Breakpoint gutter glyphs | Would require tapping Monaco's gutter at a specific line; touch on Monaco kept opening Android IME instead |
| Search across FPGA picker (34 devices) | FPGA chip in profile menu shown but the picker popover didn't open on tap during the freeze |
| Language / theme picker popovers in TopBar | Three pickers visible inside the profile menu — picker controls themselves were not individually exercised |
| Configurations pill popover w/ Save/Load/Delete | Pill shown reading "Sin config" — never opened |

## Repro script

All captures driven by `./snap.sh <subdir> <name>` which wraps
`adb exec-out screencap -p > <path>` with a 0.4s settle. Big PNGs were
post-processed with `sips -Z 1500 <file> --out /tmp/x.png` to fit the
read-tool's 5 MB image limit during inspection.

```bash
# helper
./snap.sh 03-editor 12-entidades-after-sim
# tap by physical pixel
adb shell input tap 540 1100      # RunBar Ejecutar
# scroll a page body
adb shell input swipe 540 1800 540 600 400
# emulator picked up at 1080×2400 / 420 dpi
```

## Suggested next session
1. Land in Editor first (avoid drawer-trap freezes).
2. Open Reproducción tab → toggle Debug → trigger Run to capture live step + active-line pulse.
3. Long-press / horizontal swipe within `.kmila-dock__tabs` to reveal Runs / Files / Output dock tabs.
4. Open RunBar overflow ☰ for Save / New-file actions → CreateFileModal.
5. Open Schematic Exportar → ExportSchematicModal; same for Waveform.
6. Reach Configurations pill on TopBar → open popover → Save as → ConfigurationModal.
7. Toggle theme in profile menu → re-capture every page in dark theme (currently only home rendered dark, rest came through as light).
