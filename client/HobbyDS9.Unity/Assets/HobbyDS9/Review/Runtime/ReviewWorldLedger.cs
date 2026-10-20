#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityReview
{
    // Owned colony projections only. Missing foreign/resource/ETA projections stay unavailable.
    public sealed class ReviewWorldLedger
    {
        private readonly ReviewUiModel model;
        private readonly ReviewWorkspace workspace;
        private readonly Func<VisualElement,string,Action,Button> button;
        private readonly Action<Action> perform;
        private readonly Action<ReviewWorkspacePage> navigate;
        private static readonly string[] Columns={"World","Population","Credits / turn","Research / turn","Industry / turn","Food / turn","Food stock","Dilithium stock","Logistics capacity","Current build","Work completed","Work required","Queue count","ETA","Resource output","Unexploited deposits","Power"};
        private static readonly HashSet<string> Numeric=new HashSet<string>(Columns.Skip(1).Take(8).Concat(new[]{"Work completed","Work required","Queue count"}),StringComparer.Ordinal);
        public ReviewWorldLedger(ReviewUiModel model,ReviewWorkspace workspace,Func<VisualElement,string,Action,Button> button,Action<Action> perform,Action<ReviewWorkspacePage> navigate)
        {this.model=model;this.workspace=workspace;this.button=button;this.perform=perform;this.navigate=navigate;}
        private static Label Text(VisualElement parent,string text,string id="")
        {var l=new Label(text){name=id};l.AddToClassList("console-copy");parent.Add(l);return l;}
        private static string SystemName(CampaignIntegratedStrategySnapshot facts,ColonySnapshot colony)
            =>facts.Navigation.Nodes.FirstOrDefault(n=>n.AssociatedSystemId==colony.SystemId)?.DisplayName??"Known owned system";
        private static string BuildName(CampaignIntegratedStrategySnapshot facts,ColonySnapshot colony)
        {
            var active=colony.ProductionQueue.FirstOrDefault();
            if(active==null)return "Idle";
            var design=facts.Construction.FirstOrDefault(c=>c.Design.DefinitionId==active.ShipDesignDefinitionId)?.Design;
            return design?.DisplayName??"Paid ship construction";
        }
        private static decimal? Number(ColonySnapshot c,string column)
        {
            switch(column)
            {
                case "Population":return c.Population;
                case "Credits / turn":return c.Economy.CreditOutput;
                case "Research / turn":return c.Economy.ResearchOutput;
                case "Industry / turn":return c.Economy.Industry;
                case "Food / turn":return c.Economy.FoodOutput;
                case "Food stock":return c.Food;
                case "Dilithium stock":return c.State.Stockpiles.FirstOrDefault(r=>r.ResourceDefinitionId=="resource.dilithium")?.Amount;
                case "Logistics capacity":return c.Economy.LogisticsCapacity;
                case "Work completed":return c.ProductionQueue.FirstOrDefault()?.CompletedIndustry;
                case "Work required":return c.ProductionQueue.FirstOrDefault()?.RequiredIndustry;
                case "Queue count":return c.ProductionQueue.Count;
                default:return null;
            }
        }
        private static string Cell(CampaignIntegratedStrategySnapshot facts,ColonySnapshot c,string column)
        {
            if(column=="World")return c.Environment.DisplayName;
            if(column=="Current build")return BuildName(facts,c);
            var n=Number(c,column);
            return n.HasValue?n.Value.ToString("0",CultureInfo.InvariantCulture):"Unavailable";
        }
        public void Render(VisualElement parent)
        {
            var facts=model.Current.Frame.Strategy!;
            var main=new VisualElement();main.AddToClassList("console-panel");main.AddToClassList("console-work");parent.Add(main);
            Text(main,"Empire / owned worlds").AddToClassList("console-section");
            Text(main,"Current owned observations · turn "+model.Current.Frame.Status.Turn+". Per-turn output, stock and capacity are separate.");
            var toolbar=new VisualElement();toolbar.AddToClassList("review-field-row");main.Add(toolbar);
            var scope=button(toolbar,"Owned worlds",()=>{});scope.SetEnabled(false);
            var unavailable=button(toolbar,"All-known / foreign: unavailable",()=>{});unavailable.SetEnabled(false);
            unavailable.tooltip="A permitted foreign and uncolonised-world projection is not supplied by this scenario.";
            button(toolbar,"Clear filters",()=>perform(()=>workspace.WorldFilters.Clear())).name="world-ledger-clear";
            var malformed=workspace.WorldFilters.Any(f=>Numeric.Contains(f.Key)&&f.Value.Length>0&&!decimal.TryParse(f.Value,NumberStyles.Integer,CultureInfo.InvariantCulture,out _));
            if(malformed)Text(main,"Enter a whole-number minimum for numeric filters. Invalid filters do not hide rows.","world-ledger-filter-warning");
            bool Matches(ColonySnapshot c)
            {
                foreach(var filter in workspace.WorldFilters.Where(f=>f.Value.Length>0))
                {
                    if(Numeric.Contains(filter.Key))
                    {
                        if(decimal.TryParse(filter.Value,NumberStyles.Integer,CultureInfo.InvariantCulture,out var minimum))
                        {var n=Number(c,filter.Key);if(!n.HasValue||n<minimum)return false;}
                    }
                    else if(Cell(facts,c,filter.Key).IndexOf(filter.Value,StringComparison.OrdinalIgnoreCase)<0)return false;
                }
                return true;
            }
            var rows=facts.Colonies.Where(Matches).ToList();
            rows.Sort((a,b)=>{
                var av=Number(a,workspace.WorldSort);var bv=Number(b,workspace.WorldSort);
                int comparison;
                if(Numeric.Contains(workspace.WorldSort))
                {
                    // Unknown stays after known in both directions.
                    if(av.HasValue!=bv.HasValue)return av.HasValue?-1:1;
                    comparison=Nullable.Compare(av,bv);
                }
                else comparison=StringComparer.OrdinalIgnoreCase.Compare(Cell(facts,a,workspace.WorldSort),Cell(facts,b,workspace.WorldSort));
                if(workspace.WorldSortDescending)comparison=-comparison;
                return comparison!=0?comparison:StringComparer.Ordinal.Compare(a.ColonyId.Value,b.ColonyId.Value);
            });
            Text(main,"Filtered owned totals ("+rows.Count+" current worlds): Credits "+rows.Sum(c=>(decimal)c.Economy.CreditOutput)+
                " / turn · Research "+rows.Sum(c=>(decimal)c.Economy.ResearchOutput)+" / turn · Industry "+rows.Sum(c=>(decimal)c.Economy.Industry)+" / turn.","world-ledger-totals");
            var scroll=new ScrollView(ScrollViewMode.VerticalAndHorizontal){name="world-ledger-scroll"};scroll.style.flexGrow=1;scroll.style.minHeight=0;main.Add(scroll);
            var table=new VisualElement();table.AddToClassList("world-ledger-table");scroll.Add(table);
            var header=new VisualElement();header.AddToClassList("world-ledger-row");table.Add(header);
            foreach(var column in Columns)
            {
                var cell=new VisualElement();cell.AddToClassList("world-ledger-cell");header.Add(cell);
                var sort=button(cell,column+(workspace.WorldSort==column?(workspace.WorldSortDescending?" ↓":" ↑"):""),()=>perform(()=>{
                    if(workspace.WorldSort==column)workspace.WorldSortDescending=!workspace.WorldSortDescending;
                    else{workspace.WorldSort=column;workspace.WorldSortDescending=false;}
                }));sort.name="world-sort-"+column;
                workspace.WorldFilters.TryGetValue(column,out var filter);
                var input=new TextField{value=filter??"",isDelayed=true,name="world-filter-"+column,tooltip=Numeric.Contains(column)?"Minimum; blank shows all known and unavailable values.":"Contains; blank shows all."};
                input.AddToClassList("world-ledger-filter");cell.Add(input);
                input.RegisterValueChangedCallback(e=>perform(()=>workspace.WorldFilters[column]=e.newValue.Trim()));
            }
            foreach(var group in rows.GroupBy(c=>c.SystemId.Value))
            {
                var collapsed=workspace.CollapsedSystems.Contains(group.Key);
                var toggle=button(table,(collapsed?"▸ ":"▾ ")+SystemName(facts,group.First())+" · "+group.Count()+" matching owned worlds",()=>perform(()=>{
                    if(!workspace.CollapsedSystems.Add(group.Key))workspace.CollapsedSystems.Remove(group.Key);
                }));toggle.name="world-system-"+group.Key;toggle.AddToClassList("world-system-heading");
                if(collapsed)continue;
                foreach(var colony in group)
                {
                    var row=new VisualElement{name="world-row-"+colony.ColonyId};row.AddToClassList("world-ledger-row");
                    row.EnableInClassList("world-row-selected",workspace.SelectedColony==colony.ColonyId.Value);table.Add(row);
                    foreach(var column in Columns)
                    {
                        var cell=new VisualElement();cell.AddToClassList("world-ledger-cell");row.Add(cell);
                        if(column=="World"){var choose=button(cell,colony.Environment.DisplayName,()=>perform(()=>workspace.SelectedColony=colony.ColonyId.Value));choose.name="world-select-"+colony.ColonyId;}
                        else Text(cell,Cell(facts,colony,column));
                    }
                }
            }
            if(rows.Count==0)Text(table,"No owned worlds match these filters.");
            var aside=new ScrollView(ScrollViewMode.Vertical);aside.AddToClassList("console-panel");aside.AddToClassList("console-aside");parent.Add(aside);
            Text(aside,"Selected world").AddToClassList("console-section");
            var selected=facts.Colonies.FirstOrDefault(c=>c.ColonyId.Value==workspace.SelectedColony);
            if(selected==null){Text(aside,"Select an owned world to inspect its current construction and open its queue.");return;}
            Text(aside,selected.Environment.DisplayName).AddToClassList("console-intro");
            if(!rows.Contains(selected))Text(aside,"Selected world is hidden by filters; selection is retained.");
            Text(aside,SystemName(facts,selected)+" · "+selected.Environment.PlanetClass);
            Text(aside,"Population "+selected.Population+" / "+selected.PopulationCapacity+" · Stability "+selected.State.Stability);
            Text(aside,"Current construction: "+BuildName(facts,selected),"world-current-construction");
            var active=selected.ProductionQueue.FirstOrDefault();
            if(active!=null)
            {
                Text(aside,active.CompletedIndustry+" / "+active.RequiredIndustry+" Industry · "+selected.ProductionQueue.Count+" queued");
                Text(aside,"Paid: "+active.PaidCredits+" credits. Remaining-turn estimate unavailable.");
            }
            Text(aside,"Foreign queues, power supply and unexploited deposits are unavailable in this observation; they are not counted as zero.");
            button(aside,"Open this world's production queue",()=>navigate(ReviewWorkspacePage.Production)).name="world-open-exact-queue";
        }
    }
}
