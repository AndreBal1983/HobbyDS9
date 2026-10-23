#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HobbyDS9.Contracts;
using UnityEngine;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewEntry
    {
        private bool generatedGalaxyOpen;
        private GeneratedGalaxyWorkspace? galaxy;
        private VisualElement? galaxyHost;
        private ScrollView? galaxyInspector;
        private GeneratedGalaxyMapView? galaxyMap;
        private Label? galaxyNotice;
        private string? galaxySelected,galaxyRouteStart;
        private GeneratedGalaxySelectionKind galaxySelectionKind;
        private string? galaxySelectedFeature;
        private bool galaxyDensityCauses;
        private bool galaxyBusy;
        private GeneratedGalaxyRouteDraft? galaxyDraft;
        private GalaxyReviewRoutePlanSnapshot? galaxyPlan;
        private VisualElement? galaxyRoutePanel;
        private string? galaxyDraftNotice;
        private GeneratedGalaxyRouteDraftStore? galaxyDraftStore;

        private void OpenGeneratedGalaxy()
        {
            if(galaxy==null)
            {
                try
                {
                    var asset=Resources.Load<TextAsset>(PlayableFleetOptions.ResourceRoot+"scenario");
                    if(asset==null)throw new InvalidOperationException();
                    var source=new ReviewNewtonsoftCodec().DecodeScenario(asset.bytes);
                    galaxy=new GeneratedGalaxyWorkspace(source.Definitions.Civilizations??throw new InvalidOperationException(),
                        new GeneratedGalaxyMapStore(Path.Combine(UnityEngine.Application.persistentDataPath,GeneratedGalaxyMapStore.DirectoryName))){Size=250,NaturalGeneration=true};
                }
                catch(Exception){profileMenuNotice="Generated-map review is unavailable. Your campaign and saves are kept.";Draw();return;}
            }
            generatedGalaxyOpen=true;viewEpoch++;Build();
        }
        private void BuildGeneratedGalaxy()
        {
            root!.RemoveFromClassList("player-console");root.RemoveFromClassList("client-root");
            galaxyHost=new VisualElement{name="generated-galaxy-review"};galaxyHost.AddToClassList("galaxy-root");root.Add(galaxyHost);
            GalaxyResponsive(root.contentRect.width);
            var header=new VisualElement();header.AddToClassList("galaxy-header");galaxyHost.Add(header);
            var title=Readout("GENERATED GALAXY");title.AddToClassList("galaxy-title");header.Add(title);
            var back=AddButton(header,"Back to menu",()=>{generatedGalaxyOpen=false;viewEpoch++;Build();});back.name="galaxy-back";
            var subtitle=Readout("Full generated map · review only · no campaign");subtitle.AddToClassList("galaxy-subtitle");galaxyHost.Add(subtitle);
            var controls=new VisualElement();controls.AddToClassList("galaxy-controls");galaxyHost.Add(controls);
            var epoch=viewEpoch;
            var seed=new TextField("Seed"){name="galaxy-seed",value=galaxy!.Seed,maxLength=20,tooltip="Unsigned whole number, without leading zeros; 0 is allowed."};controls.Add(seed);
            seed.RegisterValueChangedCallback(e=>{if(epoch==viewEpoch)galaxy.Seed=e.newValue;});
            var size=new DropdownField("Systems",new List<string>{"30","40","50","250","1000"},galaxy.Size.ToString(CultureInfo.InvariantCulture)){name="galaxy-size"};controls.Add(size);
            size.RegisterValueChangedCallback(e=>{if(epoch==viewEpoch){galaxy.Size=int.Parse(e.newValue,CultureInfo.InvariantCulture);root.Q<DropdownField>("galaxy-quiet-target")?.SetEnabled(galaxy.Size>=250);}});
            var generation=new DropdownField("Large-map model",new List<string>{"Natural v5","Retained scaled v4"},galaxy.NaturalGeneration?0:1){name="galaxy-generation-model"};controls.Add(generation);
            generation.RegisterValueChangedCallback(_=>{if(epoch==viewEpoch)galaxy.NaturalGeneration=generation.index==0;});
            var generate=AddButton(controls,"Generate",()=>{
                void Run(){RunGalaxyOperation(draft=>draft.Generate(),"Generating map…");}
                if(galaxy.Unexported)Confirm("Replace this unexported map? Export it first to keep it. A failed generation keeps the current map.",Run);else Run();
            });generate.name="galaxy-generate";generate.AddToClassList("primary-action");
            var slot=new DropdownField("Map slot",new List<string>{"1","2","3"},galaxy.Slot.ToString(CultureInfo.InvariantCulture)){name="galaxy-slot"};controls.Add(slot);
            slot.RegisterValueChangedCallback(e=>{if(epoch==viewEpoch)galaxy.Slot=int.Parse(e.newValue,CultureInfo.InvariantCulture);});
            var export=AddButton(controls,"Export",()=>{
                void Run(bool overwrite){galaxy.Export(overwrite);DrawGeneratedGalaxyStatus();}
                if(galaxy.SlotOccupied())Confirm("Replace the exported map in slot "+galaxy.Slot+"? Campaign saves are separate.",()=>Run(true));else Run(false);
            });export.name="galaxy-export";export.SetEnabled(galaxy.Document!=null);
            var open=AddButton(controls,"Open",()=>{
                void Run(){RunGalaxyOperation(draft=>draft.Open(),"Opening and validating map…");}
                if(galaxy.Unexported)Confirm("Open map slot "+galaxy.Slot+" and replace this unexported map? Failed validation keeps the current map.",Run);else Run();
            });open.name="galaxy-open";
            var tuning=new DropdownField("Quiet-area target",new List<string>{"1×","2×","3×"},(galaxy.QuietEmptyAreaTargetPermille/1000)+"×"){name="galaxy-quiet-target",
                tooltip="Soft quiet-empty/system mean-area target for 250/1000 maps. Disturbed empty regions are exceptions; actual results vary."};controls.Add(tuning);
            tuning.RegisterValueChangedCallback(e=>{if(epoch==viewEpoch)galaxy.QuietEmptyAreaTargetPermille=(tuning.index+1)*1000;});
            tuning.SetEnabled(galaxy.Size>=250);
            galaxyNotice=Readout(galaxy.Notice);galaxyNotice.name="galaxy-status";galaxyNotice.AddToClassList("galaxy-status");galaxyHost.Add(galaxyNotice);
            var body=new VisualElement();body.AddToClassList("galaxy-body");galaxyHost.Add(body);
            var column=new VisualElement();column.AddToClassList("galaxy-map-column");body.Add(column);
            var document=galaxy.Document;
            if(document==null)
            {
                var empty=new VisualElement{name="galaxy-empty"};empty.AddToClassList("galaxy-map");column.Add(empty);
                var instruction=Readout("Choose a seed to create your first map.\n\n250 systems + 250 empty regions at 2.5m × 2m map units.\n1000 systems + 1000 empty regions at 5m × 4m.\n30/40/50 retain the earlier generator.\nTwo candidate starting locations; no campaign activation.");
                instruction.style.marginLeft=28;instruction.style.marginTop=28;instruction.style.maxWidth=500;empty.Add(instruction);galaxyMap=null;
            }
            else
            {
                if(galaxyDraft==null)galaxyDraft=new GeneratedGalaxyRouteDraft(document.ContentHash,GalaxyReviewRoutePolicy.MaximumTargets);
                galaxyDraft.ChangeMap(document.ContentHash);
                galaxyMap=new GeneratedGalaxyMapView(document,galaxy.CivilizationNames,(b,a)=>BindButton(b,a),SelectGalaxyNode,PreviewGalaxyRoute,SelectGalaxyFeature,PreviewGalaxyTarget);column.Add(galaxyMap);
                if(galaxySelected!=null)galaxyMap.Select(galaxySelected,galaxySelectionKind);
                galaxyMap.SelectFeature(galaxySelectedFeature);
                galaxyMap.RoutePlan(galaxyDraft.Start?.NodeId,galaxyPlan,galaxyDraft.Targets);
                var tools=new VisualElement();tools.AddToClassList("galaxy-map-tools");column.Add(tools);
                AddGalaxyTool(tools,"Fit","galaxy-fit",()=>galaxyMap.Fit());
                AddGalaxyTool(tools,"−","galaxy-zoom-out",()=>galaxyMap.ZoomBy(1/1.3));
                AddGalaxyTool(tools,"+","galaxy-zoom-in",()=>galaxyMap.ZoomBy(1.3));
                AddGalaxyTool(tools,"←","galaxy-pan-left",()=>galaxyMap.Pan(90,0));
                AddGalaxyTool(tools,"→","galaxy-pan-right",()=>galaxyMap.Pan(-90,0));
                AddGalaxyTool(tools,"↑","galaxy-pan-up",()=>galaxyMap.Pan(0,90));
                AddGalaxyTool(tools,"↓","galaxy-pan-down",()=>galaxyMap.Pan(0,-90));
                var links=AddButton(tools,"Graph links: off",()=>{galaxyMap.ToggleGraphLinks();root.Q<Button>("galaxy-graph-links").text=galaxyMap.ShowGraphLinks?"Graph links: on":"Graph links: off";});
                links.name="galaxy-graph-links";
                if(document.HasEnvironment)
                {
                    var features=AddButton(tools,"Features: on",()=>{galaxyMap.ToggleFeatures();root.Q<Button>("galaxy-features").text=galaxyMap.ShowFeatures?"Features: on":"Features: off";});
                    features.name="galaxy-features";
                    var areas=AddButton(tools,"Influence areas: off",()=>{galaxyMap.ToggleInfluenceAreas();root.Q<Button>("galaxy-influence-areas").text=galaxyMap.ShowInfluenceAreas?"Influence areas: on":"Influence areas: off";});
                    areas.name="galaxy-influence-areas";
                    var causes=AddButton(tools,galaxyDensityCauses?"Density causes: on":"Density causes: off",()=>{galaxyDensityCauses=!galaxyDensityCauses;root.Q<Button>("galaxy-density-causes").text=galaxyDensityCauses?"Density causes: on":"Density causes: off";DrawGalaxyInspector();});
                    causes.name="galaxy-density-causes";
                }
            }
            var hint=Readout("Drag to pan · wheel to zoom · right-click replaces destination · Shift + right-click adds a waypoint");
            hint.AddToClassList("galaxy-hint");column.Add(hint);
            var inspectorColumn=new VisualElement();inspectorColumn.AddToClassList("galaxy-inspector-column");body.Add(inspectorColumn);
            galaxyRoutePanel=new VisualElement{name="galaxy-route-preview"};galaxyRoutePanel.AddToClassList("galaxy-route-preview");inspectorColumn.Add(galaxyRoutePanel);
            galaxyInspector=new ScrollView(ScrollViewMode.Vertical){name="galaxy-inspector"};galaxyInspector.AddToClassList("galaxy-inspector");inspectorColumn.Add(galaxyInspector);
            DrawGalaxyRoutePanel();
            DrawGalaxyInspector();DrawGeneratedGalaxyStatus();
        }
        private void AddGalaxyTool(VisualElement parent,string label,string name,Action action)
        {var button=AddButton(parent,label,action);button.name=name;}
        private async void RunGalaxyOperation(Func<GeneratedGalaxyWorkspace,bool> operation,string notice)
        {
            if(galaxyBusy)return;galaxyBusy=true;var epoch=viewEpoch;
            var live=galaxy!;var prior=live.Document;var draft=live.CaptureOperation();
            var seed=live.Seed;var size=live.Size;var slot=live.Slot;var target=live.QuietEmptyAreaTargetPermille;var natural=live.NaturalGeneration;
            galaxyHost!.SetEnabled(false);galaxyNotice!.text=notice;
            try
            {
                await Task.Run(()=>operation(draft));
                if(this==null||epoch!=viewEpoch||!ReferenceEquals(galaxy,live)||live.Seed!=seed||live.Size!=size||live.Slot!=slot||live.QuietEmptyAreaTargetPermille!=target||live.NaturalGeneration!=natural)return;
                bool replaced=live.PublishOperation(draft,prior);
                if(replaced)GalaxyReplaced();else DrawGeneratedGalaxyStatus();
            }
            catch(Exception error)
            {
                Debug.LogException(error);
                if(this!=null&&epoch==viewEpoch&&galaxyNotice!=null)galaxyNotice.text="Map operation failed. Current map is kept.";
            }
            finally{galaxyBusy=false;if(this!=null&&galaxyHost!=null)galaxyHost.SetEnabled(true);}
        }
        private void GalaxyResponsive(float width)
        {
            galaxyHost?.EnableInClassList("galaxy-compact",width<1600);
            galaxyHost?.EnableInClassList("galaxy-wide",width>=2800);
        }
        private void GalaxyReplaced()
        {
            galaxySelected=null;galaxySelectedFeature=null;galaxyDensityCauses=false;galaxyRouteStart=null;galaxyDraft=null;galaxyPlan=null;galaxyDraftNotice=null;viewEpoch++;Build();
        }
        private void DrawGeneratedGalaxyStatus(){if(galaxyNotice!=null)galaxyNotice.text=galaxy!.Notice;}
        private void SelectGalaxyNode(string node,GeneratedGalaxySelectionKind kind)
        {
            galaxySelected=node;galaxySelectedFeature=null;galaxySelectionKind=kind;galaxyMap!.Select(node,kind);galaxyMap.SelectFeature(null);DrawGalaxyInspector();galaxyInspector!.scrollOffset=Vector2.zero;
        }
        private void SelectGalaxyFeature(string featureId)
        {
            var feature=galaxy!.Document!.Features.Single(f=>f.FeatureId==featureId);
            galaxySelectedFeature=featureId;galaxySelected=feature.NodeId;galaxySelectionKind=GeneratedGalaxySelectionKind.Region;
            galaxyMap!.Select(feature.NodeId??"",GeneratedGalaxySelectionKind.Region);galaxyMap.SelectFeature(featureId);DrawGalaxyInspector();galaxyInspector!.scrollOffset=Vector2.zero;
        }
        private string GalaxyNodeName(string node)
        {
            var document=galaxy!.Document!;var region=document.Regions.Single(r=>r.NodeId==node);
            return region.SystemId==null?region.DisplayName:document.Systems.Single(s=>s.SystemId==region.SystemId).DisplayName;
        }
        private void PreviewGalaxyRoute(string target)
        {
            PreviewGalaxyTarget(target,false,GeneratedGalaxyDraftAnchorKind.RegionTransit);
        }
        private void PreviewGalaxyTarget(string target,bool append,GeneratedGalaxyDraftAnchorKind kind)
        {
            if(galaxyDraft==null)return;
            if(galaxyDraft.Start==null)
                galaxyDraft.SetStart(new GeneratedGalaxyDraftAnchor(galaxySelected??target,GeneratedGalaxyDraftAnchorKind.RegionTransit));
            galaxyDraftNotice=galaxyDraft.SetTarget(new GeneratedGalaxyDraftAnchor(target,kind),append)?null:"Route limit: "+galaxyDraft.MaximumTargets+" targets. Remove the last pin or replace the destination.";
            RecalculateGalaxyPlan();
        }
        private void RecalculateGalaxyPlan()
        {
            galaxyRouteStart=galaxyDraft?.Start?.NodeId;
            var document=galaxy!.Document!;
            galaxyPlan=galaxyRouteStart==null||galaxyDraft!.Targets.Count==0?null:galaxy.FindRoutePlan(galaxyDraft.Query(document,GalaxyFullMapFilter()));
            galaxyMap!.RoutePlan(galaxyRouteStart,galaxyPlan,galaxyDraft?.Targets??Array.Empty<GeneratedGalaxyDraftAnchor>());
            DrawGalaxyRoutePanel();
        }
        private GalaxyReviewRouteFilterDto GalaxyFullMapFilter()=>new GalaxyReviewRouteFilterDto{
            PermittedNodeIds=galaxy!.Document!.Regions.Select(r=>r.NodeId).ToList(),PermittedConnectionIds=galaxy.Document.Connections.Select(c=>c.ConnectionId).ToList()};
        private GeneratedGalaxyRouteDraftStore GalaxyDraftStore()=>galaxyDraftStore??(galaxyDraftStore=new GeneratedGalaxyRouteDraftStore(
            Path.Combine(UnityEngine.Application.persistentDataPath,GeneratedGalaxyRouteDraftStore.DirectoryName)));
        private void SaveGalaxyRouteDraft()
        {
            try
            {
                var store=GalaxyDraftStore();var slot=galaxy!.Slot;
                void Save(bool overwrite)
                {
                    try{store.Save(slot,galaxyDraft!,overwrite);galaxyDraftNotice="Saved route draft in slot "+slot+". Map export is separate.";}
                    catch(Exception error)when(error is IOException||error is UnauthorizedAccessException||error is ArgumentException||error is InvalidOperationException)
                    {galaxyDraftNotice="Draft save failed. Previous file and current route are kept.";}
                    DrawGalaxyRoutePanel();
                }
                if(store.Exists(slot))Confirm("Replace route draft in slot "+slot+"? The map and campaign saves are separate.",()=>Save(true));else Save(false);
            }
            catch(Exception error)when(error is IOException||error is UnauthorizedAccessException||error is ArgumentException)
            {galaxyDraftNotice="Draft storage is unavailable. Current route is kept.";DrawGalaxyRoutePanel();}
        }
        private void RestoreGalaxyRouteDraft()
        {
            try
            {
                var restored=GalaxyDraftStore().Load(galaxy!.Slot,galaxy,GalaxyFullMapFilter());
                galaxyDraft=restored.Draft;galaxyPlan=restored.Plan;galaxyRouteStart=galaxyDraft.Start!.NodeId;
                galaxyDraftNotice="Restored draft · slot "+galaxy.Slot+" · all legs revalidated.";
                galaxyMap!.RoutePlan(galaxyRouteStart,galaxyPlan,galaxyDraft.Targets);DrawGalaxyInspector();
            }
            catch(Exception error)when(error is IOException||error is UnauthorizedAccessException||error is ArgumentException||error is InvalidOperationException)
            {galaxyDraftNotice="Draft not restored: missing, incompatible, wrong map or unavailable route. Current route is kept.";}
            DrawGalaxyRoutePanel();
        }
        private void DrawGalaxyRoutePanel()
        {
            if(galaxyRoutePanel==null)return;var panel=galaxyRoutePanel;panel.Clear();
            panel.Add(Readout("ROUTE PREVIEW"));
            if(galaxyDraft?.Start==null)
            {
                panel.Add(Readout("Inspect a region, then choose Use as route start."));
                if(galaxy!.Document!=null)AddButton(panel,"Restore route draft",RestoreGalaxyRouteDraft).name="galaxy-route-restore";
                if(galaxyDraftNotice!=null)panel.Add(Readout(galaxyDraftNotice));return;
            }
            var start=Readout("Start · "+GalaxyNodeName(galaxyDraft.Start.NodeId));start.name="galaxy-route-origin-label";panel.Add(start);
            var list=new ScrollView(ScrollViewMode.Vertical){name="galaxy-route-target-list"};list.AddToClassList("galaxy-route-target-list");panel.Add(list);
            var pins=galaxyDraft.Targets;
            for(int i=0;i<pins.Count;i++)
            {
                int index=i;var pin=pins[i];var cost=galaxyPlan?.Success==true?" · "+galaxyPlan.Legs[i].Route.TotalCost+" cost / "+galaxyPlan.Legs[i].Route.Steps.Count+" crossings":"";
                var failure=galaxyPlan?.FailedLegIndex==i?" · "+galaxyPlan.ErrorCode:"";
                var button=AddButton(list,(i+1)+" · "+(i==pins.Count-1?"Destination":"Via")+" · "+GalaxyNodeName(pin.NodeId)+"\n"+
                    (pin.Kind==GeneratedGalaxyDraftAnchorKind.StarLocation?"Star location · no visit action":"Region transit")+cost+failure,
                    ()=>SelectGalaxyNode(pins[index].NodeId,pins[index].Kind==GeneratedGalaxyDraftAnchorKind.StarLocation?GeneratedGalaxySelectionKind.System:GeneratedGalaxySelectionKind.Region));
                button.name="galaxy-route-row-"+(i+1);
            }
            var total=Readout(galaxyPlan==null?"Right-click sets a destination. Shift + right-click appends.":galaxyPlan.Success?
                "Total cost "+galaxyPlan.TotalCost+" · "+galaxyPlan.Steps.Count+" crossings":"Total unavailable · "+galaxyPlan.ErrorCode);
            total.name="galaxy-route-cost";panel.Add(total);
            var actions=new VisualElement();actions.style.flexDirection=FlexDirection.Row;panel.Add(actions);
            AddButton(actions,"Remove last",()=>{galaxyDraft.RemoveLast();galaxyDraftNotice=null;RecalculateGalaxyPlan();}).SetEnabled(pins.Count>0);
            AddButton(actions,"Clear route",()=>{galaxyDraft.Clear();galaxyDraftNotice=null;RecalculateGalaxyPlan();}).SetEnabled(pins.Count>0);
            var files=new VisualElement();files.style.flexDirection=FlexDirection.Row;panel.Add(files);
            AddButton(files,"Save route draft",SaveGalaxyRouteDraft).name="galaxy-route-save";
            AddButton(files,"Restore route draft",RestoreGalaxyRouteDraft).name="galaxy-route-restore";
            if(galaxyDraftNotice!=null)panel.Add(Readout(galaxyDraftNotice));
            var note=Readout("Full-map draft only · no fleet order or visit.");note.AddToClassList("galaxy-inspector-id");panel.Add(note);
        }
        private void DrawGalaxyInspector()
        {
            var target=galaxyInspector!;target.Clear();var document=galaxy!.Document;
            void Text(string value,string? style=null,string? name=null){var label=Readout(value);if(style!=null)label.AddToClassList(style);if(name!=null)label.name=name;target.Add(label);}
            Text("MAP REVIEW","galaxy-inspector-heading");
            if(document==null){Text("Generate or open a map to inspect its geography.");return;}
            Text(document.Systems.Count+" systems · "+document.Regions.Count+" regions\n"+document.Regions.Count(r=>r.SystemId==null)+" empty regions",null,"galaxy-counts");
            Text("Seed "+document.Settings.Seed+"\nTwo starting candidates · no ownership");
            if(document.IsLarge)
            {
                var summary=document.ScaleSummary!;
                Text(document.Width.ToString("N0",CultureInfo.InvariantCulture)+" × "+document.Height.ToString("N0",CultureInfo.InvariantCulture)+" map units · format "+document.FormatVersion);
                Text("Quiet empty "+summary.QuietNodes.Count+" · disturbed empty "+summary.DisturbedNodes.Count+"\nQuiet/system mean area "+
                    (summary.QuietToSystemMeanArea?.ToString("F2",CultureInfo.InvariantCulture)??"n/a")+"×; target "+((document.Natural?.Settings.Map.QuietEmptyAreaTargetPermille??document.Scaled!.Settings.QuietEmptyAreaTargetPermille)/1000.0).ToString("F1",CultureInfo.InvariantCulture)+"×",null,"galaxy-area-summary");
                Text("Quiet components "+summary.QuietComponentCount+" · all-empty area "+summary.EmptyAreaShare.ToString("P1",CultureInfo.InvariantCulture)+". No galaxy-spanning corridor guarantee.");
                Text("Quiet/disturbed classifies sampled site pressure, not whole-cell coverage. The area target is soft.","galaxy-inspector-id");
            }
            Text(!document.HasEnvironment?"Retained geometry map · format 1":"Environment-first map · provisional influence tuning","galaxy-inspector-id","galaxy-map-version");
            if(galaxySelectedFeature!=null&&document.HasEnvironment)
            {
                var feature=document.Features.Single(f=>f.FeatureId==galaxySelectedFeature);
                Text(feature.Kind==GalaxyEnvironmentFeatureKind.BlackHole?"BLACK HOLE":feature.Kind==GalaxyEnvironmentFeatureKind.Nebula?"NEBULA":"STELLAR INFLUENCE","galaxy-inspector-heading","galaxy-feature-kind");
                Text(feature.DisplayName,null,"galaxy-feature-name");Text(feature.FeatureId,"galaxy-inspector-id","galaxy-feature-identity");
                Text("Generation influence weight "+feature.DensityWeight+" · ellipse radii "+feature.RadiusX+" × "+feature.RadiusY+" map units.");
                Text("Influence areas explain region generation; they are not physical extents or sensor coverage.","galaxy-inspector-id");
                if(feature.NodeId==null)Text("Spans navigation regions; no single route endpoint.");
            }
            if(galaxySelected==null)Text("Select a star, an empty region or a candidate bracket.");
            else
            {
                var region=document.Regions.Single(r=>r.NodeId==galaxySelected);
                Text(galaxySelectionKind==GeneratedGalaxySelectionKind.HomeCandidate?"STARTING CANDIDATE":
                    galaxySelectionKind==GeneratedGalaxySelectionKind.System?"STAR SYSTEM":"NAVIGATION REGION","galaxy-inspector-heading", "galaxy-selection-kind");
                Text(galaxySelectionKind==GeneratedGalaxySelectionKind.Region?region.DisplayName:GalaxyNodeName(region.NodeId),null,"galaxy-selection-name");
                Text(region.NodeId+(region.SystemId==null?"":"\n"+region.SystemId),"galaxy-inspector-id","galaxy-selection-identity");
                Text(region.SystemId==null?"Empty navigation region · no star system":"Contains "+GalaxyNodeName(region.NodeId)+".");
                if(document.ScaleSummary!=null&&region.SystemId==null)Text(document.ScaleSummary.QuietNodes.Contains(region.NodeId)?"Quiet empty site":"Disturbed empty site",null,"galaxy-quiet-class");
                if(document.HasEnvironment&&region.SystemId!=null)
                {
                    var star=document.Features.Single(f=>f.Kind==GalaxyEnvironmentFeatureKind.Star&&f.SystemId==region.SystemId);
                    var inspectStar=AddButton(target,"Inspect stellar influence",()=>SelectGalaxyFeature(star.FeatureId));inspectStar.name="galaxy-inspect-star-influence";
                }
                foreach(var home in document.HomeCandidates.Where(h=>h.NodeId==region.NodeId))
                {
                    Text(galaxy.CivilizationNames[home.CivilizationDefinitionId]+" starting candidate · unoccupied.");
                    var inspect=AddButton(target,"Inspect starting candidate",()=>SelectGalaxyNode(region.NodeId,GeneratedGalaxySelectionKind.HomeCandidate));
                    inspect.name="galaxy-inspect-candidate-"+home.CivilizationDefinitionId;
                }
                Text("Bodies and resources are not generated in this milestone.","galaxy-inspector-id");
                if(galaxyDensityCauses&&document.HasEnvironment)
                {
                    var influence=document.RegionInfluences.Single(i=>i.NodeId==region.NodeId);
                    Text("DENSITY CAUSES","galaxy-inspector-heading");
                    Text("Base "+influence.BaseDensity+" → sampled target "+influence.TargetDensity+" / cap "+document.MaximumDensity,null,"galaxy-density-values");
                    Text("Placement pressure sampled at this site; not an exact polygon-area formula. Provisional tuning.","galaxy-inspector-id");
                    foreach(var contribution in influence.Contributions)
                    {
                        var feature=document.Features.Single(f=>f.FeatureId==contribution.FeatureId);
                        var detail=AddButton(target,feature.DisplayName+" +"+contribution.Amount,()=>SelectGalaxyFeature(feature.FeatureId));detail.name="galaxy-contribution-"+feature.FeatureId;
                    }
                }
                var begin=AddButton(target,"Use as route start",()=>{galaxyDraft!.SetStart(new GeneratedGalaxyDraftAnchor(region.NodeId,galaxySelectionKind==GeneratedGalaxySelectionKind.System?GeneratedGalaxyDraftAnchorKind.StarLocation:GeneratedGalaxyDraftAnchorKind.RegionTransit));RecalculateGalaxyPlan();DrawGalaxyInspector();});
                begin.name="galaxy-route-start";
                var end=AddButton(target,"Preview route here",()=>PreviewGalaxyRoute(region.NodeId));end.name="galaxy-route-target";end.SetEnabled(galaxyRouteStart!=null);
                Text("CONNECTED REGIONS","galaxy-inspector-heading");
                foreach(var edge in document.Connections.Where(c=>c.EndpointA==region.NodeId||c.EndpointB==region.NodeId))
                {
                    var node=edge.EndpointA==region.NodeId?edge.EndpointB:edge.EndpointA;
                    var button=AddButton(target,GalaxyNodeName(node)+" · cost "+edge.BaseTraversalCost,()=>SelectGalaxyNode(node,GeneratedGalaxySelectionKind.Region));button.name="galaxy-neighbor-"+node;
                }
            }
            Text("INSPECT ANY REGION","galaxy-inspector-heading");
            var regions=document.Regions.OrderBy(r=>r.NodeId,StringComparer.Ordinal).ToList();
            var regionChoice=new DropdownField(regions.Select(r=>r.DisplayName+(r.SystemId==null?" · empty":" · system")).ToList(),Math.Max(0,regions.FindIndex(r=>r.NodeId==galaxySelected))){name="galaxy-region-picker"};
            target.Add(regionChoice);var epoch=viewEpoch;
            regionChoice.RegisterValueChangedCallback(_=>{if(epoch==viewEpoch&&regionChoice.panel!=null)SelectGalaxyNode(regions[regionChoice.index].NodeId,GeneratedGalaxySelectionKind.Region);});
            if(document.HasEnvironment)
            {
                Text("INSPECT ANY FEATURE","galaxy-inspector-heading");
                var features=document.Features.OrderBy(f=>f.FeatureId,StringComparer.Ordinal).ToList();
                var featureChoice=new DropdownField(features.Select(f=>f.DisplayName).ToList(),Math.Max(0,features.FindIndex(f=>f.FeatureId==galaxySelectedFeature))){name="galaxy-feature-picker"};
                target.Add(featureChoice);
                featureChoice.RegisterValueChangedCallback(_=>{if(epoch==viewEpoch&&featureChoice.panel!=null)SelectGalaxyFeature(features[featureChoice.index].FeatureId);});
                var inspect=AddButton(target,"Inspect chosen feature",()=>SelectGalaxyFeature(features[featureChoice.index].FeatureId));inspect.name="galaxy-inspect-feature";
            }
            Text("MAP FILES","galaxy-inspector-heading");Text(galaxy.Location,"galaxy-inspector-id");
        }
    }
}
