"""Package the existing four proofs without rendering or modifying their pixels."""
import hashlib,json,shutil,sys,zipfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3];p=Path(sys.argv[1]).resolve()
assert (p/'generation.json').exists()
assert not (p/'review.html').exists(),'Do not overwrite an assembled handoff'
for name in ['tools/art/galaxy-nebula-v3/generate.py','tools/art/galaxy-nebula-v3/verify.py','tools/art/galaxy-nebula-v3/package.py',
 'tools/art/nebula-volume-v1/generate.py','docs/art/galaxy-nebula-detail-v3.md','docs/art/galaxy-nebula-detail-v3-receipt.md']:
    dest=p/'source'/name;dest.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(ROOT/name,dest)
images=['01-actual-map-overview.png','02-actual-map-regional.png','03-pan-left.png','04-pan-right.png']
html='''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width"><title>Galaxy / nebula v3 CPU proof</title>
<style>body{margin:30px auto;max-width:1280px;background:#050a11;color:#bdccdb;font:17px/1.5 system-ui}h1{color:#edba78}a{color:#9ed9f2}img{width:100%;height:auto;border:1px solid #364e61}p{max-width:1000px}section{margin:32px 0}</style>
<h1>Detailed galaxy and nebula — one bounded CPU proof</h1><p>Four static views on the actual stored seed42/250 map. Original data; no reference image pixels. 10.965 seconds / 192.3 MiB peak. Proposed receiving assets; no Unity, GPU or final artistic acceptance.</p>
<p>The morphology remains stylised and particles subtle at this exposure. The overview crops a little at the top/bottom (244 actual stars visible). Macro preview uses the labelled integrated fallback; nebula preview uses continuous optical volume integration. No extra render batches.</p>
<p><a href="source/docs/art/galaxy-nebula-detail-v3.md">Frozen asset/renderer contract</a> · <a href="source/docs/art/galaxy-nebula-detail-v3-receipt.md">Review and limitations</a> · <a href="generation.json">Generation receipt</a> · <a href="calibration.json">Brightness comparison</a> · <a href="independent-verification.json">Verification</a></p>
<p>Data: <a href="galaxy-linear-rgba.png">2048² linear macro atlas</a> (A=dust, NOT transparency) · <a href="nebula-linear-rgba.raw">128×96×64 linear volume</a> · <a href="nebula-particles.json">256 depth particle candidates</a></p>'''
for im in images:html+=f'<section><h2>{im}</h2><img src="{im}" alt="{im}"></section>'
(p/'review.html').write_text(html,encoding='utf-8')
payload=[{'file':str(f.relative_to(p)).replace('\\','/'),'bytes':f.stat().st_size,'sha256':hashlib.sha256(f.read_bytes()).hexdigest()}
 for f in sorted(p.rglob('*')) if f.is_file() and f.name not in ['payload-manifest.json','independent-verification.json']]
(p/'payload-manifest.json').write_text(json.dumps(payload,indent=2),encoding='utf-8')
print(json.dumps({'payloads':len(payload),'manifestSha256':hashlib.sha256((p/'payload-manifest.json').read_bytes()).hexdigest()}))
