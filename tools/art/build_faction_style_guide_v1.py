"""Original faction design/reference packet. Read-only source audit; no runtime/media activation."""
import hashlib
import html
import json
import re
import subprocess
import sys
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'art/faction-style-v1'
OUT.mkdir(parents=True,exist_ok=True)
PIN='2fc0dbca46073121379dd9491c859a02a6f22dcd'
HISTORY='b3496b6b1da31634d9beb8a7357b2480f37a9a72'
GIT='C:/Users/ambal/AppData/Local/GitHubDesktop/app-3.6.1/resources/app/git/cmd/git.exe'
def git(*args): return subprocess.check_output([GIT,'-C',str(ROOT),*args])
def blob(path,pin=PIN): return git('show',pin+':'+path)
def write(name,value):
    p=OUT/name;p.parent.mkdir(parents=True,exist_ok=True)
    p.write_text(value if isinstance(value,str) else json.dumps(value,indent=2)+'\n',encoding='utf-8',newline='\n')

profiles=[
 {'id':'federation','displayName':'Federation','family':'federation','catalogueStatus':'runtime-civilization/current-generated-player',
  'civilizationDefinitionIds':['civilization.federation'],'minorDefinitionIds':[],
  'colors':{'background':'#080D15','surface':'#101B29','raised':'#182739','brandPrimary':'#EFB16B','brandSecondary':'#B9ACE6'},
  'ship':'Separated primary/secondary volumes and clear nacelle spacing; compact escort exceptions remain valid. Pearl/slate hull, restrained bronze systems, red/blue localized luminous roles. Preserve exact received class geometry and livery.',
  'audio':'Soft rounded electronic press; short resolving accepted pair; restrained orchestral/horn/string exploration bed. Existing Federation prototype cue set is evidence, not a cleared universal sound library.',
  'livery':'Restrained registry/stripe zones and dark recesses; never flatten every material into amber.',
  'sourceStatus':'Federation LCARS family is source-backed; token refinements and this sample remain proposed.'},
 {'id':'klingon','displayName':'Klingon','family':'klingon','catalogueStatus':'documented-proposed-console/no-civilization-definition',
  'civilizationDefinitionIds':[],'minorDefinitionIds':[],
  'colors':{'background':'#110C0C','surface':'#231719','raised':'#302125','brandPrimary':'#EAA294','brandSecondary':'#D6C291'},
  'ship':'Proposed broad swept planes, compact central mass and forward emphasis; readable negative spaces instead of a generic Federation saucer. Gunmetal/olive panels with controlled warm-red identification accents.',
  'audio':'Proposed dry tactile attack with a short resonant tail; low brass and sparse measured percussion as a background brief. No sampled war cries or copied film theme. Existing named Klingon track is not an auto-switch rule.',
  'livery':'Large restrained panel/mask zones; edge wear subordinate to silhouette and readable inspection lighting.',
  'sourceStatus':'Historical stepped/angular framing proposal recovered; new palette/ships/audio remain proposals.'},
 {'id':'cardassian','displayName':'Cardassian Union','family':'cardassian','catalogueStatus':'runtime-civilization/current-generated-opponent',
  'civilizationDefinitionIds':['civilization.cardassian'],'minorDefinitionIds':[],
  'colors':{'background':'#100D08','surface':'#231E14','raised':'#30291C','brandPrimary':'#D6B16D','brandSecondary':'#B7C7A8'},
  'ship':'Source-backed original cruiser prototype has broad shoulders and a long spine. Preserve ochre/plate gold/inset/red-brown material roles; new classes can use layered curved armor without pretending the prototype is final owner art.',
  'audio':'Proposed low rounded pulse and compact downward rejection; low strings, bass clarinet/bassoon and sparse texture for nonintrusive strategic music. Existing courtyard/Cardassia tracks are candidates, not runtime faction triggers.',
  'livery':'Ochre primary plates, darker articulated insets, selective plate-gold edges and restrained amber drive/ice windows.',
  'sourceStatus':'Existing original ship/material evidence and curved/bronze UI proposal; final faction skin/audio not accepted.'},
 {'id':'romulan','displayName':'Romulan','family':'romulan','catalogueStatus':'documented-proposed-console/no-civilization-definition',
  'civilizationDefinitionIds':[],'minorDefinitionIds':[],
  'colors':{'background':'#08100E','surface':'#13251F','raised':'#1B3229','brandPrimary':'#A6D5B3','brandSecondary':'#D6C391'},
  'ship':'Proposed swept/open wing geometry with deliberate central voids and clean layered hull planes. Muted jade/graphite alloys and localized pale-green/gold light; no green wash that hides the form.',
  'audio':'Proposed clear plucked/damped glass-like electronic cue, short and quiet; sustained strings, restrained woodwind and space between phrases. Avoid whispering voices or covert-event cues that imply hidden information.',
  'livery':'Sparse long sweep masks and low-contrast panel subdivisions; registry stays readable in neutral inspection light.',
  'sourceStatus':'Historical swept/open framing proposal recovered; new palette/ships/audio remain proposals.'},
 {'id':'dominion','displayName':'Dominion','family':'dominion','catalogueStatus':'runtime-civilization/authored-scenarios-not-current-two-empire-selection',
  'civilizationDefinitionIds':['civilization.dominion'],'minorDefinitionIds':[],
  'colors':{'background':'#0D0A14','surface':'#1C1729','raised':'#292039','brandPrimary':'#C7B0ED','brandSecondary':'#B4C6D9'},
  'ship':'Proposed compact faceted central volumes with paired swept elements and a strong repeated structural rhythm; graphite/violet/bronze roles. No reviewed runtime ship-family binding was found, so this is not a delivered fleet.',
  'audio':'Proposed tightly grouped synthetic notes with a soft subharmonic body; low strings and restrained repeating textures. No continuous alarm/drone or dramatic cue on mere selection.',
  'livery':'Repeated modular plate zones, small violet identification masks and readable pale edges; emission remains localized.',
  'sourceStatus':'Runtime civilization exists; historical interlocked-rib console proposal; new ship/material/audio direction proposed.'},
 {'id':'ilex','displayName':'Ilex Survey Commune','family':'neutral','catalogueStatus':'catalogued-independent-minor/not-empire-definition',
  'civilizationDefinitionIds':[],'minorDefinitionIds':['minor.ilex.v1'],
  'colors':{'background':'#091113','surface':'#14262A','raised':'#1C3236','brandPrimary':'#9CD3D7','brandSecondary':'#CAD2B5'},
  'ship':'No exact ship binding found. Proposed compact utility geometry with protected central volumes and clear component separation; no appearance-derived study/warp capability.',
  'audio':'Proposed quiet short two-tone acknowledgment and airy, sparse acoustic/electronic background. No existing Ilex-specific sound/music asset binding.',
  'livery':'Neutral slate with sparse aqua identifiers; no speculative faction emblem or technology tier from color.',
  'sourceStatus':'Actual minor identity; neutral-family styling and all media direction proposed.'},
 {'id':'namar','displayName':'Namar Local Council','family':'neutral','catalogueStatus':'catalogued-independent-minor/not-empire-definition',
  'civilizationDefinitionIds':[],'minorDefinitionIds':['minor.namar.v1'],
  'colors':{'background':'#10100E','surface':'#24241D','raised':'#303024','brandPrimary':'#D1C49F','brandSecondary':'#B6C9D6'},
  'ship':'No exact ship binding found. Use a generic lawful marker until actual authored craft exist; future craft may use simple coherent segmented utility volumes. Do not infer technological limits from visual rusticity.',
  'audio':'Proposed dry warm acknowledgment and sparse acoustic texture; no existing Namar-specific binding. Clear semantic labels remain primary.',
  'livery':'Muted mineral/slate and small warm identifiers; no invented government insignia.',
  'sourceStatus':'Actual minor identity; neutral-family styling and all media direction proposed.'},
 {'id':'neutral','displayName':'Neutral / unknown / custom','family':'neutral','catalogueStatus':'presentation-fallback/not-a-new-civilization',
  'civilizationDefinitionIds':[],'minorDefinitionIds':[],
  'colors':{'background':'#0A1018','surface':'#172230','raised':'#223144','brandPrimary':'#B8C8DB','brandSecondary':'#C2BDCF'},
  'ship':'Explicit generic marker unless a lawful exact asset mapping exists. No class/faction/equipment inference from silhouette, names or private roots.',
  'audio':'No new binding. Keep current runtime behavior or explicitly configured shared neutral cue; never call a Federation recording a delivered authentic custom-faction set.',
  'livery':'Neutral form/labels with no faction-mark revelation.',
  'sourceStatus':'Required safe design fallback; not a gameplay faction.'},
]

