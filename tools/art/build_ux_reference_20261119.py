"""Original compact UI reference, reusing source pixels/assets; no live Unity edits."""
import hashlib
import html
import json
import re
import shutil
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'art/ux-20261119'
OUT.mkdir(parents=True,exist_ok=True)
(OUT/'recovered').mkdir(exist_ok=True)
(OUT/'icons').mkdir(exist_ok=True)
BASE='fec332ee8af4e4c28054239c73b347dd0778fa4c'
def write(name,text):
    if name=='compact-reference.html':
        text=text.replace('Keep the current received-catalogue and music/audio credit records, including owner-provided local-review limitations.','Ship artwork: received project asset catalogue. Owner-provided Defiant artwork is for local review. Music and sound credits are available in Audio.')
        extra_css='.actions button.add:disabled{background:#182332!important;color:#8895a5!important;border-color:#344050!important}.filters button,.search{height:44px}.columns{font-size:13px}.build-row>strong{font-size:16px}'
        text=text.replace('</style>',extra_css+'</style>')
        measure='''window.addEventListener('load',()=>{const screen=document.querySelector('.screen.on');const limit=Math.min(screen.getBoundingClientRect().bottom,document.querySelector('.footer').getBoundingClientRect().top);const rows=[...screen.querySelectorAll('.build-row')];const visible=rows.filter(r=>r.getBoundingClientRect().top>=screen.getBoundingClientRect().top&&r.getBoundingClientRect().bottom<=limit);const actions=[...screen.querySelectorAll('.actions button')];const data={view:v,width:innerWidth,height:innerHeight,catalogueEntries:rows.length,fullyVisibleCatalogueEntries:visible.length,visibleShipRows:visible.filter(r=>r.classList.contains('ship-row')).length,visibleInstallationRows:visible.filter(r=>r.classList.contains('install-row')).length,rowHeights:[...new Set(rows.map(r=>r.getBoundingClientRect().height))],minimumActionWidth:actions.length?Math.min(...actions.map(r=>r.getBoundingClientRect().width)):null,minimumActionHeight:actions.length?Math.min(...actions.map(r=>r.getBoundingClientRect().height)):null,horizontalDocumentOverflow:document.documentElement.scrollWidth>innerWidth};const output=document.createElement('script');output.id='reference-metrics';output.type='application/json';output.textContent=JSON.stringify(data);document.body.appendChild(output);});'''
        text=text.replace('</script>',measure+'</script>')
    (OUT/name).write_text(text,encoding='utf-8',newline='\n')
def digest(p): return hashlib.sha256(p.read_bytes()).hexdigest()

shots=[('portrait','2cfe4add-9781-4b70-8ac0-1ab88bc649e5'),('world','fdda60df-3a58-4a3e-be90-2a109520d576'),
       ('belt','96be212c-0c0e-45ef-854e-ca2cb855d92b'),('production','d8a7a9b5-1b8c-4437-88e7-6a313e148208')]
provenance=[]
for name,identity in shots:
    source=Path('C:/Users/ambal/AppData/Local/Temp')/('codex-clipboard-'+identity+'.png')
    destination=OUT/'recovered'/(name+'-owner.png')
    shutil.copyfile(source,destination)
    assert digest(source)==digest(destination)
    provenance.append({'path':'recovered/'+destination.name,'source':source.as_posix(),'sha256':digest(source),
                       'status':'unchanged owner screenshot of baseline; rejected layout is not approval'})
write('recovered/provenance.json',json.dumps({'schema':1,'date':'2026-11-19','baseline':BASE,'screenshots':provenance},indent=2)+'\n')

