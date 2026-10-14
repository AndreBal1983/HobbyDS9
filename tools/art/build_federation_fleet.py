"""Original low-poly Federation-style hull studies. Run in a fresh background Blender.

No downloaded geometry, source .blend mutation, Unity import or gameplay definitions.
Blender authoring axes: -Y bow, +Z up; GLB export: +Z bow, +Y up.
"""
import bpy, bmesh, math, json, hashlib, sys
from pathlib import Path
from mathutils import Vector, Matrix

# Optional output after Blender's -- separator. Always invoke in a fresh
# --background process; this recipe resets that process's scene.
OUT=Path(sys.argv[sys.argv.index('--')+1]) if '--' in sys.argv else Path(__file__).resolve().parents[2]/'artifacts/art/federation-fleet-v1'
for d in ['sources','exports','previews','validation']:(OUT/d).mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene
scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
scene.render.engine='BLENDER_EEVEE'
scene.render.resolution_x=1600;scene.render.resolution_y=1000;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG';scene.render.image_settings.color_mode='RGBA'
scene.render.film_transparent=False
scene.view_settings.view_transform='AgX'
world=bpy.data.worlds.new('Inspection space');world.use_nodes=True
world.node_tree.nodes['Background'].inputs[0].default_value=(.012,.022,.04,1)
world.node_tree.nodes['Background'].inputs[1].default_value=.35;scene.world=world

def material(name,colour,metal=.4,rough=.38,emission=0):
    m=bpy.data.materials.new(name);m.diffuse_color=(*colour,1);m.use_nodes=True
    p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*colour,1)
    p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough
    p.inputs['Emission Color'].default_value=(*colour,1);p.inputs['Emission Strength'].default_value=emission
    return m
M={
 'Hull':material('FED_Hull_Pearl',(.51,.58,.65),.35,.36),
 'Panel':material('FED_Panel_Slate',(.29,.36,.44),.45,.4),
 'LightPanel':material('FED_Panel_Silver',(.65,.69,.71),.4,.4),
 'Dark':material('FED_Recess_Graphite',(.018,.029,.045),.35,.42),
 'Phaser':material('FED_Phaser_Bronze',(.28,.2,.105),.65,.34),
 'Blue':material('FED_Warp_Blue',(.025,.32,1),.15,.3,2.4),
 'Red':material('FED_Collector_Red',(.95,.07,.018),.2,.3,1.1),
 'Window':material('FED_Window_Ice',(.55,.76,1),.05,.35,1.3),
 'Deflector':material('FED_Deflector_Amber',(.95,.32,.055),.25,.3,1.5)
}
parts=[];group='Hull';lod=0;segments=72

def register(obj,name,mat='Hull',smooth=False):
    obj.name=name;obj.data.materials.append(M[mat])
    for p in obj.data.polygons:p.use_smooth=smooth
    obj['assembly']=group;parts.append(obj);return obj

def mesh(name,verts,faces,mat='Hull',smooth=False):
    me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);me.update()
    o=bpy.data.objects.new(name,me);scene.collection.objects.link(o)
    # Deterministic box-planar UVs; no external images required by these flat PBR materials.
    uv=me.uv_layers.new(name='UVMap')
    for p in me.polygons:
        axis=max(range(3),key=lambda a:abs(p.normal[a]));axes=[a for a in range(3) if a!=axis]
        for li in p.loop_indices:
            co=me.vertices[me.loops[li].vertex_index].co
            uv.data[li].uv=(co[axes[0]]*.01,co[axes[1]]*.01)
    return register(o,name,mat,smooth)

def box(name,loc,dim,mat='Hull',angle=0,bevel=0):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.scale=dim
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    o.rotation_euler.z=angle
    if bevel and lod<2:
        m=o.modifiers.new('Authored edge bevel','BEVEL');m.width=bevel;m.segments=2 if lod==0 else 1
        bpy.ops.object.modifier_apply(modifier=m.name)
    return register(o,name,mat)

