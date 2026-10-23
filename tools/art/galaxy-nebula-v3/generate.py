"""One bounded original CPU asset/proof batch. See versioned contract before running."""
import ast, ctypes, gc, hashlib, json, math, os, sys, time
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT=Path(__file__).resolve().parents[3]
OUT=Path(sys.argv[1]).resolve()
assert not OUT.exists(), 'Preserve every previous batch'
OUT.mkdir(parents=True)
start=time.perf_counter(); peak=0
VERSION='galaxy-nebula-detail-v3'
helper=ROOT/'tools/art/nebula-volume-v1/generate.py'
tree=ast.parse(helper.read_text(encoding='utf-8'))
pure=[n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name in ['memory','smooth','noise','fbm']]
exec(compile(ast.Module(body=pure,type_ignores=[]),str(helper),'exec'),globals())

def check(stage):
    memory()
    elapsed=time.perf_counter()-start
    assert elapsed<180, f'180 second limit at {stage}; no automatic retry'
    print(stage, round(elapsed,2),'seconds',round(peak/1048576,1),'MiB',flush=True)

def sha(p): return hashlib.sha256(Path(p).read_bytes()).hexdigest()
def write(name,value): (OUT/name).write_text(json.dumps(value,indent=2),encoding='utf-8')
def multiscale(seed):
    rng=np.random.default_rng(seed); out=np.zeros((2048,2048),np.float32)
    for size,weight in [(8,.27),(16,.23),(32,.18),(64,.13),(128,.09),(256,.06),(512,.04)]:
        small=rng.random((size,size),dtype=np.float32)
        im=Image.fromarray(small).resize((2048,2048),Image.Resampling.BILINEAR)
        out+=np.asarray(im)*weight
        memory()
    return out

# High-resolution disk coordinates, dust kept independent from emission.
n=multiscale(42); n2=multiscale(10042)
rng=np.random.default_rng(420003)
atlas=np.zeros((2048,2048,4),np.uint8)
axis=np.linspace(-1.15,1.15,2048,dtype=np.float32)
for row in range(0,2048,64):
    x,y=np.meshgrid(axis,axis[row:row+64]); a=n[row:row+64]; b=n2[row:row+64]
    radius=np.hypot(x,y); theta=np.arctan2(y,x)
    warp=(a-.5)*.40+(b-.5)*.17
    phase=2*(theta+2.32*np.log(radius+.065)+warp)
    arm=np.exp(-np.sin(phase*.5)**2/.021)
    branches=np.exp(-np.sin((phase*1.68+radius*4.2+(b-.5)*3)*.5)**2/.012)
    branches*=np.clip((radius-.18)*3,0,1)*.43
    disk=np.exp(-radius*2.9)*np.clip((1-radius)*9,0,1)
    knot=np.clip((a-.43)*5,0,1)**2
    grain=rng.gamma(.52,1.6,(len(y),2048)).astype(np.float32)
    emission=disk*(.025+(arm+branches)*(.30+1.9*knot))
    emission*=.40+.70*grain
    core=np.exp(-radius/.068)*1.15
    emission+=core
    dust=np.exp(-np.sin((phase+.27+(b-.5)*1.3)*.5)**2/.007)
    dust+=.55*np.exp(-np.sin((phase*2.3+radius*9+(a-.5)*6)*.5)**2/.006)
    dust*=disk*(.35+np.clip((b-.35)*7,0,2))*3.6
    fade=np.clip((1-radius)*18,0,1)
    warmth=np.exp(-radius*6)
    for c,(cold,warm) in enumerate(zip([.69,.80,1.0],[1.,.77,.48])):
        value=emission*(cold*(1-warmth)+warm*warmth)*fade
        atlas[row:row+64,:,c]=np.uint8(np.clip(value,0,1)*255+.5)
    atlas[row:row+64,:,3]=np.uint8(np.clip(dust/4,0,1)*fade*255+.5)
    memory()
Image.fromarray(atlas,'RGBA').save(OUT/'galaxy-linear-rgba.png')
del n,n2,x,y,a,b,phase,arm,branches,disk,knot,grain,emission,core,dust,fade,warmth,value
gc.collect();check('macro atlas written')

