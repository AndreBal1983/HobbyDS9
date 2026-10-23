"""Check source/asset receipts and actual fixed-map projection independently."""
import hashlib,json,math,sys
from pathlib import Path
import numpy as np
from PIL import Image
root=Path(__file__).resolve().parents[3];out=Path(sys.argv[1]).resolve();sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest();read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
gen=read(out/'generation.json');proof=read(out/'browser-final.json');assert proof['passed'] and not proof['errors']
for f in proof['sourceHashes']:assert sha(Path(__file__).parent/f['file'])==f['sha256']
for f in proof['captures']:assert sha(out/f['file'])==f['sha256']
raw=read(root/'art/generated-galaxy-m1/route-intent-v5/natural/seed-42-systems-250.natural.json');assert sha(root/'art/generated-galaxy-m1/route-intent-v5/natural/seed-42-systems-250.natural.json')==gen['mapFileSha256']
context=json.loads((out/'map-context.js').read_text(encoding='utf-8').removeprefix('window.NEBULA_CONTEXT=').removesuffix(';'));assert context['map']==raw
report=read(root/'art/generated-galaxy-m1/route-intent-v5/natural/seed-42-systems-250.report.json');assert context['routeResult']==report['Plan']
assert context['feature']==next(f for f in raw['Features'] if f['FeatureId']==gen['feature']['FeatureId'])
stars={f['FeatureId']:f for f in raw['Features'] if f['Kind']==1};checks=0
for o in proof['observations']:
    assert o['mapHash']==raw['ContentHash'];s=o['camera']['scale'];f=o['feature']['Position']
    for star in o['visibleStars']:
        p=stars[star['id']]['Position'];assert p==star['world']
        expected=(o['camera']['width']/2+(p['X']-f['X'])*s-o['pan'],o['camera']['height']/2+(p['Y']-f['Y'])*s-o['panY'])
        assert math.dist(expected,(star['x'],star['y']))<1e-8;checks+=1
    for g in o['guidePoints']:
        q=1/(1-g['z']) if o['depth'] else 1
        assert abs(g['x']-(o['camera']['width']/2-o['pan']*q))<1e-8
assert all(a['outsideMax']==0 and all(c['found'] for c in a['cores']) for a in proof['audits'])
correlations=[];layerHashes=[]
for style in gen['styles']:
    previous=None;values=[]
    assert len(style['layers'])==32
    for row in style['layers']:
        p=out/row['file'];im=Image.open(p);assert im.size==(512,320) and im.mode=='RGBA';layerHashes.append(sha(p));a=np.asarray(im)[:,:,3].astype(float)
        if previous is not None and previous.std()>0 and a.std()>0:values.append(float(np.corrcoef(previous.ravel(),a.ravel())[0,1]))
        previous=a
    correlations.append({'style':style['id'],'minimumAdjacentAlphaCorrelation':min(values),'meanAdjacentAlphaCorrelation':sum(values)/len(values)})
assert len(set(layerHashes))==128
result={'passed':True,'rawMapAndApiPlanExact':True,'projectedStarChecks':checks,'depthLayers':128,'uniqueLayerHashes':128,'adjacentAlphaCorrelations':correlations,'referencePixelsUsed':False,'finalBrowserReceiptSha256':sha(out/'browser-final.json'),'generationReceiptSha256':sha(out/'generation.json'),'scope':'CPU concept/perspective depth-slice proof only, not Unity implementation or performance acceptance'}
(out/'independent-proof.json').write_text(json.dumps(result,indent=2),encoding='utf-8');print(json.dumps(result))
