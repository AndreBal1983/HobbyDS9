"""Independent exact-integer shared-edge/convex containment proof from rendered points."""
from pathlib import Path
import json,sys

index_path,out=map(Path,sys.argv[1:]);index=json.loads(index_path.read_text(encoding='utf-8'));browser=json.loads((out/'browser-parity.json').read_text(encoding='utf-8'));assert browser['passed']
results=[]
def edges(r):
    b=r['Boundary'];return {tuple(sorted(((a['X'],a['Y']),(c['X'],c['Y'])))) for a,c in zip(b,b[1:]+b[:1])}
def contains(r,p2):
    b=r['Boundary'];cross=[(c['X']-a['X'])*(p2[1]-2*a['Y'])-(c['Y']-a['Y'])*(p2[0]-2*a['X']) for a,c in zip(b,b[1:]+b[:1])]
    return all(c>=0 for c in cross)or all(c<=0 for c in cross)
for obs in browser['observations']:
    item=next(s for s in index['samples']if s['seed']==obs['seed']and s['systemCount']==obs['systemCount'])
    d=json.loads((index_path.parent/item['document']).read_text(encoding='utf-8-sig'));report=json.loads((index_path.parent/item['report']).read_text(encoding='utf-8-sig'));assert d['ContentHash']==obs['contentHash']==report['ContentHash'];regions={r['NodeId']:r for r in d['Layout']['Regions']};by_edge={n:edges(r)for n,r in regions.items()}
    for e in d['Layout']['Connections']:
        assert by_edge[e['EndpointA']]&by_edge[e['EndpointB']], 'Connection without a complete shared edge'
    expected=[report['OriginNodeId']]+report['Route']['Steps'];route=obs['route'];assert route['nodes']==expected and route['portalCount']==len(expected)-1 and len(route['points'])==len(expected)*2-1
    cam=obs['camera'];world2=[]
    for p in route['points']:
        x=(p[0]-cam['tx'])/cam['scale']*2;y=(p[1]-cam['ty'])/cam['scale']*2
        assert abs(x-round(x))<1e-5 and abs(y-round(y))<1e-5
        world2.append((round(x),round(y)))
    halves=0
    for i,n in enumerate(expected):
        r=regions[n];assert world2[2*i]==(2*r['Center']['X'],2*r['Center']['Y'])and contains(r,world2[2*i])
        if i:
            common=by_edge[expected[i-1]]&by_edge[n];mid=world2[2*i-1]
            assert any(mid==(a[0]+b[0],a[1]+b[1])and a!=b for a,b in common), 'Portal rounded, snapped or not shared'
            assert contains(regions[expected[i-1]],mid)and contains(r,mid), 'Portal outside one of its cells'
            halves+=int(mid[0]%2!=0 or mid[1]%2!=0)
    results.append({'width':obs['width'],'height':obs['height'],'systems':obs['systemCount'],'regions':len(regions),'connectionsWithExactSharedEdges':len(d['Layout']['Connections']),'routeHops':len(expected)-1,'portalsProved':len(expected)-1,'halfCoordinatePortalsPreserved':halves,'convexIntraCellSegmentsProved':2*(len(expected)-1),'costUnchanged':report['Route']['TotalCost']})
receipt={'status':'Exact integer geometry proof on real rendered route vertices; not native acceptance','passed':True,'results':results}
(out/'portal-proof.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8');print(json.dumps(receipt))
