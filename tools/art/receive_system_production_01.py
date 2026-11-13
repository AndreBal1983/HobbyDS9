"""Receive/check the exact Earth/Mars production slice; stdlib only, no Unity run."""
import argparse
import hashlib
import json
from pathlib import Path
import re
import uuid

ROOT = Path(__file__).resolve().parents[2]
PACK = ROOT / 'art/asset-production-01/system-v1'
ASSETS = ROOT / 'client/HobbyDS9.Unity/Assets'
DEST = ASSETS / 'HobbyDS9/Review/Resources/SystemBodyArt/Production01'
SOURCE_PIN = '560d6c13697ba708c5aae4c450c6e5dafa72c837'


def digest(data):
    return hashlib.sha256(data).hexdigest()


def guid(path):
    return uuid.uuid5(uuid.NAMESPACE_URL, 'hobbyds9.gui.production01/' + path.relative_to(ASSETS).as_posix()).hex


def text(value):
    return value.encode('utf-8')


def payloads():
    # Pin the Git text content, independent of Windows checkout newline conversion.
    # Later Art batches extend their manifest; preserve the first receiving pin's
    # exact source evidence rather than rewriting a received inventory on extension.
    manifest_bytes = (ROOT / 'tools/art/contracts/system-production-01-560d6c1.json').read_bytes().replace(b'\r\n', b'\n')
    manifest = json.loads(manifest_bytes)
    files = {f['path']: f for f in manifest['files']}
    model_template = (DEST.parent / 'SolMeshes/Earth_LOD1.fbx.meta').read_text(encoding='utf-8')
    # Runtime constructs one explicit ProceduralSystemSurface material. Ignore all
    # FBX studio materials and external image references; keep mesh validation readable.
    assert 'materialImportMode: 0' in model_template and 'isReadable: 1' in model_template
    outputs = {}
    records = []
    for body, identity, map_name in [('Earth', 'planet.sol', 'earth_daymap'), ('Mars', 'planet.mars', 'mars')]:
        relative = f'Models/{body}_LOD1.fbx'
        data = (PACK / relative).read_bytes()
        assert digest(data) == files[relative]['sha256'], relative + ' payload mismatch'
        assert len(data) == files[relative]['bytes'], relative + ' size mismatch'
        asset = next(a for a in manifest['assets'] if a['name'] == body + '_LOD1')
        assert asset['candidate_binding'] == identity
        assert len(asset['meshes']) == 1 and asset['meshes'][0]['triangles'] == 2208
        source_texture = next(t for t in manifest['texture_sources'] if t['path'] == f'Textures/{map_name}.jpg')
        texture_path = DEST.parent / f'{map_name}.jpg'
        assert digest(texture_path.read_bytes()) == source_texture['sha256'], 'Borrowed texture changed'
        path = DEST / f'{body}_LOD1.fbx'
        outputs[path] = data
        outputs[Path(str(path) + '.meta')] = text(re.sub(r'(?m)^guid: [0-9a-f]{32}$', 'guid: ' + guid(path), model_template))
        records.append({'planetId': identity, 'requiresSurvey': True, 'source': relative,
                        'sourceAssetId': asset['asset_id'], 'resource': f'SystemBodyArt/Production01/{body}_LOD1',
                        'sha256': digest(data), 'bytes': len(data), 'triangles': 2208,
                        'guid': guid(path), 'borrowedTexture': f'SystemBodyArt/{map_name}',
                        'textureSha256': source_texture['sha256']})
    inventory = {'schema': 1, 'sourcePin': SOURCE_PIN,
                 'sourceManifestSha256': digest(manifest_bytes),
                 'sourceManifestHashFormat': 'UTF-8 with LF newlines', 'bindings': records,
                 'material': 'HobbyDS9/Review/ProceduralSystemSurface',
                 'materialBinding': 'Runtime shared material/property blocks; FBX materials not imported',
                 'atmosphereShell': 'Not received or displayed; existing shader rim only',
                 'otherSolBodies': 'Source/reference only; no runtime production binding',
                 'nativeAcceptance': 'Pending actual Unity import/URP/native QA'}
    outputs[DEST / 'receiving-inventory.json'] = text(json.dumps(inventory, indent=2) + '\n')
    outputs[DEST / 'CREDITS.txt'] = text(
        'Original unit-sphere geometry: HobbyDS9 ASSET-PRODUCTION-01, Blender 5.3.0.\n'
        'Source pin: ' + SOURCE_PIN + '\n'
        'Editable sources, manifests, export recipe and studio evidence: art/asset-production-01/system-v1.\n'
        'Runtime borrows unchanged earth_daymap.jpg / mars.jpg from the parent SystemBodyArt folder.\n'
        'Textures: Solar System Scope / INOVE, CC BY 4.0; https://www.solarsystemscope.com/textures/\n'
        'Retain parent ATTRIBUTION.txt. No studio materials, atmosphere shell or additional Sol identities received.\n')
    for path in list(outputs):
        if path.suffix not in ('.json', '.txt'):
            continue
        importer = 'TextScriptImporter' if path.suffix == '.txt' else 'DefaultImporter'
        outputs[Path(str(path) + '.meta')] = text(
            f'fileFormatVersion: 2\nguid: {guid(path)}\n{importer}:\n  externalObjects: {{}}\n'
            '  userData:\n  assetBundleName:\n  assetBundleVariant:\n')
    outputs[Path(str(DEST) + '.meta')] = text(
        f'fileFormatVersion: 2\nguid: {guid(DEST)}\nfolderAsset: yes\nDefaultImporter:\n'
        '  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n')
    return outputs


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check', action='store_true', help='Verify only; never write files')
    args = parser.parse_args()
    outputs = payloads()
    seen = {}
    for path in ASSETS.rglob('*.meta'):
        if path in outputs:
            continue
        match = re.search(r'(?m)^guid: ([0-9a-f]{32})$', path.read_text(encoding='utf-8'))
        if match:
            seen[match[1]] = path
    for path, data in outputs.items():
        if path.suffix != '.meta':
            continue
        asset_guid = re.search(rb'(?m)^guid: ([0-9a-f]{32})$', data)[1].decode()
        assert asset_guid not in seen, f'GUID collision: {path} / {seen.get(asset_guid)}'
        seen[asset_guid] = path
    for path, data in outputs.items():
        if path.exists():
            actual = path.read_bytes()
            if path.suffix != '.fbx':
                actual = actual.replace(b'\r\n', b'\n')
            assert actual == data, f'Refuse changed receiving file: {path}'
        else:
            assert not args.check, f'Missing receiving file: {path}'
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(data)
    print(f'PASS: {len(outputs)} receiving files, two exact surveyed bindings, source hashes/maps, budgets and GUIDs')


if __name__ == '__main__':
    main()
