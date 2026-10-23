"""Independent receipt, map/fixture projection and finite-bound checks."""
import hashlib,json,math,sys
from pathlib import Path
import numpy as np
from PIL import Image
ROOT=Path(__file__).resolve().parents[3];OUT=Path(sys.argv[1]).resolve();sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest();read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
proof=read(OUT/'browser-review.json');gen=read(OUT/'generation.json');assert proof['passed'] and not proof['errors'] and len(proof['captures'])==6
for f in proof['captures']:assert sha(OUT/f['file'])==f['sha256']
for f in proof['sourceHashes']:assert sha(Path(__file__).parent/f['file'])==f['sha256']
assert sha(ROOT/'tools/art/nebula-volume-v1/generate.py')==gen['sourceHelperSha256']
assert sha(OUT/'map-context.js')==gen['mapContextSha256']
context=json.loads((OUT/'map-context.js').read_text(encoding='utf-8').removeprefix('window.NEBULA_CONTEXT=').removesuffix(';'))
raw=read(ROOT/'art/generated-galaxy-m1/route-intent-v5/natural/seed-42-systems-250.natural.json');assert context['map']==raw
fixture=json.loads((OUT/'galaxy-fixture.js').read_text().removeprefix('window.GALAXY_FIXTURE=').removesuffix(';'));assert len(fixture['stars'])==600 and all(s['depth']==0 for s in fixture['stars'])
assert sum(fixture['typeCounts'].values())==600
for k,v in fixture['typeCounts'].items():assert sum(s['kind']==k for s in fixture['stars'])==v
actual={f['FeatureId']:{'X':f['Position']['X'],'Y':f['Position']['Y']} for f in raw['Features'] if f['Kind']==1};illustrated={s['id']:s for s in fixture['stars']};count=0
for o in proof['observations']:
    positions=actual if o['scene']=='map' else illustrated
    assert not o['ellipseClip']
    if o['scene']=='map':assert o['mapHash']==raw['ContentHash'] and all(s['kind']=='generic' for s in o['visibleStars'])
    else:assert 'ILLUSTRATIVE' in o['fixture'] and 'NOT CURRENT GENERATOR' in o['fixture']
    for star in o['visibleStars']:
        p=positions[star['id']];c=o['camera'];expected=(c['width']/2+(p['X']-c['X'])*c['scale']-o['pan'],c['height']/2+(p['Y']-c['Y'])*c['scale']-o['panY'])
        assert math.dist(expected,(star['x'],star['y']))<1e-8;count+=1
    for g in o['guidePoints']:
        anchor=context['feature']['Position'] if o['scene']=='map' else fixture['focus'];c=o['camera'];q=1/(1-g['z']) if o['depth'] else 1
        assert abs(g['x']-(c['width']/2+((anchor['X']-c['X'])*c['scale']-o['pan'])*q))<1e-8
layers=0;endcaps={}
for field in gen['layers']:
    for i,layer in enumerate(field['layers']):
        im=Image.open(OUT/layer['file']);assert im.size==(512,320) and im.mode=='RGBA';alpha=np.asarray(im)[:,:,3];assert not np.any(alpha[0]) and not np.any(alpha[-1]) and not np.any(alpha[:,0]) and not np.any(alpha[:,-1]);layers+=1
        if i in (0,31):endcaps.setdefault(field['id'],[]).append(int(alpha.max()))
assert all(a['outsideInfluenceAlphaFraction']>.08 for a in proof['audits'])
result={'passed':True,'captures':6,'depthLayers':layers,'independentProjectedMarkers':count,'actualMapExact':True,'fixtureExplicitlyIllustrative':True,'typeCounts':fixture['typeCounts'],'paddedXYEdgesZero':True,'depthEndAlphaMax':endcaps,'continuousSixFaceFadeProved':False,'ellipseUsedAsPhysicalClip':False,'browserReceiptSha256':sha(OUT/'browser-review.json'),'generationReceiptSha256':sha(OUT/'generation.json'),'limits':'Visual/CPU fixture proof only; no production Unity, taxonomy, true3D navigation, generator or runtime performance acceptance.'}
(OUT/'independent-proof.json').write_text(json.dumps(result,indent=2),encoding='utf-8');print(json.dumps(result))
