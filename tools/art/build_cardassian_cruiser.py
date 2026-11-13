"""Original CPU-only Cardassian-style prototype. No Blender/Unity/GPU or borrowed mesh."""
from pathlib import Path
from collections import defaultdict
import hashlib
import json
import math
import struct
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "art/ships/cardassian-cruiser-v1"
MATERIALS = [
    {"name": "CARD_Ochre", "color": [.52, .34, .16], "metallic": .3, "roughness": .55},
    {"name": "CARD_Plate_Gold", "color": [.73, .55, .29], "metallic": .25, "roughness": .48},
    {"name": "CARD_Inset", "color": [.16, .115, .065], "metallic": .2, "roughness": .7},
    {"name": "CARD_RedBrown", "color": [.34, .105, .065], "metallic": .2, "roughness": .55},
    {"name": "CARD_Drive_Amber", "color": [1, .26, .035], "metallic": 0, "roughness": .3, "emission": [1, .16, .015]},
    {"name": "CARD_Windows", "color": [.58, .73, .8], "metallic": 0, "roughness": .4, "emission": [.22, .32, .4]},
]
ANCHORS = [
    {"name": "Axis_Bow", "position": [0, 0, 240], "rotation": [0, 0, 0, 1]},
    {"name": "Axis_Up", "position": [0, 70, 0], "rotation": [0, 0, 0, 1]},
    {"name": "Effect_Beam_Fore", "position": [0, 6, 220.5], "rotation": [0, 0, 0, 1]},
    {"name": "Effect_Ordnance_Fore", "position": [0, -6, 218], "rotation": [0, 0, 0, 1]},
    {"name": "Effect_Drive_Port", "position": [-22, 0, -252], "rotation": [0, 1, 0, 0]},
    {"name": "Effect_Drive_Starboard", "position": [22, 0, -252], "rotation": [0, 1, 0, 0]},
    {"name": "Impact_Center", "position": [0, 12, 60], "rotation": [0, 0, 0, 1]},
    {"name": "Camera_Focus", "position": [0, 7, 0], "rotation": [0, 0, 0, 1]},
]


def sub(a, b): return tuple(x-y for x, y in zip(a, b))
def cross(a, b): return (a[1]*b[2]-a[2]*b[1], a[2]*b[0]-a[0]*b[2], a[0]*b[1]-a[1]*b[0])
def dot(a, b): return sum(x*y for x, y in zip(a, b))


def make_model(lod):
    faces = []
    def tri(a, b, c, center, material, part):
        normal = cross(sub(b, a), sub(c, a)); length = math.sqrt(dot(normal, normal))
        if length < 1e-8: return
        mid = tuple((a[i]+b[i]+c[i])/3 for i in range(3))
        if dot(normal, sub(mid, center)) < 0:
            b, c = c, b; normal = tuple(-v for v in normal)
        faces.append((part, material, (a, b, c), tuple(v/length for v in normal)))
    def prism(poly, bottom, top, material=0, part="Hull"):
        center = (sum(p[0] for p in poly)/len(poly), (bottom+top)/2, sum(p[1] for p in poly)/len(poly))
        upper = [(x, top, z) for x, z in poly]; lower = [(x, bottom, z) for x, z in poly]
        for i in range(1, len(poly)-1):
            tri(upper[0], upper[i], upper[i+1], center, material, part)
            tri(lower[0], lower[i+1], lower[i], center, material, part)
        for i in range(len(poly)):
            j = (i+1) % len(poly)
            tri(upper[i], lower[i], lower[j], center, material, part)
            tri(upper[i], lower[j], upper[j], center, material, part)
    def box(x, y, z, w, h, length, material=0, part="Details"):
        prism([(x-w/2,z-length/2),(x+w/2,z-length/2),(x+w/2,z+length/2),(x-w/2,z+length/2)], y-h/2, y+h/2, material, part)
    def oval(center, radius, material=0, part="Hull"):
        segments, rings = [(40,16),(24,10),(12,6)][lod]
        def point(r, s):
            a = math.pi*r/rings; b = 2*math.pi*s/segments
            return (center[0]+radius[0]*math.sin(a)*math.cos(b), center[1]+radius[1]*math.cos(a), center[2]+radius[2]*math.sin(a)*math.sin(b))
        for r in range(rings):
            for s in range(segments):
                a,b,c,d = point(r,s),point(r,s+1),point(r+1,s+1),point(r+1,s)
                tri(a,b,c,center,material,part);tri(a,c,d,center,material,part)
    prism([(-32,-228),(32,-228),(45,-170),(38,110),(26,160),(-26,160),(-38,110),(-45,-170)], -13, 13, 0, "Long_Spine")
    prism([(-180,95),(-165,145),(-60,175),(60,175),(165,145),(180,95),(70,65),(-70,65)], -12, 12, 0, "Broad_Shoulders")
    oval((0,4,170),(58,21,50),1,"Forward_Command_Head")
    oval((0,27,104),(47,17,56),1,"Layered_Bridge")
    box(0,18,-60,48,13,200,1,"Dorsal_Spine")
    box(0,-20,60,44,17,110,2,"Ventral_Keel")
    for sign in [-1,1]:
        box(sign*22,-1,-240,12,14,40,0,"Aft_Fork")
        box(sign*22,-1,-252,9,8,2,4,"Drive")
        prism([(sign*x,z)for x,z in [(65,102),(148,105),(139,139),(64,155)]],12.3,14.3,1,"Shoulder_Plates")
        box(sign*105,15.2,118,43,2,6,3,"Inset_Bands")
        box(sign*111,-2,151,28,10,9,2,"Shoulder_Recess")
        for i in range([16,8,0][lod]):
            box(sign*38,12,-146+i*15,1.5,2.2,4,5,"Windows")
        for i in range([8,4,2][lod]):
            box(sign*64,18,144+i*5,2.5,2,2,5,"Forward_Windows")
    box(0,6,219,20,9,2,2,"Forward_Aperture")
    box(0,6,220.2,13,5,1.5,4,"Emitter")
    if lod < 2:
        for z in [-170,-145,-120,-95,-70,-45,-20]: box(0,25,z,24,2,3,2,"Spine_Ribs")
        for x in [-26,0,26]: box(x,45,99,12,3,30,0,"Bridge_Caps")
    return faces


