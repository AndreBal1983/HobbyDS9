"""Controlled final light-distance refinement; same asset/pose, no rerun of the other probes."""
import sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from receiving_proof_helpers import *
assert not (OUT/'final-key-manifest.json').exists()
scene,camera=reset()
globe=imported(ASSETS/'Review/Resources/SystemBodyArt/SolMeshes/Earth_LOD1.fbx')
globe.scale=(.7,.7,.7)
key=(-6.3,2.45,-2.8)
for name,file,mars in [('Earth','earth_daymap.jpg',False),('Mars','mars.jpg',True)]:
    path=ASSETS/'Review/Resources/SystemBodyArt'/file
    records.append({'input':path.relative_to(ROOT).as_posix(),'sha256':sha(path)})
    globe.data.materials.clear();globe.data.materials.append(planet(path,key,True,mars))
    render(name+'_receiving_final_key',[globe],(0,2.8,0))
report={'inputs':records,'light_offset_Unity':list(key),'globe_radius':.7,'camera_Unity':[0,0,-2.8],
    'orthographicSize':.875,'cloud_control':0,
    'centre_day_dot':2.1/math.sqrt(6.3**2+2.45**2+2.1**2),
    'note':'Source formula proxy; retains raking direction while avoiding the overly near light of the first proposal. Native acceptance open.',
    'outputs':[{'path':p.name,'bytes':p.stat().st_size,'sha256':sha(p)} for p in sorted(OUT.glob('*_receiving_final_key.png'))]}
(OUT/'final-key-manifest.json').write_bytes((json.dumps(report,indent=2)+'\n').encode())