prior=json.loads((ROOT/'art/playtest-20261118/icon-strokes.json').read_text())['icons']
strokes={k:v['polylines'] for k,v in prior.items()}
new={
 'population':[[11,4,17,4,17,10,11,10,11,4],[8,24,8,17,11,13,17,13,20,17,20,24],[3,22,3,16,6,13],[25,22,25,16,22,13]],
 'food':[[5,25,5,14,10,5,20,5,25,14,25,25,5,25],[5,14,25,14],[10,19,20,19]],
 'queue':[[5,7,8,7],[12,7,26,7],[5,15,8,15],[12,15,26,15],[5,23,8,23],[12,23,26,23]],
 'asteroid-body':[[3,9,8,4,14,6,15,12,10,16,4,14,3,9],[18,7,24,5,28,10,26,16,20,15,18,7],[10,21,15,17,21,20,21,26,15,28,10,25,10,21]],
}
strokes.update(new)
# Existing ReviewIcon glyph strokes, reused verbatim for labelled facts.
strokes['industry']=[[4,18,14,18,14,27,4,27,4,18],[16,18,26,18,26,27,16,27,16,18],[10,7,20,7,20,16,10,16,10,7]]
strokes['research']=[[8,8,22,8,22,22,8,22,8,8]]+[[i,3,i,8] for i in (10,15,20)]+[[i,22,i,27] for i in (10,15,20)]+[[3,i,8,i] for i in (10,15,20)]+[[22,i,27,i] for i in (10,15,20)]
def icon(name):
    lines=''.join('<polyline points="'+' '.join(f'{line[i]},{line[i+1]}' for i in range(0,len(line),2))+'"/>' for line in strokes[name])
    return '<svg viewBox="0 0 30 30" aria-hidden="true" fill="none" stroke="currentColor" stroke-width="2" stroke-linejoin="round" stroke-linecap="round">'+lines+'</svg>'
for name in new:
    write('icons/'+name+'.svg',icon(name).replace('<svg ','<svg xmlns="http://www.w3.org/2000/svg" width="30" height="30" ',1)+'\n')
write('world-icon-strokes.json',json.dumps({'schema':1,'version':'world-icons-v1-20261119','canvas':[30,30],'strokeWidth':2,
    'fill':'none','status':'original receiving strokes, no runtime imports','newOriginalGlyphs':new,
    'reusedGlyphSources':['art/playtest-20261118/icon-strokes.json','Review/Runtime/ReviewIcon.cs'],
    'receiving':'Every list is a separately stroked x,y polyline; existing Painter2D Line pattern. Labels/tooltips required, picking ignored. No SVG package.'},indent=2)+'\n')

root_asset=ROOT/'client/HobbyDS9.Unity/Assets/HobbyDS9/Review/Resources'
install=[('infrastructure.fabrication-annex.v1','fusion.png'),('infrastructure.research-laboratory.v1','research.png'),
         ('infrastructure.hydroponic-extension.v1','hydroponics.png'),('existing-settlement-kit','kit.png')]
asset_rows=[]
for identity,name in install:
    p=root_asset/'ConstructionCardArt'/name
    asset_rows.append({'identity':identity,'resource':'ConstructionCardArt/'+p.stem,'path':p.relative_to(ROOT).as_posix(),
        'sha256':digest(p),'guid':re.search(r'^guid: (.+)$',p.with_suffix(p.suffix+'.meta').read_text(),re.M).group(1)})
write('asset-bindings.json',json.dumps({'schema':1,'baseline':BASE,'existingInstallationImages':asset_rows,
    'shipRows':'Reuse original art/playtest-20261118 ship stroke as a generic vessel icon. Do not restore exploded proof thumbnails.',
    'selectedExactHullInspector':'Existing ActivityHullCatalog.Find(exact HullVersionId) -> ShipPreviewStage.ShowActivityHull; reuse current special Galaxy resolver.',
    'centralCreditsSource':(root_asset/'SystemBodyArt/ATTRIBUTION.txt').relative_to(ROOT).as_posix(),
    'centralCreditsSourceSha256':digest(root_asset/'SystemBodyArt/ATTRIBUTION.txt'),
    'newPlanetOrBeltTexturesOrMeshes':0,'newRuntimeImports':0},indent=2)+'\n')

credits='''Solar System surface maps

Sun, Mercury, Venus Atmosphere, Earth Day Map, Mars, Jupiter, Saturn, Uranus and Neptune.
Solar System Scope / INOVE — CC BY 4.0.
Source: https://www.solarsystemscope.com/textures/
Licence: https://creativecommons.org/licenses/by/4.0/

JPEGs copied unchanged from the art handoff with shortened filenames. Unity imports the original 2K maps at up to 1024 pixels in sRGB. The spheres, lighting, orbits and Saturn rings are schematic and not to scale. Other generated terrain and original procedural belt geometry retain their existing project credits.

'''
write('central-credits.txt',credits.rstrip()+'\n')

def fact(label,value,glyph=None,unit='',positive=False):
    return f'<div class="fact"><label>{icon(glyph) if glyph else ""}{label}</label><strong class="{"positive" if positive else ""}">{value}<small>{unit}</small></strong></div>'

