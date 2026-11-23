#nullable enable
using System;
using System.Linq;
using System.Collections.Generic;
using HobbyDS9.Contracts;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewEntry
    {
        private void DrawPublishedConstructionMenu(VisualElement parent)
        {
            var fold=new Foldout{text="Published construction · provisional playtest",value=false,name="published-construction-menu"};parent.Add(fold);
            fold.Add(Readout("Use the seed and galaxy settings above. Build eligible published utility classes; tuning is provisional. Existing galaxies keep their original rules and saves."));
            var start=AddButton(fold,"New construction playtest",()=>Confirm("Start the provisional construction playtest? Current progress is replaced only after validation; existing saves remain intact.",()=>RunGeneratedCampaignOperation(null,published:true)));
            start.name="published-construction-start";
            foreach(var slot in GeneratedCampaignOptions.Slots()){var captured=slot;var b=AddButton(fold,"Continue playtest "+slot.Substring(slot.Length-1),
                ()=>Confirm("Continue this construction playtest? Unsaved progress is replaced only after validation.",()=>RunGeneratedCampaignOperation(captured,published:true)));b.name="published-construction-continue-"+slot;}
        }
        private void DrawPublishedConstructionPanel(VisualElement parent)
        {
            var owner=model!;var controller=owner.Current;var frame=controller.Frame;
            var records=owner.DesignLibrary().Published.OrderBy(r=>r.Blueprint.BlueprintId,StringComparer.Ordinal).ThenBy(r=>r.Blueprint.Revision).ToList();
            var colonies=frame.Strategy!.Colonies.OrderBy(c=>c.ColonyId.Value,StringComparer.Ordinal).ToList();
            if(records.Count==0||colonies.Count==0){parent.Add(Readout("Publish a class revision before selecting a colony."));return;}
            var revisions=new DropdownField("Class revision",records.Select(r=>r.Blueprint.Fit.DisplayName+" · R"+r.Blueprint.Revision+" ["+r.Blueprint.BlueprintId.Substring(Math.Max(0,r.Blueprint.BlueprintId.Length-8))+"]").ToList(),0){name="published-construction-revision"};
            var yards=new DropdownField("Colony",colonies.Select(c=>c.ColonyId.Value).ToList(),0){name="published-construction-colony"};
            var status=Readout("");status.name="published-construction-eligibility";
            CampaignPublishedConstructionEligibility? observed=null;
            string Price(CampaignPublishedConstructionEligibility offer)=>offer.Cost.Credits+" credits · "+offer.Cost.Industry+" Industry"
                +(offer.Cost.Resources.Count==0?"":" · "+string.Join(", ",offer.Cost.Resources.Select(r=>r.Amount+" "+r.ResourceDefinitionId.Replace("resource.",""))));
            var queue=AddButton(parent,"Pay and queue this revision",()=>{
                var selected=observed;if(selected==null)return;
                Confirm("Queue "+selected.BlueprintId+" R"+selected.BlueprintRevision+" at "+selected.ColonyId+"? "+Price(selected)+". Credits and materials are paid now.",
                    ()=>{if(ReferenceEquals(model,owner)&&ReferenceEquals(owner.Current,controller)&&ReferenceEquals(controller.Frame,frame))Do(()=>owner.EnqueuePublished(selected,ReviewUiModel.NewCommandId()));});
            });queue.name="published-construction-queue";
            void Refresh()
            {
                observed=null;queue.SetEnabled(false);
                if(!ReferenceEquals(model,owner)||!ReferenceEquals(owner.Current,controller)||!ReferenceEquals(controller.Frame,frame)||owner.Busy||owner.Closed)
                {status.text="Observation changed. Refresh the campaign designer before building.";return;}
                try{var r=records[revisions.index];observed=owner.ConstructionEligibility(r.Blueprint.BlueprintId,r.Blueprint.Revision,colonies[yards.index].ColonyId.Value);
                    status.text=observed.CanQueue?Price(observed)+" · eligible"
                        :!observed.Supported?"This revision can be published but has unsupported tactical fittings: "+string.Join(", ",observed.SupportIssues.Select(i=>i.Code+" "+i.Path))
                        :observed.OrdinaryReason!=CommandErrorCode.None?"Unavailable: "+observed.OrdinaryReason
                        :observed.CommandResult==CampaignTacticalError.Overflow?"This order cannot safely fit the campaign's remaining save capacity.":"Unavailable: "+observed.CommandResult;
                    queue.SetEnabled(observed.CanQueue&&!controller.HasPendingWork&&!controller.Dirty&&!controller.RefreshRequired);}
                catch(Exception){status.text="Construction observation unavailable. Refresh before retrying.";}
            }
            var selection=new VisualElement{name="published-construction-selection"};selection.AddToClassList("sd-construction-selection");
            selection.Add(revisions);selection.Add(yards);
            var reasons=new ScrollView(ScrollViewMode.Vertical){name="published-construction-reason-scroll"};reasons.Add(status);
            parent.Insert(0,selection);parent.Insert(1,reasons);
            revisions.RegisterValueChangedCallback(_=>Refresh());yards.RegisterValueChangedCallback(_=>Refresh());Refresh();
        }
    }
}
