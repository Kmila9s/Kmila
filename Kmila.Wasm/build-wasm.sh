#!/usr/bin/env bash
#
# build-wasm.sh — build / run the Kmila.Wasm browser demo without having to
# remember the -p:KmilaWasm=true flag.
#
# WHY THE FLAG IS REQUIRED (and can't live in the .csproj): it must be a GLOBAL
# MSBuild property so it applies at RESTORE, where Kmila.Shared's transitive
# Microsoft.AspNetCore.App FrameworkReference is resolved. Without it that
# framework has no browser-wasm runtime pack and the build fails (NETSDK1082).
#
# Works from anywhere — paths are resolved relative to this script; you do NOT
# need to cd into the project directory.
#
#   ./build-wasm.sh run [url]     # dev server (default http://localhost:5299)
#   ./build-wasm.sh publish [dir] # static publish (default ./bin/publish/wwwroot)
#   ./build-wasm.sh build         # compile only
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]:-$0}")" && pwd)"
CSPROJ="$SCRIPT_DIR/Kmila.Wasm.csproj"
FLAG="-p:KmilaWasm=true"
CMD="${1:-run}"

case "$CMD" in
  run)
    URL="${2:-http://localhost:5299}"
    exec dotnet run --project "$CSPROJ" -c Debug $FLAG --urls "$URL"
    ;;
  build)
    exec dotnet build "$CSPROJ" -c Release $FLAG
    ;;
  publish)
    OUT="${2:-$SCRIPT_DIR/bin/publish}"
    dotnet publish "$CSPROJ" -c Release $FLAG -o "$OUT"
    echo "Static site: $OUT/wwwroot  (serve with any static file host)"
    ;;
  *)
    echo "usage: $0 {run [url]|build|publish [dir]}" >&2
    exit 1
    ;;
esac
