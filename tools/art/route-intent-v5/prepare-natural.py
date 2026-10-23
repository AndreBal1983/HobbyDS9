"""Pin real source blobs and measure them without moving any features."""
import hashlib,json,math,subprocess
from pathlib import Path
root=Path(__file__).resolve().parents[3];folder=root/'art/generated-galaxy-m1/route-intent-v5/natural';folder.mkdir(exist_ok=True)
read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
oldidx=read(root/'art/generated-galaxy-m1/scale-v4/sample-index.json')
oldprovenance=read(root/'art/generated-galaxy-m1/scale-v4/source-provenance.json')
rows=[];inputs=[]
for count,seed in [(250,'42'),(250,'43'),(1000,'42')]:
    pair=[]
    for version in ['old','new']:
        if version=='old':
            item=next(x for x in oldidx['samples'] if x['seed']==seed and x['systemCount']==count)
            paths=[root/'art/generated-galaxy-m1/scale-v4'/item[x] for x in ['document','report']]
            assert [sha(p) for p in paths]==[item['documentSha256'],item['reportSha256']]
            pin=oldidx['backendCommit']
        else:
            pin='930b1adb34756be9bf3b605da56d6b0827902708' if count==1000 else '5d83888791a0c4ca6728627ed6cad01567313ff8'
            paths=[]
            for ext in ['natural','report']:
                name=f'seed-{seed}-systems-{count}.{ext}.json';source=f'docs/samples/natural-galaxy-v5/{name}';p=folder/name
                blob=subprocess.check_output(['git','show',pin+':'+source],cwd=root)
                if p.exists():assert p.read_bytes()==blob
                else:p.write_bytes(blob)
                paths.append(p);inputs.append({'file':str(p.relative_to(root)).replace('\\','/'),'sourceCommit':pin,'gitPath':source,'sha256':sha(p)})
        d,r=map(read,paths);assert d['ContentHash']==r['ContentHash']==r['ReopenedHash']
        settings=d['Settings'].get('Map',d['Settings']);assert settings['Seed']==seed and settings['SystemCount']==count
        diag=r['Diagnostics'].get('GeometryAndQuietSpace',r['Diagnostics'])
        stars=[f for f in d['Features'] if f['Kind']==1];nn=[]
        for f in stars:
            candidates=[]
            for g in stars:
                if f is g:continue
                sq=sum((f['Position'][axis]-g['Position'][axis])**2 for axis in ['X','Y'])
                candidates.append((sq,g['FeatureId']))
            sq,nid=min(candidates);nn.append({'FeatureId':f['FeatureId'],'NearestFeatureId':nid,'DistanceSquared':sq})
        distances=sorted(math.isqrt(x['DistanceSquared']) for x in nn);isolated=sum(x['DistanceSquared']>=120000**2 for x in nn)
        if version=='new':
            assert nn==r['Diagnostics']['NearestNeighbours']
            assert isolated==r['Diagnostics']['ActualStarsWithNearestAtLeastIsolatedSeparation']
            members=[x for g in d['StarGroups'] for x in g['StarFeatureIds']]
            assert sorted(members)==sorted(f['FeatureId'] for f in stars)
        # Exact shoelace areas, independent of renderer or supplied means.
        areas={}
        for n in d['Layout']['Regions']:
            p=n['Boundary'];areas[n['NodeId']]=abs(sum(a['X']*b['Y']-b['X']*a['Y'] for a,b in zip(p,p[1:]+p[:1])))/2
        for ids,key in [(diag['QuietEmptyNodeIds'],'QuietEmptyMeanArea'),(diag['DisturbedEmptyNodeIds'],'DisturbedEmptyMeanArea'),([n['NodeId'] for n in d['Layout']['Regions'] if n['SystemId']],'SystemMeanArea')]:
            assert math.isclose(sum(areas[x] for x in ids)/len(ids),diag[key],rel_tol=1e-12)
        metrics={'hash':d['ContentHash'],'format':d['FormatVersion'],'generator':d['GeneratorVersion'],'systems':len(stars),'regions':len(areas),'quietCount':len(diag['QuietEmptyNodeIds']),'disturbedCount':len(diag['DisturbedEmptyNodeIds']),'quietMeanArea':diag['QuietEmptyMeanArea'],'disturbedMeanArea':diag['DisturbedEmptyMeanArea'],'systemMeanArea':diag['SystemMeanArea'],'quietSystemRatio':diag['QuietEmptySystemMeanAreaRatio'],'quietComponents':diag['QuietComponentCount'],'largestQuietComponent':max(len(c['NodeIds']) for c in diag['QuietComponents']),'crossingsMedian':diag['SystemPairCrossingsMedian'],'crossingsP90':diag['SystemPairCrossingsP90'],'crossingsMax':diag['SystemPairCrossingsMaximum'],'actualNNIsolated120k':isolated,'nearestFloorMedian':(distances[(len(distances)-1)//2]+distances[len(distances)//2])/2,'provenanceArities':sorted(set(len(g['StarFeatureIds']) for g in d.get('StarGroups',[]) if g['Kind']==2)),'candidateRouteCost':r['Plan']['TotalCost'] if version=='new' else r['Route']['TotalCost']}
        route=r['Plan']['Legs'][0] if version=='new' else {'OriginNodeId':r['OriginNodeId'],'Route':r['Route']}
        documentPin=pin;reportPin=pin
        if version=='old':
            documentPin=next(x['sourceCommit'] for x in oldprovenance['files'] if x['file']==item['document'])
            reportPin=next(x['sourceCommit'] for x in oldprovenance['files'] if x['file']==item['report'])
        pair.append({'version':version,'document':str(paths[0].relative_to(root)).replace('\\','/'),'report':str(paths[1].relative_to(root)).replace('\\','/'),'documentSha256':sha(paths[0]),'reportSha256':sha(paths[1]),'documentSourceCommit':documentPin,'reportSourceCommit':reportPin,'metrics':metrics,'route':{'origin':route['OriginNodeId'],'steps':route['Route']['Steps']}})
    assert pair[0]['metrics']['hash']!=pair[1]['metrics']['hash']
    assert read(root/pair[0]['document'])['Layout']['Width']==read(root/pair[1]['document'])['Layout']['Width']
    rows.append({'seed':seed,'count':count,'versions':pair})
result={'status':'Actual source comparisons; CPU browser presentation only; no native acceptance','runtimePin':'5d83888791a0c4ca6728627ed6cad01567313ff8','samples':rows,'newRawInputs':inputs,'independentNNRows':3000,'notes':['StarGroups are provenance, not measured connected components.','Matched seed/count/extent/camera/style, different identities and features.','Quiet area target is soft and new ratios overshoot 2:1.','Route costs belong to each map/candidate pair; not the same corridor or endpoints across versions.']}
(folder/'comparison-index.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print(json.dumps([{'seed':x['seed'],'count':x['count'],'versions':[v['metrics'] for v in x['versions']]} for x in rows]))
