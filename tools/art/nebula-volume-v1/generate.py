"""Original CPU 3D-density concept. No reference pixels, GPU or Unity usage."""
import ctypes,hashlib,json,math,os,sys,time
from pathlib import Path
import numpy as np
from PIL import Image,ImageDraw,ImageFont,ImageFilter

ROOT=Path(__file__).resolve().parents[3];OUT=Path(sys.argv[1]).resolve()
assert not OUT.exists(),'Preserve previous render evidence'
OUT.mkdir(parents=True);start=time.perf_counter();peak=0
W,H,N=512,320,32
xx,yy=np.meshgrid(np.linspace(-1.38,1.38,W,dtype=np.float32),np.linspace(-1.12,1.12,H,dtype=np.float32))
def memory():
    global peak
    class Counters(ctypes.Structure):
        _fields_=[('cb',ctypes.c_ulong),('PageFaultCount',ctypes.c_ulong)]+[(k,ctypes.c_size_t) for k in ['PeakWorkingSetSize','WorkingSetSize','QuotaPeakPagedPoolUsage','QuotaPagedPoolUsage','QuotaPeakNonPagedPoolUsage','QuotaNonPagedPoolUsage','PagefileUsage','PeakPagefileUsage']]
    p=Counters();p.cb=ctypes.sizeof(p)
    ctypes.windll.kernel32.GetCurrentProcess.restype=ctypes.c_void_p
    fn=ctypes.windll.psapi.GetProcessMemoryInfo;fn.argtypes=[ctypes.c_void_p,ctypes.c_void_p,ctypes.c_ulong]
    assert fn(ctypes.windll.kernel32.GetCurrentProcess(),ctypes.byref(p),p.cb),'Memory measurement failed'
    peak=max(peak,p.PeakWorkingSetSize);assert peak<512*1024**2,'CPU concept memory bound'
    return peak
def smooth(t):return t*t*t*(t*(t*6-15)+10)
def noise(x,y,z,s):
    ix=np.floor(x);iy=np.floor(y);iz=np.floor(z);fx=smooth(x-ix);fy=smooth(y-iy);fz=smooth(z-iz)
    def h(a,b,c):
        v=np.sin(a*127.1+b*311.7+c*74.7+s*19.19)*43758.5453
        return v-np.floor(v)
    def lerp(a,b,t):return a+(b-a)*t
    a=lerp(h(ix,iy,iz),h(ix+1,iy,iz),fx);b=lerp(h(ix,iy+1,iz),h(ix+1,iy+1,iz),fx)
    c=lerp(h(ix,iy,iz+1),h(ix+1,iy,iz+1),fx);d=lerp(h(ix,iy+1,iz+1),h(ix+1,iy+1,iz+1),fx)
    return lerp(lerp(a,b,fy),lerp(c,d,fy),fz)
def fbm(x,y,z,s,octaves=5):
    value=np.zeros_like(x);amp=1.;total=0
    for i in range(octaves):
        value+=amp*noise(x,y,z,s+i*7);total+=amp;x=x*2.03+17.2;y=y*2.03-9.4;z=z*2.03+3.1;amp*=.51
    return value/total