def ellipsoid(name,loc,dim,mat='Hull'):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=max(12,segments//3),ring_count=8 if lod==0 else 6,location=loc)
    o=bpy.context.object;o.scale=dim;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    return register(o,name,mat,True)

def beam(name,a,b,width,depth,mat='Hull'):
    mid=(Vector(a)+Vector(b))/2;v=Vector(b)-Vector(a)
    o=box(name,mid,(width,depth,v.length),mat,bevel=min(width,depth)*.12)
    o.rotation_euler=v.to_track_quat('Z','Y').to_euler();return o

def saucer(name,c,rx,ry,h):
    # Closed double-curved disc; elliptical rings share vertices for continuous normals.
    profile=[(.015,h),(.12,h*.97),(.24,h*.60),(.46,h*.34),(.72,h*.21),(.9,h*.02),(1,-h*.10),(1,-h*.27),(.9,-h*.35),(.7,-h*.51),(.4,-h*.65),(.14,-h*.72),(.015,-h*.72)]
    verts=[]
    for r,z in profile:
        for i in range(segments):
            a=i*math.tau/segments;verts.append((c[0]+rx*r*math.cos(a),c[1]+ry*r*math.sin(a),c[2]+z))
    faces=[]
    for j in range(len(profile)-1):
        for i in range(segments):faces.append((j*segments+i,j*segments+(i+1)%segments,(j+1)*segments+(i+1)%segments,(j+1)*segments+i))
    faces.append(tuple(reversed(range(segments))));faces.append(tuple((len(profile)-1)*segments+i for i in range(segments)))
    o=mesh(name,verts,faces,'Hull',True)
    o.data.materials.append(M['LightPanel']);o.data.materials.append(M['Panel'])
    for p in o.data.polygons:
        j=p.index//segments;i=p.index%segments
        if j in (2,3,4,8,9) and (i//max(1,segments//24))%4==0:p.material_index=1
        if j in (5,6):p.material_index=2
    return o

def ring(name,c,rx,ry,r,width,z,mat='Panel',start=0,end=math.tau):
    n=max(8,int(segments*(end-start)/math.tau));v=[]
    for rr in (r-width/2,r+width/2):
        for i in range(n+1):
            a=start+(end-start)*i/n;v.append((c[0]+rx*rr*math.cos(a),c[1]+ry*rr*math.sin(a),c[2]+z))
    return mesh(name,v,[(n+1+i,n+2+i,i+1,i) for i in range(n)],mat)

def axial(name,x,y,z,length,radius,mat='Hull',width=1,height=1):
    n=max(12,segments//2);prof=[(-.5,.08),(-.46,.65),(-.34,.97),(.25,1),(.44,.6),(.5,.08)]
    vs=[(x+radius*width*r*math.cos(i*math.tau/n),y+t*length,z+radius*height*r*math.sin(i*math.tau/n)) for t,r in prof for i in range(n)]
    faces=[(j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i) for j in range(len(prof)-1) for i in range(n)]
    faces+=[tuple(reversed(range(n))),tuple((len(prof)-1)*n+i for i in range(n))]
    return mesh(name,vs,faces,mat,True)

def bridge(c,size):
    ellipsoid('Bridge base',(c[0],c[1],c[2]),(size,size*1.4,size*.36),'Panel')
    ellipsoid('Bridge dome',(c[0],c[1]-size*.2,c[2]+size*.28),(size*.65,size*.9,size*.3),'LightPanel')
    if lod==0:box('Bridge forward glazing',(c[0],c[1]-size*.91,c[2]+size*.3),(size*.65,.6,.7),'Window')

def windows(c,rx,ry,h):
    if lod==2:return
    n=72 if lod==0 else 24
    for i in range(n):
        if i%7==0:continue
        a=i*math.tau/n
        box('Rim window',(c[0]+rx*.995*math.cos(a),c[1]+ry*.995*math.sin(a),c[2]-h*.18),(2.4 if lod==0 else 4,.5,1.2),'Window',a+math.pi/2)

def nacelle(x,y,z,length,radius):
    axial('Nacelle housing',x,y,z,length,radius,'Hull',.8,.9)
    box('Dorsal engine spine',(x,y+length*.04,z+radius*.79),(radius*.8,length*.73,radius*.18),'Panel',bevel=radius*.05)
    ellipsoid('Forward collector',(x,y-length*.43,z),(radius*.64,radius*.35,radius*.64),'Red')
    for side in [-1,1]:
        box('Warp grille recess',(x+side*radius*.76,y+length*.05,z),(radius*.12,length*.65,radius*.65),'Dark')
        box('Warp coil glow',(x+side*radius*.83,y+length*.05,z),(radius*.045,length*.61,radius*.34),'Blue')
    if lod==0:
        for i in range(7):box('Coil divider',(x,y-length*.25+i*length*.095,z+radius*.82),(radius*.84,1.6,1.1),'Dark')

def saucer_detail(c,rx,ry,h):
    ring('Dorsal phaser arc',c,rx,ry,.80,.012,h*.145,'Phaser',.12,math.pi-.12)
    ring('Forward phaser arc',c,rx,ry,.80,.012,h*.145,'Phaser',math.pi+.12,math.tau-.12)
    if lod<2:
        ring('Outer panel seam',c,rx,ry,.92,.004,-h*.02,'Panel')
        ring('Inner panel seam',c,rx,ry,.45,.005,h*.35,'Panel')
    windows(c,rx,ry,h);bridge((c[0],c[1],c[2]+h*.9),max(3,rx*.06))

def exploration():
    global group
    c=(0,-100,20);saucer('Exploration saucer',c,180,145,21);saucer_detail(c,180,145,21)
    box('Dorsal neck',(0,6,-6),(29,92,63),'Hull',bevel=3)
    axial('Engineering hull',0,85,-48,300,35,'Hull',.9,1)
    ellipsoid('Deflector recess',(0,-44,-48),(25,4,20),'Dark')
    ellipsoid('Amber deflector',(0,-47.5,-48),(20,2.5,15),'Deflector')
    ellipsoid('Deflector core',(0,-49.4,-48),(12,1,8),'Blue')
    box('Shuttle bay',(0,206,-27),(24,20,12),'Dark',bevel=1.5)
    for side in [-1,1]:
        beam('Swept pylon',(side*19,96,-35),(side*128,148,42),12,37,'Panel')
        group='PortNacelle' if side<0 else 'StarboardNacelle';nacelle(side*130,135,45,246,11);group='Hull'
    group='ForwardEquipment';box('Forward sensor fairing',(0,-226,21),(19,13,4),'Panel',bevel=1)
    box('Sensor face',(0,-233,21),(12,.8,2),'Blue');group='Hull'
    return {'Nacelle_Port':(-130,135,45),'Nacelle_Starboard':(130,135,45),'Forward_Equipment':(0,-226,21),'Effect_Deflector':(0,-49.5,-48),'Effect_Torpedo':(0,-12,-32)}

def patrol():
    global group
    c=(0,-24,0);saucer('Patrol saucer',c,95,98,13);saucer_detail(c,95,98,13)
    box('Aft engineering block',(0,37,3),(99,70,19),'Hull',bevel=6)
    box('Shuttle hangar',(0,74,3),(35,4,10),'Dark',bevel=1)
    for side in [-1,1]:
        beam('Lower engine strut',(side*65,32,-5),(side*101,46,-24),11,28,'Panel')
        beam('Rollbar upright',(side*73,35,8),(side*73,56,41),8,13,'Hull')
        group='PortNacelle' if side<0 else 'StarboardNacelle';nacelle(side*101,53,-24,190,8);group='Hull'
    box('Rollbar crossbeam',(0,56,42),(153,16,9),'Panel',bevel=2)
    group='ForwardEquipment';box('Rollbar mission pod',(0,53,49),(26,36,11),'Hull',bevel=3)
    box('Twin forward aperture surround',(0,34,48),(18,2,6),'Dark')
    for side in [-1,1]:box('Launcher aperture',(side*4,32.8,48),(4,1,3),'Deflector')
    group='Hull'
    return {'Nacelle_Port':(-101,53,-24),'Nacelle_Starboard':(101,53,-24),'Forward_Equipment':(0,53,49),'Effect_Torpedo':(0,31,48),'Effect_Deflector':(0,-109,-4)}

def escort():
    global group
    outline=[(-20,-75), (20,-75),(45,-48),(58,0),(51,64),(24,76),(-24,76),(-51,64),(-58,0),(-45,-48)]
    vs=[(x,y,z) for z in [-10,3] for x,y in outline]
    n=len(outline);faces=[tuple(reversed(range(n))),tuple(n+i for i in range(n))]+[(i,(i+1)%n,n+(i+1)%n,n+i) for i in range(n)]
    o=mesh('Compact escort hull',vs,faces)
    bpy.context.view_layer.objects.active=o;o.select_set(True)
    b=o.modifiers.new('Armoured perimeter bevel','BEVEL');b.width=2.2;b.segments=2 if lod==0 else 1
    bpy.ops.object.modifier_apply(modifier=b.name);o.select_set(False)
    ellipsoid('Dorsal armoured core',(0,7,7),(32,55,13),'Panel')
    ellipsoid('Forward command fairing',(0,-37,10),(22,25,10),'Hull');bridge((0,-10,20),7)
    for side in [-1,1]:
        group='PortNacelle' if side<0 else 'StarboardNacelle'
        axial('Integrated warp sponson',side*49,8,-1,126,14,'Hull',.8,.65)
        box('Outer drive trench',(side*60,18,-1),(2.5,75,6),'Dark')
        box('Lateral warp strip',(side*61.5,18,-1),(.9,65,3),'Blue')
        ellipsoid('Recessed collector',(side*49,-46,-1),(7,3.8,5),'Red')
        group='Hull';beam('Shoulder armour',(side*24,-24,6),(side*43,33,6),6,18,'LightPanel')
        box('Impulse recess',(side*35,63,1),(18,4,5),'Dark')
        box('Impulse glow',(side*35,65.3,1),(14,.8,2.5),'Red')
        box('Forward emitter',(side*47,-48,-3),(7,17,5),'Dark',bevel=1)
    if lod<2:
        for y in [-19,-10,0,10,20]:box('Dorsal vent',(0,y,20),(15,2,.8),'Dark')
        for side in [-1,1]:
            for y in [-25,-12,0,12,25]:box('Hull window',(side*56,y,3.2),(.7,2.5,.8),'Window')
    group='ForwardEquipment';box('Forward sensor bay',(0,-72,-1),(20,5,6),'Dark',bevel=1)
    box('Forward sensor glow',(0,-75,-1),(12,.7,2),'Blue');group='Hull'
    return {'Nacelle_Port':(-49,8,-1),'Nacelle_Starboard':(49,8,-1),'Forward_Equipment':(0,-72,-1),'Effect_Cannon_Port':(-47,-58,-3),'Effect_Cannon_Starboard':(47,-58,-3)}

def point_at(o,p):o.rotation_euler=(Vector(p)-o.location).to_track_quat('-Z','Y').to_euler()
rig=bpy.data.collections.new('Presentation_only');scene.collection.children.link(rig)
def presentation_object(o):
    for c in list(o.users_collection):c.objects.unlink(o)
    rig.objects.link(o)
camdata=bpy.data.cameras.new('BeautyCamera');camdata.clip_end=100000;camdata.clip_start=.05
cam=bpy.data.objects.new('BeautyCamera',camdata);rig.objects.link(cam);scene.camera=cam
lights=[]
for name,col in [('Key',(1,.88,.75)),('Fill',(.48,.66,1)),('Rim',(.3,.57,1))]:
    data=bpy.data.lights.new(name,'AREA');ob=bpy.data.objects.new(name,data);rig.objects.link(ob);data.color=col;data.shape='DISK';lights.append(ob)

def bounds(objects):
    p=[o.matrix_world@Vector(c) for o in objects for c in o.bound_box]
    return [min(v[i] for v in p) for i in range(3)],[max(v[i] for v in p) for i in range(3)]

assets=[];all_roots=[]
for slug,label,fn,length in [('exploration-cruiser','Federation exploration cruiser',exploration,620),('patrol-frigate','Federation patrol frigate',patrol,280),('compact-escort','Federation compact escort',escort,170)]:
    root=bpy.data.objects.new(slug,None);scene.collection.objects.link(root);all_roots.append(root)
    root['asset_id']='art.federation.'+slug+'.v1';root['authoring_forward']='-Y';root['authoring_up']='+Z';root['nominal_length_m']=length
    root['status']='Original stylised hull art; no canonical class or gameplay identity assigned'
    lod_records=[];lod_groups=[];basecentre=None;scale=None;anchors={}
    for lod in range(3):
        segments=[72,40,20][lod];parts=[];group='Hull';rawanchors=fn();bpy.context.view_layer.update()
        lo,hi=bounds(parts)
        if lod==0:basecentre=Vector([(lo[i]+hi[i])/2 for i in range(3)]);scale=length/(hi[1]-lo[1])
        for o in parts:
            o.matrix_world=Matrix.Scale(scale,4)@Matrix.Translation(-basecentre)@o.matrix_world
        bpy.context.view_layer.update()
        parent=bpy.data.objects.new(f'{slug}_LOD{lod}',None);scene.collection.objects.link(parent);parent.parent=root;lod_groups.append(parent)
        joined=[]
        assembly_parts={category:[o for o in parts if o.get('assembly')==category] for category in ['Hull','PortNacelle','StarboardNacelle','ForwardEquipment']}
        for category in ['Hull','PortNacelle','StarboardNacelle','ForwardEquipment']:
            objs=assembly_parts[category]
            if not objs:continue
            bpy.ops.object.select_all(action='DESELECT')
            for o in objs:o.select_set(True)
            bpy.context.view_layer.objects.active=objs[0];bpy.ops.object.join();o=bpy.context.object;o.name=f'{slug}_{category}_LOD{lod}'
            bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
            # Keep every mesh's pivot at its actual authored module interface.
            a=rawanchors.get({'PortNacelle':'Nacelle_Port','StarboardNacelle':'Nacelle_Starboard','ForwardEquipment':'Forward_Equipment'}.get(category,''))
            pivot=(Vector(a)-basecentre)*scale if a else Vector((0,0,0))
            world_matrix=o.matrix_world.copy();o.data.transform(Matrix.Translation(-pivot)@world_matrix);o.matrix_world=Matrix.Translation(pivot);o.parent=parent
            bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.recalc_face_normals(bm,faces=bm.faces[:]);bm.to_mesh(o.data);bm.free()
            tri=o.modifiers.new('Export triangulation','TRIANGULATE');bpy.ops.object.modifier_apply(modifier=tri.name)
            o.data.validate(verbose=False);joined.append(o)
        bpy.context.view_layer.update();lo,hi=bounds(joined)
        if lod==0:
            anchors={n:list((Vector(v)-basecentre)*scale) for n,v in rawanchors.items()}
            anchors['Axis_Bow']=[0,length*.5*-1,0];anchors['Axis_Up']=[0,0,length*.1]
            for name,p in anchors.items():
                ob=bpy.data.objects.new(name,None);scene.collection.objects.link(ob);ob.parent=root;ob.location=p;ob.empty_display_type='ARROWS';ob.empty_display_size=length*.015
        export_objects=[root,parent]+joined+[o for o in root.children if o.type=='EMPTY' and not o.name.startswith(slug+'_LOD')]
        bpy.ops.object.select_all(action='DESELECT')
        for o in export_objects:o.select_set(True)
        fb=OUT/'exports'/f'{slug}_LOD{lod}.fbx';gl=OUT/'exports'/f'{slug}_LOD{lod}.glb'
        bpy.ops.export_scene.fbx(filepath=str(fb),use_selection=True,object_types={'EMPTY','MESH'},add_leaf_bones=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,global_scale=1,bake_anim=False,use_mesh_modifiers=True,mesh_smooth_type='FACE')
        bpy.ops.export_scene.gltf(filepath=str(gl),export_format='GLB',use_selection=True,export_yup=True,export_apply=True,export_cameras=False,export_lights=False,export_extras=True)
        lod_records.append({'level':lod,'triangles':sum(len(o.data.polygons) for o in joined),'vertices':sum(len(o.data.vertices) for o in joined),'meshObjects':len(joined),'materialSlots':sum(len(o.data.materials) for o in joined),'boundsAuthoring':[lo,hi],'fbx':fb.name,'glb':gl.name})
        for o in joined:o.hide_render=lod!=0;o.hide_set(lod!=0)
    # Hide earlier hulls for individual source, exports and previews.
    for other in all_roots[:-1]:
        for child in other.children_recursive:child.hide_render=True;child.hide_set(True)
    L=length;beauty=(L*.95,L*.8,L*.65) if slug=='exploration-cruiser' else (L*.88,-L*1.05,L*.80)
    cam.location=beauty;camdata.type='ORTHO';camdata.ortho_scale=L*1.60;point_at(cam,(0,0,0))
    for ob,loc,power,size in zip(lights,[(.6*L,-.7*L,L),(-.6*L,.2*L,.6*L),(.2*L,.8*L,.5*L)],[18,7,24],[.65,.8,.4]):
        ob.location=loc;ob.data.energy=power*L*L;ob.data.size=size*L;point_at(ob,(0,0,0))
    scene.render.filepath=str(OUT/'previews'/f'{slug}-beauty.png');bpy.ops.render.render(write_still=True)
    cam.location=(0,0,L*1.7);point_at(cam,(0,0,0));camdata.ortho_scale=L*1.90
    scene.render.filepath=str(OUT/'previews'/f'{slug}-top.png');bpy.ops.render.render(write_still=True)
    cam.location=beauty;point_at(cam,(0,0,0));camdata.ortho_scale=L*1.60
    # Separate source file contains this hull only and shared presentation/materials.
    source=OUT/'sources'/f'{slug}.blend'
    bpy.ops.wm.save_as_mainfile(filepath=str(source),compress=True)
    assets.append({'id':root['asset_id'],'name':label,'slug':slug,'nominalArtLengthMetres':length,'source':source.name,'authoringAxes':'-Y forward, +Z up','expectedGLBAxes':'+Z forward, +Y up','anchorsAuthoringMetres':anchors,'lods':lod_records})
    print('ASSET_COMPLETED',slug,flush=True)
    # Remove earlier model from live authoring scene before the next save.
    for o in list(root.children_recursive)+[root]:bpy.data.objects.remove(o,do_unlink=True)
    all_roots=[]

manifest={'created':'2026-10-14','status':'Engine-targeted original hull art candidates; native acceptance recorded separately','creator':'HobbyDS9 original procedural mesh authoring by Codex','thirdPartyGeometryUsed':False,'referenceRole':'Owner-supplied Federation silhouettes/material references only; not exact canon class replicas','recipe':'tools/art/build_federation_fleet.py','materials':[{'name':m.name,'baseColour':list(m.diffuse_color),'metallic':m.node_tree.nodes.get('Principled BSDF').inputs['Metallic'].default_value,'roughness':m.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value,'emission':m.node_tree.nodes.get('Principled BSDF').inputs['Emission Strength'].default_value,'pbr':'Principled base colour / metallic / roughness / emissive; no external textures'} for m in M.values()],'assets':assets,'files':[]}
for p in sorted(OUT.rglob('*')):
    if p.is_file() and p.suffix in ['.fbx','.glb','.blend','.png']:manifest['files'].append({'path':str(p.relative_to(OUT)).replace('\\','/'),'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()})
(OUT/'asset-manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
print('FLEET_PACK_COMPLETE',flush=True)