families={
 'federation':{'frame':'asymmetric-elbow-rail','cornerRadius':24,'cutSize':0,'sectionMotif':'segmented-rounded-rail','headingWeight':700},
 'klingon':{'frame':'stepped-angular-brackets','cornerRadius':2,'cutSize':14,'sectionMotif':'stepped-tabs','headingWeight':700},
 'cardassian':{'frame':'layered-curved-junctions','cornerRadius':32,'cutSize':0,'sectionMotif':'nested-lens-edges','headingWeight':600},
 'romulan':{'frame':'swept-open-junctions','cornerRadius':8,'cutSize':10,'sectionMotif':'paired-open-spines','headingWeight':600},
 'dominion':{'frame':'interlocked-ribs','cornerRadius':4,'cutSize':12,'sectionMotif':'symmetric-faceted-ribs','headingWeight':700},
 'neutral':{'frame':'plain-rectilinear','cornerRadius':4,'cutSize':0,'sectionMotif':'simple-rules','headingWeight':600},
}
tokens={'schemaVersion':1,'artifactStatus':'proposed-design-no-default-activation','catalogueAuditPin':PIN,'presentationOnly':True,
 'shared':{'colors':{'textPrimary':'#EFF3FA','textSecondary':'#BCC8D8','ink':'#080D15','controlBorder':'#8093AD','focus':'#F5E6BD'},
  'states':{'available':{'color':'#91C9AD','label':'Available','shape':'check'},'active':{'color':'#EFB16B','label':'Active','shape':'clock'},
   'completed':{'color':'#B9ACE6','label':'Completed','shape':'double-check'},'warning':{'color':'#F3C478','label':'Warning','shape':'triangle'},
   'error':{'color':'#FF91A4','label':'Rejected','shape':'cross'},'information':{'color':'#91C9EC','label':'Information','shape':'info'},
   'unknown':{'color':'#BCC8D8','label':'Not reported','shape':'dash'},'unavailable':{'color':'#BCC8D8','label':'Unavailable','shape':'bar'}},
  'typography':{'family':'project-default-sans','bodyPx':18,'secondaryPx':16,'numericPx':22,'headingPx':28,'lineHeightPx':24,'tabularNumerals':True},
  'layout':{'defaultReviewWidth':1920,'defaultReviewHeight':1080,'minimumControlPx':44,'compactRowPx':48,'navigationWidthPx':176,'spacingPx':[4,8,12,16,24,32]},
  'motion':{'transitionMs':120,'reducedMotionTransitionMs':0,'reducedMotionAutoRotation':False,'flashing':False},
  'audio':{'roles':['press','accepted','rejected','processing','navigation','search','alert','music'],'newRuntimeBindings':False,'previewAudioEnabled':False}},
 'families':families,'profiles':[{k:p[k] for k in ['id','displayName','family','catalogueStatus','civilizationDefinitionIds','minorDefinitionIds','colors','sourceStatus']}|{'audio':{'semanticRoleSet':'ui-semantic-v1','cueFamily':p['id'],'bindingStatus':'proposal-no-new-runtime-binding'}} for p in profiles],
 'bindingPolicy':{'lookup':'Explicit presentation theme/profile ID; exact lawful catalogue ID mapping only where listed. No inferred or newly registered civilization IDs.',
  'unknown':'neutral','application':'Opt-in sample/provider only; preserve accepted default skin. No gameplay, AI, permission, networking, save-carrier or database-activation behavior.',
  'visibility':'Own selected presentation or lawfully disclosed public identity only. No private root, hidden faction tint/music/thumbnail or unknown-opponent inference.',
  'custom':'Unrecognized IDs use neutral; explicit validated authored mapping required for a new theme.'}}
