"""Original freeform/cloud and illustrative galactic fields. CPU authoring only."""
import ast,ctypes,hashlib,json,math,os,sys,time
from pathlib import Path
import numpy as np
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[3];OUT=Path(sys.argv[1]).resolve();assert not OUT.exists();OUT.mkdir(parents=True)
start=time.perf_counter();peak=0;W,H,N=512,320,32
# Reuse only inspected pure noise/memory functions from the pinned v1 authoring code.
helper=ROOT/'tools/art/nebula-volume-v1/generate.py';tree=ast.parse(helper.read_text())
pure=[n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name in ['memory','smooth','noise','fbm']]
exec(compile(ast.Module(body=pure,type_ignores=[]),str(helper),'exec'),globals())

def padded(x,y,rx,ry):return np.clip((1-np.abs(x)/rx)/.18,0,1)*np.clip((1-np.abs(y)/ry)/.18,0,1)
def cloud(z):
    x,y=np.meshgrid(np.linspace(-2.5,2.5,W,dtype=np.float32),np.linspace(-1.65,1.65,H,dtype=np.float32));s=1237
    wx=fbm(x*1.6+3,y*1.6,z*.8,s,3)-.5;wy=fbm(x*1.6,y*1.6+9,z*.8,s+13,3)-.5
    u=.94*x+.34*y+wx*.36;v=-.34*x+.94*y+wy*.30
    n=fbm(u*4,v*4,z*.8,s+31);fine=fbm(u*20,v*20,z*2.4,s+7,3)
    a=np.exp(-((u+.63)**2/.55+(v+.09)**2/.10+(z+.14)**2/.45))
    b=np.exp(-((u-.36)**2/.24+(v-.20)**2/.26+(z-.22)**2/.33))
    c=np.exp(-((u-1.15)**2/.22+(v+.35)**2/.035+(z+.35)**2/.20))
    ridge=v-(.27*np.sin(u*2.4)-.19)
    ribbon=np.exp(-(ridge/.048)**2-((z-.2*np.sin(u*2))/.33)**2)*np.exp(-u*u/2.8)
    thread=np.exp(-((v+.44+.13*np.cos(u*3))/.022)**2-((z+.3)/.35)**2)*np.exp(-((u+.3)/1.6)**4)
    cavity=1-.90*np.exp(-((u+.08)**2/.13+(v+.02)**2/.09+(z+.04)**2/.7))
    shape=(a+b*.95+c*.8+ribbon*.48+thread*.3)*cavity
    emit=shape*np.clip((n-.35)*4.5,0,1.8)**1.5*(.45+fine)
    dust=shape*np.exp(-((ridge+.11)/.095)**2)*np.clip((n-.27)*4,0,2)*np.exp(-((z-.2)/.5)**2)*2.8
    gy,gx=np.gradient(dust);rim=np.clip(np.hypot(gx,gy)*6,0,2)
    mix=np.clip(.4+u*.35+v*.6,0,1);colour=np.array([.08,.48,.82])*(1-mix[...,None])+np.array([.88,.18,.45])*mix[...,None]
    light=.4+np.exp(-((u+.35)**2+(v+.3)**2+(z-.4)**2)*1.7)
    lit=colour*light[...,None]+rim[...,None]*np.array([1.,.70,.3])*1.4
    emit+=rim*.15;ext=emit*.95+dust*3.5;alpha=(1-np.exp(-ext*2/N*1.25))*padded(x,y,2.5,1.65)
    rgb=(lit*emit[...,None]+np.array([.004,.006,.012])*dust[...,None]*3.5)/(ext[...,None]+1e-5)
    rgb=np.clip(rgb*1.7,0,1)**(1/1.35)
    return np.dstack([rgb,alpha])

def galaxy(z):
    x,y=np.meshgrid(np.linspace(-1.38,1.38,W,dtype=np.float32),np.linspace(-1.12,1.12,H,dtype=np.float32))
    r=np.sqrt(x*x+(y*1.15)**2);theta=np.arctan2(y*1.15,x);n=fbm(x*5,y*5,z*2.4,781,4)
    phase=3*(theta+2.25*np.log(r+.09))+(n-.5)*1.6
    arms=(.5+.5*np.cos(phase))**9
    vertical=np.exp(-((z+.06*np.sin(theta*3+r*8))/.28)**2)
    disk=np.exp(-r*2.5)*np.clip((1.2-r)*5,0,1)
    core=np.exp(-r*r/.035-z*z/.27)
    emit=(arms*.95+.09)*disk*vertical*(.35+n*1.2)+core*.7
    dust=(.5+.5*np.cos(phase-.36))**14*disk*vertical*(.25+n)*1.2
    knots=np.clip((n-.60)*6,0,1)*arms
    colour=np.array([.21,.37,.69])*(1-core[...,None]*.7)+np.array([1.,.62,.22])*core[...,None]*.9+knots[...,None]*np.array([.50,.06,.15])
    ext=emit+dust*1.3;alpha=(1-np.exp(-ext*2/N*2.1))*padded(x,y,1.38,1.12)
    rgb=(colour*emit[...,None]+dust[...,None]*np.array([.018,.011,.018]))/(ext[...,None]+1e-5)
    return np.dstack([np.clip(rgb*2,0,1)**(1/1.3),alpha])

