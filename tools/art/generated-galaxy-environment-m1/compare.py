"""Descriptive controlled evidence from actual persisted exports; no generator here."""
from pathlib import Path
import hashlib
import json
import statistics
import sys
from PIL import Image, ImageDraw, ImageFont

index_path, rendered, target = map(Path, sys.argv[1:])
assert not target.exists(), 'Preserve previous comparisons'
target.mkdir(parents=True)
index = json.loads(index_path.read_text(encoding='utf-8'))
causal_path=index_path.parent/index['causalReport']['document']
assert hashlib.sha256(causal_path.read_bytes()).hexdigest()==index['causalReport']['sha256']
causal=json.loads(causal_path.read_text(encoding='utf-8'))
items = [(i,json.loads((index_path.parent/i['document']).read_text(encoding='utf-8'))) for i in index['samples']]
base_item, base = next((i,d) for i,d in items if i['variant']=='normal' and d['Settings']['Map']['Seed']=='42' and d['Settings']['Map']['SystemCount']==40)
report = {'status':'Controlled same-feature generation comparisons; descriptive local observations, not balance thresholds', 'backendCommit':index['backendCommit'], 'primaryCausalMeasurement':causal['Measurement'],'primaryCausalCaveat':causal['Caveat'],'primary17SeedTotals':causal['Totals'],'pairs':[]}
font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',22)
small=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',17)
def area(r):
    b=r['Boundary']
    return abs(sum(a['X']*c['Y']-c['X']*a['Y'] for a,c in zip(b,b[1:]+b[:1])))/2
def inside(p,f):
    return ((p['X']-f['Position']['X'])/f['RadiusX'])**2+((p['Y']-f['Position']['Y'])/f['RadiusY'])**2<1
def observations(d,kinds):
    features=[f for f in d['Features'] if f['Kind'] in kinds]
    mandatory={f['NodeId'] for f in d['Features'] if f['NodeId']}
    eligible=[r for r in d['Layout']['Regions'] if r['NodeId'] not in mandatory]
    local=[r for r in eligible if any(inside(r['Center'],f) for f in features)]
    areas=[area(r) for r in local]
    return {'additionalSitesInsideAffectedSupportUnion':len(local),'totalAdditionalSites':len(eligible),'meanLocalCellArea':statistics.mean(areas) if areas else None,'allCellAreaCV':statistics.pstdev([area(r) for r in d['Layout']['Regions']])/statistics.mean([area(r) for r in d['Layout']['Regions']])}
for item,control in items:
    if item['variant']=='normal':continue
    assert base['Features']==control['Features'], 'Features changed between controls'
    assert base['Tuning']==control['Tuning'], 'Tuning changed between controls'
    assert base['Settings']['Map']==control['Settings']['Map'], 'Map input changed'
    flags=['StellarInfluenceEnabled','BlackHoleInfluenceEnabled','NebulaInfluenceEnabled']
    changed=[f for f in flags if base['Settings'][f]!=control['Settings'][f]]
    assert changed and all(base['Settings'][f] and not control['Settings'][f] for f in changed)
    kinds=[n+1 for n,f in enumerate(flags) if f in changed]
    family={1:'Star',2:'BlackHole',3:'Nebula'}[kinds[0]] if len(kinds)==1 else None
    primary=next((c for c in causal['Comparisons'] if c['Seed']==42 and c['Family']==family),None)
    if primary:assert primary['EnabledHash']==base['ContentHash'] and primary['DisabledHash']==control['ContentHash']
    before_file='standard-control-'+item['variant']+'.png';after_file='standard-overview.png'
    before=Image.open(rendered/before_file).convert('RGB');after=Image.open(rendered/after_file).convert('RGB')
    assert before.size==after.size==(1920,1080)
    canvas=Image.new('RGB',(1880,665),'#050a10');draw=ImageDraw.Draw(canvas)
    draw.text((20,12),'Same seed 42 / 40 systems / identical feature anchors and provisional tuning',font=font,fill='#efb16b')
    draw.text((20,52),item['label']+' / actual control export',font=small,fill='#eeeaf2');draw.text((950,52),'All influences enabled / actual normal export',font=small,fill='#eeeaf2')
    canvas.paste(before.resize((910,512),Image.Resampling.LANCZOS),(20,85));canvas.paste(after.resize((910,512),Image.Resampling.LANCZOS),(950,85))
    draw.text((20,613),'Fixed region budget redistributes sites. Footprints are normalized generation influence, not hazards or physical radii.',font=small,fill='#a9b8c8')
    name='control-'+item['variant']+'-comparison.png';canvas.save(target/name)
    report['pairs'].append({'variant':item['variant'],'changedSettings':changed,'seed':'42','systemCount':40,'featuresIdentical':True,'tuningIdentical':True,'controlHash':control['ContentHash'],'normalHash':base['ContentHash'],'primaryHalfWeightCoreComparison':primary,'controlObservations':observations(control,kinds),'normalObservations':observations(base,kinds),'observationCaveat':'Support-union observations use the full influence ellipse; they differ from the Primary half-weight-core directional metric. No per-cell area guarantee.','comparisonImage':name,'controlScreenshotSha256':hashlib.sha256((rendered/before_file).read_bytes()).hexdigest(),'normalScreenshotSha256':hashlib.sha256((rendered/after_file).read_bytes()).hexdigest()})
(target/'controlled-comparison.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print(json.dumps(report))
