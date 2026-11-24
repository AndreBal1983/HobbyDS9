"""Stage an opt-in project only. This script never starts Unity or reads game saves."""
import argparse
import hashlib
import json
import pathlib
import shutil
import subprocess
import uuid


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def git(root, *args):
    return subprocess.check_output(["git", "-C", str(root), *args], text=True).strip()


def importer(name, native):
    guid = uuid.uuid5(uuid.NAMESPACE_URL, "hobbyds9-state-probe/" + name).hex
    return f"""fileFormatVersion: 2
guid: {guid}
PluginImporter:
  externalObjects: {{}}
  serializedVersion: 2
  iconMap: {{}}
  executionOrder: {{}}
  defineConstraints:
  - HOBBYDS9_STATE_REPOSITORY_PROBE
  isPreloaded: 0
  isOverridable: 0
  isExplicitlyReferenced: {0 if native else 1}
  validateReferences: 1
  platformData:
  - first:
      Any:
    second:
      enabled: 0
      settings: {{}}
  - first:
      Editor: Editor
    second:
      enabled: 1
      settings:
        CPU: {"x86_64" if native else "AnyCPU"}
        OS: Windows
  - first:
      Standalone: Win64
    second:
      enabled: 1
      settings:
        CPU: x86_64
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--project", required=True)
    parser.add_argument("--nuget-packages", required=True)
    parser.add_argument("--data-root", required=True)
    parser.add_argument("--data-source", required=True)
    parser.add_argument("--native-root", required=True)
    args = parser.parse_args()
    root = pathlib.Path(__file__).resolve().parents[2]
    base = root / "prototypes/StateRepositoryUnityProbe"
    project = pathlib.Path(args.project).resolve()
    if not project.is_relative_to(root / "artifacts") or project.exists():
        raise SystemExit("Use a new project path beneath this worktree's artifacts; no overwrite.")
    source = git(root, "rev-parse", "HEAD")
    if git(root, "diff", "--name-only", "HEAD"):
        raise SystemExit("Commit the reviewed source before native staging.")
    if git(root, "ls-files", "--others", "--exclude-standard", "--", "client/HobbyDS9.Unity/Assets/HobbyDS9/StateRepositoryProbe", "prototypes/StateRepositoryUnityProbe"):
        raise SystemExit("Commit all owned probe inputs before native staging.")
    data = pathlib.Path(args.data_root).resolve()
    if git(data, "rev-parse", "HEAD") != args.data_source or git(data, "diff", "--name-only", "HEAD"):
        raise SystemExit("Data source differs or has tracked edits.")
    cache = pathlib.Path(args.nuget_packages).resolve()
    manifest = json.loads((base / "provider-manifest.json").read_text(encoding="utf-8"))
    data_pin = json.loads((base / "data-provider-pin.json").read_text(encoding="utf-8"))
    if data_pin["Source"] != args.data_source:
        raise SystemExit("Data commit differs from the tested packaging pin.")
    selected = []
    for pin in manifest["files"]:
        path = cache / pin["package"].lower() / pin["packageVersion"] / pin["asset"]
        archive = cache / pin["package"].lower() / pin["packageVersion"] / (pin["package"].lower()+"."+pin["packageVersion"]+".nupkg")
        if digest(path) != pin["sha256"] or digest(archive) != pin["archiveSha256"]:
            raise SystemExit("Provider/native package pin differs: " + pin["file"])
        selected.append((path, not pin["managed"]))
    native_pin = manifest["native"]
    native_path = pathlib.Path(args.native_root).resolve() / "e_sqlite3.dll"
    if digest(native_path) != native_pin["sha256"]:
        raise SystemExit("R2 native hash differs; no old-package/OS fallback.")
    selected.append((native_path, True))
    for name in ("HobbyDS9.Persistence", "HobbyDS9.Persistence.Sqlite"):
        path = data / "src" / name / "bin/Release/netstandard2.1" / (name + ".dll")
        if not path.is_file():
            raise SystemExit("Build the tested Data source first: " + str(path))
        if digest(path) != data_pin["Assemblies"][name+".dll"]:
            raise SystemExit("Data assembly differs from the tested packaging artifact: "+str(path))
        selected.append((path, False))
    for pin in manifest["licenses"]:
        if digest(base / "ThirdPartyNotices" / pin["file"]) != pin["sha256"]:
            raise SystemExit("License pin differs.")
    assets = project / "Assets/StateRepositoryProbe"
    assets.parent.mkdir(parents=True)
    shutil.copytree(root / "client/HobbyDS9.Unity/Assets/HobbyDS9/StateRepositoryProbe", assets)
    for path, is_native in selected:
        dest = project / "Assets/Plugins/StateRepositoryProbe" / ("x86_64" if is_native else "Managed") / path.name
        dest.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(path, dest)
        dest.with_suffix(dest.suffix + ".meta").write_text(importer(path.name, is_native), encoding="utf-8")
    for directory in sorted((p for p in (project/"Assets").rglob("*") if p.is_dir())):
        meta = directory.with_suffix(directory.suffix+".meta")
        if not meta.exists():
            meta.write_text("fileFormatVersion: 2\nguid: "+uuid.uuid5(uuid.NAMESPACE_URL,"hobbyds9-state-probe/"+directory.relative_to(project).as_posix()).hex+"\nfolderAsset: yes\n",encoding="utf-8")
    settings = project / "ProjectSettings"; settings.mkdir()
    # Match the installed editor's full import-stable version file before pinning it.
    (settings / "ProjectVersion.txt").write_bytes(b"m_EditorVersion: 6000.6.9f1\nm_EditorVersionWithRevision: 6000.6.9f1 (8c45277fb9c5)\n")
    packages = project / "Packages"; packages.mkdir()
    (packages / "manifest.json").write_text(json.dumps({"dependencies":{"com.unity.modules.jsonserialize":"1.0.0"}},indent=2)+"\n",encoding="utf-8")
    shutil.copytree(base / "ThirdPartyNotices", project / "ThirdPartyNotices")
    shutil.copyfile(base / "provider-manifest.json", project / "provider-manifest.json")
    files = [{"Path":p.relative_to(project).as_posix(),"Sha256":digest(p)} for p in sorted(project.rglob("*")) if p.is_file()]
    recipe = {"Source":source,"DataSource":args.data_source,"NativeSha256":native_pin["sha256"],"Files":files}
    (project / "state-repository-probe-project.json").write_text(json.dumps(recipe,indent=2)+"\n",encoding="utf-8")
    print("Staged isolated probe; Unity/native execution remains QA-owned: " + str(project))


if __name__ == "__main__":
    main()
