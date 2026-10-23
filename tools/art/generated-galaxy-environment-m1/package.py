"""Freeze a new versioned M1 art review without replacing earlier deliveries."""
from pathlib import Path
import hashlib
import json
import shutil
import sys
import zipfile

rendered, comparisons, target = map(Path,sys.argv[1:])
repo=Path(__file__).resolve().parents[3]
assert not target.exists(), 'Never overwrite earlier review evidence'
checks=json.loads((rendered/'review-checks.json').read_text(encoding='utf-8'))
parity=json.loads((rendered/'feature-parity-checks.json').read_text(encoding='utf-8'))
diagnostics=json.loads((comparisons/'controlled-comparison.json').read_text(encoding='utf-8'))
assert not checks['errors'] and parity['passed']
for item in checks['captures']:
    assert hashlib.sha256((rendered/item['file']).read_bytes()).hexdigest()==item['sha256']
shutil.copytree(rendered,target)
shutil.copytree(comparisons,target/'comparison')
shutil.copytree(repo/'art/generated-galaxy-m1/environment-v3',target/'source-samples')
shutil.copytree(repo/'tools/art/generated-galaxy-environment-m1',target/'renderer-source')
shutil.copy2(repo/'docs/art/generated-galaxy-m1/environment-v3/presentation-contract.md',target/'presentation-contract.md')
handoff=repo/'docs/art/generated-galaxy-m1/environment-v3/handoff.md'
if handoff.exists():shutil.copy2(handoff,target/'handoff.md')
cards=[]
summary_rows=[]
for pair in diagnostics['pairs']:
    label=pair['variant']
    cards.append(f'<section><h2>{label} / same features, changed generation influence</h2><a href="comparison/{pair["comparisonImage"]}"><img src="comparison/{pair["comparisonImage"]}" alt="Actual control and normal map comparison"></a><p><a href="standard-control-{label}.png">Control export full resolution</a> · <a href="standard-overview.png">Normal export full resolution</a></p></section>')
    core=pair.get('primaryHalfWeightCoreComparison')
    if core:summary_rows.append(f'<tr><td>{core["Family"]}</td><td>{core["DisabledLocalExtraSites"]}</td><td>{core["EnabledLocalExtraSites"]}</td></tr>')
html='''<!doctype html><html lang="en"><meta charset="utf-8"><title>Environment-first M1 / controlled feature evidence</title><style>body{background:#050a10;color:#eeeaf2;font:18px Segoe UI;margin:28px}a{color:#9fd8fb}p{max-width:1250px;line-height:1.5}img{width:100%;border:1px solid #45566a}section{margin-top:35px}.notice{border-left:5px solid #efb16b;background:#101b29;padding:18px;max-width:1250px}h1,h2{color:#efb16b;font-weight:500}</style><h1>Environment-first M1 / controlled feature comparison</h1><div class="notice"><b>Actual seeded exports and their persisted features.</b><p>Stars and black holes anchor real navigation sites before additional sites are placed. Nebulae contribute through real influence ellipses. These are provisional generation inputs, not physical masses, sensor coverage, hazards or ownership. Site-density observations explain placement pressure, not the unique cause or exact area of an individual polygon.</p></div><p>Each comparison holds seed, map settings, all feature positions/parameters, tuning, renderer, Fit and display size fixed. Only the stated persisted influence switches differ. The fixed total region budget redistributes sites, so distant polygons can change too. Earlier v1/v2 geometry-only reviews remain preserved.</p><p><a href="review.html">All states / three display sizes</a> · <a href="index.html">Interactive actual-sample viewer</a> · <a href="comparison/controlled-comparison.json">Exact identities and descriptive observations</a> · <a href="feature-parity-checks.json">Picking / export / data checks</a> · <a href="tokens.json">Reusable tokens</a></p>'''+''.join(cards)+'''<p>Art/browser evidence only. Native Unity acceptance, portable generator checks and owner review are separate. No live campaign, sensors, exploration progression, AI or M2 work.</p></html>'''
(target/'comparison.html').write_text(html,encoding='utf-8')
summary='<h2>Seed 42 / local density comparison</h2><p>Extra sites inside the selected family\'s half-weight influence cores; mandatory star and black-hole anchors are excluded. These counts come from Primary\'s hash-pinned causal report. The images retain the full influence footprints.</p><table cellpadding="10"><tr><th>Family</th><th>Influence disabled</th><th>Influence enabled</th></tr>'+''.join(summary_rows)+'</table><p>Across the full 17-seed report, the direction is also toward greater local extra-site density for each family. It is not a per-seed or per-cell guarantee. <a href="source-samples/samples/causal-comparisons.json">Read the exact measurement, caveat and all source comparisons.</a></p>'
html=html.replace('<p>Art/browser evidence only.',summary+'<p>Art/browser evidence only.')
(target/'comparison.html').write_text(html,encoding='utf-8')
receipt={
 'status':'Environment-first M1 art/browser review; native and owner acceptance separate',
 'backendCommit':checks['backendCommit'],'entry':str(target/'comparison.html'),
 'captures':len(checks['captures']),'controlledPairs':len(diagnostics['pairs']),
 'systemCentresPicked':sum(r['systemsPicked'] for r in parity['results']),
 'featureInspections':sum(r['featuresIndependentlyInspected'] for r in parity['results']),
 'priorV1V2Preserved':True
}
payload=[]
for p in sorted(target.rglob('*')):
    if p.is_file():payload.append({'file':p.relative_to(target).as_posix(),'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()})
(target/'payload-manifest.json').write_text(json.dumps(payload,indent=2)+'\n',encoding='utf-8')
archive=target/'HobbyDS9-M1-Environment-Art-v3.zip'
with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED)as z:
    for p in sorted(target.rglob('*')):
        if p.is_file()and p!=archive:z.write(p,p.relative_to(target))
receipt.update(zipBytes=archive.stat().st_size,zipSha256=hashlib.sha256(archive.read_bytes()).hexdigest())
(target/'delivery.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')
print(json.dumps(receipt))
