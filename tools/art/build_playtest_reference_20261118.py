"""Original vector/layout reference only. No Unity/gameplay edits or external downloads."""
import hashlib
import html
import json
import re
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT/'art/playtest-20261118'
OUT.mkdir(parents=True, exist_ok=True)
(OUT/'icons').mkdir(exist_ok=True)
(OUT/'recovered').mkdir(exist_ok=True)

def write(path, text):
    (OUT/path).write_text(text, encoding='utf-8', newline='\n')

glyphs = {
    'ship': [[3,19,7,13,11,13,11,7,15,4,19,7,19,13,23,13,27,19,21,21,9,21,3,19], [7,13,7,6], [23,13,23,6], [12,24,18,24]],
    'supply': [[6,11,24,11,24,26,6,26,6,11], [10,11,10,6,20,6,20,11], [6,17,24,17], [15,11,15,26]],
    'shield': [[15,3,25,7,24,18,21,23,15,27,9,23,6,18,5,7,15,3]],
    'mission': [[5,23,11,23,11,15,20,15,20,7,26,7], [22,3,26,7,22,11], [3,20,7,20,7,26,3,26,3,20]],
    'survey': [[5,25,5,5,10,6,15,9,20,14,24,20,25,25,5,25], [5,25,18,12], [5,14,9,15,12,18,14,22,14,25]],
}
icon_spec = {'schema':1, 'version':'playtest-status-icons-v1', 'status':'original proposed receiving strokes; not runtime imports',
    'canvas':[30,30], 'strokeWidth':2, 'strokeColor':'inherit', 'fill':'none',
    'receiving':'Use existing ReviewIcon/Painter2D Line pattern. Each array is x,y pairs; every polyline is separately stroked. Do not add an SVG package.',
    'icons':{k:{'polylines':v,'meaning':{'ship':'Generic vessel only; exact disclosed hull thumbnail takes priority',
        'supply':'Actual general supply field only; not torpedo ammunition', 'shield':'Reported shield state; preserve sectors',
        'mission':'Accepted order/activity; not an unsent draft', 'survey':'Reported exploration/survey capability only'}[k]} for k,v in glyphs.items()}}
write('icon-strokes.json',json.dumps(icon_spec,indent=2)+'\n')

def svg(name, cls='icon'):
    paths=''.join('<polyline points="'+' '.join(f'{p[i]},{p[i+1]}' for i in range(0,len(p),2))+'"/>' for p in glyphs[name])
    return f'<svg class="{cls}" viewBox="0 0 30 30" aria-hidden="true" fill="none" stroke="currentColor" stroke-width="2" stroke-linejoin="round" stroke-linecap="round">{paths}</svg>'

for name in glyphs:
    write('icons/'+name+'.svg',svg(name).replace('class="icon"','xmlns="http://www.w3.org/2000/svg" width="30" height="30"')+'\n')

source = ROOT/'client/HobbyDS9.Unity/Assets/HobbyDS9/Review/Resources/ConstructionCardArt'
bindings = [('hull.constitution-explorer.v1','constitution.png'),('hull.galaxy-cruiser.v1','galaxy.jpg'),('hull.defiant-combat.v1','defiant.png'),
    ('infrastructure.fabrication-annex.v1','fusion.png'),('infrastructure.research-laboratory.v1','research.png'),
    ('infrastructure.hydroponic-extension.v1','hydroponics.png'),('infrastructure.frontier-mine.v1','mine.png'),('existing-settlement-kit','kit.png')]
inventory=[]
for identity,filename in bindings:
    p=source/filename
    meta=p.with_suffix(p.suffix+'.meta')
    inventory.append({'identity':identity,'resource':'ConstructionCardArt/'+p.stem,'path':p.relative_to(ROOT).as_posix(),
        'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest(),
        'guid':re.search(r'^guid: (.+)$',meta.read_text(),re.M).group(1),
        'metaSha256':hashlib.sha256(meta.read_bytes()).hexdigest(),
        'note':'existing local-review image; preserve original attribution/import, no new licence claim'})
write('bindings.json',json.dumps({'schema':1,'baseline':'af0273c2ab89ff199e8e7dfb57d8a69dfbb896c8','existing2DBindings':inventory,
    'newStatusGlyphs':'icon-strokes.json','runtimeEdit':'none','meshOrTextureGeneration':'none'},indent=2)+'\n')

recovered=[]
for filename,old in [
    ('owner-fleet.png',Path('C:/HobbyDS9/artifacts/coordination/GUI-REFRESH-01/references/supplementary/owner-fleet.png')),
    ('production-reference-comparison.png',Path('C:/HobbyDS9/artifacts/coordination/PROD-VIS-01/production-reference-comparison.png'))]:
    destination=OUT/'recovered'/filename
    shutil.copyfile(old,destination)
    assert destination.read_bytes()==old.read_bytes()
    recovered.append({'path':'recovered/'+filename,'source':old.as_posix(),'sha256':hashlib.sha256(old.read_bytes()).hexdigest(),
        'status':'recovered owner-directed proposed/reference layout; not native acceptance'})

