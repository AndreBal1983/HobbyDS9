"""Independent capture/source pin and raw feature projection verification."""
import hashlib,json,subprocess,sys
from pathlib import Path
root=Path(__file__).resolve().parents[3];out=Path(sys.argv[1]).resolve()
read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
receipt=read(out/'natural-checks.json');idx=read(root/'art/generated-galaxy-m1/route-intent-v5/natural/comparison-index.json')
assert receipt['passed'] and not receipt['errors'] and len(receipt['captures'])==6
assert sha(out/'capture-index.json')==receipt['indexSha256']
for f in receipt['captures']:assert sha(out/f['file'])==f['sha256']
for f in receipt['sourceHashes']:assert sha(out/f['file'])==f['sha256']==sha(Path(__file__).parent/f['file'])
raw={};blobs=0
for s in idx['samples']:
    for v in s['versions']:
        for kind in ['document','report']:
            p=root/v[kind];assert sha(p)==v[kind+'Sha256']
            folder='scaled-galaxy-v4' if v['version']=='old' else 'natural-galaxy-v5'
            source='docs/samples/'+folder+'/'+p.name
            blob=subprocess.check_output(['git','show',v[kind+'SourceCommit']+':'+source],cwd=root)
            assert p.read_bytes()==blob;blobs+=1
        raw[v['metrics']['hash']]=read(root/v['document'])
total=0;overview=0
for o in receipt['observations']:
    assert o['cameras'][0]['scale']==o['cameras'][1]['scale'] and o['cameras'][0]['tx']==o['cameras'][1]['tx'] and o['cameras'][0]['ty']==o['cameras'][1]['ty']
    for i,pane in enumerate(o['panes']):
        d=raw[o['hashes'][i]];camera=o['cameras'][i];stars={f['FeatureId']:f for f in d['Features'] if f['Kind']==1}
        assert pane['regions']==len(d['Layout']['Regions'])
        if o['factor']==1:assert len(pane['stars'])==o['count'];overview+=len(pane['stars'])
        assert len({p['id'] for p in pane['stars']})==len(pane['stars'])
        for p in pane['stars']:
            pos=stars[p['id']]['Position'];assert (p['worldX'],p['worldY'])==(pos['X'],pos['Y'])
            assert abs(p['x']-(pos['X']*camera['scale']+camera['tx']))<1e-8
            assert abs(p['y']-(pos['Y']*camera['scale']+camera['ty']))<1e-8;total+=1
result={'passed':True,'exactGitBlobs':blobs,'overviewStars':overview,'renderedStarChecks':total,'observations':len(receipt['observations']),'captures':len(receipt['captures']),'captureReceiptSha256':sha(out/'natural-checks.json'),'finalIndexSha256':sha(root/'art/generated-galaxy-m1/route-intent-v5/natural/comparison-index.json'),'note':'Exact original capture input preserved; final per-file provenance clarification changes no map/report/metric/rendered coordinates. Nearest-neighbour and shoelace checks are reproduced by prepare-natural.py.'}
(out/'independent-proof.json').write_text(json.dumps(result,indent=2),encoding='utf-8');print(json.dumps(result))
