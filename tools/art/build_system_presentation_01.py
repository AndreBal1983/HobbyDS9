"""Add original presentation accents to the produced system library; Blender background only.

Run once after --phase system. Does not alter original exports/previews/source blend.
"""
import hashlib
import json
import math
import random
import sys
from pathlib import Path
import bpy
import bmesh
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'art/asset-production-01/system-v1'
if (OUT / 'Sources/SystemPresentation.blend').exists() and '--revise-own-output' not in sys.argv:
    raise RuntimeError('Presentation source already exists; refusing overwrite')
bpy.ops.wm.open_mainfile(filepath=str(OUT / 'Sources/SystemBody_Library.blend'))
bpy.context.preferences.filepaths.save_version = 0
scene = bpy.context.scene
camera = scene.camera
for obj in scene.objects:
    if obj.type == 'MESH':
        obj.hide_render = True
        obj.hide_set(True)

def aim(obj, target):
    obj.rotation_euler = (Vector(target)-obj.location).to_track_quat('-Z', 'Y').to_euler()

def render(name, objects, width, height, ortho, loc):
    for obj in scene.objects:
        if obj.type == 'MESH':
            obj.hide_render = True
    for obj in objects:
        obj.hide_render = False
        obj.hide_set(False)
    camera.location = loc
    camera.data.ortho_scale = ortho
    aim(camera, (0, 0, 0))
    scene.render.resolution_x, scene.render.resolution_y = width, height
    scene.render.filepath = str(OUT / 'Previews' / (name+'.png'))
    bpy.ops.render.render(write_still=True)

earth = scene.objects['Earth_LOD0']
earth.rotation_euler.z = math.radians(160)
shell = scene.objects['Atmosphere_Unit']
m = bpy.data.materials.new('Studio_Atmosphere_Fresnel_NOT_GLTF')
m.use_nodes = True
n, links = m.node_tree.nodes, m.node_tree.links
n.clear()
out = n.new('ShaderNodeOutputMaterial')
mix = n.new('ShaderNodeMixShader')
transparent = n.new('ShaderNodeBsdfTransparent')
emission = n.new('ShaderNodeEmission')
emission.inputs['Color'].default_value = (.035, .24, .65, 1)
emission.inputs['Strength'].default_value = .25
weight = n.new('ShaderNodeLayerWeight')
power = n.new('ShaderNodeMath')
power.operation = 'POWER'
power.inputs[1].default_value = 5
links.new(weight.outputs['Facing'], power.inputs[0])
# Facing is zero head-on, one grazing. Only a restrained thin edge is emitted.
links.new(power.outputs[0], mix.inputs[0])
links.new(transparent.outputs[0], mix.inputs[1])
links.new(emission.outputs[0], mix.inputs[2])
links.new(mix.outputs[0], out.inputs[0])
shell.data.materials.clear()
shell.data.materials.append(m)
render('Earth_limb_studio', [earth, shell], 720, 720, 2.65, (0, -4, 1.2))

# Original radial corona: texture alpha fades at its boundary, no borrowed imagery.
size = 256
pixels = []
for y in range(size):
    for x in range(size):
        r = math.hypot((x+.5-size/2)/(size/2), (y+.5-size/2)/(size/2))
        angle = math.atan2(y-size/2, x-size/2)
        alpha = max(0, 1-r)**2 * .5
        alpha *= .9 + .1*math.sin(angle*19+r*22)
        # Sub-8-bit analytic dither prevents concentric alpha quantisation bands.
        alpha += (((x*73+y*151) % 257)/256-.5)/255
        pixels.extend([1, .32+.16*max(0, 1-r), .035, max(0, min(1, alpha)) if r < 1 else 0])
image = bpy.data.images.new('OriginalCorona', width=size, height=size, alpha=True)
image.pixels.foreach_set(pixels)
image.filepath_raw = str(OUT / 'Textures/OriginalCorona.png')
image.file_format = 'PNG'
image.save()
m = bpy.data.materials.new('Corona_Alpha_Emission')
m.use_nodes = True
m.surface_render_method = 'DITHERED'
p = m.node_tree.nodes['Principled BSDF']
p.inputs['Roughness'].default_value = 1
p.inputs['Emission Strength'].default_value = 1
t = m.node_tree.nodes.new('ShaderNodeTexImage')
t.image = image
for output, socket in [('Color', 'Base Color'), ('Color', 'Emission Color'), ('Alpha', 'Alpha')]:
    m.node_tree.links.new(t.outputs[output], p.inputs[socket])
bpy.ops.mesh.primitive_plane_add(size=2)
corona = bpy.context.object
corona.name = 'Corona_Billboard'
corona.data.materials.append(m)
bpy.ops.object.select_all(action='DESELECT')
corona.select_set(True)
bpy.ops.export_scene.gltf(filepath=str(OUT / 'Models/Corona_Billboard.glb'), export_format='GLB',
                          use_selection=True, export_yup=True, export_cameras=False, export_lights=False)
bpy.ops.export_scene.fbx(filepath=str(OUT / 'Models/Corona_Billboard.fbx'), use_selection=True,
                         object_types={'MESH'}, axis_forward='-Z', axis_up='Y', bake_anim=False,
                         add_leaf_bones=False, path_mode='RELATIVE')

