"""Independent CPU checks and channel repack; does not invoke Unity or a renderer."""
from pathlib import Path
import hashlib
import io
import json
import math
import struct
import sys
import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
PACK = Path(sys.argv[1]) if len(sys.argv) > 1 else ROOT/'artifacts/galaxy-v2-prepared'
manifest = json.loads((PACK/'derivative-manifest.json').read_text(encoding='utf-8'))
for item in manifest['textureExtraction']:
    file = PACK/item['file']; data = file.read_bytes()
    assert len(data) == item['bytes'] and hashlib.sha256(data).hexdigest() == item['sha256'], file

# glTF MR uses linear G=roughness/B=metallic; URP uses R=metal/A=smooth.
mr = np.asarray(Image.open(PACK/'Textures/MetallicRoughness.jpg').convert('RGB'))
packed = np.zeros((*mr.shape[:2], 4), dtype=np.uint8)
packed[:, :, 0] = mr[:, :, 2]; packed[:, :, 3] = 255-mr[:, :, 1]
target = PACK/'Textures/MetallicSmoothness.png'
Image.fromarray(packed).save(target)
roundtrip = np.asarray(Image.open(target))
assert np.array_equal(roundtrip[:, :, 0], mr[:, :, 2])
assert np.array_equal(roundtrip[:, :, 3], 255-mr[:, :, 1])

data = (PACK/'Validation/GalaxyMeshyV2_LOD0.glb').read_bytes()
assert data[:4] == b'glTF' and struct.unpack_from('<I', data, 8)[0] == len(data)
n = struct.unpack_from('<I', data, 12)[0]; g = json.loads(data[20:20+n]); binary = data[28+n:]
assert not g.get('extensionsRequired') and len(g['meshes']) == 1 and len(g['materials']) == 1
assert all('uri' not in image for image in g['images'])


def accessor(index):
    a = g['accessors'][index]; view = g['bufferViews'][a['bufferView']]
    dtype = {5126:'<f4', 5125:'<u4', 5123:'<u2'}[a['componentType']]
    width = {'SCALAR':1, 'VEC2':2, 'VEC3':3, 'VEC4':4}[a['type']]
    assert 'byteStride' not in view
    return np.frombuffer(binary, dtype=dtype, count=a['count']*width,
        offset=view.get('byteOffset', 0)+a.get('byteOffset', 0)).reshape(-1, width)


worlds = {}
def visit(index, parent):
    node = g['nodes'][index]
    if 'matrix' in node: local = np.array(node['matrix']).reshape(4, 4).T
    else:
        x,y,z,w = node.get('rotation', [0,0,0,1])
        local = np.eye(4)
        local[:3,:3] = [[1-2*(y*y+z*z),2*(x*y-z*w),2*(x*z+y*w)],
            [2*(x*y+z*w),1-2*(x*x+z*z),2*(y*z-x*w)],
            [2*(x*z-y*w),2*(y*z+x*w),1-2*(x*x+y*y)]]
        local[:3,:3] = local[:3,:3] @ np.diag(node.get('scale',[1,1,1]))
        local[:3,3] = node.get('translation',[0,0,0])
    world = parent @ local; worlds[node['name']] = world
    if 'mesh' in node:
        primitive = g['meshes'][node['mesh']]['primitives'][0]
        attrs = primitive['attributes']
        assert {'POSITION','NORMAL','TEXCOORD_0','TANGENT'}.issubset(attrs)
        positions = accessor(attrs['POSITION']); normals = accessor(attrs['NORMAL']); uv = accessor(attrs['TEXCOORD_0'])
        tangents = accessor(attrs['TANGENT']); indices = accessor(primitive['indices'])
        assert len(indices)//3 == 60000 and int(indices.max()) < len(positions)
        assert len(normals) == len(uv) == len(tangents) == len(positions)
        assert all(np.isfinite(a).all() for a in [positions,normals,uv,tangents])
        assert np.allclose(np.linalg.norm(normals,axis=1),1,atol=.02)
        assert np.allclose(np.linalg.norm(tangents[:,:3],axis=1),1,atol=.02)
        assert np.max(np.abs(np.sum(normals*tangents[:,:3],axis=1)))<.02
        assert np.allclose(np.abs(tangents[:,3]),1)
        assert uv.min() >= -.001 and uv.max() <= 1.001
        pos = (world @ np.c_[positions,np.ones(len(positions))].T).T[:,:3]
        results['glbLod0'] = {'triangles':len(indices)//3,'exportVertices':len(pos),'sizeXYZMetres':np.ptp(pos,axis=0).tolist(),
            'uvRange':[float(uv.min()),float(uv.max())], 'normalAndTangentsFiniteUnit':True}
        assert math.isclose(np.ptp(pos,axis=0)[2],620,rel_tol=.015)
    for child in node.get('children',[]): visit(child,world)


results = {}
for node in g['scenes'][g.get('scene',0)]['nodes']: visit(node,np.eye(4))
bow = worlds['Axis_Bow'][:3,3]; up = worlds['Axis_Up'][:3,3]
assert bow[2] > 0 and np.abs(bow[:2]).sum() < .01
assert up[1] > 0 and abs(up[0])+abs(up[2]) < .01
material = g['materials'][0]; assert material['doubleSided']
roles = {'BaseColor':material['pbrMetallicRoughness']['baseColorTexture'],
    'MetallicRoughness':material['pbrMetallicRoughness']['metallicRoughnessTexture'],
    'Normal':material['normalTexture'],'Emission':material['emissiveTexture']}
map_checks = []
for role, info in roles.items():
    image = g['images'][g['textures'][info['index']]['source']]
    view = g['bufferViews'][image['bufferView']]
    pixels = np.asarray(Image.open(io.BytesIO(binary[view.get('byteOffset',0):view.get('byteOffset',0)+view['byteLength']])).convert('RGB'))
    original = np.asarray(Image.open(PACK/f'Textures/{role}.jpg').convert('RGB'))
    # The diagnostic GLB must not silently retexture, downsample or lose a map.
    assert np.array_equal(pixels,original), role
    map_checks.append({'role':role,'size':list(pixels.shape[1::-1]),'diagnosticGlbPixelsEqualSource':True})

results.update({'allPassed':True,'scope':'CPU GLB/export/texture data only; Unity FBX import and textured render pending',
    'engineBow':bow.tolist(),'engineUp':up.tolist(),'maps':map_checks,
    'metallicSmoothness':{'file':target.relative_to(PACK).as_posix(),'size':list(roundtrip.shape[1::-1]),
        'sha256':hashlib.sha256(target.read_bytes()).hexdigest(),'channels':'R=MR.B;A=255-MR.G;G=B=0;linear'}})
(PACK/'Validation/cpu-validation.json').write_text(json.dumps(results,indent=2)+'\n',encoding='utf-8')
print('Galaxy v2 CPU GLB, +Z/+Y axes, metre bounds, UV/normals/tangents and all4maps/channel checks passed.')
