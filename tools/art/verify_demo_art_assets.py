"""Verify the tracked runtime pack without Unity, Blender, or GPU work."""
from pathlib import Path
import hashlib
import json
import re

ROOT = Path(__file__).resolve().parents[2]
PACK = ROOT / 'client/HobbyDS9.Unity/Assets/HobbyDS9/ArtDemo'
manifest = json.loads((PACK / 'runtime-manifest.json').read_text(encoding='utf-8'))
for entry in manifest['files']:
    file = PACK / entry['file']
    assert file.is_file(), file
    data = file.read_bytes()
    if entry.get('hashMode') == 'utf8-lf-trim-eol':
        data = ('\n'.join(line.rstrip() for line in data.decode('utf-8').splitlines()) + '\n').encode('utf-8')
    assert hashlib.sha256(data).hexdigest() == entry['sha256'], file
    assert len(data) == entry['bytes'], file
guids = {}
for file in (ROOT / 'client/HobbyDS9.Unity/Assets').rglob('*.meta'):
    match = re.search(r'^guid: (\w+)', file.read_text(encoding='utf-8'), re.M)
    if match:
        assert match[1] not in guids, (file, guids.get(match[1]))
        guids[match[1]] = file
for prefab in (PACK / 'Resources').rglob('*.prefab'):
    for guid in re.findall(r'guid: (\w+)', prefab.read_text(encoding='utf-8')):
        assert guid in guids, (prefab, guid)
        assert 'ArtDemo' in guids[guid].parts, (prefab, guids[guid])
assert len(list((PACK / 'Resources').rglob('*.fbx'))) == 9
assert len(list((PACK / 'Resources').rglob('*.prefab'))) == 3
assert len(list((PACK / 'Resources').rglob('*.mat'))) == 9
print(f"Verified {len(manifest['files'])} runtime files, hashes, prefab GUID references and pack counts.")
