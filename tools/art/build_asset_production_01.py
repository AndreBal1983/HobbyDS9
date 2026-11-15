"""Original asset production. Run only in Blender background, in a fresh output directory.

blender -b -t 6 --python tools/art/build_asset_production_01.py -- --phase system
Blender 5.3; no network/packages/external models. Inputs are local attributed maps.
"""
import argparse
import hashlib
import json
import math
import shutil
import sys
from pathlib import Path

import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
parser = argparse.ArgumentParser()
parser.add_argument('--phase', choices=['system', 'hulls'], required=True)
args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:])
OUT = ROOT / 'art' / 'asset-production-01' / (args.phase + '-v1')
if OUT.exists():
    raise RuntimeError('Refusing to overwrite production output: ' + str(OUT))
for sub in ['Models', 'Textures', 'Sources', 'Previews']:
    (OUT / sub).mkdir(parents=True, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 24
scene.cycles.use_denoising = True
scene.render.threads_mode = 'FIXED'
scene.render.threads = 6
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.render.film_transparent = False
scene.view_settings.view_transform = 'AgX'
scene.world = bpy.data.worlds.new('StudioWorld')
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (.006, .012, .024, 1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = .18
records = []
sources = []


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def mat(name, color=(.5, .5, .5, 1), metallic=0, rough=.6, emission=0, texture=None):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    p = m.node_tree.nodes.get('Principled BSDF')
    p.inputs['Base Color'].default_value = color
    p.inputs['Metallic'].default_value = metallic
    p.inputs['Roughness'].default_value = rough
    if emission:
        p.inputs['Emission Color'].default_value = color
        p.inputs['Emission Strength'].default_value = emission
    if texture:
        t = m.node_tree.nodes.new('ShaderNodeTexImage')
        t.image = bpy.data.images.load(str(texture), check_existing=True)
        m.node_tree.links.new(t.outputs['Color'], p.inputs['Base Color'])
        if emission:
            m.node_tree.links.new(t.outputs['Color'], p.inputs['Emission Color'])
    return m


def mesh_finish(o, name, material):
    o.name = name
    o.data.name = name + '_mesh'
    bpy.context.view_layer.objects.active = o
    o.select_set(True)
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    if material:
        o.data.materials.append(material)
    for f in o.data.polygons:
        f.use_smooth = True
    return o


def sphere(name, material, segments, rings, scale=(1, 1, 1), loc=(0, 0, 0)):
    bpy.ops.object.select_all(action='DESELECT')
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, location=loc)
    o = bpy.context.object
    o.scale = scale
    # Equirectangular maps use U=0 at the seam, north at V=1; Blender UV sphere agrees.
    return mesh_finish(o, name, material)


def annulus(name, material, inner, outer, segments=192):
    v, faces, uv = [], [], []
    for i in range(segments):
        a = math.tau * i / segments
        for r in (inner, outer):
            v.append((r * math.cos(a), r * math.sin(a), 0))
    for i in range(segments):
        j = (i + 1) % segments
        faces.append((i * 2, i * 2 + 1, j * 2 + 1, j * 2))
    me = bpy.data.meshes.new(name + '_mesh')
    me.from_pydata(v, [], faces)
    me.uv_layers.new(name='UVMap')
    for poly in me.polygons:
        for li, co in zip(poly.loop_indices, [(0, 0), (1, 0), (1, 1), (0, 1)]):
            me.uv_layers[0].data[li].uv = co
    o = bpy.data.objects.new(name, me)
    scene.collection.objects.link(o)
    me.materials.append(material)
    return o


def select_only(objects):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]


def export(name, objects, identity, lod, notes=''):
    select_only(objects)
    stats = []
    for o in objects:
        me = o.data
        me.calc_loop_triangles()
        stats.append({'name': o.name, 'triangles': len(me.loop_triangles),
                      'vertices': len(me.vertices), 'uv_sets': len(me.uv_layers),
                      'bounds_blender': [list(min(v.co[a] for v in me.vertices) for a in range(3)),
                                         list(max(v.co[a] for v in me.vertices) for a in range(3))],
                      'materials': [m.name for m in me.materials]})
    stem = OUT / 'Models' / name
    bpy.ops.export_scene.gltf(filepath=str(stem.with_suffix('.glb')), export_format='GLB',
                              use_selection=True, export_yup=True, export_apply=True,
                              export_cameras=False, export_lights=False, export_extras=True)
    bpy.ops.export_scene.fbx(filepath=str(stem.with_suffix('.fbx')), use_selection=True,
                             object_types={'MESH'}, add_leaf_bones=False, axis_forward='-Z',
                             axis_up='Y', apply_unit_scale=True, global_scale=1,
                             bake_anim=False, use_mesh_modifiers=True, mesh_smooth_type='FACE',
                             path_mode='RELATIVE')
    records.append({'asset_id': 'art.production01.' + name.lower(), 'name': name,
                    'candidate_binding': identity, 'lod': lod, 'notes': notes, 'meshes': stats,
                    'glb': 'Models/' + name + '.glb', 'fbx': 'Models/' + name + '.fbx'})


