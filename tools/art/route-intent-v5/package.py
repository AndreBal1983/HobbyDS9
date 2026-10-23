"""Freeze checked browser evidence; preserve all previous deliveries."""
import hashlib,json,shutil,subprocess,sys,zipfile
from pathlib import Path
root=Path(__file__).resolve().parents[3];target=Path(sys.argv[1]).resolve();assert not target.exists()
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
route=root/'artifacts/art/route-intent-v5-run2';natural=root/'artifacts/art/natural-v5-comparison-run1'
assert read(route/'independent-proof.json')['passed'] and read(natural/'independent-proof.json')['passed']
for source,receipt in [(route,'review-checks.json'),(natural,'natural-checks.json')]:
    for c in read(source/receipt)['captures']:assert sha(source/c['file'])==c['sha256']
target.mkdir(parents=True)
shutil.copytree(route,target/'routes',ignore=shutil.ignore_patterns('checkpoint.json'))
shutil.copytree(natural,target/'natural')
for rel in ['tools/art/route-intent-v5','art/generated-galaxy-m1/route-intent-v5','art/generated-galaxy-m1/scale-v4','docs/art/generated-galaxy-m1/route-intent-v5']:
    shutil.copytree(root/rel,target/'source'/rel)
pin=subprocess.check_output(['git','rev-parse','HEAD'],cwd=root,text=True).strip()
(target/'review.html').write_text('''<!doctype html><html lang="en"><meta charset="utf-8"><title>M1 route and natural distribution candidate</title><style>body{background:#050b12;color:#e9e8ef;font:18px Segoe UI;margin:30px;max-width:1600px}a{color:#9fd8fb}img{width:100%}.notice{border-left:6px solid #efb16b;padding:20px;background:#101c29}p{line-height:1.5}</style><h1>M1 route and natural distribution / candidate review</h1><div class="notice"><strong>Route visual acceptance remains open.</strong> Contained midpoint drawing still produces conspicuous doglegs near the start and midroute. This is evidence for a separately proved corridor-straightening follow-up, not a claim that the path is visually finished or globally taut.</div><p><a href="routes/review.html">36 route states / three viewport sizes</a> · <a href="routes/index.html">Interactive API fixture viewer</a> · <a href="routes/independent-proof.json">Independent route proof</a></p><p>Route-choice comparison holds map/endpoints/filter/drawing fixed: old/new both22crossings; centre-span proxy4,123,357→3,139,347, not line length or time. Drawing-only comparison holds the new corridor fixed. Ordered explicit anchors, repeated/zero legs and atomic failure are retained.</p><img src="routes/standard-new-policy-transit.png" alt="Actual route candidate with unresolved start and midroute doglegs"><h2>Actual old/new star distribution</h2><p><a href="natural/review.html">Six matched overview/detail captures</a> · <a href="natural/natural.html">Interactive paired maps</a> · <a href="natural/independent-proof.json">Independent raw-source/DOM proof</a> · <a href="natural/comparison-index.json">Measured metrics and exact per-file source pins</a></p><p>Review250systems seed42and43before1000seed42. Same seed/count/extent/camera/style; different map identities and generated geography. Actual isolated stars25/25/100and varied provenance arities2–9. New quiet/system mean-area ratios2.3024/2.7254/2.7101exceed the soft2×target. No jittered old triples, invented features or galaxy-spanning corridor claim.</p><img src="natural/seed-42-250-overview.png" alt="Actual format3/4 matched250seed42maps"><p><a href="source/docs/art/generated-galaxy-m1/route-intent-v5/handoff.md">Full handoff and limits</a> · <a href="payload-manifest.json">Immutable payload hashes</a></p><p>CPU headless browser/source evidence only; no native/Unity/GPU, campaign, AI, sensors or M2 work. Safe913and earlier galleries remain preserved.</p></html>''',encoding='utf-8')
payload=[{'file':str(p.relative_to(target)).replace('\\','/'),'bytes':p.stat().st_size,'sha256':sha(p)} for p in sorted(target.rglob('*')) if p.is_file()]
manifest=target/'payload-manifest.json';manifest.write_text(json.dumps(payload,indent=2),encoding='utf-8')
archive=target/'HobbyDS9-M1-Route-Natural-Art-v5.zip'
with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED) as z:
    for f in payload:z.write(target/f['file'],f['file'])
    z.write(manifest,manifest.name)
receipt={'sourcePin':pin,'status':'Candidate browser evidence; route visual dogleg issue remains open','payloadFiles':len(payload),'payloadManifestSha256':sha(manifest),'zipBytes':archive.stat().st_size,'zipSha256':sha(archive),'routeCaptures':36,'naturalCaptures':6,'paidGenerationUsed':False}
(target/'delivery.json').write_text(json.dumps(receipt,indent=2),encoding='utf-8');print(json.dumps(receipt))
