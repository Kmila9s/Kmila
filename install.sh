#!/usr/bin/env bash
#
# install.sh — install + run the Kmila web app on Linux, from scratch.
#
# Installs all prerequisites (the .NET 9 SDK for --native, or Docker for
# --docker), builds Kmila via build.sh, and sets it up to run as a service
# reachable on http://<host>:8080.
#
# Run with root privileges:
#     sudo ./install.sh                 # native + systemd service (default)
#     sudo ./install.sh --docker        # Docker image + container
#     sudo ./install.sh --no-service    # install deps + build only, don't deploy
#
# Repo source override (until the GitHub repos are public):
#     sudo KMILA_REPO_BASE=ssh://git@host:2243/kmila-9s ./install.sh
set -euo pipefail

MODE="native"
SERVICE=1
PORT="${KMILA_PORT:-8080}"
BUILD_DIR="/opt/kmila-build"
APP_DIR="/opt/kmila"
DATA_DIR="/var/lib/kmila"
SVC_USER="kmila"

log()  { printf '\033[1;36m[install]\033[0m %s\n' "$*"; }
warn() { printf '\033[1;33m[install] WARN:\033[0m %s\n' "$*" >&2; }
die()  { printf '\033[1;31m[install] ERROR:\033[0m %s\n' "$*" >&2; exit 1; }

while [ $# -gt 0 ]; do
  case "$1" in
    --native)     MODE="native" ;;
    --docker)     MODE="docker" ;;
    --no-service) SERVICE=0 ;;
    --port)       PORT="${2:?}"; shift ;;
    -h|--help)    sed -n '2,20p' "$0" | sed 's/^# \{0,1\}//'; exit 0 ;;
    *) die "unknown argument: $1" ;;
  esac
  shift
done

[ "$(id -u)" -eq 0 ] || exec sudo -E "$0" "$@"   # re-exec as root, preserving env

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

# ── detect the package manager ───────────────────────────────────────────────
if   command -v apt-get >/dev/null 2>&1; then PM=apt
elif command -v dnf     >/dev/null 2>&1; then PM=dnf
elif command -v pacman  >/dev/null 2>&1; then PM=pacman
elif command -v zypper  >/dev/null 2>&1; then PM=zypper
else die "unsupported distro (no apt/dnf/pacman/zypper). Install deps manually and use build.sh."; fi
log "package manager: $PM"

pm_install() {
  case "$PM" in
    apt)    apt-get update -y && apt-get install -y --no-install-recommends "$@" ;;
    dnf)    dnf install -y "$@" ;;
    pacman) pacman -Sy --noconfirm "$@" ;;
    zypper) zypper --non-interactive install "$@" ;;
  esac
}

log "installing base tools (git, curl, ca-certificates)"
pm_install git curl ca-certificates || warn "some base tools may already be present"

if [ "$MODE" = "native" ]; then
  # .NET runtime needs ICU for globalization; names differ per distro (best-effort).
  case "$PM" in
    apt)    pm_install libicu-dev || pm_install "libicu[0-9]*" || true ;;
    dnf)    pm_install libicu || true ;;
    pacman) pm_install icu || true ;;
    zypper) pm_install libicu || true ;;
  esac
  if dotnet --list-sdks 2>/dev/null | grep -q '^9\.'; then
    log ".NET 9 SDK already installed: $(command -v dotnet)"
  else
    log "installing the .NET 9 SDK via dotnet-install.sh"
    curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
    bash /tmp/dotnet-install.sh --channel 9.0 --install-dir /usr/share/dotnet
    ln -sf /usr/share/dotnet/dotnet /usr/local/bin/dotnet
    hash -r
    dotnet --list-sdks | grep -q '^9\.' || die ".NET 9 SDK install did not take."
  fi
else
  if command -v docker >/dev/null 2>&1; then
    log "docker already installed: $(command -v docker)"
  else
    log "installing Docker via get.docker.com"
    curl -fsSL https://get.docker.com | sh
    systemctl enable --now docker || warn "could not enable docker.service"
  fi
fi

# ── build via build.sh (into a system workspace) ─────────────────────────────
log "building Kmila ($MODE) — this clones the repos and compiles"
KMILA_WORKDIR="$BUILD_DIR" KMILA_PORT="$PORT" bash "$SCRIPT_DIR/build.sh" "--$MODE"

if [ "$SERVICE" -eq 0 ]; then
  log "done (--no-service). Build output under $BUILD_DIR."
  exit 0
fi

# ── deploy ───────────────────────────────────────────────────────────────────
if [ "$MODE" = "native" ]; then
  id "$SVC_USER" >/dev/null 2>&1 || useradd --system --home-dir "$DATA_DIR" \
      --create-home --shell /usr/sbin/nologin "$SVC_USER" 2>/dev/null \
      || useradd --system --home-dir "$DATA_DIR" --create-home --shell /bin/false "$SVC_USER"
  log "deploying to $APP_DIR"
  rm -rf "$APP_DIR"; mkdir -p "$APP_DIR"
  cp -a "$BUILD_DIR/publish/." "$APP_DIR/"
  mkdir -p "$DATA_DIR/.config"
  chown -R "$SVC_USER:$SVC_USER" "$APP_DIR" "$DATA_DIR"

  DOTNET_BIN="$(command -v dotnet)"
  log "installing systemd unit (ExecStart=$DOTNET_BIN)"
  sed "s#__DOTNET__#$DOTNET_BIN#g; s#http://+:8080#http://+:$PORT#g" \
      "$SCRIPT_DIR/deploy/kmila.service" > /etc/systemd/system/kmila.service
  systemctl daemon-reload
  systemctl enable --now kmila
  sleep 2
  systemctl is-active --quiet kmila \
    && log "kmila.service is active" \
    || { warn "kmila.service not active — see: journalctl -u kmila -n 50"; }
else
  # Docker: the container carries restart: unless-stopped, and docker.service is
  # enabled on boot, so it comes back after reboot — no extra unit needed.
  log "starting the container (restart: unless-stopped)"
  cp -a "$BUILD_DIR/docker-compose.yml" "$APP_DIR-compose.yml" 2>/dev/null || true
  ( cd "$BUILD_DIR" && docker compose up -d )
fi

HOSTIP="$(hostname -I 2>/dev/null | awk '{print $1}')"
log "installed. Kmila is at:  http://${HOSTIP:-localhost}:$PORT"
[ "$MODE" = "native" ] && log "manage with: systemctl {status|restart|stop} kmila  |  logs: journalctl -u kmila -f"
