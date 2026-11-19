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
            var context=Row(body);context.style.alignItems=Align.Center;
            var command=Text(context,"Construction Command","colonisation-construction-heading");command.style.flexGrow=1;command.style.marginBottom=0;
            command.style.fontSize=24;
            command.style.color= (Color)new Color32(240,205,165,255);
            command.style.unityFontStyleAndWeight=FontStyle.Bold;
            Picker(context,"Owned world",observation.Worlds,x=>x.Id,x=>x.Name,world,id=>{world=id;Show(page);},"colonisation-world-selector");
            var worldPicker=context.Q<DropdownField>();worldPicker.style.width=350;worldPicker.style.maxWidth=Length.Percent(100);worldPicker.labelElement.style.minWidth=90;
            var w=observation.Worlds.FirstOrDefault(x=>x.Id==world);if(w==null){Text(body,"No owned worlds observed.");return;}
            Text(body,w.Name+" \u00b7 "+w.Stage,"colonisation-world-facts");
            var readouts=Row(body);readouts.AddToClassList("construction-world-readouts");
            void Readout(string caption,long value)
            {
                var metric=new VisualElement();metric.AddToClassList("construction-world-readout");readouts.Add(metric);
                metric.style.flexDirection=FlexDirection.Row;metric.style.alignItems=Align.Center;metric.style.paddingTop=3;metric.style.paddingBottom=3;
                var key=Text(metric,caption);key.AddToClassList("construction-readout-label");key.style.fontSize=16;key.style.marginBottom=0;key.style.marginRight=8;
                var amount=Text(metric,DenseConstructionPresentation.Number(value));amount.AddToClassList("construction-readout-value");amount.style.fontSize=20;amount.style.marginBottom=0;
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
            Text(body,"Industry + local materials as work advances · ? unavailable · Select a name for details.");
            var row=Row(body);
            row.name="construction-ledger-layout";
            row.style.flexWrap=Wrap.NoWrap;
            var denseStyles=Resources.Load<StyleSheet>("DenseConstruction");if(denseStyles!=null)row.styleSheets.Add(denseStyles);
            var catalogue=new ScrollView{name="colonisation-catalogue"};
            var dock=new VisualElement{name="construction-context-dock"};
            var inspector=new ScrollView(ScrollViewMode.Vertical){name="construction-selected-hull"};
            inspector.style.height=36;inspector.style.maxHeight=Length.Percent(58);inspector.style.minHeight=0;inspector.style.flexGrow=0;inspector.style.flexShrink=1;dock.Add(inspector);
            Text(inspector,"Select a class or installation for details.","construction-inspector-hint");
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
            catalogue.style.marginRight=12;
            dock.style.flexBasis=320;dock.style.flexShrink=0;
            fitConstruction=()=>
            {
                if(!Live||page!="Worlds"||row.panel==null)return;
                var sideBySide=row.layout.width>=1040;
                var direction=sideBySide?FlexDirection.Row:FlexDirection.Column;
                row.style.flexDirection=direction;
                catalogue.style.marginRight=sideBySide?12:0;
                catalogue.style.flexBasis=sideBySide?new StyleLength(0f):new StyleLength(StyleKeyword.Auto);
                dock.style.flexBasis=sideBySide?new StyleLength(320):new StyleLength(StyleKeyword.Auto);
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
                ConstructionHeading(catalogue,ships?"Ships":"Installations · base additions / week","colonisation-section-"+kind);
                var ledger=new VisualElement{name="construction-ledger-"+kind};ledger.AddToClassList("dense-construction-ledger");catalogue.Add(ledger);
                ConstructionColumns(ledger,ships);
                foreach(var option in activities.BuildOptions.Where(x=>x.Kind==kind))
                {
                    var selected=option;
                    var title=selected.DisplayName+(selected.BlueprintRevision>0?" R"+selected.BlueprintRevision:"");
                    var queued=orders.Count(x=>x.DefinitionId==selected.DefinitionId&&x.BlueprintRevision==selected.BlueprintRevision);
                    var built=ships
                        ?observation.OwnShipSources.Count(x=>x.BlueprintId==selected.DefinitionId&&x.Revision==selected.BlueprintRevision)
                        :activities.Structures.Count(x=>x.ColonyId==w.Id&&x.DefinitionId==selected.DefinitionId);
                    var unavailable=!selected.Supported?(string.IsNullOrWhiteSpace(selected.Reason)?"Unsupported class.":selected.Reason):w.ConstructionUnavailableReason(ships);
                    if(unavailable==null&&!ships&&(built>0||queued>0))unavailable="Unique building: already built or queued.";
                    var ordinal=index;
                    var line=ConstructionLine(ledger,"construction-ledger-row-"+ordinal);
                    var identity=ConstructionCell(line,"class",0);
                    ConstructionGlyph(identity,ships,selected.DefinitionId);
                    var inspect=Button(identity,title,ships?"colonisation-inspect-"+(ordinal+1):"colonisation-installation-inspect-"+ordinal,()=>
                    {
                        foreach(var peer in catalogue.Query(className:"dense-construction-row").ToList())peer.RemoveFromClassList("dense-construction-selected");line.AddToClassList("dense-construction-selected");
                        inspectedBlueprint=selected.DefinitionId;inspectedRevision=selected.BlueprintRevision;inspectedOpen=true;
                        if(ships)InspectHull(inspector,selected);else InspectInstallation(inspector,selected);
                    });
                    inspect.AddToClassList("dense-construction-name");inspect.style.flexGrow=1;inspect.style.minWidth=0;inspect.style.marginRight=0;inspect.style.marginBottom=0;inspect.style.height=44;inspect.style.whiteSpace=WhiteSpace.NoWrap;inspect.style.textOverflow=TextOverflow.Ellipsis;
                    inspect.tooltip=title+(unavailable==null?" · inspect details":" · "+unavailable);
                    if(unavailable!=null)line.AddToClassList("dense-construction-unavailable");
                    ConstructionValue(line,DenseConstructionPresentation.Number(selected.Cost.Industry),"industry",64,"Industry workload: "+DenseConstructionPresentation.Number(selected.Cost.Industry)+"; consumed gradually with local materials.");
                    ConstructionValue(line,DenseConstructionPresentation.Materials(selected.Cost.Resources),"materials",80,DenseConstructionPresentation.MaterialDetails(selected.Cost.Resources,w.Stocks));
                    ConstructionValue(line,built+" / "+queued,"counts",60,(ships?"Owned ships":"Built here")+": "+built+"; queued at "+w.Name+": "+queued+".",ships?"Own / Q":"Built / Q");
                    var facts=ConstructionCell(line,"capabilities",238);facts.name="colonisation-ship-capabilities-"+ordinal;
                    if(ships)
                    {
                        ConstructionStat(facts,DenseConstructionPresentation.Number(selected.Comparison?.StudyWork),"Study",42,"Supported study work; ? means unavailable.");
                        ConstructionStat(facts,DenseConstructionPresentation.Number(selected.Comparison?.ContactCapacity),"Contact",46,"Supported contact capacity; ? means unavailable.");
                        ConstructionStat(facts,DenseConstructionPresentation.Number(selected.Comparison?.MaxHull),"Hull",44,"Published maximum hull; not live ship condition. ? means unavailable.");
                        ConstructionStat(facts,DenseConstructionPresentation.Number(selected.Comparison?.ShieldCapacity),"Shields",44,"Supported shield capacity; zero stays zero, ? means unavailable.");
                        ConstructionStat(facts,DenseConstructionPresentation.Arms(selected.Comparison),"Arms",62,"Weapon packages; Unarmed means a supported empty weapon list. Powered state and exact weapon details are in Inspect.");
                        facts.tooltip=ProductionComparisonPresentation.Summary(selected.Comparison);
                    }
                    else
                    {
                        var benefit=observation.BuildingBenefits.FirstOrDefault(b=>b.DefinitionId==selected.DefinitionId);
                        ConstructionStat(facts,benefit==null?"?":DenseConstructionPresentation.Delta(benefit.FoodOutput),"Food",79,"Base food addition per week; actual output depends on power and politics.");
                        ConstructionStat(facts,benefit==null?"?":DenseConstructionPresentation.Delta(benefit.Industry),"Ind",79,"Base Industry addition per week; actual output depends on power and politics.");
                        ConstructionStat(facts,benefit==null?"?":DenseConstructionPresentation.Delta(benefit.Research),"Res",80,"Base Research addition per week; actual output depends on power and politics.");
                    }
                    var actions=ConstructionCell(line,"action",68);
                    var add=Button(actions,"+ Add","colonisation-build-"+index++,()=>
                    {
                        var c=Activity(ships?CampaignActivityCommandKind.EnqueueShip:CampaignActivityCommandKind.EnqueueStructure,w.Id);
                        c.DefinitionId=selected.DefinitionId;
                        c.BlueprintRevision=selected.BlueprintRevision;
                        SendActivity(c);
                    });
                    add.tooltip="Add "+title+" at "+w.Name;
                    ConstructionPrimary(add);
                    add.AddToClassList("dense-construction-add");add.style.marginRight=0;add.style.marginBottom=0;add.style.width=64;add.style.height=44;
                    add.SetEnabled(selected.Supported&&unavailable==null);
                    if(unavailable!=null){add.tooltip=unavailable;inspect.tooltip+=" · Add unavailable";}
                    if(inspectedBlueprint==selected.DefinitionId&&inspectedRevision==selected.BlueprintRevision)
                    {line.AddToClassList("dense-construction-selected");if(ships)InspectHull(inspector,selected);else InspectInstallation(inspector,selected);}
                }
                ledger.RegisterCallback<GeometryChangedEvent>(e=>{if(ReferenceEquals(e.target,ledger))FitConstructionLedger(ledger,e.newRect.width<720);});
            }

            ConstructionHeading(catalogue,"Special projects","colonisation-section-projects");
            var kit=ConstructionLine(catalogue,"construction-kit-row");
            var kitName=ConstructionCell(kit,"class",0);ConstructionGlyph(kitName,false,"resource.settlement-kit");var kitLabel=Text(kitName,"Settlement kit");kitLabel.style.marginBottom=0;kitLabel.style.fontSize=16;
            ConstructionValue(kit,DenseConstructionPresentation.Number(observation.KitIndustry),"industry",64,"Industry workload.");
            var kitCost=observation.KitResources.Select(r=>new ResourceAmountDto{ResourceDefinitionId=r.ResourceDefinitionId,Amount=r.Amount}).ToArray();
            ConstructionValue(kit,DenseConstructionPresentation.Materials(kitCost),"materials",94,DenseConstructionPresentation.MaterialDetails(kitCost,w.Stocks));
            var kitAction=ConstructionCell(kit,"action",100);
            var addKit=Button(kitAction,"+ Kit","colonisation-build-kit",()=>
            {
                var c=Command(ColonisationAction.EnqueueKit);
                c.EnqueueKit=new EnqueueSettlementKitDto{ColonyId=w.Id};
                Send(c);
            });
            ConstructionPrimary(addKit);addKit.style.marginBottom=0;addKit.style.marginRight=0;
            var kitUnavailable=w.ConstructionUnavailableReason(false);
            addKit.SetEnabled(kitUnavailable==null);
            addKit.tooltip=kitUnavailable??"Add one settlement kit at "+w.Name;
            kit.RegisterCallback<GeometryChangedEvent>(e=>{if(!ReferenceEquals(e.target,kit))return;var narrow=e.newRect.width<460;kit.style.height=narrow?new StyleLength(StyleKeyword.Auto):new StyleLength(52);kit.style.flexWrap=narrow?Wrap.Wrap:Wrap.NoWrap;kitName.style.flexBasis=narrow?new StyleLength(Length.Percent(100)):new StyleLength(0f);});

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

        private static void ConstructionGlyph(VisualElement parent,bool ship,string definition)
        {
            VisualElement glyph;
            if(ship)
            {
                // Original Art strokes: art/playtest-20261118/icon-strokes.json,
                // reused by Nov19 reference8bd9c2. Generic vessel, never a class statistic.
                glyph=new VisualElement();glyph.generateVisualContent+=context=>
                {
                    var p=context.painter2D;p.strokeColor=glyph.resolvedStyle.color;p.lineWidth=2;
                    void Line(params float[] xy){p.BeginPath();for(var i=0;i<xy.Length;i+=2){var point=new Vector2(xy[i],xy[i+1]);if(i==0)p.MoveTo(point);else p.LineTo(point);}p.Stroke();}
                    Line(3,19,7,13,11,13,11,7,15,4,19,7,19,13,23,13,27,19,21,21,9,21,3,19);Line(7,13,7,6);Line(23,13,23,6);Line(12,24,18,24);
                };
            }
            else
            {
                var art=definition=="resource.settlement-kit"?ConstructionCardArt.Kit():ConstructionCardArt.Building(definition);
                glyph=art!=null?new Image{image=art,scaleMode=ScaleMode.ScaleToFit}:new ReviewIcon(ReviewIconKind.Building,"Installation");
            }
            glyph.pickingMode=PickingMode.Ignore;glyph.style.width=30;glyph.style.height=30;glyph.style.flexShrink=0;glyph.style.marginRight=4;glyph.style.color=new Color(.73f,.67f,.9f);parent.Add(glyph);
        }
        private static VisualElement ConstructionLine(VisualElement parent,string id)
        {
            var line=new VisualElement{name=id};line.AddToClassList("dense-construction-row");
            line.style.flexDirection=FlexDirection.Row;line.style.alignItems=Align.Center;line.style.flexShrink=0;line.style.minWidth=0;
            line.style.height=52;line.style.minHeight=52;line.style.paddingLeft=4;line.style.paddingRight=4;line.style.marginBottom=2;
            parent.Add(line);return line;
        }
        private static VisualElement ConstructionCell(VisualElement line,string role,float width)
        {
            var cell=new VisualElement();cell.AddToClassList("dense-construction-cell");cell.AddToClassList("dense-construction-"+role);
            cell.style.flexDirection=FlexDirection.Row;cell.style.alignItems=Align.Center;cell.style.minWidth=0;cell.style.flexShrink=0;
            if(width==0){cell.style.flexGrow=1;cell.style.flexBasis=0;cell.style.minWidth=140;}else cell.style.width=width;
            line.Add(cell);return cell;
        }
        private static void ConstructionStat(VisualElement parent,string value,string caption,float width,string tooltip)
        {
            var stat=new VisualElement();stat.style.flexDirection=FlexDirection.Column;stat.style.alignItems=Align.Stretch;stat.style.minWidth=0;
            if(width>0){stat.style.width=width;stat.style.flexShrink=0;}else stat.style.flexGrow=1;
            parent.Add(stat);stat.tooltip=tooltip;
            var label=Text(stat,value);label.AddToClassList("dense-construction-value");label.style.marginBottom=0;label.style.fontSize=16;
            label.style.whiteSpace=WhiteSpace.NoWrap;label.style.textOverflow=TextOverflow.Ellipsis;label.style.unityTextAlign=TextAnchor.MiddleRight;
            var key=Text(stat,caption);key.AddToClassList("dense-construction-key");key.style.fontSize=14;key.style.marginBottom=0;key.style.display=DisplayStyle.None;key.style.unityTextAlign=TextAnchor.MiddleRight;
        }
        private static void ConstructionValue(VisualElement line,string value,string role,float width,string tooltip,string? caption=null)
        {var cell=ConstructionCell(line,role,width);ConstructionStat(cell,value,caption??(role=="industry"?"Industry":role=="materials"?"Materials":role),0,tooltip);}
        private static void ConstructionColumns(VisualElement ledger,bool ships)
        {
            var head=ConstructionLine(ledger,"construction-ledger-head-"+(ships?"ships":"installations"));head.AddToClassList("dense-construction-columns");head.style.height=28;head.style.minHeight=28;
            void Heading(string caption,string role,float width,string tooltip="")
            {var cell=ConstructionCell(head,role,width);var text=Text(cell,caption);text.style.fontSize=14;text.style.marginBottom=0;text.style.flexGrow=1;text.style.whiteSpace=WhiteSpace.NoWrap;text.style.unityTextAlign=role=="class"?TextAnchor.MiddleLeft:TextAnchor.MiddleRight;text.tooltip=tooltip;}
            Heading(ships?"CLASS · inspect":"INSTALLATION · inspect","class",0);
            Heading("Industry","industry",64);Heading("Materials","materials",80,"Total required; hover a row for required / local stock.");
            Heading(ships?"Own / Q":"Built / Q","counts",60,ships?"Owned ships / queued here":"Built here / queued here");
            var facts=ConstructionCell(head,"capabilities",238);
            foreach(var entry in ships?new[]{("Study",42f),("Contact",46f),("Hull",44f),("Shields",44f),("Arms",62f)}:new[]{("Food",79f),("Ind",79f),("Res",80f)})
            {var label=Text(facts,entry.Item1);label.style.width=entry.Item2;label.style.fontSize=14;label.style.marginBottom=0;label.style.unityTextAlign=TextAnchor.MiddleRight;label.tooltip=ships?"Supported published class; ? means unavailable.":"Base addition per week, before power/politics/deposit conditions.";}
            Heading("Order","action",68);
        }
        private static void FitConstructionLedger(VisualElement ledger,bool narrow)
        {
            ledger.EnableInClassList("dense-construction-narrow",narrow);
            foreach(var line in ledger.Children())
            {
                if(line.ClassListContains("dense-construction-columns")){line.style.display=narrow?DisplayStyle.None:DisplayStyle.Flex;continue;}
                line.style.height=narrow?new StyleLength(StyleKeyword.Auto):new StyleLength(52);line.style.minHeight=narrow?100:52;
                line.style.flexWrap=narrow?Wrap.Wrap:Wrap.NoWrap;
                var identity=line.Children().First(c=>c.ClassListContains("dense-construction-class"));
                identity.style.flexBasis=narrow?new StyleLength(Length.Percent(100)):new StyleLength(0f);
                foreach(var key in line.Query<Label>(className:"dense-construction-key").ToList())key.style.display=narrow?DisplayStyle.Flex:DisplayStyle.None;
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
            Text(card,"Base additions / week: Food "+DenseConstructionPresentation.Delta(benefit.FoodOutput)+", Industry "+DenseConstructionPresentation.Delta(benefit.Industry)+
                ", Research "+DenseConstructionPresentation.Delta(benefit.Research)+".",
                "colonisation-building-effects-"+definitionId);
            Text(card,"Power supply "+DenseConstructionPresentation.Delta(benefit.PowerSupply)+" · power demand "+DenseConstructionPresentation.Delta(benefit.PowerDemand),"colonisation-building-power-"+definitionId);
            Text(card,"Logistics capacity "+benefit.LogisticsCapacity+" · Shipyard "+(benefit.Shipbuilding?"Yes":"No"));
            if(benefit.Extraction.Count>0)Text(card,"Base extraction/week: "+Materials(benefit.Extraction)+"; requires finite deposits.");
            Text(card,"Actual output depends on power, politics and deposits.");
        }

        private void ReleaseHull(){hullPreview?.Dispose();hullPreview=null;}
        private void InspectInstallation(VisualElement parent,ActivityBuildOptionDto option)
        {
            var old=currentInspector;currentInspector=null;ReleaseHull();old?.RemoveFromHierarchy();
            parent.Q("construction-inspector-hint")?.RemoveFromHierarchy();
            var fold=new Foldout{text=option.DisplayName,value=inspectedOpen,name="colonisation-hull-inspector"};fold.style.flexShrink=0;parent.Add(fold);currentInspector=fold;
            parent.style.height=fold.value?new StyleLength(Length.Percent(58)):new StyleLength(36);
            var epoch=controlEpoch;fold.RegisterValueChangedCallback(_=>{if(Live&&epoch==controlEpoch&&ReferenceEquals(currentInspector,fold)){inspectedOpen=fold.value;parent.style.height=fold.value?new StyleLength(Length.Percent(58)):new StyleLength(36);}});
            ConstructionSelectedFacts(fold,option,false);
            ConstructionEffects(fold,option.DefinitionId);
            var reason=!option.Supported?option.Reason:observation.Worlds.FirstOrDefault(w=>w.Id==world)?.ConstructionUnavailableReason(false);
            if(!string.IsNullOrWhiteSpace(reason))Text(fold,reason!);
        }
        private void ConstructionSelectedFacts(VisualElement parent,ActivityBuildOptionDto option,bool ship)
        {
            Text(parent,DenseConstructionPresentation.Number(option.Cost.Industry)+" Industry · "+DenseConstructionPresentation.Materials(option.Cost.Resources));
            var w=observation.Worlds.First(x=>x.Id==world);
            Text(parent,DenseConstructionPresentation.MaterialDetails(option.Cost.Resources,w.Stocks));
            var built=ship?observation.OwnShipSources.Count(x=>x.BlueprintId==option.DefinitionId&&x.Revision==option.BlueprintRevision):activities.Structures.Count(x=>x.ColonyId==world&&x.DefinitionId==option.DefinitionId);
            var queued=observation.Production.Count(x=>x.ColonyId==world&&x.DefinitionId==option.DefinitionId&&x.BlueprintRevision==option.BlueprintRevision);
            Text(parent,(ship?"Owned ships ":"Built here ")+built+" · Queued here "+queued);
            if(!ship&&(built>0||queued>0))Text(parent,"Unique building: already built or queued.");
        }
        private void InspectHull(VisualElement parent,ActivityBuildOptionDto option)
        {
            var oldInspector=currentInspector;
            currentInspector=null;ReleaseHull();oldInspector?.RemoveFromHierarchy();
            parent.Q("construction-inspector-hint")?.RemoveFromHierarchy();
            var fold=new Foldout{text="Inspect "+option.DisplayName+" R"+option.BlueprintRevision,value=inspectedOpen,name="colonisation-hull-inspector"};fold.style.flexShrink=0;fold.style.fontSize=16;parent.Add(fold);
            parent.style.height=fold.value?new StyleLength(Length.Percent(58)):new StyleLength(36);
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
            fold.RegisterValueChangedCallback(_=>{if(!OwnInspector())return;inspectedOpen=fold.value;parent.style.height=fold.value?new StyleLength(Length.Percent(58)):new StyleLength(36);Refresh();});fold.RegisterCallback<AttachToPanelEvent>(_=>Refresh());
            Button(fold,"Rotate hull","colonisation-hull-orbit",()=>{if(OwnInspector())hullPreview?.OrbitReference(80,26);});
            ConstructionSelectedFacts(fold,option,true);
            Text(fold,"Published class capabilities");
            foreach(var fact in ProductionComparisonPresentation.Details(option.Comparison))Text(fold,fact);
            // An inspector added to an attached page has already received its attach event.
            if(fold.panel!=null)Refresh();
        }
    }
}
