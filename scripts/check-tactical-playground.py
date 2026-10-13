"""Execute the real console host without a desktop; fail on every subprocess error."""
import re
import subprocess
from pathlib import Path


def run(arguments):
    result = subprocess.run(
        ["dotnet", "run", "--project", "src/HobbyDS9.TacticalPlayground", "-c", "Release",
         "--no-build", "--no-restore", "--", *arguments],
        capture_output=True, text=True, check=True,
    )
    if result.stderr:
        raise AssertionError(result.stderr)
    if result.stdout.count("AFTER-ACTION REPORT") > 3:
        raise AssertionError("Unexpected fourth tactical round")
    return result.stdout


def checksum(output):
    matches = re.findall(r"playground-sha256=([0-9a-f]{64})", output)
    if len(matches) != 1:
        raise AssertionError("Missing/duplicate authoritative checksum")
    return matches[0]


fixtures = [
    ("cruiser", "Flank;Flank;Flank", "0de23f5fb76b8e3150a0593d69a760254c7a931a669c5258beb06d5b2618eee3"),
    ("cruiser", "Charge;Charge;Charge", "f67cd3e66055f7db10880ddb37845459f6c81bf2d410db7870f2f557eb07052e"),
    ("screen", "Screen;Charge;Screen", "82f027e8d29ba28ea0c3e5117dafb53de47aac38727fcb36cdb2f843910eae72"),
    ("breakthrough", "Breakthrough;Breakthrough", "ac8cbcb951e0f75ccab886a4adaba63367941bedaa30e9dd992115b2c65c17a6"),
]
canonical = None
for scenario, orders, expected in fixtures:
    arguments = ["--scenario", scenario, "--seed", "47", "--script", orders]
    output = run(arguments)
    if checksum(output) != expected:
        raise AssertionError(f"Unexpected {scenario}/{orders} result")
    if canonical is None:
        canonical = output
        if run(arguments) != output or run([*arguments, "--reverse-fixture"]) != output:
            raise AssertionError("Repeated/reversed console transcript changed")
    print(f"Playable {scenario}/{orders}: {expected} passed")

directory = Path("artifacts/tactical-playground")
directory.mkdir(parents=True, exist_ok=True)
for boundary in (1, 2):
    path = directory / f"ci-boundary-{boundary}.json"
    run(["--scenario", "cruiser", "--seed", "47", "--script", ";".join(["Flank"] * boundary),
         "--stop-after", str(boundary), "--save", str(path)])
    resumed = run(["--resume", str(path), "--script", ";".join(["Flank"] * (3 - boundary))])
    if checksum(resumed) != fixtures[0][2]:
        raise AssertionError(f"Round {boundary} resume changed the result")
    print(f"Round {boundary} save/resume: exact authoritative checksum passed")
