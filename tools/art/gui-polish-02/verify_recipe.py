"""Focused source-contract/data checks, not a shader compiler or native rendering test."""
import ast
import hashlib
import json
import math
import re
from pathlib import Path

ROOT=Path(__file__).resolve().parents[3]
PACK=ROOT/'art/gui-polish-02'
ASSETS=ROOT/'client/HobbyDS9.Unity/Assets/HobbyDS9'
recipe=json.loads((PACK/'receiving-recipe.json').read_text())
old=(ASSETS/'Review/Resources/SystemBodyArt/ProceduralSurface.shader').read_text()
new=(PACK/'PlanetSurfaceLighting.shader').read_text()
fail=[]
def check(ok,label):
    if not ok:fail.append(label)
properties=lambda s:set(re.findall(r'^\s*(_\w+)\s*\(',s,re.M))
check(properties(old)<=properties(new),'existing property ABI')
check(re.search(r'Shader\s+"([^"]+)"',old)[1]==re.search(r'Shader\s+"([^"]+)"',new)[1],'same shader identity')
check(len(re.findall(r'\bPass\s*\{',new))==1,'one shader pass')
check(new.count('{')==new.count('}'),'balanced shader braces')
check(new.index('if(_Lit>.5 && _Unknown>.5)')<new.index('half3 albedo=SAMPLE_TEXTURE2D'),'unknown early neutral branch')
check('saturate(_Atmosphere)' in new and 'limbLight' in new,'bounded lit-side atmosphere')
root=ASSETS/'ArtDemo/Resources/ArtDemo/ActivityHulls/Defiant'
source=json.loads((root/'source-contract.json').read_text())
for i,image in enumerate(source['images']):
    path=root/'Textures'/('Defiant_Image_'+str(i)+'.jpg')
    check(hashlib.sha256(path.read_bytes()).hexdigest()==image['sha256'],'Defiant source map '+str(i))
    meta=Path(str(path)+'.meta').read_text()
    check(('sRGBTexture: '+('1' if i==0 else '0')) in meta,'Defiant colour space '+str(i))
    check(('textureType: '+('1' if i==2 else '0')) in meta,'Defiant texture role '+str(i))
check(not any('Emission' in l['toSocket'] for l in source['materialLinks']),'no recorded source emission link')
check(source['lods'][0]['triangles']==29998 and source['lods'][0]['materials']==['Material_0'],'legacy mesh identity contract')
for p in Path(__file__).parent.glob('*.py'):ast.parse(p.read_text())
angles=recipe['orbit_camera']
check(abs(math.sin(math.radians(angles['proposed_elevation_degrees']))-angles['proposed_projected_circle_minor_major'])<1e-9,'projection ratio')
key=recipe['planet_shader']['selected_portrait']['presentation_light_offset_world']
# Centre surface is (0,0,-.7), normal (0,0,-1); use the positional light, not an infinite-light approximation.
def actual_centre_dot(k):return -(k[2]+.7)/math.sqrt(k[0]**2+k[1]**2+(k[2]+.7)**2)
old_dot=actual_centre_dot([-3,2,-4]);new_dot=actual_centre_dot(key)
check(.25<new_dot<.35 and new_dot<old_dot,'raking face-centre light')
report={'passed':not fail,'failures':fail,'shader_sha256':hashlib.sha256((PACK/'PlanetSurfaceLighting.shader').read_bytes()).hexdigest(),
    'centre_day_dot':{'retained':old_dot,'candidate':new_dot},
    'pure_albedo_day_night_contrast':{'retained':1/.16,'overview':(1.15+.04)/.04,'portrait':(1.15+.025)/.025},
    'source_checks':'property ABI/name, pass count, unknown gating, retained legacy map hashes/roles/colour spaces/emission link record, Python syntax, projection/key math',
    'not_executed':['Unity shader compilation','native pixels/input/picking','GPU timing/memory','approved replacement palette recovery']}
print(json.dumps(report,indent=2))
raise SystemExit(1 if fail else 0)
