"""Check the actual report facts used by art; no generator/authority changes."""
from pathlib import Path
import json,sys,hashlib

p=Path(sys.argv[1]);idx=json.loads(p.read_text(encoding='utf-8'));results=[]
for i in idx['samples']:
    files={k:p.parent/i[k] for k in ['document','report']}
    for k,f in files.items():assert hashlib.sha256(f.read_bytes()).hexdigest()==i[k+'Sha256']
    d=json.loads(files['document'].read_text(encoding='utf-8-sig'));r=json.loads(files['report'].read_text(encoding='utf-8-sig'));q=r['Diagnostics'];l=d['Layout'];assert d['ContentHash']==r['ContentHash']==q['ContentHash']
    features={f['FeatureId']:f for f in d['Features']};influence={a['NodeId']:a for a in d['RegionInfluences']};black={f['NodeId'] for f in d['Features']if f['Kind']==2};threshold=d['Tuning']['QuietDisturbanceThreshold'];quiet=[];disturbed=[];systems=[]
    for a in l['Regions']:
        if a['SystemId']:systems.append(a['NodeId'])
        elif a['NodeId']in black or any(features[c['FeatureId']]['Kind']in[2,3]and c['Amount']>=threshold for c in influence[a['NodeId']]['Contributions']):disturbed.append(a['NodeId'])
        else:quiet.append(a['NodeId'])
    assert sorted(quiet)==q['QuietEmptyNodeIds']and sorted(disturbed)==q['DisturbedEmptyNodeIds']
    neighbors={a['NodeId']:[]for a in l['Regions']}
    for e in l['Connections']:neighbors[e['EndpointA']].append(e['EndpointB']);neighbors[e['EndpointB']].append(e['EndpointA'])
    allowed=set(quiet);seen=set();components=[]
    for n in sorted(allowed):
        if n in seen:continue
        reached={n};todo=[n]
        while todo:
            a=todo.pop()
            for b in neighbors[a]:
                if b in allowed and b not in reached:reached.add(b);todo.append(b)
        seen.update(reached);components.append(sorted(reached))
    assert sorted(components)==sorted(c['NodeIds']for c in q['QuietComponents'])and len(components)==q['QuietComponentCount']
    def area2(a):
        b=a['Boundary'];return abs(sum(u['X']*v['Y']-v['X']*u['Y']for u,v in zip(b,b[1:]+b[:1])))
    areas={a['NodeId']:area2(a)for a in l['Regions']};ratio=(sum(areas[n]for n in quiet)/len(quiet))/(sum(areas[n]for n in systems)/len(systems));assert abs(ratio-q['QuietEmptySystemMeanAreaRatio'])<1e-12
    assert sum(areas.values())==2*l['Width']*l['Height']
    results.append({'seed':i['seed'],'systemCount':i['systemCount'],'rawHashesVerified':True,'quietSites':len(quiet),'disturbedSites':len(disturbed),'quietComponents':len(components),'largestQuietComponent':max(map(len,components)),'quietSystemAreaRatio':ratio,'reportedClassificationsAndComponentsMatched':True})
out=p.parent/'diagnostic-parity.json';out.write_text(json.dumps({'passed':True,'status':'Independent parity of actual diagnostics used by art; not generator acceptance','results':results},indent=2)+'\n',encoding='utf-8');print(json.dumps(results))
