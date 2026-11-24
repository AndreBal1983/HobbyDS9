"""Acquire Architecture's R2 official artifact; never fetch at game runtime."""
import argparse
import hashlib
import io
import json
import pathlib
import urllib.request
import zipfile


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--archive", help="Optional existing official ZIP; the same cryptographic pin is required.")
    args = parser.parse_args()
    root = pathlib.Path(__file__).resolve().parents[2]
    manifest = json.loads((root/"prototypes/StateRepositoryUnityProbe/provider-manifest.json").read_text(encoding="utf-8"))["native"]
    data = pathlib.Path(args.archive).read_bytes() if args.archive else urllib.request.urlopen(manifest["url"],timeout=40).read()
    if hashlib.sha3_256(data).hexdigest() != manifest["archiveSha3_256"]:
        raise SystemExit("Official SQLite ZIP SHA3-256 differs.")
    archive = zipfile.ZipFile(io.BytesIO(data))
    binary = archive.read("sqlite3.dll")
    if hashlib.sha256(binary).hexdigest() != manifest["sha256"]:
        raise SystemExit("Official native DLL SHA256 differs.")
    output = root/"artifacts/provider-r2";output.mkdir(parents=True,exist_ok=True)
    for name, content in (("sqlite-dll-win-x64-3540100.zip",data),("e_sqlite3.dll",binary)):
        path = output/name
        if path.exists() and path.read_bytes() != content:
            raise SystemExit("Different earlier acquisition exists; preserving it: "+str(path))
        path.write_bytes(content)
    print("Verified R2 SQLite3.54.1; unchanged sqlite3.dll bytes staged as e_sqlite3.dll: "+str(output))


if __name__ == "__main__":
    main()
