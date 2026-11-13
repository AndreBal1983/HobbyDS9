"""Independent receiving audit: topology preservation, material routing, tangents and UV basis.

Stdlib only. Use alongside the existing byte/geometry verifier, not instead of native import.
"""
import collections
import hashlib
import json
import math
import struct
import sys
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
pack=Path(sys.argv[1]).resolve()
m=json.loads((pack/'manifest.json').read_text())
failures=[]

def read(path):
    raw=path.read_bytes();offset=12;doc=None;binary=None
    while offset<len(raw):
        length,kind=struct.unpack_from('<II',raw,offset)
        data=raw[offset+8:offset+8+length]
        if kind==0x4e4f534a:doc=json.loads(data)
        elif kind==0x004e4942:binary=data
        offset+=length+8
    def accessor(index):
        a=doc['accessors'][index];v=doc['bufferViews'][a['bufferView']]
        fmt,size={5123:('H',2),5125:('I',4),5126:('f',4)}[a['componentType']]
        width={'SCALAR':1,'VEC2':2,'VEC3':3,'VEC4':4}[a['type']]
        start=v.get('byteOffset',0)+a.get('byteOffset',0)
        stride=v.get('byteStride',width*size)
        return [struct.unpack_from('<'+fmt*width,binary,start+i*stride) for i in range(a['count'])]
    rows=[]
    for mesh in doc['meshes']:
        for primitive in mesh['primitives']:
            attrs={key:accessor(value) for key,value in primitive['attributes'].items()}
            rows.append((doc['materials'][primitive['material']]['name'],attrs,
                         [v[0] for v in accessor(primitive['indices'])]))
    return doc,rows

def triangles(rows,uv=False):
    result=[]
    for role,attrs,indices in rows:
        for offset in range(0,len(indices),3):
            corners=[]
            for index in indices[offset:offset+3]:
                values=attrs['POSITION'][index]+(attrs['TEXCOORD_0'][index] if uv else ())
                corners.append(tuple(round(v,5) for v in values))
            result.append(tuple(sorted(corners)))
    return collections.Counter(result)

reports=[]
for asset in m['assets']:
    doc,rows=read(pack/asset['glb'])
    name=asset['name']
    original_name=name.replace('_Runtime','').replace('_ReceivingWitness','')
    original=ROOT/'art/asset-production-01'/(m['phase']+'-v1')/'Models'/(original_name+'.glb')
    _,before=read(original)
    identical=triangles(rows)==triangles(before)
    if not identical:failures.append('Changed geometry '+name)
    if len(rows)!=1:failures.append('Expected one primitive '+name)
    winding_bad=0
    tangent_count=0
    for role,attrs,indices in rows:
        if m['phase']=='hulls' and role!='Material_0':failures.append('Slot name '+name)
        for tangent in attrs.get('TANGENT',[]):
            tangent_count+=1
            if abs(sum(v*v for v in tangent[:3])-1)>.01 or abs(abs(tangent[3])-1)>.01:
                failures.append('Tangent '+name);break
        for offset in range(0,len(indices),3):
            ids=indices[offset:offset+3]
            a,b,c=[attrs['POSITION'][i] for i in ids]
            u,v=[b[k]-a[k] for k in range(3)],[c[k]-a[k] for k in range(3)]
            cross=(u[1]*v[2]-u[2]*v[1],u[2]*v[0]-u[0]*v[2],u[0]*v[1]-u[1]*v[0])
            normal=[sum(attrs['NORMAL'][i][k] for i in ids) for k in range(3)]
            winding_bad+=sum(cross[k]*normal[k] for k in range(3))<=0
    if winding_bad:failures.append('Normal/winding disagreement '+name)
    report={'asset':name,'same_triangle_geometry_as_v1':identical,'primitives':len(rows),
            'normal_winding_disagreements':winding_bad,'tangent_vertices':tangent_count}
    if m['phase']=='system':
        same_uv=triangles(rows,True)==triangles(before,True)
        if not same_uv:failures.append('Witness unexpectedly changed v1 UVs '+name)
        errors={'RH_gltf_direct':0,'X_reflected_receiver_candidate':0}
        for _,attrs,_ in rows:
            for pos,uv in zip(attrs['POSITION'],attrs['TEXCOORD_0']):
                radius=math.sqrt(sum(v*v for v in pos))
                if abs(pos[1]/radius)>.9999:continue
                for key,x in [('RH_gltf_direct',pos[0]),('X_reflected_receiver_candidate',-pos[0])]:
                    wanted=math.atan2(pos[2],x)/math.tau%1
                    error=abs((uv[0]-wanted+.5)%1-.5)
                    errors[key]=max(errors[key],error)
        report.update({'same_UV_as_v1':same_uv,'max_longitude_U_error':errors,
            'interpretation':'RH glTF differs from the Unity fallback basis until handedness/root conversion is accounted for. '
            'X-reflected candidate matches the stated basis. This is not a measured native FBX transform or an established v1 defect.'})
    else:
        if not tangent_count:failures.append('Missing normal-map tangents '+name)
        contract=json.loads((pack/'material-contract.json').read_text())[name.split('_')[0]]
        roles=contract['original_role_tiles']
        geometry_roles=collections.defaultdict(set)
        for role,attrs,indices in before:
            for k in range(0,len(indices),3):
                key=tuple(sorted(tuple(round(v,5) for v in attrs['POSITION'][i]) for i in indices[k:k+3]))
                geometry_roles[key].add(role)
        wrong_tile=0
        for _,attrs,indices in rows:
            for k in range(0,len(indices),3):
                ids=indices[k:k+3]
                key=tuple(sorted(tuple(round(v,5) for v in attrs['POSITION'][i]) for i in ids))
                expected=geometry_roles[key]
                for i in ids:
                    u,t=attrs['TEXCOORD_0'][i]
                    v=1-t  # glTF image V convention is opposite the authoring/Unity UV convention.
                    tile=int(v*2)*4+int(u*4)
                    if not 0<=tile<len(roles) or roles[tile] not in expected:wrong_tile+=1
        report['atlas_corners_routed_to_wrong_original_role']=wrong_tile
        if wrong_tile:failures.append('Atlas role routing '+name)
    reports.append(report)
print(json.dumps({'pack':pack.name,'passed':not failures,'assets':reports,'failures':failures,
    'not_executed':['Unity importer/handedness measurement','native shader/pixels/performance']},indent=2))
sys.exit(1 if failures else 0)
