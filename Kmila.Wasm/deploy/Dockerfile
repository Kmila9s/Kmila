# Kmila.Wasm — static Blazor WebAssembly demo image.
#
# The build context MUST be the repo root: Kmila.Wasm -> Kmila.Shared ->
# ../../../{Debugger,Interpreter,LibraryCompiler,Parser,TimeMachine}. Build with:
#
#   docker build -f App/Kmila/Kmila.Wasm/deploy/Dockerfile -t kmila-wasm:latest .
#
# The result is a ~tiny nginx image serving pure static files — no runtime,
# no volume, no SignalR, no database on the server. Everything runs in the
# visitor's browser and persists in their IndexedDB.

# ── build stage ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# Blazor WASM relinks a native e_sqlite3 into the wasm (so SQLite runs in the
# browser); that native step needs the wasm-tools workload. It also produces
# the AOT/trimmed, size-optimized assets a public demo wants.
RUN dotnet workload install wasm-tools

# The native wasm relink (e_sqlite3 -> wasm via Emscripten) invokes `python`,
# which the dotnet SDK image doesn't ship. Provide it (as `python`, not just python3).
RUN apt-get update && apt-get install -y --no-install-recommends python3 \
    && ln -sf /usr/bin/python3 /usr/local/bin/python \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /src
COPY . .

# -p:KmilaWasm=true strips the ASP.NET Core server flavor from Kmila.Shared and
# the engine graph (it also auto-trips on the browser-wasm RID, but we pass it
# explicitly). Publish the .csproj, never a .sln, so no MAUI workload is pulled.
RUN dotnet publish "App/Kmila/Kmila.Wasm/Kmila.Wasm.csproj" \
        -c Release -o /app/publish \
        -p:KmilaWasm=true

# ── runtime stage — static file server only ─────────────────────────────────
FROM nginx:alpine AS runtime
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
COPY App/Kmila/Kmila.Wasm/deploy/nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
HEALTHCHECK --interval=30s --timeout=3s --retries=3 \
    CMD wget -qO- http://localhost/ >/dev/null 2>&1 || exit 1
