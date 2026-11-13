"""Receive/check two explicitly proposed original hulls; no Blender/Unity required."""
import argparse
import json
from pathlib import Path
import re
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
from receive_system_production_01 import ROOT, ASSETS, digest, guid, text

PACK = ROOT / 'art/asset-production-01/hulls-v1'
DEST = ASSETS / 'HobbyDS9/ArtDemo/Resources/ArtDemo/Production01/Hulls'
SOURCE_PIN = '70d55db18f2fb3be4e0196094736c799a88aa3f9'


def canonical(path):
    return path.read_bytes().replace(b'\r\n', b'\n')


def meta(path, importer='DefaultImporter', folder=False):
    return text(f'fileFormatVersion: 2\nguid: {guid(path)}\n' + ('folderAsset: yes\n' if folder else '') +
                f'{importer}:\n  externalObjects: {{}}\n' +
                ('  mainObjectFileID: 2100000\n' if importer == 'NativeFormatImporter' else '') +
                '  userData:\n  assetBundleName:\n  assetBundleVariant:\n')


def material(name, recipe, shader_guid):
    color = recipe['base_color_linear_factor']
    emission = [v * recipe['emission_strength'] for v in recipe['emission_color_linear']] + [1]
    assert len(color) == 4 and color[3] == 1 and recipe['alpha_mode'] == 'OPAQUE'
    def rgba(values):
        return '{' + ', '.join(k + ': ' + format(v, '.9g') for k, v in zip('rgba', values)) + '}'
    return text('%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!21 &2100000\nMaterial:\n'
                f'  serializedVersion: 8\n  m_ObjectHideFlags: 0\n  m_Name: {name}\n'
                f'  m_Shader: {{fileID: 4800000, guid: {shader_guid}, type: 3}}\n'
                '  m_ValidKeywords: []\n  m_InvalidKeywords: []\n  m_LightmapFlags: 2\n'
                '  m_EnableInstancingVariants: 0\n  m_DoubleSidedGI: 0\n  m_CustomRenderQueue: -1\n'
                '  stringTagMap: {}\n  disabledShaderPasses: []\n  m_SavedProperties:\n'
                '    serializedVersion: 3\n    m_TexEnvs: []\n    m_Ints: []\n    m_Floats:\n'
                f'    - _Metallic: {recipe["metallic"]:.9g}\n    - _Smoothness: {recipe["smoothness"]:.9g}\n'
                '    - _BumpScale: 1\n    - _UseNormalMap: 0\n    - _UseMetallicRoughnessMap: 0\n'
                f'    m_Colors:\n    - _BaseColor: {rgba(color)}\n    - _EmissionColor: {rgba(emission)}\n')


