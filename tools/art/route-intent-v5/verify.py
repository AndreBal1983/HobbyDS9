"""Independent raw-polygon/API/rendered-SVG checks. No search or geometry repair."""
import hashlib,json,math,sys
from pathlib import Path
root=Path(__file__).resolve().parents[3]
out=Path(sys.argv[1]).resolve()
read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
idx=read(root/'art/generated-galaxy-m1/route-intent-v5/sample-index.json')
for f in idx['files']:assert sha(root/'art/generated-galaxy-m1/route-intent-v5'/f['file'])==f['sha256']
inputs={Path(f['file']).name:read(root/'art/generated-galaxy-m1/route-intent-v5'/f['file']) for f in idx['files']}
raw=inputs['seed-42-systems-250.scaled.json'];examples=inputs['examples.json'];receipt=read(out/'review-checks.json')
assert not receipt['errors'] and len(receipt['captures'])==36
for f in receipt['sourceHashes']:
    assert sha(out/f['file'])==f['sha256']==sha(Path(__file__).parent/f['file'])
for c in receipt['captures']:assert sha(out/c['file'])==c['sha256']
nodes={n['NodeId']:n for n in raw['Layout']['Regions']}
stars={f['SystemId']:f['Position'] for f in raw['Features'] if f['Kind']==1}
xy=lambda p:(p['X'],p['Y'])
twice=lambda p:tuple(int(v*2) for v in xy(p))
edges={}
for n in nodes.values():
    poly=list(map(xy,n['Boundary']))
    for a,b in zip(poly,poly[1:]+poly[:1]):edges.setdefault(tuple(sorted((a,b))),[]).append(n['NodeId'])
shared={}
for (a,b),owners in edges.items():
    if len(owners)==2:
        assert a!=b
        shared.setdefault(tuple(sorted(owners)),[]).append((a,b))
def portal(a,b):
    # Contract chooses ordinal edge-key, not numeric point ordering.
    es=shared[tuple(sorted((a,b)))]
    key=lambda e:'|'.join(sorted(','.join(map(str,p)) for p in e))
    a,b=min(es,key=key)
    return(a[0]+b[0],a[1]+b[1])
def inside(point2,node):
    poly=[twice(p) for p in nodes[node]['Boundary']];signs=[]
    for a,b in zip(poly,poly[1:]+poly[:1]):
        cross=(b[0]-a[0])*(point2[1]-a[1])-(b[1]-a[1])*(point2[0]-a[0])
        if cross:signs.append(cross>0)
    return not signs or all(s==signs[0] for s in signs)
cases={c['Name']:c for c in examples['Cases']};counts={'observations':0,'legs':0,'portals':0,'containedSegments':0,'renderedPoints':0}
for o in receipt['observations']:
    counts['observations']+=1;r=o['result'];assert o['mapHash']==raw['ContentHash']==examples['ContentHash']
    pins=sorted(i for g in o['pinGroups'] for i in g);assert pins==list(range(len(o['targets'])))
    if o['scenario'] in cases:assert r==cases[o['scenario']]['Result']
    elif o['policy']=='new':assert r==examples['Comparison']['Result']
    else:assert r['Legs'][0]['Route']==examples['Comparison']['LegacyRoute']
    if not r['Success']:
        assert not r['Legs'] and not r['Steps'] and r['TotalCost'] is None and r['GeographicLengthUnits'] is None
        assert not o['domLegs'] and not o['geometry'] and 'Total cost —' in o['visibleTotal'] and len(o['targets'])==2
        continue
    assert len(o['domLegs'])==len(r['Legs'])==len(o['targets'])
    assert sum(l['Route']['TotalCost'] for l in r['Legs'])==r['TotalCost']
    assert [x for l in r['Legs'] for x in l['Route']['Steps']]==r['Steps']
    start=nodes[r['OriginNodeId']]['Center'];prev=r['OriginNodeId']
    for leg,dom in zip(r['Legs'],o['domLegs']):
        counts['legs']+=1;assert leg['OriginNodeId']==prev
        ids=[leg['OriginNodeId'],*leg['Route']['Steps']];assert dom['nodes']==ids
        assert leg['Route']['TotalCost']==len(ids)-1
        t=leg['Target'];end=stars[t['SystemId']] if t['Intent']==2 else nodes[t['NodeId']]['Center']
        ps=[portal(a,b) for a,b in zip(ids,ids[1:])];counts['portals']+=len(ps)
        expected=[twice(start),*ps,twice(end)]
        if o['drawing']=='centres':
            expected=[twice(start)]
            for p,n in zip(ps,ids[1:]):expected.extend([p,twice(nodes[n]['Center'])])
            expected[-1]=twice(end)
        assert len(expected)==len(dom['points'])
        camera=o['camera']
        for actual,p in zip(dom['points'],expected):
            got=((actual[0]-camera['tx'])/camera['scale']*2,(actual[1]-camera['ty'])/camera['scale']*2)
            assert max(abs(got[i]-p[i]) for i in (0,1))<1e-5 # Projection roundtrip only; raw geometry exact.
            counts['renderedPoints']+=1
        if o['drawing']=='transit':
            for n,a,b in zip(ids,expected,expected[1:]):
                assert inside(a,n) and inside(b,n);counts['containedSegments']+=1
        span=sum(math.isqrt(sum((nodes[a]['Center'][k]-nodes[b]['Center'][k])**2 for k in ('X','Y'))) for a,b in zip(ids,ids[1:]))
        assert span==leg['GeographicLengthUnits']
        start=end;prev=t['NodeId']
    if o['scenario']=='return-a-b-a':
        assert len(r['Legs'])==2 and o['geometry'][0]['end']==o['geometry'][1]['start']
        assert o['geometry'][0]['end']==nodes[o['targets'][0]['NodeId']]['Center']
    if o['scenario']=='zero-leg-a-b-b-c':assert len(r['Legs'])==3 and r['Legs'][1]['Route']['TotalCost']==0
result={'passed':True,'counts':counts,'receiptSha256':sha(out/'review-checks.json'),'scope':'Exact raw boundaries, explicit per-leg anchors, returned API results, actual rendered SVG projection, atomic failure; not global tautness or native acceptance','visualFinding':'Remaining midpoint doglegs are conspicuous at the start and midroute. Candidate needs separately proved ordered-corridor straightening before visual acceptance.'}
(out/'independent-proof.json').write_text(json.dumps(result,indent=2),encoding='utf-8');print(json.dumps(result))
