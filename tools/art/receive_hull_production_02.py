"""Receive/check the same original hull candidates with one atlas material each."""
import argparse
import json
from pathlib import Path
import re
import struct
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
from receive_system_production_01 import ROOT, ASSETS, digest, guid, text
from receive_hull_production_01 import meta, canonical

PACK = ROOT / 'art/asset-production-01/hulls-runtime-v2'
DEST = ASSETS / 'HobbyDS9/ArtDemo/Resources/ArtDemo/Production02/Hulls'
SOURCE_PIN = 'be7ef4575a7f820741333e2dbd6683da9aa85249'
MAPS = {'BaseColor': '_BaseMap', 'ORM': '_MetallicRoughnessMap',
        'Normal': '_BumpMap', 'Emission': '_EmissionMap'}


def payloads():
    manifest_bytes = canonical(PACK / 'manifest.json')
    contract_bytes = canonical(PACK / 'material-contract.json')
    manifest, contract = json.loads(manifest_bytes), json.loads(contract_bytes)
    files = {f['path']: f for f in manifest['files']}
    shader_meta = ASSETS / 'HobbyDS9/ArtDemo/Resources/ArtDemo/PreviewStudio.shader.meta'
    shader_guid = re.search(r'(?m)^guid: ([0-9a-f]{32})$', shader_meta.read_text())[1]
    template = (ASSETS / 'HobbyDS9/Review/Resources/SystemBodyArt/SolMeshes/Earth_LOD1.fbx.meta').read_text()
    outputs, records = {}, []

    def source(relative):
        data = (PACK / relative).read_bytes()
        assert digest(data) == files[relative]['sha256'] and len(data) == files[relative]['bytes'], relative
        return data

    for name, hull, length, triangles in [('Cruiser', 'hull.galaxy-cruiser.v1', 620, 9884),
                                         ('Escort', 'hull.defiant-combat.v1', 170, 7560)]:
        c = contract[name]
        assert c['candidate_binding'] == hull and c['material_slots'] == ['Material_0']
        assert c['atlas_size'] == [512, 256] and c['submeshes'] == 1
        expected = {'_BaseColor': [1, 1, 1, 1], '_Metallic': 1, '_Smoothness': .4,
                    '_UseMetallicRoughnessMap': 1, '_UseNormalMap': 1,
                    '_BumpScale': 1, '_EmissionColor': [2, 2, 2, 1]}
        assert c['PreviewStudio'] == expected, name + ' material factors'
        texture_records, envs = [], []
        for role, prop in MAPS.items():
            relative = c['textures'][role]
            data = source(relative)
            assert data[:8] == b'\x89PNG\r\n\x1a\n' and struct.unpack('>II', data[16:24]) == (512, 256)
            path = DEST / relative
            outputs[path] = data
            color = role in ('BaseColor', 'Emission')
            outputs[Path(str(path) + '.meta')] = text(
                f'fileFormatVersion: 2\nguid: {guid(path)}\nTextureImporter:\n'
                '  externalObjects: {}\n  serializedVersion: 13\n  mipmaps:\n    enableMipMap: 1\n'
                f'  isReadable: 0\n  sRGBTexture: {int(color)}\n  maxTextureSize: 512\n'
                f'  textureType: {1 if role == "Normal" else 0}\n  textureCompression: 1\n'
                '  alphaIsTransparency: 0\n  textureSettings:\n    serializedVersion: 2\n'
                '    filterMode: 2\n    aniso: 1\n    mipBias: 0\n    wrapU: 1\n    wrapV: 1\n    wrapW: 1\n')
            envs.append(f'    - {prop}:\n        m_Texture: {{fileID: 2800000, guid: {guid(path)}, type: 3}}\n'
                        '        m_Scale: {x: 1, y: 1}\n        m_Offset: {x: 0, y: 0}\n')
            texture_records.append({'role': role, 'property': prop, 'source': relative,
                                    'sha256': digest(data), 'guid': guid(path),
                                    'dimensions': [512, 256], 'sRGB': color,
                                    'normalMap': role == 'Normal', 'mipmaps': True})
        material_path = DEST / 'Materials' / (name + '.mat')
        outputs[material_path] = text(
            '%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!21 &2100000\nMaterial:\n'
            f'  serializedVersion: 8\n  m_ObjectHideFlags: 0\n  m_Name: {name}_OriginalCandidate\n'
            f'  m_Shader: {{fileID: 4800000, guid: {shader_guid}, type: 3}}\n'
            '  m_ValidKeywords: []\n  m_InvalidKeywords: []\n  m_LightmapFlags: 2\n'
            '  m_EnableInstancingVariants: 0\n  m_DoubleSidedGI: 0\n  m_CustomRenderQueue: -1\n'
            '  stringTagMap: {}\n  disabledShaderPasses: []\n  m_SavedProperties:\n'
            '    serializedVersion: 3\n    m_TexEnvs:\n' + ''.join(envs) +
            '    m_Ints: []\n    m_Floats:\n    - _Metallic: 1\n    - _Smoothness: 0.4\n'
            '    - _UseMetallicRoughnessMap: 1\n    - _UseNormalMap: 1\n    - _BumpScale: 1\n'
            '    m_Colors:\n    - _BaseColor: {r: 1, g: 1, b: 1, a: 1}\n'
            '    - _EmissionColor: {r: 2, g: 2, b: 2, a: 1}\n')
        outputs[Path(str(material_path) + '.meta')] = meta(material_path, 'NativeFormatImporter')
        asset = next(a for a in manifest['assets'] if a['name'] == name + '_Runtime_LOD0')
        assert asset['candidate_binding'] == hull and len(asset['meshes']) == 1
        mesh = asset['meshes'][0]
        assert mesh['triangles'] == triangles and mesh['materials'] == ['Material_0']
        path = DEST / (name + '_Runtime_LOD0.fbx')
        data = source(asset['fbx']);outputs[path] = data
        remap = ('  externalObjects:\n  - first:\n      type: UnityEngine:Material\n'
                 '      assembly: UnityEngine.CoreModule\n      name: Material_0\n'
                 '    second: {fileID: 2100000, guid: ' + guid(material_path) + ', type: 2}')
        model_meta = re.sub(r'(?m)^guid: [0-9a-f]{32}$', 'guid: ' + guid(path), template)
        outputs[Path(str(path) + '.meta')] = text(model_meta.replace('  externalObjects: {}', remap)
                                                       .replace('materialImportMode: 0', 'materialImportMode: 2'))
        records.append({'hullVersionId': hull, 'candidate': True, 'resource': f'ArtDemo/Production02/Hulls/{name}_Runtime_LOD0',
                        'source': asset['fbx'], 'sha256': digest(data), 'guid': guid(path),
                        'triangles': triangles, 'submeshes': 1, 'nominalArtLengthMetres': length,
                        'materialResource': f'ArtDemo/Production02/Hulls/Materials/{name}',
                        'materialGuid': guid(material_path), 'textures': texture_records})
    inventory = {'schema': 2, 'sourcePin': SOURCE_PIN, 'sourceManifestSha256': digest(manifest_bytes),
                 'sourceMaterialContractSha256': digest(contract_bytes), 'textHashFormat': 'UTF-8 with LF newlines',
                 'shader': 'HobbyDS9/ArtDemo/PreviewStudio', 'shaderGuid': shader_guid,
                 'bindings': records, 'status': 'Original candidate exteriors; native/owner artistic acceptance pending',
                 'previousRuntimeSlice': 'Production01 superseded; v1 source packet remains outside Assets',
                 'nativePerformance': 'Unmeasured; one slot is not an FPS or actual residency result'}
    outputs[DEST / 'receiving-inventory.json'] = text(json.dumps(inventory, indent=2) + '\n')
    outputs[DEST / 'CREDITS.txt'] = text('HobbyDS9 original procedural hull/atlas candidates; no borrowed texture pixels.\n'
        'Source pin: ' + SOURCE_PIN + '\nSources/provenance: art/asset-production-01/hulls-runtime-v2.\n'
        'Same original v1 geometry; one atlas material. Artistic approval and native acceptance pending.\n'
        'Approved external replacements remain separately unreceived; existing legacy credits remain unchanged.\n')
    for path in list(outputs):
        if path.suffix in ('.json', '.txt'):
            outputs[Path(str(path) + '.meta')] = meta(path, 'TextScriptImporter' if path.suffix == '.txt' else 'DefaultImporter')
    for path in (DEST.parent, DEST, DEST / 'Materials', DEST / 'Textures'):
        outputs[Path(str(path) + '.meta')] = meta(path, folder=True)
    return outputs


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check', action='store_true');args = parser.parse_args()
    outputs, seen = payloads(), {}
    for path in ASSETS.rglob('*.meta'):
        if path in outputs:continue
        found = re.search(r'(?m)^guid: ([0-9a-f]{32})$', path.read_text())
        if found:seen[found[1]] = path
    for path, data in outputs.items():
        if path.suffix == '.meta':
            key = re.search(rb'(?m)^guid: ([0-9a-f]{32})$', data)[1].decode()
            assert key not in seen, f'GUID collision: {path} / {seen.get(key)}';seen[key] = path
    for path, data in outputs.items():
        if path.exists():
            actual = path.read_bytes() if path.suffix in ('.fbx', '.png') else canonical(path)
            assert actual == data, f'Refuse changed receiving file: {path}'
        else:
            assert not args.check, f'Missing receiving file: {path}'
            path.parent.mkdir(parents=True, exist_ok=True);path.write_bytes(data)
    print(f'PASS: {len(outputs)} receiving files; exact two candidates/one slot/eight atlas maps, channels, hashes, GUIDs')


if __name__ == '__main__':main()