write('theme-tokens.json',tokens)

scenario=json.loads(blob('scenarios/ds9-slice/scenario.json'))
activities=json.loads(blob('scenarios/activities-v1/activities.json'))
civs=scenario['Definitions']['Civilizations']
minors=activities['Minors']
audit={'schemaVersion':1,'auditPin':PIN,'workOrderPin':'2584f276df325f9f1c750daf8ea6dad599932cb9','historicalReferencePin':HISTORY,
 'scope':'Union of actual civilization/minor catalogue and explicitly documented theme families; species/media labels are not civilizations.',
 'civilizations':[{'id':r['DefinitionId'],'displayName':r['DisplayName']} for r in civs],
 'minorSocieties':[{'id':r['DefinitionId'],'displayName':r['DisplayName']} for r in minors],
 'documentedThemeFamilies':['Federation','Klingon','Cardassian','Romulan','Dominion'],
 'sourceEvidence':[{'pin':PIN,'path':'scenarios/ds9-slice/scenario.json','sha256':hashlib.sha256(blob('scenarios/ds9-slice/scenario.json')).hexdigest()},
  {'pin':PIN,'path':'scenarios/activities-v1/activities.json','sha256':hashlib.sha256(blob('scenarios/activities-v1/activities.json')).hexdigest()},
  {'pin':PIN,'path':'client/HobbyDS9.Unity/Assets/HobbyDS9/Review/Runtime/ReviewEntry.GeneratedCampaign.cs','claim':'Current home civilization IDs Federation and Cardassian.'},
  {'pin':PIN,'path':'client/HobbyDS9.Unity/Assets/HobbyDS9/Review/Runtime/ReviewEntry.CampaignMenus.cs','claim':'Current generated setup says Federation / Two empires.'},
  {'pin':PIN,'path':'docs/art/wireframes/combat-interface.html','claim':'Five named theme families; illustrative selector.'},
  {'pin':HISTORY,'path':'docs/art/coherent-interface/design-handoff.md','claim':'Four supplemental framing comparisons are proposals.'}],
 'speciesOnly':['Human','Vulcan','Andorian','Klingon','Cardassian','Bajoran','Ferengi','Founder','Ilex','Namar'],
 'referenceOnlyNotRegistered':['Borg-like actors mentioned by exploration design; not a catalogue/theme-selector faction.'],
 'notFoundAsCatalogueOrTheme':['Breen'],
 'missingReferences':['E:/HobbyDS9/docs/art/faction-interfaces.md','E:/HobbyDS9/docs/art/music-direction.md'],
 'profiles':[{k:p[k] for k in ['id','displayName','family','catalogueStatus','civilizationDefinitionIds','minorDefinitionIds','sourceStatus']} for p in profiles],
 'newGameplayRegistrations':0}
assert {r['id'] for r in audit['civilizations']}=={'civilization.federation','civilization.cardassian','civilization.dominion'}
assert {r['id'] for r in audit['minorSocieties']}=={'minor.ilex.v1','minor.namar.v1'}
write('catalogue-audit.json',audit)
write('SCHEMA.md','''# Faction presentation token schema v1

Proposed design, no default/runtime activation. Read `theme-tokens.json` with `schemaVersion=1`.

Frozen shared keys: `shared.colors`, `shared.states`, `shared.typography`, `shared.layout`, `shared.motion`, `shared.audio`. Family geometry is in `families`. Every `profiles[]` item has explicit presentation `id`, `family`, five `colors` keys (background/surface/raised/brandPrimary/brandSecondary), taxonomy/status and separate `civilizationDefinitionIds` / `minorDefinitionIds` arrays. Unknown maps to `neutral`. Empty arrays do not register invented IDs.

All seven audited groups plus neutral have profiles. Runtime civilization IDs exist only for Federation/Cardassian/Dominion; Klingon/Romulan are documented style proposals, Ilex/Namar are independent minors. Preserve semantic state labels/shapes/colors, data ordering, navigation hierarchy and 44px controls across families; brand and chrome differ. No LCARS recolor mandate for non-Federation families.

Theme lookup is explicit and presentation-only, with lawful identity. It must not consult private state, infer faction from names/models/music, alter commands/rules/AI/authorization, activate database mode, or change offline/online authority. Keep current default behavior; GUI owns an add-only binding/sample. JSON is an authoring/reference asset, not a request to add a JSON package or bind every profile to the current New Campaign chooser. No new audio file/automatic faction playlist binding.

The complete guide/sample/provenance packet is available through [README.md](README.md) under the same version. Source audit pin is `2fc0dbca46073121379dd9491c859a02a6f22dcd`; source work-order ancestry is older and does not imply newer assets were deleted.
''')
if '--schema-only' in sys.argv:
    print('Frozen schema v1: 3civilizations,2minor societies,5documentedthemefamilies,8presentationprofiles inclneutral.')
    raise SystemExit(0)

