"""Restore the exact source GLB using only the Python 3 standard library."""
import gzip
import hashlib
import json
from pathlib import Path


def verified(data, expected, name):
    if len(data) != expected["bytes"] or hashlib.sha256(data).hexdigest() != expected["sha256"]:
        raise SystemExit("Integrity check failed: " + name)
    return data


def main():
    root = Path(__file__).resolve().parent
    manifest = json.loads((root / "provenance.json").read_text(encoding="utf-8"))
    target = root / "galaxy-class-v2.glb"
    expected = manifest["files"][target.name]
    if target.exists():
        verified(target.read_bytes(), expected, target.name)
        print("Already restored and verified: " + str(target))
        return
    parts = []
    for part in manifest["distribution"]["parts"]:
        parts.append(verified((root / part["name"]).read_bytes(), part, part["name"]))
    data = verified(gzip.decompress(b"".join(parts)), expected, target.name)
    # Exclusive creation prevents overwriting an existing user file.
    with target.open("xb") as output:
        output.write(data)
    print("Restored and SHA-256 verified: " + str(target))


if __name__ == "__main__":
    main()
