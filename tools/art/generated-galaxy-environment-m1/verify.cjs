/* Independent raw-export, actual screen transform, picking and export checks. */
const fs=require('fs'),path=require('path'),crypto=require('crypto'),{chromium}=require('playwright');
const indexPath=path.resolve(process.argv[2]),out=path.resolve(process.argv[3]),index=JSON.parse(fs.readFileSync(indexPath));
const assert=(v,m)=>{if(!v)throw Error(m)},sha=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const captures=JSON.parse(fs.readFileSync(path.join(out,'review-checks.json')));for(const c of captures.captures)assert(sha(path.join(out,c.file))===c.sha256,'Screenshot changed');
for(const s of captures.sourceHashes)assert(sha(path.join(__dirname,s.file))===s.sha256,'Source changed after capture');
let browser;const results=[],errors=[];
(async()=>{
 browser=await chromium.launch({channel:'chrome',headless:true,args:['--disable-gpu']});const page=await browser.newPage();page.setDefaultTimeout(10000);page.on('pageerror',e=>errors.push(e.message));const url='file:///'+out.replaceAll('\\','/')+'/index.html';
 for(const [width,height]of [[1280,720],[1920,1080],[3440,1440]]){
  await page.setViewportSize({width,height});await page.goto(url);
  for(const item of index.samples.filter(s=>s.variant==='normal')){
   const raw=JSON.parse(fs.readFileSync(path.resolve(path.dirname(indexPath),item.document))),d=raw.Layout;
   await page.locator('#seed').fill(raw.Settings.Map.Seed);await page.locator('#size').selectOption(String(d.Systems.length));await page.locator('#fixture').selectOption('normal');await page.locator('#generate').click();
   const screen=async p=>page.evaluate(p=>{const s=GENERATED_MAP_DESIGN.state.camera,r=document.querySelector('#map').getBoundingClientRect();return{x:r.left+p.X*s.scale+s.tx,y:r.top+p.Y*s.scale+s.ty}},p);
   for(const f of raw.Features.filter(f=>f.Kind===1)){const p=await screen(f.Position);await page.mouse.click(p.x,p.y);const actual=await page.evaluate(()=>GENERATED_MAP_DESIGN.state.selection);assert(actual.kind==='system'&&actual.id===f.SystemId,'Actual star centre picking mismatch');const shown=await page.locator('.density-details').evaluate(e=>({node:e.dataset.densityNode,base:+e.dataset.baseDensity,target:+e.dataset.targetDensity,contributions:[...e.querySelectorAll('.contribution')].map(c=>({FeatureId:c.dataset.featureId,Amount:+c.dataset.amount}))})),truth=raw.RegionInfluences.find(i=>i.NodeId===f.NodeId);assert(shown.node===truth.NodeId&&shown.base===truth.BaseDensity&&shown.target===truth.TargetDensity&&JSON.stringify(shown.contributions)===JSON.stringify(truth.Contributions),'Inspector invented density contributions');}
   // All overlapping features are independently reachable through the real index controls.
   for(const f of raw.Features.filter(f=>f.Kind!==1)){await page.locator('#environment').click();await page.locator(`[data-inspect-feature="${f.FeatureId}"]`).click();assert(await page.evaluate(()=>GENERATED_MAP_DESIGN.state.selection.id)===f.FeatureId,'Feature index inaccessible');assert((await page.locator('#inspector').innerText()).includes(f.DisplayName),'Feature name mismatch');}
   await page.locator('#influence-toggle').click();assert(await page.locator('[data-influence-footprint]').count()===raw.Features.length,'Invented/missing footprints');
   const clipping=await page.evaluate(()=>{const s=GENERATED_MAP_DESIGN.state.camera,e=document.querySelector('#extent-rect');return {x:+e.getAttribute('x'),y:+e.getAttribute('y'),width:+e.getAttribute('width'),height:+e.getAttribute('height'),features:document.querySelector('#features').getAttribute('clip-path'),influences:document.querySelector('#influence').getAttribute('clip-path'),camera:s};});assert(clipping.x===clipping.camera.tx&&clipping.y===clipping.camera.ty&&clipping.width===d.Width*clipping.camera.scale&&clipping.height===d.Height*clipping.camera.scale&&clipping.features==='url(#map-extent)'&&clipping.influences==='url(#map-extent)','Feature footprints not clipped to exact map extent');
   for(const f of raw.Features){const e=page.locator(`[data-influence-footprint="${f.FeatureId}"]`),s=await page.evaluate(()=>GENERATED_MAP_DESIGN.state.camera);assert(Math.abs(+await e.getAttribute('rx')-f.RadiusX*s.scale)<1e-7&&Math.abs(+await e.getAttribute('ry')-f.RadiusY*s.scale)<1e-7,'Footprint radius mismatch');}
   await page.locator('#influence-toggle').click();
   if(raw.Settings.Map.Seed==='42'){
    await page.locator('#route').click();
    const routePoints=await page.evaluate(()=>{
     const state=GENERATED_MAP_DESIGN.state,raw=GENERATED_SAMPLES.find(s=>s.document.ContentHash===state.sampleHash).document,p=document.querySelector('[data-route-nodes]'),ids=p.dataset.routeNodes.split('|'),actual=p.getAttribute('points').split(' ').map(s=>s.split(',').map(Number));
     ids.forEach((id,i)=>{const r=raw.Layout.Regions.find(r=>r.NodeId===id);if(Math.abs(actual[i][0]-(r.Center.X*state.camera.scale+state.camera.tx))>1e-7||Math.abs(actual[i][1]-(r.Center.Y*state.camera.scale+state.camera.ty))>1e-7)throw Error('Route point coordinate mismatch');});return ids.length;
    });assert(routePoints>1,'Route has no returned steps');
   }
   // Keyboard selection must retain an actual focusable target after the redraw.
   const bh=page.locator('.black-hole').first();await bh.focus();await page.keyboard.press('Enter');assert(await page.evaluate(()=>document.activeElement.classList.contains('black-hole')),'Feature keyboard focus lost');
   const star=page.locator('.system').first();await star.focus();await page.keyboard.press('Enter');assert(await page.evaluate(()=>document.activeElement.classList.contains('system')),'System keyboard focus lost');
   const download=page.waitForEvent('download');await page.locator('#export').click();const file=path.join(out,`verified-${width}-${raw.Settings.Map.Seed}-${d.Systems.length}.json`);await(await download).saveAs(file);assert(JSON.stringify(JSON.parse(fs.readFileSync(file)))===JSON.stringify(raw),'Export altered Format2 document');
   const retainedHash=await page.evaluate(()=>GENERATED_MAP_DESIGN.state.sampleHash);await page.locator('#seed').fill('18446744073709551615');await page.locator('#generate').click();assert(await page.evaluate(()=>GENERATED_MAP_DESIGN.state.sampleHash)===retainedHash,'Unavailable sample replaced current map');
   results.push({width,height,seed:raw.Settings.Map.Seed,systemsPicked:d.Systems.length,featuresIndependentlyInspected:raw.Features.filter(f=>f.Kind!==1).length,footprintsMatched:raw.Features.length,exportUnchanged:true,keyboardFocusRetained:true,unavailableSamplePreservesCurrentMap:true});
  }
 }
 await page.goto('file:///'+out.replaceAll('\\','/')+'/review.html');for(const img of await page.locator('img').all())assert(await img.evaluate(e=>e.complete&&e.naturalWidth>0),'Missing gallery screenshot');
 assert(!errors.length,errors.join('\n'));await browser.close();browser=null;const result={status:'Browser/data evidence, not native Unity acceptance',passed:true,results,capturesVerified:captures.captures.length,sourceHashesVerified:true,errors};fs.writeFileSync(path.join(out,'feature-parity-checks.json'),JSON.stringify(result,null,2));console.log(JSON.stringify({passed:true,maps:results.length,systemsPicked:results.reduce((n,x)=>n+x.systemsPicked,0),featuresInspected:results.reduce((n,x)=>n+x.featuresIndependentlyInspected,0)}));
})().catch(async e=>{console.error(e);await browser?.close();process.exitCode=1});