NX,NY,NZ=128,96,64
volume=np.zeros((NZ,NY,NX,4),np.uint8)
x,y=np.meshgrid(np.linspace(-2.5,2.5,NX,dtype=np.float32),np.linspace(-1.65,1.65,NY,dtype=np.float32))
for iz,z in enumerate(np.linspace(-1,1,NZ)):
    wx=fbm(x*1.6+3,y*1.6,z*.8,1237,3)-.5
    wy=fbm(x*1.6,y*1.6+9,z*.8,1250,3)-.5
    u=.94*x+.34*y+wx*.36; v=-.34*x+.94*y+wy*.30
    rough=fbm(u*4,v*4,z*.8,1268,4); fine=fbm(u*19,v*19,z*2.4,1244,3)
    body=np.exp(-((u+.63)**2/.55+(v+.09)**2/.10+(z+.14)**2/.45))
    body+=.95*np.exp(-((u-.36)**2/.24+(v-.20)**2/.26+(z-.22)**2/.33))
    body+=.8*np.exp(-((u-1.15)**2/.22+(v+.35)**2/.035+(z+.35)**2/.20))
    ridge=v-(.27*np.sin(u*2.4)-.19)
    ribbon=np.exp(-(ridge/.050)**2-((z-.2*np.sin(u*2))/.33)**2)*np.exp(-u*u/2.8)
    thread=np.exp(-((v+.44+.13*np.cos(u*3))/.024)**2-((z+.3)/.35)**2)*np.exp(-((u+.3)/1.6)**4)
    cavity=1-.92*np.exp(-((u+.08)**2/.13+(v+.02)**2/.09+(z+.04)**2/.7))
    shape=(body+ribbon*.8+thread*.6)*cavity
    density=shape*np.clip((rough-.32)*3.7,0,1.8)**1.5*(.4+fine)
    dust=shape*np.exp(-((ridge+.11)/.095)**2)*np.clip((rough-.27)*4,0,2)*np.exp(-((z-.2)/.5)**2)
    guard=np.clip((2.5-abs(x))/.4,0,1)*np.clip((1.65-abs(y))/.3,0,1)*np.clip((1-abs(z))/.20,0,1)
    j=(density+ribbon*.2+thread*.20)*guard
    mix=np.clip(.4+u*.35+v*.6,0,1)
    for c,(cool,warm) in enumerate(zip([.13,.57,.78],[.80,.24,.42])):
        col=cool*(1-mix)+warm*mix
        volume[iz,:,:,c]=np.uint8(np.clip(j*col,0,1)*255+.5)
    volume[iz,:,:,3]=np.uint8(np.clip((density*.30+dust*.85)*guard,0,1)*255+.5)
    memory()
volume[[0,-1]]=0;volume[:,[0,-1]]=0;volume[:,:,[0,-1]]=0
volume.tofile(OUT/'nebula-linear-rgba.raw')
check('coherent nebula written')

rng=np.random.default_rng(421237);particles=[]
while len(particles)<256:
    xi=int(rng.integers(1,NX-1));yi=int(rng.integers(1,NY-1));zi=int(rng.integers(1,NZ-1))
    weight=float(volume[zi,yi,xi,:3].max())/255
    if rng.random()>weight*.8:continue
    particles.append({'index':len(particles),'position':[(xi/(NX-1)*2-1)*2.5,(yi/(NY-1)*2-1)*1.65,zi/(NZ-1)*2-1],
                      'radius':float(rng.uniform(.008,.026)),'brightness':float(rng.uniform(.035,.11))})
write('nebula-particles.json',{'version':VERSION,'seed':421237,'visibleTotalCap':256,'points':particles})

contextPath=Path('E:/HobbyDS9/artifacts/art/nebula-volume-v1-20261023-2/map-context.js')
contextBytes=contextPath.read_bytes();(OUT/'map-context.js').write_bytes(contextBytes)
context=json.loads(contextBytes.decode('utf-8').split('=',1)[1].rstrip(';'))
m=context['map'];layout=m['Layout'];feat=context['feature'];center=feat['Position']
write('map-provenance.json',{'source':str(contextPath),'sha256':sha(contextPath),'seed':m['Settings']['Map']['Seed'],
 'mapContentHash':m['ContentHash'],'systemCount':len(layout['Systems']),'feature':feat,'routeNote':context['routeNote'],
 'navigationChanged':False,'typedStarsInvented':False})