world=f'''<div class="page-heading"><div><h1>Earth</h1><span class="muted">Sol / World details</span></div><button>Back to system</button></div>
<div class="world-context"><div class="portrait"><div class="portrait-crop"><img src="recovered/portrait-owner.png" alt="Existing lit Earth portrait, reused owner pixels"></div><div class="portrait-cue">Drag to rotate</div><div class="portrait-actions"><button>Pause rotation</button><button>Reset view</button></div></div>
<div class="world-facts"><div class="world-identity"><div class="chip-row"><span class="chip">Temperate</span><span class="chip established">Established</span><span class="chip">Own colony</span></div><div class="scopes"><span>Owned economy record</span><span>Control · Federation · observed W3</span></div></div>
<div class="stat-grid economy">{fact('Population','1,013','population')}{fact('Industry / week','8','industry')}{fact('Research / week','50','research')}</div>
<div class="food-heading">{icon('food')} FOOD</div><div class="stat-grid food">{fact('Produced / week','14')}{fact('Consumed / week','11')}{fact('Net / week','+3',positive=True)}{fact('Stored','26')}</div>
<button class="queue-strip">{icon('queue')}<b>Build queue</b><span>View orders ▸</span></button>
<div class="disclosures"><button>Environment<span>Known ▸</span></button><button>Resources<span>Initial record ▸</span></button><button>Survey<span>No completed record ▸</span></button></div>
<div class="world-actions"><button class="primary">Manage Earth</button></div></div></div>
<div class="scope-example"><span class="chip retained">Retained observation · W2</span><span>Population <b>—</b></span><span>Industry <b>—</b></span><span>Research <b>—</b></span><span>Settlement <b>Not reported</b></span></div>
<div class="reference-note">Scope example: a retained control/environment record does not supply a current economy, remaining deposits or settlement status. Numeric cells require reported values; unknown is not zero.</div>'''

def cell(value,cls=''):
    return f'<span class="{cls}">{value}</span>'
def ship(name,cost,study,contact,hull,shields,material,owned,queued):
    return '<div class="build-row ship-row">'+icon('ship')+f'<strong>{name}</strong>'+''.join(cell(v,'number') for v in [cost,study,contact,hull,shields])+cell(material)+cell(f'{owned} / {queued}','number')+'<div class="actions"><button title="Inspect exact published class">⋯</button><button class="add" title="Add one validated build order">+</button></div></div>'
def installation(name,image,cost,effect,power,material,count,queued,disabled=False):
    return '<div class="build-row install-row"><img class="asset" src="../../client/HobbyDS9.Unity/Assets/HobbyDS9/Review/Resources/ConstructionCardArt/'+image+'" alt="">'+f'<strong>{name}</strong>'+cell(cost,'number')+cell(effect,'effect')+cell(power,'number')+cell(material)+cell(f'{count} / {queued}','number')+'<div class="actions"><button title="Inspect effects and conditions">⋯</button><button class="add" '+('disabled title="Already built or queued"' if disabled else 'title="Add one validated build order"')+'>+</button></div></div>'

production=f'''<div class="page-heading"><div><h1>Construction</h1><span class="muted"><b>Earth</b> · Industry 8 / week · Local Dilithium 4</span></div><button>World context ▾</button></div>
<div class="construction"><div class="catalogue"><div class="filters"><div class="search">Search construction</div><button>Category: All ▾</button></div>
<div class="section-title">SHIPS <span>Exact published class / revision</span></div>
<div class="columns ship-row"><span></span><b>Class</b><b>Industry</b><b>Study</b><b>Contact</b><b>Hull</b><b>Shields</b><b>Materials</b><b>Own / Q</b><b>Inspect / +</b></div>
{ship('Constitution Explorer R1',108,3,1,30,0,'Dil 1',1,0)}
{ship('Defiant Combat R1',107,1,0,24,0,'Dil 1',0,1)}
{ship('Galaxy Colony Transport R1',141,0,0,60,0,'Dil 3',0,0)}
{ship('Galaxy Cruiser R1',175,4,3,60,0,'Dil 1',0,0)}
<div class="section-title">INSTALLATIONS <span>Base effects; conditions in Inspect</span></div>
<div class="columns install-row"><span></span><b>Installation</b><b>Industry</b><b>Base effect / week</b><b>Power demand</b><b>Materials</b><b>Built / Q</b><b>Inspect / +</b></div>
{installation('Fabrication annex','fusion.png',80,'Industry +6',2,'Dil 2',0,0)}
{installation('Research laboratory','research.png',60,'Research +15',2,'Dil 1',1,0,True)}
{installation('Hydroponic extension','hydroponics.png',40,'Food +6',1,'Dil 1',0,0)}
<div class="section-title">SPECIAL PROJECTS</div>
{installation('Settlement kit','kit.png',60,'Equipment kit','—','Dil 6',0,0)}
<div class="empty-category">SYSTEM IMPROVEMENTS <span>No eligible choices in this example</span></div></div>
<aside class="queue"><div class="eyebrow">EARTH / QUEUE</div><h2>1 order</h2><div class="queue-order"><div class="queue-name">1 · Defiant Combat R1</div><div class="progress-label">Industry <b>0 / 107</b></div><div class="progress"><i></i></div><span class="muted">Materials · not consumed</span><button>Cancel order</button></div><div class="inspect-slot"><div class="eyebrow">SELECTED INSPECTION</div><h3>Class / installation</h3><div class="muted">Armor · speed · movement<br>Weapons / powered packages<br>Effects / power conditions</div><button>Open full details</button></div></aside></div>'''

