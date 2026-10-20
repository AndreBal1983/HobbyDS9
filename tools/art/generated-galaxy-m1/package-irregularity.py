"""Preserve separately versioned geometry evidence, not physical-feature acceptance."""
from pathlib import Path
import hashlib
import json
import shutil
import sys
import zipfile

after,before,comparisons,target=map(Path,sys.argv[1:]);repo=Path(__file__).resolve().parents[3]
assert not target.exists(),'Never overwrite earlier art evidence'
checks=json.loads((after/'review-checks.json').read_text(encoding='utf-8'));parity=json.loads((after/'parity-checks.json').read_text(encoding='utf-8'));picking=json.loads((after/'scattered-picking-checks.json').read_text(encoding='utf-8'))
assert not checks['errors'] and picking['passed'] and parity['exportDocumentUnchanged']
assert checks['sourceHashes']==json.loads((before/'review-checks.json').read_text(encoding='utf-8'))['sourceHashes']
for item in checks['captures']:assert hashlib.sha256((after/item['file']).read_bytes()).hexdigest()==item['sha256']
shutil.copytree(after,target);shutil.copytree(comparisons,target/'comparison');shutil.copytree(repo/'art/generated-galaxy-m1',target/'source-samples')
shutil.copy2(repo/'docs/art/generated-galaxy-m1/irregularity-correction.md',target/'irregularity-correction.md')
diagnostics=json.loads((comparisons/'layout-diagnostics.json').read_text(encoding='utf-8'));cards=[]
for p in diagnostics['pairs']:
    seed=p['seed'];count=p['systemCount'];suffix='overview'if seed=='42'else'alternate-seed'if seed=='43'else'overview-'+str(count)
    before_name=f'before-seed-{seed}-systems-{count}.png';shutil.copy2(before/f'standard-{suffix}.png',target/'comparison'/before_name)
    cards.append(f'<section><h2>Seed {seed} / {count} systems</h2><a href="comparison/{p["comparisonImage"]}"><img src="comparison/{p["comparisonImage"]}" alt="Same-seed v1 and v2 geometry comparison"></a><p><a href="comparison/{before_name}">Before full resolution</a> · <a href="standard-{suffix}.png">After full resolution</a></p></section>')
html='''<!doctype html><html lang="en"><meta charset="utf-8"><title>M1 irregularity / actual geometry evidence</title><style>body{background:#050a10;color:#eeeaf2;font:18px Segoe UI;margin:28px}a{color:#9fd8fb}p{max-width:1250px;line-height:1.5}img{width:100%;border:1px solid #45566a}.notice{border-left:5px solid #efb16b;background:#101b29;padding:18px;max-width:1250px}section{margin-top:35px}h1,h2{color:#efb16b;font-weight:500}</style><h1>M1 region irregularity / same-seed before and after</h1><div class="notice"><b>Geometry evidence only; not final M1 design acceptance.</b><p>V2 reduces the internal lattice by changing the actual generator placement. It still has no stellar-mass, black-hole or nebula influence. The owner's environment-first direction is not implemented here. No physical-feature overlays were invented.</p></div><p>Same renderer source hashes, viewport, Fit, extents and style. Actual Primary exports: v1 versus2fad182 v2, at42/40,43/40,12345/30 and20261020/50. Original stable bundle is preserved. Empty region counts remain exactly half; area and distribution vary. The rectangular overall extent and convex Voronoi cells remain.</p><p><a href="review.html">All revised states at three sizes</a> · <a href="index.html">Revised sample viewer</a> · <a href="comparison/layout-diagnostics.json">Descriptive layout diagnostics</a> · <a href="parity-checks.json">Exact data parity</a> · <a href="scattered-picking-checks.json">Dense-map picking</a></p>'''+''.join(cards)+'''<p>Browser/CPU art evidence only. Native and generator/version acceptance are separate; no M2 work or package promotion.</p></html>'''
(target/'comparison.html').write_text(html,encoding='utf-8')
archive=target/'HobbyDS9-M1-Irregularity-Geometry-v2.zip'
with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED)as z:
    for p in sorted(target.rglob('*')):
        if p.is_file()and p!=archive:z.write(p,p.relative_to(target))
receipt={'status':'Geometry comparison only; no physical feature influence or final M1 acceptance','backendSource':checks['sourceCommit'],'entry':str(target/'comparison.html'),'defaultContentHash':parity['parity']['contentHash'],'v2Captures':len(checks['captures']),'matchingV1ReferenceCaptures':39,'systemCentresPickedAt1280':sum(x['systemCentresPicked']for x in picking['results']),'candidateBracketsPickedAt1280':sum(x['candidateBracketsPicked']for x in picking['results']),'zipBytes':archive.stat().st_size,'zipSha256':hashlib.sha256(archive.read_bytes()).hexdigest()}
(target/'delivery.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8');print(json.dumps(receipt))
