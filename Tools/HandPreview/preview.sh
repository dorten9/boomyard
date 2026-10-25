#!/usr/bin/env bash
# Renders the first-person fist (Core/HandRig.cs) from three sides to a PNG, without Unity: the rig is dumped
# to JSON with Mono and drawn with three.js in headless Chromium. Usage: Tools/HandPreview/preview.sh <out.png>
set -euo pipefail
here="$(cd "$(dirname "$0")" && pwd)"
work="$here/bin"
mkdir -p "$work"
mcs -nologo -out:"$work/Dump.exe" "$here/../../Assets/BoomYard/Scripts/Core/HandRig.cs" "$here/Dump.cs"
mono "$work/Dump.exe" > "$work/hand.json"
[ -f "$work/three.min.js" ] || (cd "$work" && npm i --no-save --silent three@0.160.0 && cp node_modules/three/build/three.min.js .)
cp "$here/preview.html" "$work/index.html"
(cd "$work" && python3 -m http.server 8790 >/dev/null 2>&1 & echo $! > "$work/server.pid")
sleep 1
node "$here/shot.mjs" http://localhost:8790/index.html "${1:-$here/bin/hand.png}" || true
kill "$(cat "$work/server.pid")" 2>/dev/null || true