def image(name):
    ext='jpg' if name=='galaxy' else 'png'
    return f'<img class="ship-image" src="../../client/HobbyDS9.Unity/Assets/HobbyDS9/Review/Resources/ConstructionCardArt/{name}.{ext}" alt="{name} hull reference">'

def fleet_row(name,hull,art,shield,mission,selected=False):
    return f'''<div class="ship-row {'selected' if selected else ''}"><span class="pick" aria-label="Select ship"></span>{image(art)}
      <div class="identity"><strong>{name}</strong><span>{hull}</span></div>
      <div class="status unknown">{svg('supply')}<span>Not reported</span></div>
      <div class="status">{svg('shield')}<span>{shield}</span></div>
      <div class="status mission">{svg('mission')}<span>{mission}</span></div><button class="inspect">Inspect</button></div>'''

fleet=f'''<div class="page-title"><div><h1>Fleet roster</h1><p>Own fleets <b>2</b> <span class="sep">/</span> Ships <b>5</b></p></div><span class="scope">FILTERED OWN SHIPS · EXAMPLE</span></div>
  <div class="filters"><div class="field wide">Search fleet, ship or accepted mission</div><div class="field">Location: All own</div><div class="field">Hull: All states</div><button>Clear</button></div>
  <div class="list-head"><span>SHIP / CLASS</span><span>SUPPLY</span><span>SHIELDS</span><span>ACCEPTED MISSION</span></div>
  <div class="fleet-group"><b>▾ Expedition fleet</b><span>Sol · 3 ships</span><span class="group-end">Select fleet</span></div>
  {fleet_row('U.S.S. Enterprise','Galaxy Cruiser R1','galaxy','Fore 72 / 100','Hold position',True)}
  {fleet_row('U.S.S. Pathfinder','Constitution Explorer R1','constitution','Full · 4 sectors','Survey assigned')}
  {fleet_row('U.S.S. Valiant','Defiant Combat R1','defiant','Not reported','Travel · destination')}
  <div class="fleet-group"><b>▾ Border patrol</b><span>Vulcan · 2 ships</span><span class="group-end">Select fleet</span></div>
  {fleet_row('U.S.S. Resolute','Defiant Combat R1','defiant','Full · 4 sectors','Patrol assigned')}
  {fleet_row('U.S.S. Endeavour','Constitution Explorer R1','constitution','Fore 60 / 100','Hold position')}
  <div class="lower"><div class="detail">{image('galaxy')}<div><div class="eyebrow">SELECTED SHIP</div><h2>U.S.S. Enterprise</h2><p>Galaxy Cruiser R1 · Expedition fleet · Sol</p><p class="minor">Shield summary expands to Fore / Port / Starboard / Aft. General supply remains “Not reported”.</p></div></div>
  <div class="organize"><div class="eyebrow">FLEET ORGANIZATION</div><p>1 selected ship</p><button>Review transfer</button><p class="minor">Selection alone issues no order.</p></div></div>'''

def product(name,art,cost,exploration='Not reported',combat='Not reported',material='Dilithium 1',owned='0 / 0',ship=True):
    capabilities=f'<span>{svg("survey")} Exploration: {exploration}</span><span>Combat: {combat}</span>' if ship else ''
    return f'''<div class="product-row">{image(art)}<div class="product-main"><strong>{name}</strong><div class="facts"><b>Industry {cost}</b>{capabilities}</div><div class="minor">{material} <span class="sep">/</span> Owned / queued {owned}</div></div><button class="inspect">Inspect</button><button class="add" aria-label="Add {html.escape(name)}">+</button></div>'''

