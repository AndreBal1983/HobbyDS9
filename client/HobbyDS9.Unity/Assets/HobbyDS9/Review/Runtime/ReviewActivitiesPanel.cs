#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HobbyDS9.ArtDemo;
using HobbyDS9.Contracts;
using UnityEngine;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityReview
{
    // A view of one detached actor observation. Eligibility is advisory only;
    // the host revalidates the captured command at its original revision.
    public sealed class ReviewActivitiesPanel : VisualElement, IDisposable
    {
        private readonly CampaignActivityObservationDto observation;
        private readonly CampaignStreamedActivityObservationDto? streamed;
        private readonly Func<CampaignActivityCommandDto,CampaignActivityEligibilityDto>? evaluate;
        private readonly Action<CampaignActivityCommandDto> submit;
        private readonly Action<CampaignActivityCommandDto,string>? requestReview;
        private readonly string actor;
        private readonly int turn;
        private readonly long revision;
        private readonly VisualElement worlds,expeditions,review;
        private readonly ScrollView pageScroll;
        private ReviewConstructionCatalogue? construction;
        private readonly Label status;
        private CampaignActivityCommandDto? pending;
        private bool disposed,expeditionsSelected,submitted;
        private int colonyIndex,shipIndex,siteIndex;
        private VisualElement? buildList,queueList,siteDetails,history;
        private Label? colonyFacts,shipFacts,previewCaption;
        private Image? previewImage;
        private ShipPreviewStage? stage;
        private float yaw=37.57f;

        // Ephemeral presentation only: no observed payload, pending order or
        // callback survives replacement. The shell separately binds the session.
        internal sealed class Selection
        {
            internal readonly string Actor;
            internal readonly bool Expeditions;
            internal readonly string? Colony,Ship,Site;
            internal readonly ReviewConstructionCatalogue.Selection? Construction;
            internal Selection(string actor,bool expeditions,string? colony,string? ship,string? site,ReviewConstructionCatalogue.Selection? construction=null)
            {Actor=actor;Expeditions=expeditions;Colony=colony;Ship=ship;Site=site;Construction=construction;}
        }
        internal Selection? CaptureSelection()=>disposed?null:new Selection(actor,expeditionsSelected,Colony?.ColonyId,Ship?.ShipId,Site?.Site.SiteId,construction?.CaptureSelection());
        internal void RestoreSelection(Selection? saved)
        {
            if(disposed||saved==null||saved.Actor!=actor)return;
            var colony=observation.Colonies.FindIndex(c=>c.ColonyId==saved.Colony);
            var ship=observation.Ships.FindIndex(s=>s.ShipId==saved.Ship);
            var site=observation.Sites.FindIndex(s=>s.Site.SiteId==saved.Site);
            // Current fields run their normal view callbacks; missing IDs keep
            // the new observation's default selection independently.
            if(colony>=0)this.Q<DropdownField>("activity-colony-selector").index=colony;
            if(ship>=0)this.Q<DropdownField>("activity-ship-selector").index=ship;
            if(site>=0)this.Q<DropdownField>("activity-site-selector").index=site;
            SelectPage(saved.Expeditions);
            construction?.RestoreSelection(saved.Construction);
        }

        public ReviewActivitiesPanel(CampaignActivityObservationDto observation,
            Func<CampaignActivityCommandDto,CampaignActivityEligibilityDto> evaluate,
            Action<CampaignActivityCommandDto> submit)
            :this(observation,evaluate??throw new ArgumentNullException(nameof(evaluate)),submit,null){}
        public ReviewActivitiesPanel(CampaignStreamedActivityObservationDto observation,
            Func<CampaignActivityCommandDto,CampaignActivityEligibilityDto> evaluate,
            Action<CampaignActivityCommandDto> submit)
            :this(observation.Activities,evaluate??throw new ArgumentNullException(nameof(evaluate)),submit,observation){}
        public ReviewActivitiesPanel(CampaignStreamedActivityObservationDto observation,
            Action<CampaignActivityCommandDto,string> requestReview,Action<CampaignActivityCommandDto> submit)
            :this(observation.Activities,null,submit,observation)
        {this.requestReview=requestReview??throw new ArgumentNullException(nameof(requestReview));}
        private ReviewActivitiesPanel(CampaignActivityObservationDto observation,
            Func<CampaignActivityCommandDto,CampaignActivityEligibilityDto>? evaluate,
            Action<CampaignActivityCommandDto> submit,CampaignStreamedActivityObservationDto? streamed)
        {
            this.streamed=streamed;
            this.observation=observation??throw new ArgumentNullException(nameof(observation));
            this.evaluate=evaluate;
            this.submit=submit??throw new ArgumentNullException(nameof(submit));
            actor=observation.ActorId;turn=observation.Turn;revision=observation.Revision;
            name="campaign-activities-panel";style.flexGrow=1;style.minHeight=0;style.minWidth=0;
            Text(this,"Worlds & expeditions","console-section");
            Text(this,"Week "+turn+" · "+N(observation.Credits)+" credits","console-copy","activity-observation-summary");
            var tabs=Row(this);
            Button(tabs,"World construction","activity-tab-worlds",()=>SelectPage(false));
            Button(tabs,"Expeditions & contact","activity-tab-expeditions",()=>SelectPage(true));
            pageScroll=new ScrollView(ScrollViewMode.Vertical){name="activity-scroll"};pageScroll.style.flexGrow=1;pageScroll.style.minHeight=0;
            worlds=new VisualElement{name="activity-worlds"};
            if(streamed!=null){worlds.style.flexGrow=1;worlds.style.minHeight=0;Add(worlds);Add(pageScroll);}else{Add(pageScroll);pageScroll.Add(worlds);}
            expeditions=new VisualElement{name="activity-expeditions"};pageScroll.Add(expeditions);
            BuildWorlds();BuildExpeditions();
            review=Card(this,"Order review","activity-order-review");review.style.display=DisplayStyle.None;
            review.style.flexShrink=0;review.style.maxHeight=240;
            status=Text(this,"Select an activity to review its current availability.","console-copy","activity-command-status");
            status.style.flexShrink=0;
            RegisterCallback<AttachToPanelEvent>(_=>{if(!disposed && expeditionsSelected)UpdateShipPreview();});
            RegisterCallback<DetachFromPanelEvent>(_=>ReleasePreview());
            SelectPage(false);
        }

        private static string N(long value)=>value.ToString("N0",CultureInfo.InvariantCulture);
        private static string ResearchPoints(long internalUnits)=>(internalUnits/(decimal)ResearchUnits.Scale).ToString("0.##",CultureInfo.InvariantCulture);
        private static string Friendly(string? value,string fallback)=>string.IsNullOrWhiteSpace(value)?fallback:value!;
        private static Label Text(VisualElement parent,string value,string cls="console-copy",string id="")
        {
            var text=new Label(value){name=id};text.AddToClassList(cls);text.style.whiteSpace=WhiteSpace.Normal;
            text.style.flexShrink=0;text.style.marginBottom=5;parent.Add(text);return text;
        }
        private static VisualElement Row(VisualElement parent)
        {
            var row=new VisualElement();row.style.flexDirection=FlexDirection.Row;row.style.flexWrap=Wrap.Wrap;
            row.style.flexShrink=0;row.style.marginBottom=5;parent.Add(row);return row;
        }
        private static VisualElement Card(VisualElement parent,string title,string id="")
        {
            var card=new VisualElement{name=id};card.AddToClassList("console-panel");card.style.flexShrink=0;
            card.style.paddingLeft=10;card.style.paddingRight=10;card.style.paddingTop=8;card.style.paddingBottom=8;
            card.style.marginBottom=8;card.style.minWidth=0;parent.Add(card);Text(card,title,"console-section");return card;
        }
        private static Button Button(VisualElement parent,string caption,string id,Action action)
        {
            var button=new Button(action){text=caption,name=id};button.style.minHeight=36;button.style.fontSize=16;button.style.whiteSpace=WhiteSpace.Normal;
            button.style.marginRight=6;button.style.marginBottom=4;parent.Add(button);return button;
        }
        private static DropdownField Picker(VisualElement parent,string label,IReadOnlyList<string> labels,string id,Action<int> changed)
        {
            // Stable ordinal suffixes disambiguate identical display names without
            // exposing entity IDs or looking up a hidden name catalogue.
            var choices=labels.Select((s,i)=>s+" · "+(i+1)).ToList();
            var field=new DropdownField(label,choices,choices.Count==0?-1:0){name=id};
            field.style.minHeight=30;field.style.flexShrink=0;parent.Add(field);
            field.RegisterValueChangedCallback(_=>changed(field.index));field.SetEnabled(choices.Count>0);return field;
        }
        private void SelectPage(bool expedition)
        {
            expeditionsSelected=expedition;worlds.style.display=expedition?DisplayStyle.None:DisplayStyle.Flex;
            expeditions.style.display=expedition?DisplayStyle.Flex:DisplayStyle.None;
            if(streamed!=null)pageScroll.style.display=expedition?DisplayStyle.Flex:DisplayStyle.None;
            construction?.SetActive(!expedition);
            DismissReview();
            if(streamed!=null)status.text=expedition?"Select an expedition or contact action to review it.":"Inspect a tile, or use + to add one order.";
            if(expedition && panel!=null)UpdateShipPreview();else ReleasePreview();
        }
        private ActivityOwnColonyDto? Colony=>colonyIndex>=0 && colonyIndex<observation.Colonies.Count?observation.Colonies[colonyIndex]:null;
        private ActivityOwnShipDto? Ship=>shipIndex>=0 && shipIndex<observation.Ships.Count?observation.Ships[shipIndex]:null;
        private ActivityKnownSiteDto? Site=>siteIndex>=0 && siteIndex<observation.Sites.Count?observation.Sites[siteIndex]:null;

        private void BuildWorlds()
        {
            var own=Card(worlds,"Owned world","activity-owned-world");
            if(observation.Colonies.Count==0){Text(own,"No owned world is available in this observation.");return;}
            Picker(own,"World",observation.Colonies.Select(c=>Friendly(c.DisplayName,"Owned world")).ToList(),"activity-colony-selector",i=>{colonyIndex=i;DismissReview();RefreshWorld();});
            colonyFacts=Text(own,"",id:"activity-world-economy");
            buildList=new VisualElement{name="activity-build-options"};worlds.Add(buildList);
            queueList=new VisualElement{name="activity-production-queue"};worlds.Add(queueList);
            RefreshWorld();
        }
        private void RefreshWorld()
        {
            if(Colony==null || buildList==null || queueList==null)return;
            var colony=Colony;
            colonyFacts!.text="Per week: Industry "+N(colony.Industry)+" · Research "+N(colony.Research)+" · Food "+N(colony.Food);
            buildList.Clear();queueList.Clear();
            if(streamed!=null)
            {
                construction?.Dispose();
                construction=new ReviewConstructionCatalogue(streamed,colony.ColonyId,command=>
                {
                    if(disposed||submitted)return;
                    submitted=true;submit(Copy(command));
                },id=>Review(Command(CampaignActivityCommandKind.CancelProduction,id),"Cancel this order? Completed work and consumed materials are lost. Unused local stock stays here."));
                buildList.style.flexGrow=1;buildList.style.minHeight=0;buildList.Add(construction);queueList.style.display=DisplayStyle.None;
                return;
            }
            var options=Card(buildList,"Build at this world");
            if(observation.BuildOptions.Count==0)Text(options,"No construction options are supplied.");
            for(int index=0;index<observation.BuildOptions.Count;index++)
            {
                var option=observation.BuildOptions[index];var item=Card(options,Friendly(option.DisplayName,option.Kind==ActivityProductionKind.Infrastructure?"Infrastructure":"Ready ship class"));
                Text(item,(option.Kind==ActivityProductionKind.Infrastructure?"Infrastructure":"Ship · revision "+option.BlueprintRevision)
                    +(streamed==null?" · "+N(option.Cost.Credits)+" credits":" · No normal credit charge")+" · "+N(option.Cost.Industry)+" Industry work");
                if(option.Cost.Maintenance>0)Text(item,"Maintenance: "+N(option.Cost.Maintenance));
                if(option.Cost.Resources!=null && option.Cost.Resources.Count>0)
                    Text(item,"Materials: "+string.Join(" · ",option.Cost.Resources.Select((r,i)=>(r.ResourceDefinitionId=="resource.dilithium"?"Dilithium":"Material "+(i+1))+": "+N(r.Amount))));
                if(!option.Supported)Text(item,Friendly(option.Reason,"Construction is unsupported in this campaign."));
                var colonyId=colony.ColonyId;var definition=option.DefinitionId;var blueprint=option.BlueprintRevision;
                var kind=option.Kind==ActivityProductionKind.Infrastructure?CampaignActivityCommandKind.EnqueueStructure:CampaignActivityCommandKind.EnqueueShip;
                Button(item,"Review construction","activity-build-review-"+index,()=>Review(Command(kind,colonyId,definition,blueprint),Friendly(option.DisplayName,"Construction")
                    +(streamed==null?" · "+N(option.Cost.Credits)+" credits paid now":" · Materials are consumed locally as work progresses. Supply interruptions pause work.")));
            }
            var queue=Card(queueList,streamed==null?"Paid construction queue":"Construction queue","activity-queue");
            var items=observation.Production.Where(p=>p.ColonyId==colony.ColonyId).ToList();
            if(streamed==null&&items.Count==0)Text(queue,"No paid work queued.");
            if(streamed!=null)DrawStreamedQueue(queue,colony.ColonyId);
            else for(int i=0;i<items.Count;i++)
            {
                var item=items[i];var paid=item.Paid;if(paid==null){Text(queue,"A queue receipt is unavailable.");continue;}
                var option=observation.BuildOptions.FirstOrDefault(o=>o.DefinitionId==item.DefinitionId);
                var card=Card(queue,(i==0?"Working: ":"Queued: ")+Friendly(option?.DisplayName,item.Kind==ActivityProductionKind.Infrastructure?"Infrastructure":"Ship construction"));
                Text(card,N(paid.CompletedIndustry)+" / "+N(paid.RequiredIndustry)+" Industry work · "+N(paid.PaidCredits)+" credits paid");
                var progress=new ProgressBar{lowValue=0,highValue=100,value=paid.RequiredIndustry>0?(float)Math.Min(100,(double)paid.CompletedIndustry/paid.RequiredIndustry*100):0};
                progress.style.height=14;card.Add(progress);
                var queueId=paid.QueueItemId;
                Button(card,"Review cancellation","activity-queue-cancel-"+i,()=>Review(Command(CampaignActivityCommandKind.CancelProduction,queueId),"Cancel this paid queue item"));
            }
            var structures=observation.Structures.Where(s=>s.ColonyId==colony.ColonyId).ToList();
            if(structures.Count>0)
            {
                var completed=Card(queueList,"Completed infrastructure");
                foreach(var receipt in structures)
                {
                    var option=observation.BuildOptions.FirstOrDefault(o=>o.DefinitionId==receipt.DefinitionId);
                    Text(completed,Friendly(option?.DisplayName,"Completed infrastructure")+" · week "+receipt.CompletedTurn);
                }
            }
        }

        private void DrawStreamedQueue(VisualElement queue,string colonyId)
        {
            var orders=streamed!.Production.Where(q=>q.ColonyId==colonyId).ToList();
            if(orders.Count==0)Text(queue,"No construction orders at this world.");
            var supply=streamed.Supply.SingleOrDefault(s=>s.ColonyId==colonyId);
            if(supply!=null)
            {
                Text(queue,"Local stock: "+string.Join(" · ",supply.Stocks.Select(r=>(r.ResourceDefinitionId=="resource.dilithium"?"Dilithium":r.ResourceDefinitionId)+" "+N(r.Amount))));
                foreach(var delivery in supply.Deliveries.Where(d=>d.ResourceDefinitionId!="resource.food"))
                    Text(queue,"Last delivery: "+N(delivery.Delivered)+" · "+(delivery.RouteIntegrity==0?"Route blocked or unavailable":delivery.Delivered==0?"No shipment available":"Physical stock delivered"));
            }
            for(int i=0;i<orders.Count;i++)
            {
                var order=orders[i];var option=observation.BuildOptions.FirstOrDefault(o=>o.DefinitionId==order.DefinitionId&&o.BlueprintRevision==(order.Blueprint?.Revision??0));
                var available=streamed.Availability.SingleOrDefault(a=>a.QueueItemId==order.QueueItemId);
                var item=Card(queue,Friendly(option?.DisplayName,"Construction")+" · "+(i==0?available?.Status??"Awaiting materials":"Queued"));
                Text(item,N(order.CompletedIndustry)+" / "+N(order.RequiredIndustry)+" Industry · no credits charged");
                Text(item,"Consumed: "+string.Join(" · ",order.RequiredResources!.Select(r=>(r.ResourceDefinitionId=="resource.dilithium"?"Dilithium":r.ResourceDefinitionId)+" "+N(order.ConsumedResources!.Single(c=>c.ResourceDefinitionId==r.ResourceDefinitionId).Amount)+" / "+N(r.Amount))));
                if(available!=null)Text(item,"Current stocks allow "+N(available.WorkWithCurrentStocks)+" Industry; next deliveries may change this.");
                var progress=new ProgressBar{lowValue=0,highValue=100,value=(float)(100d*order.CompletedIndustry/order.RequiredIndustry)};progress.style.height=14;item.Add(progress);
                var id=order.QueueItemId;
                Button(item,"Review cancellation","activity-queue-cancel-"+i,()=>Review(Command(CampaignActivityCommandKind.CancelProduction,id),"Cancel this order? Completed work and consumed materials are lost. Unused local stock stays here."));
            }
        }
        private void BuildExpeditions()
        {
            var own=Card(expeditions,"Expedition ship","activity-owned-ship");
            if(observation.Ships.Count>0)
                Picker(own,"Ship",observation.Ships.Select(s=>Friendly(s.DisplayName,"Owned ship")).ToList(),"activity-ship-selector",i=>{shipIndex=i;DismissReview();RefreshShip();RefreshSite();if(panel!=null && expeditionsSelected)UpdateShipPreview();});
            else Text(own,"No owned ships are available.");
            shipFacts=Text(own,"",id:"activity-ship-capabilities");
            previewImage=new Image{name="activity-ship-model",scaleMode=ScaleMode.ScaleToFit,pickingMode=PickingMode.Ignore};
            previewImage.style.height=180;previewImage.style.flexShrink=0;previewImage.style.display=DisplayStyle.None;own.Add(previewImage);
            previewCaption=Text(own,"",id:"activity-ship-art-caption");
            var orbit=Row(own);
            Button(orbit,"Rotate hull","activity-ship-orbit",()=>{if(stage!=null){yaw+=35;stage.OrbitReference(yaw,26);}});
            var known=Card(expeditions,"Known opportunities","activity-known-sites");
            if(observation.Sites.Count>0)
                Picker(known,"Site",observation.Sites.Select(s=>Friendly(s.Definition.Title,"Known opportunity")+" — "+Friendly(s.Body.DisplayName,"Observed body")).ToList(),"activity-site-selector",i=>{siteIndex=i;DismissReview();RefreshSite();});
            else Text(known,"No opportunities have been discovered. Explore the map to reveal them.");
            siteDetails=new VisualElement{name="activity-site-details"};known.Add(siteDetails);
            history=Card(expeditions,"Expedition progress & reports","activity-findings");
            RefreshShip();RefreshSite();
        }
        private void RefreshShip()
        {
            if(shipFacts==null)return;
            var ship=Ship;
            shipFacts.text=ship==null?"Select an owned expedition ship.":(ship.Stationary?"Stationary":"In transit")
                +" · powered study work "+ship.StudyWork+" / week · contact capacity "+ship.ContactCapacity;
        }
        private void RefreshSite()
        {
            if(siteDetails==null || history==null)return;
            siteDetails.Clear();history.Clear();Text(history,"Expedition progress & reports","console-section");
            var site=Site;var ship=Ship;
            if(site!=null)
            {
                Text(siteDetails,Friendly(site.Definition.DiscoveryText,"An opportunity has been observed."));
                if(site.MinorDefinition!=null)
                    Text(siteDetails,Friendly(site.MinorDefinition.DisplayName,"Observed society")+" · "+Friendly(site.MinorDefinition.SpeciesName,"Species not disclosed")
                        +(site.MinorDefinition.WarpCapable?" · warp capable":" · pre-warp")+" · relation: "+(site.Relation?.ToString()??"Observed"));
                if(ship!=null)Text(siteDetails,ship.NodeId==site.Site.NodeId && ship.Stationary?"Selected ship is at this site.":"Move the selected ship to this site using the map before starting work.");
                if(site.Choices.Count==0)Text(siteDetails,"No further choices are offered at the current relation.");
                for(int i=0;i<site.Choices.Count;i++)
                {
                    var choice=site.Choices[i];var card=Card(siteDetails,Friendly(choice.Caption,"Study opportunity"));
                    Text(card,N(choice.Credits)+" credits · "+choice.Work+" work · "+choice.Deadline+" weekly advances");
                    Text(card,"Requires study "+choice.RequiredStudy+" · contact "+choice.RequiredContact);
                    Text(card,choice.Discipline+" insight: "+ResearchPoints(choice.LearnableInsightInternalUnits)+" learnable research points"
                        +(choice.UnlearnableInsightInternalUnits>0?" · "+ResearchPoints(choice.UnlearnableInsightInternalUnits)+" research points unavailable to current research":""));
                    if(choice.HullRiskBps>0 || choice.HullDamage>0)
                        Text(card,"Hull risk "+(choice.HullRiskBps/100m).ToString("0.##",CultureInfo.InvariantCulture)+"% · potential damage "+choice.HullDamage);
                    var siteId=site.Site.SiteId;var choiceId=choice.ChoiceId;
                    var button=Button(card,"Review expedition","activity-choice-review-"+i,()=>
                    {
                        if(Ship==null)return;
                        Review(Command(CampaignActivityCommandKind.StartStudy,siteId,choiceId,ship:Ship.ShipId),Friendly(choice.Caption,"Expedition")+" · "+N(choice.Credits)+" credits paid now");
                    });
                    button.SetEnabled(ship!=null);
                }
            }
            if(observation.Findings.Count==0){Text(history,"No expedition orders or findings yet.");return;}
            for(int i=0;i<observation.Findings.Count;i++)
            {
                var finding=observation.Findings[i];
                var known=observation.Sites.FirstOrDefault(s=>s.Site.SiteId==finding.SiteId);
                var card=Card(history,Friendly(known?.Definition.Title,"Recorded expedition")+" · "+StageName(finding.Stage));
                var owner=observation.Ships.FirstOrDefault(s=>s.ShipId==finding.ShipId);
                Text(card,Friendly(owner?.DisplayName,"Expedition ship")+" · "+finding.WorkDone+" work completed · "+finding.RemainingAdvances+" advances remaining");
                Text(card,N(finding.PaidCredits)+" credits paid · started week "+finding.StartedTurn+(finding.ResolvedTurn.HasValue?" · resolved week "+finding.ResolvedTurn:""));
                if(!string.IsNullOrWhiteSpace(finding.Report))Text(card,finding.Report);
                if(!string.IsNullOrWhiteSpace(finding.ContactOutcome))Text(card,finding.ContactOutcome);
                if(finding.InsightApplied>0 || finding.InsightUnlearnable>0)
                    Text(card,"Research points applied "+ResearchPoints(finding.InsightApplied)+" · unavailable "+ResearchPoints(finding.InsightUnlearnable));
                var siteId=finding.SiteId;var choice=finding.ChoiceId;var shipId=finding.ShipId;
                if(finding.Stage==ActivityProgressStage.AwaitingClaim)
                    Button(card,"Review finding","activity-finding-claim-"+i,()=>Review(Command(CampaignActivityCommandKind.ClaimFinding,siteId,choice,ship:shipId),"Claim the completed finding"));
                if(finding.Stage==ActivityProgressStage.Active || finding.Stage==ActivityProgressStage.AwaitingClaim)
                    Button(card,"Review abort","activity-finding-abort-"+i,()=>Review(Command(CampaignActivityCommandKind.AbortStudy,siteId,choice,ship:shipId),"Abort this expedition order"));
            }
        }
        private static string StageName(ActivityProgressStage stage)
        {
            switch(stage)
            {case ActivityProgressStage.Active:return "In progress";case ActivityProgressStage.AwaitingClaim:return "Finding ready";case ActivityProgressStage.Completed:return "Completed";case ActivityProgressStage.Aborted:return "Aborted";case ActivityProgressStage.Failed:return "Failed";case ActivityProgressStage.Expired:return "Expired";default:return "Status unavailable";}
        }

        private CampaignActivityCommandDto Command(CampaignActivityCommandKind kind,string target,string definition="",int blueprint=0,string ship="")
            =>new CampaignActivityCommandDto{ActorId=actor,ExpectedTurn=turn,ExpectedRevision=revision,CommandId="command.activity."+Guid.NewGuid().ToString("N"),Kind=kind,TargetId=target,DefinitionId=definition,BlueprintRevision=blueprint,ShipId=ship};
        private static CampaignActivityCommandDto Copy(CampaignActivityCommandDto c)
            =>new CampaignActivityCommandDto{ActorId=c.ActorId,ExpectedTurn=c.ExpectedTurn,ExpectedRevision=c.ExpectedRevision,CommandId=c.CommandId,Kind=c.Kind,TargetId=c.TargetId,DefinitionId=c.DefinitionId,BlueprintRevision=c.BlueprintRevision,ShipId=c.ShipId};
        private void Review(CampaignActivityCommandDto candidate,string caption)
        {
            if(disposed || submitted)return;
            if(requestReview!=null){requestReview(Copy(candidate),caption);return;}
            CampaignActivityEligibilityDto eligibility;
            try{eligibility=evaluate!(Copy(candidate));}
            catch{DismissReview();status.text="Availability could not be checked. Refresh the campaign.";return;}
            PresentReviewedOrder(candidate,caption,eligibility);
        }
        internal void PresentReviewedOrder(CampaignActivityCommandDto candidate,string caption,CampaignActivityEligibilityDto eligibility)
        {
            if(disposed||submitted)return;
            DismissReview();review.Clear();review.style.display=DisplayStyle.Flex;Text(review,caption,"console-section");
            var exact=candidate.ActorId==actor&&candidate.ExpectedTurn==turn&&candidate.ExpectedRevision==revision&&eligibility!=null && eligibility.ActorId==actor && eligibility.Turn==turn && eligibility.Revision==revision;
            if(!exact){Text(review,"This observation is out of date. Refresh the campaign before issuing another order.");return;}
            Text(review,Friendly(eligibility!.Reason,eligibility.Accepted?"Ready to submit at the observed campaign state.":"This order is unavailable."));
            if(eligibility.IndustryAfterCompletion.HasValue || eligibility.ResearchAfterCompletion.HasValue || eligibility.FoodAfterCompletion.HasValue)
                Text(review,"After completion: Industry "+(eligibility.IndustryAfterCompletion.HasValue?N(eligibility.IndustryAfterCompletion.Value):"unavailable")
                    +" · Research "+(eligibility.ResearchAfterCompletion.HasValue?N(eligibility.ResearchAfterCompletion.Value):"unavailable")
                    +" · Food "+(eligibility.FoodAfterCompletion.HasValue?N(eligibility.FoodAfterCompletion.Value):"unavailable"));
            var controls=Row(review);
            Button(controls,"Back","activity-order-back",DismissReview);
            if(!eligibility.Accepted){status.text="This order is unavailable at the observed campaign state.";return;}
            pending=Copy(candidate);
            Button(controls,"Confirm order","activity-order-confirm",()=>
            {
                if(disposed || submitted || pending==null)return;
                var command=Copy(pending);submitted=true;pending=null;review.style.display=DisplayStyle.None;
                status.text="Order submitted for validation. Refresh the campaign to see its outcome.";
                // No automatic retry or stamp refresh; callback may replace and
                // dispose this entire panel synchronously.
                submit(command);
            }).AddToClassList("primary-action");
            status.text="Review this order, then confirm. No credits have been spent by this preview.";
        }
        private void DismissReview(){pending=null;if(review!=null)review.style.display=DisplayStyle.None;}

        private void UpdateShipPreview()
        {
            if(disposed || !expeditionsSelected || previewImage==null || previewCaption==null)return;
            var ship=Ship;
            if(ship==null || ActivityHullCatalog.Find(ship.HullVersionId)==null)
            {ReleasePreview();previewCaption.text="No exact hull art binding is available for this observed ship.";return;}
            stage??=new ShipPreviewStage(512,320);
            stage.ShowActivityHull(ship.HullVersionId);previewImage.image=stage.Texture;
            previewImage.style.display=stage.Availability==ShipPreviewAvailability.Model?DisplayStyle.Flex:DisplayStyle.None;
            previewCaption.text=stage.Label;
        }
        private void ReleasePreview()
        {
            if(previewImage!=null){previewImage.image=null;previewImage.style.display=DisplayStyle.None;}
            stage?.Dispose();stage=null;
        }
        public void Dispose(){if(disposed)return;disposed=true;pending=null;construction?.Dispose();ReleasePreview();}
    }
}