# Inventory exact existing source bytes, never copy/re-encode runtime media.
audio_dir='client/HobbyDS9.Unity/Assets/HobbyDS9/Audio/Music/'
music_catalog_path='client/HobbyDS9.Unity/Assets/HobbyDS9/Resources/MusicCatalog.asset'
catalog=blob(music_catalog_path).decode('utf-8-sig')
music_files=git('ls-tree','--name-only',PIN+':'+audio_dir.rstrip('/')).decode('utf-8').splitlines()
by_guid={}
for name in music_files:
    if name.endswith('.mp3'):
        meta=blob(audio_dir+name+'.meta').decode('utf-8')
        by_guid[re.search(r'^guid: (\w+)',meta,re.M).group(1)]=name
tracks=[]
for block in catalog.split('  - id: ')[1:]:
    identity=block.splitlines()[0].strip()
    guid=re.search(r'clip:.*?guid: (\w+)',block).group(1)
    name=by_guid[guid];data=blob(audio_dir+name)
    generated=name in ['Quiet Frontier.mp3','Across the Stars.mp3','Secrets in the Courtyard.mp3']
    owner_origin=name in ['Deep Station.mp3','Deep Station track 1.mp3']
    tracks.append({'id':identity,'path':audio_dir+name,'sourcePin':PIN,'guid':guid,'bytes':len(data),'sha256':hashlib.sha256(data).hexdigest(),
        'category':int(re.search(r'category: (\d+)',block).group(1)),
        'crossfadeSeconds':float(re.search(r'crossfadeSeconds: ([\d.]+)',block).group(1)),
        'quietTailSeconds':float(re.search(r'quietTailSeconds: ([\d.]+)',block).group(1)),
        'originRecord':'Original ElevenLabs generation record in docs/audio-sources.md' if generated else 'Owner-supplied Suno-origin record' if owner_origin else 'Owner-folder addition; creator/rights details unsupplied',
        'rightsStatus':'Existing source record only; terms not re-audited or expanded by this guide',
        'automaticFactionAssignment':False,'newPlaybackOrLoopClaim':False})
sfx_dir='client/HobbyDS9.Unity/Assets/HobbyDS9/Audio/SFX/LCARS/'
sfx=[]
for role,field,name in [('press','buttonPress','federation-key-beep-1.wav'),('accepted','success','federation-input-successful-1.wav'),
 ('rejected','error','federation-access-denied.wav'),('processing','processing','federation-processing-1.wav'),
 ('navigation','screenOpen','federation-screen-display-1.wav'),('search','search','federation-lcars-search.wav')]:
    data=blob(sfx_dir+name);meta=blob(sfx_dir+name+'.meta').decode('utf-8')
    sfx.append({'semanticRole':role,'existingCatalogField':field,'path':sfx_dir+name,'sourcePin':PIN,'bytes':len(data),
       'sha256':hashlib.sha256(data).hexdigest(),'guid':re.search(r'^guid: (\w+)',meta,re.M).group(1),
       'family':'Federation prototype','rightsStatus':'User-supplied franchise recordings; intended source access failed403, direct retrieval unverified, redistribution unclear. No new clearance.'})
source_records=['docs/audio-sources.md','docs/lcars-sfx-foundation.md','docs/art/combat-interface.md','docs/art/playable-battle-art-handoff-2026-10-20.md',
 'docs/demo-art-runtime-contract.md','client/HobbyDS9.Unity/Assets/HobbyDS9/ArtDemo/Runtime/ShipVisualCatalog.cs',
 'client/HobbyDS9.Unity/Assets/HobbyDS9/ArtDemo/Runtime/ActivityHullPresentation.cs',
 'client/HobbyDS9.Unity/Assets/HobbyDS9/ArtDemo/Resources/ArtDemo/ActivityHulls/CREDITS.txt',
 'client/HobbyDS9.Unity/Assets/HobbyDS9/ArtDemo/Resources/ArtDemo/Designer/Galaxy/ATTRIBUTION.txt',
 'art/ships/cardassian-cruiser-v1/manifest.json',music_catalog_path]
media={'schemaVersion':1,'sourcePin':PIN,'music':tracks,'federationPrototypeSfx':sfx,
 'newMediaFiles':0,'newAudioBinding':False,'listeningOrNativeAcceptance':False,
 'sourceRecords':[{'pin':PIN,'path':p,'sha256':hashlib.sha256(blob(p)).hexdigest()} for p in source_records],
 'shipCoverage':[
  {'family':'Federation','mapping':'civilization.federation + ship-design.source-cruiser -> ArtDemo/Federation/Prefabs/exploration-cruiser','status':'Exact received binding; lawful identity required'},
  {'family':'Federation','mapping':'hull.constitution-explorer.v1 / hull.galaxy-cruiser.v1 / hull.defiant-combat.v1 -> current ActivityHullCatalog inspector','status':'Original Constitution-style; Galaxy CC BY-NC-SA3.0; Defiant owner-provided local review with unreceived source licence/task details. Preserve exact notices.'},
  {'family':'Cardassian','mapping':'civilization.cardassian + ship-design.travel-target -> current ReviewBattleStage exact binding','status':'Original Cardassian-style cruiser prototype; final owner art/native acceptance unestablished'},
  {'family':'Klingon/Romulan/Dominion/Ilex/Namar','mapping':None,'status':'No reviewed exact ship-family runtime asset binding recovered; new shape/material direction is proposed only'}],
 'missingOriginalReferences':audit['missingReferences'],
 'shipMaterialNote':'Serialized material tuples are source values, not UI hex swatches or measured screen colors. No palette/emission replacement is made.',
 'generationBoundary':'No purchases, audio/image generation, old service reinstalls, Suno automation, media downloads, re-encoding or runtime default changes.'}
