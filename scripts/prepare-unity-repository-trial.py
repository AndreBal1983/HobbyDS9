"""Stage the reviewed schema-2 provider in a fresh, isolated QA worktree."""

import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import uuid


DATA_SOURCE = "43846f42bd61bf9eeb04006dc440cd3d550d1657"
DEFINE = "HOBBYDS9_STATE_REPOSITORY_TRIAL"
PROVIDER_FILES = {
    "HobbyDS9.Persistence.Sqlite.dll": "054385282a97e8eb37ff6d3dcb7c979ea4b68502569d53af7fcac18e185c9de6",
    "HobbyDS9.Persistence.dll": "aa7f1f6b51c923067d8c7e5275dab4198395c43f9b09524d1b96158c74903e96",
    "Microsoft.Data.Sqlite.dll": "6a851c2c3767c96319ef50a5c1ce80dfd17270d27125a74ca0b7e911256fa064",
    "SQLitePCLRaw.core.dll": "50c999a1ab2afeacc93bce5ac2948d3bb630d99d23efc3cfcbd8bc11d3426eb7",
    "SQLitePCLRaw.provider.e_sqlite3.dll": "e9acf8fcd48769891a67890359de341ece1065066cc9b58fc6196c3da3a41fde",
    "e_sqlite3.dll": "4b59d3cded9dc1608544160f3b4b10ac974c6ac0d6ffc23064e5f4c8dbd05aea",
}
HOST = "HobbyDS9.CampaignRepositoryHost.dll"


def sha(data):
    return hashlib.sha256(data).hexdigest()


def git(root, *args):
    return subprocess.check_output(["git", "-C", str(root), *args], text=True).strip()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source", required=True)
    parser.add_argument("--data-bin", type=Path, required=True)
    parser.add_argument("--probe-project", type=Path, required=True)
    args = parser.parse_args()
    root = Path(__file__).resolve().parent.parent
    if not (root / ".git").is_file() or git(root, "rev-parse", "HEAD") != args.source:
        raise SystemExit("Use a fresh linked QA worktree at the exact receiving source SHA.")
    if git(root, "status", "--porcelain", "--untracked-files=no"):
        raise SystemExit("Stage from clean tracked source before importing the project in Unity.")
    if git(root, "diff", "--name-only", DATA_SOURCE, "HEAD", "--", "src/HobbyDS9.Persistence", "src/HobbyDS9.Persistence.Sqlite"):
        raise SystemExit("The checked-in storage sources differ from the reviewed Data delivery.")
    project = root / "client/HobbyDS9.Unity"
    assets = project / "Assets"
    plugins = assets / "Plugins/StateRepositoryTrial"
    notices = assets / "StreamingAssets/StateRepositoryTrial"
    receipt = root / "artifacts/repository-trial-stage.json"
    if any(path.exists() for path in (plugins, notices, receipt)):
        raise SystemExit("A trial stage already exists. Preserve it and start from a fresh worktree.")
    if any(assets.rglob("e_sqlite3.dll")):
        raise SystemExit("An existing SQLite native binary would make the trial ambiguous.")

    host_bin = root / "prototypes/HobbyDS9.CampaignRepositoryHost/bin/Release/netstandard2.1"
    for name in (HOST, "HobbyDS9.TacticalReviewClient.dll"):
        if not (host_bin / name).is_file():
            raise SystemExit("Build CampaignRepositoryHost in Release in this worktree first.")
    host_review = (host_bin / "HobbyDS9.TacticalReviewClient.dll").read_bytes()
    if (assets / "Plugins/HobbyDS9/HobbyDS9.TacticalReviewClient.dll").read_bytes() != host_review:
        raise SystemExit("Prepare Unity and build the repository host from the same source.")

    probe = args.probe_project / "Assets/Plugins/StateRepositoryProbe"
    files = {}
    for name in (*PROVIDER_FILES, HOST):
        is_native = name == "e_sqlite3.dll"
        folder = "x86_64" if is_native else "Managed"
        data = ((host_bin if name == HOST else args.data_bin) / name).read_bytes()
        if name != HOST and sha(data) != PROVIDER_FILES[name]:
            raise SystemExit(f"Provider hash mismatch for {name}.")
        meta_name = "HobbyDS9.Persistence.dll" if name == HOST else name
        meta = (probe / folder / (meta_name + ".meta")).read_text(encoding="utf-8")
        lines = meta.splitlines()
        if len(lines) < 5 or not lines[1].startswith("guid: ") or meta.count("HOBBYDS9_STATE_REPOSITORY_PROBE") != 1:
            raise SystemExit(f"Unexpected isolated probe importer for {name}.")
        lines[1] = "guid: " + uuid.uuid5(uuid.NAMESPACE_URL, "hobbyds9-repository-trial/" + name).hex
        files[plugins / folder / name] = data
        files[plugins / folder / (name + ".meta")] = ("\n".join(lines) + "\n").replace("HOBBYDS9_STATE_REPOSITORY_PROBE", DEFINE).encode()

    probe_link = args.probe_project / "Assets/StateRepositoryProbe/Runtime/link.xml"
    linker_lines = [line for line in probe_link.read_text(encoding="utf-8").splitlines() if 'fullname="HobbyDS9.StateRepositoryProbe"' not in line]
    files[plugins / "link.xml"] = ("\n".join(linker_lines) + "\n").encode()
    for name in ("Apache-2.0.txt", "Microsoft-MIT.txt", "NOTICE.txt"):
        files[notices / name] = (root / "prototypes/StateRepositoryUnityProbe/ThirdPartyNotices" / name).read_bytes()

    asmdef = assets / "HobbyDS9/Review/Runtime/HobbyDS9.UnityReview.asmdef"
    assembly = json.loads(asmdef.read_bytes())
    assembly["precompiledReferences"].extend(name for name in (*PROVIDER_FILES, HOST) if name != "e_sqlite3.dll")
    settings = project / "ProjectSettings/ProjectSettings.asset"
    original_settings = settings.read_text(encoding="utf-8")
    marker = "  scriptingDefineSymbols: {}\n"
    if original_settings.count(marker) != 1:
        raise SystemExit("Expected the unmodified standalone define settings.")
    changes = {
        asmdef: (json.dumps(assembly, indent=2) + "\n").encode(),
        settings: original_settings.replace(marker, "  scriptingDefineSymbols:\n    Standalone: " + DEFINE + "\n").encode(),
        project / "ProjectSettings/ProjectVersion.txt": b"m_EditorVersion: 6000.6.9f1\nm_EditorVersionWithRevision: 6000.6.9f1 (8c45277fb9c5)\n",
    }

    for path, data in files.items():
        path.parent.mkdir(parents=True, exist_ok=True)
        with path.open("xb") as stream:
            stream.write(data)
    for path, data in changes.items():
        path.write_bytes(data)
    pins = {str(path.relative_to(project)).replace("\\", "/"): sha(data) for path, data in {**files, **changes}.items()}
    receipt.parent.mkdir(parents=True, exist_ok=True)
    with receipt.open("x", encoding="utf-8") as stream:
        json.dump({"source": args.source, "dataSource": DATA_SOURCE, "schema": 2, "nativeAcceptance": "pending", "pins": pins}, stream, indent=2)
        stream.write("\n")
    print(receipt)


if __name__ == "__main__":
    main()