def export_obj(faces, file):
    lines = ["# Original procedural Cardassian-style prototype; metres, +Y up, +Z forward", "mtllib cardassian-cruiser.mtl"]
    current = None
    for index, (part, material, vertices, normal) in enumerate(faces):
        if current != (part, material):
            lines += ["g " + part, "usemtl " + MATERIALS[material]["name"]];current = (part, material)
        for v in vertices: lines.append("v " + " ".join(f"{x:.7f}"for x in v))
        for v in vertices: lines.append(f"vt {(v[0]+180)/360:.7f} {(v[2]+260)/480:.7f}")
        for _ in vertices: lines.append("vn " + " ".join(f"{x:.7f}"for x in normal))
        lines.append("f " + " ".join(f"{n}/{n}/{n}"for n in range(index*3+1,index*3+4)))
    file.write_text("\n".join(lines)+"\n",encoding="utf-8")


def export_glb(faces, file):
    data = bytearray();views=[];accessors=[];nodes=[];meshes=[]
    def accessor(values, width, kind, bounds=False):
        while len(data)%4: data.append(0)
        start = len(data);flat = [x for v in values for x in (v if isinstance(v,tuple)else(v,))]
        data.extend(struct.pack("<"+kind*len(flat),*flat));views.append({"buffer":0,"byteOffset":start,"byteLength":len(data)-start})
        a={"bufferView":len(views)-1,"componentType":5126 if kind=='f' else 5125,"count":len(values),"type":{1:"SCALAR",2:"VEC2",3:"VEC3"}[width]}
        if bounds:a.update(min=[min(v[i]for v in values)for i in range(width)],max=[max(v[i]for v in values)for i in range(width)])
        accessors.append(a);return len(accessors)-1
    groups=defaultdict(list)
    for face in faces:groups[(face[0],face[1])].append(face)
    for (part,material),tris in groups.items():
        positions=[v for _,_,vs,_ in tris for v in vs];normals=[n for _,_,vs,n in tris for v in vs];uvs=[((v[0]+180)/360,(v[2]+260)/480)for v in positions]
        attrs={"POSITION":accessor(positions,3,'f',True),"NORMAL":accessor(normals,3,'f'),"TEXCOORD_0":accessor(uvs,2,'f')}
        meshes.append({"name":part,"primitives":[{"attributes":attrs,"indices":accessor(list(range(len(positions))),1,'I'),"material":material}]});nodes.append({"name":part,"mesh":len(meshes)-1})
    for a in ANCHORS:nodes.append({"name":a['name'],"translation":a['position'],"rotation":a['rotation']})
    materials=[{"name":m['name'],"pbrMetallicRoughness":{"baseColorFactor":m['color']+[1],"metallicFactor":m['metallic'],"roughnessFactor":m['roughness']},**({"emissiveFactor":m['emission']}if'emission'in m else{})}for m in MATERIALS]
    doc={"asset":{"version":"2.0","generator":"HobbyDS9 original CPU geometry"},"scene":0,"scenes":[{"nodes":list(range(len(nodes)))}],"nodes":nodes,"meshes":meshes,"materials":materials,"buffers":[{"byteLength":len(data)}],"bufferViews":views,"accessors":accessors}
    encoded=json.dumps(doc,separators=(',',':')).encode();encoded+=b' '*((-len(encoded))%4);data+=b'\0'*((-len(data))%4)
    file.write_bytes(struct.pack('<III',0x46546C67,2,12+8+len(encoded)+8+len(data))+struct.pack('<II',len(encoded),0x4E4F534A)+encoded+struct.pack('<II',len(data),0x004E4942)+data)


