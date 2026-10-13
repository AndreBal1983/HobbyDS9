"""Headless inspection must reject activation and preserve the legacy campaign."""
import re
import subprocess

BASELINE = "fb2b75f18c8c5423f3a0850f38f6e53ee8249d3d08385978b5b2532041cc6089"


def run(extra):
    result = subprocess.run(
        ["dotnet", "run", "--project", "src/HobbyDS9.Headless", "-c", "Release",
         "--no-build", "--no-restore", "--", "--turns", "25", *extra],
        capture_output=True, text=True, check=True,
    )
    if result.stderr:
        raise AssertionError(result.stderr)
    if re.findall(r"save-sha256=([0-9a-f]{64})", result.stdout) != [BASELINE]:
        raise AssertionError("Inspection changed the legacy save baseline")
    if "rng=d8919406d6e01cf1" not in result.stdout:
        raise AssertionError("Inspection changed campaign RNG")
    return result.stdout


plain = run([])
inspected = run(["--inspect-tactical-v2", "civ.federation"])
if "can-activate=False, campaign-resolver=combat-resolver.phased/1" not in inspected:
    raise AssertionError("Campaign v2 activation must remain blocked")
for blocker in ("ExplicitEquipmentSchemaRequired", "PersistentShipResourcesAndRecoveryPolicyRequired",
                "SessionRoundCheckpointAndOrderAuthorityRequired", "StrategicRetreatAndObjectivePolicyRequired",
                "VersionedCampaignActivationAndSaveContractRequired"):
    if blocker not in inspected:
        raise AssertionError(f"Missing activation blocker: {blocker}")
strategic_lines = "\n".join(line for line in inspected.splitlines() if not line.startswith("tactical-"))
if strategic_lines != "\n".join(plain.splitlines()):
    raise AssertionError("Inspection changed a strategic diagnostic")
print("Campaign v2 inspection: explicit gate closed; exact legacy state/RNG/transcript preserved")
