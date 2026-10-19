#nullable enable
using System;
using System.Linq;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using UnityEngine;
using UnityEngine.UIElements;
namespace HobbyDS9.UnityReview
{
    // Live player panels: all facts, prices/progress/eligibility from the scoped session.
    public sealed class ReviewPlayerView
    {
        private readonly ReviewUiModel model;private readonly ReviewWorkspace workspace;
        private readonly Func<VisualElement,string,Action,Button> button;
        private readonly Action<Action> perform;private readonly Action<string,Action> confirm;
        private readonly Action<ReviewWorkspacePage> navigate;private readonly Action<string,string?,string?> selectFleet;
        private string? colony {get=>workspace.SelectedColony;set=>workspace.SelectedColony=value;}
        private KnownRouteOffer? route {get=>workspace.RoutePreview;set=>workspace.RoutePreview=value;}
        public ReviewPlayerView(ReviewUiModel model,ReviewWorkspace workspace,Func<VisualElement,string,Action,Button> button,Action<Action> perform,Action<string,Action> confirm,Action<ReviewWorkspacePage> navigate,Action<string,string?,string?> selectFleet)
        {this.model=model;this.workspace=workspace;this.button=button;this.perform=perform;this.confirm=confirm;this.navigate=navigate;this.selectFleet=selectFleet;}
        private bool Editable=>model.Current.Frame.Status.Phase==SessionPhase.Planning&&(model.Current.Frame.Demo==null||model.Current.Frame.Demo.Outcome==CampaignDemoOutcome.Active)&&!model.Current.HasPendingWork&&!model.Current.RefreshRequired&&!model.Current.Dirty&&!model.Busy&&!model.Closed;
        private static Label Text(VisualElement p,string text,string cls="console-copy",string? id=null)
        {var l=new Label(text){name=id??""};l.AddToClassList(cls);p.Add(l);return l;}
        private Button Btn(VisualElement p,string text,string id,Action action,bool enabled=true,bool primary=false)
        {var b=button(p,text,action);b.name=id;b.SetEnabled(enabled);if(primary)b.AddToClassList("primary-action");return b;}
        private static VisualElement Row(VisualElement p,string cls="review-field-row"){var r=new VisualElement();r.AddToClassList(cls);p.Add(r);return r;}
        private static ScrollView Panel(VisualElement p,string heading,string cls="console-work")
        {var s=new ScrollView(ScrollViewMode.Vertical);s.AddToClassList("console-panel");s.AddToClassList(cls);p.Add(s);Text(s,heading,"console-section");return s;}
        public void Render(VisualElement root,ReviewWorkspacePage page)
        {
            root.Clear();var facts=model.Current.Frame.Strategy!;var columns=Row(root,"console-columns");
            if(page==ReviewWorkspacePage.Research)Research(columns,facts);
            else if(page==ReviewWorkspacePage.Worlds)Worlds(columns,facts);
            else if(page==ReviewWorkspacePage.Production)Production(columns,facts);
            else if(page==ReviewWorkspacePage.FleetOrders)Fleets(columns,facts);
            else Map(columns,facts);
        }
        private void Research(VisualElement root,CampaignIntegratedStrategySnapshot facts)
        {
            var main=Panel(root,"Science & Engineering");var aside=Panel(root,"R&D direction","console-aside");
            var tabs=Row(main,"console-tabs");
            foreach(var tab in new[]{"science","engineering"}){var b=Btn(tabs,tab=="science"?"Science":"Engineering","research-tab-"+tab,()=>perform(()=>workspace.ResearchTab=tab));b.EnableInClassList("selected-tab",workspace.ResearchTab==tab);}
            var r=facts.Research;var d=model.ResearchInputs()!;bool edit=Editable&&d.Current(model.Current.Frame);
            if(workspace.ResearchTab=="science")
            {
                if(!workspace.Compact)Text(main,"Invest in discovery","console-intro");
                var grid=Row(main,"science-grid");grid.name="six-science-cards";
                foreach(var card in r.Science)
                {
                    var c=new VisualElement{name="science-card-"+card.Discipline};c.AddToClassList("science-card");grid.Add(c);
                    if(workspace.Compact)
                    {
                        c.AddToClassList("science-compact-row");var name=new VisualElement();name.style.width=145;c.Add(name);Text(name,card.Discipline.ToString(),"console-section");Text(name,"Level "+card.Level+" · "+card.ProgressPercent.ToString("0.##")+"%");
                        var share=Text(c,(100m*d.Weight(card.Discipline)/600).ToString("0.##")+"%","console-metric");share.style.width=75;
                        Btn(c,"−","science-minus-"+card.Discipline,()=>perform(()=>d.Adjust(card.Discipline,-30)),edit&&d.Weight(card.Discipline)>0);Btn(c,"+","science-plus-"+card.Discipline,()=>perform(()=>d.Adjust(card.Discipline,30)),edit&&d.Weight(card.Discipline)<600);
                        Btn(c,"Details","science-details-"+card.Discipline,()=>perform(()=>workspace.ScienceDetails=workspace.ScienceDetails==card.Discipline?null:card.Discipline));
                        continue;
                    }
                    Text(c,card.Discipline.ToString(),"console-section");Text(c,"Level "+card.Level+" / "+card.MaxLevel);
                    c.Add(new ProgressBar{value=(float)card.ProgressPercent});
                    Text(c,card.ProgressPercent.ToString("0.##")+"% to next level");
                    Text(c,(100m*d.Weight(card.Discipline)/ResearchUnits.Scale).ToString("0.##")+"% Science share","console-metric");
                    var controls=Row(c);
                    Btn(controls,"− 5%","science-minus-"+card.Discipline,()=>perform(()=>d.Adjust(card.Discipline,-30)),edit&&d.Weight(card.Discipline)>0);
                    Btn(controls,"+ 5%","science-plus-"+card.Discipline,()=>perform(()=>d.Adjust(card.Discipline,30)),edit&&d.Weight(card.Discipline)<600);
                    var details=new Foldout{text="Breakthrough details",value=false};c.Add(details);
                    foreach(var point in card.Breakpoints)Text(details,(point.Achieved?"✓ ":"○ ")+"Breakthrough "+point.Ordinal+" · threshold "+Units(point.Threshold));
                    if(card.AllocatedWhileMaxed)Text(details,"This share stays in reserve at maximum level. Reallocate explicitly to use it elsewhere.");
                }
                Btn(main,"Equalize Science","integrated-equalize-science",()=>perform(()=>d.Equalize()),edit);
                var receipts=new Foldout{text="Latest-turn research",value=false};main.Add(receipts);
                foreach(var receipt in r.Receipts)Text(receipts,receipt.Category+" · "+Units(receipt.Amount)+(receipt.Discipline.HasValue?" · "+receipt.Discipline:""));
            }
            else
            {
                Text(main,"Develop capabilities","console-intro");
                foreach(var project in r.Engineering)
                {
                    var card=new VisualElement{name="engineering-project-"+project.Definition.DefinitionId};card.AddToClassList("console-queue");main.Add(card);
                    Text(card,project.Definition.DisplayName,"console-section");Text(card,project.IsTarget?"Current project":Reason(project.UnavailableReason));
                    card.Add(new ProgressBar{value=project.MaturityPercent});Text(card,project.MaturityPercent+"% · "+project.Band,"console-metric");
                    Text(card,"Cost: "+Units(project.Cost)+" research points");Text(card,project.Definition.EffectSummary);
                    Text(card,"Construction in this preview supports "+string.Join(", ",facts.Construction.Select(o=>ShipName(o.Design)).Distinct())+". Additional class unlocks are not available here yet.");
                    var requirements=new Foldout{text="Requirements & progress",value=false};card.Add(requirements);
                    for(int i=0;i<project.Definition.ScienceRequirements.Count;i++){var q=project.Definition.ScienceRequirements[i];Text(requirements,(project.ScienceSatisfied[i]?"✓ ":"○ ")+q.Discipline+" interval "+q.IntervalStartLevel+" breakthrough "+q.BreakthroughOrdinal);}
                    Btn(card,"Develop project","integrated-develop-"+project.Definition.DefinitionId,()=>confirm("Develop "+project.Definition.DisplayName+"? Earned progress is retained when you change targets. Work progresses on End turn.",()=>perform(()=>model.Engineering(project.Definition.DefinitionId.Value,ReviewUiModel.NewCommandId()))),edit&&project.CanTarget,true);
                }
                Btn(main,"Clear Engineering target","integrated-clear-engineering",()=>confirm("Clear the current Engineering target? Earned progress will remain.",()=>perform(()=>model.Engineering(null,ReviewUiModel.NewCommandId()))),edit&&r.Target.HasValue);
            }
            if(workspace.ScienceDetails.HasValue&&workspace.ResearchTab=="science")
            {
                var detail=r.Science.Single(c=>c.Discipline==workspace.ScienceDetails.Value);
                Text(aside,detail.Discipline+" breakthroughs","console-section");
                foreach(var point in detail.Breakpoints)Text(aside,(point.Achieved?"✓ ":"○ ")+"Breakthrough "+point.Ordinal+" · threshold "+Units(point.Threshold));
                Btn(aside,"Close details","science-close-details",()=>perform(()=>workspace.ScienceDetails=null));
            }
            Text(aside,"R&D capacity","console-copy");Text(aside,r.ResearchOutput.ToString(),"console-metric");
            Text(aside,"Science / Engineering");
            var split=new SliderInt(0,20){name="integrated-research-split",value=d.ScienceUnits/30};split.SetEnabled(edit);aside.Add(split);
            Text(aside,(100m*d.ScienceUnits/600).ToString("0.##")+"% / "+(100m*(600-d.ScienceUnits)/600).ToString("0.##")+"%","console-metric");
            split.RegisterValueChangedCallback(e=>perform(()=>d.ScienceUnits=e.newValue*30));
            Btn(aside,"Apply R&D split","integrated-apply-split",()=>confirm("Apply "+(100m*d.ScienceUnits/600).ToString("0.##")+"% Science and "+(100m*(600-d.ScienceUnits)/600).ToString("0.##")+"% Engineering? Other Science-share edits remain unsent.",()=>perform(()=>model.AllocateResearch(d.ScienceUnits,ReviewUiModel.NewCommandId()))),edit&&d.SplitChanged);
            Text(aside,"Science reserve: "+r.ScienceReserveConceptual);Text(aside,"Engineering reserve: "+r.EngineeringReserveConceptual);
            Text(aside,"One Engineering project at a time. Progress and eligibility are checked by the campaign.");
            Btn(aside,workspace.ResearchTab=="science"?"Open Engineering":"Open Science","open-other-research",()=>perform(()=>workspace.ResearchTab=workspace.ResearchTab=="science"?"engineering":"science"));
            if(!edit)Text(aside,"Research changes are unavailable until combat is complete or your view is refreshed.");
        }
        private static string ShipName(ShipDesignSnapshot d)=>d.DefinitionId.Value=="ship-design.source-cruiser"?"Cruiser":d.DisplayName;
        private static string Resource(string id)=>id=="resource.dilithium"?"Dilithium":id.Substring(id.LastIndexOf('.')+1).Replace('-',' ');
        private void Production(VisualElement root,CampaignIntegratedStrategySnapshot facts)
        {
            var main=Panel(root,"Production queue");var aside=Panel(root,"Build a ship","console-aside");
            var eligible=facts.Construction.FirstOrDefault(o=>o.CanEnqueue)?.ColonyId.Value;
            if(colony==null||!facts.Colonies.Any(c=>c.ColonyId.Value==colony))colony=facts.Colonies.FirstOrDefault(c=>c.ProductionQueue.Count>0)?.ColonyId.Value??eligible??facts.Colonies.FirstOrDefault()?.ColonyId.Value;
            var selection=Row(main);
            foreach(var world in facts.Colonies)Btn(selection,world.Environment.DisplayName+(world.ProductionQueue.Count>0?" · queue "+world.ProductionQueue.Count:"")+(world.ColonyId.Value==colony?" · selected":""),"production-colony-"+world.ColonyId,()=>perform(()=>colony=world.ColonyId.Value));
            var selected=facts.Colonies.FirstOrDefault(c=>c.ColonyId.Value==colony);
            if(selected==null){Text(main,"You have no owned shipyards.");return;}
            Text(main,selected.Environment.DisplayName+" · Industry "+selected.Economy.Industry,"console-intro");
            if(selected.ProductionQueue.Count==0)Text(main,"Queue is empty. Choose an available ship to start construction.");
            foreach(var item in selected.ProductionQueue)
            {
                var card=new VisualElement{name="production-queue-"+item.QueueItemId};card.AddToClassList("console-queue");main.Add(card);
                var design=facts.Construction.FirstOrDefault(o=>o.Design.DefinitionId==item.ShipDesignDefinitionId)?.Design;
                Text(card,design==null?item.ShipDesignDefinitionId.Value:ShipName(design),"console-section");
                Text(card,"Industry "+item.CompletedIndustry+" / "+item.RequiredIndustry,"console-metric");
                card.Add(new ProgressBar{value=item.RequiredIndustry==0?0:100f*item.CompletedIndustry/item.RequiredIndustry});
                Text(card,"Already paid: "+item.PaidCredits+" credits · "+string.Join(", ",item.PaidResources.Select(r=>r.Amount+" "+Resource(r.ResourceDefinitionId))));
                Btn(card,"Cancel construction","integrated-cancel-"+item.QueueItemId,()=>confirm("Cancel this construction at "+selected.Environment.DisplayName+"? Unused credits/resources are refunded by the campaign; completed Industry is lost.",()=>perform(()=>model.CancelProduction(selected.ColonyId.Value,item.QueueItemId.Value,ReviewUiModel.NewCommandId()))),Editable);
            }
            Text(main,"Completed ships join a stationary fleet or form a new fleet. Find them under Fleets when the queue finishes.");
            Btn(main,"Open Fleets","production-open-fleets",()=>navigate(ReviewWorkspacePage.FleetOrders));
            Text(aside,"Available credits","console-copy");Text(aside,facts.Civilization.Credits.ToString(),"console-metric");
            foreach(var option in facts.Construction.Where(o=>o.ColonyId==selected.ColonyId))
            {
                var d=option.Design;Text(aside,ShipName(d),"console-section");
                Text(aside,d.CreditCost+" credits + "+string.Join(", ",d.ResourceCosts.Select(r=>r.Amount+" "+Resource(r.ResourceDefinitionId))),"console-metric");
                Text(aside,d.IndustryCost+" Industry · progresses on End turn");Text(aside,Reason(option.UnavailableReason));
                Btn(aside,"Build "+ShipName(d),"integrated-queue-"+d.DefinitionId,()=>confirm("Build "+ShipName(d)+" at "+selected.Environment.DisplayName+"?\nCost: "+d.CreditCost+" credits + "+string.Join(", ",d.ResourceCosts.Select(r=>r.Amount+" "+Resource(r.ResourceDefinitionId)))+".\nWork: "+d.IndustryCost+" Industry. Current credits: "+facts.Civilization.Credits+".\nConstruction progresses when you end a turn; it does not create an instant ship.",()=>perform(()=>model.Enqueue(selected.ColonyId.Value,d.DefinitionId.Value,ReviewUiModel.NewCommandId()))),Editable&&option.CanEnqueue,true);
                var details=new Foldout{text="Template & equipment details",value=false};aside.Add(details);Text(details,d.DisplayName);Text(details,"Equipment "+option.FitId+" revision "+option.FitRevision);
            }
        }
        private void Map(VisualElement root,CampaignIntegratedStrategySnapshot facts)
        {
            var main=Panel(root,"Strategic operations");var aside=Panel(root,"Selection / context","console-aside");
            var map=new VisualElement{name="integrated-known-map"};map.style.flexGrow=1;map.style.minHeight=workspace.Compact?320:500;main.Add(map);
            var sky=new Image{name="map-space-backdrop",image=Resources.Load<Texture2D>("PlayerArt/space-backdrop"),scaleMode=ScaleMode.ScaleAndCrop,pickingMode=PickingMode.Ignore};sky.StretchToParentSize();map.Add(sky);
            var lines=new VisualElement{pickingMode=PickingMode.Ignore};lines.StretchToParentSize();map.Add(lines);
            var nodes=facts.Navigation.Nodes.ToList();
            Vector2 Point(string id){var i=nodes.FindIndex(n=>n.NodeId.Value==id);var angle=2*Mathf.PI*i/Math.Max(1,nodes.Count)-Mathf.PI/2;return new Vector2(map.contentRect.width*.5f+Mathf.Cos(angle)*map.contentRect.width*.32f,map.contentRect.height*.5f+Mathf.Sin(angle)*map.contentRect.height*.34f);}
            lines.generateVisualContent+=ctx=>{var p=ctx.painter2D;foreach(var link in facts.Navigation.Links){p.strokeColor=link.Kind==GalaxyEdgeKind.Wormhole?new Color(.72f,.64f,.9f):new Color(.61f,.76f,.87f);p.lineWidth=3;p.BeginPath();p.MoveTo(Point(link.EndpointA.Value));p.LineTo(Point(link.EndpointB.Value));p.Stroke();}};
            foreach(var node in nodes)
            {
                var marker=Btn(map,node.DisplayName,"map-node-"+node.NodeId,()=>perform(()=>workspace.SelectedNode=node.NodeId.Value));marker.style.position=Position.Absolute;marker.style.width=178;marker.style.fontSize=16;marker.style.whiteSpace=WhiteSpace.Normal;
                map.RegisterCallback<GeometryChangedEvent>(_=>{var p=Point(node.NodeId.Value);marker.style.left=p.x-89;marker.style.top=p.y-22;map.MarkDirtyRepaint();});
            }
            Text(main,"Known navigation · arrangement is schematic. Select your fleet, then a destination to preview its route.");
            foreach(var fleet in facts.Fleets){var b=Btn(aside,"Your fleet · "+fleet.ShipIds.Count+" ship(s)\n"+NodeName(facts,fleet.LocationNodeId.Value),"map-fleet-orders-"+fleet.FleetId,()=>{workspace.SelectedFleet=fleet.FleetId.Value;selectFleet(fleet.FleetId.Value,workspace.SelectedNode,null);});b.style.whiteSpace=WhiteSpace.Normal;}
            var selected=nodes.FirstOrDefault(n=>n.NodeId.Value==workspace.SelectedNode);Text(aside,selected?.DisplayName??"Select a sector","console-intro");
            foreach(var world in facts.Colonies.Where(c=>selected==null||selected.AssociatedSystemId==c.SystemId))
            {Text(aside,world.Environment.DisplayName,"console-section");Text(aside,"Population "+world.Population+" / "+world.PopulationCapacity);Text(aside,"Food "+world.Food+" · Industry "+world.Economy.Industry+" · R&D "+world.Economy.ResearchOutput);Btn(aside,"Inspect world","world-inspect-"+world.ColonyId,()=>{colony=world.ColonyId.Value;navigate(ReviewWorkspacePage.Worlds);});Btn(aside,"Manage production","world-production-"+world.ColonyId,()=>{colony=world.ColonyId.Value;navigate(ReviewWorkspacePage.Production);});}
            if(facts.Status.Phase!=SessionPhase.Planning)Btn(aside,"Open pending battle","strategy-open-combat",()=>navigate(ReviewWorkspacePage.Combat),true,true);
        }
        private void Fleets(VisualElement root,CampaignIntegratedStrategySnapshot facts)
        {
            var main=Panel(root,"Your fleets & ships");var aside=Panel(root,"Plot a course","console-aside");
            if(workspace.SelectedFleet==null||!facts.Fleets.Any(f=>f.FleetId.Value==workspace.SelectedFleet))workspace.SelectedFleet=facts.Fleets.FirstOrDefault()?.FleetId.Value;
            foreach(var f in facts.Fleets)Btn(main,"Fleet · "+NodeName(facts,f.LocationNodeId.Value)+" · "+f.ShipIds.Count+" ships"+(f.FleetId.Value==workspace.SelectedFleet?" · selected":""),"fleet-select-"+f.FleetId,()=>perform(()=>{workspace.SelectedFleet=f.FleetId.Value;route=null;}));
            var fleet=facts.Fleets.FirstOrDefault(f=>f.FleetId.Value==workspace.SelectedFleet);
            if(fleet==null){Text(main,"No owned surviving fleet can receive orders. You may build a supported ship if you have an eligible shipyard.");return;}
            var ships=facts.Ships.Where(s=>s.FleetId==fleet.FleetId).ToList();
            if(!ships.Any(s=>s.ShipId.Value==workspace.SelectedOwnShip))workspace.SelectedOwnShip=ships.FirstOrDefault()?.ShipId.Value;
            foreach(var own in ships)Btn(main,"Inspect ship "+(ships.IndexOf(own)+1)+(own.ShipId.Value==workspace.SelectedOwnShip?" · selected":""),"own-ship-select-"+own.ShipId,()=>perform(()=>workspace.SelectedOwnShip=own.ShipId.Value));
            var selectedShip=ships.FirstOrDefault(s=>s.ShipId.Value==workspace.SelectedOwnShip);
            if(selectedShip!=null)main.Add(new ReviewShipArtPanel(facts.Civilization.DefinitionId.Value,selectedShip.ShipDesignDefinitionId.Value));
            Text(main,NodeName(facts,fleet.LocationNodeId.Value),"console-intro");Text(main,"Accepted route: "+(fleet.RouteNodeIds.Count==0?"None":string.Join(" → ",fleet.RouteNodeIds.Select(n=>NodeName(facts,n.Value)))));
            foreach(var ship in model.Current.Frame.OwnShips.Where(s=>s.Condition.FleetId==fleet.FleetId.Value&&s.Condition.ShipId==workspace.SelectedOwnShip))
            {var c=new VisualElement();c.AddToClassList("console-queue");main.Add(c);var design=facts.Construction.FirstOrDefault(o=>o.Design.DefinitionId==selectedShip?.ShipDesignDefinitionId)?.Design;Text(c,design==null?"Ship":ShipName(design),"console-section");Text(c,"Hull "+ship.Condition.Hull+" / "+ship.Condition.MaxHull);Text(c,"Fore / Port / Starboard / Aft: "+string.Join(" / ",ship.Condition.Shields));foreach(var w in ship.Weapons.Where(w=>w.Class==TacticalWeaponClass.Torpedo))Text(c,"Torpedoes "+w.Ammunition+" / "+w.MagazineCapacity);}
            Text(aside,"1 · Select fleet","console-section");Text(aside,NodeName(facts,fleet.LocationNodeId.Value)+" · "+fleet.ShipIds.Count+" ships");Text(aside,"2 · Choose destination","console-section");
            Text(aside,workspace.SelectedNode==null?"No destination selected":NodeName(facts,workspace.SelectedNode));
            var destinations=new Foldout{name="fleet-route-destination",text="Destination",value=workspace.DestinationExpanded};aside.Add(destinations);destinations.RegisterValueChangedCallback(e=>workspace.DestinationExpanded=e.newValue);
            foreach(var n in facts.Navigation.Nodes)Btn(destinations,n.DisplayName+(n.NodeId.Value==workspace.SelectedNode?" · selected":""),"route-destination-"+n.NodeId,()=>perform(()=>{workspace.SelectedNode=n.NodeId.Value;workspace.DestinationExpanded=false;route=null;}));
            var target=workspace.SelectedNode;
            if(route!=null&&!workspace.RoutePreviewMatchesCurrent)route=null;
            Btn(aside,"3 · Preview route","player-preview-route",()=>perform(()=>route=model.Current.FindKnownRoute(fleet.FleetId.Value,target!)),Editable&&target!=null&&!model.HasLocalEdits);
            if(route!=null)
            {
                if(route.Route.Success)Text(aside,"Unsent preview: "+string.Join(" → ",route.Route.Steps.Select(n=>NodeName(facts,n)))+" · cost "+route.Route.TotalCost);
                else Text(aside,"A route to that destination is unavailable: "+route.Route.ErrorCode);
                var offer=route;
                Btn(aside,"4 · Confirm route","player-confirm-route",()=>confirm("Move this fleet from "+NodeName(facts,offer.Origin)+" to "+NodeName(facts,offer.Destination)+"?\nRoute: "+string.Join(" → ",offer.Route.Steps.Select(n=>NodeName(facts,n)))+". Cost: "+offer.Route.TotalCost+". Travel progresses on End turn; it is not guaranteed escape.",()=>perform(()=>model.SendPreviewedTravel(offer,ReviewUiModel.NewCommandId()))),Editable&&workspace.RoutePreviewMatchesCurrent&&offer.Route.Success&&!model.HasLocalEdits,true);
            }
            Btn(aside,"Remain here","player-stay",()=>confirm("Keep this fleet in its current sector without requesting combat?",()=>perform(()=>{if(model.DraftTravel(fleet.FleetId.Value,CampaignTravelIntent.Stay,null,null))model.SendTravel(ReviewUiModel.NewCommandId());})),Editable&&!model.HasLocalEdits);
            Btn(aside,"Stay / seek combat","player-stay-engage",()=>confirm("Stay and seek a lawful engagement on the next turn? This does not start an immediate battle.",()=>perform(()=>{if(model.DraftTravel(fleet.FleetId.Value,CampaignTravelIntent.StayEngage,null,null))model.SendTravel(ReviewUiModel.NewCommandId());})),Editable&&!model.HasLocalEdits);
            var details=new Foldout{text="Contact details",value=false};aside.Add(details);
            foreach(var known in model.Current.Frame.Travel!.KnownContacts)Text(details,"Last seen contact at "+NodeName(facts,known.LastSeenNodeId)+", turn "+known.LastSeenTurn+" · "+(known.Fresh?"fresh":"stale")+". Current destination is unknown.");
            Btn(details,"Open complete order controls","advanced-fleet-orders",()=>navigate(ReviewWorkspacePage.Combat));
        }
        private void Worlds(VisualElement root,CampaignIntegratedStrategySnapshot facts)
        {
            var main=Panel(root,"Your worlds");var aside=Panel(root,"Manage selected world","console-aside");
            if(colony==null||!facts.Colonies.Any(c=>c.ColonyId.Value==colony))colony=facts.Colonies.FirstOrDefault()?.ColonyId.Value;
            var choices=Row(main);
            foreach(var world in facts.Colonies)Btn(choices,world.Environment.DisplayName+(world.ColonyId.Value==colony?" · selected":""),"world-select-"+world.ColonyId,()=>perform(()=>colony=world.ColonyId.Value));
            var selected=facts.Colonies.FirstOrDefault(c=>c.ColonyId.Value==colony);
            if(selected==null){Text(main,"No owned worlds.");return;}
            Text(main,selected.Environment.DisplayName,"console-intro");
            Text(main,"Population "+selected.Population+" / "+selected.PopulationCapacity+" · Stability "+selected.State.Stability,"console-metric");
            Text(main,"Food stock "+selected.Food+" · Output "+selected.Economy.FoodOutput+" / Demand "+selected.Economy.FoodDemand);
            Text(main,"Industry "+selected.Economy.Industry+" · Credits "+selected.Economy.CreditOutput+" · R&D "+selected.Economy.ResearchOutput+" · Logistics capacity "+selected.Economy.LogisticsCapacity);
            Text(main,selected.Environment.PlanetClass+" · Atmosphere "+selected.Environment.Atmosphere+" · Gravity "+selected.Environment.Gravity+" · Pressure "+selected.Environment.Pressure);
            var cohorts=new Foldout{text="Population & politics",value=false};main.Add(cohorts);
            foreach(var cohort in selected.State.Cohorts)
            {
                var name=facts.References.Species.FirstOrDefault(s=>s.DefinitionId==cohort.SpeciesDefinitionId)?.DisplayName??cohort.SpeciesDefinitionId;
                Text(cohorts,name+" · "+cohort.Population+" · "+cohort.LegalStatus,"console-section");
                Text(cohorts,"Satisfaction "+cohort.Satisfaction+" · Integration "+cohort.Integration+" · Civic alignment "+cohort.CivicAlignment+" · Radicalisation "+cohort.Radicalisation);
            }
            Text(cohorts,"Resistance "+selected.State.Politics.Resistance+" · Pressure "+selected.State.Politics.Pressure);
            var stocks=new Foldout{text="Infrastructure & stockpiles",value=false};main.Add(stocks);
            foreach(var item in selected.State.Infrastructure)Text(stocks,(facts.References.Infrastructure.FirstOrDefault(i=>i.DefinitionId==item.DefinitionId)?.DisplayName??item.DefinitionId)+" × "+item.Count);
            foreach(var stock in selected.State.Stockpiles)Text(stocks,(facts.References.Resources.FirstOrDefault(r=>r.DefinitionId==stock.ResourceDefinitionId)?.DisplayName??Resource(stock.ResourceDefinitionId))+": "+stock.Amount);
            foreach(var allocation in selected.Economy.Allocations)Text(cohorts,"Habitat "+(allocation.Habitat?.ToString()??"Artificial")+" · "+allocation.Population+" residents · habitability "+allocation.Habitability);
            Btn(aside,"Manage production","world-manage-production",()=>navigate(ReviewWorkspacePage.Production),true,true);
            Text(aside,"Governance","console-section");
            var current=facts.References.GovernancePolicies.FirstOrDefault(p=>p.DefinitionId==selected.State.Politics.GovernancePolicyDefinitionId);
            Text(aside,"Current: "+(current?.DisplayName??"Unavailable"));
            foreach(var policy in facts.References.GovernancePolicies)
            {
                Text(aside,policy.Description);
                Btn(aside,policy.DisplayName,"world-policy-"+policy.DefinitionId,()=>confirm("Apply "+policy.DisplayName+" at "+selected.Environment.DisplayName+"? The campaign validates ownership and timing.",()=>perform(()=>{
                    var command=model.StrategicEnvelope(CommandKind.SetWorldGovernancePolicy,ReviewUiModel.NewCommandId());
                    command.SetWorldGovernancePolicy=new SetWorldGovernancePolicyPayload{ColonyId=selected.ColonyId.Value,PolicyDefinitionId=policy.DefinitionId};model.Strategic(command);
                })),Editable&&!model.HasLocalEdits&&policy.DefinitionId!=current?.DefinitionId);
            }
        }
        private static string NodeName(CampaignIntegratedStrategySnapshot f,string id)=>f.Navigation.Nodes.FirstOrDefault(n=>n.NodeId.Value==id)?.DisplayName??id;
        private static string Units(long x)=>((decimal)x/600).ToString("0.###",System.Globalization.CultureInfo.InvariantCulture);
        private static string Reason(CommandErrorCode e)=>e==CommandErrorCode.None?"Available":e==CommandErrorCode.InsufficientCredits?"Not enough credits":e==CommandErrorCode.InsufficientStrategicResource?"Required resources are unavailable at this shipyard":e==CommandErrorCode.EngineeringScienceRequirementMissing?"Earn the required Science breakthroughs first":e==CommandErrorCode.EngineeringPrerequisiteMissing?"Complete the prerequisite Engineering project first":e==CommandErrorCode.EngineeringAlreadyMature?"Completed":e==CommandErrorCode.EngineeringTargetUnchanged?"Current target":e==CommandErrorCode.CombatPending?"Finish combat before changing plans":System.Text.RegularExpressions.Regex.Replace(e.ToString(),"([a-z])([A-Z])","$1 $2");
    }
}