assert len(tracks)==9 and len(sfx)==6
write('media-inventory.json',media)

directions={'schemaVersion':1,'status':'Original proposals with source-status boundary; no runtime asset claim',
 'profiles':[{k:p[k] for k in ['id','displayName','ship','livery','audio','sourceStatus']} for p in profiles]}
write('art-audio-directions.json',directions)

backlog=[
 ('P0','Theme sample binding','GUI','Exact IDs, neutral fallback, shared states and no private-data/gameplay dependency; add-only opt-in sample. Storage remains first priority.'),
 ('P0','Source and rights receipts','Art / owner morning record','Complete unreceived faction/music references and Defiant/owner-folder provenance. Preserve Galaxy CC BY-NC-SA3.0 and current notices; no new redistribution assurance.'),
 ('P0','Palette / semantic accessibility','Art / GUI / QA','Measured palette pairs, label+shape meaning, keyboard focus, disabled/unknown/stale, reduced motion and mute; default1920review. Static checks do not prove full native accessibility.'),
 ('P1','Five family component sheets','Art / GUI','Shared hierarchy with genuinely different chrome; no Federation elbow rails copied onto every family. Inspect current accepted Federation before opt-in refinements.'),
 ('P1','Non-Federation shape/material studies','Art','One original gray-form study per missing major family, only after scope/rights review; then LOD/material/lighting receive gates. Do not label these guide SVGs production ships.'),
 ('P1','Semantic UI cue briefs','Art / Audio','Six short original cue roles per needed family, consistent meaning, bounded levels and no copied franchise themes/voices. Existing recordings stay prototype; no bulk generation tonight.'),
 ('P1','Music curation and audition','Art / QA','Existing nine cues, role/category/provenance review and bounded transitions before a new faction-context policy. Names do not activate switching; no seamless-loop assumption.'),
 ('P2','Minor society detail','Art / Architecture','Ilex/Namar neutral-family presentation, lawful public identity and generic craft until actual bindings exist. No capability inference from name or visual style.'),
 ('P2','Custom content / theme validation','GUI / Architecture','Bounded versioned local presentation packs, explicit mapping and neutral fallback; no untrusted script/URL/font dependency or rules change.'),
 ('P2','Native family review','QA','Default1920with real world/fleet/Engineering/production/outcome screens, own/unknown contacts, state/focus/mute/motion consistency. No global rollout before review.'),
]
write('backlog.json',{'schemaVersion':1,'status':'scoped proposals; no new production activation',
 'items':[dict(priority=a,title=b,owner=c,acceptance=d) for a,b,c,d in backlog],
 'morningDecisions':['Confirm any intended faction expansion beyond the audited five theme families and two minors; no extra civilization IDs invented.',
  'Supply missing detailed faction/music reference files if still desired; current proposals are explicitly marked.',
  'Confirm future original cue/ship-study commissioning and usable rights records before public distribution or generation spending.',
  'Approve any future opt-in theme rollout separately from storage D2; this guide does not activate either.']})

