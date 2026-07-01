#!/bin/bash
# Usage: ./snap.sh <subdir> <name>
SNAP="/Users/miku/Documents/Kmila-9s/App/Kmila/full_app_exploration_2026-06-14"
SUB="$1"
NAME="$2"
mkdir -p "$SNAP/$SUB"
sleep 0.4
adb exec-out screencap -p > "$SNAP/$SUB/$NAME.png"
echo "→ $SNAP/$SUB/$NAME.png ($(stat -f%z "$SNAP/$SUB/$NAME.png") bytes)"