def field(style,z,seed):
    wx=fbm(xx*1.5+4,yy*1.5,z*1.5,seed,3)-.5
    wy=fbm(xx*1.5,yy*1.5+9,z*1.5,seed+13,3)-.5
    x=xx+wx*.40;y=yy+wy*.42
    n=fbm(x*3.2,y*3.2,z*.9,seed+31)
    fine=fbm(x*22+3,y*22,z*2.4,seed+7,3)
    env=np.clip(1-(xx/1.30)**2-(yy/1.06)**2-z*z*.65,0,1)**.60
    cloud=np.clip((n-.36)*4.2,0,1.6)**1.65
    if style=='rim':
        r=np.sqrt(((x+.12)/.84)**2+((y+.05)/.83)**2)
        shell=np.exp(-((r-(.70+.18*wx))/.22)**2)
        body=(.23+shell*1.7)*env
        dust=np.exp(-((x+.10+.34*y)/(.21+.07*np.sin(y*5)))**2)*np.clip(y+.75,0,1)*2.2
        mix=np.clip((x*.5-y*.9+.15),0,1)
    elif style=='pillars':
        dust=np.zeros_like(x)
        for cx,top,height,width in [(-.47,-.65,1.3,.12),(-.05,-.30,1.,.105),(.36,-.50,1.2,.09)]:
            bend=cx+.09*np.sin(y*3+cx*8)+wx*.18
            pillar=np.exp(-((x-bend)/(width*(1+.6*(y+.4))))**2)*np.clip((y-top)*12,0,1)*np.clip((top+height-y)*8,0,1)
            dust+=pillar*3
        body=(.34+1.3*np.exp(-((x+.1)**2+(y+.15)**2)*2))*env
        mix=np.zeros_like(x)
    elif style=='lanes':
        body=(.4+1.3*np.exp(-((x-.2)**2/.4+(y+.12)**2/.7)))*env
        lane=np.exp(-((y+.15-.27*np.sin(x*4.1)+wx*.2)/.055)**2)
        branch=np.exp(-((x+.15+.3*y+wy*.2)/.055)**2)*np.clip(y+.8,0,1)
        dust=(lane+branch)*2.6
        mix=np.clip(.5+y*.9+x*.2,0,1)
    else:
        r=np.sqrt(((x+.04)/.95)**2+((y+.05)/.72)**2)
        body=(.35+1.25*np.exp(-((r-.62)/.30)**2))*env
        dust=(np.exp(-((y-.32-.2*np.sin(x*5)+wy*.3)/.10)**2)+np.exp(-((x+.47+.17*np.sin(y*4))/ .07)**2))*1.6
        mix=np.clip(.5+x*.65-y*.25,0,1)
    emitting=body*cloud*(.4+fine*1.3)
    dust=dust*env*(.25+cloud*1.4)*np.exp(-((z-.25)/.56)**2)
    # Structured backlit rim: coherent density gradient, no copied light texture.
    gy,gx=np.gradient(dust);rim=np.clip(np.sqrt(gx*gx+gy*gy)*5.5,0,1.8)
    rim*=np.clip(.8+(-gx+gy)*3,0,1.5)
    colourA=np.array([.05,.52,.66]);colourB=np.array([1.,.34,.12])
    if style in ('lanes','orion'):colourA=np.array([.08,.34,.90]);colourB=np.array([.95,.13,.40])
    colour=colourA[None,None,:]*(1-mix[...,None])+colourB[None,None,:]*mix[...,None]
    light=np.exp(-((x+.32)**2+(y+.25)**2+(.5-z)**2)*1.7)
    lit=colour*(.28+light[...,None]*1.1)+rim[...,None]*np.array([1.,.72,.28])[None,None,:]*1.6
    emitting+=rim*.22
    extinction=emitting*.85+dust*3.8
    alpha=1-np.exp(-extinction*(2/N)*1.15)
    rgb=(lit*emitting[...,None]+np.array([.005,.005,.009])*dust[...,None]*3.8)/(extinction[...,None]+1e-5)
    rgb=np.clip(rgb*1.65,0,1)**(1/1.35)
    return np.dstack((rgb,alpha))

styles=[('rim','A · Turquoise cloud / amber dust rim',1021),('pillars','B · Backlit pillars / quiet teal',2033),('lanes','C · Blue / rose emission with dark lanes',3079),('orion','D · Luminous cavity / violet-blue veil',4051)]
rows=[];assets=[]
for style,title,seed in styles:
    folder=OUT/style;folder.mkdir();layers=[];beauty=Image.new('RGBA',(W,H),(3,7,14,255))
    # Far to near; every slab samples the same coherent 3D field.
    for i,z in enumerate(np.linspace(-1,1,N)):
        a=field(style,float(z),seed);im=Image.fromarray(np.uint8(np.clip(a,0,1)*255),'RGBA');file=f'{style}/depth-{i:02}.png';im.save(OUT/file)
        layers.append({'file':file,'zNormalized':float(z),'zOverCameraDistance':float(z)*.18});beauty=Image.alpha_composite(beauty,im);memory()
    beauty=beauty.convert('RGB').resize((1024,640),Image.Resampling.LANCZOS)
    # Illustration stars are confined to treatment previews, never the map demo.
    rng=np.random.default_rng(seed);draw=ImageDraw.Draw(beauty)
    for _ in range(230):
        x=int(rng.integers(10,1014));y=int(rng.integers(10,630));b=int(rng.integers(55,170));draw.point((x,y),fill=(b,int(b*.91),min(255,int(b*1.1))))
    file=f'{style}/concept.png';beauty.save(OUT/file);rows.append({'id':style,'title':title,'seed':seed,'layers':layers,'preview':file});print(f'{style}:32 coherent depth layers; peak {memory()/1024**2:.1f}MiB',flush=True)
    assert time.perf_counter()-start<240,'Bounded initial CPU batch time'

