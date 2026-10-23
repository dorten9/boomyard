"""Fails (exit 1) when a smoke screenshot is mostly magenta (a missing shader) or blank (nothing rendered)."""
import sys
from PIL import Image

im = Image.open(sys.argv[1]).convert('RGB').resize((233, 108))
px = list(im.getdata())
magenta = sum(1 for r, g, b in px if r > 230 and g < 40 and b > 230) / len(px)
distinct = len(set((r // 16, g // 16, b // 16) for r, g, b in px))
print(f'magenta: {magenta:.1%}, colour buckets: {distinct}')
sys.exit(1 if magenta > 0.05 or distinct < 6 else 0)
