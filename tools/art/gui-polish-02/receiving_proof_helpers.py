"""GUI-POLISH-02: actual retained assets, matched-pose shader/source-material probes.

Blender CPU only. Output is local review evidence, not Unity/native acceptance.
Unknown-license legacy geometry/textures are referenced in place, never re-exported.
"""
import hashlib
import json
import math
from pathlib import Path
import bpy
from mathutils import Vector, Matrix

ROOT=Path(__file__).resolve().parents[3]
OUT=Path('C:/HobbyDS9/artifacts/coordination/GUI-POLISH-02/art-proofs')
OUT.mkdir(parents=True,exist_ok=True)
ASSETS=ROOT/'client/HobbyDS9.Unity/Assets/HobbyDS9'
records=[]

def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()

def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.device='CPU';scene.cycles.samples=16
    scene.render.threads_mode='FIXED';scene.render.threads=4
    scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG'
    scene.view_settings.view_transform='Standard'
    scene.world=bpy.data.worlds.new('Constant background');scene.world.use_nodes=True
    scene.world.node_tree.nodes['Background'].inputs['Color'].default_value=(.003,.006,.012,1)
    scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=1
    camera=bpy.data.objects.new('Probe camera',bpy.data.cameras.new('Probe camera'))
    scene.collection.objects.link(camera);scene.camera=camera;camera.data.type='ORTHO';camera.data.clip_end=10000
    return scene,camera

def imported(path):
    bpy.ops.import_scene.fbx(filepath=str(path),use_image_search=False)
    objects=[o for o in bpy.context.scene.objects if o.type=='MESH']
    assert len(objects)==1,'Expected one delivered mesh'
    obj=objects[0]
    matrix=obj.matrix_world.copy();obj.data.transform(matrix);obj.parent=None;obj.matrix_world=Matrix.Identity(4)
    obj.data.calc_loop_triangles()
    records.append({'input':path.relative_to(ROOT).as_posix(),'sha256':sha(path),'triangles':len(obj.data.loop_triangles)})
    return obj

def image(path,linear=False):
    im=bpy.data.images.load(str(path),check_existing=True)
    if linear:im.colorspace_settings.name='Non-Color'
    return im

class Nodes:
    def __init__(self,name):
        self.m=bpy.data.materials.new(name);self.m.use_nodes=True
        self.n=self.m.node_tree.nodes;self.n.clear();self.l=self.m.node_tree.links
    def scalar(self,op,a,b=0):
        n=self.n.new('ShaderNodeMath');n.operation=op
        for i,v in enumerate([a,b]):
            if isinstance(v,(float,int)):n.inputs[i].default_value=v
            else:self.l.new(v,n.inputs[i])
        return n.outputs[0]
    def vector(self,op,a,b=(0,0,0)):
        n=self.n.new('ShaderNodeVectorMath');n.operation=op
        for i,v in enumerate([a,b]):
            if isinstance(v,(list,tuple,Vector)):n.inputs[i].default_value=v
            else:self.l.new(v,n.inputs[i])
        return n.outputs['Value'] if op=='DOT_PRODUCT' else n.outputs['Vector']
    def scale(self,v,s):
        n=self.n.new('ShaderNodeVectorMath');n.operation='SCALE'
        if isinstance(v,(list,tuple)):n.inputs[0].default_value=v
        else:self.l.new(v,n.inputs[0])
        if isinstance(s,(int,float)):n.inputs['Scale'].default_value=s
        else:self.l.new(s,n.inputs['Scale'])
        return n.outputs['Vector']
    def receiver(self,value):
        split=self.n.new('ShaderNodeSeparateXYZ');combine=self.n.new('ShaderNodeCombineXYZ')
        self.l.new(value,split.inputs[0])
        self.l.new(self.scalar('MULTIPLY',split.outputs['X'],-1),combine.inputs['X'])
        self.l.new(split.outputs['Z'],combine.inputs['Y'])
        self.l.new(self.scalar('MULTIPLY',split.outputs['Y'],-1),combine.inputs['Z'])
        return combine.outputs[0]
    def texture(self,path,linear=False):
        n=self.n.new('ShaderNodeTexImage');n.image=image(path,linear);return n.outputs['Color']
    def finish(self,color):
        emission=self.n.new('ShaderNodeEmission');out=self.n.new('ShaderNodeOutputMaterial')
        self.l.new(color,emission.inputs['Color']);self.l.new(emission.outputs[0],out.inputs[0]);return self.m
    def clamp(self,a):return self.scalar('MINIMUM',1,self.scalar('MAXIMUM',0,a))

def planet(path,light_position,new=False,mars=False,night=.025):
    q=Nodes('Proposed formula proxy' if new else 'Retained formula proxy')
    geo=q.n.new('ShaderNodeNewGeometry');normal=q.receiver(geo.outputs['Normal'])
    light=q.vector('NORMALIZE',q.vector('SUBTRACT',light_position,q.receiver(geo.outputs['Position'])))
    signed=q.vector('DOT_PRODUCT',normal,light);day=q.clamp(signed)
    facing=q.clamp(q.vector('DOT_PRODUCT',normal,q.receiver(geo.outputs['Incoming'])))
    rim=q.scalar('POWER',q.scalar('SUBTRACT',1,facing),5 if new else 3)
    albedo=q.texture(path)
    lit=q.scalar('ADD',night if new else .16,q.scalar('MULTIPLY',1.15 if new else .84,day))
    color=q.scale(albedo,lit)
    # Controlled no-cloud case isolates lighting; actual runtime cloud mask/seed is not available from owner crop.
    if new:
        t=q.clamp(q.scalar('DIVIDE',q.scalar('ADD',signed,.12),.4))
        smooth=q.scalar('MULTIPLY',q.scalar('MULTIPLY',t,t),q.scalar('SUBTRACT',3,q.scalar('MULTIPLY',2,t)))
        gate=q.scalar('MULTIPLY',smooth,q.scalar('ADD',.1,q.scalar('MULTIPLY',.9,q.scalar('SQRT',day))))
        limb=q.scalar('MULTIPLY',rim,q.scalar('MULTIPLY',0 if mars else .7*.12,gate))
        color=q.vector('ADD',color,q.scale((.055,.24,.70),limb))
    else:color=q.vector('ADD',color,q.scale((.08,.19,.29),q.scalar('MULTIPLY',rim,.7)))
    return q.finish(color)

def render(name,objects,camera_location,target=(0,0,0),width=512,height=512,scale=1.75):
    scene=bpy.context.scene;camera=scene.camera
    for o in scene.objects:
        if o.type=='MESH':o.hide_render=o not in objects
    camera.location=camera_location;camera.rotation_euler=(Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler()
    camera.data.ortho_scale=scale
    scene.render.resolution_x,scene.render.resolution_y=width,height
    scene.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True)
    print('PROOF',name,flush=True)