production=f'''<div class="page-title"><div><h1>Construction command</h1><p><b>Earth</b> · Established <span class="sep">/</span> Industry per turn <b>18</b> <span class="sep">/</span> Local Dilithium <b>4</b></p></div><button>World context ▾</button></div>
  <div class="production-grid"><div class="catalogue"><div class="filters"><div class="field wide">Search construction</div><div class="field">Category: All</div></div>
  <div class="section-label">SHIPS <span>EXACT CLASS / REVISION · COMPARABLE FIELDS</span></div>
  {product('Constitution Explorer R1','constitution','108',owned='1 / 1')}
  {product('Defiant Combat R1','defiant','107',owned='0 / 2')}
  {product('Galaxy Colony Transport R1','galaxy','141',material='Dilithium 2')}
  {product('Galaxy Cruiser R1','galaxy','175',material='Dilithium 2',owned='1 / 0')}
  <div class="section-label">PLANETARY BUILDINGS</div>
  {product('Fabrication annex','fusion','48',material='Actual recipe materials',owned='1 / 0',ship=False)}
  {product('Research laboratory','research','56',material='Actual recipe materials',owned='1 / 0',ship=False)}
  <div class="collapsed">▸ Special projects <span>Existing eligible choices only</span></div><div class="collapsed">▸ System improvements <span>Existing eligible choices only</span></div></div>
  <aside class="queue"><div class="eyebrow">EARTH · BUILD QUEUE</div><h2>3 orders</h2><div class="queue-item"><span class="chip">1</span><b>Defiant Combat R1</b><p>Industry 40 / 107</p><div class="bar"><i></i></div><p class="minor">Materials: actual streamed progress</p><button>Cancel order</button></div><div class="queue-item"><span class="chip">2</span><b>Constitution Explorer R1</b><p class="minor">Waiting · Industry 108</p></div><div class="queue-item"><span class="chip">3</span><b>Defiant Combat R1</b><p class="minor">Waiting · Industry 107</p></div><div class="queue-note">Separate scrolling for catalogue and queue. Inspect selects; + submits one validated enqueue.</div></aside></div>'''

css='''*{box-sizing:border-box}html,body{margin:0;background:#070b10;color:#eaf4ff;font:18px Arial,sans-serif}body{min-width:1080px}button,.field{height:44px;border:1px solid #45556a;background:#121e2c;color:#eaf4ff;border-radius:7px;padding:0 14px;font:16px Arial}button{white-space:nowrap}h1{font-size:30px;margin:0 0 8px}h2{font-size:21px;margin:5px 0 8px}p{margin:5px 0;line-height:1.35}.frame{border-left:12px solid #ffb360;border-radius:42px 0 0 42px;min-height:900px;padding:0 22px 0 20px}.top{height:60px;display:flex;align-items:center;justify-content:space-between;border-bottom:3px solid #ffb360}.brand,.eyebrow,.section-label{color:#b7a6e0;font-size:16px;font-weight:bold;letter-spacing:.5px}.brand{color:#ffb360}.tabs a{font-size:16px;color:#9bd9ff;text-decoration:none;padding:8px 12px}.tabs a.active{border-bottom:3px solid #ffb360;color:#ffb360}.body{display:grid;grid-template-columns:140px minmax(0,1fr);gap:20px;padding-top:18px}.nav{display:flex;flex-direction:column;gap:7px;padding-top:10px}.nav span{padding:12px;border:1px solid #334659;border-radius:6px;font-size:17px}.nav span.on{border-left:6px solid #ffb360;padding-left:7px;background:#242937}.nav .spacer{height:165px;border:0}.page-title{height:86px;display:flex;justify-content:space-between;align-items:flex-start}.page-title p{color:#a9c2d5;font-size:16px}.page-title b{color:#9bd9ff}.scope{font-size:13px;border:1px solid #435166;border-radius:12px;padding:6px;color:#b7a6e0}.filters{display:flex;gap:8px;height:55px}.field{display:flex;align-items:center;color:#9caabb;min-width:150px}.field.wide{flex:1}.icon{width:24px;height:24px;flex:none;color:#b7a6e0}.list-head{display:grid;grid-template-columns:minmax(250px,1fr) 142px 166px 240px;font-size:13px;color:#b7a6e0;height:30px;align-items:center;padding-left:77px;padding-right:10px}.fleet-group{height:44px;display:flex;gap:25px;align-items:center;border-top:1px solid #53647a;background:#192331;padding:0 12px;color:#9bd9ff;font-size:16px}.fleet-group b{color:#ffb360}.group-end{margin-left:auto;font-size:14px;color:#b7a6e0}.ship-row{height:58px;display:grid;grid-template-columns:24px 56px minmax(180px,1fr) 142px 166px 160px 80px;gap:9px;align-items:center;padding:0 9px 0 15px;background:#0d1722;border-bottom:1px solid #283c50}.ship-row.selected{background:#273040;box-shadow:inset 3px 0 #ffb360}.pick{height:20px;width:20px;border:1px solid #8ca0b5;border-radius:3px}.ship-row.selected .pick{background:#ffb360;border:5px solid #263040}.ship-image{width:56px;height:40px;object-fit:contain;background:#071019;border-radius:5px;flex:none}.identity{min-width:0}.identity strong,.product-main>strong{font-size:17px;font-weight:600;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;display:block}.identity span{font-size:15px;color:#a3b4c5;display:block;padding-top:4px}.status{display:flex;align-items:center;gap:6px;font-size:15px;line-height:1.2}.unknown{color:#9aa8b6}.inspect{font-size:14px;height:44px;padding:0 10px}.lower{display:grid;grid-template-columns:1fr 260px;gap:16px;margin-top:18px}.detail{min-height:140px;border:1px solid #526279;border-radius:8px;display:flex;align-items:center;gap:18px;padding:15px}.detail .ship-image{width:160px;height:105px}.minor{font-size:14px;color:#a2b2c2;line-height:1.4}.organize{padding:12px;border:1px solid #526279;border-radius:8px}.organize button{width:100%;margin:6px 0}.footer{height:55px;border-top:2px solid #ffb360;margin-top:20px;display:flex;align-items:center;gap:20px;font-size:14px;color:#b7a6e0}.footer strong{color:#ffb360}.production-grid{display:grid;grid-template-columns:minmax(0,1fr) 260px;gap:18px}.catalogue,.queue{min-width:0}.section-label{height:29px;border-bottom:1px solid #49596d;margin:0 0 4px;display:flex;align-items:center;justify-content:space-between}.section-label span{font-size:11px;font-weight:normal;color:#91a4b8}.product-row{height:76px;display:flex;align-items:center;gap:10px;padding:5px 8px;border-bottom:1px solid #263c50;background:#0d1722}.product-main{flex:1;min-width:0}.facts{display:flex;gap:16px;font-size:14px;margin:7px 0 4px;flex-wrap:wrap}.facts b{color:#9bd9ff}.facts span{display:flex;gap:3px;align-items:center}.facts .icon{width:16px;height:16px}.add{width:44px;padding:0;background:#8b663e;color:#fff;font-size:25px;border-color:#d7a161}.sep{color:#53647a;margin:0 5px}.collapsed{height:32px;font-size:15px;color:#b7a6e0;padding:7px;border-bottom:1px solid #314257}.collapsed span{font-size:13px;color:#97a7b8;margin-left:12px}.queue{padding:12px;border:1px solid #4d5f73;border-radius:8px;background:#0c1620}.queue-item{border-top:1px solid #45556a;padding:17px 0}.queue-item>b{font-size:16px}.queue-item p{font-size:15px;margin:12px 0}.queue-item button{font-size:15px;width:100%;margin-top:8px}.chip{color:#ffb360;border:1px solid #6c6b68;padding:2px 6px;margin-right:5px;border-radius:5px;font-size:15px}.bar{height:9px;background:#263d50;border-radius:4px;overflow:hidden}.bar i{display:block;width:37%;height:100%;background:#9bd9ff}.queue-note{font-size:14px;color:#a5b3c4;line-height:1.5;border-top:1px solid #45556a;padding-top:14px}.screen{display:none}.screen.on{display:block}@media(min-width:1700px){.body{grid-template-columns:170px minmax(0,1fr);gap:26px}.production-grid{grid-template-columns:minmax(0,1fr) 310px}.ship-row{grid-template-columns:24px 64px minmax(260px,1fr) 160px 210px 230px 100px}.list-head{grid-template-columns:minmax(350px,1fr) 160px 210px 340px}.facts{font-size:16px}.identity strong,.product-main>strong{font-size:19px}.product-row{height:76px}.product-row .ship-image{width:72px}}'''

