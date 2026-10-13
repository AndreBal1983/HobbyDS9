#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
namespace HobbyDS9.UnityClient
{
    // Progressive presentation; the existing draft and Application own editing/preview.
    public sealed class ShipDesignerView : VisualElement, IDisposable
    {
        public ShipDesignDraft Draft { get; }
        public Button ResetButton { get; }
        public Button CloseButton { get; }
        public string SelectedSystem { get; private set; }="Overview";
        public static readonly string[] Systems={"Overview","Hull","Power","Propulsion","Crew","Command","Sensors","Science","Shields","Weapons","Support","Profiles","Validation"};
        private static readonly string[] Captions={"Overview","Hull","Power","Propulsion","Crew / Life support","Command / Computing","Sensors","Science","Shields","Weapons","Mission / Support","Operating profiles","Cost / Validation"};
        private readonly VisualElement workspace=new VisualElement(), confirmation=new VisualElement(), navigation=new VisualElement();
        private readonly Label state=new LcarsReadout(), capacity=new LcarsReadout(), cost=new LcarsReadout(), hullCaption=new LcarsReadout();
        private readonly TextField className;
        private readonly DropdownField templates, roles;
        private readonly DropdownField systemPicker;
        private readonly ShipDesignerGraphics? graphics;
        private readonly CampaignDesigner? campaign;
        private readonly Action? campaignChanged;
        private readonly Action? restartWorkspace;
        private readonly Action<HobbyDS9.Contracts.EntityId>? selectActor;
        private Button publication=null!;
        private string? selectedComponent, replacementSource;
        private string? renderedSystem, renderedComponent;
        private bool comparisonOpen;
        private string? selectedPowerComponent;
        private readonly Dictionary<string,LcarsPowerSlider[]> powerEditors=new Dictionary<string,LcarsPowerSlider[]>();
        public ShipDesignerView(ShipDesignDraft draft,Action closeRequested,ShipDesignerGraphics? graphicCatalogue=null,
            CampaignDesigner? campaign=null,Action? campaignChanged=null,Action? restartWorkspace=null,
            Action<HobbyDS9.Contracts.EntityId>? selectActor=null)
        {
            this.campaign=campaign;this.campaignChanged=campaignChanged;
            this.restartWorkspace=restartWorkspace;this.selectActor=selectActor;
            Draft=draft; graphics=graphicCatalogue??Resources.Load<ShipDesignerGraphics>("ShipDesignerGraphics");
            name="ship-designer";AddToClassList("ship-designer");
            var header=new VisualElement();header.AddToClassList("sd-shell");Add(header);
            var identity=new VisualElement();identity.AddToClassList("sd-identity");header.Add(identity);
            className=new TextField("Class") {name="designer-name",isDelayed=true};identity.Add(className);className.RegisterValueChangedCallback(e=>Draft.Rename(e.newValue));
            roles=new DropdownField("Role",draft.Provider.Roles.Select(r=>r.Caption).ToList(),0) {name="designer-role"};identity.Add(roles);
            roles.RegisterValueChangedCallback(_=>Draft.SetRole(Draft.Provider.Roles[roles.index].Id));
            templates=new DropdownField("Template",draft.Provider.Templates.Select(t=>t.Caption).ToList(),0) {name="designer-template"};identity.Add(templates);
            templates.RegisterValueChangedCallback(_=>{if(campaign==null)Draft.RequestTemplate(Draft.Provider.Templates[templates.index].Id);else {campaign.NewClass();Refresh();}});
            var actions=Buttons(header);actions.AddToClassList("sd-actions");
            state.name="designer-state";actions.Add(state);capacity.name="designer-capacity";actions.Add(capacity);cost.name="designer-cost-summary";actions.Add(cost);actions.Add(hullCaption);
            ResetButton=new LcarsPillButton("Reset",Draft.RequestReset) {name="designer-reset"};actions.Add(ResetButton);
            CloseButton=new LcarsPillButton("Back",()=>RequestLeave(closeRequested)) {name="designer-close"};actions.Add(CloseButton);
            actions.Add(new LcarsPillButton("Review issues",()=>SelectSystem("Validation")) {name="designer-global-issues"});
            confirmation.name="designer-confirmation";confirmation.AddToClassList("confirmation");Add(confirmation);confirmation.Add(new LcarsReadout("Discard this modified local draft?"));
            var confirms=Buttons(confirmation);confirms.Add(new LcarsPillButton("Keep edits",Draft.CancelDiscard) {name="designer-keep"});confirms.Add(new LcarsPillButton("Discard / continue",Draft.ConfirmDiscard) {name="designer-discard"});
            var body=new VisualElement {name="designer-body"};body.AddToClassList("sd-body");Add(body);
            var nav=new ScrollView(ScrollViewMode.Vertical) {name="designer-system-navigation"};nav.AddToClassList("sd-nav-scroll");body.Add(nav);navigation.AddToClassList("sd-navigation");nav.Add(navigation);
            for(var i=0;i<Systems.Length;i++){var id=Systems[i];navigation.Add(new LcarsPillButton(Captions[i],()=>SelectSystem(id)) {name="designer-system-"+id.ToLowerInvariant()});}
            systemPicker=new DropdownField("System",Captions.ToList(),0) {name="designer-system-picker"};systemPicker.AddToClassList("sd-system-picker");body.Add(systemPicker);
            systemPicker.RegisterValueChangedCallback(_=>SelectSystem(Systems[systemPicker.index]));
            workspace.name="designer-workspace";workspace.AddToClassList("sd-workspace");body.Add(workspace);
            var provenance=Buttons(this);provenance.AddToClassList("sd-provenance");provenance.Add(new LcarsReadout(draft.Provider.StatusLabel) {name="designer-provider-status"});
            publication=new LcarsPillButton("Blueprint publication integration pending",()=> {
                if(campaign==null || !campaign.CanPublish)return;
                publication.SetEnabled(false);
                try {var result=campaign.Publish();SfxDirector.Cue(SfxActionFeedback.For(result));}
                finally {Refresh();campaignChanged?.Invoke();}
            }) {name="designer-construction"};provenance.Add(publication);
            if(campaign!=null)provenance.Add(new LcarsPillButton("Blueprint history / refresh",()=>SelectSystem("Validation")) {name="designer-blueprints"});
            if(campaign!=null)
            {
                var restart=new LcarsPillButton("Discard old workspace / use loaded campaign",()=>Draft.RequestDiscard(()=>restartWorkspace?.Invoke())) {name="designer-use-loaded-campaign"};
                provenance.Add(restart);
            }
            Draft.Changed+=Refresh;RegisterCallback<GeometryChangedEvent>(e=>{ApplyLogicalWidth(e.newRect.width);ApplyLogicalHeight(e.newRect.height);});ApplyLogicalWidth(1700);Refresh();
        }
        public void Dispose()=>Draft.Changed-=Refresh;
        public void RefreshCampaignState()
        {
            publication.SetEnabled(campaign?.CanPublish??false);
            if(campaign!=null) { publication.text=campaign.EditingRevision.HasValue?"Publish revision "+campaign.NextRevision:"Publish new blueprint";
                publication.tooltip=campaign.SessionReplaced?campaign.Notice:campaign.BlueprintId+" / revision "+campaign.NextRevision+" • backend validates the complete fit";
                this.Q<Label>("designer-provider-status").text=campaign.SessionReplaced?"PREVIOUS CAMPAIGN DRAFT / publication blocked":Draft.Provider.StatusLabel; }
            var restart=this.Q<Button>("designer-use-loaded-campaign");if(restart!=null)restart.style.display=campaign?.SessionReplaced==true?DisplayStyle.Flex:DisplayStyle.None;
        }
        public void RequestLeave(Action leave)=>Draft.RequestDiscard(leave);
        public void ApplyLogicalWidth(float width)
        {
            if(width<=0||float.IsNaN(width)||float.IsInfinity(width))return;
            var changed=ClassListContains("sd-narrow")!=(width<1450);
            EnableInClassList("sd-narrow",width<1450);EnableInClassList("sd-wide",width>=2200);
            if(changed && SelectedSystem=="Profiles" && powerEditors.Count>0)Refresh();
        }
        private void ApplyLogicalHeight(float height)
        {
            if(height<=0||float.IsNaN(height)||float.IsInfinity(height))return;
            var changed=ClassListContains("sd-short")!=(height<600);
            EnableInClassList("sd-short",height<600);
            if(changed && SelectedSystem=="Profiles" && powerEditors.Count>0)Refresh();
        }
        public void SelectSystem(string id){if(!Systems.Contains(id))throw new ArgumentException("Unknown system",nameof(id));SelectedSystem=id;selectedComponent=null;replacementSource=null;Refresh();}
        private static string Caption(string id)=>Captions[Array.IndexOf(Systems,id)];
        private static VisualElement Buttons(VisualElement parent){var e=new VisualElement();e.AddToClassList("sd-buttons");parent.Add(e);return e;}
        private static ScrollView Scroll(string id){var e=new ScrollView(ScrollViewMode.Vertical) {name=id};e.AddToClassList("sd-scroll");return e;}
        private static LcarsInspectorBlock Block(VisualElement parent,string title){var e=new LcarsInspectorBlock(title);e.AddToClassList("sd-block");parent.Add(e);return e;}
        private static void Metrics(VisualElement parent,string title,IEnumerable<DesignerMetric> metrics){var b=Block(parent,title);foreach(var m in metrics)b.Row(m.Caption,m.Value);}
        private static string Status(DesignerStatus s)=>s==DesignerStatus.HardError?"HARD ERROR":s==DesignerStatus.NotEvaluated?"NOT EVALUATED":s.ToString().ToUpperInvariant();
        private static void StatusClass(VisualElement e,DesignerStatus s){e.EnableInClassList("sd-error",s==DesignerStatus.HardError);e.EnableInClassList("sd-warning",s==DesignerStatus.Warning);e.EnableInClassList("sd-valid",s==DesignerStatus.Valid);e.EnableInClassList("sd-unavailable",s==DesignerStatus.NotEvaluated);}
        private void Refresh()
        {
            var focused=panel?.focusController.focusedElement as VisualElement;
            var trackFocus=focused?.name?.StartsWith("track-power-",StringComparison.Ordinal)==true?focused.name:null;
            var offsets=new Dictionary<string,Vector2>();
            if(renderedSystem==SelectedSystem)
                foreach(var scroll in workspace.Query<ScrollView>().ToList())
                    if(scroll.name!="designer-component-detail" || renderedComponent==selectedComponent)offsets[scroll.name]=scroll.scrollOffset;
            var fit=Draft.Current;var p=Draft.Preview();className.SetValueWithoutNotify(fit.Name);roles.SetValueWithoutNotify(Draft.Provider.Roles.FirstOrDefault(r=>r.Id==fit.Role)?.Caption??fit.Role);templates.SetValueWithoutNotify(Draft.Provider.Templates.FirstOrDefault(t=>t.Id==Draft.TemplateId)?.Caption??(campaign!=null && !campaign.EditingRevision.HasValue?"Empty campaign fit":"Local revision copy"));
            hullCaption.text=Draft.Provider.Hulls.FirstOrDefault(h=>h.Id==fit.HullId)?.Caption??fit.HullId;state.text=Status(p.Status)+(Draft.Modified?" / EDITED":campaign?.EditingRevision!=null?" / PUBLISHED COPY":" / TEMPLATE");StatusClass(state,p.Status);
            RefreshCampaignState();
            capacity.text=p.UsedCapacity.HasValue?$"Capacity {p.UsedCapacity} / {p.TotalCapacity}":"Capacity not evaluated";cost.text=Metric(p.Costs,"Construction");ResetButton.SetEnabled(Draft.Modified);
            confirmation.style.display=Draft.ConfirmationPending?DisplayStyle.Flex:DisplayStyle.None;this.Q<Button>("designer-global-issues").text=p.Issues.Count+" issues";
            foreach(var id in Systems)navigation.Q<Button>("designer-system-"+id.ToLowerInvariant()).EnableInClassList("sd-selected",id==SelectedSystem);
            systemPicker.SetValueWithoutNotify(Caption(SelectedSystem));
            workspace.Clear();workspace.EnableInClassList("sd-profile-page",SelectedSystem=="Profiles");workspace.Add(new LcarsSectionHeader(Caption(SelectedSystem).ToUpperInvariant()) {name="designer-workspace-title"});
            if(SelectedSystem=="Overview")Overview(p);else if(SelectedSystem=="Hull")Hull(p);else if(SelectedSystem=="Profiles")Profiles(p);else if(SelectedSystem=="Validation")Validation(p);else SystemPage(p);
            renderedSystem=SelectedSystem;renderedComponent=selectedComponent;
            foreach(var scroll in workspace.Query<ScrollView>().ToList())
                if(offsets.TryGetValue(scroll.name,out var offset))scroll.schedule.Execute(()=>scroll.scrollOffset=offset);
            if(trackFocus!=null){var track=workspace.Q<VisualElement>(trackFocus);if(track!=null)track.schedule.Execute(()=>track.Focus());}
        }
        private ShipSystemGraphic Graphic(string id)=>new ShipSystemGraphic(id,Caption(id),graphics!);
        private IEnumerable<DesignerMetric> Headlines(DesignerPreview p,string system)
        {
            if(system=="Overview")
            {
                return new[]{new DesignerMetric("Crew required / embarked",Metric(p.Capabilities,"Required / embarked crew")),new DesignerMetric("Hull / movement",Metric(p.Capabilities,"Compiled hull / movement")),
                    new DesignerMetric("Powered shields",p.Capabilities.FirstOrDefault(m=>m.Caption=="ShieldCapacity")?.Value??"No powered output"),
                    new DesignerMetric("Weapons / long · medium · close",string.Join(" / ",new[]{"LongRangeAttack","MediumRangeAttack","CloseRangeAttack"}.Select(k=>p.Capabilities.FirstOrDefault(m=>m.Caption==k)?.Value??"—"))),
                    new DesignerMetric("Sensors / science",string.Join(" / ",new[]{"Sensors","Science"}.Select(k=>p.Capabilities.FirstOrDefault(m=>m.Caption==k)?.Value??"—")))};
            }
            bool Relevant(string c)=>system=="Overview"?new[]{"Required / embarked crew","Compiled hull / movement","ShieldCapacity","Sensors","Science","LongRangeAttack","MediumRangeAttack","CloseRangeAttack"}.Contains(c):
                system=="Weapons"?c.Contains("Attack")||c.Contains("Accuracy")||c.Contains("mount"):system=="Shields"?c.Contains("Shield"):system=="Propulsion"?c.Contains("Movement")||c.Contains("Propulsion")||c.Contains("Evasion")||c.Contains("movement"):
                system=="Crew"?c.Contains("Crew")||c.Contains("crew"):system=="Command"?c.Contains("Computer"):system=="Sensors"?c=="Sensors":system=="Science"?c=="Science":c=="OperationalRange";
            return p.Capabilities.Where(m=>Relevant(m.Caption));
        }
        private void Overview(DesignerPreview p)
        {
            var s=Scroll("designer-overview");workspace.Add(s);var stage=new VisualElement();stage.AddToClassList("sd-overview-stage");s.Add(stage);
            var art=new VisualElement();art.AddToClassList("sd-overview-art");stage.Add(art);art.Add(Graphic("Overview"));art.Add(new LcarsReadout("Illustrative side view / powered performance shown alongside"));
            var facts=new VisualElement();facts.AddToClassList("sd-overview-facts");stage.Add(facts);Metrics(facts,"SHIP AT A GLANCE / "+Draft.Profile,Headlines(p,"Overview"));Metrics(facts,"POWER",p.Power);Metrics(facts,"CONSTRUCTION / UPKEEP",p.Costs);
            foreach(var i in p.Issues.Take(2))Issue(s,i);if(p.Issues.Count>2)s.Add(new LcarsReadout("See Cost / Validation for all "+p.Issues.Count+" issues."));Comparison(s,p,true);
        }
        private void Hull(DesignerPreview p)
        {
            var s=Scroll("designer-hull-page");workspace.Add(s);s.Add(Graphic("Hull"));var hull=new DropdownField("Hull / version",Draft.Provider.Hulls.Select(h=>h.Caption).ToList(),0) {name="designer-hull"};s.Add(hull);
            hull.SetValueWithoutNotify(Draft.Provider.Hulls.Single(h=>h.Id==Draft.Current.HullId).Caption);hull.RegisterValueChangedCallback(_=>Draft.SetHull(Draft.Provider.Hulls[hull.index].Id));s.Add(new LcarsReadout(Draft.Provider.Hulls.Single(h=>h.Id==Draft.Current.HullId).Facts));
            if(p.UsedCapacity.HasValue&&p.TotalCapacity.HasValue)s.Add(new LcarsMeter("Used capacity",p.UsedCapacity.Value,p.TotalCapacity.Value));Context(s,p);Comparison(s,p,true);
        }
        private void SystemPage(DesignerPreview p)
        {
            var relevant=Draft.Provider.Catalogue.Where(c=>c.SystemId==SelectedSystem).ToList();if(!relevant.Any(c=>c.Id==selectedComponent))selectedComponent=relevant.FirstOrDefault()?.Id;
            var split=new VisualElement();split.AddToClassList("sd-focus-columns");workspace.Add(split);var centre=Scroll("designer-system-content");centre.AddToClassList("sd-system-content");split.Add(centre);centre.Add(Graphic(SelectedSystem));
            if(SelectedSystem=="Crew"){var crew=new LongField("Embarked crew") {name="designer-crew",isDelayed=true};crew.SetValueWithoutNotify(Draft.Current.EmbarkedCrew);crew.RegisterValueChangedCallback(e=>Draft.SetCrew(e.newValue));centre.Add(crew);}
            Metrics(centre,"POWERED PERFORMANCE / "+Draft.Profile,SelectedSystem=="Power"?p.Power:Headlines(p,SelectedSystem));
            var list=Block(centre,"PACKAGES / "+SelectedSystem.ToUpperInvariant());list.name="designer-component-list";
            foreach(var c in relevant)
            {
                var qty=Draft.Current.Installed.TryGetValue(c.Id,out var q)?q:0;var row=new VisualElement {name="component-row-"+c.Id};row.AddToClassList("sd-component-row");list.Add(row);
                var select=new LcarsPillButton(c.Name+"  × "+qty,()=>{selectedComponent=c.Id;Refresh();}) {name="select-component-"+c.Id};select.EnableInClassList("sd-selected",c.Id==selectedComponent);row.Add(select);
                var provider=Draft.Provider as IShipDesignCatalogueFacts;if(provider!=null)foreach(var m in provider.ComponentFacts(c.Id,Draft.Profile).Where(m=>new[]{"Capacity / crew","Credits / industry","Full / minimum power"}.Contains(m.Caption)))row.Add(new LcarsDataRow(m.Caption,m.Value));
                row.Add(new LcarsReadout(provider==null?"Availability from preview":provider.Available(c.Id)?"Available • compatibility checked by preview":"Version unavailable"));
            }
            if(relevant.Count==0)list.Add(new LcarsReadout("No packages in this catalogue."));Context(centre,p);Comparison(centre,p,false);
            var detail=Scroll("designer-component-detail");detail.AddToClassList("sd-detail");split.Add(detail);if(selectedComponent!=null)Component(detail,relevant.Single(c=>c.Id==selectedComponent),p);
        }
        private void Component(VisualElement detail,DesignerComponent item,DesignerPreview p)
        {
            var fit=Draft.Current;var count=fit.Installed.TryGetValue(item.Id,out var q)?q:0;detail.Add(new LcarsSectionHeader(item.Name));detail.Add(new LcarsReadout(Caption(SelectedSystem)+" package / "+Draft.Profile+" performance") {name="designer-component-description"});
            var facts=(Draft.Provider as IShipDesignCatalogueFacts)?.ComponentFacts(item.Id,Draft.Profile);
            var countRow=Block(detail,"INSTALLED");countRow.Row("Quantity",count.ToString());var controls=Buttons(detail);
            var add=new LcarsPillButton("Add",()=>Draft.Add(item.Id)) {name="designer-add"};add.SetEnabled((Draft.Provider as IShipDesignCatalogueFacts)?.Available(item.Id)??true);controls.Add(add);
            var remove=new LcarsPillButton("Remove",()=>Draft.Remove(item.Id)) {name="remove-"+item.Id};remove.SetEnabled(count>0);controls.Add(remove);
            var minus=new LcarsPillButton("−1",()=>Draft.SetQuantity(item.Id,count-1)) {name="quantity-minus-"+item.Id};minus.SetEnabled(count>0);controls.Add(minus);var plus=new LcarsPillButton("+1",()=>Draft.Add(item.Id)) {name="quantity-plus-"+item.Id};plus.SetEnabled(add.enabledSelf);controls.Add(plus);
            var installed=Draft.Provider.Catalogue.Where(c=>c.SystemId==SelectedSystem&&fit.Installed.ContainsKey(c.Id)).ToList();
            if(installed.Count>0)
            {
                if(!installed.Any(c=>c.Id==replacementSource))replacementSource=installed[0].Id;var selector=new DropdownField("Replace installed",installed.Select(c=>c.Name).ToList(),installed.FindIndex(c=>c.Id==replacementSource)) {name="designer-replacement-source"};detail.Add(selector);
                selector.RegisterValueChangedCallback(_=>{replacementSource=installed[selector.index].Id;Refresh();});var replace=new LcarsPillButton("Replace with this",()=>Draft.Replace(replacementSource!,item.Id)) {name="designer-replace"};replace.SetEnabled(replacementSource!=item.Id&&add.enabledSelf);detail.Add(replace);
            }
            if(facts!=null)Metrics(detail,"PACKAGE / "+Draft.Profile,facts);else detail.Add(new LcarsReadout(item.Facts));
            var proposed=Draft.PreviewCopy();proposed.Add(item.Id);var next=proposed.Preview();var delta=Block(detail,"IF YOU ADD ONE");delta.name="designer-proposed-preview";
            delta.Row("Capacity",(p.UsedCapacity?.ToString()??"—")+" → "+(next.UsedCapacity?.ToString()??"—"));delta.Row("Allocated power",Metric(p.Power,"Actual allocation")+" → "+Metric(next.Power,"Actual allocation"));delta.Row("Construction",Metric(p.Costs,"Construction")+" → "+Metric(next.Costs,"Construction"));delta.Row("Design status",Status(p.Status)+" → "+Status(next.Status));
            detail.Add(new LcarsReadout("New or replaced packages start unpowered. Set explicit allocations in Operating profiles."));foreach(var issue in p.Issues.Where(i=>i.SystemId==item.Id))Issue(detail,issue);
        }
        private static string Metric(IEnumerable<DesignerMetric> values,string key)=>values.FirstOrDefault(m=>m.Caption==key)?.Value??"Not evaluated";
        private void Profiles(DesignerPreview p)
        {
            this.Q<Label>("designer-workspace-title").text="OPERATING PROFILE / "+Draft.Profile.ToString().ToUpperInvariant();
            var narrow=ClassListContains("sd-narrow")||ClassListContains("sd-short");
            var buttons=Buttons(workspace);buttons.AddToClassList("sd-profile-actions");
            var caption=new Label("OPERATING PROFILE / "+Draft.Profile.ToString().ToUpperInvariant());caption.AddToClassList("sd-profile-caption");buttons.Add(caption);
            foreach(DesignerProfile mode in Enum.GetValues(typeof(DesignerProfile))){var b=new LcarsPillButton(mode.ToString(),()=>Draft.SelectProfile(mode)) {name="designer-profile-"+mode.ToString().ToLowerInvariant()};b.EnableInClassList("sd-selected",Draft.Profile==mode);buttons.Add(b);}
            var totals=new VisualElement {name="designer-power-totals"};totals.AddToClassList("sd-power-totals");if(narrow)buttons.Add(totals);else workspace.Add(totals);
            totals.Add(new Label("AVAILABLE / REQUESTED  "+Metric(p.Power,"Available / requested")+"     ALLOCATED  "+Metric(p.Power,"Actual allocation")));
            // No numeric excess is projected; expose the backend's budget issues.
            var global=p.Issues.Where(i=>i.SystemId=="" && (!i.Profile.HasValue || i.Profile==Draft.Profile)).ToList();
            totals.Add(new Label(global.Any(i=>i.Severity==DesignerStatus.HardError)?"! BACKEND ERROR • review bank validation":"EXCESS: backend issues"));
            var scale=new LcarsPowerScale(p.PowerRanges.Select(r=>r.Maximum).DefaultIfEmpty(0).Max(),narrow?128:240,narrow?4:5);
            totals.Add(new Label("COMMON SCALE • "+scale.Legend));
            var s=Scroll("designer-profile-results");s.horizontalScrollerVisibility=ScrollerVisibility.Hidden;s.contentContainer.style.width=Length.Percent(100);workspace.Add(s);
            var bank=new VisualElement {name="designer-power-bank"};bank.AddToClassList("sd-power-bank");s.Add(bank);
            powerEditors.Clear();
            foreach(var item in Draft.Provider.Catalogue.Where(c=>Draft.Current.Installed.ContainsKey(c.Id)))
            {
                var row=new VisualElement {name="power-tile-"+item.Id};row.AddToClassList("sd-profile-row");bank.Add(row);
                var title=new Button(()=>{SfxDirector.Cue(LcarsSfx.ButtonPress);ShowPowerDetails(item.Id,p);}) {text=item.Name+" × "+Draft.Current.Installed[item.Id],name="select-power-"+item.Id,tooltip="Select "+item.Name+" exact editor"};
                title.AddToClassList("sd-instrument-title");row.Add(title);
                var power=Draft.Current.Power(item.Id,Draft.Profile);var fields=Buttons(row);
                var range=p.PowerRanges.FirstOrDefault(r=>r.ComponentId==item.Id);
                var issues=p.Issues.Where(i=>i.SystemId==item.Id && (!i.Profile.HasValue || i.Profile==Draft.Profile)).ToList();
                var invalid=issues.Concat(global).Any(i=>i.Severity==DesignerStatus.HardError);
                if(range!=null && range.Maximum>=0)
                {
                    var requested=new LcarsPowerSlider("Requested","power-requested-"+item.Id,power.RequestedPower,range.Maximum,invalid,v=>Draft.SetPower(item.Id,v,Draft.Current.Power(item.Id,Draft.Profile).AllocatedPower),range.PoweredMinimum,scale);
                    var allocated=new LcarsPowerSlider("Allocated","power-allocated-"+item.Id,power.AllocatedPower,range.Maximum,invalid,v=>Draft.SetPower(item.Id,Draft.Current.Power(item.Id,Draft.Profile).RequestedPower,v),range.PoweredMinimum,scale);
                    fields.Add(requested);fields.Add(allocated);powerEditors.Add(item.Id,new[]{requested,allocated});
                    foreach(var editor in new[]{requested,allocated})editor.Track.RegisterCallback<FocusInEvent>(_=>ShowPowerDetails(item.Id,p));
                    row.Add(new Label("MAX "+range.Maximum+(scale.Paired?"":" • ≈ common scale")));
                }
                else
                {
                    var requested=new LongField("Requested") {name="power-requested-"+item.Id,isDelayed=true};requested.SetValueWithoutNotify(power.RequestedPower);var allocated=new LongField("Allocated") {name="power-allocated-"+item.Id,isDelayed=true};allocated.SetValueWithoutNotify(power.AllocatedPower);
                    requested.RegisterValueChangedCallback(e=>Draft.SetPower(item.Id,e.newValue,Draft.Current.Power(item.Id,Draft.Profile).AllocatedPower));allocated.RegisterValueChangedCallback(e=>Draft.SetPower(item.Id,Draft.Current.Power(item.Id,Draft.Profile).RequestedPower,e.newValue));fields.Add(requested);fields.Add(allocated);
                    row.Add(new LcarsReadout("Range unavailable • exact input."));
                }
                if(issues.Any(i=>i.Severity==DesignerStatus.HardError)){var error=new Label("! "+issues.Count(i=>i.Severity==DesignerStatus.HardError)+" ERRORS • select to review");error.AddToClassList("sd-error");row.Add(error);}
            }
            foreach(var issue in global)Issue(s,issue);
            var profileIssues=p.Issues.Where(i=>i.SystemId!="" && (!i.Profile.HasValue || i.Profile==Draft.Profile)).ToList();
            var validation=new Foldout {name="designer-bank-validation",text="BACKEND VALIDATION • "+profileIssues.Count(i=>i.Severity==DesignerStatus.HardError)+" errors / "+profileIssues.Count(i=>i.Severity==DesignerStatus.Warning)+" warnings",value=false};s.Add(validation);
            foreach(var issue in profileIssues)Issue(validation,issue);
            Metrics(s,"RESULTING CAPABILITIES",Headlines(p,"Overview"));Context(s,p);
            var detail=Scroll("designer-power-detail");detail.AddToClassList("sd-power-detail");
            if(narrow){s.Insert(1,detail);}else workspace.Add(detail);
            if(selectedPowerComponent==null || !powerEditors.ContainsKey(selectedPowerComponent))selectedPowerComponent=powerEditors.Keys.FirstOrDefault();
            if(selectedPowerComponent!=null)ShowPowerDetails(selectedPowerComponent,p);
        }
        private void ShowPowerDetails(string id,DesignerPreview p)
        {
            var detail=workspace.Q<ScrollView>("designer-power-detail");
            if(detail==null || !powerEditors.TryGetValue(id,out var editors))return;
            // Move existing fields, never duplicate a value editor or recreate tracks
            // while pointer capture is active.
            foreach(var field in detail.Query<LongField>().ToList())
            {
                var control=workspace.Q<LcarsPowerSlider>("slider-"+field.name);
                control?.Q<VisualElement>(className:"lps-exact-store").Add(field);
            }
            selectedPowerComponent=id;detail.Clear();
            foreach(var pair in powerEditors)workspace.Q<VisualElement>("power-tile-"+pair.Key).EnableInClassList("sd-power-selected",pair.Key==id);
            var item=Draft.Provider.Catalogue.Single(c=>c.Id==id);var range=p.PowerRanges.Single(r=>r.ComponentId==id);
            detail.Add(new LcarsSectionHeader(item.Name+" / "+Draft.Profile+" • MAX "+range.Maximum+" • POWERED MIN "+range.PoweredMinimum));
            var fields=Buttons(detail);fields.Add(editors[0].Exact);fields.Add(editors[1].Exact);
            detail.Add(new LcarsReadout((editors[0].Scale.Paired?"Either bar in a pair = one whole power unit.":"Coarse bars round upwards to the common scale; use exact entry for smaller changes.")+" Zero strip = 0. Scrub; release to validate. Arrows ±1 • Home 0 • End max. Exact input commits on Return/focus loss; invalid values retained."));
            var installation=Draft.Current.Power(id,Draft.Profile).InstallationId;
            detail.Add(new LcarsReadout("Backend output: "+Metric(p.Capabilities,installation)));
            foreach(var issue in p.Issues.Where(i=>(i.SystemId==id || i.SystemId=="") && (!i.Profile.HasValue || i.Profile==Draft.Profile)))Issue(detail,issue);
        }
        private void Validation(DesignerPreview p)
        {
            var s=Scroll("designer-validation");workspace.Add(s);if(campaign!=null)CampaignHistory(s);Metrics(s,"TOTAL COST / UPKEEP",p.Costs);var b=Block(s,"COMPLETE VALIDATION / "+Status(p.Status));if(p.Issues.Count==0)b.Add(new LcarsReadout("No validation issues."));
            foreach(var issue in p.Issues){Issue(b,issue);var system=IssueSystem(issue);b.Add(new LcarsPillButton("Review "+Caption(system),()=>SelectSystem(system)) {name="issue-route-"+b.childCount});}Comparison(s,p,true);
        }
        private void CampaignHistory(VisualElement parent)
        {
            var b=Block(parent,"CAMPAIGN BLUEPRINTS / IMMUTABLE REVISIONS");
            var humans=campaign!.Observed.Campaign.Civilizations.Where(c=>c.ControllerKind==HobbyDS9.Contracts.ControllerKind.Human).ToList();
            if(selectActor!=null && humans.Count>1)
            {
                var actor=new DropdownField("Human actor",humans.Select(c=>c.CivilizationId.Value).ToList(),humans.FindIndex(c=>c.CivilizationId==campaign.Actor)) {name="designer-actor"};
                actor.SetEnabled(!campaign.SessionReplaced);actor.RegisterValueChangedCallback(_=>Draft.RequestDiscard(()=>selectActor(humans[actor.index].CivilizationId)));b.Add(actor);
            }
            b.Add(new LcarsReadout(campaign!.Notice) {name="designer-command-feedback"});
            b.Row("Local target",campaign.BlueprintId+" / next revision "+campaign.NextRevision);
            b.Row("Observed campaign","Turn "+campaign.Observed.Campaign.TurnNumber+" / state "+campaign.Observed.Campaign.StateRevision);
            var buttons=Buttons(b);
            var refresh=new LcarsPillButton("Refresh campaign",()=> {campaign.Refresh();Refresh();}) {name="designer-refresh-campaign"};refresh.SetEnabled(!campaign.SessionReplaced);buttons.Add(refresh);
            var create=new LcarsPillButton("New class",()=> {campaign.NewClass();Refresh();}) {name="designer-new-class"};create.SetEnabled(!campaign.SessionReplaced);buttons.Add(create);
            foreach(var record in campaign.History)
            {
                var blueprint=record.Blueprint;var cost=blueprint.Performance.Cost!;
                b.Add(new LcarsReadout(blueprint.Fit.DisplayName+" • "+blueprint.BlueprintId+" / revision "+blueprint.Revision+(record.IsLatestRevision?" / latest":" / historical")));
                b.Add(new LcarsReadout(cost.Credits+" credits • "+cost.Industry+" Industry • "+string.Join(", ",cost.Resources.Select(r=>r.Amount+" "+r.ResourceDefinitionId))));
                var edit=new LcarsPillButton("Edit copy of revision "+blueprint.Revision,()=> {campaign.Edit(record);Refresh();}) {name="designer-edit-"+blueprint.BlueprintId+"-r"+blueprint.Revision};
                edit.SetEnabled(!campaign.SessionReplaced && campaign.CanEdit(record));
                edit.tooltip=campaign.CanEdit(record)?"Stored revision remains immutable":"Separate stacks or quantities above Int32 require an expanded editor; this revision remains constructible.";
                b.Add(edit);
            }
            if(campaign.History.Count==0)b.Add(new LcarsReadout("No published blueprints. Build a complete fit and publish; construction orders belong to Production."));
        }
        public string IssueSystem(DesignerIssue issue)
        {
            var item=Draft.Provider.Catalogue.FirstOrDefault(c=>c.Id==issue.SystemId);if(item!=null&&Systems.Contains(item.SystemId))return item.SystemId;var t=issue.Explanation.ToLowerInvariant();
            if(t.Contains("mount")||t.Contains("unarmed"))return "Weapons";if(t.Contains("computer")||t.Contains("command"))return "Command";if(t.Contains("crew")||t.Contains("life support"))return "Crew";if(t.Contains("propulsion"))return "Propulsion";
            if(t.Contains("capacity")||t.Contains("hull")||t.Contains("envelope"))return "Hull";if(t.Contains("allocation")||t.Contains("profile"))return "Profiles";if(t.Contains("power")||t.Contains("reactor"))return "Power";return "Validation";
        }
        private static void Issue(VisualElement parent,DesignerIssue issue){var e=new LcarsReadout(Status(issue.Severity)+(issue.Profile.HasValue?" / "+issue.Profile:"")+" • "+issue.Explanation);StatusClass(e,issue.Severity);parent.Add(e);}
        private void Context(VisualElement parent,DesignerPreview p){var issues=p.Issues.Where(i=>IssueSystem(i)==SelectedSystem).ToList();if(issues.Count==0)return;var b=Block(parent,"THIS SYSTEM / VALIDATION");foreach(var issue in issues)Issue(b,issue);}
        private void Comparison(VisualElement parent,DesignerPreview p,bool full)
        {
            var f=new Foldout {name="designer-comparison",text="Compared with starting design",value=comparisonOpen};parent.Add(f);f.RegisterValueChangedCallback(e=>comparisonOpen=e.newValue);var start=Draft.Preview(true);f.Add(new LcarsDataRow("Capacity",(start.UsedCapacity?.ToString()??"—")+" → "+(p.UsedCapacity?.ToString()??"—")));
            foreach(var c in Draft.Provider.Catalogue.Where(c=>full||c.SystemId==SelectedSystem)){var a=Draft.Starting.Installed.TryGetValue(c.Id,out var x)?x:0;var b=Draft.Current.Installed.TryGetValue(c.Id,out var y)?y:0;if(a!=b)f.Add(new LcarsDataRow(c.Name,a+" → "+b));}
            foreach(var m in (full?Headlines(start,"Overview"):Headlines(start,SelectedSystem)).Concat(start.Costs))f.Add(new LcarsDataRow(m.Caption,m.Value+" → "+Metric((full?Headlines(p,"Overview"):Headlines(p,SelectedSystem)).Concat(p.Costs),m.Caption)));
        }
    }
}
