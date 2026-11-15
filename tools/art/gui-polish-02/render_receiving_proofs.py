"""Render full GUI-POLISH-02 controlled probes; shared helpers do not launch renders."""
import sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from receiving_proof_helpers import *
assert not (OUT/"manifest.json").exists(),"Refusing to overwrite completed proofs"
scene,camera=reset()
globe=imported(ASSETS/'Review/Resources/SystemBodyArt/SolMeshes/Earth_LOD1.fbx')
assert .98<max(v.co.length for v in globe.data.vertices)<1.02,'Unexpected delivered unit radius'
globe.scale=(.7,.7,.7)
for label,file,mars in [('Earth','earth_daymap.jpg',False),('Mars','mars.jpg',True)]:
    path=ASSETS/'Review/Resources/SystemBodyArt'/file
    records.append({'input':path.relative_to(ROOT).as_posix(),'sha256':sha(path)})
    for mode,new,light in [('baseline_bound_formula',False,(-3,2,-4)),
                           ('proposed_same_light',True,(-3,2,-4)),
                           ('proposed_raking_light',True,(-3.6,1.4,-1.2))]:
        globe.data.materials.clear();globe.data.materials.append(planet(path,light,new,mars))
        render(label+'_'+mode,[globe],(0,2.8,0))

# Same circular geometry/pose, two actual perspective-plane elevations; no nonuniform ellipse.
def unlit(name,color):
    q=Nodes(name);rgb=q.n.new('ShaderNodeRGB');rgb.outputs[0].default_value=(*color,1);return q.finish(rgb.outputs[0])
star_material=unlit('Unchanged schematic star',(1,.44,.08))
bpy.ops.mesh.primitive_uv_sphere_add(segments=48,ring_count=24,radius=.75)
star=bpy.context.object;star.data.materials.append(star_material)
for p in star.data.polygons:p.use_smooth=True
objects=[star]
for i,(name,file,angle,radius) in enumerate([('Mars','mars.jpg',140,2.4),('Earth','earth_daymap.jpg',270,3.6)]):
    o=globe.copy();o.data=globe.data.copy();scene.collection.objects.link(o)
    theta=math.radians(angle);ux,uz=radius*math.cos(theta),radius*math.sin(theta)
    o.location=(-ux,-uz,0);o.data.materials.clear()
    o.data.materials.append(planet(ASSETS/'Review/Resources/SystemBodyArt'/file,(0,0,0),True,name=='Mars',.04));objects.append(o)
    bpy.ops.mesh.primitive_torus_add(major_radius=radius,minor_radius=.009,major_segments=192,minor_segments=4)
    ring=bpy.context.object;ring.data.materials.append(unlit('Orbit '+name,(.5,.25,.08) if i else (.05,.09,.15)))
    ring.visible_shadow=False;ring.location.z=-.045;objects.append(ring)
for elevation in [54,24]:
    angle=math.radians(elevation);aspect=1440/420
    size=max(3.6*math.sin(angle)+.7,(3.6+.7)/aspect)*1.1
    render('Shared_plane_'+str(elevation)+'deg_proposal',objects,(0,12*math.cos(angle),12*math.sin(angle)),
           width=1440,height=420,scale=size*2*aspect)

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
    direction=Vector((sign*.55,-.72,.42)).normalized()
    render('Legacy_Defiant_bound_formula_yaw_'+str(sign),[hull],centre+direction*400,centre,998,544,205)

manifest={'date':'2026-11-14','status':'supporting CPU/formula/geometry review evidence; not Unity captures',
    'inputs':records,'blender':bpy.app.version_string,
    'planet_control':'Same retained Earth_LOD1 mesh and exact source albedo, radius .7, camera (Unity 0,0,-2.8), orthographic size .875. '
        'Controlled no-cloud comparison, not the owner frame cloud seed or a pixel-exact native reproduction.',
    'orbit_control':'Same two disclosed-body fixture poses/radii, same circular shared plane; elevation54 vs24, fitted with sine elevation. '
        'Illustrative pose, not claimed owner campaign state/picking evidence.',
    'legacy_defiant_control':'Exact retained FBX and three existing source maps,29998 triangles, no emission link. '
        'Two candidate yaw signs bracket owner crop; crop contains no camera/build metadata. No new export/recolor or approved-palette claim.',
    'outputs':[]}
for p in sorted(OUT.glob('*.png')):manifest['outputs'].append({'path':p.name,'sha256':sha(p),'bytes':p.stat().st_size})
(OUT/'manifest.json').write_bytes((json.dumps(manifest,indent=2)+'\n').encode())
print('GUI_POLISH_02_PROOFS_COMPLETE',flush=True)