rows=[]
for name,fn,depth in [('freeform',cloud,.18),('galaxy',galaxy,.11)]:
    (OUT/name).mkdir();layers=[];beauty=Image.new('RGBA',(W,H),(3,7,14,255));alphaTotal=np.zeros((H,W))
    for i,z in enumerate(np.linspace(-1,1,N)):
        a=fn(float(z));im=Image.fromarray(np.uint8(np.clip(a,0,1)*255),'RGBA');file=f'{name}/depth-{i:02}.png';im.save(OUT/file);beauty=Image.alpha_composite(beauty,im);alphaTotal=1-(1-alphaTotal)*(1-a[:,:,3]);layers.append({'file':file,'zOverD':float(z)*depth});memory()
    beauty.convert('RGB').resize((1280,800),Image.Resampling.LANCZOS).save(OUT/f'{name}-concept.png')
    edge=np.concatenate([alphaTotal[0],alphaTotal[-1],alphaTotal[:,0],alphaTotal[:,-1]])
    row={'id':name,'layers':layers,'boundsNormalized':[2.5,1.65] if name=='freeform' else [1.38,1.12],'edgeAlphaMax':float(edge.max())};assert row['edgeAlphaMax']==0
    if name=='freeform':
        x,y=np.meshgrid(np.linspace(-2.5,2.5,W),np.linspace(-1.65,1.65,H));mask=x*x+y*y>1;row['integratedAlphaOutsideOldInfluenceEllipseFraction']=float(alphaTotal[mask].sum()/alphaTotal.sum());assert row['integratedAlphaOutsideOldInfluenceEllipseFraction']>.08
    rows.append(row);print(name,round(time.perf_counter()-start,2),'s',round(memory()/1024**2,1),'MiB',flush=True);assert time.perf_counter()-start<120

old=Path('E:/HobbyDS9/artifacts/art/nebula-volume-v1-20261023-2');binding=json.loads((old/'delivery.json').read_text());manifest=json.loads((old/'payload-manifest.json').read_text());context=old/'map-context.js';assert hashlib.sha256(context.read_bytes()).hexdigest()==next(x['sha256'] for x in manifest if x['file']=='map-context.js');(OUT/'map-context.js').write_bytes(context.read_bytes())
rng=np.random.default_rng(8675309);stars=[];kinds=['red-dwarf','sunlike','red-giant','blue-giant','brown-dwarf','white-dwarf'];weights=[.35,.20,.12,.13,.10,.10]
while len(stars)<600:
    r=float(np.sqrt(rng.uniform(.01,.96)));t=float(rng.uniform(-math.pi,math.pi));prob=.16+.84*((1+math.cos(3*(t+2.25*math.log(r+.09))))/2)**7
    if rng.uniform()>prob:continue
    kind=str(rng.choice(kinds,p=weights));stars.append({'id':f'illustration.star.{len(stars)+1:04}','X':500+r*390*math.cos(t),'Y':400+r*310*math.sin(t),'kind':kind,'depth':0})
focus=next(s for s in stars if s['kind']=='blue-giant' and .30<math.hypot((s['X']-500)/390,(s['Y']-400)/310)<.65)
local=[]
for i,s in enumerate([focus,*[s for s in stars if s['kind']=='blue-giant'][5:8]]):local.append({'id':f'illustration.nebula.{i+1}','X':s['X'],'Y':s['Y'],'scaleX':45+i*9,'scaleY':35+i*6,'angle':[-.4,.8,-1.0,.25][i]})
fixture={'label':'ILLUSTRATIVE GALAXY FIXTURE / NOT CURRENT GENERATOR','seed':'8675309','width':1000,'height':800,'stars':stars,'nebulae':local,'focus':focus,'typeCounts':{k:sum(s['kind']==k for s in stars) for k in kinds},'classDistribution':'Deliberately illustrative, not an astrophysical population or persisted portable taxonomy','glyphsNotPhysicalScale':True};(OUT/'galaxy-fixture.js').write_text('window.GALAXY_FIXTURE='+json.dumps(fixture)+';',encoding='utf-8')
receipt={'artVersion':'freeform-nebula-galaxy-v2','layers':rows,'resolution':[W,H],'shape':'Asymmetric lobe union + warped ribbons + subtractive cavities; separate padded Cartesian edge fade; no influence ellipse clip','galaxy':'Illustrative seeded diffuse warped spiral density,600demonstration typed markers onZ0; no generator/topology change','elapsedSeconds':time.perf_counter()-start,'peakWorkingSetBytes':memory(),'runtime':sys.executable,'processId':os.getpid(),'sourceHelperSha256':hashlib.sha256(helper.read_bytes()).hexdigest(),'mapContextSha256':hashlib.sha256(context.read_bytes()).hexdigest(),'preservedSourcePin':binding['sourcePin'],'referencePixelsUsed':False,'gpuUsed':False};(OUT/'generation.json').write_text(json.dumps(receipt,indent=2),encoding='utf-8');(OUT/'fields.js').write_text('window.FIELDS='+json.dumps(rows)+';',encoding='utf-8');print(json.dumps({k:receipt[k] for k in ['elapsedSeconds','peakWorkingSetBytes','shape']}))