# Exact map context; no procedural replacement of navigable geometry.
mapPath=ROOT/'art/generated-galaxy-m1/route-intent-v5/natural/seed-42-systems-250.natural.json'
reportPath=mapPath.with_name('seed-42-systems-250.report.json');raw=json.loads(mapPath.read_text(encoding='utf-8-sig'));report=json.loads(reportPath.read_text(encoding='utf-8-sig'))
nodes={r['NodeId']:r for r in raw['Layout']['Regions']};leg=report['Plan']['Legs'][0];ids=[leg['OriginNodeId'],*leg['Route']['Steps']]
edges={}
for r in nodes.values():
    p=r['Boundary']
    for a,b in zip(p,p[1:]+p[:1]):edges.setdefault(tuple(sorted(((a['X'],a['Y']),(b['X'],b['Y'])))),[]).append(r['NodeId'])
route=[nodes[ids[0]]['Center']]
for a,b in zip(ids,ids[1:]):
    candidates=[e for e,owners in edges.items() if sorted(owners)==sorted([a,b])];assert candidates
    e=min(candidates,key=lambda e:'|'.join(sorted(','.join(map(str,p)) for p in e)))
    route.append({'X':(e[0][0]+e[1][0])/2,'Y':(e[0][1]+e[1][1])/2})
target=leg['Target'];route.append(next(f['Position'] for f in raw['Features'] if f['SystemId']==target['SystemId'] and f['Kind']==1))
feature=min((f for f in raw['Features'] if f['Kind']==3),key=lambda f:min(((p['X']-f['Position']['X'])/f['RadiusX'])**2+((p['Y']-f['Position']['Y'])/f['RadiusY'])**2 for p in route))
(OUT/'map-context.js').write_text('window.NEBULA_CONTEXT='+json.dumps({'map':raw,'feature':feature,'route':route,'routeResult':report['Plan'],'routeNote':'Actual public API plan, exact shared-edge midpoint presentation baseline; not certified shortest drawing.'})+';',encoding='utf-8')
sheet=Image.new('RGB',(2080,1450),(5,11,18));d=ImageDraw.Draw(sheet)
fontPath='C:/Windows/Fonts/segoeui.ttf';font=ImageFont.truetype(fontPath,24);small=ImageFont.truetype(fontPath,18)
d.text((28,16),'NEBULA MORPHOLOGY · ORIGINAL PROCEDURAL 3D DENSITY',fill='#efb16b',font=font)
d.text((28,53),'Four art treatments · 32 coherent depth slices each · CPU concept, not final Unity rendering',fill='#bfc6d7',font=small)
for i,row in enumerate(rows):
    x=22+(i%2)*1035;y=94+(i//2)*675;sheet.paste(Image.open(OUT/row['preview']),(x,y));d.text((x+12,y+644),row['title'],fill='#d8d9e6',font=small)
sheet.save(OUT/'concept-sheet.png')
receipt={'artVersion':'nebula-volume-concept-v1','density':'Coherent domain-warped 3D value-noise fBm plus explicit lobe/pillar/cavity/dust fields','depthSlices':N,'sliceResolution':[W,H],'depthRangeOverCameraDistance':[-.18,.18],'projection':'q=D/(D-z); map plane q=1','variantCount':4,'styles':rows,'mapContentHash':raw['ContentHash'],'mapFileSha256':hashlib.sha256(mapPath.read_bytes()).hexdigest(),'feature':feature,'elapsedSeconds':time.perf_counter()-start,'peakWorkingSetBytes':memory(),'processId':os.getpid(),'runtime':sys.executable,'gpuUsed':False,'referencePixelsUsed':False,'externalDownloads':False,'limits':'Depth-slice approximation of an original 3D field; no arbitrary-orbit raymarch or Unity performance acceptance.'}
(OUT/'generation.json').write_text(json.dumps(receipt,indent=2),encoding='utf-8');print(json.dumps({k:receipt[k] for k in ['elapsedSeconds','peakWorkingSetBytes','feature']}))