def bilerp(data,u,v):
    h,w=data.shape[:2];u=np.clip(u,0,1)*(w-1);v=np.clip(v,0,1)*(h-1)
    ix=np.floor(u).astype(np.int32);iy=np.floor(v).astype(np.int32);fx=(u-ix)[...,None];fy=(v-iy)[...,None]
    jx=np.minimum(ix+1,w-1);jy=np.minimum(iy+1,h-1)
    return (data[iy,ix]*(1-fx)+data[iy,jx]*fx)*(1-fy)+(data[jy,ix]*(1-fx)+data[jy,jx]*fx)*fy

def trilerp(u,v,w):
    z=np.clip(w,0,1)*(NZ-1);iz=np.floor(z).astype(np.int32);fz=(z-iz)[...,None]
    # All samples in a ray step share depth, avoiding temporary expanded volumes.
    return (bilerp(volume[iz],u,v)*(1-fz)+bilerp(volume[min(iz+1,NZ-1)],u,v)*fz)/255

def srgb(l):return np.where(l<=.0031308,l*12.92,1.055*np.maximum(l,0)**(1/2.4)-.055)
def tolinear(a):return np.where(a<=.04045,a/12.92,((a+.055)/1.055)**2.4)
def stats(a):
    a=tolinear(np.asarray(a,dtype=np.float32)/255);lum=a@np.array([.2126,.7152,.0722],np.float32)
    return {'linearLuminanceMean':float(lum.mean()),'p50':float(np.quantile(lum,.50)),
            'p95':float(np.quantile(lum,.95)),'p99':float(np.quantile(lum,.99))}

referencePaths=[Path('C:/Users/ambal/AppData/Local/Temp/codex-clipboard-a84f80b1-fc47-4e0c-b375-6bb50c6cd2e0.png'),
 Path('C:/Users/ambal/AppData/Local/Temp/codex-clipboard-dbad6025-ca16-46d2-850f-6ff40c749c2b.png')]
calibration={'references':[],'referencePixelsRedistributed':False,'macroGain':.12,'linearFloor':[.0015,.0023,.004]}
for p in referencePaths:
    im=Image.open(p).convert('RGB');calibration['references'].append({'source':str(p),'sha256':sha(p),'stats':stats(im)})
old=Path('E:/HobbyDS9/artifacts/galaxy-volume-engine-5cb498d-20261023/screenshots/1920x1080-volume-1000-overview.png')
calibration['accepted5cb']={'source':str(old),'sha256':sha(old),'cropXYXY':[360,232,1240,936],
 'stats':stats(Image.open(old).convert('RGB').crop((360,232,1240,936))),'differentMapSystemCount':1000}

FONT='C:/Windows/Fonts/segoeui.ttf';BOLD='C:/Windows/Fonts/segoeuib.ttf'
font=ImageFont.truetype(FONT,16);small=ImageFont.truetype(FONT,13);title=ImageFont.truetype(BOLD,24)
W,H=960,640;RW,RH=480,320
pane=(20,104);proofRows=[];tracks=[]

