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

Frozen shared keys: `shared.colors`, `shared.states`, `shared.typography`, `shared.layout`, `shared.motion`, `shared.audio`. Family geometry is in `families`. Every `profiles[]` item has explicit presentation `id`, `family`, six `colors`, taxonomy/status and separate `civilizationDefinitionIds` / `minorDefinitionIds` arrays. Unknown maps to `neutral`. Empty arrays do not register invented IDs.

All seven audited groups plus neutral have profiles. Runtime civilization IDs exist only for Federation/Cardassian/Dominion; Klingon/Romulan are documented style proposals, Ilex/Namar are independent minors. Preserve semantic state labels/shapes/colors, data ordering, navigation hierarchy and 44px controls across families; brand and chrome differ. No LCARS recolor mandate for non-Federation families.

Theme lookup is explicit and presentation-only, with lawful identity. It must not consult private state, infer faction from names/models/music, alter commands/rules/AI/authorization, activate database mode, or change offline/online authority. Keep current default behavior; GUI owns an add-only binding/sample. JSON is an authoring/reference asset, not a request to add a JSON package or bind every profile to the current New Campaign chooser. No new audio file/automatic faction playlist binding.

The complete guide/sample/provenance packet follows under the same version. Source audit pin is `2fc0dbca46073121379dd9491c859a02a6f22dcd`; source work-order ancestry is older and does not imply newer assets were deleted.
''')
if '--schema-only' in sys.argv:
    print('Frozen schema v1: 3civilizations,2minor societies,5documentedthemefamilies,8presentationprofiles inclneutral.')
    raise SystemExit(0)
