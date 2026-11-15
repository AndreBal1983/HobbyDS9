"""Original bounded belt geometry/recipe and Blender-only illustration; no campaign RNG.

Blender 5.3 background CPU. Outputs only art/system-richness-01/belt-v1.
"""
import hashlib
import json
import math
from pathlib import Path
import bpy
from mathutils import Euler, Vector

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'art/system-richness-01/belt-v1'
if (OUT/'manifest.json').exists():
    raise RuntimeError('Version already exists; use a new version for revisions')
for name in ['Models', 'Sources', 'Previews', 'Shaders']:
    (OUT/name).mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
bpy.context.preferences.filepaths.save_version = 0

def write(name, value):
    (OUT/name).write_text(json.dumps(value, indent=2)+'\n', encoding='utf-8', newline='\n')

def seed(text):
    s = 2166136261
    for b in text.encode('utf-8'):
        s = ((s ^ b)*16777619) & 0xffffffff
    return s or 1

class Draws:
    def __init__(self, s): self.s = s
    def unit(self):
        x = self.s
        x ^= (x << 13) & 0xffffffff
        x ^= x >> 17
        x ^= (x << 5) & 0xffffffff
        self.s = x & 0xffffffff
        return (self.s >> 8)/16777216.0

seed_text = 'belt-v1\nsystem.example\nfeature.belt.example'
random = Draws(seed(seed_text))
points = []
for i in range(320):
    # Exactly eleven unsigned draws per rock, in a fixed order.
    a = [random.unit() for _ in range(11)]
    theta = (i*.6180339887498949 + .12*(a[0]-.5)) % 1
    # Triangle radial density softens both edges; avoid a rigid beaded circle.
    points.append({'index': i, 'angleTurns': theta, 'radial01': (a[1]+a[2])*.5,
                   'heightOverMidRadius': .012*(a[3]-.5),
                   'radiusOverMidRadius': .006+.010*a[4]*a[4],
                   'prototype': min(3, int(a[5]*4)),
                   'rotationRadiansXYZ': [a[6]*math.tau, a[7]*math.tau, a[8]*math.tau],
                   'stretchXYZ': [.8+.5*a[9], .7+.4*a[10], .75+.35*a[2]],
                   'tone': .68+.28*a[10]})
write('layout-example.json', {'schema': 1, 'visualVersion': 'belt-v1', 'seedText': seed_text,
                            'seedU32': seed(seed_text), 'points': points})

def material(name, color, emission=0):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    p = m.node_tree.nodes['Principled BSDF']
    p.inputs['Base Color'].default_value = (*color, 1)
    p.inputs['Metallic'].default_value = 0
    p.inputs['Roughness'].default_value = .92
    p.inputs['Emission Color'].default_value = (*color, 1)
    p.inputs['Emission Strength'].default_value = emission
    return m

rock_mat = material('Belt_Rock_VertexColor', (.25, .23, .20), .015)
attribute = rock_mat.node_tree.nodes.new('ShaderNodeVertexColor')
attribute.layer_name = 'RockTone'
rock_mat.node_tree.links.new(attribute.outputs['Color'], rock_mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'])
prototypes = {}
exports = []

def export(obj, filename):
    bpy.ops.object.select_all(action='DESELECT')
    obj.hide_set(False)
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.export_scene.fbx(filepath=str(OUT/'Models'/filename), use_selection=True,
                             object_types={'MESH'}, axis_forward='-Z', axis_up='Y',
                             use_mesh_modifiers=True, add_leaf_bones=False, bake_anim=False,
                             path_mode='AUTO', colors_type='LINEAR')
    exports.append({'path': 'Models/'+filename, 'triangles': sum(len(p.vertices)-2 for p in obj.data.polygons),
                    'vertices': len(obj.data.vertices), 'materialSlots': len(obj.data.materials)})

for lod, sub in [(0, 2), (1, 1)]:
    for variant in range(4):
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=sub, radius=1)
        obj = bpy.context.object
        obj.name = f'Belt_Rock_{variant}_LOD{lod}'
        for v in obj.data.vertices:
            x, y, z = v.co
            perturb = 1+.14*math.sin(x*9+variant*1.7)*math.cos(z*7-y*5)
            v.co = (x*perturb, y*perturb*(.72+.07*variant), z*perturb*(.82+.04*variant))
        # Explicitly bound every prototype inside the unit sphere.
        bound = max(v.co.length for v in obj.data.vertices)
        for v in obj.data.vertices: v.co /= bound
        obj.data.materials.append(rock_mat)
        colors = obj.data.color_attributes.new(name='RockTone', type='FLOAT_COLOR', domain='CORNER')
        for c in colors.data: c.color = (.30, .28, .24, 1)
        prototypes[lod, variant] = obj
        export(obj, obj.name+'.fbx')
        obj.hide_render = True
        obj.hide_set(True)