def capture(file,label,cx,cy,worldWidth,pan=0):
    worldHeight=worldWidth*H/W
    gx,gy=np.meshgrid(np.linspace(-worldWidth/2,worldWidth/2,RW,dtype=np.float32),np.linspace(-worldHeight/2,worldHeight/2,RH,dtype=np.float32))
    planeX=cx+gx+pan;planeY=cy+gy
    dx=(planeX/layout['Width']-.5)/.47
    dy=(planeY/layout['Height']-.5)*layout['Height']/layout['Width']/.47
    ang=math.radians(32);diskx=math.cos(ang)*dx+math.sin(ang)*dy
    disky=(-math.sin(ang)*dx+math.cos(ang)*dy)/math.cos(math.radians(48))
    uvx=(diskx/1.15+1)/2;uvy=(disky/1.15+1)/2
    field=bilerp(atlas,uvx,uvy)/255
    inside=(abs(diskx)<1.15)&(abs(disky)<1.15)
    light=np.broadcast_to(np.array([.0015,.0023,.004],np.float32),(RH,RW,3)).copy()
    light+=field[:,:,:3]*.12*np.exp(-field[:,:,3:]*4*.65)*inside[:,:,None]
    # CPU continuous volume integration; no sprite/card slices in the proof.
    transmission=np.ones((RH,RW,1),np.float32);emitted=np.zeros((RH,RW,3),np.float32)
    for z in np.linspace(.984375,-.984375,48):
        q=4/(4-z*.8)
        sampleX=cx+(planeX-cx)/q;sampleY=cy+(planeY-cy)/q
        lx=(sampleX-center['X'])/feat['RadiusX'];ly=(sampleY-center['Y'])/feat['RadiusY']
        a=-.28;u=math.cos(a)*lx-math.sin(a)*ly;v=math.sin(a)*lx+math.cos(a)*ly
        rgba=trilerp((u/2.5+1)/2,(v/1.65+1)/2,(z+1)/2)
        mask=((abs(u)<2.5)&(abs(v)<1.65))[:,:,None]
        sigma=rgba[:,:,3:]*4*mask;j=rgba[:,:,:3]*.30*mask
        absorb=1-np.exp(-sigma*(2/48))
        emitted+=transmission*j/np.maximum(sigma,1e-5)*absorb
        transmission*=1-absorb
    light=light*transmission+emitted
    def project(x,y,z=0):
        q=4/(4-z*.8)
        return W/2+((x-cx)*q-pan)*W/worldWidth,H/2+(y-cy)*q*W/worldWidth
    # Stable, depth-distributed soft points, one field/256 total in this proof.
    for p in particles:
        u,v,z=p['position'];a=-.28
        lx=math.cos(a)*u+math.sin(a)*v;ly=-math.sin(a)*u+math.cos(a)*v
        px,py=project(center['X']+lx*feat['RadiusX'],center['Y']+ly*feat['RadiusY'],z)
        radius=max(.5,p['radius']*feat['RadiusX']*W/worldWidth*4/(4-z*.8))/2
        px/=2;py/=2
        xa=max(0,int(px-radius*3));xb=min(RW,int(px+radius*3)+1)
        ya=max(0,int(py-radius*3));yb=min(RH,int(py+radius*3)+1)
        if xa<xb and ya<yb:
            sx,sy=np.meshgrid(np.arange(xa,xb),np.arange(ya,yb))
            glow=np.exp(-((sx-px)**2+(sy-py)**2)/(radius*radius)*2)
            light[ya:yb,xa:xb]+=glow[:,:,None]*np.array([.72,.86,1.])*p['brightness']*.14
    image=Image.fromarray(np.uint8(np.clip(srgb(light),0,1)*255),'RGB').resize((W,H),Image.Resampling.BILINEAR)
    pixel=ImageDraw.Draw(image)
    rawStats=stats(image)
    for region in layout['Regions']:
        points=[project(p['X'],p['Y']) for p in region['Boundary']]
        if points:pixel.line(points+[points[0]],fill=(45,64,80),width=1)
    route=[project(p['X'],p['Y']) for p in context['route']]
    pixel.line(route,fill=(1,5,12),width=7);pixel.line(route,fill=(232,185,112),width=2)
    count=0
    for f in m['Features']:
        if f['Kind']!=1:continue
        px,py=project(f['Position']['X'],f['Position']['Y'])
        if 0<=px<W and 0<=py<H:
            count+=1;pixel.ellipse((px-3.5,py-3.5,px+3.5,py+3.5),fill=(2,6,14))
            pixel.ellipse((px-1.8,py-1.8,px+1.8,py+1.8),fill=(214,234,242))
    canvas=Image.new('RGB',(1280,800),(5,10,17));d=ImageDraw.Draw(canvas)
    d.text((20,18),label,font=title,fill=(243,187,111))
    d.text((20,58),'CPU ART PROOF  |  Actual seed 42 / 250 map  |  Stored navigation unchanged',font=font,fill=(169,191,208))
    canvas.paste(image,pane);d.rectangle((19,103,980,744),outline=(76,101,121),width=1)
    notes=['ORIGINAL MORPHOLOGY','Branching spiral arms','Compact warm bulge','Independent dust lanes','Fine starlight grain','','LOCAL NEBULA','Continuous 3D field','Wisps / knots / cavities','256 depth points total','','Navigation on z = 0',f'{count} visible actual stars','','Proposed visual assets','No Unity/GPU acceptance']
    for i,text in enumerate(notes):d.text((1000,112+i*27),text,font=small,fill=(229,182,119) if i in [0,7] else (171,191,206))
    d.text((20,760),'Macro: integrated fallback preview. Nebula: CPU volume integration + seeded depth points. Not a runtime capture.',font=small,fill=(149,171,190))
    canvas.save(OUT/file)
    proofRows.append({'file':file,'camera':[cx,cy],'worldWidth':worldWidth,'panWorld':pan,'visibleActualStars':count,'backgroundStats':rawStats})
    if pan!=0:
        nearest=max(particles,key=lambda p:p['position'][2]);far=min(particles,key=lambda p:p['position'][2])
        for name,p in [('near',nearest),('far',far)]:
            u,v,z=p['position'];a=-.28;xx=center['X']+(math.cos(a)*u+math.sin(a)*v)*feat['RadiusX'];yy=center['Y']+(-math.sin(a)*u+math.cos(a)*v)*feat['RadiusY']
            # Pan is a camera translation: both sample rays and point projection use the shifted centre.
            tracks.append({'frame':file,'name':name,'localZ':z,'q':4/(4-z*.8),'screenXY':list(project(xx,yy,z))})
    check(file)

