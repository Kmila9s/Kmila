#!/usr/bin/env bash
#
# build.sh — one-shot builder for the Kmila web app.
#
# Kmila's code is split across 6 git repos; this script clones them into the
# correct layout, applies the net10.0-only "clean build" fix (no MAUI workload
# needed), and builds — either a native `dotnet publish` or a Docker image.
#
# Works on macOS / Linux (and Windows via Git Bash or WSL; native Windows users
# have build.ps1). Everything happens in a self-contained workspace so nothing
# on your machine is touched outside it.
#
#   ./build.sh                     # native publish into ~/Downloads/kmila-build
#   ./build.sh --docker            # build the Docker image instead
#   ./build.sh --native --run      # build, then start it on http://localhost:8080
#   ./build.sh --workdir /tmp/kb   # use a different workspace
#
# Repo source: https://github.com/Kmila9s by default. Override with
#   KMILA_REPO_BASE=ssh://git@host:2243/kmila-9s ./build.sh
# (the base is joined with "/<Repo>.git").
set -euo pipefail

# ── config ──────────────────────────────────────────────────────────────────
REPO_BASE="${KMILA_REPO_BASE:-https://github.com/Kmila9s}"
WORKDIR="${KMILA_WORKDIR:-$HOME/Downloads/kmila-build}"
MODE="native"
RUN=0
BRANCH=""
PORT="${KMILA_PORT:-8080}"

# git repo name  ->  destination path inside the workspace root
REPOS=(
  "Kmila:App/Kmila"
  "Interpreter:Interpreter"
  "Parser:Parser"
  "Debugger:Debugger"
  "LibraryCompiler:LibraryCompiler"
  "TimeMachine:TimeMachine"
)

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]:-$0}")" && pwd)"

log()  { printf '\033[1;36m[build]\033[0m %s\n' "$*"; }
warn() { printf '\033[1;33m[build] WARN:\033[0m %s\n' "$*" >&2; }
die()  { printf '\033[1;31m[build] ERROR:\033[0m %s\n' "$*" >&2; exit 1; }

usage() {
  sed -n '2,25p' "$0" | sed 's/^# \{0,1\}//'
  exit "${1:-0}"
}

# ── args ────────────────────────────────────────────────────────────────────
while [ $# -gt 0 ]; do
  case "$1" in
    --native)  MODE="native" ;;
    --docker)  MODE="docker" ;;
    --run)     RUN=1 ;;
    --workdir) WORKDIR="${2:?--workdir needs a path}"; shift ;;
    --branch)  BRANCH="${2:?--branch needs a name}"; shift ;;
    --port)    PORT="${2:?--port needs a number}"; shift ;;
    -h|--help) usage 0 ;;
    *) die "unknown argument: $1 (try --help)" ;;
  esac
  shift
done

# ── prerequisites ───────────────────────────────────────────────────────────
command -v git >/dev/null 2>&1 || die "git is required but not installed."

# ── clone / update the 6 repos into the workspace layout ────────────────────
log "workspace: $WORKDIR"
log "repo base: $REPO_BASE"
mkdir -p "$WORKDIR"
for entry in "${REPOS[@]}"; do
  name="${entry%%:*}"; dest="${entry##*:}"
  target="$WORKDIR/$dest"
  url="$REPO_BASE/$name.git"
  if [ -d "$target/.git" ]; then
    log "update $name -> $dest"
    git -C "$target" pull --ff-only ${BRANCH:+origin "$BRANCH"} >/dev/null 2>&1 \
      || warn "could not fast-forward $dest (keeping current checkout)"
  else
    log "clone  $name -> $dest"
    mkdir -p "$(dirname "$target")"
    git clone --depth 1 ${BRANCH:+--branch "$BRANCH"} "$url" "$target" \
      || die "clone failed for $url — check access (repos are private until public) or KMILA_REPO_BASE."
  fi
done

APP="$WORKDIR/App/Kmila"
# Container recipe travels with this script (App/Kmila/deploy), so it works
# even before the freshly-cloned repo carries deploy/. Fall back to the cloned
# copy if the script was invoked detached from its deploy/ folder.
DEPLOY="$SCRIPT_DIR/deploy"
[ -f "$DEPLOY/Dockerfile" ] || DEPLOY="$APP/deploy"
WEB_CSPROJ="$APP/Kmila.Web/Kmila.Web.csproj"
[ -f "$WEB_CSPROJ" ] || die "missing $WEB_CSPROJ — did the Kmila repo clone correctly?"

# ── build ───────────────────────────────────────────────────────────────────
if [ "$MODE" = "native" ]; then
  command -v dotnet >/dev/null 2>&1 \
    || die "dotnet not found. Install the .NET 10 SDK (Linux: run ./install.sh), or use --docker."
  if ! dotnet --list-sdks 2>/dev/null | grep -q '^10\.'; then
    warn "no .NET 10 SDK detected in 'dotnet --list-sdks'; attempting the build anyway."
  fi
  OUT="$WORKDIR/publish"
  log "publishing Kmila.Web (net10.0, Release) -> $OUT"
  # Force net10.0 across the multi-targeted graph so no MAUI workload is needed
  # (ios/maccatalyst can't even restore off-Apple). Publish the .csproj, not a .sln.
  dotnet publish "$WEB_CSPROJ" -c Release -f net10.0 \
    -p:TargetFrameworks=net10.0 -p:UseAppHost=false \
    -o "$OUT"
  log "done. Published to: $OUT"
  RUN_CMD="ASPNETCORE_URLS=http://+:$PORT dotnet \"$OUT/Kmila.Web.dll\""
  if [ "$RUN" -eq 1 ]; then
    log "starting on http://localhost:$PORT  (Ctrl-C to stop)"
    ( cd "$OUT" && ASPNETCORE_URLS="http://+:$PORT" exec dotnet Kmila.Web.dll )
  else
    log "run it with:"
    printf '    (cd "%s" && ASPNETCORE_URLS=http://+:%s dotnet Kmila.Web.dll)\n' "$OUT" "$PORT"
  fi

elif [ "$MODE" = "docker" ]; then
  command -v docker >/dev/null 2>&1 \
    || die "docker not found. Install Docker (Linux: run ./install.sh --docker), or use --native."
  [ -f "$DEPLOY/Dockerfile" ] || die "missing $DEPLOY/Dockerfile"
  log "staging deploy assets into the workspace root"
  cp "$DEPLOY/Dockerfile" "$DEPLOY/.dockerignore" "$WORKDIR/"
  cp "$DEPLOY/docker-compose.yml" "$WORKDIR/docker-compose.yml"
  log "building image kmila-web:latest (context: $WORKDIR)"
  docker build -t kmila-web:latest "$WORKDIR"
  log "done. Image: kmila-web:latest"
  if [ "$RUN" -eq 1 ]; then
    log "starting via docker compose on http://localhost:$PORT"
    ( cd "$WORKDIR" && docker compose up -d )
    log "up. Check: curl http://localhost:$PORT/  |  stop: (cd $WORKDIR && docker compose down)"
  else
    log "run it with:"
    printf '    (cd "%s" && docker compose up -d)   # http://localhost:8080\n' "$WORKDIR"
  fi
fi
