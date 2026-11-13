"""Independently inspect exported GLB buffers and exact delivery bytes; stdlib only."""
import hashlib
import json
import math
import struct
import sys
from pathlib import Path

pack = Path(sys.argv[1]).resolve()
manifest = json.loads((pack / 'manifest.json').read_text(encoding='utf-8'))
failures, results = [], []


def check(ok, message):
    if not ok:
        failures.append(message)


for item in manifest['files']:
    p = pack / item['path']
    check(p.is_file(), 'Missing ' + item['path'])
    if p.is_file():
        check(p.stat().st_size == item['bytes'], 'Size ' + item['path'])
        check(hashlib.sha256(p.read_bytes()).hexdigest() == item['sha256'], 'Hash ' + item['path'])

formats = {5120: ('b', 1), 5121: ('B', 1), 5122: ('h', 2), 5123: ('H', 2),
           5125: ('I', 4), 5126: ('f', 4)}
widths = {'SCALAR': 1, 'VEC2': 2, 'VEC3': 3, 'VEC4': 4, 'MAT4': 16}
for asset in manifest['assets']:
    data = (pack / asset['glb']).read_bytes()
    magic, version, size = struct.unpack_from('<4sII', data)
    check((magic, version, size) == (b'glTF', 2, len(data)), 'GLB header ' + asset['name'])
    offset, document, binary = 12, None, None
    while offset < len(data):
        length, kind = struct.unpack_from('<II', data, offset)
        chunk = data[offset + 8:offset + 8 + length]
        if kind == 0x4E4F534A:
            document = json.loads(chunk)
        if kind == 0x004E4942:
            binary = chunk
        offset += length + 8
    check(document is not None and binary is not None, 'GLB chunks ' + asset['name'])
    check(all('uri' not in v for v in document.get('buffers', [])), 'External buffer ' + asset['name'])
    check(all('uri' not in v for v in document.get('images', [])), 'External image ' + asset['name'])
    if manifest.get('phase') == 'hulls':
        check(len(document.get('materials', [])) <= 8, 'Hull material budget ' + asset['name'])

    def accessor(index):
        a = document['accessors'][index]
        view = document['bufferViews'][a['bufferView']]
        fmt, scalar = formats[a['componentType']]
        width = widths[a['type']]
        stride = view.get('byteStride', width * scalar)
        start = view.get('byteOffset', 0) + a.get('byteOffset', 0)
        return [struct.unpack_from('<' + fmt * width, binary, start + i * stride)
                for i in range(a['count'])]

    triangles, vertices, degenerate = 0, 0, 0
    for mesh in document['meshes']:
        for primitive in mesh['primitives']:
            check(primitive.get('mode', 4) == 4, 'Nontriangle primitive ' + asset['name'])
            attr = primitive['attributes']
            check('NORMAL' in attr and 'TEXCOORD_0' in attr, 'Normals/UV missing ' + asset['name'])
            pos, normals, uv = [accessor(attr[a]) for a in ['POSITION', 'NORMAL', 'TEXCOORD_0']]
            vertices += len(pos)
            check(len(pos) == len(normals) == len(uv), 'Attribute lengths ' + asset['name'])
            check(all(math.isfinite(v) for a in [pos, normals, uv] for row in a for v in row),
                  'Nonfinite attribute ' + asset['name'])
            check(all(abs(sum(v*v for v in n) - 1) < .01 for n in normals),
                  'Nonunit normal ' + asset['name'])
            indices = [i[0] for i in accessor(primitive['indices'])]
            check(len(indices) % 3 == 0 and all(0 <= i < len(pos) for i in indices),
                  'Triangle indices ' + asset['name'])
            triangles += len(indices) // 3
            for i in range(0, len(indices), 3):
                a, b, c = [pos[k] for k in indices[i:i+3]]
                u, v = [b[k]-a[k] for k in range(3)], [c[k]-a[k] for k in range(3)]
                cross = [u[1]*v[2]-u[2]*v[1], u[2]*v[0]-u[0]*v[2], u[0]*v[1]-u[1]*v[0]]
                degenerate += sum(x*x for x in cross) < 1e-16
    expected = sum(m['triangles'] for m in asset['meshes'])
    check(triangles == expected, 'Export triangle mismatch ' + asset['name'])
    check(degenerate == 0, 'Degenerate triangles ' + asset['name'])
    check(triangles <= (35000 if manifest.get('phase') == 'hulls' else 10000),
          'Triangle budget ' + asset['name'])
    results.append({'asset': asset['name'], 'triangles': triangles, 'exported_vertices': vertices,
                    'degenerate_triangles': degenerate, 'embedded_images': len(document.get('images', []))})

for item in manifest['texture_sources']:
    original = Path(__file__).resolve().parents[2] / item['source_repo_path']
    check(hashlib.sha256(original.read_bytes()).hexdigest() == item['sha256'],
          'Original texture source ' + item['path'])

report = {'pack': pack.name, 'checks': 'manifest byte hashes; source copies; GLB structure/dependency closure; '
          'buffer indices, finite UVs/positions/normals, normal length, triangle area/count/budget',
          'assets': results, 'failures': failures, 'passed': not failures,
          'not_executed': ['Unity importer', 'URP shader compilation/render', 'native workflow/visual acceptance']}
print(json.dumps(report, indent=2))
sys.exit(1 if failures else 0)