capture('01-actual-map-overview.png','DETAILED GALAXY / ACTUAL MAP',layout['Width']/2,layout['Height']/2,layout['Width']*1.12)
capture('02-actual-map-regional.png','NEBULA / ACTUAL REGIONAL MAP',center['X'],center['Y'],feat['RadiusX']*6)
# Shift camera centres, not geometry or star anchors, for the two matched pan views.
shift=feat['RadiusX']*.38
capture('03-pan-left.png','DEPTH PROOF / CAMERA LEFT',center['X']-shift,center['Y'],feat['RadiusX']*6,pan=-1)
capture('04-pan-right.png','DEPTH PROOF / CAMERA RIGHT',center['X']+shift,center['Y'],feat['RadiusX']*6,pan=1)
write('calibration.json',calibration)
write('proof.json',{'captures':proofRows,'particleTracks':tracks,'framesTotal':4,
 'sameStoredMap':True,'macroPreview':'integrated 2D fallback, receiving contract requires continuous thickness',
 'nebulaPreview':'continuous trilinear 48-step CPU integration, depth-projected soft points',
 'runtimeAcceptance':False,'gpuTimingMeasured':False})
faces=[volume[0],volume[-1],volume[:,0],volume[:,-1],volume[:,:,0],volume[:,:,-1]]
assert all(int(f.max())==0 for f in faces)
assert all(int(f.max())==0 for f in [atlas[0],atlas[-1],atlas[:,0],atlas[:,-1]])
check('batch complete')
write('generation.json',{'version':VERSION,'elapsedSeconds':time.perf_counter()-start,'peakWorkingSetBytes':peak,
 'processId':os.getpid(),'runtime':sys.executable,'gpuUsed':False,'referencePixelsUsed':False,
 'sourceSha256':sha(__file__),'helperSha256':sha(helper),'contractSha256':sha(ROOT/'docs/art/galaxy-nebula-detail-v3.md'),
 'mapContextSha256':sha(contextPath),'galaxyDimensions':[2048,2048,4],'volumeDimensionsXYZ':[NX,NY,NZ],
 'galaxySeed':42,'volumeSeed':1237,'particleSeed':421237,'sixFaceMaximum':0,'atlasEdgeMaximum':0,
 'capturesTotal':4,'boundedGenerationSeconds':180,'boundedMemoryBytes':512*1048576})
payload=[{'file':str(p.relative_to(OUT)).replace('\\','/'),'bytes':p.stat().st_size,'sha256':sha(p)} for p in sorted(OUT.rglob('*')) if p.is_file()]
write('payload-manifest.json',payload)
