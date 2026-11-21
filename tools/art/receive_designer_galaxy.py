"""Receive an existing verified CPU art pack. No Unity, Blender or image generation.
The destination must be absent. Preserve source bytes and explicit material roles.
"""
import argparse,hashlib,json,shutil,uuid
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('--source',type=Path,required=True);p.add_argument('--repo',type=Path,required=True);p.add_argument('--extend-existing',action='store_true');a=p.parse_args()
source=a.source.resolve();repo=a.repo.resolve()
dest=repo/'client/HobbyDS9.Unity/Assets/HobbyDS9/ArtDemo/Resources/ArtDemo/Designer/Galaxy'
assert not dest.exists() or a.extend_existing,'Preserve existing receiving versions'
if dest.exists():
    previous=json.loads((dest/'receiving-inventory.json').read_text())
    for entry in previous['files']:
        f=dest/entry['path'];assert hashlib.sha256(f.read_bytes()).hexdigest()==entry['sha256'],entry['path']
contract=json.loads((source/'asset-contract.json').read_text());manifest=json.loads((source/'delivery-manifest.json').read_text())
selected={x['path']:x for x in manifest['files']}
parts={'GalaxyHull':0,'GalaxyHullCutaway':1,'SovereignNacelle_Port':0,'SovereignNacelle_Starboard':0,'WarpCore':0,'Deflector_Blue_concentric':0,'GalaxyNacelle_Port':1,'GalaxyNacelle_Starboard':1,'Deflector_Amber_oval':1,'Deflector_Segmented_survey':1}
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def guid(relative):return uuid.uuid5(uuid.NAMESPACE_URL,'hobbyds9/designer-galaxy-v1/'+relative).hex
def write(path,text):path.parent.mkdir(parents=True,exist_ok=True);path.write_text(text,encoding='utf-8',newline='\n')
def meta(path,body=''):
    write(Path(str(path)+'.meta'),'fileFormatVersion: 2\nguid: '+guid(path.relative_to(dest.parent).as_posix())+'\n'+body)
def copy(relative):
    src=source/relative
    if relative!='delivery-manifest.json':
        entry=selected[relative];assert sha(src)==entry['sha256'] and src.stat().st_size==entry['bytes'],relative
    out=dest/relative;out.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(src,out);return out
dest.mkdir(parents=True,exist_ok=True)
modeltemplate=(repo/'client/HobbyDS9.Unity/Assets/HobbyDS9/ArtDemo/Resources/ArtDemo/Federation/Models/exploration-cruiser_LOD0.fbx.meta').read_text()
for name,lod in parts.items():
    data=contract['assets'][name]['lods'][lod];out=copy(data['fbx'])
    remap='externalObjects:\n'
    for role in data['materials']:
        matguid=guid('Galaxy/Materials/'+role+'.mat')
        remap+='  - first:\n      type: UnityEngine:Material\n      assembly: UnityEngine.CoreModule\n      name: '+role+'\n    second: {fileID: 2100000, guid: '+matguid+', type: 2}\n'
    text=modeltemplate.replace(modeltemplate.splitlines()[1],'guid: '+guid('Galaxy/'+data['fbx'])).replace('externalObjects: {}',remap.rstrip())
    write(Path(str(out)+'.meta'),text)
for entry in contract['textures']:
    out=copy(entry['path']);meta(out,'TextureImporter:\n  externalObjects: {}\n  serializedVersion: 13\n  mipmaps:\n    enableMipMap: 1\n  isReadable: 0\n  sRGBTexture: 1\n  maxTextureSize: 2048\n  textureType: 0\n  textureCompression: 1\n  alphaIsTransparency: 0\n  textureSettings:\n    serializedVersion: 2\n    filterMode: 2\n    aniso: 1\n    mipBias: 0\n    wrapU: 0\n    wrapV: 0\n    wrapW: 0\n')
for role,m in contract['materials'].items():
    out=dest/'Materials'/f'{role}.mat';texture=m['baseColorTexture'];textureid='{fileID: 0}' if texture is None else '{fileID: 2800000, guid: '+guid('Galaxy/'+texture)+', type: 3}'
    color=m['baseColorLinear'];emission=[c*m['emissionStrength'] for c in m['emissionLinear']]
    tex=''
    for key,val in [('_BaseMap',textureid),('_BumpMap','{fileID: 0}'),('_EmissionMap','{fileID: 0}')]:
        tex+=f'    - {key}:\n        m_Texture: {val}\n        m_Scale: {{x: 1, y: 1}}\n        m_Offset: {{x: 0, y: 0}}\n'
    write(out,'%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!21 &2100000\nMaterial:\n  serializedVersion: 8\n  m_ObjectHideFlags: 0\n  m_Name: '+role+'\n  m_Shader: {fileID: 4800000, guid: 6ae380f4425f5ce7984d46c8c519a336, type: 3}\n  m_ValidKeywords: []\n  m_InvalidKeywords: []\n  m_LightmapFlags: 2\n  m_EnableInstancingVariants: 0\n  m_DoubleSidedGI: 0\n  m_CustomRenderQueue: -1\n  stringTagMap: {}\n  disabledShaderPasses: []\n  m_SavedProperties:\n    serializedVersion: 3\n    m_TexEnvs:\n'+tex+'    m_Ints: []\n    m_Floats:\n    - _Metallic: '+str(m['metallic'])+'\n    - _Smoothness: '+str(m['smoothness'])+'\n    - _BumpScale: 1\n    - _UseNormalMap: 0\n    m_Colors:\n    - _BaseColor: {r: '+str(color[0])+', g: '+str(color[1])+', b: '+str(color[2])+', a: '+str(color[3])+'}\n    - _EmissionColor: {r: '+str(emission[0])+', g: '+str(emission[1])+', b: '+str(emission[2])+', a: 1}\n')
    meta(out,'NativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 2100000\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n')
for name in ['asset-contract.json','delivery-manifest.json','ATTRIBUTION.txt','SOURCE-LICENSE.html','RECEIVING.txt']:
    out=copy(name);meta(out,'TextScriptImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n')
for folder in [dest.parent,dest,dest/'Models',dest/'Materials',dest/'Textures']:
    mp=Path(str(folder)+'.meta')
    if not mp.exists():write(mp,'fileFormatVersion: 2\nguid: '+guid('folder/'+folder.name)+'\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n')
payloads=[{'path':f.relative_to(dest).as_posix(),'bytes':f.stat().st_size,'sha256':sha(f)} for f in sorted(dest.rglob('*')) if f.is_file()]
proof={'source':str(source),'sourceContractSha256':sha(source/'asset-contract.json'),'selectedParts':parts,'allSelectedResourceTriangles':sum(contract['assets'][k]['lods'][lod]['triangles'] for k,lod in parts.items()),'defaultVisibleTriangles':75534,'materials':len(contract['materials']),'textures':len(contract['textures']),'files':[x for x in payloads if x['path'] not in ['receiving-inventory.json','receiving-inventory.json.meta']],'runtimeImportAndTextureFidelity':'not yet validated in Unity'}
write(dest/'receiving-inventory.json',json.dumps(proof,indent=2));meta(dest/'receiving-inventory.json','TextScriptImporter:\n  externalObjects: {}\n')
print(json.dumps({k:v for k,v in proof.items() if k!='files'}))