credits_html='''<div class="page-heading"><div><h1>Help & credits</h1><span class="muted">Central source attribution</span></div></div><article class="credit-panel"><div class="eyebrow">VISUAL & AUDIO CREDITS</div><h2>Solar System surface maps</h2><p>Sun, Mercury, Venus Atmosphere, Earth Day Map, Mars, Jupiter, Saturn, Uranus and Neptune.</p><p><b>Solar System Scope / INOVE</b> · CC BY 4.0</p><p><a href="https://www.solarsystemscope.com/textures/">Texture source</a> <span> / </span> <a href="https://creativecommons.org/licenses/by/4.0/">CC BY 4.0 licence</a></p><p class="muted">Source JPEGs unchanged; shortened filenames and Unity import downscaling. Rendered on schematic spheres with illustrative lighting, orbits and rings.</p><div class="credit-divider"></div><h2>Ship assets & audio</h2><p class="muted">Keep the current received-catalogue and music/audio credit records, including owner-provided local-review limitations.</p><button>Open Audio</button></article>'''

css='''*{box-sizing:border-box}html,body{margin:0;background:#080d15;color:#eeeaf2;font:16px Arial,sans-serif}body{min-width:1180px}button{height:44px;min-width:44px;padding:0 12px;background:#182739;color:#eeeaf2;border:1px solid #65728b;border-radius:5px;font:15px Arial;white-space:nowrap}button:disabled{background:#141c29;color:#8895a5;border-color:#344050}.frame{height:100vh;min-height:700px;border-left:10px solid #efb16b;border-radius:32px 0 0 32px;padding:0 18px;display:flex;flex-direction:column}.top{height:48px;flex:none;display:flex;align-items:center;justify-content:space-between;border-bottom:2px solid #efb16b}.brand{color:#efb16b;font-size:16px;font-weight:bold;letter-spacing:1px}.tabs{display:flex;gap:22px}.tabs a,a{color:#91c9ec;text-decoration:none}.tabs a.active{color:#efb16b}.body{flex:1;min-height:0;display:grid;grid-template-columns:120px minmax(0,1fr);gap:16px;padding-top:12px}.nav{display:flex;flex-direction:column;gap:6px;padding-top:3px}.nav span,.nav a{padding:12px 9px;border:1px solid #34465c;border-radius:5px;font-size:16px;min-height:44px;color:#eeeaf2}.nav .on{background:#282a3a;border-left:5px solid #efb16b;padding-left:5px}.nav .spacer{flex:1;border:0}.nav .help{color:#91c9ec}main{min-width:0;min-height:0}.page-heading{height:58px;display:flex;align-items:flex-start;justify-content:space-between}.page-heading h1{font-size:25px;margin:0 0 5px;line-height:1.1}h2{font-size:21px;margin:8px 0 15px}h3{font-size:17px;margin:10px 0}.muted{font-size:14px;color:#a8b8ca}.page-heading .muted b{color:#91c9ec}.footer{height:36px;flex:none;display:flex;align-items:center;gap:16px;font-size:12px;color:#a9b8cf;border-top:2px solid #efb16b;margin-top:10px}.footer b{color:#efb16b}.screen{display:none;height:100%;overflow-y:auto}.screen.on{display:block}.world-context{display:grid;grid-template-columns:256px minmax(0,760px);gap:20px;align-items:start}.portrait{text-align:center;padding-top:8px}.portrait-crop{width:256px;height:256px;position:relative;overflow:hidden;margin:0 auto;border-radius:6px;background:#070b10}.portrait-crop img{position:absolute;width:281.6px;max-width:none;left:-11.2px;top:-6.4px}.portrait-cue{font-size:14px;color:#b9ace6;padding:10px}.portrait-actions{display:flex;gap:8px;justify-content:center}.portrait-actions button{font-size:14px;padding:0 9px}.world-facts{background:#101b29;border:1px solid #34465c;border-left:3px solid #b9ace6;border-radius:6px;padding:12px;min-width:0}.chip-row{display:flex;flex-wrap:wrap;gap:6px}.chip{font-size:14px;line-height:18px;padding:4px 8px;background:#1c2b3c;color:#c9bfeb;border:1px solid #4f5475;border-radius:12px}.chip.established{color:#91c9ad}.scopes{display:flex;flex-wrap:wrap;justify-content:space-between;gap:5px;color:#acbad0;font-size:12px;margin:10px 0 9px}.stat-grid{display:grid;gap:6px}.economy{grid-template-columns:repeat(3,minmax(0,1fr))}.fact{background:#142336;border-left:2px solid #b9ace6;padding:7px 9px;min-width:0}.fact label{display:flex;align-items:center;gap:6px;font-size:14px;color:#c9c3df;white-space:nowrap}.fact strong{display:block;font-size:22px;font-weight:600;color:#eef0ff;margin-top:4px}.fact strong small{font-size:14px;color:#a9b9ce;margin-left:6px}.positive{color:#91c9ad!important}svg{width:24px;height:24px;flex:none;color:#b9ace6}.food-heading{display:flex;gap:6px;align-items:center;color:#b9ace6;font-size:12px;margin:10px 0 5px;line-height:16px}.food-heading svg{height:18px;width:18px}.food{grid-template-columns:repeat(4,minmax(0,1fr))}.food .fact{padding-top:6px;padding-bottom:6px}.food .fact label{font-size:13px}.food .fact strong{font-size:21px}.queue-strip{display:flex;align-items:center;gap:10px;text-align:left;width:100%;margin-top:10px}.queue-strip span{margin-left:auto;color:#91c9ec}.disclosures{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:6px;margin-top:8px}.disclosures button{font-size:14px;text-align:left;padding:4px 8px;line-height:18px}.disclosures span{display:block;font-size:12px;color:#acbad0}.world-actions{display:flex;justify-content:flex-end;margin-top:8px}.primary,.add{background:#efb16b!important;color:#15131a!important;border-color:#efb16b!important}.scope-example{display:flex;flex-wrap:wrap;align-items:center;gap:20px;margin:20px 0 9px;padding:12px;border:1px dashed #4f5475;border-radius:6px;color:#a9b9ce;font-size:14px;max-width:1036px}.retained{color:#efb16b}.reference-note{font-size:13px;color:#a5b2c4;max-width:1000px;line-height:1.5}.construction{display:grid;grid-template-columns:minmax(0,1fr) 210px;gap:12px;min-height:0}.catalogue{min-width:0;overflow-x:hidden}.filters{display:flex;gap:8px;height:48px;align-items:flex-start}.search{height:40px;display:flex;align-items:center;border:1px solid #65728b;border-radius:5px;background:#101b29;color:#a1b0c4;padding:0 12px;flex:1}.filters button{height:40px}.section-title{height:25px;display:flex;align-items:center;justify-content:space-between;font-size:14px;color:#b9ace6;font-weight:bold;letter-spacing:.4px;border-bottom:1px solid #57627b}.section-title span{font-size:11px;font-weight:normal;color:#a3b4ca;letter-spacing:0}.columns,.build-row{display:grid;align-items:center;gap:5px}.ship-row{grid-template-columns:24px minmax(145px,1fr) 57px 42px 48px 42px 48px 60px 57px 92px}.install-row{grid-template-columns:24px minmax(145px,1fr) 57px minmax(110px,1fr) 72px 60px 57px 92px}.columns{height:28px;font-size:11px;color:#b9ace6}.columns b{font-weight:normal}.columns b:nth-child(n+3){text-align:center}.build-row{height:48px;border-bottom:1px solid #304258;background:#101b29;padding-right:3px}.build-row:nth-of-type(even){background:#0d1724}.build-row>strong{font-size:15px;font-weight:600;line-height:17px;max-height:36px;overflow:hidden;padding-right:3px}.build-row>span{font-size:14px;text-align:center;white-space:nowrap}.number{color:#91c9ec;font-variant-numeric:tabular-nums}.build-row>.effect{font-size:13px;text-align:left}.build-row>svg,.asset{width:24px;height:24px;object-fit:contain}.actions{display:flex;gap:4px;justify-content:flex-end}.actions button{width:44px;height:44px;padding:0;font-size:21px}.empty-category{height:25px;display:flex;align-items:center;justify-content:space-between;color:#b9ace6;font-size:12px;border-bottom:1px solid #304258}.empty-category span{color:#9caec3;font-size:11px}.queue{background:#101b29;border:1px solid #40516a;border-left:2px solid #b9ace6;border-radius:5px;padding:11px}.eyebrow{font-size:12px;color:#b9ace6;letter-spacing:.6px;font-weight:bold}.queue h2{margin:9px 0 14px}.queue-name{font-size:16px;font-weight:bold}.progress-label{font-size:14px;margin:13px 0 8px}.progress-label b{float:right;color:#91c9ec}.progress{height:6px;background:#2d3e52;border-radius:3px;margin-bottom:9px}.progress i{width:0;height:100%;display:block;background:#91c9ec}.queue-order>.muted{display:block;font-size:12px;line-height:1.5}.queue button{width:100%;margin-top:14px;font-size:14px}.inspect-slot{margin-top:20px;border-top:1px solid #44516b;padding-top:15px}.inspect-slot>.muted{font-size:12px;line-height:1.6}.credit-panel{max-width:780px;background:#101b29;border-left:3px solid #b9ace6;padding:20px;border-radius:6px}.credit-panel p{font-size:16px;line-height:1.6}.credit-panel .muted{font-size:14px}.credit-divider{border-top:1px solid #44516b;margin:22px 0}.credit-panel button{margin-top:12px}@media(min-width:1600px){.body{grid-template-columns:160px minmax(0,1fr);gap:22px}.construction{grid-template-columns:minmax(0,1fr) 250px;gap:18px}.ship-row{grid-template-columns:30px minmax(260px,1fr) 82px 74px 74px 74px 74px 100px 90px 98px}.install-row{grid-template-columns:30px minmax(240px,1fr) 82px minmax(180px,1fr) 130px 100px 90px 98px}.build-row>strong{font-size:17px}.build-row>span{font-size:16px}.columns{font-size:14px}.build-row>.effect{font-size:16px}.world-context{grid-template-columns:300px minmax(0,780px);gap:30px}.portrait{padding-top:15px}.portrait-crop{transform:scale(1.12);transform-origin:top center;margin-bottom:30px}.world-facts{padding:15px}.fact label{font-size:16px}.fact strong{font-size:24px}.food .fact label{font-size:14px}.food .fact strong{font-size:23px}.scopes{font-size:14px}.queue-strip,.disclosures button{font-size:16px}.disclosures span{font-size:14px}.scope-example{max-width:1110px}.section-title{font-size:16px}.section-title span{font-size:13px}}'''