def hide_all():
    for o in scene.objects:
        if o.type == 'MESH':
            o.hide_render = True


def aim(o, point):
    o.rotation_euler = (Vector(point) - o.location).to_track_quat('-Z', 'Y').to_euler()


def area(name, loc, power, size, color=(1, 1, 1)):
    d = bpy.data.lights.new(name, 'AREA')
    d.energy, d.shape, d.size, d.color = power, 'DISK', size, color
    o = bpy.data.objects.new(name, d)
    scene.collection.objects.link(o)
    o.location = loc
    aim(o, (0, 0, 0))
    return o


cam_data = bpy.data.cameras.new('PresentationCamera')
camera = bpy.data.objects.new('PresentationCamera', cam_data)
scene.collection.objects.link(camera)
scene.camera = camera
cam_data.type = 'ORTHO'
cam_data.clip_end = 10000


def render(name, objects, loc=(0, -4, 1.2), width=720, height=720, ortho=2.65, target=(0, 0, 0)):
    hide_all()
    for o in objects:
        o.hide_render = False
    camera.location = loc
    aim(camera, target)
    camera.data.ortho_scale = ortho
    scene.render.resolution_x, scene.render.resolution_y = width, height
    scene.render.filepath = str(OUT / 'Previews' / (name + '.png'))
    bpy.ops.render.render(write_still=True)
    print('PRODUCTION_PREVIEW', name, flush=True)


def save_source(name):
    # Save external textures using relative paths. No third-party mesh in this scene.
    for image in bpy.data.images:
        if image.source == 'FILE':
            image.filepath = '//../Textures/' + Path(image.filepath).name
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT / 'Sources' / (name + '.blend')))


if args.phase == 'system':
    input_dir = ROOT / 'client/HobbyDS9.Unity/Assets/HobbyDS9/Review/Resources/SystemBodyArt'
    bodies = [('Earth', 'earth_daymap', 'planet.sol'), ('Mars', 'mars', 'planet.mars'),
              ('Sun', 'sun', None), ('Mercury', 'mercury', None),
              ('Venus', 'venus_atmosphere', None), ('Jupiter', 'jupiter', None),
              ('Saturn', 'saturn', None), ('Uranus', 'uranus', None), ('Neptune', 'neptune', None)]
    all_lods, top = [], {}
    key = area('Key', (-3, -4, 3), 170, 2.3, (1, .94, .86))
    fill = area('Fill', (3, 1, 1), 8, 3, (.3, .5, 1))
    for name, filename, identity in bodies:
        src = input_dir / (filename + '.jpg')
        dst = OUT / 'Textures' / src.name
        shutil.copyfile(src, dst)
        sources.append({'path': dst.relative_to(OUT).as_posix(), 'sha256': sha(dst),
                        'source_repo_path': src.relative_to(ROOT).as_posix(),
                        'author': 'Solar System Scope / INOVE', 'license': 'CC-BY-4.0',
                        'source_url': 'https://www.solarsystemscope.com/textures/',
                        'license_verified': '2026-11-13', 'changes': 'None; exact-byte copy'})
        m = mat(name + '_Surface', rough=.82, texture=dst, emission=1 if name == 'Sun' else 0)
        for lod, (seg, rings) in enumerate([(96, 48), (48, 24), (24, 12)]):
            o = sphere(name + '_LOD' + str(lod), m, seg, rings)
            export(o.name, [o], identity, lod,
                   'Unit radius, centred pivot; reference-only body' if identity is None else
                   'Exact known planet presentation candidate; no simulation change')
            all_lods.append(o)
            if lod == 0:
                top[name] = o
        # Rotate the longitude shown in the studio; geometry/UV remain unrotated in exports.
        top[name].rotation_euler.z = math.radians(160 if name == 'Earth' else 35)
        render(name + '_studio', [top[name]])
    ring_m = mat('Saturn_Rings', (.48, .40, .27, 1), rough=.9)
    ring_objects = []
    # Original separate concentric bands, not a copied ring texture.
    for i, (a, b) in enumerate([(1.24, 1.48), (1.53, 1.89), (1.93, 2.18)]):
        ring_objects.append(annulus('Saturn_RingBand_' + str(i), ring_m, a, b))
    export('Saturn_Rings', ring_objects, None, 0, 'Reference-only, two-sided URP material; 1 unit body radius')
    render('Saturn_rings_studio', [top['Saturn']] + ring_objects,
           loc=(0, -5, 3.7), ortho=5)
    orbit_m = mat('Orbit_Tint', (.08, .18, .27, 1), rough=1, emission=.25)
    orbit = annulus('Orbit_Unit', orbit_m, .997, 1.003, 256)
    export('Orbit_Unit', [orbit], None, 0,
           'Nonpickable schematic orbit. Use screen-space line if this mesh aliases; no gameplay collider')
    shell = sphere('Atmosphere_Unit', mat('Atmosphere_Placeholder', (.02, .15, .35, 1)), 48, 24,
                   scale=(1.018, 1.018, 1.018))
    export('Atmosphere_Unit', [shell], 'planet.sol', 1,
           'Optional separate mesh, disabled by default. Requires receiving Fresnel alpha shader; solid material is placeholder')
    # Flat conservative reference: same albedo/mesh, direct sun, no atmosphere/cloud or post bloom.
    fill.data.energy = 0
    key.data.type = 'SUN'
    key.data.energy = 2
    key.data.angle = math.radians(1)
    render('Earth_direct_light_reference', [top['Earth']])
    render('Mars_direct_light_reference', [top['Mars']])
    # Illustrative three-body overview, explicitly not a campaign snapshot or scale simulation.
    key.data.energy = 2.4
    for name, pos, s in [('Sun', (-3.3, 0, 0), .57), ('Earth', (.2, -.55, 0), .76),
                         ('Mars', (3.3, .5, 0), .53)]:
        o = top[name]
        o.location = pos
        o.scale = (s, s, s)
    render('System_three_body_composition', [top['Sun'], top['Earth'], top['Mars']],
           loc=(0, -8, 5), width=1440, height=640, ortho=9.2)
    # Reset asset transforms for the editable library; presentation rig stays in the blend.
    for o in all_lods:
        o.location = (0, 0, 0)
        o.scale = (1, 1, 1)
        o.rotation_euler = (0, 0, 0)
    hide_all()
    top['Earth'].hide_render = False
    save_source('SystemBody_Library')