# Decorative star backdrop. Seeded, no catalogue, no simulation/system picking identity.
w, h = 1440, 640
background = [0.0] * (w*h*4)
rng = random.Random(13012026)
for _ in range(240):
    x, y = rng.randrange(w), rng.randrange(h)
    brightness = rng.uniform(.06, .4)
    for dx, dy, falloff in [(0, 0, 1), (-1, 0, .15), (1, 0, .15), (0, -1, .15), (0, 1, .15)]:
        px, py = x+dx, y+dy
        if 0 <= px < w and 0 <= py < h:
            k = (py*w+px)*4
            background[k:k+4] = [.5, .65, .9, brightness*falloff]
star_image = bpy.data.images.new('DecorativeStars', width=w, height=h, alpha=True)
star_image.pixels.foreach_set(background)
star_image.filepath_raw = str(OUT / 'Textures/DecorativeStars.png')
star_image.file_format = 'PNG'
star_image.save()

sun, mars = scene.objects['Sun_LOD0'], scene.objects['Mars_LOD0']
sun.location, sun.scale = (-2.65, 0, 0), (.6, .6, .6)
earth.location, earth.scale = (.2, -.55, 0), (.76, .76, .76)
shell.location, shell.scale = earth.location, earth.scale
mars.location, mars.scale = (3.3, .6, 0), (.53, .53, .53)
corona.location = sun.location
corona.scale = (1.3, 1.3, 1.3)
camera.location = (0, -8, 5)
corona.rotation_euler = camera.rotation_euler
aim(camera, (0, 0, 0))
corona.rotation_euler = camera.rotation_euler
corona.location = sun.location - (camera.location-sun.location).normalized()*.8
# Decorative alpha backdrop is visible to camera only; it carries no body identity.
star_m = bpy.data.materials.new('Studio_DecorativeStars')
star_m.use_nodes = True
star_m.surface_render_method = 'DITHERED'
star_p = star_m.node_tree.nodes['Principled BSDF']
star_p.inputs['Emission Strength'].default_value = .6
star_t = star_m.node_tree.nodes.new('ShaderNodeTexImage')
star_t.image = star_image
for output, socket in [('Color', 'Base Color'), ('Color', 'Emission Color'), ('Alpha', 'Alpha')]:
    star_m.node_tree.links.new(star_t.outputs[output], star_p.inputs[socket])
bpy.ops.mesh.primitive_plane_add(size=2, location=(0, 3, -1.875))
backdrop = bpy.context.object
backdrop.name = 'Studio_DecorativeStars'
backdrop.rotation_euler = camera.rotation_euler
backdrop.scale = (4.75, 4.75*640/1440, 1)
backdrop.data.materials.append(star_m)
backdrop.visible_shadow = False
corona.visible_shadow = False
# Mesh orbit bands are presentation-only, shallow oblique, with no labels/picking inferred.
orbits = []
for i, radius in enumerate([2.9, 5.6]):
    o = scene.objects['Orbit_Unit'].copy()
    o.data = o.data.copy()
    scene.collection.objects.link(o)
    o.name = 'Studio_Orbit_' + str(i)
    o.location = sun.location
    o.scale = (radius, radius, radius)
    o.visible_shadow = False
    for j, v in enumerate(o.data.vertices):
        v.co *= (.9992/.997 if j % 2 == 0 else 1.0008/1.003)
    bpy.context.view_layer.update()
    # Presentation gap keeps an orbit from crossing a selected body silhouette.
    # The deliverable unit orbit remains whole; receiving owns lawful body masking/depth.
    bm = bmesh.new()
    bm.from_mesh(o.data)
    occluded = [f for f in bm.faces if any(
        ((o.matrix_world@v.co)-body.location).length < radius
        for v in f.verts for body, radius in [(earth, .94), (mars, .69)])]
    bmesh.ops.delete(bm, geom=occluded, context='FACES_ONLY')
    bm.to_mesh(o.data)
    bm.free()
    orbits.append(o)
render('System_presentation_studio', [earth, shell, mars, sun, corona, backdrop] + orbits,
       1440, 640, 9.5, (0, -8, 5))
for img in bpy.data.images:
    if img.source == 'FILE':
        img.filepath = '//../Textures/' + Path(img.filepath).name
bpy.ops.wm.save_as_mainfile(filepath=str(OUT / 'Sources/SystemPresentation.blend'))
manifest = json.loads((OUT / 'manifest.json').read_text())
manifest['assets'] = [a for a in manifest['assets'] if a['name'] != 'Corona_Billboard']
manifest['assets'].append({'asset_id': 'art.production01.corona_billboard', 'name': 'Corona_Billboard',
    'candidate_binding': None, 'lod': 0,
    'notes': 'Original analytic RGBA texture, camera-facing nonpickable plane; no bloom required. Authoring plane XY normal +Z.',
    'meshes': [{'name': 'Corona_Billboard', 'triangles': 2, 'vertices': 4, 'uv_sets': 1,
                'bounds_blender': [[-1, -1, 0], [1, 1, 0]], 'materials': ['Corona_Alpha_Emission']}],
    'glb': 'Models/Corona_Billboard.glb', 'fbx': 'Models/Corona_Billboard.fbx'})
manifest['files'] = [{'path': p.relative_to(OUT).as_posix(), 'bytes': p.stat().st_size,
                      'sha256': hashlib.sha256(p.read_bytes()).hexdigest()}
                     for p in sorted(OUT.rglob('*')) if p.is_file() and p.name != 'manifest.json']
(OUT / 'manifest.json').write_text(json.dumps(manifest, indent=2)+'\n', encoding='utf-8')
print('PRESENTATION_COMPLETE', flush=True)
