"""Build local comparison sheet and exact source/evidence inventory without duplicating legacy assets."""
import hashlib
import html
import json
import shutil
from pathlib import Path

ROOT=Path(__file__).resolve().parents[3]
COORD=Path('C:/HobbyDS9/artifacts/coordination/GUI-POLISH-02')
OUT=COORD/'art-proofs'
reference=Path('C:/HobbyDS9/artifacts/worktrees/SYS-VIS-01-art/artifacts/SYS-VIS-01/coherent-system-1920.png')
shutil.copyfile(reference,OUT/'recovered-concept-system.png')
cards=[('../owner-system-art-copy.png','Owner native before · build/camera not embedded'),
       ('recovered-concept-system.png','Recovered PR60/68 concept · fictional blue globe, not Earth'),
       ('Shared_plane_54deg_proposal.png','Controlled circular plane · elevation54°'),
       ('Shared_plane_24deg_proposal.png','Same geometry/poses · elevation24° · not native picking evidence'),
       ('Earth_baseline_bound_formula.png','Earth · current shader/key · controlled no-cloud case'),
       ('Earth_proposed_same_light.png','Same Earth/pose/current key · candidate shader'),
       ('Earth_receiving_final_key.png','Same Earth/pose · candidate shader + FINAL farther raking key'),
       ('Mars_baseline_bound_formula.png','Mars · current shader/key'),
       ('Mars_receiving_final_key.png','Same Mars/pose · candidate shader/key, no Earth rim'),
       ('../owner-defiant-art-copy.png','Owner Defiant crop · detailed legacy hull'),
       ('Legacy_Defiant_owner_pose_formula_yaw_-1.png','Exact retained Meshy source/maps · low approximate matched pose'),
       ('Legacy_Defiant_owner_pose_formula_yaw_1.png','Same retained source · opposite yaw diagnostic')]
for path,_ in cards:assert (OUT/path).exists(),path
figures=''.join('<figure><img src="'+html.escape(path)+'"><figcaption>'+html.escape(caption)+'</figcaption></figure>' for path,caption in cards)
page='''<!doctype html><meta charset="utf-8"><title>GUI-POLISH-02 Art comparison</title><style>
body{margin:24px;background:#070d17;color:#dbe5ee;font:16px Segoe UI,sans-serif}h1{font-size:25px}p{color:#b1c0d1;max-width:1300px;line-height:1.5}
main{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:16px}figure{margin:0;border:1px solid #27384a;background:#03060a}
img{width:100%;height:280px;object-fit:contain;display:block}figcaption{padding:10px;font-size:13px;min-height:36px}
a{color:#b8d8ff}</style><h1>GUI-POLISH-02 · exact-source receiving comparisons</h1>
<p>14Nov2026. Owner before images are native; controlled shader/geometry probes are Blender CPU formula evidence, NOT Unity after-captures.
Planet controls: same retained960-triangle sphere, same albedo/pose, .7radius/.875ortho/512², no cloud fixture. Final key=(-6.3,2.45,-2.8).
Orbit figures share actual circular3D geometry/poses. Defiant shows retained legacy Meshy model; its source gray/red palette is unchanged.
The approved external palette files remain unreceived. <a href="https://nightfever77.artstation.com/projects/2xm8eA">Recovered primary Klemm reference</a>
is distinct and is not a received numeric material contract. Native compilation/appearance, matched current-state clouds, picking and performance remain open.
Earth/Mars maps: Solar System Scope / INOVE, <a href="https://www.solarsystemscope.com/textures/">source</a>,
<a href="https://creativecommons.org/licenses/by/4.0/">CC BY4.0</a>; unchanged local maps, derived globe proofs.</p><main>'''+figures+'</main>'
(OUT/'comparison.html').write_bytes(page.encode())
files=[]
for directory in [ROOT/'art/gui-polish-02',ROOT/'tools/art/gui-polish-02']:
    for p in sorted(directory.glob('*')):
        if p.is_file():files.append({'path':p.relative_to(ROOT).as_posix(),'sha256':hashlib.sha256(p.read_bytes()).hexdigest(),'bytes':p.stat().st_size})
evidence=[]
for p in sorted(OUT.glob('*')):
    if p.is_file() and p.name!='evidence-manifest.json':evidence.append({'path':p.name,'sha256':hashlib.sha256(p.read_bytes()).hexdigest(),'bytes':p.stat().st_size})
manifest={'date':'2026-11-14','source_input_base':'0de6eee979333d9eabdf4a59cedcfaf85dda7a08',
    'receiving_pin':'see sibling art.md, source payload hashes below avoid circular commit pinning',
    'source_files':files,'evidence_files':evidence,
    'reference':{'source':str(reference),'sha256':hashlib.sha256(reference.read_bytes()).hexdigest(),
        'provenance':'PR60 b3496b6 / PR68 7419313, recovered source reproduction; proposed fictional fixture, not newest standalone render'},
    'artist_reference':{'url':'https://nightfever77.artstation.com/projects/2xm8eA','accessed':'2026-11-14',
        'finding':'Different355-object/22-material435k-polygon asset; no approved local payload or numeric palette recovered'},
    'status':'no native after-image or artistic acceptance; legacy colour correction target remains missing'}
(OUT/'evidence-manifest.json').write_bytes((json.dumps(manifest,indent=2)+'\n').encode())
print('Evidence packaged:',len(files),'source files,',len(evidence),'local evidence files')
