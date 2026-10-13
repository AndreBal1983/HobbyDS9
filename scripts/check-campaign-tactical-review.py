"""Real owned campaign review-layer proof; default activation remains closed."""
from pathlib import Path
import subprocess

root=Path(__file__).resolve().parents[1]
command=["dotnet","run","--project","src/HobbyDS9.Headless","-c","Release","--no-build","--no-restore","--","--review-campaign-tactical-v2"]
result=subprocess.run(command,cwd=root,text=True,capture_output=True,check=True)
output=result.stdout.replace("\r\n","\n")
assert "AUTHORITATIVE REVIEW LAYER:" in output and "production activation=False" in output
assert "physical travel integration pending" in output
assert output.count("campaign finalized turn=")==2
assert "Actual campaign two-battle/current-pool/commitment/round/save/finalization equality passed" in output
vectors=[line.split("save-sha256=")[1] for line in output.splitlines() if line.startswith("authoritative-review ")]
assert len(vectors)==4 and len(set(vectors))==1,"Canonical/reverse/repeated/reload diverged."
assert "enemy-ammo" not in output and "civ.cardassian:ammo" not in output
print(output,end="")