write('compact-reference.html',f'''<!doctype html><html lang="en"><meta charset="utf-8"><title>HobbyDS9 compact LCARS reference — proposed</title><meta name="viewport" content="width=device-width, initial-scale=1"><style>{css}</style>
<body><div class="frame"><header class="top"><div class="brand">HOBBYDS9 / FEDERATION COMMAND</div><div class="tabs"><a id="fleet-tab" href="?view=fleet">Fleet reference</a><a id="production-tab" href="?view=production">Production reference</a></div></header><div class="body"><nav class="nav"><span>Map</span><span>Worlds</span><span id="nav-production">Production</span><span id="nav-fleet">Fleets</span><span>Research</span><span>Designer</span><span class="spacer"></span><span>Settings</span><span>Help</span></nav><main><section class="screen" id="fleet">{fleet}</section><section class="screen" id="production">{production}</section></main></div><footer class="footer"><strong>PROPOSED REFERENCE</strong><span>Illustrative names / values · Not a campaign snapshot or an implemented screen · Labels accompany icons · No new capability ratings</span></footer></div>
<script>const view=new URLSearchParams(location.search).get('view')==='production'?'production':'fleet';document.getElementById(view).classList.add('on');document.getElementById(view+'-tab').classList.add('active');document.getElementById('nav-'+view).classList.add('on');</script></body></html>''')

write('recovered/provenance.json',json.dumps({'schema':1,'references':recovered},indent=2)+'\n')
print('Wrote proposed compact references, five original vector glyphs, eight exact existing image bindings; no runtime changes.')
