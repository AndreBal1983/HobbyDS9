"""Bounded refinements of the delivered v1 assets for the concrete GUI receiving contract.

Blender 5.3 background: --python tools/art/build_runtime_asset_slice_02.py -- system
No v1 files are changed. Fresh output required. No network or external model input.
"""
import hashlib
import json
import math
import shutil
import struct
import sys
import zlib
from pathlib import Path
import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
PHASE = sys.argv[sys.argv.index('--')+1]
assert PHASE in ['system', 'hulls']
OUT = ROOT/'art/asset-production-01'/('system-receiving-witness-v2' if PHASE == 'system' else 'hulls-runtime-v2')
if OUT.exists():
    raise RuntimeError('Refusing to overwrite '+str(OUT))
for sub in ['Models', 'Textures', 'Sources', 'Previews']:
    (OUT/sub).mkdir(parents=True)
assets, inputs, texture_sources = [], [], []


def digest(p):
    return hashlib.sha256(p.read_bytes()).hexdigest()


def source(path):
    inputs.append({'path': path.relative_to(ROOT).as_posix(), 'sha256': digest(path)})
    bpy.ops.wm.open_mainfile(filepath=str(path))
    bpy.context.preferences.filepaths.save_version = 0


def keep_meshes(names):
    for obj in list(bpy.context.scene.objects):
        if obj.type == 'MESH' and obj.name not in names:
            bpy.data.objects.remove(obj, do_unlink=True)
    for name in names:
        obj = bpy.data.objects[name]
        obj.hide_set(False)
        obj.hide_render = False


def material(name):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    p = m.node_tree.nodes['Principled BSDF']
    p.inputs['Base Color'].default_value = (.65,.65,.65,1)
    p.inputs['Metallic'].default_value = 0
    p.inputs['Roughness'].default_value = .82
    return m


def export(name, obj, identity, lod, notes):
    bpy.ops.object.select_all(action='DESELECT')
    obj.hide_set(False)
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    me = obj.data
    me.calc_loop_triangles()
    bounds = [[min(v.co[a] for v in me.vertices) for a in range(3)],
              [max(v.co[a] for v in me.vertices) for a in range(3)]]
    bpy.ops.export_scene.gltf(filepath=str(OUT/'Models'/(name+'.glb')), export_format='GLB',
        use_selection=True, export_yup=True, export_apply=True, export_tangents=True,
        export_cameras=False, export_lights=False)
    bpy.ops.export_scene.fbx(filepath=str(OUT/'Models'/(name+'.fbx')), use_selection=True,
        object_types={'MESH'}, add_leaf_bones=False, axis_forward='-Z', axis_up='Y',
        apply_unit_scale=True, global_scale=1, bake_anim=False, mesh_smooth_type='FACE',
        path_mode='RELATIVE', use_tspace=True)
    assets.append({'asset_id': 'art.production02.'+name.lower(), 'name': name,
        'candidate_binding': identity, 'lod': lod, 'notes': notes,
        'meshes': [{'name': obj.name, 'vertices': len(me.vertices), 'triangles': len(me.loop_triangles),
                    'uv_sets': len(me.uv_layers), 'bounds_blender': bounds,
                    'materials': [m.name for m in me.materials]}],
        'glb': 'Models/'+name+'.glb', 'fbx': 'Models/'+name+'.fbx'})


