#nullable enable
using System;
using System.Linq;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using UnityEngine.UIElements;
using EntityId = HobbyDS9.Contracts.EntityId;

namespace HobbyDS9.UnityClient
{
    // Presentation only: all costs, thresholds, eligibility and output come from Application.
    // Editing a dropdown does not issue a command; buttons use the observed turn/revision.
    public sealed class ResearchPanelView : VisualElement
    {
        public ResearchPanelView(){name="science-engineering";AddToClassList("research-panel");}
        public void Present(CampaignSnapshot snapshot,StrategicClient client,Action<Func<string>> run)
        {
            Clear();Add(new LcarsSectionHeader("Science & Engineering"));
            var actor=snapshot.Campaign.Civilizations.FirstOrDefault(c=>c.ControllerKind==ControllerKind.Human);
            if(actor==null){Line("No human civilization.");return;}
            var observed=ResearchOverviewSnapshot.For(snapshot,actor.CivilizationId);
            Line($"R&D Capacity {observed.ResearchOutput}\nScience {observed.ScienceAllocationBps/100m:0.##}% ({observed.ScienceAllocation}) • Engineering {(10000-observed.ScienceAllocationBps)/100m:0.##}% ({observed.EngineeringAllocation})");
            Line($"Science reserve {observed.ScienceReserve} • Engineering reserve {observed.EngineeringReserve}");
            Line($"Science focus: {observed.Focus?.ToString()??"None"}\nEngineering target: {(observed.Target.HasValue?snapshot.Content.Engineering.Single(d=>d.DefinitionId==observed.Target.Value).DisplayName:"None")}");
            var allocations=Enumerable.Range(0,21).Select(i=>i*500).ToList();
            if(!allocations.Contains(observed.ScienceAllocationBps)){allocations.Add(observed.ScienceAllocationBps);allocations.Sort();}
            var split=new DropdownField("Science / Engineering",allocations.Select(b=>$"{b/100m:0.##}% / {(10000-b)/100m:0.##}%").ToList(),allocations.IndexOf(observed.ScienceAllocationBps)){name="research-split"};
            split.AddToClassList("combat-plan-field");Add(split);
            var access=StrategicOptions.PlanningAccess(snapshot,actor.CivilizationId);
            var allocation=Button("Apply allocation","apply-research-split",()=>run(()=>SfxFeedback.Describe(client.AllocateResearch(snapshot,actor.CivilizationId,allocations[split.index]))),access==CommandErrorCode.None);
            split.RegisterValueChangedCallback(_=>allocation.SetEnabled(access==CommandErrorCode.None&&allocations[split.index]!=observed.ScienceAllocationBps));
            allocation.SetEnabled(false);
            Add(new LcarsSectionHeader("Science"));
            foreach(var discipline in observed.Science)
            {
                var card=new VisualElement{name="science-"+discipline.Discipline};card.AddToClassList("research-card");Add(card);
                Line($"{discipline.Discipline} • Level {discipline.Level}/{discipline.MaxLevel}"+(discipline.IsFocus?" • FOCUS":""),card);
                card.Add(new ProgressBar{value=(float)discipline.ProgressPercent,title=discipline.Level==10?"Maximum level":$"{discipline.Progress}/{discipline.IntervalCost} • {discipline.ProgressPercent:0.#}%"});
                foreach(var breakpoint in discipline.Breakpoints)
                    Line($"{(breakpoint.Achieved?"✓":"○")} Breakthrough {breakpoint.Ordinal}: {breakpoint.Threshold} • chunk {breakpoint.ChunkCost}",card);
                Button("Focus "+discipline.Discipline,"focus-"+discipline.Discipline,()=>run(()=>SfxFeedback.Describe(client.FocusScience(snapshot,actor.CivilizationId,discipline.Discipline))),discipline.CanFocus,CommandFeedback.Explain(discipline.UnavailableReason),card);
            }
            Add(new LcarsSectionHeader("Engineering"));
            foreach(var application in observed.Engineering)
            {
                var definition=application.Definition;var card=new VisualElement{name="engineering-"+definition.DefinitionId.Value};card.AddToClassList("research-card");Add(card);
                var state=application.Band==EngineeringMaturityBand.Mature?"Mature":application.IsTarget?"Current":application.ScienceSatisfied.All(s=>s)&&application.PrerequisitesSatisfied.All(s=>s)?"Available":"Locked";
                Line(definition.DisplayName+" • "+state,card);
                card.Add(new ProgressBar{value=application.MaturityPercent,title=$"{application.Progress}/{application.Cost} • {application.MaturityPercent}% • {application.Band}"});
                for(int i=0;i<definition.ScienceRequirements.Count;i++)
                {var q=definition.ScienceRequirements[i];Line($"{(application.ScienceSatisfied[i]?"✓":"○")} {q.Discipline} interval {q.IntervalStartLevel}→{q.IntervalStartLevel+1}, breakthrough {q.BreakthroughOrdinal}",card);}
                for(int i=0;i<definition.Prerequisites.Count;i++)
                    Line((application.PrerequisitesSatisfied[i]?"✓ ":"○ ")+snapshot.Content.Engineering.Single(d=>d.DefinitionId==definition.Prerequisites[i]).DisplayName,card);
                Line("Only Mature applications unlock ships and prerequisite applications.",card);
                if(!application.CanTarget)Line(CommandFeedback.Explain(application.UnavailableReason),card);
                Button("Develop "+definition.DisplayName,"target-"+definition.DefinitionId.Value,()=>run(()=>SfxFeedback.Describe(client.Engineering(snapshot,actor.CivilizationId,definition.DefinitionId))),application.CanTarget,CommandFeedback.Explain(application.UnavailableReason),card);
            }
            Button("Clear Engineering target","clear-engineering-target",()=>run(()=>SfxFeedback.Describe(client.Engineering(snapshot,actor.CivilizationId,null))),access==CommandErrorCode.None&&observed.Target.HasValue);
            var receipts=new Foldout{name="research-receipts",text="Latest-turn research",value=false};Add(receipts);
            if(observed.Receipts.Count==0)Line("No Research phase receipts yet.",receipts);
            foreach(var receipt in observed.Receipts)
                Line($"{ReceiptLabel(receipt.Category)}: {receipt.Amount}"+(receipt.Discipline.HasValue?$" • {receipt.Discipline} interval {receipt.IntervalStartLevel}":"")+(receipt.BreakthroughOrdinal>0?$" • breakthrough {receipt.BreakthroughOrdinal}":"")+(receipt.ApplicationDefinitionId.HasValue?" • "+snapshot.Content.Engineering.Single(d=>d.DefinitionId==receipt.ApplicationDefinitionId.Value).DisplayName:""),receipts);
        }
        private static string ReceiptLabel(string category)
        {
            switch(category)
            {
                case "allocation": return "R&D generated";
                case "science-allocated": return "Science allocated";
                case "engineering-allocated": return "Engineering allocated";
                case "science-spent": return "Science spent";
                case "science-breakthrough": return "Science breakthrough";
                case "science-level": return "Science level reached";
                case "engineering-spent": return "Engineering progress added";
                case "engineering-band": return "Engineering maturity band";
                case "engineering-mature": return "Engineering application matured";
                default: return category;
            }
        }
        private void Line(string text,VisualElement? parent=null){var label=new Label(text);label.style.whiteSpace=WhiteSpace.Normal;(parent??this).Add(label);}
        private Button Button(string text,string id,Action clicked,bool enabled=true,string tooltip="",VisualElement? parent=null)
        {var button=new LcarsPillButton("",clicked){text=text,name=id,tooltip=tooltip};button.SetEnabled(enabled);(parent??this).Add(button);return button;}
    }
}