# Original vector silhouette studies: design grammar, not catalogue models or copied emblems.
shapes={
 'federation':'M36 40 C36 24 58 16 85 16 C112 16 134 24 134 40 C134 56 112 64 85 64 C58 64 36 56 36 40Z M76 63 L94 63 L100 112 L85 128 L70 112Z M34 44 L46 44 L47 124 L33 124Z M124 44 L136 44 L137 124 L123 124Z M45 82 L76 93 L74 101 L44 92Z M125 82 L94 93 L96 101 L126 92Z',
 'klingon':'M85 13 L103 40 L153 62 L169 108 L124 89 L101 73 L99 116 L85 128 L71 116 L69 73 L46 89 L1 108 L17 62 L67 40Z',
 'cardassian':'M85 11 Q110 33 101 51 Q142 48 166 80 L146 106 L109 88 L99 127 L71 127 L61 88 L24 106 L4 80 Q28 48 69 51 Q60 33 85 11Z',
 'romulan':'M85 9 L109 31 L105 51 L165 30 L151 91 L110 122 L104 76 L93 64 L93 128 L77 128 L77 64 L66 76 L60 122 L19 91 L5 30 L65 51 L61 31Z',
 'dominion':'M85 13 L106 29 L112 54 L151 47 L166 70 L139 102 L111 93 L101 124 L69 124 L59 93 L31 102 L4 70 L19 47 L58 54 L64 29Z',
 'neutral':'M60 17 L110 17 L127 43 L116 108 L97 127 L73 127 L54 108 L43 43Z',
}
for family,path in shapes.items():
    write('silhouettes/'+family+'.svg',f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 174 140"><path d="{path}" fill="#425061" stroke="#C4D0DF" stroke-width="2"/><path d="M85 25 V108" fill="none" stroke="#9EB0C5" stroke-width="2"/></svg>\n')

def luminance(color):
    c=[int(color[i:i+2],16)/255 for i in (1,3,5)]
    c=[v/12.92 if v<=.04045 else ((v+.055)/1.055)**2.4 for v in c]
    return .2126*c[0]+.7152*c[1]+.0722*c[2]
def contrast(a,b):
    v=sorted([luminance(a),luminance(b)]);return (v[1]+.05)/(v[0]+.05)
checks=[]
for p in profiles:
    for background in ['background','surface','raised']:
        for fg in ['textPrimary','textSecondary']:
            ratio=contrast(tokens['shared']['colors'][fg],p['colors'][background]);assert ratio>=4.5
            checks.append({'profile':p['id'],'foreground':fg,'background':background,'ratio':round(ratio,3),'minimum':4.5})
        for role,state in tokens['shared']['states'].items():
            ratio=contrast(state['color'],p['colors'][background]);assert ratio>=4.5
            checks.append({'profile':p['id'],'foreground':'state.'+role,'background':background,'ratio':round(ratio,3),'minimum':4.5})
        for fg in ['controlBorder','focus']:
            ratio=contrast(tokens['shared']['colors'][fg],p['colors'][background]);assert ratio>=3
            checks.append({'profile':p['id'],'foreground':fg,'background':background,'ratio':round(ratio,3),'minimum':3})
    for brand in ['brandPrimary','brandSecondary']:
        ratio=contrast(p['colors'][brand],p['colors']['surface']);assert ratio>=4.5
        checks.append({'profile':p['id'],'foreground':brand,'background':'surface','ratio':round(ratio,3),'minimum':4.5})
        ratio=contrast(tokens['shared']['colors']['ink'],p['colors'][brand]);assert ratio>=4.5
        checks.append({'profile':p['id'],'foreground':'ink','background':brand,'ratio':round(ratio,3),'minimum':4.5})
write('contrast-checks.json',{'schemaVersion':1,'status':'static sRGB token-pair measurements only, not whole-app WCAG certification',
 'formulaSource':'https://www.w3.org/WAI/WCAG22/Understanding/contrast-minimum.html',
 'nonTextSource':'https://www.w3.org/WAI/WCAG22/Understanding/non-text-contrast.html','checks':checks})

css='''*{box-sizing:border-box}html,body{margin:0;font:18px Arial,sans-serif;background:var(--bg);color:#EFF3FA}body{min-width:1450px}button,select{height:44px;min-width:44px;padding:0 14px;border:1px solid #8093AD;border-radius:4px;background:var(--raised);color:#EFF3FA;font:16px Arial}button:focus-visible,select:focus-visible,a:focus-visible{outline:2px solid #F5E6BD;outline-offset:2px}a{color:var(--brand)}.shell{height:100vh;min-height:960px;padding:18px 24px 12px 30px;border-left:8px solid var(--brand);display:flex;flex-direction:column}.header{height:82px;flex:none;display:flex;justify-content:space-between;align-items:center;border-bottom:2px solid var(--brand)}.identity{display:flex;align-items:center;gap:18px}.mark{width:54px;height:54px;background:var(--brand);opacity:.9}.eyebrow{font-size:14px;letter-spacing:1px;color:var(--secondary);margin-bottom:6px}h1{font-size:28px;margin:0}h2{font-size:21px;margin:0 0 14px}h3{font-size:17px;margin:8px 0 12px}.header-right{display:flex;align-items:center;gap:18px}.turn{font-size:16px;color:#BCC8D8}.turn strong{font-size:22px;display:block;color:#EFF3FA}.body{display:grid;grid-template-columns:176px minmax(0,1fr) 355px;gap:20px;flex:1;min-height:0;padding-top:18px}.nav{display:flex;flex-direction:column;gap:8px}.nav button{text-align:left}.nav button.current{border-left:5px solid var(--brand);background:var(--surface)}.nav .spacer{flex:1}.panel{padding:18px;background:var(--surface);border:1px solid #8093AD;border-radius:var(--radius);margin-bottom:16px;position:relative}.content{min-width:0;overflow:auto}.panel-head{display:flex;justify-content:space-between;align-items:center}.small{font-size:14px;color:#BCC8D8;line-height:1.5}.muted{color:#BCC8D8}.facts{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:12px}.fact{border-left:2px solid var(--secondary);padding:8px 12px;background:var(--raised)}.fact label{font-size:16px;color:#BCC8D8}.fact strong{display:block;font-size:24px;margin-top:8px;font-variant-numeric:tabular-nums}.fact .unit{font-size:14px;color:#BCC8D8;margin-left:6px}.primary{background:var(--brand);color:#080D15;border-color:var(--brand)}.columns,.fleet-row{display:grid;grid-template-columns:minmax(170px,1fr) 160px 180px 170px 90px;gap:12px;align-items:center}.columns{height:34px;color:var(--secondary);font-size:14px}.fleet-row{height:56px;border-top:1px solid #8093AD;font-size:16px}.fleet-row.selected{box-shadow:inset 4px 0 var(--brand);padding-left:12px;background:var(--raised)}.fleet-row strong{font-size:17px}.count{color:var(--secondary);font-variant-numeric:tabular-nums}.pill{display:inline-flex;align-items:center;gap:7px;padding:5px 8px;border:1px solid currentColor;border-radius:4px;font-size:14px;white-space:nowrap}.pill svg{width:15px;height:15px;flex:none}.actions{display:flex;gap:10px;margin-top:14px}.state-strip{display:flex;flex-wrap:wrap;gap:9px;margin-top:8px}.right{overflow:auto;min-height:0}.swatches{display:grid;grid-template-columns:repeat(3,1fr);gap:8px}.swatch{height:30px;border:1px solid #8093AD;border-radius:3px}.swatch-name{font-size:11px;color:#BCC8D8;margin-top:5px}.silhouette{height:130px;text-align:center;margin:8px 0}.silhouette img{height:100%;width:100%;object-fit:contain}.brief{font-size:14px;color:#BCC8D8;line-height:1.55}.footer{height:38px;flex:none;display:flex;align-items:center;justify-content:space-between;border-top:1px solid #8093AD;margin-top:8px;font-size:13px;color:#BCC8D8}.footer b{color:var(--secondary)}.family-federation .shell{border-top-left-radius:48px;border-bottom-left-radius:48px}.family-federation .mark{border-radius:28px 7px 7px 7px}.family-federation .panel-head{border-left:16px solid var(--brand);border-radius:18px 0 0 0;padding-left:12px}.family-klingon .mark{clip-path:polygon(0 0,75% 0,100% 35%,100% 100%,25% 100%,0 65%)}.family-klingon .header{border-bottom-width:5px}.family-klingon .panel{border-left:5px solid var(--brand);box-shadow:7px 7px 0 -4px var(--secondary)}.family-klingon h2{border-top:3px solid var(--brand);padding-top:8px}.family-cardassian .mark{border-radius:50% 20% 50% 20%}.family-cardassian .panel{border-radius:28px 9px 28px 9px;border-left:5px solid var(--brand);box-shadow:inset 0 0 0 4px var(--raised)}.family-cardassian .panel-head{border-bottom:2px solid var(--secondary);padding-bottom:8px;margin-bottom:12px}.family-romulan .mark{clip-path:polygon(0 0,40% 30%,50% 100%,60% 30%,100% 0,85% 85%,15% 85%)}.family-romulan .panel{border-right:0;border-top:2px solid var(--brand);border-left:2px solid var(--secondary);border-radius:0 18px 0 0}.family-romulan .panel-head{border-left:4px double var(--secondary);padding-left:12px}.family-dominion .mark{clip-path:polygon(25% 0,75% 0,100% 25%,75% 50%,100% 75%,75% 100%,25% 100%,0 75%,25% 50%,0 25%)}.family-dominion .panel{border-top:5px solid var(--brand);border-bottom:5px solid var(--secondary);border-radius:4px;box-shadow:inset 7px 0 0 var(--raised),inset -7px 0 0 var(--raised)}.family-dominion .panel-head{border-left:8px solid var(--secondary);padding-left:12px}.family-neutral .mark{border-radius:3px;background:transparent;border:3px solid var(--brand)}'''
embedded=json.dumps(tokens).replace('</','<\\/')
directions_embedded=json.dumps({p['id']:{'ship':p['ship'],'audio':p['audio'],'status':p['sourceStatus']} for p in profiles}).replace('</','<\\/')
page='''<!doctype html><html lang="en"><meta charset="utf-8"><title>HobbyDS9 faction style reference v1</title><meta name="viewport" content="width=device-width,initial-scale=1"><style>CSS</style><body><div class="shell"><header class="header"><div class="identity"><div class="mark" aria-hidden="true"></div><div><div class="eyebrow">PROPOSED FAMILY REFERENCE / SAME SAMPLE FACTS</div><h1 id="theme-name"></h1></div></div><div class="header-right"><div class="turn">Reference phase<strong>Planning</strong></div><label>Preview family <select id="theme-select" aria-label="Preview presentation family"></select></label></div></header><div class="body"><nav class="nav"><button>Map</button><button class="current">Worlds</button><button>Production</button><button>Fleets</button><button>Research</button><button>Designer</button><div class="spacer"></div><button>Settings</button><button>Help & credits</button></nav><main class="content"><section class="panel"><div class="panel-head"><h2>Reference colony</h2><span class="pill" style="color:#91C9AD">✓ Own record</span></div><div class="facts"><div class="fact"><label>Population</label><strong>1,013</strong></div><div class="fact"><label>Industry</label><strong>8<span class="unit">/ week</span></strong></div><div class="fact"><label>Research</label><strong>50<span class="unit">/ week</span></strong></div><div class="fact"><label>Food balance</label><strong>+3<span class="unit">/ week</span></strong></div></div><div class="actions"><button class="primary">Manage colony</button><button>Environment</button><button>Initial resources</button><button>Survey record</button></div></section><section class="panel"><div class="panel-head"><h2>Fleet / same lawful sample</h2><span class="small">Fixture values; no hidden faction state</span></div><div class="columns"><span>Vessel</span><span>Hull</span><span>Shields</span><span>Supply</span><span>Inspect</span></div><div class="fleet-row selected"><strong>Concept vessel A</strong><span class="count">80 / 100</span><span>Fore 30 / 40</span><span>Not reported</span><button>Inspect</button></div><div class="fleet-row"><strong>Concept vessel B</strong><span class="count">100 / 100</span><span>Not reported</span><span>Not reported</span><button>Inspect</button></div><div class="fleet-row"><strong>Concept vessel C</strong><span class="count">60 / 80</span><span>Aft 20 / 40</span><span>Not reported</span><button>Inspect</button></div><div class="actions"><button>Review organization</button><button>Accepted mission</button></div></section><section class="panel"><div class="panel-head"><h2>Shared meaning across every family</h2></div><div id="states" class="state-strip"></div><p class="small">Labels and owned shapes retain meaning. Brand accents never redefine acceptance, rejection, unknown information or keyboard focus.</p></section></main><aside class="right"><section class="panel"><div class="eyebrow">PROFILE / PROVENANCE</div><h3 id="profile-status"></h3><div id="swatches" class="swatches"></div><p id="source-status" class="brief"></p></section><section class="panel"><div class="eyebrow">ORIGINAL SHAPE STUDY / PROPOSED</div><div class="silhouette"><img id="silhouette" alt="Original silhouette grammar study, not a delivered class"></div><div id="ship-direction" class="brief"></div></section><section class="panel"><div class="eyebrow">AUDIO DIRECTION / SILENT REFERENCE</div><div id="audio-direction" class="brief"></div></section></aside></div><footer class="footer"><b>DESIGN PROPOSAL · NOT LIVE UI / NATIVE ACCEPTANCE</b><span>No media playback, gameplay effects, new civilization registration or default activation</span></footer></div><script id="theme-data" type="application/json">TOKENS</script><script id="directions-data" type="application/json">DIRECTIONS</script><script>const data=JSON.parse(document.getElementById('theme-data').textContent),dirs=JSON.parse(document.getElementById('directions-data').textContent);const id=new URLSearchParams(location.search).get('theme')||'federation';const p=data.profiles.find(v=>v.id===id)||data.profiles.find(v=>v.id==='neutral');document.body.className='family-'+p.family;for(const [key,value] of Object.entries(p.colors)){document.documentElement.style.setProperty('--'+({background:'bg',surface:'surface',raised:'raised',brandPrimary:'brand',brandSecondary:'secondary'}[key]),value);}document.documentElement.style.setProperty('--radius',data.families[p.family].cornerRadius+'px');document.getElementById('theme-name').textContent=p.displayName;document.getElementById('profile-status').textContent=p.catalogueStatus.replaceAll('-',' ');document.getElementById('source-status').textContent=dirs[p.id].status;document.getElementById('ship-direction').textContent=dirs[p.id].ship;document.getElementById('audio-direction').textContent=dirs[p.id].audio;document.getElementById('silhouette').src='silhouettes/'+p.family+'.svg';const select=document.getElementById('theme-select');for(const option of data.profiles){const e=document.createElement('option');e.value=option.id;e.textContent=option.displayName;e.selected=option.id===p.id;select.appendChild(e);}select.onchange=()=>location.search='?theme='+encodeURIComponent(select.value);for(const [key,value] of Object.entries(p.colors)){const box=document.createElement('div');const swatch=document.createElement('div');swatch.className='swatch';swatch.style.background=value;box.appendChild(swatch);const label=document.createElement('div');label.className='swatch-name';label.textContent=key+' '+value;box.appendChild(label);document.getElementById('swatches').appendChild(box);}const symbols={available:'M2 8L6 12L14 3',active:'M8 1L8 8L13 11',completed:'M1 8L5 12L12 3M6 8L10 12L16 3',warning:'M8 1L15 14H1ZM8 5V9M8 11V12',error:'M3 3L13 13M13 3L3 13',information:'M8 6V13M8 2V3',unknown:'M3 8H13',unavailable:'M2 6H14V10H2Z'};for(const [key,state] of Object.entries(data.shared.states)){const chip=document.createElement('span');chip.className='pill';chip.style.color=state.color;chip.innerHTML='<svg viewBox="0 0 17 16" aria-hidden="true"><path fill="none" stroke="currentColor" stroke-width="2" d="'+symbols[key]+'"/></svg>';chip.appendChild(document.createTextNode(state.label));document.getElementById('states').appendChild(chip);}</script></body></html>'''.replace('CSS',css).replace('TOKENS',embedded).replace('DIRECTIONS',directions_embedded)
write('samples.html',page)

cards=[]
for p in profiles:
    cards.append('<a class="card" href="samples.html?theme='+p['id']+'"><h2>'+html.escape(p['displayName'])+'</h2><div class="status">'+html.escape(p['catalogueStatus'].replace('-',' '))+'</div><img src="previews/'+p['id']+'-1920.png" alt="'+html.escape(p['displayName'])+' proposed sample"><div class="swatches">'+''.join('<i style="background:'+c+'"></i>' for c in p['colors'].values())+'</div><p>'+html.escape(families[p['family']]['frame'].replace('-',' '))+'</p></a>')
write('index.html','<!doctype html><html lang="en"><meta charset="utf-8"><title>Faction comparison board</title><style>*{box-sizing:border-box}body{margin:0;background:#080D15;color:#EFF3FA;font:18px Arial;padding:22px}h1{margin:0 0 8px;font-size:28px}header p{color:#BCC8D8;margin:0 0 18px}main{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:16px}a{color:inherit;text-decoration:none}.card{background:#101B29;border:1px solid #8093AD;border-radius:6px;padding:14px;min-width:0}.card:focus-visible{outline:3px solid #F5E6BD;outline-offset:2px}h2{font-size:20px;margin:0 0 6px}.status{font-size:13px;min-height:36px;color:#BCC8D8}.card img{width:100%;aspect-ratio:16/9;object-fit:contain;margin:10px 0}.swatches{display:flex;gap:8px}.swatches i{display:block;width:42px;height:20px;border:1px solid #8093AD}.card p{font-size:14px;color:#BCC8D8;margin-bottom:0}footer{font-size:14px;color:#BCC8D8;margin-top:18px}</style><header><h1>HobbyDS9 / faction presentation comparison</h1><p>Five documented theme families, two actual minor societies and neutral fallback. New designs are proposals; identity status is explicit. Click a card for the full1920reference.</p></header><main>'+''.join(cards)+'</main><footer>Original design/reference samples, not native acceptance, delivered ships, new civilization registration or active audio. Shared state labels/contrast/privacy rules remain consistent.</footer></html>\n')

matrix=['# Comparative faction presentation matrix','',
 'All new palettes/media directions are proposals. Runtime/species/minor taxonomy is in catalogue-audit.json.','',
 '| Profile | Catalogue status | Geometry | Brand / secondary | Ships/materials/livery | Audio direction |',
 '| --- | --- | --- | --- | --- | --- |']
for p in profiles:
    matrix.append('| '+ ' | '.join([p['displayName'],p['catalogueStatus'],families[p['family']]['frame'],p['colors']['brandPrimary']+' / '+p['colors']['brandSecondary'],p['ship']+' '+p['livery'],p['audio']])+' |')
write('comparison-matrix.md','\n'.join(matrix)+'\n')
print('Wrote full proposed family reference packet:8profiles,6geometryfamilies,9existingmusic/6prototypeSFX receipts; no runtime/media edits.')
