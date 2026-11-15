"""Separate existing original candidate parts without inventing a new module catalogue.

Blender background only. Input: own delivered cruiser atlas source. Output refuses overwrite.
The original whole hull remains the runtime candidate; module admission/socket gameplay is absent.
"""
import hashlib
import json
import math
import shutil
from pathlib import Path
import bpy
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[2]
SOURCE=ROOT/'art/asset-production-01/hulls-runtime-v2'
OUT=ROOT/'art/asset-production-01/cruiser-modules-v1'
assert not OUT.exists(),'Refusing to overwrite '+str(OUT)
for sub in ['Models','Textures','Sources','Previews']:(OUT/sub).mkdir(parents=True)
bpy.ops.wm.open_mainfile(filepath=str(SOURCE/'Sources/Cruiser_RuntimeAtlas.blend'))
bpy.context.preferences.filepaths.save_version=0
scene=bpy.context.scene
original=bpy.data.objects['Cruiser_LOD0']
for obj in list(scene.objects):
    if obj.type=='MESH' and obj!=original:bpy.data.objects.remove(obj,do_unlink=True)
for path in (SOURCE/'Textures').glob('Cruiser_*.png'):
    shutil.copyfile(path,OUT/'Textures'/path.name)
parent_contract=json.loads((SOURCE/'material-contract.json').read_text())
(OUT/'material-contract.json').write_bytes((json.dumps({'Cruiser':parent_contract['Cruiser'],
    'note':'Identical four maps and Material_0 recipe; components remain an unbound art interface.'},indent=2)+'\n').encode())
for image in bpy.data.images:
    if image.source=='FILE':image.filepath=str(OUT/'Textures'/Path(image.filepath).name)
material=original.data.materials[0]
me=original.data
adj=[set() for _ in me.vertices]
for edge in me.edges:
    a,b=edge.vertices;adj[a].add(b);adj[b].add(a)
groups={name:set() for name in ['Remainder','Nacelle_A','Nacelle_B','Deflector']}
unseen=set(range(len(me.vertices)))
components=[]
while unseen:
    pending=[min(unseen)];indices=set()
    while pending:
        index=pending.pop()
        if index in indices:continue
        indices.add(index);unseen.discard(index)
        pending.extend(adj[index]-indices)
    lo=Vector([min(me.vertices[i].co[a] for i in indices) for a in range(3)])
    hi=Vector([max(me.vertices[i].co[a] for i in indices) for a in range(3)])
    centre=(lo+hi)/2
    role='Remainder'
    # Classification of existing disconnected original geometry, not a physical cut or new shape.
    if abs(centre.x)>620*.19 and centre.y>-6:
        role='Nacelle_A' if centre.x<0 else 'Nacelle_B'
    elif abs(centre.x)<620*.01 and -620*.12<centre.y<-620*.08 and centre.z<-620*.04:
        role='Deflector'
    groups[role].update(indices)
    components.append({'role':role,'vertices':len(indices),'centre_blender':list(centre)})
assert all(groups.values()),'Expected existing parts were not identified'
assert len(components)>4
parts={};sockets={};records=[]
for role,indices in groups.items():
    indices=sorted(indices);mapping={old:new for new,old in enumerate(indices)}
    faces=[p for p in me.polygons if p.vertices[0] in mapping]
    assert all(all(i in mapping for i in p.vertices) for p in faces),'A partition would cut connected geometry'
    if role=='Remainder':pivot=Vector((0,0,0))
    else:
        lo=Vector([min(me.vertices[i].co[a] for i in indices) for a in range(3)])
        hi=Vector([max(me.vertices[i].co[a] for i in indices) for a in range(3)])
        pivot=(lo+hi)/2
    data=bpy.data.meshes.new('OriginalCruiser_'+role+'_mesh')
    data.from_pydata([me.vertices[i].co-pivot for i in indices],[],
                     [[mapping[i] for i in p.vertices] for p in faces])
    data.update();data.uv_layers.new(name='UVMap')
    normals=[]
    for target,source in zip(data.polygons,faces):
        target.use_smooth=source.use_smooth
        for to_li,from_li in zip(target.loop_indices,source.loop_indices):
            data.uv_layers[0].data[to_li].uv=me.uv_layers.active.data[from_li].uv
            normals.append(me.corner_normals[from_li].vector)
    data.normals_split_custom_set(normals)
    data.materials.append(material)
    obj=bpy.data.objects.new('OriginalCruiser_'+role,data);scene.collection.objects.link(obj)
    obj.location=pivot;parts[role]=obj
    sockets[role]={'parent_space_translation_blender_metres':list(pivot),
        'local_rotation_blender_degrees':[0,0,0],'local_scale':[1,1,1],
        'receiver_X_reflection_candidate_metres':[-pivot.x,pivot.z,-pivot.y],
        'receiver_note':'Transform authoring coordinates through the measured actual FBX import basis; candidate values are not native measurements.',
        'gameplay_component_binding':None}
bpy.data.objects.remove(original,do_unlink=True)

def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()

