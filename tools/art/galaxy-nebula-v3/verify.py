"""Independent asset and evidence checks; creates no images and does not regenerate."""
import hashlib,json,sys
from pathlib import Path
import numpy as np
from PIL import Image

p=Path(sys.argv[1]).resolve()
manifest=json.loads((p/'payload-manifest.json').read_text(encoding='utf-8'))
for row in manifest:
    f=p/row['file'];assert f.stat().st_size==row['bytes']
    assert hashlib.sha256(f.read_bytes()).hexdigest()==row['sha256'],row['file']
g=json.loads((p/'generation.json').read_text(encoding='utf-8'))
assert g['elapsedSeconds']<180 and g['peakWorkingSetBytes']<512*1048576
assert not g['gpuUsed'] and not g['referencePixelsUsed']
v=np.fromfile(p/'nebula-linear-rgba.raw',np.uint8).reshape(64,96,128,4)
a=np.asarray(Image.open(p/'galaxy-linear-rgba.png'))
assert a.shape==(2048,2048,4)
assert max(int(x.max()) for x in [v[0],v[-1],v[:,0],v[:,-1],v[:,:,0],v[:,:,-1]])==0
assert max(int(x.max()) for x in [a[0],a[-1],a[:,0],a[:,-1]])==0
assert np.count_nonzero(v[:,:,:,3])>1000 and np.count_nonzero(a[:,:,3])>10000
particles=json.loads((p/'nebula-particles.json').read_text(encoding='utf-8'))['points']
assert len(particles)==256 and len(set(q['index'] for q in particles))==256
for q in particles:
    assert all(abs(c)<=b for c,b in zip(q['position'],[2.5,1.65,1]))
proof=json.loads((p/'proof.json').read_text(encoding='utf-8'))
images=sorted(p.glob('[0-9][0-9]-*.png'));assert len(images)==4
assert all(Image.open(im).size==(1280,800) for im in images)
tracks=proof['particleTracks'];delta={}
for label in ['near','far']:
    left=next(t for t in tracks if t['name']==label and 'left' in t['frame'])
    right=next(t for t in tracks if t['name']==label and 'right' in t['frame'])
    delta[label]=left['screenXY'][0]-right['screenXY'][0]
    assert left['screenXY'][1]==right['screenXY'][1]
assert delta['near']>delta['far']>0
near=next(t for t in tracks if t['name']=='near');far=next(t for t in tracks if t['name']=='far')
assert abs(delta['near']/delta['far']-near['q']/far['q'])<.0001
result={'verifiedPayloadCount':len(manifest),'failures':0,'edgeChecks':'six nebula faces and four atlas edges zero',
 'nearDeltaPixels':delta['near'],'farDeltaPixels':delta['far'],'differentialParallaxPixels':delta['near']-delta['far'],
 'limitsPassed':True,'capturesTotal':4,'runtimeAcceptance':False,
 'overviewVisibleActualStars':proof['captures'][0]['visibleActualStars'],'overviewNote':'244/250 visible; proof viewport crops a little at top and bottom'}
(p/'independent-verification.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print(json.dumps(result))