write('compact-reference.html',f'''<!doctype html><html lang="en"><meta charset="utf-8"><title>HobbyDS9 Nov19 compact reference</title><meta name="viewport" content="width=device-width, initial-scale=1"><style>{css}</style><body><div class="frame"><header class="top"><div class="brand">HOBBYDS9 / FEDERATION COMMAND</div><div class="tabs"><a id="world-tab" href="?view=world">World pane</a><a id="production-tab" href="?view=production">Dense construction</a><a id="credits-tab" href="?view=credits">Central credits</a></div></header><div class="body"><nav class="nav"><span>Map</span><span id="world-nav">Worlds</span><span id="production-nav">Production</span><span>Fleets</span><span>Research</span><span>Designer</span><span class="spacer"></span><span>Settings</span><a class="help" id="credits-nav" href="?view=credits">Help & credits</a></nav><main><section class="screen" id="world">{world}</section><section class="screen" id="production">{production}</section><section class="screen" id="credits">{credits_html}</section></main></div><footer class="footer"><b>PROPOSED REFERENCE</b><span>Owner sample / illustrative values · Not live UI or a native screenshot · Existing lighting / assets retained</span></footer></div><script>const q=new URLSearchParams(location.search).get('view');const v=['world','production','credits'].includes(q)?q:'world';document.getElementById(v).classList.add('on');document.getElementById(v+'-tab').classList.add('active');document.getElementById(v+'-nav').classList.add('on');</script></body></html>''')
print('Wrote Nov19 compact world, actual-route dense ship/installation and central-credits references; no runtime/planet/belt assets changed.')
