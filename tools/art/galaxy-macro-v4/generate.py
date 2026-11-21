"""Original HobbyDS9 macro galaxy v4. CPU only; no reference photograph pixels.
Run with bundled Python/numpy/Pillow. Never overwrites a previous payload.
"""
from pathlib import Path
import argparse,hashlib,json,math,shutil,time
import numpy as np
from PIL import Image,ImageDraw

parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--output',required=True,type=Path,help='New, absent output directory')
args=parser.parse_args()
OUT=args.output.resolve()
assert not OUT.exists(), 'Output directory must be absent; preserve all prior batches'
OUT.mkdir(parents=True)
shutil.copyfile(Path(__file__).resolve(),OUT/'generate.py')
START=time.monotonic(); N=2048; SEED=420004
rng=np.random.default_rng(SEED)
def noise(seed):
    rr=np.random.default_rng(seed); field=np.zeros((N,N),np.float32)
    for size,weight in [(6,.23),(12,.22),(24,.18),(48,.14),(96,.10),(192,.08),(384,.05)]:
        field+=np.asarray(Image.fromarray(rr.random((size,size),dtype=np.float32)).resize((N,N),Image.Resampling.BICUBIC))*weight
    return field
def smooth(a,b,x):
    t=np.clip((x-a)/(b-a),0,1);return t*t*(3-2*t)
def angle(a):return np.arctan2(np.sin(a),np.cos(a))
large=noise(SEED);small=noise(SEED+1)
axis=np.linspace(-1.15,1.15,N,dtype=np.float32)
atlas=np.zeros((N,N,4),np.uint8)
for row in range(0,N,64):
    assert time.monotonic()-START<150,'Bounded generation timeout'
    x,y=np.meshgrid(axis,axis[row:row+64]);a=large[row:row+64];b=small[row:row+64]
    # Low-amplitude coordinate warp; no symmetric ring/oval support silhouette.
    wx=x+(a-.5)*.065;wy=y+(b-.5)*.065
    r=np.hypot(wx,wy);theta=np.arctan2(wy,wx)
    fade=1-smooth(.82,1.0,r)
    phase=theta+1.95*np.log(r+.13)+(a-.5)*.65+.13*np.sin(theta*3+r*4)
    width=.16+.09*(1-r)+.08*b
    arms=np.zeros_like(r);dust=np.zeros_like(r)
    for i,(p,strength) in enumerate([(0,1.0),(math.pi+.22,.77)]):
        delta=angle(phase-p)
        ridge=np.exp(-.5*(delta/width)**2)
        radial=smooth(.12,.27,r)*(1-smooth(.64+.11*i,1.0,r))
        broken=.32+.68*smooth(.28,.68,a+.10*np.sin(r*19+i*2))
        arms+=strength*ridge*radial*broken
        dd=angle(phase-p-.20-.08*np.sin(r*11+i))
        dust+=np.exp(-.5*(dd/(width*.46))**2)*radial*smooth(.34,.63,b)*(.6+.4*a)
    # Two partial spurs independently terminate and curve, instead of doubling every arm.
    for p,lo,hi,strength in [(1.13,.34,.72,.30),(4.55,.46,.91,.24)]:
        delta=angle(phase+.6*r-p+(b-.5)*.42)
        arms+=strength*np.exp(-.5*(delta/(width*.82))**2)*smooth(lo,lo+.08,r)*(1-smooth(hi-.08,hi,r))*smooth(.23,.66,a)
    # Widespread unresolved stellar population connects arms into a luminous disk.
    disk=np.exp(-r*2.9)*fade
    clumps=.60+1.25*smooth(.28,.72,a)*smooth(.25,.75,b)
    grain=rng.gamma(1.3,1/1.3,r.shape).astype(np.float32)
    emission=disk*(.125+.95*arms*clumps)*(.78+.22*grain)
    # Elliptical old-star bulge, a broad halo and differentiated compact nucleus.
    re=np.sqrt((x/1.10)**2+(y/.86)**2)
    bulge=.61*np.exp(-re/.115)+.10*np.exp(-re*re/.035)
    nucleus=.13*np.exp(-(re/.021)**2)
    emission+=bulge*(.96+.04*a)+nucleus
    # Fragmented extinction: coarse clouds + offset arm-edge dust, tapered near nucleus.
    patch=smooth(.45,.69,b)*smooth(.31,.66,a)*.32
    extinction=(dust*.48+patch)*np.exp(-r*1.45)*fade*smooth(.035,.14,r)
    warmth=np.exp(-r*4)
    tint=np.stack([np.ones_like(r),.965+.012*(1-warmth),.905+.035*(1-warmth)],axis=-1)
    atlas[row:row+64,:,:3]=np.uint8(np.clip(emission[:,:,None]*tint,0,1)*255+.5)
    atlas[row:row+64,:,3]=np.uint8(np.clip(extinction,0,1)*255+.5)
