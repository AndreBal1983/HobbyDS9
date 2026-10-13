"""Execute review-only alternatives; no campaign import, activation or golden replacement."""
import hashlib
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ["dotnet", "run", "--project", "src/HobbyDS9.Headless", "-c", "Release",
           "--no-build", "--no-restore", "--", "--review-lifecycle-prototypes"]


def run():
    completed = subprocess.run(COMMAND, cwd=ROOT, capture_output=True, text=True, check=True)
    return completed.stdout.replace("\r\n", "\n")


first = run()
assert first == run(), "Repeated review transcript diverged."
for expected in [
    "PROTOTYPE ONLY:",
    "travel simultaneous-transit DeparturesFirst meetings=0",
    "travel simultaneous-transit ArrivalsBeforeDepartures meetings=1",
    "contact node=B time=1/2 stopped=a,b",
    "travel known-contact-pursuit speed=1 meetings=0",
    "travel known-contact-pursuit speed=3 meetings=1",
    "round mutual-cloak=False boundary=2 rolls=2 ammo=ship.a:5,ship.b:7 hidden=0",
    "round mutual-cloak=True boundary=2 rolls=0 ammo=ship.a:7,ship.b:7 hidden=0",
    "prototype-batch later-failure-rollback=True finalize-once=True",
    "travel shared-start DepartBeforeRenewedContact meetings=0",
    "travel shared-start ContactBeforeDeparture meetings=1",
    "travel weighted-edge origin=B destination=C progress=3/1 sector-contact=False",
    "activation=false",
]:
    assert expected in first, expected
vectors = [line.split("sha256=")[1] for line in first.splitlines() if line.startswith("prototype-three-round ")]
assert len(vectors) == 4 and len(set(vectors)) == 1, "Prototype canonical/reverse/reorder/reload differ."
enemy_reports = [line for line in first.splitlines() if line.startswith("lawful-report actor.b ")]
assert not any("ship.a" in line or "ammo" in line or "fit." in line for line in enemy_reports), "Hidden launcher/private resources leaked."
print(first, end="")
print("Review prototypes passed; transcript sha256=" + hashlib.sha256(first.encode("utf-8")).hexdigest())
