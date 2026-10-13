"""Actual weighted campaign travel and tactical lifecycle; proposed profile stays gated."""
from pathlib import Path
import subprocess

root = Path(__file__).resolve().parents[1]
result = subprocess.run([
    "dotnet", "run", "--project", "src/HobbyDS9.Headless", "-c", "Release",
    "--no-build", "--no-restore", "--", "--review-physical-travel"
], cwd=root, text=True, capture_output=True, check=True)
output = result.stdout.replace("\r\n", "\n")
assert "PHYSICAL AUTHORITATIVE REVIEW:" in output and "production activation=False" in output
assert "policies proposed" in output
assert "physical faster met at nav.bajor time=3/4" in output
assert "physical slower missed: own-progress=2/3; target-progress=1/2" in output
assert "physical finalized battle=1 turn=2 own-ammo=3" in output
assert "physical finalized battle=2 turn=3 own-ammo=0" in output
expected = {
    "faster": "a6a2522a4f389b0bb8aa562d6273b0f02b3ea1fdeb57c7fe21d8368e4fa4d177",
    "slower": "77073cc154b78dbca2e5f6fa1a618cae490d11410f51f4cb2bb9e640a33210c4",
}
for profile, vector in expected.items():
    lines = [line for line in output.splitlines() if line.startswith(f"physical {profile} ") and " save-sha256=" in line]
    assert len(lines) == 5
    assert {line.split(" save-sha256=")[1] for line in lines} == {vector}, "Investigate physical determinism drift."
assert "two battles/save-reload equality passed; no prototype itinerary imported" in output
print(output, end="")
