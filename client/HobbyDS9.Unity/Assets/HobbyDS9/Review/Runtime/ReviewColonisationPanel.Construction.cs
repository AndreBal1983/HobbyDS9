#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;
using HobbyDS9.ArtDemo;
using UnityEngine;
using UnityEngine.UIElements;
namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewColonisationPanel
    {
        private void Worlds()
        {
            var command=Text(body,"Construction Command","colonisation-construction-heading");
            command.style.fontSize=24;
            command.style.color= (Color)new Color32(240,205,165,255);
            command.style.unityFontStyleAndWeight=FontStyle.Bold;
            Picker(body,"Owned world",observation.Worlds,x=>x.Id,x=>x.Name,world,id=>{world=id;Show(page);},"colonisation-world-selector");
            var w=observation.Worlds.FirstOrDefault(x=>x.Id==world);if(w==null){Text(body,"No owned worlds observed.");return;}
            Text(body,w.Name+" \u00b7 "+w.Stage,"colonisation-world-facts");
            var readouts=Row(body);readouts.AddToClassList("construction-world-readouts");
            void Readout(string caption,long value)
            {
                var metric=new VisualElement();metric.AddToClassList("construction-world-readout");readouts.Add(metric);
                Text(metric,caption).AddToClassList("construction-readout-label");
                Text(metric,value.ToString()).AddToClassList("construction-readout-value");
            }
            Readout("Population",w.Population);Readout("Industry / week",w.Industry);
            Readout("Research / week",w.Research);Readout("Food output / week",w.FoodOutput);
            Text(body,"Local stocks · Food "+w.Food+" · "+(Materials(w.Stocks)==""?"no other materials":Materials(w.Stocks)),"construction-local-stocks");
            var balance=w.LocalFoodBalance.HasValue?(w.LocalFoodBalance.Value>=0?"+":"")+w.LocalFoodBalance.Value:"unknown";
            Text(body,w.FoodDemand.HasValue?"Residents need "+w.FoodDemand.Value+" food/week; local balance "+balance+" before imports/exports.":"Resident food demand unavailable.","colonisation-food-balance");
            var preparation=new Foldout{text="Prepare a settlement expedition",value=false,name="colonisation-preparation-guide"};preparation.style.flexShrink=0;body.Add(preparation);
            if(w.Stage==ColonyEstablishmentStage.Established)Text(preparation,"Consecutive food-shortage weeks: "+w.ShortageWeeks+". Deliveries depend on actual surplus, routes and capacity.","colonisation-food-shortage");
            Text(preparation,"Choose a populated source world separately from your industrial yard. Check food output against resident demand before loading voyage food. Planetary buildings show their base effects below.");
            Text(preparation,"At a food-deficit yard, consider a food-producing building before a long ship order. Check its material cost and base effects below. Materials and work are consumed gradually; extra food output begins after actual completion.");
            Text(preparation,"Build a transport at a shipyard and a settlement kit at the source world, then bring the empty transport there. In Transports, review the actual source cohort, remaining residents, local kit and voyage food. Survey a suitable known planet, travel there and review deployment; retain food for establishment and later supply trips. Travel and deliveries are not guaranteed safe.");
            if(w.Stage==ColonyEstablishmentStage.Founding)Text(body,"Establishment: "+w.EstablishmentWork+" work, "+w.FedWeeks+" fed weeks. Shortage "+w.ShortageWeeks+" weeks; deaths "+w.Deaths+". "+w.Report);
            if(w.Stage==ColonyEstablishmentStage.EmptyOutpost)Text(body,"Empty outpost: no production or freight. A surveyed, supplied settlement expedition must recommission it.");
            Text(body,"Builds use Industry and local materials as work advances.");
            var row=Row(body);
            row.style.flexWrap=Wrap.NoWrap;
            var catalogue=new ScrollView{name="colonisation-catalogue"};
            var dock=new VisualElement{name="construction-context-dock"};
            var inspector=new VisualElement{name="construction-selected-hull"};dock.Add(inspector);
            var queue=new ScrollView{name="colonisation-queue"};
            queue.style.flexGrow=1;queue.style.flexBasis=0;queue.style.minHeight=120;queue.style.minWidth=0;dock.Add(queue);
            foreach(var column in new VisualElement[]{catalogue,dock})
            {
                column.style.flexBasis=0;
                column.style.minWidth=0;
                column.style.height=420;
                row.Add(column);
            }
            catalogue.style.flexGrow=1;
            catalogue.style.marginRight=16;
            dock.style.flexBasis=380;dock.style.flexShrink=0;
            fitConstruction=()=>
            {
                if(!Live||page!="Worlds"||row.panel==null)return;
                var sideBySide=row.layout.width>=760;
                var direction=sideBySide?FlexDirection.Row:FlexDirection.Column;
                row.style.flexDirection=direction;
                catalogue.style.marginRight=sideBySide?16:0;
                catalogue.style.flexBasis=sideBySide?new StyleLength(0f):new StyleLength(StyleKeyword.Auto);
                dock.style.flexBasis=sideBySide?new StyleLength(380):new StyleLength(StyleKeyword.Auto);
                var available=Mathf.Clamp(body.contentViewport.layout.height-row.layout.y-12,320,680);
                if(Mathf.Abs(catalogue.style.height.value.value-(sideBySide?available:420))>.5f)catalogue.style.height=sideBySide?available:420;
                if(Mathf.Abs(dock.style.height.value.value-(sideBySide?available:520))>.5f)dock.style.height=sideBySide?available:520;
                catalogue.style.flexShrink=sideBySide?1:0;
                dock.style.flexShrink=0;
            };
            row.RegisterCallback<GeometryChangedEvent>(_=>fitConstruction?.Invoke());

            var orders=observation.Production.Where(x=>x.ColonyId==w.Id).ToArray();
            ConstructionHeading(queue,"Build queue · "+orders.Length+(orders.Length==1?" order":" orders"),"colonisation-queue-heading");
            int index=0;
            foreach(var kind in new[]{ActivityProductionKind.PublishedShip,ActivityProductionKind.Infrastructure})
            {
                var ships=kind==ActivityProductionKind.PublishedShip;
                ConstructionHeading(catalogue,ships?"Ships":"Planetary buildings","colonisation-section-"+kind);
                var cards=Row(catalogue);
                cards.RegisterCallback<GeometryChangedEvent>(e=>
                {
                    var useGrid=e.newRect.width>=620;
                    var width=Length.Percent(useGrid?48:100);
                    foreach(var card in cards.Children())
                    {card.style.width=width;card.style.marginRight=useGrid?6:0;}
                });
                foreach(var option in activities.BuildOptions.Where(x=>x.Kind==kind))
                {
                    var selected=option;
                    // Ship study thumbnails can resemble exploded parts. Exact live hull inspection
                    // belongs in the shared dock; never enlarge those old pixels into a hero.
                    var art=ships?null:ConstructionCardArt.Building(selected.DefinitionId);
                    var title=selected.DisplayName+(selected.BlueprintRevision>0?" R"+selected.BlueprintRevision:"");
                    var card=ConstructionCard(cards,title,art);
                    card.AddToClassList("construction-option-card");
                    card.style.marginRight=6;
                    card.style.width=Length.Percent(100);
                    Text(card,selected.Cost.Industry+" Industry");
                    var materials=selected.Cost.Resources;
                    Text(card,"Materials: "+(materials==null||materials.Count==0?"none":string.Join(", ",
                        materials.Select(r=>MaterialName(r.ResourceDefinitionId)+" "+r.Amount))));

                    var queued=orders.Count(x=>x.DefinitionId==selected.DefinitionId&&x.BlueprintRevision==selected.BlueprintRevision);
                    var built=ships
                        ?observation.OwnShipSources.Count(x=>x.BlueprintId==selected.DefinitionId&&x.Revision==selected.BlueprintRevision)
                        :activities.Structures.Count(x=>x.ColonyId==w.Id&&x.DefinitionId==selected.DefinitionId);
                    Text(card,ships?"Owned ships: "+built+" · queued here: "+queued:"Built here: "+built+" · queued here: "+queued);
                    if(ships)Text(card,ProductionComparisonPresentation.Summary(selected.Comparison),"colonisation-ship-capabilities-"+index);

                    var unavailable=!selected.Supported?selected.Reason:w.ConstructionUnavailableReason(ships);
                    if(unavailable==null&&!ships&&(built>0||queued>0))unavailable="Unique building: already built or queued.";
                    var actions=Row(card);
                    var add=Button(actions,"+ Add","colonisation-build-"+index++,()=>
                    {
                        var c=Activity(ships?CampaignActivityCommandKind.EnqueueShip:CampaignActivityCommandKind.EnqueueStructure,w.Id);
                        c.DefinitionId=selected.DefinitionId;
                        c.BlueprintRevision=selected.BlueprintRevision;
                        SendActivity(c);
                    });
                    add.tooltip="Add "+title+" at "+w.Name;
                    ConstructionPrimary(add);
                    add.SetEnabled(selected.Supported&&unavailable==null);
                    if(unavailable!=null)Text(card,unavailable);
                    if(ships)
                    {
                        var inspect=Button(actions,"Inspect hull","colonisation-inspect-"+index,()=>
                        {
                            inspectedBlueprint=selected.DefinitionId;
                            inspectedRevision=selected.BlueprintRevision;
                            inspectedOpen=true;
                            InspectHull(inspector,selected);
                        });
                        inspect.tooltip="Inspect "+title+" · exact published hull";
                        if(inspectedBlueprint==selected.DefinitionId&&inspectedRevision==selected.BlueprintRevision)
                            InspectHull(inspector,selected);
                    }
                    else
                    {
                        var effects=new Foldout{text="Effects & conditions",value=false};card.Add(effects);
                        ConstructionEffects(effects,selected.DefinitionId);
                    }
                }
            }

            ConstructionHeading(catalogue,"Special projects","colonisation-section-projects");
            var kit=ConstructionCard(catalogue,"Settlement kit",ConstructionCardArt.Kit());
            kit.AddToClassList("construction-option-card");
            Text(kit,observation.KitIndustry+" Industry · "+ConstructionMaterials(observation.KitResources));
            var addKit=Button(kit,"+ Settlement kit","colonisation-build-kit",()=>
            {
                var c=Command(ColonisationAction.EnqueueKit);
                c.EnqueueKit=new EnqueueSettlementKitDto{ColonyId=w.Id};
                Send(c);
            });
            ConstructionPrimary(addKit);
            var kitUnavailable=w.ConstructionUnavailableReason(false);
            addKit.SetEnabled(kitUnavailable==null);
            if(kitUnavailable!=null)Text(kit,kitUnavailable);

            ConstructionHeading(catalogue,"System improvements","colonisation-section-system");
            Text(catalogue,"No system improvements are available in this playtest.");

            if(orders.Length==0)Text(queue,"No queued orders.");
            for(var i=0;i<orders.Length;i++)
            {
                var order=orders[i];
                var artwork=order.Kind switch
                {
                    ColonisationProductionKind.PublishedShip => null,
                    ColonisationProductionKind.Infrastructure => ConstructionCardArt.Building(order.DefinitionId),
                    ColonisationProductionKind.SettlementKit => ConstructionCardArt.Kit(),
                    _ => null,
                };
                var card=ConstructionCard(queue,order.Name,artwork);
                card.AddToClassList("construction-queue-row");
                if(i==0)card.style.borderLeftColor= (Color)new Color32(239,179,107,255);
                Text(card,(i+1)+" · "+order.Status);
                Text(card,"Industry progress: "+order.CompletedIndustry+" / "+order.RequiredIndustry);
                if(order.RequiredIndustry>0)
                    card.Add(new ProgressBar{lowValue=0,highValue=order.RequiredIndustry,value=order.CompletedIndustry});
                var materialsDetail=new Foldout{text="Materials & cancellation",value=false};card.Add(materialsDetail);
                Text(materialsDetail,"Materials required: "+ConstructionMaterials(order.Required));
                Text(materialsDetail,"Materials consumed: "+ConstructionMaterials(order.Consumed));
                Text(materialsDetail,"Cancel discards work and consumed materials.");
                var cancel=Button(card,"Cancel order","colonisation-queue-cancel-"+order.Id,
                    ()=>SendActivity(Activity(CampaignActivityCommandKind.CancelProduction,order.Id)));
                cancel.tooltip="Cancel construction now. Work and consumed materials are not refunded.";
            }
        }

        private static void ConstructionPrimary(Button button)
        {
            button.AddToClassList("primary-action");
            button.style.backgroundColor= (Color)new Color32(239,179,107,255);
            button.style.color= (Color)new Color32(17,24,32,255);
        }

        private static void ConstructionHeading(VisualElement parent,string title,string name)
        {
            var heading=Text(parent,title,name);
            heading.style.color= (Color)new Color32(175,162,228,255);
            heading.style.unityFontStyleAndWeight=FontStyle.Bold;
            heading.style.marginTop=8;
        }

        private static VisualElement ConstructionCard(VisualElement parent,string title,Texture2D? artwork)
        {
            var card=new VisualElement();
            card.AddToClassList("console-panel");
            card.style.flexShrink=0;
            card.style.minWidth=0;
            card.style.marginBottom=8;
            card.style.backgroundColor= (Color)new Color32(17,32,47,255);
            parent.Add(card);
            var header=Row(card);
            if(artwork!=null)
            {
                var image=new Image{image=artwork,scaleMode=ScaleMode.ScaleToFit,pickingMode=PickingMode.Ignore};
                image.style.width=78;
                image.style.height=54;
                image.style.marginRight=8;
                header.Add(image);
            }
            var label=Text(header,title);
            label.style.maxWidth=Length.Percent(100);
            label.style.unityFontStyleAndWeight=FontStyle.Bold;
            return card;
        }

        private static string ConstructionMaterials(IEnumerable<ResourceAmountSnapshot> materials)
        {
            var text=Materials(materials);
            return text.Length==0?"none":text;
        }

        private void ConstructionEffects(VisualElement card,string definitionId)
        {
            var benefit=observation.BuildingBenefits.FirstOrDefault(b=>b.DefinitionId==definitionId);
            if(benefit==null){Text(card,"Building effects unavailable.");return;}
            Text(card,"Base building effects: Food +"+benefit.FoodOutput+", Industry +"+benefit.Industry+
                ", Research +"+benefit.Research+", power supply +"+benefit.PowerSupply+", power demand +"+benefit.PowerDemand+".",
                "colonisation-building-effects-"+definitionId);
            if(benefit.Extraction.Count>0)Text(card,"Base extraction/week: "+Materials(benefit.Extraction)+"; requires finite deposits.");
            Text(card,"Actual output depends on power, politics and deposits.");
        }

        private void ReleaseHull(){hullPreview?.Dispose();hullPreview=null;}
        private void InspectHull(VisualElement parent,ActivityBuildOptionDto option)
        {
            var oldInspector=currentInspector;
            currentInspector=null;ReleaseHull();oldInspector?.RemoveFromHierarchy();
            var fold=new Foldout{text="Inspect "+option.DisplayName+" R"+option.BlueprintRevision,value=inspectedOpen,name="colonisation-hull-inspector"};fold.style.flexShrink=0;fold.style.fontSize=16;parent.Add(fold);
            currentInspector=fold;var inspectorEpoch=controlEpoch;
            bool OwnInspector()=>Live&&inspectorEpoch==controlEpoch&&ReferenceEquals(currentInspector,fold);
            var image=new Image{name="colonisation-hull-image",scaleMode=ScaleMode.ScaleToFit};image.style.height=180;image.style.minHeight=180;image.style.flexShrink=0;fold.Add(image);
            image.tooltip="Exact published hull presentation. No statistics or unlocks are supplied by this image.";
            var label=Text(fold,"Expand to inspect exact hull art.");
            void Refresh()
            {
                if(!OwnInspector())return;
                if(!fold.value||fold.panel==null){image.image=null;ReleaseHull();return;}
                if(ActivityHullCatalog.Find(option.HullVersionId)==null){label.text="No exact hull art binding is available.";return;}
                ReleaseHull();hullPreview=new ShipPreviewStage(512,320);hullPreview.ShowActivityHull(option.HullVersionId);
                image.image=hullPreview.Availability==ShipPreviewAvailability.Model?hullPreview.Texture:null;
                label.text=hullPreview.Availability==ShipPreviewAvailability.Model&&!hullPreview.Label.Contains("missing art")
                    ?ActivityHullCatalog.Find(option.HullVersionId).Label:hullPreview.Label;
                label.tooltip=hullPreview.Label;
            }
            fold.RegisterValueChangedCallback(_=>{if(!OwnInspector())return;inspectedOpen=fold.value;Refresh();});fold.RegisterCallback<AttachToPanelEvent>(_=>Refresh());
            Button(fold,"Rotate hull","colonisation-hull-orbit",()=>{if(OwnInspector())hullPreview?.OrbitReference(80,26);});
            Text(fold,"Published class capabilities");
            foreach(var fact in ProductionComparisonPresentation.Details(option.Comparison))Text(fold,fact);
            // An inspector added to an attached page has already received its attach event.
            if(fold.panel!=null)Refresh();
        }
    }
}
