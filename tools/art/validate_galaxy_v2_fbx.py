"""Fresh background Blender CPU re-import checks; no render or Unity invocation."""
import bpy
import json
import math
import sys
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
PACK = Path(sys.argv[sys.argv.index('--')+1]) if '--' in sys.argv else ROOT/'artifacts/galaxy-v2-prepared'
manifest = json.loads((PACK/'derivative-manifest.json').read_text(encoding='utf-8'))
records=[]
for lod in manifest['lods']:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(PACK/lod['fbx']))
    objects=[o for o in bpy.context.scene.objects if o.type=='MESH']
    assert len(objects)==1
    obj=objects[0];mesh=obj.data;mesh.calc_loop_triangles()
    assert len(mesh.loop_triangles)==lod['triangles'] and len(mesh.materials)==1 and len(mesh.uv_layers)==1
    points=[obj.matrix_world@v.co for v in mesh.vertices]
    size=[max(v[i] for v in points)-min(v[i] for v in points) for i in range(3)]
    assert abs(size[1]-620)<620*.015
    bow=next(o.matrix_world.translation for o in bpy.context.scene.objects if o.name=='Axis_Bow')
    up=next(o.matrix_world.translation for o in bpy.context.scene.objects if o.name=='Axis_Up')
    assert bow.y<0 and abs(bow.x)+abs(bow.z)<.01
    assert up.z>0 and abs(up.x)+abs(up.y)<.01
    assert all(math.isfinite(v) for p in mesh.uv_layers.active.data for v in p.uv)
    assert all(math.isfinite(x) for v in mesh.vertices for x in v.normal)
    mesh.calc_tangents(uvmap=mesh.uv_layers.active.name)
    zero_tangents=sum(loop.tangent.length<1e-6 for loop in mesh.loops)
    assert zero_tangents<=16, ('Unexpected widespread tangent damage',zero_tangents)
    records.append({'level':lod['level'],'triangles':len(mesh.loop_triangles),'vertices':len(mesh.vertices),
        'oneMeshOneMaterial':True,'sizeBlenderXYZMetres':size,'bowBlender':list(bow),'upBlender':list(up),
        'uvFinite':True,'normalsFinite':True,'blenderRecalculatedZeroTangentLoops':zero_tangents,
        'nativeTangentGate':'Unity MikkTSpace generation; explicit finite/unit frame test and UV-gradient fallback if needed'})
    print('GALAXY_FBX_CPU',records[-1],flush=True)
report={'allGeometryChecksPassed':True,'scope':'Fresh Blender FBX re-import, not Unity/graphics acceptance',
    'lods':records,'textureAndTangentGraphicsAcceptance':'pending'}
(PACK/'Validation/fbx-cpu-validation.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
