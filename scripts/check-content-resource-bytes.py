"""Check authored/prepared byte identity and historical composition compatibility.

--checkout-matrix also exercises Git checkout conversion with true/false/input
autocrlf settings using private temporary indexes (never the contributor index).
"""
import argparse
import hashlib
import os
from pathlib import Path
import re
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[1]
RES = "client/HobbyDS9.Unity/Assets/HobbyDS9/Review/Resources/"
GENERATED_PAIRS = [
    ("scenarios/ds9-slice/scenario.json", "client/HobbyDS9.Unity/Assets/HobbyDS9/Resources/ds9-slice.json"),
    ("scenarios/ship-design-fixtures/fixture.json", "client/HobbyDS9.Unity/Assets/HobbyDS9/Resources/ship-design-fixture.json"),
]
PAIRS = []
PAIRS += [("scenarios/engineering-development-v1/scenario.json", f"{RES}PlayableFleetPackages/v1/engineering-scenario.json")]
for source, target, names in [
    ("tactical-v2-physical-review/faster", "ReviewPackages/faster", "package core scenario"),
    ("tactical-v2-physical-review/slower", "ReviewPackages/slower", "package core scenario"),
    ("integrated-playtest-v1", "IntegratedPackages/v1", "package physical core scenario"),
    ("singleplayer-demo-v1", "DemoPackages/v1", "demo package physical core scenario"),
    ("frontier-reconnaissance-v1", "FrontierPackages/v1", "frontier package physical core scenario"),
    ("playable-fleet-v1", "PlayableFleetPackages/v1", "playable package physical core scenario"),
    ("combat-playtest-v1", "CombatPlaytestV1", "demo package physical core scenario"),
]:
    PAIRS += [(f"scenarios/{source}/{n}.json", f"{RES}{target}/{n}.json") for n in names.split()]
COLONY = "catalogue ready-classes support settlement".split()
OPTIONS = "client/HobbyDS9.Unity/Assets/HobbyDS9/Review/Runtime/ColonisationPlaytestOptions.cs"


def check(root, prepared=False):
    for source, resource in PAIRS + (GENERATED_PAIRS if prepared else []):
        raw = (root / source).read_bytes()
        assert b"\r" not in raw, f"Non-LF authoring: {source}"
        assert raw == (root / resource).read_bytes(), f"Prepared bytes differ: {resource}"
    pins = re.findall(r'"([0-9a-f]{64})"', (root / OPTIONS).read_text(encoding="utf-8"))
    assert len(pins) == 5
    for name, pin in zip(COLONY, pins[:4]):
        source = (root / f"scenarios/colonisation-v1/{name}.json").read_bytes()
        raw = (root / f"{RES}ColonisationV1/{name}.json").read_bytes()
        assert b"\r" not in source
        assert raw == source.replace(b"\n", b"\r\n"), f"Historical asset differs: {name}"
        assert hashlib.sha256(raw).hexdigest() == pin, f"Historical pin differs: {name}"
    roster = (root / f"{RES}ColonisationRosterV2/roster.json").read_bytes()
    assert hashlib.sha256(roster).hexdigest() == pins[4], "Historical roster pin differs"


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--checkout-matrix", action="store_true")
    parser.add_argument("--prepared", action="store_true", help="Also check the two ignored default resources after preparation")
    parser.add_argument("--git", default="git")
    args = parser.parse_args()
    check(ROOT, args.prepared)
    print(f"Exact resource bytes: {len(PAIRS) + (len(GENERATED_PAIRS) if args.prepared else 0)} pairs; 5 historical CRLF pins passed")
    if args.checkout_matrix:
        paths = {p for pair in PAIRS for p in pair} | {".gitattributes", OPTIONS}
        paths |= {f"scenarios/colonisation-v1/{n}.json" for n in COLONY}
        paths |= {f"{RES}ColonisationV1/{n}.json" for n in COLONY}
        paths.add(f"{RES}ColonisationRosterV2/roster.json")
        with tempfile.TemporaryDirectory(prefix="hobbyds9-byte-check-") as temporary:
            env = dict(os.environ, GIT_INDEX_FILE=str(Path(temporary) / "index"))
            def git(*command, **kwargs):
                return subprocess.run([args.git, *command], cwd=ROOT, env=env, check=True, **kwargs)
            git("read-tree", "HEAD")
            for mode in ("true", "false", "input"):
                output = Path(temporary) / mode
                output.mkdir()
                git("-c", f"core.autocrlf={mode}", "checkout-index", "--force", "-z",
                    f"--prefix={output.as_posix()}/", "--stdin", input=("\0".join(sorted(paths)) + "\0").encode("utf-8"))
                check(output)
                print(f"Committed checkout core.autocrlf={mode}: passed")


if __name__ == "__main__":
    main()
