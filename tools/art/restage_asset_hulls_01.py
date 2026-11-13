"""Render a higher inspection angle from delivered editable sources, unchanged geometry."""
from pathlib import Path
import sys
import bpy
from mathutils import Vector
ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT/'art/asset-production-01/hulls-v1'
for name, length in [('Cruiser', 620), ('Escort', 170)]:
    bpy.ops.wm.open_mainfile(filepath=str(OUT/'Sources'/(name+'_OriginalCandidate.blend')))
    scene = bpy.context.scene
    for obj in scene.objects:
        if obj.type == 'MESH':
            obj.hide_render = obj.name != name+'_LOD0'
            obj.hide_set(obj.hide_render)
    camera = scene.camera
    camera.data.clip_end = length*8
    bpy.context.preferences.filepaths.save_version = 0
    views = [('three_quarter_studio', (length*.65, -length*.90, length*1.15), 1200, 800, 1.25),
             ('top_reference', (0, 0, length*2), 800, 1000, 1.16),
             ('side_reference', (length*2, 0, length*.08), 1200, 600, 1.16)]
    if '--hero-only' in sys.argv:
        views = views[:1]
    for view, location, width, height, scale in views:
        camera.location = location
        camera.rotation_euler = (-camera.location).to_track_quat('-Z', 'Y').to_euler()
        bpy.context.view_layer.update()
        # Fit the actual mesh in camera space, including its asymmetric projected centre.
        mesh = scene.objects[name+'_LOD0']
        transform = camera.matrix_world.inverted()@mesh.matrix_world
        projected = [transform@v.co for v in mesh.data.vertices]
        lo = Vector([min(v[a] for v in projected) for a in range(3)])
        hi = Vector([max(v[a] for v in projected) for a in range(3)])
        centre = (lo+hi)/2
        camera.location += camera.matrix_world.to_3x3()@Vector((centre.x, centre.y, 0))
        aspect = width/height
        camera.data.ortho_scale = (max(hi.x-lo.x, (hi.y-lo.y)*aspect) if aspect >= 1 else
                                  max(hi.y-lo.y, (hi.x-lo.x)/aspect))*1.12
        scene.render.resolution_x, scene.render.resolution_y = width, height
        scene.render.filepath = str(OUT/'Previews'/(name+'_'+view+'.png'))
        bpy.ops.render.render(write_still=True)
        if view == 'three_quarter_studio':
            bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Sources'/(name+'_Studio.blend')))
            bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Sources'/(name+'_OriginalCandidate.blend')))
