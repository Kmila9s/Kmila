# Kmila.Wasm — Blazor WebAssembly demo head

> 📖 End-user guide: see the **[Manual de Usuario](../docs/manual/README.md)** (Spanish); the WASM demo's caveats are noted in [Cap. 1](../docs/manual/01-primeros-pasos.md) and [Cap. 18](../docs/manual/18-solucion-de-problemas.md).

A **third, parallel head** next to `Kmila` (MAUI) and `Kmila.Web` (Blazor
Server). This one is a Blazor **WebAssembly** client: the entire VHDL engine
(parse → simulate → synthesize → SVG) runs **in the browser**, so the app can be
published as static files for an anonymous public demo — "hosted remotely,
running locally". It is **not** a replacement for `Kmila.Web`; both coexist and
reuse the same `Kmila.Shared` RCL and engine, unchanged.

## How it reuses everything

- References `Kmila.Shared` verbatim — every page, component, style, and the
  engine libs come along.
- **SQLite runs in the browser**: `WasmBuildNative=true` relinks the
  `browser-wasm` `e_sqlite3` native asset into the wasm, so `ApplicationDbContext`
  and every repository work unmodified. Requires the **`wasm-tools`** workload.
- **Persistence**: the writable tree lives under `/kmila` in the in-memory
  (MEMFS) filesystem; `BrowserFsSync` + `wwwroot/js/kmila-wasm-fs.js` mirror it to
  IndexedDB so projects survive reloads.
- **Assets** (Dictionaries, Learn/Practice/Concepts, the Data/ bibliography
  `sources.json`, FPGA devices) load over HTTP from `_content/Kmila.Shared/...`
  instead of the server filesystem.

## Shared-code changes (all additive & gated — other heads unaffected)

- `Kmila.Shared.csproj`: a `KmilaWasm` flavor that drops the ASP.NET Core server
  framework + `/api/v1` (auto-trips on the `browser-wasm` RID).
- 7 base-path seams resolve to `/kmila` under `OperatingSystem.IsBrowser()`.
- `UserAssetStore` skips `FileSystemWatcher` in the browser.
- `TimeMachine/Repositories/Timer.cs` skips a 500 ms blocking wait in the browser
  (would deadlock the single wasm thread) — simulation output is identical.
- `IDemoCapabilities` (nullable) lets the shared UI hide GitHub OAuth login
  (CORS-blocked in the sandbox). Library import is kept — it works client-side.

## Build & run (dev)

Every browser build needs **`-p:KmilaWasm=true`** as a GLOBAL property: it has to
affect *restore* (that's where `Kmila.Shared`'s transitive
`Microsoft.AspNetCore.App` FrameworkReference is resolved — it has no
browser-wasm runtime pack), so it can't be baked into the csproj. Forget it and
the build fails with `NETSDK1082`. Two ways that supply it for you:

**A) Run from THIS directory** — `Directory.Build.rsp` here auto-applies the flag
to any MSBuild/`dotnet` command run from this folder, so a bare command works:

```bash
cd App/Kmila/Kmila.Wasm
dotnet build -f net9.0     # or: dotnet run --urls http://localhost:5299
```
(The .rsp is picked up from the current working directory only, so it never
affects the solution build or the other heads. Corollary: don't build the whole
`.sln` while cd'd into this folder, or the flag would apply solution-wide.)

**B) From anywhere** — the wrapper script adds the flag explicitly:

```bash
App/Kmila/Kmila.Wasm/build-wasm.sh run            # dev server on :5299
App/Kmila/Kmila.Wasm/build-wasm.sh publish        # static site -> bin/publish/wwwroot
```

Or explicitly:

```bash
dotnet run --project App/Kmila/Kmila.Wasm/Kmila.Wasm.csproj -p:KmilaWasm=true --urls http://localhost:5299
```

`WasmBuildNative=true` (default in the csproj) links native SQLite, so the first
build runs an ~30–60 s emcc relink.

### Solution (.sln) note

`Kmila.Wasm` IS listed in `Kmila.sln` (so it's visible in the IDE) but is
**excluded from "Build Solution"** — its `.Build.0` entries were removed from the
solution's `ProjectConfigurationPlatforms`, leaving only `.ActiveCfg`.

Why: the `KmilaWasm` flag can't travel through a solution/IDE build, and
`Kmila.Shared`'s single `net9.0` output can't be both the server flavor (needs
`Microsoft.AspNetCore.App`, for Kmila.Web) and the browser flavor (must not have
it) in one build — so building Kmila.Wasm as part of the solution fails with
`NETSDK1082`. Excluding it keeps the whole-solution build green. Build/run the
demo explicitly via `build-wasm.sh`, the `dotnet … -p:KmilaWasm=true` command, or
the Docker image.

(A future option to make it build cleanly *inside* the solution is to give
`Kmila.Shared` a dedicated `net9.0-browser` target framework — deferred, as it
touches the multi-targeting the other heads share.)

## Deploy (static)

```bash
# from the repo root — builds an nginx image serving pure static files
docker compose -f App/Kmila/Kmila.Wasm/deploy/docker-compose.yml up -d --build
# → http://localhost:8081
```

No volume, no backend, no SignalR/WebSocket proxy. Any static host or CDN works.

## Known notes

- `NU1903` advisory on `SQLitePCLRaw.lib.e_sqlite3` (all current 2.x) — no patched
  release yet; low risk for a local, single-user, client-side DB.
- First load downloads the .NET wasm runtime + assemblies (tens of MB in Debug;
  much smaller published/trimmed).
- **Simulation freezes the page until it finishes.** WASM runs on the browser's
  single thread, so the CPU-bound delta-cycle loop monopolizes it — `Task.Run`
  does not offload to a real thread here — and the page can't repaint until the
  run ends. This is inherent to the browser demo; the MAUI and Blazor-Server
  heads run the sim off the UI thread and stay responsive. As of v1.27.0 the
  Editor shows a **demo overlay** while a run is in flight (gated on
  `OperatingSystem.IsBrowser()`), explaining the pause and linking to the desktop
  app (`kmila.com.mx/download`); it's painted before the loop starts and its
  spinner is a CSS animation so it keeps moving on the compositor thread during
  the freeze. Softening options (cooperative `Task.Yield` in the loop, AOT, real
  wasm threads) are noted but deferred.
- **GitHub features are unavailable in the demo.** OAuth login and the per-project
  version control both talk to GitHub, which is CORS-blocked in the wasm sandbox,
  so `IDemoCapabilities.AllowGitHubLogin` hides the Source Control tab + sign-in
  here. They work on the MAUI (native) and Blazor-Server heads.
