"""Actual-cell diagnostic: reproduce previous chord rule versus exact portal rule."""
from pathlib import Path
from fractions import Fraction
import json,sys
from PIL import Image, ImageDraw, ImageFont

index_path,target=map(Path,sys.argv[1:]);assert not target.exists();target.mkdir(parents=True)
idx=json.loads(index_path.read_text(encoding='utf-8'));item=next(i for i in idx['samples']if i['seed']=='42'and i['systemCount']==250)
d=json.loads((index_path.parent/item['document']).read_text(encoding='utf-8-sig'));r=json.loads((index_path.parent/item['report']).read_text(encoding='utf-8-sig'));nodes={n['NodeId']:n for n in d['Layout']['Regions']};ids=[r['OriginNodeId']]+r['Route']['Steps']
def edges(n):
 b=n['Boundary'];return {tuple(sorted(((a['X'],a['Y']),(c['X'],c['Y']))))for a,c in zip(b,b[1:]+b[:1])}
def cross(a,b):return a[0]*b[1]-a[1]*b[0]
found=None
for a,b in zip(ids,ids[1:]):
 common=edges(nodes[a])&edges(nodes[b]);assert common
 u,v=sorted(common)[0];p=(nodes[a]['Center']['X'],nodes[a]['Center']['Y']);q=(nodes[b]['Center']['X'],nodes[b]['Center']['Y']);direction=(q[0]-p[0],q[1]-p[1]);edge=(v[0]-u[0],v[1]-u[1]);offset=(u[0]-p[0],u[1]-p[1]);den=cross(direction,edge)
 if not den:continue
 along=Fraction(cross(offset,direction),den)
 if along<0 or along>1:found=(a,b,u,v,p,q,along);break
assert found,'This route has no misleading chord; do not invent an example'
a,b,u,v,p,q,along=found;mid=((u[0]+v[0])/2,(u[1]+v[1])/2)
points=[(p['X'],p['Y'])for n in [nodes[a],nodes[b]]for p in n['Boundary']];minx=min(p[0]for p in points);maxx=max(p[0]for p in points);miny=min(p[1]for p in points);maxy=max(p[1]for p in points);pad=max(maxx-minx,maxy-miny)*.15

canvas=Image.new('RGB',(1480,720),'#050a10');draw=ImageDraw.Draw(canvas)
font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',21);small=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',17)
draw.text((25,14),'Same actual 250-system map / same accepted graph hop / unchanged cost 1',font=font,fill='#eeeaf2')
loX=max(0,minx-pad);hiX=min(d['Layout']['Width'],maxx+pad);loY=max(0,miny-pad);hiY=min(d['Layout']['Height'],maxy+pad)
scale=min(700/(hiX-loX),500/(hiY-loY));tx=(700-(hiX-loX)*scale)/2-loX*scale;ty=(500-(hiY-loY)*scale)/2-loY*scale
project=lambda p:(p[0]*scale+tx,p[1]*scale+ty)
for panel,title,route in [(0,'Previous centre-chord rule (reproduced)',[p,q]),(1,'Exact shared-edge portal rule',[p,mid,q])]:
 plot=Image.new('RGB',(700,500),'#08121e');ink=ImageDraw.Draw(plot)
 for n in d['Layout']['Regions']:
  coords=[project((v['X'],v['Y']))for v in n['Boundary']];ink.polygon(coords,fill='#152c3c'if n['NodeId']in[a,b]else'#08121e',outline='#34495d')
 ink.line([project(u),project(v)],fill='#9fd8fb',width=4);ink.line([project(pt)for pt in route],fill='#efb16b',width=3)
 for n,pt in [(a,p),(b,q)]:
  x,y=project(pt);ink.ellipse((x-4,y-4,x+4,y+4),fill='#d5e6f2');ink.text((x+9,y+8),nodes[n]['DisplayName'],font=small,fill='#eeeaf2',stroke_width=2,stroke_fill='#08121e')
 if len(route)==3:
  x,y=project(mid);ink.ellipse((x-5,y-5,x+5,y+5),fill='#08121e',outline='#efb16b',width=2)
 x=25+panel*730;draw.text((x,57),title,font=font,fill='#efb16b');canvas.paste(plot,(x,100));draw.rectangle((x,100,x+700,600),outline='#45566a')
draw.text((25,628),'Left: chord crosses the shared-edge line outside its finite segment. Right: both segments stay inside their accepted cells.',font=small,fill='#a9b8c8')
draw.text((25,657),'A portal is a drawing vertex, not another navigation step. Crooked boundaries and accepted graph cost are unchanged.',font=small,fill='#a9b8c8')
canvas.save(target/'portal-comparison.png')
(target/'portal-comparison.json').write_text(json.dumps({'status':'Actual-cell diagnostic; left reproduces former presentation rule, not an old executable screenshot','backendCommit':idx['backendCommit'],'contentHash':d['ContentHash'],'from':a,'to':b,'sharedEdge':[u,v],'midpoint2':[u[0]+v[0],u[1]+v[1]],'oldChordEdgeLineParameter':str(along),'oldChordOutsideSharedSegment':True,'graphCostUnchanged':1},indent=2)+'\n',encoding='utf-8')
print(json.dumps({'from':a,'to':b,'oldChordOutsideSharedSegment':True,'output':str(target/'portal-comparison.png')}))