atlas[[0,-1]]=0;atlas[:,[0,-1]]=0
Image.fromarray(atlas,'RGBA').save(OUT/'galaxy-linear-rgba.png')

def srgb(x):return np.where(x<=.0031308,x*12.92,1.055*np.maximum(x,0)**(1/2.4)-.055)
def sample(x,y):
    u=np.clip((x+1.15)/2.3*(N-1),0,N-1);v=np.clip((y+1.15)/2.3*(N-1),0,N-1)
    ix=u.astype(np.int32);iy=v.astype(np.int32);fx=(u-ix)[...,None];fy=(v-iy)[...,None]
    jx=np.minimum(ix+1,N-1);jy=np.minimum(iy+1,N-1)
    return ((atlas[iy,ix]*(1-fx)+atlas[iy,jx]*fx)*(1-fy)+(atlas[jy,ix]*(1-fx)+atlas[jy,jx]*fx)*fy)/255
stats={}
for name,inclined in [('face-on',False),('inclined',True)]:
    size=960;u,v=np.meshgrid(np.linspace(-1.23,1.23,size,dtype=np.float32),np.linspace(-1.23,1.23,size,dtype=np.float32))
    if inclined:
        c=math.cos(math.radians(32));s=math.sin(math.radians(32))
        x=c*u+s*v;y=(-s*u+c*v)/math.cos(math.radians(48))
    else:x,y=u,v
    data=sample(x,y);inside=(abs(x)<=1.15)&(abs(y)<=1.15)
    # Illustrative single column only, not the Unity twelve-bin optical integration.
    light=np.array([.0015,.0023,.004])+data[:,:,:3]*.12*np.exp(-4*data[:,:,3:]*.65)*inside[:,:,None]
    im=Image.fromarray(np.uint8(np.clip(srgb(light),0,1)*255+.5),'RGB')
    im.save(OUT/f'preview-{name}.png')
    lum=light@np.array([.2126,.7152,.0722]);stats[name]={'meanLinear':float(lum.mean()),'p95Linear':float(np.quantile(lum,.95)),'p99Linear':float(np.quantile(lum,.99))}
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
contract={'version':'galaxy-macro-v4','seed':SEED,'originalProceduralArt':True,'referencePixelsUsed':False,
 'sourceLineage':'Original HobbyDS9 v3 CPU recipe, rewritten macro morphology; v3 local volume/particles untouched.',
 'dimensions':[2048,2048],'format':'RGBA8 PNG, linear numerical channels; sRGB importer OFF',
 'channels':{'RGB':'Independent unpremultiplied stellar emission, including bulge+nucleus','A':'Independent dust extinction, not transparency'},
 'orientation':'PNG row0 is authored y=-1.15; x grows left-to-right, y grows row-down; one existing Unity row/UV correction',
 'bounds':[-1.15,1.15,-1.15,1.15],'support':'Smooth zero by radius1; >.15 black padding, explicit zero exterior faces',
 'import':'RGBA32, uncompressed, linear, nonreadable, Clamp/Trilinear, independent box-filtered complete mips; alphaIsTransparency=false',
 'gpuAtlasBytesWithMips':22369620,'runtime':'Use asset-emission partition into thin disk and thick bulge; do not add/replace a second analytical core. Existing12 ray bins and depth unchanged.',
 'previewLimit':'CPU illustrative single-column .12 RGB exp(-4*A*.65), sRGB output. Not actual Unity shader, parallax, visibility, GPU performance or art acceptance evidence.',
 'previewStats':stats,'generationSeconds':time.monotonic()-START,
 'hashes':{name:sha(OUT/name) for name in ('generate.py','galaxy-linear-rgba.png','preview-face-on.png','preview-inclined.png')}}
(OUT/'contract.json').write_text(json.dumps(contract,indent=2),encoding='utf-8')
print(json.dumps({'output':str(OUT),'seconds':time.monotonic()-START,'hashes':contract['hashes']}))