def preview(faces, file, top=False):
    # Software painter's projection of actual exported triangle geometry.
    scale=2;image=Image.new('RGB',(1400*scale,1000*scale),(7,13,21));draw=ImageDraw.Draw(image)
    right=(1,0,0)if top else(.81,0,-.59);up=(0,0,1)if top else(.29,.87,.4);depth=cross(up,right)
    points=[v for _,_,vertices,_ in faces for v in vertices];xs=[dot(v,right)for v in points];ys=[dot(v,up)for v in points];factor=min(1150/(max(xs)-min(xs)),730/(max(ys)-min(ys)));cx=(min(xs)+max(xs))/2;cy=(min(ys)+max(ys))/2
    def projected(p):return ((700+(dot(p,right)-cx)*factor)*scale,(510-(dot(p,up)-cy)*factor)*scale)
    for _,material,vertices,normal in sorted(faces,key=lambda f:sum(dot(v,depth)for v in f[2])):
        if dot(normal,depth)<0:continue
        light=.32+.68*max(0,dot(normal,(-.3,.85,.43)))
        base=MATERIALS[material]['color'];color=tuple(min(255,int((c*light)**.7*255))for c in base)
        draw.polygon([projected(v)for v in vertices],fill=color)
    draw.text((45*scale,35*scale),"CARDASSIAN-STYLE CRUISER / ORIGINAL PROTOTYPE / CPU GEOMETRY PREVIEW",fill=(235,188,123),font_size=21*scale)
    draw.text((45*scale,940*scale),"Source geometry only. No Unity import, final art or canon-class approval implied.",fill=(175,190,207),font_size=19*scale)
    image.resize((1400,1000),Image.Resampling.LANCZOS).save(file)


def main():
    OUT.mkdir(parents=True,exist_ok=True)
    mtl=[]
    for m in MATERIALS:
        mtl += ["newmtl "+m['name'],"Kd "+" ".join(map(str,m['color'])),"Ks .25 .25 .25","Ns 48"]
        if'emission'in m:mtl.append("Ke "+" ".join(map(str,m['emission'])))
    (OUT/'cardassian-cruiser.mtl').write_text('\n'.join(mtl)+'\n',encoding='utf-8')
    lods=[]
    for lod in range(3):
        faces=make_model(lod);obj=OUT/f'cardassian-cruiser_LOD{lod}.obj';glb=OUT/f'cardassian-cruiser_LOD{lod}.glb';export_obj(faces,obj);export_glb(faces,glb)
        positions=[v for _,_,vs,_ in faces for v in vs];bounds=[[min(v[i]for v in positions)for i in range(3)],[max(v[i]for v in positions)for i in range(3)]]
        assert all(math.isfinite(x)for v in positions for x in v)
        assert all(abs(dot(n,n)-1)<1e-6 for _,_,_,n in faces)
        lods.append({"level":lod,"triangles":len(faces),"boundsXYZ":bounds,"obj":obj.name,"glb":glb.name})
        if lod==0:preview(faces,OUT/'three-quarter.png');preview(faces,OUT/'top.png',True)
    assert lods[0]['triangles']>lods[1]['triangles']>lods[2]['triangles']
    manifest={"visualKey":"card.cruiser-prototype.v1","status":"Original Cardassian-style prototype; no native import/owner final-art acceptance","provenance":"Fresh authored procedural geometry; owner reference images inform silhouette only, no third-party mesh/texture","axes":"metres; +Y up; +Z forward; root origin identity","artScaleIsNotGameplay":True,"materials":MATERIALS,"anchors":ANCHORS,"lods":lods,"validation":"CPU finite positions, unit normals, nondegenerate triangles and decreasing LOD counts; independent export check follows","files":[]}
    for p in sorted(OUT.iterdir()):
        if p.name not in ('manifest.json','validation.json'):manifest['files'].append({"file":p.name,"bytes":p.stat().st_size,"sha256":hashlib.sha256(p.read_bytes()).hexdigest()})
    (OUT/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n',encoding='utf-8');print(json.dumps(lods))


if __name__=='__main__':main()
