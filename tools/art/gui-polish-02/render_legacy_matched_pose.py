"""Retained legacy source at a lower matched camera elevation; local review only."""
import sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from receiving_proof_helpers import *
assert not (OUT/"legacy-pose-manifest.json").exists()
# Retained legacy Defiant: never export or recolor this unknown-license source.
scene,camera=reset()
path=ASSETS/'ArtDemo/Resources/ArtDemo/ActivityHulls/Defiant/Models/Defiant_LOD0.fbx'
hull=imported(path)
assert len(hull.data.loop_triangles)==29998
texture_root=ASSETS/'ArtDemo/Resources/ArtDemo/ActivityHulls/Defiant/Textures'
paths=[texture_root/('Defiant_Image_'+str(i)+'.jpg') for i in range(3)]
records.extend({'input':p.relative_to(ROOT).as_posix(),'sha256':sha(p)} for p in paths)
q=Nodes('Exact legacy PreviewStudio formula proxy')
geo=q.n.new('ShaderNodeNewGeometry')
normal_node=q.n.new('ShaderNodeNormalMap');q.l.new(q.texture(paths[2],True),normal_node.inputs['Color'])
n=q.receiver(normal_node.outputs[0]);view=q.receiver(geo.outputs['Incoming'])
key=tuple(Vector((.4,.7,.6)).normalized());fill=tuple(Vector((-.6,.25,-.4)).normalized())
diffuse=q.scalar('ADD',.24,q.scalar('ADD',q.scalar('MULTIPLY',.66,q.scalar('MAXIMUM',0,q.vector('DOT_PRODUCT',n,key))),
    q.scalar('MULTIPLY',.2,q.scalar('MAXIMUM',0,q.vector('DOT_PRODUCT',n,fill)))))
split=q.n.new('ShaderNodeSeparateColor');q.l.new(q.texture(paths[1],True),split.inputs[0])
half=q.vector('NORMALIZE',q.vector('ADD',view,key))
spec=q.scalar('POWER',q.scalar('MAXIMUM',0,q.vector('DOT_PRODUCT',n,half)),
    q.scalar('ADD',8,q.scalar('MULTIPLY',88,q.scalar('SUBTRACT',1,split.outputs['Green']))))
spec=q.scalar('MULTIPLY',spec,q.scalar('ADD',.12,q.scalar('MULTIPLY',.18,split.outputs['Blue'])))
rgb=q.n.new('ShaderNodeCombineXYZ')
for s in rgb.inputs:q.l.new(spec,s)
proxy=q.finish(q.vector('ADD',q.scale(q.texture(paths[0]),diffuse),rgb.outputs[0]))
hull.data.materials.clear();hull.data.materials.append(proxy)
lo=Vector([min(v.co[a] for v in hull.data.vertices) for a in range(3)])
hi=Vector([max(v.co[a] for v in hull.data.vertices) for a in range(3)])
centre=(lo+hi)/2
for sign in [-1,1]:
    direction=Vector((sign*.55,-.72,.20)).normalized()
    render('Legacy_Defiant_owner_pose_formula_yaw_'+str(sign),[hull],centre+direction*400,centre,998,544,225)


report={"inputs":records,"pose":"two yaw signs,12.45-degree elevation; owner crop has no camera metadata","source_palette":"unchanged legacy gray/red albedo; no replacement or recovered approved-palette claim","outputs":[{"path":p.name,"sha256":sha(p),"bytes":p.stat().st_size} for p in sorted(OUT.glob("Legacy_Defiant_owner_pose*.png"))]}
(OUT/"legacy-pose-manifest.json").write_bytes((json.dumps(report,indent=2)+'\n').encode())