def render(name, obj, location, texture=None, unlit=False):
    scene = bpy.context.scene
    for o in scene.objects:
        if o.type == 'MESH':
            o.hide_render = o != obj
    cam = scene.camera
    cam.location = location
    cam.rotation_euler = (-cam.location).to_track_quat('-Z','Y').to_euler()
    cam.data.clip_end = 10000
    cam.data.ortho_scale = 2.65
    scene.render.engine = 'CYCLES'
    scene.cycles.device = 'CPU'
    scene.cycles.samples = 16
    scene.cycles.use_denoising = True
    scene.render.threads_mode = 'FIXED'
    scene.render.threads = 4
    scene.render.resolution_x = scene.render.resolution_y = 512
    scene.render.resolution_percentage = 100
    scene.render.filepath = str(OUT/'Previews'/(name+'.png'))
    if texture:
        m = material(name+'_proof_material')
        p = m.node_tree.nodes['Principled BSDF']
        t = m.node_tree.nodes.new('ShaderNodeTexImage')
        t.image = bpy.data.images.load(str(texture), check_existing=True)
        m.node_tree.links.new(t.outputs['Color'], p.inputs['Base Color'])
        if unlit:
            m.node_tree.links.new(t.outputs['Color'],p.inputs['Emission Color'])
            p.inputs['Emission Strength'].default_value = 1
        obj.data.materials.clear()
        obj.data.materials.append(m)
    bpy.ops.render.render(write_still=True)


def save(name):
    for image in bpy.data.images:
        if image.source == 'FILE':
            image.filepath = '//../Textures/'+Path(image.filepath).name
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Sources'/(name+'.blend')))