def bake(lod, count):
    verts, faces, tones = [], [], []
    for row in points[:count]:
        mid = 1.12
        r = 1+.24*row['radial01']
        t = row['angleTurns']*math.tau
        center = Vector((r*math.cos(t), r*math.sin(t), mid*row['heightOverMidRadius']))
        rotation = Euler(row['rotationRadiansXYZ'], 'XYZ').to_matrix()
        proto = prototypes[lod, row['prototype']].data
        offset = len(verts)
        for v in proto.vertices:
            scaled = Vector(tuple(v.co[j]*row['stretchXYZ'][j] for j in range(3)))
            verts.append(rotation @ (scaled*(mid*row['radiusOverMidRadius']))+center)
        for f in proto.polygons:
            faces.append(tuple(offset+i for i in f.vertices))
            tones.append(row['tone'])
    mesh = bpy.data.meshes.new(f'Belt_Example_LOD{lod}')
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(mesh.name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    mesh.materials.append(rock_mat)
    colors = mesh.color_attributes.new(name='RockTone', type='FLOAT_COLOR', domain='CORNER')
    for poly, tone in zip(mesh.polygons, tones):
        for loop in poly.loop_indices: colors.data[loop].color = (.34*tone, .30*tone, .25*tone, 1)
    export(obj, obj.name+'.fbx')
    return obj

near = bake(0, 320)
mid = bake(1, 120)
mid.hide_render = True
mid.hide_set(True)

# Single broad low-opacity annulus: UV U=turns, V=radial01, no Saturn bands.
verts, faces = [], []
for i in range(193):
    t = math.tau*i/192
    for r in (1, 1.24): verts.append((r*math.cos(t), r*math.sin(t), 0))
for i in range(192):
    k = i*2
    faces.extend([(k,k+1,k+3), (k,k+3,k+2)])
mesh = bpy.data.meshes.new('Belt_SoftAnnulus')
mesh.from_pydata(verts, [], faces)
mesh.uv_layers.new(name='UVMap')
for p in mesh.polygons:
    for loop in p.loop_indices:
        index = mesh.loops[loop].vertex_index
        mesh.uv_layers.active.data[loop].uv = ((index//2)/192, index%2)
annulus = bpy.data.objects.new(mesh.name, mesh)
bpy.context.scene.collection.objects.link(annulus)
dust = material('Belt_SoftAnnulus', (.35, .31, .25), .6)
nodes, links = dust.node_tree.nodes, dust.node_tree.links
uv = nodes.new('ShaderNodeTexCoord')
sep = nodes.new('ShaderNodeSeparateXYZ')
links.new(uv.outputs['UV'], sep.inputs[0])
def op(operation, x, y=None):
    node = nodes.new('ShaderNodeMath'); node.operation = operation
    if isinstance(x, (int, float)): node.inputs[0].default_value = x
    else: links.new(x, node.inputs[0])
    if y is not None:
        if isinstance(y, (int, float)): node.inputs[1].default_value = y
        else: links.new(y, node.inputs[1])
    return node.outputs[0]
radial = op('POWER', op('SINE', op('MULTIPLY', sep.outputs['Y'], math.pi)), 1.4)
grain = op('MULTIPLY', op('SINE', op('ADD', op('MULTIPLY', sep.outputs['X'], math.tau*37), .6)),
           op('SINE', op('MULTIPLY', sep.outputs['X'], math.tau*11)))
alpha = op('MULTIPLY', op('MULTIPLY', radial, op('ADD', .70, op('MULTIPLY', grain, .30))), .065)
links.new(alpha, nodes['Principled BSDF'].inputs['Alpha'])
dust.surface_render_method = 'DITHERED'
mesh.materials.append(dust)
export(annulus, 'Belt_SoftAnnulus.fbx')

scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 16
scene.cycles.use_denoising = True
scene.world = bpy.data.worlds.new('DeepSpace')
scene.world.use_nodes = True
bg = scene.world.node_tree.nodes['Background']
bg.inputs['Color'].default_value = (.012, .018, .030, 1)
bg.inputs['Strength'].default_value = .15
scene.view_settings.view_transform = 'AgX'
scene.render.image_settings.file_format = 'PNG'
scene.render.resolution_percentage = 100
scene.render.film_transparent = False

def aim(obj, at): obj.rotation_euler = (Vector(at)-obj.location).to_track_quat('-Z','Y').to_euler()
def light(kind, name, location, energy, size):
    data = bpy.data.lights.new(name, kind); data.energy = energy
    if kind == 'AREA': data.shape = 'DISK'; data.size = size
    else: data.shadow_soft_size = size
    obj = bpy.data.objects.new(name, data); scene.collection.objects.link(obj)
    obj.location = location; aim(obj, (0,0,0))
light('AREA', 'Illustration_Readability_Key', (-6,-10,14), 2400, 15)
light('POINT', 'Illustration_Central_Star_Light', (0,0,1.3), 2600, 1)
bpy.ops.object.camera_add()
camera = bpy.context.object
camera.name = 'Illustration_Camera'
camera.data.type = 'ORTHO'
scene.camera = camera

# Illustration fixtures only: no runtime body identities or generated facts.
fixtures = []
maps = ROOT/'art/asset-production-01/system-v1/Textures'
for i, (name, radius, distance, angle) in enumerate([
        ('sun',1.15,0,0), ('mars',.56,3.1,-28), ('earth_daymap',.7,5.5,142),
        ('jupiter',1,8.1,35), ('neptune',.78,16.8,224)]):
    t = math.radians(angle)
    bpy.ops.mesh.primitive_uv_sphere_add(segments=48, ring_count=24, radius=radius,
        location=(distance*math.cos(t), distance*math.sin(t), 0))
    obj = bpy.context.object; obj.name = 'ILLUSTRATION_ONLY_'+name
    mat = material(obj.name, (1,1,1), 1 if name == 'sun' else .03)
    texture = mat.node_tree.nodes.new('ShaderNodeTexImage')
    texture.image = bpy.data.images.load(str(maps/(name+'.jpg')), check_existing=True)
    principled = mat.node_tree.nodes['Principled BSDF']
    mat.node_tree.links.new(texture.outputs['Color'], principled.inputs['Base Color'])
    mat.node_tree.links.new(texture.outputs['Color'], principled.inputs['Emission Color'])
    obj.data.materials.append(mat)
    for p in obj.data.polygons: p.use_smooth = True
    fixtures.append(obj)
    if distance:
        curve = bpy.data.curves.new('Illustration_Orbit', 'CURVE'); curve.dimensions = '3D'
        curve.bevel_depth = .009; curve.bevel_resolution = 0
        spline = curve.splines.new('POLY'); spline.points.add(191)
        for j, point in enumerate(spline.points):
            theta = j/192*math.tau
            point.co = (distance*math.cos(theta), distance*math.sin(theta), -.08, 1)
        spline.use_cyclic_u = True
        track = bpy.data.objects.new('ILLUSTRATION_ONLY_Orbit', curve)
        scene.collection.objects.link(track)
        curve.materials.append(material('Illustration_Orbit', (.025,.05,.08), .7))
        fixtures.append(track)
near.scale = annulus.scale = (10,10,10)

def render(name, location, target, width, height, ortho):
    camera.location = location; aim(camera,target); camera.data.ortho_scale = ortho
    scene.render.resolution_x, scene.render.resolution_y = width,height
    scene.render.filepath = str(OUT/'Previews'/name)
    bpy.ops.render.render(write_still=True)

render('overview-24deg.png', (0,-40,40*math.tan(math.radians(24))), (0,0,0), 1440,650,48)
for obj in fixtures: obj.hide_render = True
annulus.hide_render = True
render('belt-detail.png', (0,-16,10), (0,-10.8,0), 1200,650,9)
for obj in fixtures: obj.hide_render = False
annulus.hide_render = False
camera.location = (0,-40,40*math.tan(math.radians(24)))
aim(camera,(0,0,0)); camera.data.ortho_scale = 48
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Sources/AsteroidBelt_v1.blend'))
bpy.ops.file.make_paths_relative()
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Sources/AsteroidBelt_v1.blend'))
write('geometry-validation.json', {'schema': 1, 'assets': exports,
    'seedU32': seed(seed_text), 'rockCountLOD0':320, 'rockCountLOD1':120,
    'assertions': {'finiteVertices': all(math.isfinite(v) for obj in (near,mid,annulus) for vert in obj.data.vertices for v in vert.co),
                   'prototypeMaxRadiusAtMostOne': all(max(v.co.length for v in obj.data.vertices) < 1.00001 for obj in prototypes.values()),
                   'onlyOriginalGeometry': True}})
files = []
for p in sorted(OUT.rglob('*')):
    if p.is_file():
        b = p.read_bytes()
        files.append({'path':p.relative_to(OUT).as_posix(), 'bytes':len(b), 'sha256':hashlib.sha256(b).hexdigest()})
write('manifest.json', {'schema':1, 'assetVersion':'asteroid-belt-v1', 'date':'2026-11-15',
    'status':'original art candidate; Blender preview only; Unity receiving pending',
    'generator':'tools/art/build_asteroid_belt_v1.py', 'blender':bpy.app.version_string,
    'blenderBuild':bpy.app.build_hash.decode(), 'provenance':{
        'geometry':'Original procedural mesh generation for HobbyDS9; no external model/texture input.',
        'materials':'Original factor/vertex-color rocks and analytic annulus; no new raster textures.',
        'previewOnlyMaps':'Existing Solar System Scope / INOVE CC BY 4.0 maps from system-v1; not belt dependencies.',
        'licenceURL':'https://creativecommons.org/licenses/by/4.0/',
        'mapSourceURL':'https://www.solarsystemscope.com/textures/'},
    'coordinates':{'authoring':'Blender XY orbital plane, +Z up; origin central star',
        'fbx':'axis_forward=-Z axis_up=Y; receiver verifies centered Unity XZ plane and positive scale',
        'radius':'Unit annulus inner1 outer1.24; prototype sphere bound1; baked example is a fixture'},
    'geometry':exports, 'files':files})
print('BELT_V1_COMPLETE', str(OUT))