def export(name,objects,standalone=False):
    bpy.ops.object.select_all(action='DESELECT')
    positions=[o.location.copy() for o in objects]
    if standalone:
        assert len(objects)==1
        objects[0].location=(0,0,0)
    bpy.context.view_layer.update()
    stats=[]
    for obj in objects:
        obj.hide_set(False);obj.select_set(True);data=obj.data;data.calc_loop_triangles()
        world=[obj.matrix_world@v.co for v in data.vertices]
        stats.append({'name':obj.name,'vertices':len(data.vertices),'triangles':len(data.loop_triangles),
            'uv_sets':len(data.uv_layers),'materials':[m.name for m in data.materials],
            'bounds_blender':[[min(v[a] for v in world) for a in range(3)],
                              [max(v[a] for v in world) for a in range(3)]]})
    bpy.context.view_layer.objects.active=objects[0]
    bpy.ops.export_scene.gltf(filepath=str(OUT/'Models'/(name+'.glb')),export_format='GLB',
        use_selection=True,export_yup=True,export_apply=True,export_tangents=True,export_cameras=False,export_lights=False)
    bpy.ops.export_scene.fbx(filepath=str(OUT/'Models'/(name+'.fbx')),use_selection=True,
        object_types={'MESH'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,
        apply_unit_scale=True,global_scale=1,bake_anim=False,path_mode='RELATIVE',use_tspace=True)
    records.append({'asset_id':'art.production02.'+name.lower(),'name':name,'candidate_binding':None,'lod':0,
        'notes':'Existing original candidate geometry separated for art study; no gameplay socket/module admission.',
        'meshes':stats,'glb':'Models/'+name+'.glb','fbx':'Models/'+name+'.fbx'})
    for obj,pos in zip(objects,positions):obj.location=pos

for role,obj in parts.items():export(obj.name,[obj],True)
export('OriginalCruiser_ReassembledProof',list(parts.values()))

def render(name,objects,width=640,height=480):
    for obj in scene.objects:
        if obj.type=='MESH':obj.hide_render=obj not in objects
    points=[obj.matrix_world@Vector(co) for obj in objects for co in obj.bound_box]
    lo=Vector([min(p[a] for p in points) for a in range(3)])
    hi=Vector([max(p[a] for p in points) for a in range(3)])
    centre=(lo+hi)/2;size=max((hi-lo).length,1)
    yaw,pitch=math.radians(37.57),math.radians(26)
    direction=Vector((-math.sin(yaw)*math.cos(pitch),-math.cos(yaw)*math.cos(pitch),math.sin(pitch)))
    cam=scene.camera;cam.location=centre+direction*size*3
    cam.rotation_euler=(centre-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.clip_end=10000
    bpy.context.view_layer.update()
    points=[cam.matrix_world.inverted()@p for p in points]
    lo=Vector([min(p[a] for p in points) for a in range(3)])
    hi=Vector([max(p[a] for p in points) for a in range(3)])
    midpoint=(lo+hi)/2
    cam.location+=cam.matrix_world.to_3x3()@Vector((midpoint.x,midpoint.y,0))
    cam.data.ortho_scale=max(hi.x-lo.x,(hi.y-lo.y)*width/height)*1.12
    scene.view_settings.view_transform='AgX';scene.cycles.samples=16;scene.cycles.device='CPU'
    scene.render.threads_mode='FIXED';scene.render.threads=4
    scene.render.resolution_x,scene.render.resolution_y=width,height;scene.render.resolution_percentage=100
    scene.render.filepath=str(OUT/'Previews'/(name+'.png'));bpy.ops.render.render(write_still=True)

render('Reassembled_original_geometry_studio',list(parts.values()),1200,800)
for role in ['Nacelle_A','Nacelle_B','Deflector']:render(role+'_studio',[parts[role]])
for obj in parts.values():obj.hide_render=False
for image in bpy.data.images:
    if image.source=='FILE':image.filepath='//../Textures/'+Path(image.filepath).name
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Sources/OriginalCruiser_SeparatedParts.blend'))
interface={'status':'Art reassembly interface only; not installed equipment or old Galaxy compatibility',
    'parent_candidate':'hull.galaxy-cruiser.v1 original procedural candidate',
    'source_complete_hull':'art/asset-production-01/hulls-runtime-v2/Models/Cruiser_Runtime_LOD0.glb',
    'source_complete_sha256':sha(SOURCE/'Models/Cruiser_Runtime_LOD0.glb'),
    'socket_space':'Blender parent metres, +Z up/-Y bow. Preserve actual FBX transforms exactly once.',
    'parts':sockets,'connected_components':components,
    'naming':'A/B denotes authoring negative/positive X; no unverified port/starboard import claim.',
    'material_contract':'Same Material_0 / four original Cruiser atlases as hulls-runtime-v2/material-contract.json; explicit emission and ORM binding required.'}
(OUT/'art-interface.json').write_bytes((json.dumps(interface,indent=2)+'\n').encode())
m={'schema':1,'phase':'modules','status':'proposed separated existing art; native receiving outstanding',
   'blender':bpy.app.version_string,'inputs':[{'path':str((SOURCE/'Sources/Cruiser_RuntimeAtlas.blend').relative_to(ROOT)),
       'sha256':sha(SOURCE/'Sources/Cruiser_RuntimeAtlas.blend')}],
   'texture_sources':[{'path':'Textures/'+p.name,'sha256':sha(p),
        'source_repo_path':(SOURCE/'Textures'/p.name).relative_to(ROOT).as_posix(),
        'author':'Original HobbyDS9 procedural atlas','changes':'None; exact copy from own hulls-runtime-v2 packet'}
        for p in sorted((OUT/'Textures').glob('*.png'))], 'assets':records,'files':[]}
for path in sorted(OUT.rglob('*')):
    if path.is_file():m['files'].append({'path':path.relative_to(OUT).as_posix(),'bytes':path.stat().st_size,'sha256':sha(path)})
(OUT/'manifest.json').write_bytes((json.dumps(m,indent=2)+'\n').encode())
print('ORIGINAL_MODULES_COMPLETE',flush=True)
