"""Check a generated static Unity build before it can replace the team site."""
import json
import re
import sys
from pathlib import Path

folder = Path(sys.argv[1] if len(sys.argv) > 1 else "Builds/WebGL")
html = (folder / "index.html").read_text(encoding="utf-8")
info = json.loads((folder / "build-info.json").read_text(encoding="utf-8"))
assert "{{{" not in html, "Unexpanded Unity template expression"
assert info["version"] in html, "HTML and version metadata disagree"
assert (folder / ".nojekyll").is_file(), "Missing GitHub Pages marker"
assets = set(re.findall(r"Build/[a-zA-Z0-9_.-]+", html))
assert len(assets) >= 4, "Missing Unity loader/data/framework/wasm references"
for asset in assets:
    file = folder / asset
    assert file.is_file() and file.stat().st_size > 0, f"Missing or empty asset: {asset}"
    assert file.stat().st_size < 100 * 1024 * 1024, f"Asset exceeds GitHub file limit: {asset}"
wasm = list((folder / "Build").glob("*.wasm"))
assert len(wasm) == 1, "Expected one WebAssembly module"
assert wasm[0].read_bytes()[:4] == b"\x00asm", "Invalid WebAssembly module"
print(f"WEB_FILES_PASSED: {len(assets)} assets / {info['version']}")