def payloads():
    manifest_bytes = canonical(PACK / 'manifest.json')
    recipes_bytes = canonical(PACK / 'material-recipes.json')
    manifest, recipes = json.loads(manifest_bytes), json.loads(recipes_bytes)
    files = {f['path']: f for f in manifest['files']}
    shader_meta = ASSETS / 'HobbyDS9/ArtDemo/Resources/ArtDemo/PreviewStudio.shader.meta'
    shader_guid = re.search(r'(?m)^guid: ([0-9a-f]{32})$', shader_meta.read_text())[1]
    template = (ASSETS / 'HobbyDS9/Review/Resources/SystemBodyArt/SolMeshes/Earth_LOD1.fbx.meta').read_text()
    outputs, records = {}, []
    assert len(recipes) == 8
    for role, recipe in recipes.items():
        path = DEST / 'Materials' / (role + '.mat')
        outputs[path] = material(role, recipe, shader_guid)
        outputs[Path(str(path) + '.meta')] = meta(path, 'NativeFormatImporter')
    for name, hull, length, triangles in [('Cruiser', 'hull.galaxy-cruiser.v1', 620, 9884),
                                         ('Escort', 'hull.defiant-combat.v1', 170, 7560)]:
        asset = next(a for a in manifest['assets'] if a['name'] == name + '_LOD0')
        relative = asset['fbx']
        data = (PACK / relative).read_bytes()
        assert digest(data) == files[relative]['sha256'] and len(data) == files[relative]['bytes']
        assert asset['candidate_binding'] == hull and len(asset['meshes']) == 1
        mesh = asset['meshes'][0]
        roles = mesh['materials']
        assert mesh['triangles'] == triangles and mesh['triangles'] <= 30000
        assert len(roles) == 8 and all(role in recipes for role in roles)
        path = DEST / (name + '_LOD0.fbx')
        outputs[path] = data
        remaps = '\n'.join('  - first:\n      type: UnityEngine:Material\n'
                           '      assembly: UnityEngine.CoreModule\n' + f'      name: {role}\n'
                           '    second: {fileID: 2100000, guid: ' + guid(DEST / 'Materials' / (role + '.mat')) + ', type: 2}'
                           for role in roles)
        model_meta = re.sub(r'(?m)^guid: [0-9a-f]{32}$', 'guid: ' + guid(path), template)
        model_meta = model_meta.replace('  externalObjects: {}', '  externalObjects:\n' + remaps)
        model_meta = model_meta.replace('materialImportMode: 0', 'materialImportMode: 2')
        outputs[Path(str(path) + '.meta')] = text(model_meta)
        records.append({'hullVersionId': hull, 'status': 'Original candidate exterior; owner artistic approval pending',
                        'sourceAssetId': asset['asset_id'], 'source': relative,
                        'resource': f'ArtDemo/Production01/Hulls/{name}_LOD0',
                        'nominalArtLengthMetres': length, 'triangles': triangles, 'roles': roles,
                        'sha256': digest(data), 'bytes': len(data), 'guid': guid(path)})
    inventory = {'schema': 1, 'sourcePin': SOURCE_PIN, 'sourceManifestSha256': digest(manifest_bytes),
                 'sourceMaterialRecipesSha256': digest(recipes_bytes), 'textHashFormat': 'UTF-8 with LF newlines',
                 'shader': 'HobbyDS9/ArtDemo/PreviewStudio', 'shaderGuid': shader_guid,
                 'materialPolicy': 'Explicit source slot remaps; factor-only opaque studio materials; private runtime clones',
                 'textures': [], 'bindings': records,
                 'approvedExternalReplacementModels': 'Remain separately unreceived; these are original proposals',
                 'nativeAcceptance': 'Pending Unity import/material/frame/lifetime/performance and owner review'}
    outputs[DEST / 'receiving-inventory.json'] = text(json.dumps(inventory, indent=2) + '\n')
    outputs[DEST / 'CREDITS.txt'] = text(
        'Original procedural hull candidates and factor-only materials: HobbyDS9 ASSET-PRODUCTION-01.\n'
        'Source pin: ' + SOURCE_PIN + '\n'
        'Editable Blender sources/export recipes/provenance: art/asset-production-01/hulls-v1.\n'
        'No downloaded hull mesh, Kuhn/Meshy mesh, reference-image pixels or unique texture allocation.\n'
        'Original proposals only; no owner artistic approval or approved external replacement identity claimed.\n'
        'Existing separate legacy source/attribution records remain unchanged.\n')
    for path in list(outputs):
        if path.suffix in ('.json', '.txt'):
            outputs[Path(str(path) + '.meta')] = meta(path, 'TextScriptImporter' if path.suffix == '.txt' else 'DefaultImporter')
    for path in (DEST.parent, DEST, DEST / 'Materials'):
        outputs[Path(str(path) + '.meta')] = meta(path, folder=True)
    return outputs


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check', action='store_true')
    args = parser.parse_args()
    if (ASSETS / 'HobbyDS9/ArtDemo/Resources/ArtDemo/Production02').exists():
        parser.error('v1 runtime slice is archived; use receive_hull_production_02.py --check. Reproduce v1 from source07e46c3.')
    outputs, seen = payloads(), {}
    for path in ASSETS.rglob('*.meta'):
        if path in outputs:
            continue
        found = re.search(r'(?m)^guid: ([0-9a-f]{32})$', path.read_text())
        if found:
            seen[found[1]] = path
    for path, data in outputs.items():
        if path.suffix == '.meta':
            key = re.search(rb'(?m)^guid: ([0-9a-f]{32})$', data)[1].decode()
            assert key not in seen, f'GUID collision: {path} / {seen.get(key)}'
            seen[key] = path
    for path, data in outputs.items():
        if path.exists():
            actual = path.read_bytes() if path.suffix == '.fbx' else canonical(path)
            assert actual == data, f'Refuse changed receiving file: {path}'
        else:
            assert not args.check, f'Missing receiving file: {path}'
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(data)
    print(f'PASS: {len(outputs)} receiving files; two original candidate hulls, eight factor-only materials, exact hashes/slots/GUIDs')


if __name__ == '__main__':
    main()