else:
    # Original silhouettes built only from the analytic geometry below.
    # No scan, imported model, image tracing or approved-replacement geometry.
    pearl = mat('Hull_Pearl', (.48, .56, .64, 1), .35, .36)
    panel = mat('Hull_Slate', (.15, .21, .28, 1), .5, .42)
    dark = mat('Recess_Graphite', (.018, .035, .055, 1), .3, .55)
    trim = mat('Trim_Titanium', (.55, .58, .57, 1), .65, .28)
    blue = mat('Engine_Blue', (.025, .28, .8, 1), .15, .28, 2)
    red = mat('Collector_Red', (.8, .035, .012, 1), .2, .3, 1.5)
    windows = mat('Windows_Warm', (.8, .55, .25, 1), .1, .4, .65)
    gold = mat('Sensor_Bronze', (.36, .23, .1, 1), .65, .4)

    def box(name, center, size, material, bevel=0):
        bpy.ops.object.select_all(action='DESELECT')
        bpy.ops.mesh.primitive_cube_add(size=1, location=center)
        o = bpy.context.object
        o.scale = size
        mesh_finish(o, name, material)
        if bevel:
            mod = o.modifiers.new('Manufactured edge', 'BEVEL')
            # Stay below half the thin dimension: a clamped bevel can collapse edge faces.
            mod.width, mod.segments = min(bevel, min(size)*.45), 2
            bpy.ops.object.modifier_apply(modifier=mod.name)
        for p in o.data.polygons:
            p.use_smooth = False
        return o

    def saucer(name, center, rx, ry, profile, segments, material):
        # Closed axisymmetric lenticular plate; poles have unique triangles, no degeneracies.
        vertices = [(center[0], center[1], center[2] + profile[0][1])]
        for radius, z in profile[1:-1]:
            for j in range(segments):
                a = math.tau * j / segments
                vertices.append((center[0] + rx * radius * math.cos(a),
                                 center[1] + ry * radius * math.sin(a), center[2] + z))
        vertices.append((center[0], center[1], center[2] + profile[-1][1]))
        faces = []
        rings = len(profile) - 2
        for j in range(segments):
            faces.append((0, 1+j, 1+(j+1) % segments))
        for k in range(rings - 1):
            for j in range(segments):
                a, b = 1 + k*segments+j, 1+k*segments+(j+1) % segments
                faces.append((a, a+segments, b+segments, b))
        for j in range(segments):
            faces.append((len(vertices)-1, 1+(rings-1)*segments+(j+1) % segments,
                          1+(rings-1)*segments+j))
        me = bpy.data.meshes.new(name + '_mesh')
        me.from_pydata(vertices, [], faces)
        me.update()
        me.uv_layers.new(name='UVMap')
        for p in me.polygons:
            p.use_smooth = True
            for li in p.loop_indices:
                co = me.vertices[me.loops[li].vertex_index].co
                me.uv_layers[0].data[li].uv = ((co.x-center[0])/(2*rx)+.5, (co.y-center[1])/(2*ry)+.5)
        o = bpy.data.objects.new(name, me)
        scene.collection.objects.link(o)
        me.materials.append(material)
        return o

    def join_normalize(parts, name, length):
        select_only(parts)
        bpy.ops.object.join()
        o = bpy.context.object
        bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
        # Authored origin is geometric bounds centre; nominal metre length matches existing preview contract.
        lo = Vector([min(v.co[a] for v in o.data.vertices) for a in range(3)])
        hi = Vector([max(v.co[a] for v in o.data.vertices) for a in range(3)])
        centre = (lo+hi)/2
        factor = length / (hi.y-lo.y)
        for v in o.data.vertices:
            v.co = (v.co-centre)*factor
        o.name, o.data.name = name, name + '_mesh'
        return o

    def capsule_y(name, center, rx, rz, length, segments, material):
        cap = min(14, length*.3)
        o = saucer(name, (0, 0, 0), rx, rz,
                   [(0, length/2), (.75, length/2-cap*.3), (1, length/2-cap),
                    (1, -length/2+cap), (.75, -length/2+cap*.3), (0, -length/2)],
                   segments, material)
        o.rotation_euler.x = math.pi/2
        o.location = center
        return o

    area('Key', (-400, -420, 550), 2800000, 430, (.82, .91, 1))
    area('Warm_Fill', (350, -80, 160), 1100000, 350, (1, .78, .56))
    area('Rim', (50, 450, 330), 3800000, 300, (.2, .5, 1))
    for kind, identity, length in [('Cruiser', 'hull.galaxy-cruiser.v1', 620),
                                    ('Escort', 'hull.defiant-combat.v1', 170)]:
        lods = []
        for lod, seg in enumerate([96, 48, 24]):
            parts = []
            profile = [(0, 17), (.24, 16), (.53, 10), (.86, 3), (1, 0),
                       (1, -5), (.85, -9), (.45, -12), (0, -13)]
            if kind == 'Cruiser':
                parts.append(saucer('Primary_hull', (0, -135, 40), 157, 140, profile, seg, pearl))
                parts.append(saucer('Bridge_deck', (0, -120, 56), 48, 40,
                                    [(0, 10), (.5, 9), (1, 2), (1, 0), (0, -2)], seg//2, panel))
                parts.append(sphere('Bridge', pearl, seg//2, 12, (16, 23, 7), (0, -125, 68)))
                parts.append(sphere('Engineering', pearl, seg//2, 16, (45, 148, 28), (0, 65, -16)))
                parts.append(box('Dorsal_spine', (0, -5, 15), (24, 120, 43), pearl, 5))
                parts.append(sphere('Deflector_recess', dark, seg//2, 12, (25, 5, 17), (0, -66, -18)))
                parts.append(sphere('Deflector', gold, seg//2, 12, (19, 5, 12), (0, -70, -18)))
                parts.append(sphere('Deflector_core', blue, 16, 8, (9, 3, 6), (0, -74, -18)))
                for side in [-1, 1]:
                    pylon = box('Swept_pylon', (side*88, 100, 0), (125, 34, 13), panel, 4)
                    pylon.rotation_euler.y = side * math.radians(-15)
                    pylon.rotation_euler.z = side * math.radians(-18)
                    parts.append(pylon)
                    parts.append(capsule_y('Nacelle', (side*154, 115, 27), 21, 18, 256, seg//2, pearl))
                    parts.append(capsule_y('Nacelle_cap', (side*154, -16, 27), 18, 14, 20, seg//2, red))
                    parts.append(box('Nacelle_grille', (side*174.5, 123, 29), (2, 170, 10), blue, .7))
                    parts.append(box('Nacelle_rail', (side*174, 122, 38), (3, 176, 3), trim, 1))
                if lod < 2:
                    for j in range(48 if lod == 0 else 24):
                        a = math.tau*j/(48 if lod == 0 else 24)
                        # Small luminous apertures along the rim, illustrative rather than sockets.
                        o = box('Rim_window', (157.1*math.cos(a), -135+140.1*math.sin(a), 37.5),
                                (3.3, 1.1, 2), windows)
                        o.rotation_euler.z = a+math.pi/2
                        parts.append(o)
                    for side in [-1, 1]:
                        for y in [-45, -18, 9, 36, 63, 90, 117]:
                            x = 45*math.sqrt(1-((y-65)/148)**2-(6/28)**2)+.1
                            parts.append(box('Engineering_window', (side*x, y, -10), (1, 4, 2), windows))
                # Recessed sensor annuli break the primary hull's broad surface with original geometry.
                for radius in ([.72, .88] if lod < 2 else [.82]):
                    ring = annulus('Saucer_sensor_band', dark, radius-.008, radius+.008, seg)
                    ring.scale = (157, 140, 1)
                    ring.location = (0, -135, 40 + (8 if radius < .8 else 3))
                    parts.append(ring)
            else:
                # Compact, low centre body with swept side pods; deliberately an original candidate.
                parts.append(saucer('Escort_primary', (0, -4, 0), 49, 77,
                                    [(0, 13), (.38, 12), (.7, 8), (1, 0), (.95, -5),
                                     (.55, -10), (0, -10)], seg, pearl))
                parts.append(box('Dorsal_deck', (0, -2, 11), (40, 67, 9), panel, 2))
                parts.append(sphere('Bridge', pearl, seg//2, 10, (12, 18, 5), (0, -24, 18)))
                parts.append(box('Spine', (0, 34, 13), (9, 42, 8), trim, 2))
                for side in [-1, 1]:
                    parts.append(capsule_y('Flank_pod', (side*47, 11, -1), 15, 12, 98, seg//2, pearl))
                    parts.append(sphere('Intake_recess', dark, seg//2, 10, (11, 5, 8), (side*47, -37, 0)))
                    parts.append(sphere('Intake_collector', red, seg//2, 10, (8, 3, 5), (side*47, -41, 0)))
                    parts.append(box('Engine_grille', (side*62.1, 18, 0), (.7, 46, 7), blue, .3))
                    parts.append(box('Pod_shoulder', (side*44, 18, 11), (18, 42, 3), panel, 1.5))
                    if lod < 2:
                        for y in [-18, -6, 6, 18]:
                            parts.append(box('Deck_window', (side*20.1, y, 11), (.7, 4, 1.2), windows))
                        for y in [2, 12, 22, 32]:
                            parts.append(box('Pod_vent', (side*47, y, 12.7), (9, 2.5, .8), dark))
                parts.append(sphere('Bow_sensor', gold, seg//2, 8, (12, 4, 5), (0, -76, -1)))
            o = join_normalize(parts, kind + '_LOD' + str(lod), length)
            export(o.name, [o], identity, lod,
                   'PROPOSED original procedural candidate, not the unreceived approved external replacement; no sockets/stats')
            lods.append(o)
        render(kind + '_three_quarter_studio', [lods[0]], loc=(length*.85, -length*.95, length*.7),
               width=1200, height=800, ortho=length*1.25)
        render(kind + '_top_reference', [lods[0]], loc=(0, 0, length*2),
               width=800, height=1000, ortho=length*1.16)
        render(kind + '_side_reference', [lods[0]], loc=(length*2, 0, length*.08),
               width=1200, height=600, ortho=length*1.16)
        for obj in scene.objects:
            if obj.type == 'MESH':
                obj.hide_set(obj != lods[0])
        # Save each source independently, keeping the other candidate's objects in its own library only.
        save_source(kind + '_OriginalCandidate')
        for obj in list(scene.objects):
            if obj.type == 'MESH':
                bpy.data.objects.remove(obj, do_unlink=True)


manifest = {'schema': 1, 'phase': args.phase, 'status': 'proposed original asset candidate; native receiving outstanding',
            'generator': 'tools/art/build_asset_production_01.py',
            'blender': bpy.app.version_string, 'blender_build': bpy.app.build_hash.decode(),
            'coordinates': {'blender': 'metres, +Z up; hull bow -Y',
                            'gltf': 'metres, +Y up; hull bow +Z',
                            'fbx': 'metres; axis_forward=-Z, axis_up=Y; verify receiving root TRS'},
            'texture_sources': sources, 'assets': records,
            'files': []}
for p in sorted(OUT.rglob('*')):
    if p.is_file():
        manifest['files'].append({'path': p.relative_to(OUT).as_posix(), 'bytes': p.stat().st_size,
                                  'sha256': sha(p)})
(OUT / 'manifest.json').write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8')
print('PRODUCTION_COMPLETE', str(OUT), flush=True)
