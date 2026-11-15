"""Independent raw GLB reassembly comparison; stdlib only, no generator import."""
import collections
import itertools
import json
import math
import struct
import sys
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
pack=ROOT/'art/asset-production-01/cruiser-modules-v1'
interface=json.loads((pack/'art-interface.json').read_text())
failures=[]

def read(path,extra=(0,0,0)):
    raw=path.read_bytes();offset=12
    while offset<len(raw):
        size,kind=struct.unpack_from('<II',raw,offset);chunk=raw[offset+8:offset+8+size]
        if kind==0x4e4f534a:doc=json.loads(chunk)
        elif kind==0x004e4942:binary=chunk
        offset+=size+8
    def accessor(index):
        a=doc['accessors'][index];view=doc['bufferViews'][a['bufferView']]
        fmt,width={5123:('H',2),5125:('I',4),5126:('f',4)}[a['componentType']]
        n={'SCALAR':1,'VEC2':2,'VEC3':3,'VEC4':4}[a['type']]
        start=view.get('byteOffset',0)+a.get('byteOffset',0);stride=view.get('byteStride',width*n)
        return [struct.unpack_from('<'+fmt*n,binary,start+i*stride) for i in range(a['count'])]
    triangles=[]
    for node in doc['nodes']:
        if 'mesh' not in node:continue
        assert node.get('scale',[1,1,1])==[1,1,1]
        assert node.get('rotation',[0,0,0,1])==[0,0,0,1]
        assert 'matrix' not in node
        t=node.get('translation',[0,0,0])
        for p in doc['meshes'][node['mesh']]['primitives']:
            attrs={key:accessor(value) for key,value in p['attributes'].items()}
            if 'TANGENT' not in attrs:
                failures.append('Missing tangent basis: '+path.name)
            elif any(not all(math.isfinite(v) for v in tangent) or
                     abs(sum(v*v for v in tangent[:3])-1)>.01 or abs(abs(tangent[3])-1)>.01
                     for tangent in attrs['TANGENT']):
                failures.append('Invalid tangent basis: '+path.name)
            indices=[i[0] for i in accessor(p['indices'])]
            for k in range(0,len(indices),3):
                triangles.append([(tuple(attrs['POSITION'][i][a]+t[a]+extra[a] for a in range(3)),
                                   attrs['TEXCOORD_0'][i]) for i in indices[k:k+3]])
    return triangles

source=read(ROOT/interface['source_complete_hull'])
points={}
for triangle in source:
    for point,_ in triangle:
        if point not in points:points[point]=len(points)
grid=collections.defaultdict(list)
for point,index in points.items():grid[tuple(math.floor(v/.001) for v in point)].append((point,index))
cache={};worst=0

def source_index(point):
    global worst
    if point in cache:return cache[point]
    cell=tuple(math.floor(v/.001) for v in point)
    candidates=[p for delta in itertools.product([-1,0,1],repeat=3)
                for p in grid.get(tuple(cell[a]+delta[a] for a in range(3)),[])]
    assert candidates,'No source vertex near module vertex'
    original,index=min(candidates,key=lambda p:sum((point[a]-p[0][a])**2 for a in range(3)))
    error=math.sqrt(sum((point[a]-original[a])**2 for a in range(3)))
    worst=max(worst,error)
    assert error<.0002,'Reassembly position deviates by '+str(error)+' metres'
    cache[point]=index
    return index

def signature(triangles,uv=False):
    return collections.Counter(tuple(sorted((source_index(point),tuple(round(v,5) for v in tex)) if uv
        else source_index(point) for point,tex in triangle)) for triangle in triangles)

proof=read(pack/'Models/OriginalCruiser_ReassembledProof.glb')
reassembled=[];part_counts={}
for role,socket in interface['parts'].items():
    x,y,z=socket['parent_space_translation_blender_metres']
    triangles=read(pack/'Models'/('OriginalCruiser_'+role+'.glb'),(x,z,-y))
    part_counts[role]=len(triangles);reassembled.extend(triangles)
for label,triangles in [('assembly proof',proof),('standalone parts + interface transforms',reassembled)]:
    if signature(triangles)!=signature(source):failures.append('Changed topology: '+label)
    if signature(triangles,True)!=signature(source,True):failures.append('Changed atlas UVs: '+label)
report={'passed':not failures,'failures':failures,'source_triangles':len(source),
    'part_triangles':part_counts,'sum_part_triangles':sum(part_counts.values()),
    'assembly_proof_triangles':len(proof),'max_reassembly_position_error_metres':worst,
    'checks':'Raw GLB node transforms, full triangle multiset and UV preservation, standalone modules plus explicit art interface',
    'not_claimed':'Unity importer axes, gameplay equipment/socket mapping, external Galaxy compatibility or native visual acceptance'}
print(json.dumps(report,indent=2));sys.exit(1 if failures else 0)
