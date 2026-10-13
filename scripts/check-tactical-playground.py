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
    ("cruiser", "Flank;Flank;Flank", "23f2fcd1d5a5481cdcb33facb340827c8ea7d696aa9d1fc5e87cf8000f74f640"),
    ("cruiser", "Charge;Charge;Charge", "54a3f0914e63281994689a97029f48c38a2a9b1df60c5eb42ed262ac43c0f347"),
    ("screen", "Screen;Charge;Screen", "63202d4cd495e05bf29f9da449c8b90f9d794356cfc0a5e1c49557b8975c5248"),
    ("breakthrough", "Breakthrough;Breakthrough", "b7786e5c9bde2def82b9711b45c7a56d9c3bf327ecc6d5443f6eb699938e4197"),
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
