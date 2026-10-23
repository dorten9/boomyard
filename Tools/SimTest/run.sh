#!/usr/bin/env bash
# Compiles the Unity-free game core with Mono and runs its checks (scoring, chains, fuses, wind, round).
set -euo pipefail
cd "$(dirname "$0")"
mkdir -p bin
mcs -nologo -optimize+ -out:bin/CoreTests.exe ../../Assets/BoomYard/Scripts/Core/*.cs CoreTests.cs
mono bin/CoreTests.exe "$@"
