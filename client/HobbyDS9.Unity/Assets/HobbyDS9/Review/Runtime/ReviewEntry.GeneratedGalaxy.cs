#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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
        private string? galaxySelected,galaxyRouteStart,galaxyRouteTarget;
        private GeneratedGalaxySelectionKind galaxySelectionKind;
        private StrategicRouteSnapshot? galaxyRoute;

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
                        new GeneratedGalaxyMapStore(Path.Combine(UnityEngine.Application.persistentDataPath,GeneratedGalaxyMapStore.DirectoryName)));
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
            var seed=new TextField("Seed"){name="galaxy-seed",value=galaxy!.Seed,maxLength=20};controls.Add(seed);
            seed.RegisterValueChangedCallback(e=>{if(epoch==viewEpoch)galaxy.Seed=e.newValue;});
            var size=new DropdownField("Systems",new List<string>{"30","40","50"},galaxy.Size.ToString(CultureInfo.InvariantCulture)){name="galaxy-size"};controls.Add(size);
            size.RegisterValueChangedCallback(e=>{if(epoch==viewEpoch)galaxy.Size=int.Parse(e.newValue,CultureInfo.InvariantCulture);});
            var generate=AddButton(controls,"Generate",()=>{
                void Run(){if(galaxy.Generate())GalaxyReplaced();else DrawGeneratedGalaxyStatus();}
                if(galaxy.Unexported)Confirm("Replace this unexported map? Export it first to keep it. A failed generation keeps the current map.",Run);else Run();
            });generate.name="galaxy-generate";generate.AddToClassList("primary-action");
            var slot=new DropdownField("Map slot",new List<string>{"1","2","3"},galaxy.Slot.ToString(CultureInfo.InvariantCulture)){name="galaxy-slot"};controls.Add(slot);
            slot.RegisterValueChangedCallback(e=>{if(epoch==viewEpoch)galaxy.Slot=int.Parse(e.newValue,CultureInfo.InvariantCulture);});
            var export=AddButton(controls,"Export",()=>{
                void Run(bool overwrite){galaxy.Export(overwrite);DrawGeneratedGalaxyStatus();}
                if(galaxy.SlotOccupied())Confirm("Replace the exported map in slot "+galaxy.Slot+"? Campaign saves are separate.",()=>Run(true));else Run(false);
            });export.name="galaxy-export";export.SetEnabled(galaxy.Document!=null);
            var open=AddButton(controls,"Open",()=>{
                void Run(){if(galaxy.Open())GalaxyReplaced();else DrawGeneratedGalaxyStatus();}
                if(galaxy.Unexported)Confirm("Open map slot "+galaxy.Slot+" and replace this unexported map? Failed validation keeps the current map.",Run);else Run();
            });open.name="galaxy-open";
            galaxyNotice=Readout(galaxy.Notice);galaxyNotice.name="galaxy-status";galaxyNotice.AddToClassList("galaxy-status");galaxyHost.Add(galaxyNotice);
            var body=new VisualElement();body.AddToClassList("galaxy-body");galaxyHost.Add(body);
            var column=new VisualElement();column.AddToClassList("galaxy-map-column");body.Add(column);
            var document=galaxy.Document;
            if(document==null)
            {
                var empty=new VisualElement{name="galaxy-empty"};empty.AddToClassList("galaxy-map");column.Add(empty);
                var instruction=Readout("Choose a seed to create your first map.\n\n30, 40 or 50 systems, with empty navigation regions and two candidate starting locations.");
                instruction.style.marginLeft=28;instruction.style.marginTop=28;instruction.style.maxWidth=500;empty.Add(instruction);galaxyMap=null;
            }
            else
            {
                galaxyMap=new GeneratedGalaxyMapView(document,galaxy.CivilizationNames,(b,a)=>BindButton(b,a),SelectGalaxyNode,PreviewGalaxyRoute);column.Add(galaxyMap);
                if(galaxySelected!=null)galaxyMap.Select(galaxySelected,galaxySelectionKind);
                galaxyMap.Route(galaxyRouteStart,galaxyRoute?.Steps);
                var tools=new VisualElement();tools.AddToClassList("galaxy-map-tools");column.Add(tools);
                AddGalaxyTool(tools,"Fit","galaxy-fit",()=>galaxyMap.Fit());
                AddGalaxyTool(tools,"−","galaxy-zoom-out",()=>galaxyMap.ZoomBy(1/1.3));
                AddGalaxyTool(tools,"+","galaxy-zoom-in",()=>galaxyMap.ZoomBy(1.3));
                AddGalaxyTool(tools,"←","galaxy-pan-left",()=>galaxyMap.Pan(90,0));
                AddGalaxyTool(tools,"→","galaxy-pan-right",()=>galaxyMap.Pan(-90,0));
                AddGalaxyTool(tools,"↑","galaxy-pan-up",()=>galaxyMap.Pan(0,90));
                AddGalaxyTool(tools,"↓","galaxy-pan-down",()=>galaxyMap.Pan(0,-90));
            }
            var hint=Readout("Drag empty space to pan · wheel to zoom · star or region to inspect · right-click for a route preview");
            hint.AddToClassList("galaxy-hint");column.Add(hint);
            galaxyInspector=new ScrollView(ScrollViewMode.Vertical){name="galaxy-inspector"};galaxyInspector.AddToClassList("galaxy-inspector");body.Add(galaxyInspector);
            DrawGalaxyInspector();DrawGeneratedGalaxyStatus();
        }
        private void AddGalaxyTool(VisualElement parent,string label,string name,Action action)
        {var button=AddButton(parent,label,action);button.name=name;}
        private void GalaxyResponsive(float width)
        {
            galaxyHost?.EnableInClassList("galaxy-compact",width<1600);
            galaxyHost?.EnableInClassList("galaxy-wide",width>=2800);
        }
        private void GalaxyReplaced()
        {
            galaxySelected=null;galaxyRouteStart=null;galaxyRouteTarget=null;galaxyRoute=null;viewEpoch++;Build();
        }
        private void DrawGeneratedGalaxyStatus(){if(galaxyNotice!=null)galaxyNotice.text=galaxy!.Notice;}
        private void SelectGalaxyNode(string node,GeneratedGalaxySelectionKind kind)
        {
            galaxySelected=node;galaxySelectionKind=kind;galaxyMap!.Select(node,kind);DrawGalaxyInspector();
        }
        private string GalaxyNodeName(string node)
        {
            var document=galaxy!.Document!;var region=document.Regions.Single(r=>r.NodeId==node);
            return region.SystemId==null?region.DisplayName:document.Systems.Single(s=>s.SystemId==region.SystemId).DisplayName;
        }
        private void PreviewGalaxyRoute(string target)
        {
            if(galaxyRouteStart==null)
            {
                galaxyRouteStart=galaxySelected??target;galaxyRouteTarget=null;galaxyRoute=null;
                if(galaxySelected==null){SelectGalaxyNode(target,GeneratedGalaxySelectionKind.Region);galaxyMap!.Route(target,null);return;}
            }
            galaxyRouteTarget=target;galaxyRoute=galaxy!.FindRoute(galaxyRouteStart,target);
            galaxyMap!.Route(galaxyRouteStart,galaxyRoute.Success?galaxyRoute.Steps:null);DrawGalaxyInspector();
        }
        private void DrawGalaxyInspector()
        {
            var target=galaxyInspector!;target.Clear();var document=galaxy!.Document;
            void Text(string value,string? style=null,string? name=null){var label=Readout(value);if(style!=null)label.AddToClassList(style);if(name!=null)label.name=name;target.Add(label);}
            Text("MAP REVIEW","galaxy-inspector-heading");
            if(document==null){Text("Generate or open a map to inspect its geography.");return;}
            Text(document.Systems.Count+" systems · "+document.Regions.Count+" regions\n"+document.Regions.Count(r=>r.SystemId==null)+" empty regions",null,"galaxy-counts");
            Text("Seed "+document.Settings.Seed+"\nTwo starting candidates · no ownership");
            if(galaxySelected==null)Text("Select a star, an empty region or a candidate bracket.");
            else
            {
                var region=document.Regions.Single(r=>r.NodeId==galaxySelected);
                Text(galaxySelectionKind==GeneratedGalaxySelectionKind.HomeCandidate?"STARTING CANDIDATE":
                    galaxySelectionKind==GeneratedGalaxySelectionKind.System?"STAR SYSTEM":"NAVIGATION REGION","galaxy-inspector-heading", "galaxy-selection-kind");
                Text(galaxySelectionKind==GeneratedGalaxySelectionKind.Region?region.DisplayName:GalaxyNodeName(region.NodeId),null,"galaxy-selection-name");
                Text(region.NodeId+(region.SystemId==null?"":"\n"+region.SystemId),"galaxy-inspector-id","galaxy-selection-identity");
                Text(region.SystemId==null?"Empty navigation region · no star system":"Contains "+GalaxyNodeName(region.NodeId)+".");
                foreach(var home in document.HomeCandidates.Where(h=>h.NodeId==region.NodeId))
                    Text(galaxy.CivilizationNames[home.CivilizationDefinitionId]+" starting candidate · unoccupied.");
                Text("Bodies and resources are not generated in this milestone.","galaxy-inspector-id");
                var begin=AddButton(target,"Use as route start",()=>{galaxyRouteStart=region.NodeId;galaxyRouteTarget=null;galaxyRoute=null;galaxyMap!.Route(galaxyRouteStart,null);DrawGalaxyInspector();});
                begin.name="galaxy-route-start";
                var end=AddButton(target,"Preview route here",()=>PreviewGalaxyRoute(region.NodeId));end.name="galaxy-route-target";end.SetEnabled(galaxyRouteStart!=null);
                Text("CONNECTED REGIONS","galaxy-inspector-heading");
                foreach(var edge in document.Connections.Where(c=>c.EndpointA==region.NodeId||c.EndpointB==region.NodeId))
                {
                    var node=edge.EndpointA==region.NodeId?edge.EndpointB:edge.EndpointA;
                    var button=AddButton(target,GalaxyNodeName(node)+" · cost "+edge.BaseTraversalCost,()=>SelectGalaxyNode(node,GeneratedGalaxySelectionKind.Region));button.name="galaxy-neighbor-"+node;
                }
            }
            Text("ROUTE PREVIEW","galaxy-inspector-heading");
            Text(galaxyRouteStart==null?"Choose a starting region.":"From "+GalaxyNodeName(galaxyRouteStart),null,"galaxy-route-origin-label");
            if(galaxyRouteTarget!=null)Text("To "+GalaxyNodeName(galaxyRouteTarget),null,"galaxy-route-target-label");
            if(galaxyRoute!=null)
            {
                Text(galaxyRoute.Success?"Graph cost "+galaxyRoute.TotalCost+" · "+galaxyRoute.Steps.Count+" crossings":"No route is available.",null,"galaxy-route-cost");
                if(galaxyRoute.Success)Text(string.Join(" → ",new[]{galaxyRouteStart!}.Concat(galaxyRoute.Steps).Select(GalaxyNodeName)),null,"galaxy-route-path");
            }
            else if(galaxyRouteStart!=null)Text("Right-click a star or region to choose the destination.");
            Text("Preview only · no fleet order or travel time.","galaxy-inspector-id");
            Text("INSPECT ANY REGION","galaxy-inspector-heading");
            var regions=document.Regions.OrderBy(r=>r.NodeId,StringComparer.Ordinal).ToList();
            var regionChoice=new DropdownField(regions.Select(r=>r.DisplayName+(r.SystemId==null?" · empty":" · system")).ToList(),Math.Max(0,regions.FindIndex(r=>r.NodeId==galaxySelected))){name="galaxy-region-picker"};
            target.Add(regionChoice);var epoch=viewEpoch;
            regionChoice.RegisterValueChangedCallback(_=>{if(epoch==viewEpoch&&regionChoice.panel!=null)SelectGalaxyNode(regions[regionChoice.index].NodeId,GeneratedGalaxySelectionKind.Region);});
            Text("MAP FILES","galaxy-inspector-heading");Text(galaxy.Location,"galaxy-inspector-id");
        }
    }
}
