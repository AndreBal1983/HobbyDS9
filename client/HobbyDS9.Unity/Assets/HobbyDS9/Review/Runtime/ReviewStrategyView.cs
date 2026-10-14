#nullable enable
using System;
using System.Linq;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using UnityEngine;
using UnityEngine.UIElements;
namespace HobbyDS9.UnityReview
{
    // Only the copied actor strategy projection and lawful contact facts reach this view.
    public sealed class ReviewStrategyView
    {
        private readonly ReviewUiModel model;
        private readonly Func<VisualElement,string,Action,Button> button;
        private readonly Action<Action> perform;
        private readonly Action<string,Action> confirm;
        private readonly Action<ReviewWorkspacePage> navigate;
        private readonly ReviewWorkspace workspace;
        private readonly Action<string,string?,string?> selectFleet;
        public ReviewStrategyView(ReviewUiModel model,ReviewWorkspace workspace,Func<VisualElement,string,Action,Button> button,Action<Action> perform,Action<string,Action> confirm,Action<ReviewWorkspacePage> navigate,Action<string,string?,string?> selectFleet)
        {this.model=model;this.workspace=workspace;this.button=button;this.perform=perform;this.confirm=confirm;this.navigate=navigate;this.selectFleet=selectFleet;}
        private Button ActionButton(VisualElement root,string label,string id,Action action,bool enabled=true)
        {var b=button(root,label,action);b.name=id;b.SetEnabled(enabled);return b;}
        private static void Line(VisualElement root,string text){var l=new Label(text);l.style.whiteSpace=WhiteSpace.Normal;root.Add(l);}
        private bool Editable=>model.Current.Frame.Status.Phase==SessionPhase.Planning&&!model.Current.HasPendingWork&&!model.Current.RefreshRequired&&!model.Current.Dirty&&!model.Busy&&!model.Closed;
        public void Render(VisualElement root,ReviewWorkspacePage page)
        {
            root.Clear();var facts=model.Current.Frame.Strategy;
            if(facts==null){Line(root,"This scenario does not provide integrated strategy/R&D/production. Use Launch-IntegratedPlaytest.cmd for that explicit campaign.");return;}
            var scroll=new ScrollView(ScrollViewMode.Vertical){name="integrated-strategy-details"};scroll.style.flexGrow=1;scroll.style.minHeight=0;root.Add(scroll);
            Line(scroll,page==ReviewWorkspacePage.Research?"Science & Engineering":page==ReviewWorkspacePage.Production?"Owned ship production — credits "+facts.Civilization.Credits:"Known strategic map — "+facts.Status.Phase);
            if(page==ReviewWorkspacePage.Map)Map(scroll,facts);
            else if(page==ReviewWorkspacePage.Research)Research(scroll,facts);
            else Production(scroll,facts);
            var footer=new VisualElement{name="integrated-strategy-footer"};footer.AddToClassList("review-footer");root.Add(footer);
            ActionButton(footer,"Fleet orders","strategy-fleet-orders",()=>navigate(ReviewWorkspacePage.FleetOrders));
            if(model.Current.Frame.Encounters.Count>0)ActionButton(footer,"Open pending combat","strategy-open-combat",()=>navigate(ReviewWorkspacePage.Combat));
            ActionButton(footer,"Advance accepted strategic orders","strategy-advance",()=>confirm("Advance this same campaign once? Unsent edits must first be applied or discarded.",()=>perform(()=>model.Advance())),Editable&&!model.HasLocalEdits);
            if(model.HasLocalEdits)Line(footer,"Unsent edits retained. Saving stores committed state only.");
        }
        private void Map(VisualElement root,CampaignIntegratedStrategySnapshot facts)
        {
            var map=new VisualElement{name="integrated-known-map"};map.style.height=340;map.style.minHeight=200;map.style.flexShrink=0;map.style.backgroundColor=new Color(.03f,.07f,.12f);root.Add(map);
            var nodes=facts.Navigation.Nodes.ToList();
            Vector2 PointFor(string id)
            {int i=nodes.FindIndex(n=>n.NodeId.Value==id);float angle=2*Mathf.PI*i/Math.Max(1,nodes.Count)-Mathf.PI/2;return new Vector2(map.contentRect.width*.5f+Mathf.Cos(angle)*map.contentRect.width*.32f,170+Mathf.Sin(angle)*120);}
            map.generateVisualContent+=ctx=>{
                var p=ctx.painter2D;p.strokeColor=new Color(.35f,.55f,.7f);p.lineWidth=2;
                foreach(var link in facts.Navigation.Links){p.BeginPath();p.MoveTo(PointFor(link.EndpointA.Value));p.LineTo(PointFor(link.EndpointB.Value));p.Stroke();}
            };
            foreach(var node in nodes)
            {
                var marker=ActionButton(map,node.DisplayName,"map-node-"+node.NodeId.Value,()=>perform(()=>workspace.SelectedNode=node.NodeId.Value));marker.style.position=Position.Absolute;marker.style.width=150;marker.tooltip="Known sector "+node.NodeId.Value;
                map.RegisterCallback<GeometryChangedEvent>(_=>{var p=PointFor(node.NodeId.Value);marker.style.left=p.x-75;marker.style.top=p.y-18;map.MarkDirtyRepaint();});
            }
            Line(root,"Schematic presentation of known topology; movement uses authoritative routes/costs. No enemy current-position markers.");
            Line(root,"Selected known destination: "+(workspace.SelectedNode??"choose a map sector, then select an owned fleet"));
            foreach(var link in facts.Navigation.Links)Line(root,link.EndpointA+" ↔ "+link.EndpointB+" | "+link.Kind+" | authoritative cost "+link.TraversalCost);
            foreach(var colony in facts.Colonies)Line(root,colony.Environment.DisplayName+" | owned colony | Industry "+colony.Economy.Industry+" | R&D output "+colony.Economy.ResearchOutput);
            foreach(var fleet in facts.Fleets)
            {
                Line(root,"Your fleet "+fleet.FleetId+" | sector "+fleet.LocationNodeId+" | progress "+fleet.TransitProgress+"/"+fleet.TransitTotalCost);
                ActionButton(root,"Orders for "+fleet.FleetId,"map-fleet-orders-"+fleet.FleetId,()=>selectFleet(fleet.FleetId.Value,workspace.SelectedNode,null));
                foreach(var ship in facts.Ships.Where(s=>s.FleetId==fleet.FleetId))
                {
                    Line(root,"Own ship "+ship.ShipId+" | "+(ship.ResolvedDesign?.DisplayName??ship.ShipDesignDefinitionId.Value)+" | campaign hull "+ship.CurrentHull);
                    ActionButton(root,"Inspect "+ship.ShipId,"inspect-own-"+ship.ShipId.Value,()=>perform(()=>model.Inspect(ship.ShipId.Value)));
                    ActionButton(root,"Fleet orders / select "+ship.ShipId,"orders-own-"+ship.ShipId,()=>selectFleet(ship.FleetId.Value,workspace.SelectedNode,ship.ShipId.Value));
                }
            }
            Line(root,model.Inspection());
            foreach(var known in model.Current.Frame.Travel!.KnownContacts)Line(root,"Last seen "+known.FleetId+" at "+known.LastSeenNodeId+", turn "+known.LastSeenTurn+" (age "+(facts.Status.Turn-known.LastSeenTurn)+"); fresh "+known.Fresh+". Current enemy route/destination unavailable.");
            if(facts.Status.Phase!=SessionPhase.Planning)Line(root,"Combat consequences are staged; finalize the complete batch before new strategic orders.");
        }
        private static string Units(long value)=>((decimal)value/ResearchUnits.Scale).ToString("0.###",System.Globalization.CultureInfo.InvariantCulture);
        private void Research(VisualElement root,CampaignIntegratedStrategySnapshot facts)
        {
            var r=facts.Research;var draft=model.ResearchInputs()!;bool editable=Editable&&draft.Current(model.Current.Frame);
            Line(root,"R&D capacity "+r.ResearchOutput+" | Science "+r.SciencePercent+"% / Engineering "+r.EngineeringPercent+"% | reserves "+r.ScienceReserveConceptual+" / "+r.EngineeringReserveConceptual);
            var values=Enumerable.Range(0,21).Select(i=>i*ResearchUnits.Scale/20).ToList();if(!values.Contains(draft.ScienceUnits)){values.Add(draft.ScienceUnits);values.Sort();}
            var split=new DropdownField("Science / Engineering share",values.Select(v=>(100m*v/ResearchUnits.Scale).ToString("0.##")+"% Science").ToList(),values.IndexOf(draft.ScienceUnits)){name="integrated-research-split"};split.SetEnabled(editable);root.Add(split);
            split.RegisterValueChangedCallback(_=>perform(()=>draft.ScienceUnits=values[split.index]));
            ActionButton(root,"Review / apply R&D split","integrated-apply-split",()=>confirm("Apply only the selected R&D split? Other local Science shares remain unsent.",()=>perform(()=>model.AllocateResearch(draft.ScienceUnits,ReviewUiModel.NewCommandId()))),editable&&draft.SplitChanged);
            Line(root,"Six Science shares are local desired allocations; Apply sends the complete six-way plan. Progress and breakthroughs come from authority.");
            foreach(var card in r.Science)
            {
                Line(root,card.Discipline+" | Level "+card.Level+" / "+card.MaxLevel+" | progress "+card.ProgressPercent.ToString("0.##")+"% | local share "+(100m*draft.Weight(card.Discipline)/ResearchUnits.Scale).ToString("0.##")+"%");
                foreach(var point in card.Breakpoints)Line(root,(point.Achieved?"✓ ":"○ ")+"Breakthrough "+point.Ordinal+" threshold "+Units(point.Threshold));
                var row=new VisualElement();row.AddToClassList("review-field-row");root.Add(row);
                ActionButton(row,"− 5%","science-minus-"+card.Discipline,()=>perform(()=>draft.Adjust(card.Discipline,-ResearchUnits.Scale/20)),editable);
                ActionButton(row,"+ 5%","science-plus-"+card.Discipline,()=>perform(()=>draft.Adjust(card.Discipline,ResearchUnits.Scale/20)),editable);
                if(card.AllocatedWhileMaxed)Line(root,"Maximum level: allocated points remain in Science reserve until deliberately reallocated.");
            }
            ActionButton(root,"Equalize local shares","integrated-equalize-science",()=>perform(()=>draft.Equalize()),editable);
            ActionButton(root,"Review / apply six Science shares","integrated-apply-science",()=>confirm("Apply all six desired Science shares?",()=>perform(()=>model.AllocateScience(draft.Values(),ReviewUiModel.NewCommandId()))),editable&&draft.ScienceChanged);
            foreach(var project in r.Engineering)
            {
                var d=project.Definition;Line(root,d.DisplayName+" | "+d.Kind+" | "+project.Band+" | "+project.MaturityPercent+"% | cost "+Units(project.Cost)+" | "+(project.IsTarget?"Current target":Reason(project.UnavailableReason)));
                for(int i=0;i<d.ScienceRequirements.Count;i++){var q=d.ScienceRequirements[i];Line(root,(project.ScienceSatisfied[i]?"✓ ":"○ ")+q.Discipline+" interval "+q.IntervalStartLevel+" breakthrough "+q.BreakthroughOrdinal);}
                Line(root,d.EffectSummary);
                ActionButton(root,"Develop "+d.DisplayName,"integrated-develop-"+d.DefinitionId,()=>confirm("Develop this earned Engineering project?",()=>perform(()=>model.Engineering(d.DefinitionId.Value,ReviewUiModel.NewCommandId()))),editable&&project.CanTarget);
            }
            ActionButton(root,"Clear Engineering target","integrated-clear-engineering",()=>confirm("Clear target while retaining earned progress?",()=>perform(()=>model.Engineering(null,ReviewUiModel.NewCommandId()))),editable&&r.Target.HasValue);
            foreach(var receipt in r.Receipts)Line(root,"Latest Research: "+receipt.Category+" | "+Units(receipt.Amount)+(receipt.Discipline.HasValue?" | "+receipt.Discipline:"")+(receipt.ApplicationDefinitionId.HasValue?" | "+receipt.ApplicationDefinitionId:""));
            if(!draft.Current(model.Current.Frame))Line(root,"Research inputs belong to an earlier boundary. Discard them deliberately before editing the new observation.");
        }
        private void Production(VisualElement root,CampaignIntegratedStrategySnapshot facts)
        {
            Line(root,"Only explicitly admitted authored-v2 templates are available. Work, costs and completion are authoritative; no automatic refit or blueprint conversion.");
            foreach(var colony in facts.Colonies)
            {
                Line(root,colony.Environment.DisplayName+" | Industry "+colony.Economy.Industry);
                foreach(var item in colony.ProductionQueue)
                {
                    Line(root,"Queued "+item.ShipDesignDefinitionId+" | paid "+item.PaidCredits+" credits + "+string.Join(", ",item.PaidResources.Select(v=>v.Amount+" "+v.ResourceDefinitionId))+" | Industry "+item.CompletedIndustry+"/"+item.RequiredIndustry);
                    ActionButton(root,"Review / cancel queue item","integrated-cancel-"+item.QueueItemId,()=>confirm("Cancel this owned paid queue item? Backend calculates the unused-cost refund; completed Industry is lost.",()=>perform(()=>model.CancelProduction(colony.ColonyId.Value,item.QueueItemId.Value,ReviewUiModel.NewCommandId()))),Editable);
                }
                foreach(var option in facts.Construction.Where(o=>o.ColonyId==colony.ColonyId))
                {
                    var d=option.Design;Line(root,d.DisplayName+" | "+d.CreditCost+" credits + "+string.Join(", ",d.ResourceCosts.Select(v=>v.Amount+" "+v.ResourceDefinitionId))+" | "+d.IndustryCost+" Industry | "+Reason(option.UnavailableReason));
                    Line(root,"Exact authored equipment "+option.FitId+" revision "+option.FitRevision+". Initial stocks are provisioned once on completion.");
                    ActionButton(root,"Review / queue "+d.DisplayName,"integrated-queue-"+d.DefinitionId,()=>confirm("Pay the displayed cost and enqueue this eligible template once?",()=>perform(()=>model.Enqueue(colony.ColonyId.Value,d.DefinitionId.Value,ReviewUiModel.NewCommandId()))),Editable&&option.CanEnqueue);
                }
            }
            Line(root,"Current owned ships — completed production joins an eligible stationary fleet or creates a new fleet through authority.");
            foreach(var ship in facts.Ships)Line(root,ship.ShipId+" | "+(ship.ResolvedDesign?.DisplayName??ship.ShipDesignDefinitionId.Value)+" | fleet "+ship.FleetId+" | hull "+ship.CurrentHull);
        }
        private static string Reason(CommandErrorCode reason)
        {
            switch(reason)
            {
                case CommandErrorCode.None:return "Available";
                case CommandErrorCode.InsufficientCredits:return "Insufficient credits";
                case CommandErrorCode.InsufficientStrategicResource:return "Insufficient local construction resources";
                case CommandErrorCode.CombatPending:return "Complete campaign combat first";
                case CommandErrorCode.EngineeringScienceRequirementMissing:return "Required Science breakthroughs not yet earned";
                case CommandErrorCode.EngineeringPrerequisiteMissing:return "Required Engineering prerequisites not yet earned";
                case CommandErrorCode.EngineeringAlreadyMature:return "Completed";
                case CommandErrorCode.EngineeringTargetUnchanged:return "Current target";
                case CommandErrorCode.DesignEngineeringPrerequisiteMissing:return "Required mature Engineering unavailable";
                case CommandErrorCode.ShipbuildingUnavailable:return "Shipbuilding unavailable at this colony";
                default:return System.Text.RegularExpressions.Regex.Replace(reason.ToString(),"([a-z])([A-Z])","$1 $2");
            }
        }
    }
}
