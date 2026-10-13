"""Exercise authoritative scenario/session inspection; never activate campaign v2."""
import json
import subprocess
import tempfile
from pathlib import Path

scenario_path = Path('scenarios/tactical-v2-equipment-source/scenario.json')


def run(path, inspect=True):
    args = ['dotnet', 'run', '--project', 'src/HobbyDS9.Headless', '-c', 'Release',
            '--no-build', '--no-restore', '--', str(path)]
    if inspect:
        args.extend(['--inspect-tactical-v2', 'civ.federation'])
    return subprocess.run(args, capture_output=True, text=True)


plain, inspected, repeated = run(scenario_path, False), run(scenario_path), run(scenario_path)
for result in (plain, inspected, repeated):
    if result.returncode or result.stderr:
        raise AssertionError(result.stderr)
if inspected.stdout != repeated.stdout:
    raise AssertionError('Repeated source inspection differs')
diagnostic_prefixes = ('tactical-v2-inspection-', 'tactical-v2-blockers=', 'tactical-source=',
                       'tactical-equipment=', 'tactical-power=', 'tactical-capabilities=',
                       'tactical-initial-shield=', 'tactical-weapon=')
if '\n'.join(line for line in inspected.stdout.splitlines() if not line.startswith(diagnostic_prefixes)) != plain.stdout.rstrip():
    raise AssertionError('Inspection changed the campaign diagnostic')
for evidence in ('can-activate=False', 'tactical-source=ship.equipment-cruiser',
                 'tactical-source=ship.equipment-surveyor', 'can-project-v2=True',
                 'tactical-source=ship.federation-scout', 'can-project-v2=False',
                 'essentials=26, equipment-budget=54, equipment-allocated=36',
                 'class=Beam', 'class=Cannon', 'class=Torpedo', 'authored-initial=7/12',
                 'per-ship source only; formation derivation unavailable',
                 'SessionRoundCheckpointAndOrderAuthorityRequired'):
    if evidence not in inspected.stdout:
        raise AssertionError('Missing authored source evidence: ' + evidence)
if 'tactical-equipment=ship.cardassian' in inspected.stdout:
    raise AssertionError('Foreign source disclosed')

bad = json.loads(scenario_path.read_text(encoding='utf-8'))
bad['Definitions']['AuthoredTacticalEquipment']['Fits'][0]['DistributionLimit'] = 35
with tempfile.TemporaryDirectory() as directory:
    invalid = Path(directory) / 'infeasible-source.json'
    invalid.write_text(json.dumps(bad), encoding='utf-8')
    rejected = run(invalid)
    if not rejected.returncode or 'InvalidTacticalEquipmentPower' not in rejected.stderr:
        raise AssertionError('Essential reservations did not reject infeasible power')
print('Authored equipment: actual scenario sources complete, legacy ineligible, gate closed; repeat/state/power rejection passed')