if PHASE == 'system':
    source(ROOT/'art/asset-production-01/system-v1/Sources/SystemBody_Library.blend')
    names = ['Earth_LOD1','Mars_LOD1']
    keep_meshes(names)
    for name in names:
        obj = bpy.data.objects[name]
        obj.rotation_euler = (0,0,0)
        # Preserve the received v1 UVs: glTF RH coordinates alone cannot establish
        # a Unity FBX UV defect. QA must inspect the full imported hierarchy/basis.
        obj.data.materials.clear()
        obj.data.materials.append(material('UnitGlobe_OverrideAtRuntime'))
        export(name.replace('_LOD1','_ReceivingWitness_LOD1'), obj, None, 1,
               'QA witness only; preserves v1 UVs/positions. Not a replacement recommendation or new runtime binding. '
               'RH glTF U0=-X,U.25=+Z; X-reflected FBX receiver candidate U0=+X,U.25=+Z. '
               'QA must measure actual imported hierarchy; no UV correction is established.')

    # Original labelled UV witness. Blender internal pixels are bottom-up, so N is high V.
    w,h = 1024,512
    pixels = [.025,.045,.08,1]*(w*h)
    glyphs = {'X':['10001','01010','00100','01010','10001'],
              'Z':['11111','00010','00100','01000','11111'],
              'N':['10001','11001','10101','10011','10001'],
              'S':['11111','10000','11111','00001','11111'],
              '+':['00100','00100','11111','00100','00100'],
              '-':['00000','00000','11111','00000','00000'],
              '0':['01110','10011','10101','11001','01110'],
              '1':['00100','01100','00100','00100','01110'],
              '2':['11110','00001','01110','10000','11111'],
              '5':['11111','10000','11110','00001','11110'],
              '7':['11111','00010','00100','01000','01000'],
              '.':['00000','00000','00000','00000','00100'],
              'U':['10001','10001','10001','10001','01110']}

    def pixel(x,y,c):
        if 0 <= x < w and 0 <= y < h:
            pixels[(y*w+x)*4:(y*w+x)*4+4] = [*c,1]

    def text(label,x,y,color=(1,1,1),scale=5):
        for char in label:
            for row,bits in enumerate(glyphs[char]):
                for col,bit in enumerate(bits):
                    if bit == '1':
                        for dx in range(scale):
                            for dy in range(scale):
                                pixel(x+col*scale+dx,y+(4-row)*scale+dy,color)
            x += 6*scale

    colors = [(1,.08,.04),(.05,1,.12),(.1,.3,1),(1,.65,.05)]
    for y in range(h):
        for x in range(w):
            if x%256 < 4:
                pixel(x,y,colors[x//256])
            elif y%64 < 2:
                pixel(x,y,(.17,.23,.3))
    for i,label in enumerate(['+X','+Z','-X','-Z']):
        text(label,i*256+12,260,colors[i])
        text(['U0','U.25','U.5','U.75'][i],i*256+12,222,scale=4)
        text('N',i*256+108,466)
        text('S',i*256+108,20,(1,.1,.7))
    image = bpy.data.images.new('UV_Basis_Witness',width=w,height=h,alpha=False)
    image.pixels.foreach_set(pixels)
    image.filepath_raw = str(OUT/'Textures/UV_Basis_Witness.png')
    image.file_format = 'PNG'
    image.save()
    earth = bpy.data.objects['Earth_LOD1']
    for name,loc in [('plus_X',(-4,0,.6)),('plus_Z',(0,-4,.6)),
                     ('minus_X',(4,0,.6)),('minus_Z',(0,4,.6))]:
        render('UV_witness_FBX_X_reflection_candidate_'+name,earth,loc,OUT/'Textures/UV_Basis_Witness.png',True)
    for name,filename,identity in [('Earth','earth_daymap.jpg','planet.sol'),('Mars','mars.jpg','planet.mars')]:
        src = ROOT/'art/asset-production-01/system-v1/Textures'/filename
        dst = OUT/'Textures'/filename
        shutil.copyfile(src,dst)
        texture_sources.append({'path':'Textures/'+filename,'sha256':digest(dst),
            'source_repo_path':src.relative_to(ROOT).as_posix(),'author':'Solar System Scope / INOVE',
            'license':'CC-BY-4.0','source_url':'https://www.solarsystemscope.com/textures/',
            'changes':'None; unchanged existing local source, studio preview only. Runtime borrows current map.'})
        render(name+'_unchanged_UV_studio',bpy.data.objects[name+'_LOD1'],(0,-4,1.2),dst)
    save('Unchanged_System_ReceivingWitness')
else:
    contracts = {}

    def png(path,w,h,rgba,srgb=False):
        def chunk(kind,data):
            return struct.pack('>I',len(data))+kind+data+struct.pack('>I',zlib.crc32(kind+data)&0xffffffff)
        rows = b''.join(b'\0'+rgba[y*w*4:(y+1)*w*4] for y in reversed(range(h)))
        data = b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR',struct.pack('>IIBBBBB',w,h,8,6,0,0,0))
        if srgb:
            data += chunk(b'sRGB',b'\0')
        path.write_bytes(data+chunk(b'IDAT',zlib.compress(rows,9))+chunk(b'IEND',b''))

    def encode_color(x):
        return 12.92*x if x<=.0031308 else 1.055*pow(x,1/2.4)-.055

    def texture(path,linear=False):
        image = bpy.data.images.load(str(path),check_existing=True)
        if linear:
            image.colorspace_settings.name = 'Non-Color'
        return image

    def atlas_material(kind,paths):
        m = material('Material_0')
        p,n,l = m.node_tree.nodes['Principled BSDF'],m.node_tree.nodes,m.node_tree.links
        nodes = {}
        for role,path in paths.items():
            t = n.new('ShaderNodeTexImage')
            t.image = texture(path,role in ['ORM','Normal'])
            nodes[role] = t
        l.new(nodes['BaseColor'].outputs['Color'],p.inputs['Base Color'])
        split = n.new('ShaderNodeSeparateColor')
        l.new(nodes['ORM'].outputs['Color'],split.inputs[0])
        l.new(split.outputs['Green'],p.inputs['Roughness'])
        l.new(split.outputs['Blue'],p.inputs['Metallic'])
        normal = n.new('ShaderNodeNormalMap')
        l.new(nodes['Normal'].outputs['Color'],normal.inputs['Color'])
        l.new(normal.outputs[0],p.inputs['Normal'])
        l.new(nodes['Emission'].outputs['Color'],p.inputs['Emission Color'])
        p.inputs['Emission Strength'].default_value = 2
        return m

    def shader_proxy(paths):
        # Source-formula proxy, not a native render. The exact shader SHA is recorded below.
        m = bpy.data.materials.new('PreviewStudio_CPU_Formula_Proxy_NOT_UNITY')
        m.use_nodes = True
        n,l = m.node_tree.nodes,m.node_tree.links
        n.clear()
        def scalar(op,a,b=0):
            node = n.new('ShaderNodeMath');node.operation = op
            for i,v in enumerate([a,b]):
                if isinstance(v,(int,float)):
                    node.inputs[i].default_value = v
                else:
                    l.new(v,node.inputs[i])
            return node.outputs[0]
        def vector(op,a,b):
            node = n.new('ShaderNodeVectorMath');node.operation = op
            for i,v in enumerate([a,b]):
                if isinstance(v,(tuple,list)):
                    node.inputs[i].default_value = v
                else:
                    l.new(v,node.inputs[i])
            return node.outputs['Value'] if op=='DOT_PRODUCT' else node.outputs['Vector']
        def scale(v,s):
            node=n.new('ShaderNodeVectorMath');node.operation='SCALE'
            l.new(v,node.inputs[0]);l.new(s,node.inputs['Scale'])
            return node.outputs['Vector']
        def to_receiver(v):
            split=n.new('ShaderNodeSeparateXYZ');combine=n.new('ShaderNodeCombineXYZ')
            l.new(v,split.inputs[0]);l.new(scalar('MULTIPLY',split.outputs['X'],-1),combine.inputs['X'])
            l.new(split.outputs['Z'],combine.inputs['Y'])
            l.new(scalar('MULTIPLY',split.outputs['Y'],-1),combine.inputs['Z'])
            return combine.outputs[0]
        geometry=n.new('ShaderNodeNewGeometry')
        normal=to_receiver(geometry.outputs['Normal'])
        view=to_receiver(geometry.outputs['Incoming'])
        key=tuple(Vector((.4,.7,.6)).normalized())
        fill=tuple(Vector((-.6,.25,-.4)).normalized())
        diffuse=scalar('ADD',.24,scalar('ADD',scalar('MULTIPLY',.66,scalar('MAXIMUM',vector('DOT_PRODUCT',normal,key),0)),
            scalar('MULTIPLY',.2,scalar('MAXIMUM',vector('DOT_PRODUCT',normal,fill),0))))
        images={}
        for role in ['BaseColor','ORM','Emission']:
            t=n.new('ShaderNodeTexImage');t.image=texture(paths[role],role=='ORM');images[role]=t.outputs['Color']
        split=n.new('ShaderNodeSeparateColor');l.new(images['ORM'],split.inputs[0])
        smooth=scalar('SUBTRACT',1,split.outputs['Green'])
        half=n.new('ShaderNodeVectorMath');half.operation='NORMALIZE'
        l.new(vector('ADD',view,key),half.inputs[0])
        spec=scalar('POWER',scalar('MAXIMUM',vector('DOT_PRODUCT',normal,half.outputs['Vector']),0),
                    scalar('ADD',8,scalar('MULTIPLY',88,smooth)))
        spec=scalar('MULTIPLY',spec,scalar('ADD',.12,scalar('MULTIPLY',.18,split.outputs['Blue'])))
        rgb=n.new('ShaderNodeCombineXYZ')
        for socket in rgb.inputs:
            l.new(spec,socket)
        final=vector('ADD',vector('ADD',scale(images['BaseColor'],diffuse),rgb.outputs[0]),
                     scale(images['Emission'],scalar('ADD',.9,0)))
        emission=n.new('ShaderNodeEmission');output=n.new('ShaderNodeOutputMaterial')
        l.new(final,emission.inputs['Color']);l.new(emission.outputs[0],output.inputs[0])
        return m

    def hull_render(name,obj,length,width,height,proxy=False):
        scene=bpy.context.scene
        for o in scene.objects:
            if o.type=='MESH':o.hide_render=o!=obj
        obj.hide_set(False)
        cam=scene.camera
        yaw,pitch=math.radians(37.57),math.radians(26)
        direction=Vector((-math.sin(yaw)*math.cos(pitch),-math.cos(yaw)*math.cos(pitch),math.sin(pitch)))
        cam.location=direction*length*3
        cam.rotation_euler=(-cam.location).to_track_quat('-Z','Y').to_euler()
        cam.data.clip_end=length*8
        bpy.context.view_layer.update()
        transform=cam.matrix_world.inverted()@obj.matrix_world
        points=[transform@Vector(co) for co in obj.bound_box]
        lo=Vector([min(v[a] for v in points) for a in range(3)])
        hi=Vector([max(v[a] for v in points) for a in range(3)])
        centre=(lo+hi)/2
        cam.location+=cam.matrix_world.to_3x3()@Vector((centre.x,centre.y,0))
        aspect=width/height
        cam.data.ortho_scale=max(hi.x-lo.x,(hi.y-lo.y)*aspect)*1.12
        scene.render.resolution_x,scene.render.resolution_y=width,height
        scene.render.resolution_percentage=100
        scene.cycles.samples=16;scene.cycles.device='CPU';scene.render.threads_mode='FIXED';scene.render.threads=4
        if proxy:
            scene.view_settings.view_transform='Standard'
            scene.world.node_tree.nodes['Background'].inputs['Color'].default_value=(.018,.028,.045,1)
            scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=1
        scene.render.filepath=str(OUT/'Previews'/(name+'.png'))
        bpy.ops.render.render(write_still=True)

    for kind,identity,length in [('Cruiser','hull.galaxy-cruiser.v1',620),('Escort','hull.defiant-combat.v1',170)]:
        source(ROOT/'art/asset-production-01/hulls-v1/Sources'/(kind+'_OriginalCandidate.blend'))
        names=[kind+'_LOD'+str(i) for i in range(3)]
        keep_meshes(names)
        roles=sorted(m.name for m in bpy.data.objects[names[0]].data.materials)
        factors={}
        for role in roles:
            p=bpy.data.materials[role].node_tree.nodes['Principled BSDF']
            factors[role]={'color':list(p.inputs['Base Color'].default_value),
                'metallic':p.inputs['Metallic'].default_value,'roughness':p.inputs['Roughness'].default_value,
                'emission_color':list(p.inputs['Emission Color'].default_value),
                'emission_strength':p.inputs['Emission Strength'].default_value}
        tile,pad=128,8
        w,h=tile*4,tile*2
        buffers={role:bytearray(w*h*4) for role in ['BaseColor','ORM','Normal','Emission']}
        for index,role in enumerate(roles):
            f=factors[role]
            for y in range(tile):
                for x in range(tile):
                    qx,qy=min(tile-pad-1,max(pad,x)),min(tile-pad-1,max(pad,y))
                    panel=1
                    if role=='Hull_Pearl':
                        # Original restrained panel pattern, no baked illumination or borrowed pixels.
                        panel=.93 if qx%32 in [0,1] or qy%32 in [0,1] else (1 if (qx//32+qy//32)%2 else .985)
                    colors={'BaseColor':[encode_color(max(0,min(1,c*panel))) for c in f['color'][:3]],
                            'ORM':[1,f['roughness'],f['metallic']], 'Normal':[128/255,128/255,1],
                            'Emission':[encode_color(max(0,min(1,c*f['emission_strength']/2))) for c in f['emission_color'][:3]]}
                    offset=((index//4*tile+y)*w+index%4*tile+x)*4
                    for channel,color in colors.items():
                        buffers[channel][offset:offset+4]=bytes([round(c*255) for c in color]+[255])
        paths={role:OUT/'Textures'/(kind+'_'+role+'.png') for role in buffers}
        for role,data in buffers.items():
            png(paths[role],w,h,data,role in ['BaseColor','Emission'])
        atlas=atlas_material(kind,paths)
        for lod,name in enumerate(names):
            obj=bpy.data.objects[name]
            me=obj.data
            original=[m.name for m in me.materials]
            for face in me.polygons:
                index=roles.index(original[face.material_index])
                for li in face.loop_indices:
                    uv=me.uv_layers.active.data[li].uv
                    uv.x=(index%4*tile+pad+max(0,min(1,uv.x))*(tile-pad*2-1)+.5)/w
                    uv.y=(index//4*tile+pad+max(0,min(1,uv.y))*(tile-pad*2-1)+.5)/h
                face.material_index=0
            me.materials.clear();me.materials.append(atlas)
            export(kind+'_Runtime_LOD'+str(lod),obj,identity,lod,
                'Same v1 geometry/bounds, original palette atlas, one opaque Material_0 submesh. '
                'New candidate resources; current binder must explicitly receive atlas/emission and exact identity.')
        contracts[kind]={'candidate_binding':identity,'nominal_art_length_metres':length,'submeshes':1,
            'material_slots':['Material_0'],'original_role_tiles':roles,'original_factors':factors,
            'textures':{k:'Textures/'+v.name for k,v in paths.items()},'atlas_size':[w,h],'gutter_pixels':pad,
            'PreviewStudio':{'_BaseColor':[1,1,1,1],'_Metallic':1,'_Smoothness':.4,
                '_UseMetallicRoughnessMap':1,'_UseNormalMap':1,'_BumpScale':1,'_EmissionColor':[2,2,2,1]},
            'channels':{'BaseColor':'sRGB RGB, alpha opaque','ORM':'linear R=1 unused,G=roughness,B=metallic,A=1',
                'Normal':'linear tangent-space flat +Z; import NormalMap, +Y convention; no invented surface detail',
                'Emission':'sRGB linear-decoded RGB multiplied by HDR (2,2,2) in runtime'},
            'receiving_gap':'Current Defiant binder requires new prefix/credit and explicit EmissionMap/EmissionColor. '
                'Galaxy needs an exact candidate route, not silent replacement of its existing assembly. '
                'The generic studio copier does not copy packed ORM: bind it explicitly or keep that gate open.'}
        top=bpy.data.objects[names[0]]
        hull_render(kind+'_atlas_studio',top,length,1200,800)
        proxy=shader_proxy(paths)
        top.data.materials[0]=proxy
        hull_render(kind+'_PreviewStudio_formula_proxy_512x320',top,length,512,320,True)
        top.data.materials[0]=atlas
        save(kind+'_RuntimeAtlas')
    shader=ROOT/'client/HobbyDS9.Unity/Assets/HobbyDS9/ArtDemo/Resources/ArtDemo/PreviewStudio.shader'
    contracts['proxy_limits']={'shader_source':shader.relative_to(ROOT).as_posix(),'shader_sha256':digest(shader),
        'method':'Blender CPU node evaluation of current normal-based PreviewStudio formula; no normal perturbation from the flat map.',
        'basis':'Candidate Unity FBX X reflection: Blender (-X,+Z,-Y); verify native importer.',
        'camera':'Existing activity defaults yaw37.57/pitch26, projected bounding-box fit with 1.12 margin; 512x320.',
        'not_claimed':'Native Unity render, colour-management parity, workflow/performance or owner acceptance'}
    (OUT/'material-contract.json').write_bytes((json.dumps(contracts,indent=2)+'\n').encode())

m = {'schema':1,'phase':PHASE,'status':'proposed runtime receiving refinement; not Unity acceptance',
     'blender':bpy.app.version_string,'blender_build':bpy.app.build_hash.decode(),
     'inputs':inputs,'texture_sources':texture_sources,'assets':assets,'files':[]}
for p in sorted(OUT.rglob('*')):
    if p.is_file():
        m['files'].append({'path':p.relative_to(OUT).as_posix(),'bytes':p.stat().st_size,'sha256':digest(p)})
(OUT/'manifest.json').write_bytes((json.dumps(m,indent=2)+'\n').encode())
print('RUNTIME_SLICE_COMPLETE',PHASE,flush=True)
