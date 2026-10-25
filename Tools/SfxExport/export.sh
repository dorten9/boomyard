#!/usr/bin/env bash
# Renders every synthesised sound and the music loop (Scripts/Game/Sfx.cs) to WAV files, with Mono, no Unity.
# The synthesis half of Sfx.cs is cut out as text and compiled against a tiny AudioClip/Mathf shim.
# Usage: Tools/SfxExport/export.sh <outDir>
set -euo pipefail
here="$(cd "$(dirname "$0")" && pwd)"
out="${1:-$here/out}"
src="$here/../../Assets/BoomYard/Scripts/Game/Sfx.cs"
mkdir -p "$here/bin" "$out"
python3 - "$src" "$here/bin/SfxGen.cs" <<'PY'
import re, sys
s = open(sys.argv[1]).read()
fields = re.search(r"\n(\s*AudioClip flick,[^;]*;)", s).group(1)
awake = s[s.index("var r = new System.Random(7);"):s.index("ui = gameObject.AddComponent")]
synth = s[s.index("// ---------- synthesis ----------"):s.rindex("}", 0, s.rindex("}"))]
consts = "\n".join(re.findall(r"\n\s*(const int \w+ = \d+;)", s))
names = re.findall(r"\b(\w+) = ", fields.replace("AudioClip", "").replace(",", " = ").replace(";", " = "))
names = [n for n in re.findall(r"\w+", fields) if n != "AudioClip"]
out = """using System.Collections.Generic;
using UnityEngine;
namespace BoomYard.Game {
public sealed class SfxGen {
%s
public %s
public AudioClip music;
public void Build() {
%s
music = MakeMusic();
}
public Dictionary<string, AudioClip> All() { return new Dictionary<string, AudioClip> { %s, { "music", music } }; }
%s
}
}""" % (consts, fields.strip(), awake, ", ".join('{ "%s", %s }' % (n, n) for n in names), synth)
open(sys.argv[2], "w").write(out)
PY
# The street's own sounds (car alarm, barks, whistle, rain) live in StreetLife.cs as a self-contained class.
python3 - "$here/../../Assets/BoomYard/Scripts/Game/StreetLife.cs" "$here/bin/StreetGen.cs" <<'PY'
import sys
s = open(sys.argv[1]).read()
body = s[s.index("    /// <summary>The street's own sounds"):s.rindex("}")]
open(sys.argv[2], "w").write("using UnityEngine;\nnamespace BoomYard.Game {\n" + body + "}\n")
PY
mcs -nologo -optimize+ -out:"$here/bin/SfxExport.exe" "$here/bin/SfxGen.cs" "$here/bin/StreetGen.cs" "$here/Shim.cs" "$here/Main.cs"
mono "$here/bin/SfxExport.exe" "$out"
